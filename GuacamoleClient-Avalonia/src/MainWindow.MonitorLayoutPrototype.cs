using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using GuacamoleClient.Common.Settings;
using System;
using System.Linq;
using System.Text.Json;

namespace GuacClient;

public partial class MainWindow
{
    private const string MonitorLayoutHostObjectName = "GuacamoleMonitorHost";
    private const string MonitorLayoutReadyMessageType = "guacamoleClient.monitorLayoutPrototype.ready";
    private const string MonitorLayoutMessageType = "guacamoleClient.monitorLayout";
    private const int MonitorLayoutSchemaVersion = 1;

    private bool _monitorLayoutPrototypeNegotiated;

    private void InitializeMonitorLayoutPrototypeBridge()
    {
        _web.RegisterJavascriptObject(MonitorLayoutHostObjectName, new Func<string, bool>(HandleMonitorLayoutPrototypeReady));
        Screens.Changed += Screens_ChangedForMonitorLayoutPrototype;
        Closed += (_, __) => Screens.Changed -= Screens_ChangedForMonitorLayoutPrototype;
    }

    private bool HandleMonitorLayoutPrototypeReady(string messageJson)
    {
        if (!IsTrustedMonitorLayoutPrototypeSource(_web.Address))
            return false;

        try
        {
            using var message = JsonDocument.Parse(messageJson);
            var root = message.RootElement;
            if (!root.TryGetProperty("type", out var type)
                || type.GetString() != MonitorLayoutReadyMessageType
                || !root.TryGetProperty("version", out var version)
                || version.GetInt32() != MonitorLayoutSchemaVersion
                || !SupportsMonitorLayoutVersion(root, MonitorLayoutSchemaVersion))
                return false;

            _monitorLayoutPrototypeNegotiated = true;
            Dispatcher.UIThread.Post(SendMonitorLayoutPrototypePayload);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void MonitorLayoutPrototypeNavigationChanged(string url)
    {
        if (!IsTrustedMonitorLayoutPrototypeSource(url))
            _monitorLayoutPrototypeNegotiated = false;
    }

    private bool IsTrustedMonitorLayoutPrototypeSource(string? source)
    {
        if (_activeProfile?.ProfileKind != GuacamoleServerProfileKind.MonitorLayoutPrototype
            || !Uri.TryCreate(source, UriKind.Absolute, out var sourceUri)
            || !Uri.TryCreate(_activeProfile.Url, UriKind.Absolute, out var profileUri))
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

    private void Screens_ChangedForMonitorLayoutPrototype(object? sender, EventArgs e)
    {
        if (_monitorLayoutPrototypeNegotiated)
            Dispatcher.UIThread.Post(SendMonitorLayoutPrototypePayload);
    }

    private void SendMonitorLayoutPrototypePayload()
    {
        if (!_monitorLayoutPrototypeNegotiated || !IsTrustedMonitorLayoutPrototypeSource(_web.Address))
            return;

        var screens = Screens.All;
        if (screens.Count == 0)
            return;

        int left = screens.Min(screen => screen.Bounds.X);
        int top = screens.Min(screen => screen.Bounds.Y);
        int right = screens.Max(screen => screen.Bounds.Right);
        int bottom = screens.Max(screen => screen.Bounds.Bottom);
        double activeScale = Screens.ScreenFromVisual(_web)?.Scaling ?? 1d;
        var monitors = screens.Select((screen, index) => new
        {
            id = string.IsNullOrWhiteSpace(screen.DisplayName) ? $"DISPLAY-{index + 1}" : screen.DisplayName,
            x = screen.Bounds.X,
            y = screen.Bounds.Y,
            width = screen.Bounds.Width,
            height = screen.Bounds.Height,
            primary = screen.IsPrimary,
            orientation = GetOrientationDegrees(screen.CurrentOrientation),
            scalePercent = (int)Math.Round(screen.Scaling * 100d),
            dpiX = (int)Math.Round(screen.Scaling * 96d),
            dpiY = (int)Math.Round(screen.Scaling * 96d)
        }).ToArray();

        var payload = new
        {
            type = MonitorLayoutMessageType,
            version = MonitorLayoutSchemaVersion,
            coordinateSpace = "avaloniaPhysicalPixels",
            revision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            capturedAtUtc = DateTimeOffset.UtcNow,
            primaryMonitorId = monitors.FirstOrDefault(monitor => monitor.primary)?.id,
            legacyDisplay = new
            {
                width = Math.Max(1, (int)Math.Round(_web.Bounds.Width * activeScale)),
                height = Math.Max(1, (int)Math.Round(_web.Bounds.Height * activeScale)),
                dpi = Math.Max(1, (int)Math.Round(activeScale * 96d))
            },
            boundingRectangle = new { x = left, y = top, width = right - left, height = bottom - top },
            monitors
        };

        string payloadJson = JsonSerializer.Serialize(payload);
        _web.ExecuteScript($"window.GuacamoleMonitorPrototype?.receiveLayout({payloadJson});", frameName: null!);
    }

    private static int GetOrientationDegrees(ScreenOrientation orientation)
        => orientation switch
        {
            ScreenOrientation.Portrait => 90,
            ScreenOrientation.LandscapeFlipped => 180,
            ScreenOrientation.PortraitFlipped => 270,
            _ => 0
        };
}
