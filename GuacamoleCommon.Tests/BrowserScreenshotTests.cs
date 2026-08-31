using GuacamoleClient.Common;
using GuacamoleClient.Common.Localization;
using NUnit.Framework;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace GuacamoleCommon.Tests;

public class BrowserScreenshotTests
{
    private const string OnePixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/l9sAAAAASUVORK5CYII=";
    private const string ConnectionUrl = "https://guacamole.example/#/client/connection-id";
    private static readonly DateTime CaptureTime = new(2026, 8, 31, 16, 50, 28);
    private const string FileNameSuffix = "-2026-08-31_16-50-28.png";

    [TestCase("beiweb01", "beiweb01")]
    [TestCase("beiweb01.ad.example.org", "beiweb01.ad.example.org")]
    [TestCase("München 東京 🖥", "München 東京 🖥")]
    [TestCase("host|rdp", "host_rdp")]
    [TestCase("host<>:\"/\\|?*end", "host_________end")]
    [TestCase("host\0\r\n\t\u001f\u007fend", "host______end")]
    [TestCase("  ..beiweb01..  ", "beiweb01")]
    [TestCase("..", "GuacamoleClient")]
    [TestCase(" ", "GuacamoleClient")]
    [TestCase("", "GuacamoleClient")]
    [TestCase(null, "GuacamoleClient")]
    public void CreateFileName_UsesPortableConnectionTitle(string? title, string expectedPrefix)
    {
        // The same title must produce the same portable filename on every host OS.
        Assert.That(BrowserScreenshot.CreateFileName(title, ConnectionUrl, CaptureTime),
            Is.EqualTo(expectedPrefix + FileNameSuffix));
    }

    [TestCase("CON")]
    [TestCase("prn")]
    [TestCase("Aux")]
    [TestCase("NUL")]
    [TestCase("COM1")]
    [TestCase("COM9")]
    [TestCase("LPT1")]
    [TestCase("LPT9")]
    [TestCase("COM¹")]
    [TestCase("COM²")]
    [TestCase("COM³")]
    [TestCase("LPT¹")]
    [TestCase("LPT²")]
    [TestCase("LPT³")]
    [TestCase("CONIN$")]
    [TestCase("CONOUT$")]
    [TestCase("CLOCK$")]
    [TestCase("con.example.org")]
    [TestCase("NUL .example.org")]
    public void CreateFileName_ProtectsReservedWindowsNamesOnEveryPlatform(string title)
    {
        Assert.That(BrowserScreenshot.CreateFileName(title, ConnectionUrl, CaptureTime),
            Is.EqualTo("_" + title + FileNameSuffix));
    }

    [TestCase("console")]
    [TestCase("COM10")]
    [TestCase("LPT10")]
    [TestCase("connection")]
    public void CreateFileName_PreservesNamesThatOnlyResembleDevices(string title)
    {
        Assert.That(BrowserScreenshot.CreateFileName(title, ConnectionUrl, CaptureTime),
            Is.EqualTo(title + FileNameSuffix));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not a URL")]
    [TestCase("https://guacamole.example/#/")]
    [TestCase("https://guacamole.example/#/settings/preferences")]
    [TestCase("https://guacamole.example/#/client/")]
    [TestCase("https://guacamole.example/?next=/client/id")]
    public void CreateFileName_UsesFallbackOutsideConnectionPages(string? url)
    {
        // A stale title after returning home must not name the screenshot after a former connection.
        Assert.That(BrowserScreenshot.CreateFileName("beiweb01", url, CaptureTime),
            Is.EqualTo("GuacamoleClient" + FileNameSuffix));
    }

    [TestCase("host")]
    [TestCase("é")]
    [TestCase("東京")]
    [TestCase("🖥")]
    [TestCase("ᾂ")]
    public void CreateFileName_BoundsLongNamesWithoutBreakingUnicode(string unit)
    {
        string title = string.Concat(Enumerable.Repeat(unit, 300));
        string fileName = BrowserScreenshot.CreateFileName(title, ConnectionUrl, CaptureTime);
        var strictUtf8 = new UTF8Encoding(false, true);

        Assert.Multiple(() =>
        {
            Assert.That(strictUtf8.GetByteCount(fileName.Normalize(NormalizationForm.FormD)), Is.LessThanOrEqualTo(255));
            Assert.That(title, Does.StartWith(fileName[..^FileNameSuffix.Length]));
            Assert.That(fileName, Does.EndWith(FileNameSuffix));
        });
    }

    [Test]
    public void CreateFileName_UsesGregorianTimestampRegardlessOfCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            Assert.That(BrowserScreenshot.CreateFileName("beiweb01", ConnectionUrl, CaptureTime),
                Is.EqualTo("beiweb01" + FileNameSuffix));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Test]
    public void DecodePngResponse_PreservesImageBytes()
    {
        // Saving and clipboard decoding must receive the exact captured PNG.
        byte[] result = BrowserScreenshot.DecodePngResponse(JsonSerializer.Serialize(new { data = OnePixelPng }));
        Assert.That(result, Is.EqualTo(Convert.FromBase64String(OnePixelPng)));
    }

    [TestCase("{}")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{\"data\":null}")]
    [TestCase("{\"data\":42}")]
    [TestCase("{\"data\":\"\"}")]
    [TestCase("{\"data\":\"invalid base64\"}")]
    [TestCase("{\"data\":\"aGVsbG8=\"}")]
    [TestCase("{\"data\":\"iVBORw0KGgo=\"}")]
    [TestCase("{\"error\":{\"message\":\"Capture failed\"}}")]
    [TestCase("not json")]
    public void DecodePngResponse_RejectsMissingOrInvalidImages(string response)
    {
        // Failed browser responses must never be saved as successful screenshots.
        Assert.Throws<InvalidDataException>(() => BrowserScreenshot.DecodePngResponse(response));
    }

    [Test]
    public void CaptureRequest_UsesPngAndOnlyTheViewport()
    {
        // Offscreen page content must not unexpectedly become part of the screenshot.
        using var parameters = JsonDocument.Parse(BrowserScreenshot.CaptureParameters);
        Assert.Multiple(() =>
        {
            Assert.That(BrowserScreenshot.CaptureMethod, Is.EqualTo("Page.captureScreenshot"));
            Assert.That(parameters.RootElement.GetProperty("format").GetString(), Is.EqualTo("png"));
            Assert.That(parameters.RootElement.GetProperty("fromSurface").GetBoolean(), Is.True);
            Assert.That(parameters.RootElement.GetProperty("captureBeyondViewport").GetBoolean(), Is.False);
        });
    }

    [TestCase("de-DE", "Aktionen", "Screenshot speichern unter …", "Screenshot in Zwischenablage kopieren")]
    [TestCase("en-US", "Actions", "Save screenshot as …", "Copy screenshot to clipboard")]
    [TestCase("fr-FR", "Actions", "Save screenshot as …", "Copy screenshot to clipboard")]
    public void ScreenshotLabels_AreLocalizedWithEnglishFallback(string culture, string actions, string save, string copy)
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Assert.Multiple(() =>
            {
                Assert.That(LocalizationProvider.Get(LocalizationKeys.Menu_Actions), Is.EqualTo(actions));
                Assert.That(LocalizationProvider.Get(LocalizationKeys.Menu_SaveScreenshotAs), Is.EqualTo(save));
                Assert.That(LocalizationProvider.Get(LocalizationKeys.Menu_CopyScreenshotToClipboard), Is.EqualTo(copy));
                Assert.That(LocalizationProvider.Get(LocalizationKeys.Screenshot_Failed_Title), Does.Not.Contain("_"));
                Assert.That(LocalizationProvider.Get(LocalizationKeys.Screenshot_Failed_Text, "test error"), Does.Contain("test error"));
            });
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}
