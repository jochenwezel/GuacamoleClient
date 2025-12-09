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

            if ((string.IsNullOrWhiteSpace(url)) || !IsValidUrl(url) || !IsGuacamoleResponseWithStartPage(url))
            {
                while (true)
                {
                    string input = Microsoft.VisualBasic.Interaction.InputBox(
                        LocalizedString(LocalizationKeys.InputBoxPromptForStartUrl) + Environment.NewLine +
                        LocalizedString(LocalizationKeys.StartUrlExample),
                        LocalizedString(LocalizationKeys.InputBoxTitleStartUrl),
                        "https://",
                        -1, -1
                    );

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        var result = MessageBox.Show(
                            LocalizedString(LocalizationKeys.ErrorMessageStartUrlRequired),
                            LocalizedString(LocalizationKeys.ErrorTitleStartUrlRequired),
                            MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);

                        if (result == DialogResult.Cancel)
                            return null;

                        continue; // Retry
                    }

                    if (IsValidUrl(input) && IsGuacamoleResponseWithStartPage(url))
                    {
                        url = input.Trim();
                        SaveStartUrlToRegistry(url);
                        break;
                    }
                    else
                    {
                        MessageBox.Show(
                            LocalizedString(LocalizationKeys.ErrorMessageInvalidUrlOrNoGuacamoleServerResponse),
                            LocalizedString(LocalizationKeys.ErrorTitleInvalidUrl), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    LocalizedString(LocalizationKeys.RegistrySaveError) +
                    Environment.NewLine + ex.Message,
                    LocalizedString(LocalizationKeys.ErrorTitleStorageError), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsValidUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
                return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
            return false;
        }

        /// <summary>
        /// Determines whether the specified HTTP response content represents a Guacamole start page.
        /// </summary>
        /// <param name="url">The url to the guacamole server</param>
        /// <returns>true if the content contains a Guacamole start page; otherwise, false.</returns>
        private static bool IsGuacamoleResponseWithStartPage(string? url)
        {
            if (url != null && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                System.Net.Http.HttpClient request = new System.Net.Http.HttpClient();
                try
                {
                    var response = request.GetAsync(uri).Result;
                    if (!response.IsSuccessStatusCode)
                        return false;
                    var content = response.Content.ReadAsStringAsync().Result;
                    return ContentIsGuacamoleStartPage(content);
                }
                catch
                {
                    return false;
                }
            }
            else
                return false;
        }

        /// <summary>
        /// Determines whether the specified HTTP response content represents a Guacamole login form.
        /// </summary>
        /// <param name="url">The url to the guacamole server</param>
        /// <returns>true if the content contains a Guacamole login form; otherwise, false.</returns>
        private static bool IsGuacamoleResponseWithLoginForm(string url)
        {
            if (url != null && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                System.Net.Http.HttpClient request = new System.Net.Http.HttpClient();
                try
                {
                    var response = request.GetAsync(uri).Result;
                    if (!response.IsSuccessStatusCode)
                        return false;
                    var content = response.Content.ReadAsStringAsync().Result;
                    return ContentIsGuacamoleLoginForm(content);
                }
                catch
                {
                    return false;
                }
            }
            else
                return false;
        }

        private static bool ContentIsGuacamoleLoginForm(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            return content.Contains("class=\"login-fields\"") && content.Contains("id=\"guac-field-") && content.Contains("name=\"username\"") && content.Contains("name=\"password\"");
        }
        private static bool ContentIsGuacamoleStartPage(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            return content.Contains("<guac-modal>");
        }

        private enum LocalizationKeys
        {
            InputBoxPromptForStartUrl,
            InputBoxTitleStartUrl,
            StartUrlExample,
            ErrorMessageStartUrlRequired,
            ErrorTitleStartUrlRequired,
            ErrorMessageInvalidUrlOrNoGuacamoleServerResponse,
            ErrorTitleInvalidUrl,
            RegistrySaveError,
            ErrorTitleStorageError,
        }

        private static string LocalizedString(LocalizationKeys key)
        {
            // German localization 
            if (System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de")
            {
                switch (key)
                {
                    case LocalizationKeys.InputBoxPromptForStartUrl:
                        return "Bitte geben Sie die Start-URL für Apache Guacamole ein:";
                    case LocalizationKeys.InputBoxTitleStartUrl:
                        return "Start-URL festlegen";
                    case LocalizationKeys.StartUrlExample:
                        return "(Beispiel: https://remote.example.com/guacamole/)";
                    case LocalizationKeys.ErrorMessageStartUrlRequired:
                        return "Ohne Start-URL kann die Anwendung nicht fortfahren. Möchten Sie erneut versuchen?";
                    case LocalizationKeys.ErrorTitleStartUrlRequired:
                        return "Start-URL erforderlich";
                    case LocalizationKeys.ErrorMessageInvalidUrlOrNoGuacamoleServerResponse:
                        return "Die URL ist ungültig oder es antwortet kein Guacamole Server. Bitte prüfen Sie das Format (https://...).";
                    case LocalizationKeys.RegistrySaveError:
                        return "Die Start-URL konnte nicht in der Registry gespeichert werden:";
                    case LocalizationKeys.ErrorTitleStorageError:
                        return "Speicherfehler";
                    case LocalizationKeys.ErrorTitleInvalidUrl:
                        return "Ungültige URL";
                }
            }

            // Fallback: English localization 
            switch (key)
            {
                case LocalizationKeys.InputBoxPromptForStartUrl:
                    return "Please input the start URL for your Apache Guacamole server:";
                case LocalizationKeys.InputBoxTitleStartUrl:
                    return "Configure start URL";
                case LocalizationKeys.StartUrlExample:
                    return "(example: https://remote.example.com/guacamole/)";
                case LocalizationKeys.ErrorMessageStartUrlRequired:
                    return "The app can't continue without start URL. Do you want to retry?";
                case LocalizationKeys.ErrorTitleStartUrlRequired:
                    return "Start URL required";
                case LocalizationKeys.ErrorMessageInvalidUrlOrNoGuacamoleServerResponse:
                    return "The URL is invalid or the response is no Guacamole server. Please check the format (https://...).";
                case LocalizationKeys.RegistrySaveError:
                    return "The start URL can't be saved in Registry:";
                case LocalizationKeys.ErrorTitleStorageError:
                    return "Error on saving";
                case LocalizationKeys.ErrorTitleInvalidUrl:
                    return "Invalid URL";
                default:
                    throw new NotImplementedException("Localization key not implemented: " + key.ToString());
            }
        }
    }
}
