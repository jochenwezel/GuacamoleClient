using System.Text.Json;

namespace GuacamoleClient.Common;

internal static class ClipboardTextBridgeEncoding
{
    public static string SerializeForJavaScript(string text)
        => JsonSerializer.Serialize(text);
}
