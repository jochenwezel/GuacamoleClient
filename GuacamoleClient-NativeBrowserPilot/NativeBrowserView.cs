using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using GuacamoleClient.Common.Settings;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Text.Json;

namespace GuacClient;

internal sealed class NativeBrowserView : UserControl, IDisposable
{
    private static readonly NativeProfileSessions Sessions = new(Path.Combine(PilotPaths.DataDirectory, "BrowserProfiles"));
    private readonly Dictionary<string, Func<string, bool>> _registeredFunctions = [];
    private readonly SemaphoreSlim _protocolQueue = new(1, 1);
    private readonly TaskCompletionSource _ready = new();
    private NativeWebView? _native;
    private CoreWebView2? _core;
    private NativeProfileSessions.Lease? _lease;
    private GuacamoleServerProfile? _profile;
    private bool _disposed;

    internal event Action? TitleChanged;
    internal event Action<string, string>? Navigated;
    internal string Title => _core?.DocumentTitle ?? "";
    internal bool IsBrowserInitialized => _core is not null && !_disposed;
    internal string Address
    {
        get => _core?.Source ?? _native?.Source?.ToString() ?? "about:blank";
        set => (_native ?? throw new InvalidOperationException("Configure the browser profile before navigation.")).Source = new Uri(value);
    }

    internal async Task ConfigureProfileAsync(GuacamoleServerProfile? profile)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        profile = profile?.Clone();
        if (_native is not null)
        {
            if (_profile?.Id != profile?.Id || _profile?.LocalCacheEnabled != profile?.LocalCacheEnabled)
                throw new InvalidOperationException("Open another window to change the browser profile.");
            _profile = profile;
            await _ready.Task.WaitAsync(TimeSpan.FromSeconds(45));
            return;
        }
        _profile = profile;
        _lease = Sessions.Acquire(profile?.Id ?? Guid.Empty, profile?.LocalCacheEnabled == true);
        var native = new NativeWebView();
        _native = native;
        native.EnvironmentRequested += (_, args) =>
        {
            if (args is WindowsWebView2EnvironmentRequestedEventArgs windows)
            {
                windows.UserDataFolder = _lease.DirectoryPath;
                windows.ProfileName = "profile-" + (profile?.Id ?? Guid.Empty).ToString("N");
                windows.IsInPrivateModeEnabled = _lease.IsPrivate;
            }
        };
        native.AdapterCreated += async (_, _) =>
        {
            if (_disposed) return;
            try
            {
                if (native.TryGetPlatformHandle() is not IWindowsWebView2PlatformHandle handle)
                    throw new PlatformNotSupportedException("The Windows pilot requires Microsoft Edge WebView2 Runtime.");
                _core = CoreWebView2.CreateFromComICoreWebView2(handle.CoreWebView2);
                _core.Settings.IsStatusBarEnabled = false;
                _core.Settings.AreDevToolsEnabled = false;
                _core.Settings.AreBrowserAcceleratorKeysEnabled = false;
                _core.DocumentTitleChanged += CoreTitleChanged;
                _core.SourceChanged += CoreSourceChanged;
                _core.ServerCertificateErrorDetected += CoreCertificateError;
                _core.WebMessageReceived += CoreWebMessageReceived;
                foreach (string name in _registeredFunctions.Keys)
                    await InstallFunctionAsync(name);
                _ready.TrySetResult();
            }
            catch (Exception error) { _ready.TrySetException(error); }
        };
        native.NavigationCompleted += (_, _) => Navigated?.Invoke(Address, "");
        Content = native;
        await _ready.Task.WaitAsync(TimeSpan.FromSeconds(45));
    }

    private void CoreTitleChanged(object? sender, object e) => TitleChanged?.Invoke();
    private void CoreSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e) => Navigated?.Invoke(Address, "");

    private void CoreCertificateError(object? sender, CoreWebView2ServerCertificateErrorDetectedEventArgs e)
    {
        // Honor only the explicit setting for this profile's exact configured origin.
        if (_profile?.IgnoreCertificateErrors == true
            && NativeBridgeOrigin.IsSameOrigin(_profile.Url, e.RequestUri))
            e.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
    }

    internal bool Focus() => _native?.Focus() ?? base.Focus();

    internal async Task ExecuteScriptAsync(string script)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => ExecuteScriptAsync(script));
            return;
        }
        await (_core ?? throw new InvalidOperationException("The browser is unavailable.")).ExecuteScriptAsync(script);
    }

    internal void ExecuteScript(string script, string? frameName = null) => _ = ObserveAsync(ExecuteScriptAsync(script));

    private static async Task ObserveAsync(Task task)
    {
        try { await task; }
        catch (Exception error) { Trace.TraceError("Native browser operation failed: {0}", error.Message); }
    }

    internal async Task SendKeyAsync(Key key, int virtualKey, string type, IReadOnlyCollection<Key> modifiers, char? character = null)
    {
        // Window activation can deliver modifier releases before AdapterCreated.
        // There is no document to receive those keys until the browser is ready.
        if (!IsBrowserInitialized) return;
        string payload = NativeKeyEventBuilder.Build(key, virtualKey, type, modifiers, character);
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => DispatchKeyAsync(payload));
            return;
        }
        await DispatchKeyAsync(payload);
    }

    private async Task DispatchKeyAsync(string payload)
    {
        await _protocolQueue.WaitAsync();
        try
        {
            if (_disposed || _core is null) return;
            await _core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", payload);
        }
        finally { _protocolQueue.Release(); }
    }

    internal async Task<byte[]> CaptureAsync()
    {
        using var stream = new MemoryStream();
        await (_core ?? throw new InvalidOperationException("The browser is unavailable."))
            .CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
        return stream.ToArray();
    }

    internal void RegisterJavascriptObject(string name, Func<string, bool> callback)
    {
        _registeredFunctions.Add(name, callback);
        if (_core is not null) _ = ObserveAsync(InstallFunctionAsync(name));
    }

    private async Task InstallFunctionAsync(string name)
    {
        string serializedName = JsonSerializer.Serialize(name);
        string script = $$"""
            (() => {
                const pending = window.__guacamolePilotPending ||= new Map();
                window.__guacamolePilotResolve = (id, result) => {
                    const resolve = pending.get(id);
                    if (resolve) { pending.delete(id); resolve(result); }
                };
                window[{{serializedName}}] = (message) => new Promise(resolve => {
                    const id = crypto.randomUUID();
                    pending.set(id, resolve);
                    window.chrome.webview.postMessage(JSON.stringify({
                        kind: 'guacamolePilotCall', name: {{serializedName}}, id, message
                    }));
                });
            })();
            """;
        await _core!.AddScriptToExecuteOnDocumentCreatedAsync(script);
        await _core.ExecuteScriptAsync(script);
    }

    private async void CoreWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            if (!NativeBridgeOrigin.IsSameOrigin(Address, e.Source)) return;
            using var json = JsonDocument.Parse(e.TryGetWebMessageAsString());
            var root = json.RootElement;
            if (!root.TryGetProperty("kind", out var kind) || kind.GetString() != "guacamolePilotCall") return;
            string? name = root.GetProperty("name").GetString();
            if (name is null || !_registeredFunctions.TryGetValue(name, out var callback)) return;
            bool result = callback(root.GetProperty("message").GetString() ?? "");
            string id = JsonSerializer.Serialize(root.GetProperty("id").GetString());
            await ExecuteScriptAsync($"window.__guacamolePilotResolve?.({id},{JsonSerializer.Serialize(result)});");
        }
        catch (Exception error)
        {
            Trace.TraceWarning("Rejected native browser bridge message: {0}", error.Message);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _ready.TrySetCanceled();
        if (_core is not null)
        {
            _core.DocumentTitleChanged -= CoreTitleChanged;
            _core.SourceChanged -= CoreSourceChanged;
            _core.ServerCertificateErrorDetected -= CoreCertificateError;
            _core.WebMessageReceived -= CoreWebMessageReceived;
        }
        Content = null;
        _core = null;
        _native = null;
        _lease?.Dispose();
        _lease = null;
    }
}
