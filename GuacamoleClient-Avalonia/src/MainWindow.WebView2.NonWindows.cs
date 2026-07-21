using Avalonia.Controls;
using Avalonia.Threading;
using GuacamoleClient.Common.Localization;
using System;
using System.Threading.Tasks;

namespace GuacClient
{
    public partial class MainWindow
    {
        private const string HostShortcutMessagePrefix = "guacamoleclient:host-shortcut:";

        private void ConfigurePlatformWebViewEvents()
            => _web.WebMessageReceived += Web_WebMessageReceived;

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
        }
    }
}
