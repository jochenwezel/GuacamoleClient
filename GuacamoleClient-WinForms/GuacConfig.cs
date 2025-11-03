using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GuacamoleClient.WinForms
{
    public static class GuacConfig
    {
        private const string RegPath = @"Software\CompuMaster\GuacamoleLauncher";
        private const string RegValueName = "StartUrl";

        /// <summary>
        /// Liest die Start-URL aus HKCU; fragt per InputBox, wenn nicht vorhanden/ungültig.
        /// Gibt null zurück, wenn der Benutzer abbricht.
        /// </summary>
        public static string? GetOrAskStartUrl()
        {
            string? url = ReadStartUrlFromRegistry();

            if ((string.IsNullOrWhiteSpace(url)) || !IsValidUrl(url))
            {
                while (true)
                {
                    string input = Microsoft.VisualBasic.Interaction.InputBox(
                        "Bitte geben Sie die Start-URL für Apache Guacamole ein:" + Environment.NewLine +
                        "(Beispiel: https://remote.example.com/guacamole/)",
                        "Start-URL festlegen",
                        "https://",
                        -1, -1
                    );

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        var result = MessageBox.Show(
                            "Ohne Start-URL kann die Anwendung nicht fortfahren. Möchten Sie erneut versuchen?",
                            "Start-URL erforderlich",
                            MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);

                        if (result == DialogResult.Cancel)
                            return null;

                        continue; // Retry
                    }

                    if (IsValidUrl(input))
                    {
                        url = input.Trim();
                        SaveStartUrlToRegistry(url);
                        break;
                    }
                    else
                    {
                        MessageBox.Show(
                            "Die URL ist ungültig. Bitte prüfen Sie das Format (https://...).",
                            "Ungültige URL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            return url;
        }

        private static string? ReadStartUrlFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegPath, false))
                {
                    if (key == null) return null;
                    var val = key.GetValue(RegValueName) as string;
                    return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
                }
            }
            catch
            {
                return null; // neutral: bei Fehler später erneut fragen
            }
        }

        private static void SaveStartUrlToRegistry(string url)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegPath, true))
                {
                    key.SetValue(RegValueName, url, RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Die Start-URL konnte nicht in der Registry gespeichert werden:" +
                    Environment.NewLine + ex.Message,
                    "Speicherfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsValidUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
                return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
            return false;
        }
    }
}
