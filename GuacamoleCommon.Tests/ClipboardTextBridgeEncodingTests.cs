using GuacamoleClient.Common;
using NUnit.Framework;
using System.Text.Json;

namespace GuacamoleCommon.Tests;

public class ClipboardTextBridgeEncodingTests
{
    [Test]
    public void SerializeForJavaScript_PreservesUnicodeAndEscapesScriptSyntax()
    {
        const string text = "line 1\r\nline 2: \"quoted\" \\ path; </script> 東京 🖥️ 😀";

        string serialized = ClipboardTextBridgeEncoding.SerializeForJavaScript(text);
        string decoded = JsonSerializer.Deserialize<string>(serialized)!;

        Assert.That(decoded, Is.EqualTo(text));
    }
}
