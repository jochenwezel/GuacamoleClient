using System;
using System.Globalization;

namespace GuacamoleClient.Common.Localization
{
    /// <summary>
    /// Very small localization helper (DE + EN fallback) shared between UI frontends.
    /// </summary>
    public static class LocalizationProvider
    {
        public static string Get(LocalizationKey key) => Get(key, Array.Empty<object>());

        public static string Get(LocalizationKey key, params object[] args)
        {
            var template = GetTemplate(key, CultureInfo.CurrentUICulture);
            if (args == null || args.Length == 0) return template;
            return string.Format(CultureInfo.CurrentUICulture, template, args);
        }

        private static string GetTemplate(LocalizationKey key, CultureInfo culture)
        {
            // German localization
            if (string.Equals(culture.TwoLetterISOLanguageName, "de", StringComparison.OrdinalIgnoreCase))
            {
                switch (key)
                {
                    // MainForm hints / tips
                    case LocalizationKey.Hint_CtrlAltF4_AppWillBeClosed:
                        return "Strg+Alt+F4: Anwendung wird geschlossen…";
                    case LocalizationKey.Hint_AltF4_CatchedAndIgnored:
                        return "Alt+F4 wurde abgefangen (App bleibt offen).";
                    case LocalizationKey.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer:
                        return "Strg+Umschalt+Esc wurde abgefangen (lokaler Task-Manager). Nicht an Remote weiterleitbar. Tipp: In Guacamole-Menü \"Strg+Alt+Entf\" nutzen.";
                    case LocalizationKey.Hint_CtrlAltEnd_WithoutEffect_mstsc:
                        return "Hinweis: Strg+Alt+Ende hat in diesem Setup üblicherweise keine Wirkung (mstsc-Sonderfall).";
                    case LocalizationKey.Hint_WinR_Catched_NotForwardableToRemoteServer:
                        return "WIN+R wurde abgefangen. Nicht an Remote weiterleitbar. Workaround: Strg+Esc drücken und dort \"Ausführen\" suchen.";
                    case LocalizationKey.Hint_CtrlAltBreak_FullscreenModeOff:
                        return "Hinweis: Strg+Alt+Untbr >> Full Screen wurde deaktiviert";
                    case LocalizationKey.Hint_CtrlAltIns_FullscreenModeOff:
                        return "Hinweis: Strg+Alt+Einfügen >> Full Screen wurde deaktiviert";
                    case LocalizationKey.Hint_CtrlAltIns_FullscreenModeOn:
                        return "Hinweis: Strg+Alt+Einfügen >> Full Screen wurde aktiviert";
                    case LocalizationKey.Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow:
                        return "Strg+Alt+Rollen zum Tastaturfokus freigeben";
                    case LocalizationKey.Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow:
                        return "Strg+Alt+Rollen zum Tastaturfokus einfangen";
                    case LocalizationKey.FocussedAnotherControlWarning:
                        return "ACHTUNG: Eingabe-Fokus liegt aktuell bei ";

                    // Menu
                    case LocalizationKey.Menu_OpenAnotherGuacamoleServer:
                        return "Weiteren Guacamole-Server öffnen…";

                    // Choose server dialog
                    case LocalizationKey.ChooseServer_Title:
                        return "Guacamole-Server auswählen";
                    case LocalizationKey.ChooseServer_Column_Name:
                        return "Name";
                    case LocalizationKey.ChooseServer_Column_Url:
                        return "URL";
                    case LocalizationKey.ChooseServer_Column_Color:
                        return "Farbe";
                    case LocalizationKey.ChooseServer_Button_OpenNewWindow:
                        return "Öffnen (neues Fenster)";
                    case LocalizationKey.ChooseServer_Button_Manage:
                        return "Verwalten…";
                    case LocalizationKey.ChooseServer_Button_SetDefault:
                        return "Als Standard";
                    case LocalizationKey.Common_Button_Cancel:
                        return "Abbrechen";

                    // Manage servers dialog
                    case LocalizationKey.ManageServers_Title:
                        return "Guacamole-Server verwalten";
                    case LocalizationKey.ManageServers_Button_Add:
                        return "Hinzufügen…";
                    case LocalizationKey.ManageServers_Button_Edit:
                        return "Bearbeiten…";
                    case LocalizationKey.ManageServers_Button_Remove:
                        return "Entfernen";
                    case LocalizationKey.ManageServers_Button_SetDefault:
                        return "Als Standard";
                    case LocalizationKey.ManageServers_Button_Close:
                        return "Schließen";
                    case LocalizationKey.ManageServers_ConfirmRemove_Title:
                        return "Bestätigen";
                    case LocalizationKey.ManageServers_ConfirmRemove_Text:
                        return "Ausgewähltes Serverprofil entfernen?";

                    // Add/Edit dialog
                    case LocalizationKey.AddServer_Title:
                        return "Guacamole-Server hinzufügen";
                    case LocalizationKey.EditServer_Title:
                        return "Guacamole-Server bearbeiten";
                    case LocalizationKey.AddEdit_Label_ServerUrl:
                        return "Server-URL";
                    case LocalizationKey.AddEdit_Label_DisplayNameOptional:
                        return "Anzeigename (optional)";
                    case LocalizationKey.AddEdit_Label_ColorScheme:
                        return "Farbschema";
                    case LocalizationKey.AddEdit_Label_CustomColorHex:
                        return "Benutzerdefinierte Farbe (Hex)";
                    case LocalizationKey.AddEdit_Check_IgnoreCertificateErrorsUnsafe:
                        return "Zertifikatsfehler ignorieren (unsicher)";
                    case LocalizationKey.AddEdit_Button_Save:
                        return "Speichern";
                    case LocalizationKey.AddEdit_Validation_Title:
                        return "Validierung";
                    case LocalizationKey.AddEdit_Validation_ServerUrlRequired:
                        return "Server-URL ist erforderlich.";
                    case LocalizationKey.AddEdit_Validation_InvalidUrlScheme:
                        return "Die URL ist ungültig. Bitte http:// oder https:// verwenden.";
                    case LocalizationKey.AddEdit_Validation_DuplicateUrl:
                        return "Diese URL ist bereits registriert.";
                    case LocalizationKey.AddEdit_Validation_InvalidColor:
                        return "Ungültiger Farbwert. Bitte eine Palettenfarbe wählen oder einen Hex-Wert wie #A1B2C3 eingeben.";
                    case LocalizationKey.AddEdit_Warn_ColorAlreadyInUse_Title:
                        return "Farbe bereits in Verwendung";
                    case LocalizationKey.AddEdit_Warn_ColorAlreadyInUse_Text:
                        return "Diese Farbe ist bereits zugewiesen an: {0}\n\nTrotzdem verwenden?";
                    case LocalizationKey.AddEdit_TestFailed_Title:
                        return "Server-Test fehlgeschlagen";
                    case LocalizationKey.AddEdit_TestFailed_Text:
                        return "Die URL antwortet nicht mit einer Apache-Guacamole-Startseite (oder ist nicht erreichbar).\n\nBeispiel: {0}";

                    // Suffixes
                    case LocalizationKey.Common_Suffix_Default:
                        return "(Standard)";
                }
            }

            // English fallback
            switch (key)
            {
                // MainForm hints / tips
                case LocalizationKey.Hint_CtrlAltF4_AppWillBeClosed:
                    return "Ctrl+Alt+F4: Application will be closed…";
                case LocalizationKey.Hint_AltF4_CatchedAndIgnored:
                    return "Alt+F4 catched (app stays open).";
                case LocalizationKey.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer:
                    return "Ctrl+SHIFT+Esc catched (local Task-Manager). Not forwardable to remote. Hint: use \"Ctrl+Alt+Del\" in Guacamole menu.";
                case LocalizationKey.Hint_CtrlAltEnd_WithoutEffect_mstsc:
                    return "Hint: Ctrl+Alt+End usually without effect in this environment (special behaviour of mstsc).";
                case LocalizationKey.Hint_WinR_Catched_NotForwardableToRemoteServer:
                    return "WIN+R catched. Not forwardable to remote. Workaround: press Ctrl+Esc and search for \"Run\".";
                case LocalizationKey.Hint_CtrlAltBreak_FullscreenModeOff:
                    return "Hint: Ctrl+Alt+Break >> Full Screen has been disabled";
                case LocalizationKey.Hint_CtrlAltIns_FullscreenModeOff:
                    return "Hint: Ctrl+Alt+Insert >> Full Screen has been disabled";
                case LocalizationKey.Hint_CtrlAltIns_FullscreenModeOn:
                    return "Hint: Ctrl+Alt+Insert >> Full Screen has been enabled";
                case LocalizationKey.Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow:
                    return "Ctrl+Alt+Scroll to release keyboard focus";
                case LocalizationKey.Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow:
                    return "Ctrl+Alt+Scroll to capture keyboard focus";
                case LocalizationKey.FocussedAnotherControlWarning:
                    return "ATTENTION: input focus currently at control ";

                // Menu
                case LocalizationKey.Menu_OpenAnotherGuacamoleServer:
                    return "Open another Guacamole server…";

                // Choose server dialog
                case LocalizationKey.ChooseServer_Title:
                    return "Choose Guacamole server";
                case LocalizationKey.ChooseServer_Column_Name:
                    return "Name";
                case LocalizationKey.ChooseServer_Column_Url:
                    return "URL";
                case LocalizationKey.ChooseServer_Column_Color:
                    return "Color";
                case LocalizationKey.ChooseServer_Button_OpenNewWindow:
                    return "Open (new window)";
                case LocalizationKey.ChooseServer_Button_Manage:
                    return "Manage…";
                case LocalizationKey.ChooseServer_Button_SetDefault:
                    return "Set default";
                case LocalizationKey.Common_Button_Cancel:
                    return "Cancel";

                // Manage servers dialog
                case LocalizationKey.ManageServers_Title:
                    return "Manage Guacamole servers";
                case LocalizationKey.ManageServers_Button_Add:
                    return "Add…";
                case LocalizationKey.ManageServers_Button_Edit:
                    return "Edit…";
                case LocalizationKey.ManageServers_Button_Remove:
                    return "Remove";
                case LocalizationKey.ManageServers_Button_SetDefault:
                    return "Set default";
                case LocalizationKey.ManageServers_Button_Close:
                    return "Close";
                case LocalizationKey.ManageServers_ConfirmRemove_Title:
                    return "Confirm";
                case LocalizationKey.ManageServers_ConfirmRemove_Text:
                    return "Remove selected server profile?";

                // Add/Edit dialog
                case LocalizationKey.AddServer_Title:
                    return "Add Guacamole server";
                case LocalizationKey.EditServer_Title:
                    return "Edit Guacamole server";
                case LocalizationKey.AddEdit_Label_ServerUrl:
                    return "Server URL";
                case LocalizationKey.AddEdit_Label_DisplayNameOptional:
                    return "Display name (optional)";
                case LocalizationKey.AddEdit_Label_ColorScheme:
                    return "Color scheme";
                case LocalizationKey.AddEdit_Label_CustomColorHex:
                    return "Custom color (hex)";
                case LocalizationKey.AddEdit_Check_IgnoreCertificateErrorsUnsafe:
                    return "Ignore certificate errors (unsafe)";
                case LocalizationKey.AddEdit_Button_Save:
                    return "Save";
                case LocalizationKey.AddEdit_Validation_Title:
                    return "Validation";
                case LocalizationKey.AddEdit_Validation_ServerUrlRequired:
                    return "Server URL is required.";
                case LocalizationKey.AddEdit_Validation_InvalidUrlScheme:
                    return "The URL is invalid. Please use http:// or https://.";
                case LocalizationKey.AddEdit_Validation_DuplicateUrl:
                    return "This URL is already registered.";
                case LocalizationKey.AddEdit_Validation_InvalidColor:
                    return "Invalid color value. Choose a palette color or enter a hex value like #A1B2C3.";
                case LocalizationKey.AddEdit_Warn_ColorAlreadyInUse_Title:
                    return "Color already in use";
                case LocalizationKey.AddEdit_Warn_ColorAlreadyInUse_Text:
                    return "This color is already assigned to: {0}\n\nDo you want to use it anyway?";
                case LocalizationKey.AddEdit_TestFailed_Title:
                    return "Server test failed";
                case LocalizationKey.AddEdit_TestFailed_Text:
                    return "The URL does not respond with an Apache Guacamole start page (or is not reachable).\n\nExample: {0}";

                // Suffixes
                case LocalizationKey.Common_Suffix_Default:
                    return "(Default)";
                default:
                    // Intentionally not throwing - keep UI resilient.
                    return key.ToString();
            }
        }
    }
}
