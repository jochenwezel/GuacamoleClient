using GuacamoleClient.Common.Settings;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        private const string MonitorLayoutReadyMessageType = "guacamoleClient.monitorLayoutPrototype.ready";
        private const string MonitorLayoutMessageType = "guacamoleClient.monitorLayout";
        private const int MonitorLayoutSchemaVersion = 1;
        private bool _monitorLayoutPrototypeNegotiated;

        private void InitializeMonitorLayoutPrototypeBridge()
        {
            if (_webview2_core == null || ServerProfile.ProfileKind != GuacamoleServerProfileKind.MonitorLayoutPrototype)
                return;

            _webview2_core.WebMessageReceived += MonitorLayoutPrototype_WebMessageReceived;
            SystemEvents.DisplaySettingsChanged += MonitorLayoutPrototype_DisplaySettingsChanged;
            WebBrowserHostPanel!.Resize += MonitorLayoutPrototypeHostPanel_Resize;
            FormClosed += (_, __) =>
            {
                SystemEvents.DisplaySettingsChanged -= MonitorLayoutPrototype_DisplaySettingsChanged;
                if (WebBrowserHostPanel != null)
                    WebBrowserHostPanel.Resize -= MonitorLayoutPrototypeHostPanel_Resize;
            };
        }

        private void MonitorLayoutPrototype_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (_webview2_core == null || !IsTrustedPrototypeSource(e.Source))
                return;

            try
            {
                using var message = JsonDocument.Parse(e.WebMessageAsJson);
                var root = message.RootElement;
                if (!root.TryGetProperty("type", out var type)
                    || type.GetString() != MonitorLayoutReadyMessageType
                    || !root.TryGetProperty("version", out var version)
                    || version.GetInt32() != MonitorLayoutSchemaVersion
                    || !SupportsMonitorLayoutVersion(root, MonitorLayoutSchemaVersion))
                    return;

                _monitorLayoutPrototypeNegotiated = true;
                SendMonitorLayoutPrototypePayload();
            }
            catch (JsonException)
            {
                // Ignore malformed or unrelated page messages.
            }
        }

        private bool IsTrustedPrototypeSource(string source)
        {
            if (ServerProfile.ProfileKind != GuacamoleServerProfileKind.MonitorLayoutPrototype
                || !Uri.TryCreate(source, UriKind.Absolute, out var sourceUri)
                || !Uri.TryCreate(ServerProfile.Url, UriKind.Absolute, out var profileUri))
                return false;

            return string.Equals(sourceUri.Scheme, profileUri.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(sourceUri.Host, profileUri.Host, StringComparison.OrdinalIgnoreCase)
                && sourceUri.Port == profileUri.Port;
        }

        private static bool SupportsMonitorLayoutVersion(JsonElement root, int version)
        {
            if (!root.TryGetProperty("capabilities", out var capabilities)
                || !capabilities.TryGetProperty("monitorLayoutVersions", out var versions)
                || versions.ValueKind != JsonValueKind.Array)
                return false;

            return versions.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.Number && item.GetInt32() == version);
        }

        private void SendMonitorLayoutPrototypePayload()
        {
            if (_webview2_core == null
                || !_monitorLayoutPrototypeNegotiated
                || !IsTrustedPrototypeSource(_webview2_core.Source))
                return;

            var screens = Screen.AllScreens;
            Rectangle desktopBounds = screens.Select(screen => screen.Bounds)
                .Aggregate((current, next) => Rectangle.Union(current, next));
            var monitors = screens.Select(screen =>
            {
                GetMonitorDpi(screen, out uint dpiX, out uint dpiY);
                return new
                {
                    id = screen.DeviceName,
                    x = screen.Bounds.X,
                    y = screen.Bounds.Y,
                    width = screen.Bounds.Width,
                    height = screen.Bounds.Height,
                    primary = screen.Primary,
                    orientation = 0,
                    scalePercent = (int)Math.Round(dpiX / 96d * 100d),
                    dpiX,
                    dpiY
                };
            }).ToArray();

            var payload = new
            {
                type = MonitorLayoutMessageType,
                version = MonitorLayoutSchemaVersion,
                coordinateSpace = "windowsPhysicalPixels",
                revision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                capturedAtUtc = DateTimeOffset.UtcNow,
                primaryMonitorId = screens.FirstOrDefault(screen => screen.Primary)?.DeviceName,
                legacyDisplay = new
                {
                    width = WebBrowserHostPanel?.ClientSize.Width ?? desktopBounds.Width,
                    height = WebBrowserHostPanel?.ClientSize.Height ?? desktopBounds.Height,
                    dpi = DeviceDpi
                },
                boundingRectangle = new
                {
                    x = desktopBounds.X,
                    y = desktopBounds.Y,
                    width = desktopBounds.Width,
                    height = desktopBounds.Height
                },
                monitors
            };

            _webview2_core.PostWebMessageAsJson(JsonSerializer.Serialize(payload));
        }

        private void MonitorLayoutPrototype_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            if (!_monitorLayoutPrototypeNegotiated || IsDisposed)
                return;

            BeginInvoke(SendMonitorLayoutPrototypePayload);
        }

        private void MonitorLayoutPrototypeHostPanel_Resize(object? sender, EventArgs e)
        {
            if (_monitorLayoutPrototypeNegotiated)
                SendMonitorLayoutPrototypePayload();
        }

        private static void GetMonitorDpi(Screen screen, out uint dpiX, out uint dpiY)
        {
            dpiX = 96;
            dpiY = 96;
            IntPtr monitor = MonitorFromPoint(new NativePoint(screen.Bounds.Left, screen.Bounds.Top), MonitorDefaultToNearest);
            if (monitor != IntPtr.Zero)
            {
                int result = GetDpiForMonitor(monitor, MonitorDpiType.Effective, out uint detectedDpiX, out uint detectedDpiY);
                if (result == 0 && detectedDpiX > 0 && detectedDpiY > 0)
                {
                    dpiX = detectedDpiX;
                    dpiY = detectedDpiY;
                }
            }
        }

        private const uint MonitorDefaultToNearest = 2;

        private enum MonitorDpiType
        {
            Effective = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public NativePoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr monitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);
    }
}
