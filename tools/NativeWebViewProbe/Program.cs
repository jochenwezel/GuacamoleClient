using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
#if PROBE_WINDOWS
using Microsoft.Web.WebView2.Core;
#endif

namespace NativeWebViewProbe;

internal static class Program
{
    internal static string Phase { get; private set; } = "seed";
    internal static string Output { get; private set; } = "";
    internal static int Port { get; private set; } = 49171;

    [STAThread]
    private static void Main(string[] args)
    {
        for (int i = 0; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException("Arguments require a value.");
            switch (args[i])
            {
                case "--phase": Phase = args[i + 1]; break;
                case "--output": Output = Path.GetFullPath(args[i + 1]); break;
                case "--port": Port = int.Parse(args[i + 1]); break;
                default: throw new ArgumentException("Unknown probe argument.");
            }
        }
        if (Phase is not ("seed" or "verify"))
            throw new ArgumentException("Phase must be seed or verify.");
        if (Output.Length == 0)
            Output = Path.Combine(Path.GetTempPath(), "GuacamoleNativeWebViewProbe", "local");
        Directory.CreateDirectory(Output);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            File.WriteAllText(Path.Combine(Output, "crash.txt"), e.ExceptionObject.ToString());
        AppBuilder.Configure<ProbeApp>().UsePlatformDetect().LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}

internal sealed class ProbeApp : Application
{
    private readonly List<object> _results = [];
    private readonly List<ProbeWindow> _windows = [];
    private HttpListener? _listener;
    private ProbeWindow? _main;
    private DispatcherTimer? _inputTimer;
    private bool _polling;

    /// <inheritdoc/>
    public override void Initialize() => Styles.Add(new FluentTheme());

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Program.Port}/");
            _listener.Start();
            _ = ServeAsync(_listener);
            _main = NewWindow("persistent-a", false);
            desktop.MainWindow = _main;
            desktop.Exit += (_, _) => _listener.Close();
            _ = RunAsync();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private ProbeWindow NewWindow(string profile, bool secure)
    {
        var window = new ProbeWindow(profile, secure);
        _windows.Add(window);
        return window;
    }

    private async Task ServeAsync(HttpListener listener)
    {
        try
        {
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                var path = context.Request.Url!.AbsolutePath;
                string text;
                if (path == "/shutdown" && context.Request.HttpMethod == "POST")
                {
                    text = "Closing probe windows.";
                    Dispatcher.UIThread.Post(() =>
                        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown());
                }
                else if (path == "/report")
                {
                    context.Response.ContentType = "application/json";
                    string reportPath = Path.Combine(Program.Output, $"results-{Program.Phase}.json");
                    text = File.Exists(reportPath) ? await File.ReadAllTextAsync(reportPath) : "{\"running\":true}";
                }
                else
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    using var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("NativeWebViewProbe.fixture.html")!;
                    using var reader = new StreamReader(stream);
                    text = await reader.ReadToEndAsync();
                }
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }
        catch (Exception e) when (e is HttpListenerException or ObjectDisposedException) { }
    }

    private void Record(string name, bool passed, string detail)
        => _results.Add(new { name, passed, detail });

    private async Task CheckAsync(string name, Func<Task<bool>> test)
    {
        try { Record(name, await test(), "Executed against live NativeWebView."); }
        catch (Exception e) { Record(name, false, e.ToString()); }
    }

    private void SaveReport() => File.WriteAllText(
        Path.Combine(Program.Output, $"results-{Program.Phase}.json"),
        JsonSerializer.Serialize(new
        {
            timestamp = DateTimeOffset.UtcNow,
            phase = Program.Phase,
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            avalonia = typeof(Application).Assembly.GetName().Version?.ToString(),
            webview = typeof(NativeWebView).Assembly.GetName().Version?.ToString(),
            results = _results,
            limitations = new[]
            {
                "Local fixture tests are not a real Guacamole/RDP session.",
                "DevTools keyboard injection does not verify an operating-system keyboard hook.",
                "InPrivate state checks do not establish the absence of sensitive disk artifacts.",
                "A local build does not establish macOS or Linux runtime behavior."
            }
        }, new JsonSerializerOptions { WriteIndented = true }));

    private async Task RunAsync()
    {
        try
        {
            await _main!.Ready.Task.WaitAsync(TimeSpan.FromSeconds(45));
            Record("navigation", true, await _main.ScriptAsync("navigator.userAgent"));
            await CheckAsync("javascript-roundtrip", async () =>
                await _main.ScriptAsync("String(6 * 7)") == "42");
            await CheckAsync("web-message", async () =>
            {
                var received = new TaskCompletionSource<string>();
                void Handler(object? sender, WebMessageReceivedEventArgs e) => received.TrySetResult(e.Body ?? "");
                _main.Web.WebMessageReceived += Handler;
                try
                {
                    await _main.ScriptAsync("invokeCSharpAction('probe-message')");
                    return await received.Task.WaitAsync(TimeSpan.FromSeconds(5)) == "probe-message";
                }
                finally { _main.Web.WebMessageReceived -= Handler; }
            });
            if (Program.Phase == "seed")
                await _main.ScriptAsync("localStorage.setItem('probe','persistent-marker'); document.cookie='probe=persistent-marker; max-age=3600; path=/'; 'seeded'");
            else
                await CheckAsync("persistent-state-after-process-restart", async () =>
                    await _main.ScriptAsync("String(localStorage.getItem('probe')==='persistent-marker' && document.cookie.includes('probe=persistent-marker'))") == "true");

            var sibling = NewWindow("persistent-a", false);
            sibling.Show();
            await sibling.Ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await CheckAsync("same-profile-shared-state", async () =>
                await sibling.ScriptAsync("localStorage.getItem('probe')") == "persistent-marker");
            var other = NewWindow("persistent-b", false);
            other.Show();
            await other.Ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await CheckAsync("different-profile-isolation", async () =>
                await other.ScriptAsync("String(localStorage.getItem('probe')===null && !document.cookie.includes('probe='))") == "true");
            other.Close();
            sibling.Close();

            var secure = NewWindow("secure", true);
            secure.Show();
            await secure.Ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await CheckAsync("private-state-empty", async () =>
                await secure.ScriptAsync("String(localStorage.getItem('probe')===null && !document.cookie.includes('probe='))") == "true");
            await secure.ScriptAsync("localStorage.setItem('probe','private-marker'); document.cookie='probe=private-marker; path=/'; 'seeded'");
            var secureSibling = NewWindow("secure", true);
            secureSibling.Show();
            await secureSibling.Ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await CheckAsync("private-sibling-shares-session", async () =>
                await secureSibling.ScriptAsync("String(localStorage.getItem('probe')==='private-marker' && document.cookie.includes('private-marker'))") == "true");
            secure.Close();
            await CheckAsync("private-sibling-survives-owner-close", async () =>
                await secureSibling.ScriptAsync("localStorage.getItem('probe')") == "private-marker");
            secureSibling.Close();
            await CheckAsync("private-marker-not-in-persistent-profile", async () =>
                await _main.ScriptAsync("localStorage.getItem('probe')") == "persistent-marker");

#if PROBE_WINDOWS
            await CheckAsync("windows-screenshot", async () =>
            {
                var core = _main.GetWindowsCore();
                await using var file = File.Create(Path.Combine(Program.Output, $"browser-{Program.Phase}.png"));
                await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, file);
                return file.Length > 1024;
            });
            await CheckAsync("windows-devtools-keyboard-bridge", async () =>
            {
                await _main.ScriptAsync("document.getElementById('input').focus(); probeEvents.length=0; 'ready'");
                var core = _main.GetWindowsCore();
                await core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent",
                    "{\"type\":\"rawKeyDown\",\"key\":\"F4\",\"code\":\"F4\",\"windowsVirtualKeyCode\":115,\"modifiers\":2}");
                await core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent",
                    "{\"type\":\"keyUp\",\"key\":\"F4\",\"code\":\"F4\",\"windowsVirtualKeyCode\":115,\"modifiers\":2}");
                return await _main.ScriptAsync("String(probeEvents.some(e=>e.type==='keydown' && e.key==='F4' && e.ctrl && e.trusted) && probeEvents.some(e=>e.type==='keyup' && e.key==='F4'))") == "true";
            });
#endif
            await _main.ScriptAsync("probeEvents.length=0; document.getElementById('input').value=''; document.getElementById('input').focus(); 'ready'");
            _main.Status.Text = "Automated checks finished. Keyboard input and overlay ready for inspection.";
            _main.Activate();
            _inputTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _inputTimer.Tick += async (_, _) =>
            {
                if (_polling) return;
                _polling = true;
                try
                {
                    string state = await _main.ScriptAsync("JSON.stringify(probeState())");
                    File.WriteAllText(Path.Combine(Program.Output, $"input-{Program.Phase}.json"), state);
                }
                catch { _inputTimer?.Stop(); }
                finally { _polling = false; }
            };
            _inputTimer.Start();
        }
        catch (Exception e)
        {
            Record("probe-run", false, e.ToString());
            if (_main is not null) _main.Status.Text = "Probe failed; see results JSON.";
        }
        finally { SaveReport(); }
    }
}

internal sealed class ProbeWindow : Window
{
    internal NativeWebView Web { get; } = new();
    internal TextBlock Status { get; } = new() { Text = "Running browser integration checks...", Margin = new Thickness(12) };
    internal TaskCompletionSource Ready { get; } = new();

    internal ProbeWindow(string profile, bool secure)
    {
        Title = $"Guacamole NativeWebView Probe - {profile} - {Program.Phase}";
        Width = 1100;
        Height = 800;
        Web.EnvironmentRequested += (_, args) =>
        {
            string profileDirectory = Path.Combine(Program.Output, "profiles", profile);
            switch (args)
            {
                case WindowsWebView2EnvironmentRequestedEventArgs windows:
                    windows.UserDataFolder = profileDirectory;
                    windows.ProfileName = profile;
                    windows.IsInPrivateModeEnabled = secure;
                    break;
                case AppleWKWebViewEnvironmentRequestedEventArgs apple:
                    apple.NonPersistentDataStore = secure;
                    apple.DataStoreIdentifier = profile == "persistent-a"
                        ? Guid.Parse("7fac3285-795c-47de-b402-c16530805aa1")
                        : Guid.Parse("7fac3285-795c-47de-b402-c16530805aa2");
                    break;
                case GtkWebViewEnvironmentRequestedEventArgs gtk:
                    gtk.EphemeralDataManager = secure;
                    gtk.BaseDataDirectory = profileDirectory;
                    gtk.BaseCacheDirectory = Path.Combine(profileDirectory, "cache");
                    break;
                case LinuxWpeWebViewEnvironmentRequestedEventArgs wpe:
                    // WPE's exposed settings do not offer an ephemeral data-store switch.
                    // Keep its disk state isolated, but do not claim memory-only privacy.
                    wpe.DataDirectory = profileDirectory;
                    wpe.CacheDirectory = Path.Combine(profileDirectory, "cache");
                    break;
            }
        };
        Web.NavigationCompleted += (_, e) =>
        {
            if (e.IsSuccess) Ready.TrySetResult();
            else Ready.TrySetException(new InvalidOperationException("Fixture navigation failed."));
        };
        var menu = new Menu { ItemsSource = new[]
        {
            new MenuItem { Header = "Probe menu", ItemsSource = new Control[]
            {
                new MenuItem { Header = "Normal item" }, new Separator(),
                new MenuItem { Header = "Disabled item", IsEnabled = false }
            } }
        } };
        var header = new StackPanel { Children = { menu, Status } };
        var grid = new Grid();
        grid.Children.Add(Web);
        grid.Children.Add(new Border
        {
            Background = Brushes.Crimson, Padding = new Thickness(14),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(20), IsHitTestVisible = false,
            Child = new TextBlock { Text = "OVERLAY TEST", Foreground = Brushes.White }
        });
        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        dock.Children.Add(grid);
        Content = dock;
        Web.Source = new Uri($"http://127.0.0.1:{Program.Port}/fixture");
    }

    internal async Task<string> ScriptAsync(string script)
    {
        string? value = await Web.InvokeScript(script);
        if (value is null) return "null";
        try
        {
            using var json = JsonDocument.Parse(value);
            if (json.RootElement.ValueKind == JsonValueKind.String)
                return json.RootElement.GetString() ?? "null";
        }
        catch (JsonException) { }
        return value;
    }

#if PROBE_WINDOWS
    internal CoreWebView2 GetWindowsCore()
    {
        if (Web.TryGetPlatformHandle() is not IWindowsWebView2PlatformHandle handle)
            throw new PlatformNotSupportedException("The WebView2 native handle is unavailable.");
        return CoreWebView2.CreateFromComICoreWebView2(handle.CoreWebView2);
    }
#endif
}
