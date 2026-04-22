using System.Diagnostics;
using System.Net;
using System.Text;
using GuacamoleClient.Common.Settings;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class GuacamoleColorSchemePreviewTests
{
    [Test]
    public void CreatePalettePreviewHtml_WritesTemporaryFilePath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "GuacamoleClient.Tests");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "guacamole-color-scheme-preview.html");
        File.WriteAllText(outputPath, BuildPreviewHtml(), Encoding.UTF8);

        Console.WriteLine(outputPath);

        Assert.That(File.Exists(outputPath), Is.True);
        Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));

        if (Debugger.IsAttached)
        {
            Process.Start(new ProcessStartInfo(outputPath)
            {
                UseShellExecute = true
            });
        }
    }

    private static string BuildPreviewHtml()
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"de\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("  <title>Guacamole Profil-Farben Vorschau</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    :root { color-scheme: light dark; font-family: Segoe UI, Arial, sans-serif; }");
        html.AppendLine("    body { margin: 24px; background: #f4f6f8; color: #1f2937; }");
        html.AppendLine("    h1 { margin: 0 0 8px; font-size: 28px; }");
        html.AppendLine("    .hint { margin: 0 0 24px; color: #4b5563; }");
        html.AppendLine("    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 16px; }");
        html.AppendLine("    .card { border: 1px solid #d1d5db; border-radius: 8px; overflow: hidden; background: white; box-shadow: 0 1px 2px rgba(0,0,0,.08); }");
        html.AppendLine("    .header { padding: 18px; }");
        html.AppendLine("    .name { font-size: 20px; font-weight: 700; margin-bottom: 6px; }");
        html.AppendLine("    .meta { font-family: Consolas, monospace; font-size: 13px; opacity: .85; }");
        html.AppendLine("    .samples { padding: 14px; display: grid; gap: 10px; }");
        html.AppendLine("    .sample { border-radius: 6px; padding: 12px; border: 1px solid rgba(0,0,0,.18); }");
        html.AppendLine("    .sample-title { font-weight: 700; margin-bottom: 4px; }");
        html.AppendLine("    .sample-value { font-family: Consolas, monospace; font-size: 13px; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <h1>Guacamole Profil-Farben Vorschau</h1>");
        html.AppendLine("  <p class=\"hint\">Schriftfarbe und Hintergrundfarbe je hinterlegter Profilfarbe, inklusive Hover-, Auswahl- und Inaktiv-Farben.</p>");
        html.AppendLine("  <main class=\"grid\">");

        foreach (var paletteEntry in GuacamoleColorPalette.Colors.OrderBy(kvp => kvp.Key))
        {
            var scheme = new GuacamoleColorScheme(paletteEntry.Key);
            AppendSchemeCard(html, paletteEntry.Key, scheme);
        }

        html.AppendLine("  </main>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static void AppendSchemeCard(StringBuilder html, string paletteName, GuacamoleColorScheme scheme)
    {
        html.AppendLine("    <section class=\"card\">");
        html.Append("      <div class=\"header\" style=\"background:");
        html.Append(Html(scheme.PrimaryColorHexValue));
        html.Append(";color:");
        html.Append(Html(scheme.TextColorHexValue));
        html.AppendLine("\">");
        html.Append("        <div class=\"name\">");
        html.Append(Html(paletteName));
        html.AppendLine("</div>");
        html.Append("        <div class=\"meta\">Primary ");
        html.Append(Html(scheme.PrimaryColorHexValue));
        html.Append(" / Text ");
        html.Append(Html(scheme.TextColorHexValue));
        html.AppendLine("</div>");
        html.AppendLine("      </div>");
        html.AppendLine("      <div class=\"samples\">");
        AppendSample(html, "Inaktiv", scheme.PrimaryColorHexValue, scheme.InactiveTextColorHexValue);
        AppendSample(html, "Hover", scheme.HoverBackgroundColorHexValue, scheme.HoverTextColorHexValue);
        AppendSample(html, "Auswahl", scheme.SelectedItemBackgroundColorHexValue, scheme.SelectedItemTextColorHexValue);
        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
    }

    private static void AppendSample(StringBuilder html, string title, string backgroundHex, string textHex)
    {
        html.Append("        <div class=\"sample\" style=\"background:");
        html.Append(Html(backgroundHex));
        html.Append(";color:");
        html.Append(Html(textHex));
        html.AppendLine("\">");
        html.Append("          <div class=\"sample-title\">");
        html.Append(Html(title));
        html.AppendLine("</div>");
        html.Append("          <div class=\"sample-value\">Background ");
        html.Append(Html(backgroundHex));
        html.Append(" / Text ");
        html.Append(Html(textHex));
        html.AppendLine("</div>");
        html.AppendLine("        </div>");
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
