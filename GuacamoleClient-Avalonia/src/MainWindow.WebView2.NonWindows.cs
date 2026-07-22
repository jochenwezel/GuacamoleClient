using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using GuacamoleClient.Common.Localization;
using System;
using System.Threading.Tasks;

namespace GuacClient
{
    public partial class MainWindow
    {
        private const string HostShortcutMessagePrefix = "guacamoleclient:host-shortcut:";
        private readonly TaskCompletionSource<bool> _linuxWebViewAdapterCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _linuxWebViewSurfaceRefreshApplied;
        private IntPtr _linuxGtkWebViewHandle;

        private void ConfigurePlatformWebViewEvents()
        {
            _web.WebMessageReceived += Web_WebMessageReceived;
            if (OperatingSystem.IsLinux())
                _web.SizeChanged += async (_, _) => await ApplyLinuxWebViewSizeSafeAsync().ConfigureAwait(true);
        }

        private async Task ConfigurePlatformWebViewPageAsync(Uri? request)
        {
            if (request == null || !_trustedHosts.Contains(request.Host))
                return;

            const string script = """
                (() => {
                    if (window.__guacamoleClientHostShortcutsInstalled)
                        return;

                    window.__guacamoleClientHostShortcutsInstalled = true;
                    const sendHostShortcut = action => {
                        const message = 'guacamoleclient:host-shortcut:' + action;
                        if (typeof window.invokeCSharpAction === 'function') {
                            window.invokeCSharpAction(message);
                            return;
                        }

                        window.webkit?.messageHandlers?.invokeCSharpAction?.postMessage(message);
                    };

                    window.addEventListener('keydown', event => {
                        if (!event.ctrlKey || !event.altKey || event.shiftKey || event.metaKey)
                            return;

                        let action = null;
                        switch (event.code) {
                            case 'Backspace': action = 'toggle-keyboard-capture'; break;
                            case 'F4': action = 'close'; break;
                            case 'Insert': action = 'toggle-full-screen'; break;
                            case 'Home': action = 'connection-home'; break;
                            case 'KeyN': action = 'new-window'; break;
                            case 'Pause': action = 'exit-full-screen'; break;
                        }

                        if (!action)
                            return;

                        event.preventDefault();
                        event.stopImmediatePropagation();
                        if (!event.repeat)
                            sendHostShortcut(action);
                    }, true);
                })();
                """;

            try
            {
                await _web.InvokeScript(script).ConfigureAwait(true);
            }
            catch (InvalidOperationException)
            {
                // A redirect can replace the document while the optional shortcut bridge is being installed.
            }

        }

        private async Task RefreshPlatformWebViewAfterNavigationStartedAsync(string url)
        {
            if (!OperatingSystem.IsLinux() || _linuxWebViewSurfaceRefreshApplied)
                return;

            await Task.WhenAny(
                _linuxWebViewAdapterCreated.Task,
                Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(true);
            await Task.Delay(250).ConfigureAwait(true);

            if (_closeRequested)
                return;

            _linuxWebViewSurfaceRefreshApplied = true;
            await ApplyLinuxWebViewSizeSafeAsync().ConfigureAwait(true);
            _web.IsVisible = false;
            await Task.Delay(50).ConfigureAwait(true);
            _web.IsVisible = true;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
            await ApplyLinuxWebViewSizeSafeAsync().ConfigureAwait(true);

            // Reattachment can discard a navigation that started against the original XEmbed surface.
            NavigateWebView(url);
        }

        private async Task ApplyLinuxWebViewSizeSafeAsync()
        {
            if (_linuxGtkWebViewHandle == IntPtr.Zero || !_web.IsVisible)
                return;

            try
            {
                double scaling = TopLevel.GetTopLevel(_web)?.RenderScaling ?? 1.0;
                int width = Math.Max(1, (int)Math.Ceiling(_web.Bounds.Width * scaling));
                int height = Math.Max(1, (int)Math.Ceiling(_web.Bounds.Height * scaling));
                await GtkWebViewSizeWorkaround.ApplyAsync(_linuxGtkWebViewHandle, width, height).ConfigureAwait(true);
            }
            catch (Exception)
            {
                // The native WebView can disappear while a queued GTK resize is pending during shutdown.
            }
        }

        private void Web_WebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
        {
            string? message = e.Body?.Trim().Trim('"');
            if (message == null || !message.StartsWith(HostShortcutMessagePrefix, StringComparison.Ordinal))
                return;

            Uri? source = _web.Source;
            if (source == null || !_trustedHosts.Contains(source.Host))
                return;

            string action = message[HostShortcutMessagePrefix.Length..];
            Dispatcher.UIThread.Post(() => HandleWebViewHostShortcut(action), DispatcherPriority.Input);
        }

        private void HandleWebViewHostShortcut(string action)
        {
            switch (action)
            {
                case "toggle-keyboard-capture":
                    ToggleKeyboardCapture();
                    break;
                case "close":
                    _ = CloseApplicationWithHintAsync();
                    break;
                case "toggle-full-screen":
                    SetFullScreenMode(
                        WindowState != WindowState.FullScreen,
                        WindowState == WindowState.FullScreen
                            ? LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff)
                            : LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn));
                    break;
                case "connection-home":
                    GoToConnectionHome();
                    break;
                case "new-window":
                    OpenNewWindow();
                    break;
                case "exit-full-screen" when WindowState == WindowState.FullScreen:
                    SetFullScreenMode(false, LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff));
                    break;
            }
        }

        private void ConfigurePlatformWebViewAdapter(WebViewAdapterEventArgs e)
        {
            if (!OperatingSystem.IsLinux() || e.TryGetPlatformHandle() is not IGtkWebViewPlatformHandle gtkHandle)
                return;

            _linuxGtkWebViewHandle = gtkHandle.WebKitWebView;
            _linuxWebViewAdapterCreated.TrySetResult(true);
            _ = ApplyLinuxWebViewSizeSafeAsync();
        }
    }

}
