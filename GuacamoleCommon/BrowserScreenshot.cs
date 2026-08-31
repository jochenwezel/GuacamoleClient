using System.Text.Json;

using System.Globalization;
using System.Text;

namespace GuacamoleClient.Common;

/// <summary>
/// Provides the Chromium request and response handling for browser screenshots.
/// </summary>
public static class BrowserScreenshot
{
    /// <summary>
    /// Gets the DevTools method that captures the browser viewport.
    /// </summary>
    public const string CaptureMethod = "Page.captureScreenshot";

    /// <summary>
    /// Gets the parameters for a PNG screenshot limited to the visible viewport.
    /// </summary>
    public const string CaptureParameters = "{\"format\":\"png\",\"fromSurface\":true,\"captureBeyondViewport\":false}";

    /// <summary>
    /// Creates a portable screenshot filename from the current connection title.
    /// </summary>
    /// <param name="documentTitle">The browser document title used in the application title bar.</param>
    /// <param name="currentUrl">The current browser URL used to identify a Guacamole connection page.</param>
    /// <param name="capturedAt">The local capture time to include without culture-specific formatting.</param>
    /// <returns>A PNG filename using the sanitized connection title, or GuacamoleClient outside a connection page.</returns>
    /// <remarks>
    /// Applies Windows, Linux, and macOS restrictions on every platform. Invalid characters are replaced
    /// with underscores, leading and trailing spaces and periods are removed, and reserved device names
    /// are prefixed with an underscore. The title is limited to 200 UTF-8 bytes after canonical decomposition
    /// to leave room for the timestamp and extension without splitting Unicode characters.
    /// </remarks>
    public static string CreateFileName(string? documentTitle, string? currentUrl, DateTime capturedAt)
    {
        const string connectionRoute = "#/client/";
        bool isConnection = Uri.TryCreate(currentUrl, UriKind.Absolute, out var uri)
            && uri.Fragment.StartsWith(connectionRoute, StringComparison.Ordinal)
            && uri.Fragment.Length > connectionRoute.Length;
        string title = isConnection ? documentTitle?.Trim() ?? string.Empty : string.Empty;
        var prefix = new StringBuilder();
        int byteCount = 0;

        // Do not use Path.GetInvalidFileNameChars(): its rules depend on the current OS.
        foreach (var rune in title.EnumerateRunes())
        {
            string part = Rune.IsControl(rune) || "<>:\"/\\|?*".Contains(rune.ToString(), StringComparison.Ordinal)
                ? "_"
                : rune.ToString();
            int partBytes = Encoding.UTF8.GetByteCount(part.Normalize(NormalizationForm.FormD));
            if (byteCount + partBytes > 200)
                break;
            prefix.Append(part);
            byteCount += partBytes;
        }

        string name = prefix.ToString().Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(name))
            name = "GuacamoleClient";

        // Windows reserves these names even when followed by a dot and an extension.
        string deviceName = name.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
        bool reserved = deviceName is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" or "CLOCK$"
            || (deviceName.Length == 4
                && (deviceName.StartsWith("COM", StringComparison.Ordinal) || deviceName.StartsWith("LPT", StringComparison.Ordinal))
                && "123456789¹²³".Contains(deviceName[3]));
        if (reserved)
            name = "_" + name;

        return name + "-" + capturedAt.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".png";
    }

    /// <summary>
    /// Decodes the PNG data returned by Chromium.
    /// </summary>
    /// <param name="responseJson">The DevTools method result containing base64-encoded image data.</param>
    /// <returns>The PNG image bytes.</returns>
    /// <exception cref="InvalidDataException">The response does not contain PNG image data.</exception>
    public static byte[] DecodePngResponse(string responseJson)
    {
        try
        {
            using var response = JsonDocument.Parse(responseJson);
            if (response.RootElement.ValueKind != JsonValueKind.Object
                || !response.RootElement.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("The browser did not return a screenshot.");

            byte[] png = Convert.FromBase64String(data.GetString()!);
            ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
            if (png.Length <= signature.Length || !png.AsSpan(0, signature.Length).SequenceEqual(signature))
                throw new InvalidDataException("The browser did not return a PNG screenshot.");

            return png;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or ArgumentNullException)
        {
            throw new InvalidDataException("The browser returned invalid screenshot data.", ex);
        }
    }
}
