using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;

namespace GuacClient
{
    public partial class MainWindow
    {
        private void ConfigurePlatformWebViewEvents()
        {
        }

        private static Task ConfigurePlatformWebViewPageAsync(Uri? request)
            => Task.CompletedTask;

        private void ConfigurePlatformWebViewAdapter(WebViewAdapterEventArgs e)
        {
            if (e.TryGetPlatformHandle() is not IWindowsWebView2PlatformHandle windowsHandle ||
                windowsHandle.CoreWebView2 == IntPtr.Zero)
            {
                return;
            }

            CoreWebView2 coreWebView = CoreWebView2.CreateFromComICoreWebView2(windowsHandle.CoreWebView2);
            coreWebView.PermissionRequested += CoreWebView2_PermissionRequested;
        }

        private void CoreWebView2_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
                return;

            bool isTrusted = uri.Scheme == Uri.UriSchemeHttps && _trustedHosts.Contains(uri.Host);
            if (isTrusted && e.PermissionKind == CoreWebView2PermissionKind.ClipboardRead)
            {
                e.State = CoreWebView2PermissionState.Allow;
                e.Handled = true;
            }
        }
    }
}
