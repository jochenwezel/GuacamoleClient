using System;
using System.Globalization;

namespace GuacamoleClient.Common.Localization
{
    /// <summary>
    /// Very small localization helper (DE + EN fallback) shared between UI frontends.
    /// </summary>
    public static class LocalizationProvider
    {
        /// <summary>
        /// Retrieves the localized string associated with the specified localization key.
        /// </summary>
        /// <param name="key">The key that identifies the localized string to retrieve.</param>
        /// <returns>The localized string corresponding to the specified key. If no localized value is found, English text is
        /// returned as a fallback.</returns>
        public static string Get(LocalizationKeys key) => Get(key, Array.Empty<object>());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Get(LocalizationKeys key, params object[] args)
        {
            var template = LocalizedString(key, CultureInfo.CurrentUICulture);
            if (args == null || args.Length == 0) return template;
            return string.Format(CultureInfo.CurrentUICulture, template, args);
        }

        /// <summary>
        /// Retrieves the localized string corresponding to the specified localization key for the current user
        /// interface culture.
        /// </summary>
        /// <remarks>This method supports localization for multiple languages. If the current UI culture
        /// is not supported, a default fallback string is provided. The method is intended for internal use to
        /// centralize localization logic.</remarks>
        /// <param name="key">The key that identifies the string to be localized.</param>
        /// <returns>A localized string for the specified key, based on the current UI culture. If a localized value is not
        /// available for the current culture, a fallback string is returned.</returns>
        private static string LocalizedString(LocalizationKeys key, CultureInfo culture)
        {
            if (System.Diagnostics.Debugger.IsAttached)
                LocalizedFallbackString(key); //just call it once to ensure all fallback values are implemented or that NotImplementedException is thrown

            // German localization
            if (string.Equals(culture.TwoLetterISOLanguageName, "de", StringComparison.OrdinalIgnoreCase))
            {
                switch (key)
                {
                    // MainForm hints / tips
                    case LocalizationKeys.Hint_CtrlAltF4_AppWillBeClosed:
                        return "Strg+Alt+F4: Anwendung wird geschlossen…";
                    case LocalizationKeys.Hint_AltF4_CatchedAndIgnored:
                        return "Alt+F4 wurde abgefangen (App bleibt offen).";
                    case LocalizationKeys.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer:
                        return "Strg+Umschalt+Esc wurde abgefangen (lokaler Task-Manager). Nicht an Remote weiterleitbar. Tipp: In Guacamole-Menü \"Strg+Alt+Entf\" nutzen.";
                    case LocalizationKeys.Hint_RemoteWindowsKey_Sent:
                        return "{0} wurde abgefangen und an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_RemoteWindowsCombination_Sent:
                        return "{0}+{1} wurde abgefangen und an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_RemoteWindowsCombination_NotMapped:
                        return "{0}+{1} wurde abgefangen, ist aber derzeit nicht für DOM-Weiterleitung gemappt.";
                    case LocalizationKeys.Hint_RemoteWindowsPause_NotReliable:
                        return "{0}+Pause wurde abgefangen. Das Remote-Pause/Break-Mapping ist derzeit nicht zuverlässig und kann ohne Wirkung bleiben.";
                    case LocalizationKeys.Hint_RemoteCtrlAltDel_Sent:
                        return "{0}+{1}+Entf wurde abgefangen und an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_RemoteCtrlAltEnd_AsCtrlAltDel_Sent:
                        return "{0}+{1}+Ende wurde abgefangen und als {0}+{1}+Entf an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_RemoteCtrlAltBackspace_Sent:
                        return "{0}+{1}+Rücktaste wurde an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_RemoteCtrlShiftEsc_Sent:
                        return "{0}+{1}+Esc wurde abgefangen und an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_RemoteCtrlAltEnd_Sent:
                        return "{0}+{1}+Ende wurde abgefangen und an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_RemoteAltF4_Sent:
                        return "{0}+F4 wurde abgefangen und an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_RemoteAltTab_Sent:
                        return "{0}+Tab wurde abgefangen und an die Remote-Sitzung gesendet.";
                    case LocalizationKeys.Hint_GuacamoleMenu_Toggled:
                        return "Guacamole-Menü wurde umgeschaltet.";
                    case LocalizationKeys.Hint_ActiveModifiers:
                        return "Aktive Sondertasten: {0}";
                    case LocalizationKeys.Hint_CtrlAltEnd_WithoutEffect_mstsc:
                        return "Hinweis: Strg+Alt+Ende hat in diesem Setup üblicherweise keine Wirkung (mstsc-Sonderfall).";
                    case LocalizationKeys.Hint_WinR_Catched_NotForwardableToRemoteServer:
                        return "WIN+R wurde abgefangen. Nicht an Remote weiterleitbar. Workaround: Strg+Esc drücken und dort \"Ausführen\" suchen.";
                    case LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff:
                        return "Hinweis: Strg+Alt+Untbr >> Full Screen wurde deaktiviert";
                    case LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff:
                        return "Hinweis: Strg+Alt+Einfügen >> Full Screen wurde deaktiviert";
                    case LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn:
                        return "Hinweis: Strg+Alt+Einfügen >> Full Screen wurde aktiviert";
                    case LocalizationKeys.Tip_CtrlAltBackspace_StopKeyboardGrabbingOfGuacamoleWindow:
                        return "Strg+Alt+Rücktaste zum Tastaturfokus freigeben";
                    case LocalizationKeys.Tip_CtrlAltBackspace_StartKeyboardGrabbingOfGuacamoleWindow:
                        return "Strg+Alt+Rücktaste zum Tastaturfokus einfangen";
                    case LocalizationKeys.FocussedAnotherControlWarning:
                        return "ACHTUNG: Eingabe-Fokus liegt aktuell bei ";

                    // Shortcut keystroke descriptions
                    case LocalizationKeys.ShortcutKeystroke_ConnectionHome:
                        return "Strg+Alt+Pos1";
                    case LocalizationKeys.ShortcutKeystroke_NewWindowToolStripMenuItem:
                        return "Strg+Alt+N";
                    case LocalizationKeys.ShortcutKeystroke_QuitToolStripMenuItem:
                        return "Strg+Alt+F4";
                    case LocalizationKeys.ShortcutKeystroke_FullScreenToolStripMenuItem:
                        return "Strg+Alt+Einfg";
                    case LocalizationKeys.ShortcutKeystroke_StopFullScreenModeToolStripMenuItem:
                        return "Strg+Alt+Untbr";
                    case LocalizationKeys.ShortcutKeystroke_HintStopWebcontrol2FocusShortcut:
                        return "Strg+Alt+Rücktaste zum Einfangen/Freigeben der Tastatur";
                    case LocalizationKeys.ShortcutKeystroke_SendCtrlAltDelToolStripMenuItem:
                        return "Strg+Alt+Ende";
                    case LocalizationKeys.ShortcutKeystroke_OpenGuacamoleMenuToolStripMenuItem:
                        return "Strg+Alt+Umschalt";

                    // Menu
                    case LocalizationKeys.Menu_OpenAnotherGuacamoleServer:
                        return "Weiteren Guacamole-Server öffnen…";
                    case LocalizationKeys.Menu_Connection:
                        return "Verbindung";
                    case LocalizationKeys.Menu_View:
                        return "Ansicht";
                    case LocalizationKeys.Menu_SendKeyCombination:
                        return "Tasten-Kombination senden";
                    case LocalizationKeys.Menu_ViewFullScreen:
                        return "Vollbild-Modus";
                    case LocalizationKeys.Menu_Quit:
                        return "Beenden";
                    case LocalizationKeys.Menu_NewWindow:
                        return "Neues Fenster";
                    case LocalizationKeys.Menu_ConnectionHome:
                        return "Verbindungen Startseite";
                    case LocalizationKeys.Menu_GuacamoleUserSettings:
                        return "Guacamole Benutzer-Einstellungen";
                    case LocalizationKeys.Menu_GuacamoleConnectionConfigurations:
                        return "Guacamole Verbindungs-Konfiguration";
                    case LocalizationKeys.Menu_SendCtrlAltDel:
                        return "Sende Strg+Alt+Entf";
                    case LocalizationKeys.Menu_SendCtrlAltEnd:
                        return "Sende Strg+Alt+Ende";
                    case LocalizationKeys.Menu_SendCtrlAltBackspace:
                        return "Sende Strg+Alt+Rücktaste";
                    case LocalizationKeys.Menu_OpenGuacamoleMenu:
                        return "Guacamole-Menü öffnen";

                    // Choose server dialog
                    case LocalizationKeys.ChooseServer_Column_Name:
                        return "Name";
                    case LocalizationKeys.ChooseServer_Column_Url:
                        return "URL";
                    case LocalizationKeys.ChooseServer_Column_Color:
                        return "Farbe";
                    case LocalizationKeys.ChooseServer_Button_Open:
                        return "Verbinden";
                    case LocalizationKeys.Common_Button_Cancel:
                        return "Abbrechen";
                    case LocalizationKeys.ChooseServer_Title:
                        return "Guacamole-Server auswählen";
                    case LocalizationKeys.ChooseServer_Button_Add:
                        return "Hinzufügen…";
                    case LocalizationKeys.ChooseServer_Button_Edit:
                        return "Bearbeiten…";
                    case LocalizationKeys.ChooseServer_Button_Remove:
                        return "Entfernen";
                    case LocalizationKeys.ChooseServer_Button_SetDefault:
                        return "Als Standard";
                    case LocalizationKeys.ChooseServer_Button_Close:
                        return "Schließen";
                    case LocalizationKeys.ChooseServer_ConfirmRemove_Title:
                        return "Bestätigen";
                    case LocalizationKeys.ChooseServer_ConfirmRemove_Text:
                        return "Ausgewähltes Serverprofil entfernen?";

                    // Add/Edit dialog
                    case LocalizationKeys.AddEdit_ModeAddServer_Title:
                        return "Guacamole-Server hinzufügen";
                    case LocalizationKeys.AddEdit_ModeEditServer_Title:
                        return "Guacamole-Server bearbeiten";
                    case LocalizationKeys.AddEdit_Label_ServerUrl:
                        return "Server-URL";
                    case LocalizationKeys.AddEdit_Label_DisplayNameOptional:
                        return "Anzeigename (optional)";
                    case LocalizationKeys.AddEdit_Label_ColorScheme:
                        return "Farbschema";
                    case LocalizationKeys.AddEdit_Label_CustomColorHex:
                        return "Benutzerdefinierte Farbe (Hex)";
                    case LocalizationKeys.AddEdit_Check_IgnoreCertificateErrorsUnsafe:
                        return "Zertifikatsfehler ignorieren (unsicher)";
                    case LocalizationKeys.AddEdit_Button_Save:
                        return "Speichern";
                    case LocalizationKeys.AddEdit_Validation_Title:
                        return "Validierung";
                    case LocalizationKeys.AddEdit_Validation_ServerUrlRequired:
                        return "Server-URL ist erforderlich.";
                    case LocalizationKeys.AddEdit_Validation_InvalidUrlScheme:
                        return "Die URL ist ungültig. Bitte http:// oder https:// verwenden.";
                    case LocalizationKeys.AddEdit_Validation_DuplicateUrl:
                        return "Diese URL ist bereits registriert.";
                    case LocalizationKeys.AddEdit_Validation_InvalidColor:
                        return "Ungültiger Farbwert. Bitte eine Palettenfarbe wählen oder einen Hex-Wert wie #A1B2C3 eingeben.";
                    case LocalizationKeys.AddEdit_Warn_ColorAlreadyInUse_Title:
                        return "Farbe bereits in Verwendung";
                    case LocalizationKeys.AddEdit_Warn_ColorAlreadyInUse_Text:
                        return "Diese Farbe ist bereits zugewiesen an: {0}\n\nTrotzdem verwenden?";
                    case LocalizationKeys.AddEdit_TestFailed_Title:
                        return "Server-Test fehlgeschlagen";
                    case LocalizationKeys.AddEdit_TestFailed_Text:
                        return "Die URL antwortet nicht mit einer Apache-Guacamole-Startseite (oder ist nicht erreichbar).\n\nBeispiel: {0}";
                    case LocalizationKeys.AddEdit_Link_SetupGuideGuacamoleTestServer:
                        return "Setup Leitfaden für Guacamole Test Server mit Docker";

                    // App start / startup error handling
                    case LocalizationKeys.AppStart_StartupError_Title:
                        return "Startfehler";
                    case LocalizationKeys.AppStart_UnexpectedError_Title:
                        return "Unerwarteter Fehler";
                    case LocalizationKeys.AppStart_BackgroundError_Title:
                        return "Hintergrundfehler";
                    case LocalizationKeys.AppStart_StartUrlRequired_Title:
                        return "Start-URL erforderlich";
                    case LocalizationKeys.AppStart_WebViewError_Title:
                        return "WebView-Fehler";
                    case LocalizationKeys.AppStart_ErrorMessage_Text:
                        return "Die Anwendung konnte nicht korrekt gestartet werden.";
                    case LocalizationKeys.AppStart_ErrorDetails_Label:
                        return "Details:";
                    case LocalizationKeys.AppStart_StartUrlRequired_Text:
                        return "Ohne gültige Start-URL kann die Anwendung nicht fortfahren.";
                    case LocalizationKeys.AppStart_UnexpectedErrorWillClose_Text:
                        return "Ein unerwarteter Fehler ist aufgetreten und die Anwendung muss geschlossen werden.";

                    // Suffixes
                    case LocalizationKeys.Common_Suffix_Default:
                        return "(Standard)";
                }
            }

            return LocalizedFallbackString(key);
        }

        /// <summary>
        /// Localized English fallback strings for known application hints and tips.
        /// </summary>
        /// <param name="key">The localization key that identifies the message to retrieve.</param>
        /// <returns>A localized string associated with the specified key.</returns>
        /// <exception cref="NotImplementedException">Thrown if the specified key does not have a corresponding localized string defined.</exception>
        private static string LocalizedFallbackString(LocalizationKeys key)
        { 
            switch (key)
            {
                // MainForm hints / tips
                case LocalizationKeys.Hint_CtrlAltF4_AppWillBeClosed:
                    return "Ctrl+Alt+F4: Application will be closed…";
                case LocalizationKeys.Hint_AltF4_CatchedAndIgnored:
                    return "Alt+F4 catched (app stays open).";
                case LocalizationKeys.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer:
                    return "Ctrl+SHIFT+Esc catched (local Task-Manager). Not forwardable to remote. Hint: use \"Ctrl+Alt+Del\" in Guacamole menu.";
                case LocalizationKeys.Hint_RemoteWindowsKey_Sent:
                    return "{0} was caught and sent to the remote session.";
                case LocalizationKeys.Hint_RemoteWindowsCombination_Sent:
                    return "{0}+{1} was caught and sent to the remote session.";
                case LocalizationKeys.Hint_RemoteWindowsCombination_NotMapped:
                    return "{0}+{1} was caught, but this key is currently not mapped for DOM forwarding.";
                case LocalizationKeys.Hint_RemoteWindowsPause_NotReliable:
                    return "{0}+Pause was caught. The remote Pause/Break mapping is currently not reliable and may have no effect.";
                case LocalizationKeys.Hint_RemoteCtrlAltDel_Sent:
                    return "{0}+{1}+Del was caught and sent to the remote session.";
                case LocalizationKeys.Hint_RemoteCtrlAltEnd_AsCtrlAltDel_Sent:
                    return "{0}+{1}+End was caught and sent to the remote session as {0}+{1}+Del.";
                case LocalizationKeys.Hint_RemoteCtrlAltBackspace_Sent:
                    return "{0}+{1}+Backspace was sent to the remote session.";
                case LocalizationKeys.Hint_RemoteCtrlShiftEsc_Sent:
                    return "{0}+{1}+Esc was caught and sent to the remote session.";
                case LocalizationKeys.Hint_RemoteCtrlAltEnd_Sent:
                    return "{0}+{1}+End was caught and sent to the remote session.";
                case LocalizationKeys.Hint_RemoteAltF4_Sent:
                    return "{0}+F4 was caught and sent to the remote session.";
                case LocalizationKeys.Hint_RemoteAltTab_Sent:
                    return "{0}+Tab was caught and sent to the remote session.";
                case LocalizationKeys.Hint_GuacamoleMenu_Toggled:
                    return "Guacamole menu was toggled.";
                case LocalizationKeys.Hint_ActiveModifiers:
                    return "Active special keys: {0}";
                case LocalizationKeys.Hint_CtrlAltEnd_WithoutEffect_mstsc:
                    return "Hint: Ctrl+Alt+End usually without effect in this environment (special behaviour of mstsc).";
                case LocalizationKeys.Hint_WinR_Catched_NotForwardableToRemoteServer:
                    return "WIN+R catched. Not forwardable to remote. Workaround: press Ctrl+Esc and search for \"Run\".";
                case LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff:
                    return "Hint: Ctrl+Alt+Break >> Full Screen has been disabled";
                case LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff:
                    return "Hint: Ctrl+Alt+Insert >> Full Screen has been disabled";
                case LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn:
                    return "Hint: Ctrl+Alt+Insert >> Full Screen has been enabled";
                case LocalizationKeys.Tip_CtrlAltBackspace_StopKeyboardGrabbingOfGuacamoleWindow:
                    return "Ctrl+Alt+Backspace to release keyboard focus";
                case LocalizationKeys.Tip_CtrlAltBackspace_StartKeyboardGrabbingOfGuacamoleWindow:
                    return "Ctrl+Alt+Backspace to capture keyboard focus";
                case LocalizationKeys.FocussedAnotherControlWarning:
                    return "ATTENTION: input focus currently at control ";

                // Shortcut keystroke descriptions
                case LocalizationKeys.ShortcutKeystroke_ConnectionHome:
                    return "Ctrl+Alt+Pos1";
                case LocalizationKeys.ShortcutKeystroke_NewWindowToolStripMenuItem:
                    return "Ctrl+Alt+N";
                case LocalizationKeys.ShortcutKeystroke_QuitToolStripMenuItem:
                    return "Ctrl+Alt+F4";
                case LocalizationKeys.ShortcutKeystroke_FullScreenToolStripMenuItem:
                    return "Ctrl+Alt+Insert";
                case LocalizationKeys.ShortcutKeystroke_StopFullScreenModeToolStripMenuItem:
                    return "Ctrl+Alt+Break";
                case LocalizationKeys.ShortcutKeystroke_HintStopWebcontrol2FocusShortcut:
                    return "Ctrl+Alt+Backspace to capture/release keyboard";
                case LocalizationKeys.ShortcutKeystroke_SendCtrlAltDelToolStripMenuItem:
                    return "Ctrl+Alt+End";
                case LocalizationKeys.ShortcutKeystroke_OpenGuacamoleMenuToolStripMenuItem:
                    return "Ctrl+Alt+Shift";

                // Menu
                case LocalizationKeys.Menu_OpenAnotherGuacamoleServer:
                    return "Open another Guacamole server…";
                case LocalizationKeys.Menu_Connection:
                    return "Connection";
                case LocalizationKeys.Menu_View:
                    return "View";
                case LocalizationKeys.Menu_SendKeyCombination:
                    return "Send key combination";
                case LocalizationKeys.Menu_ViewFullScreen:
                    return "Full-Screen";
                case LocalizationKeys.Menu_Quit:
                    return "Quit";
                case LocalizationKeys.Menu_NewWindow:
                    return "New window";
                case LocalizationKeys.Menu_ConnectionHome:
                    return "Connection Home";
                case LocalizationKeys.Menu_GuacamoleUserSettings:
                    return "Guacamole User Settings";
                case LocalizationKeys.Menu_GuacamoleConnectionConfigurations:
                    return "Guacamole Connections Configuration";
                case LocalizationKeys.Menu_SendCtrlAltDel:
                    return "Send Ctrl+Alt+Del";
                case LocalizationKeys.Menu_SendCtrlAltEnd:
                    return "Send Ctrl+Alt+End";
                case LocalizationKeys.Menu_SendCtrlAltBackspace:
                    return "Send Ctrl+Alt+Backspace";
                case LocalizationKeys.Menu_OpenGuacamoleMenu:
                    return "Open Guacamole menu";

                // Choose server dialog
                case LocalizationKeys.ChooseServer_Column_Name:
                    return "Name";
                case LocalizationKeys.ChooseServer_Column_Url:
                    return "URL";
                case LocalizationKeys.ChooseServer_Column_Color:
                    return "Color";
                case LocalizationKeys.ChooseServer_Button_Open:
                    return "Connect";
                case LocalizationKeys.Common_Button_Cancel:
                    return "Cancel";
                case LocalizationKeys.ChooseServer_Title:
                    return "Choose Guacamole servers";
                case LocalizationKeys.ChooseServer_Button_Add:
                    return "Add…";
                case LocalizationKeys.ChooseServer_Button_Edit:
                    return "Edit…";
                case LocalizationKeys.ChooseServer_Button_Remove:
                    return "Remove";
                case LocalizationKeys.ChooseServer_Button_SetDefault:
                    return "Set default";
                case LocalizationKeys.ChooseServer_Button_Close:
                    return "Close";
                case LocalizationKeys.ChooseServer_ConfirmRemove_Title:
                    return "Confirm";
                case LocalizationKeys.ChooseServer_ConfirmRemove_Text:
                    return "Remove selected server profile?";

                // Add/Edit dialog
                case LocalizationKeys.AddEdit_ModeAddServer_Title:
                    return "Add Guacamole server";
                case LocalizationKeys.AddEdit_ModeEditServer_Title:
                    return "Edit Guacamole server";
                case LocalizationKeys.AddEdit_Label_ServerUrl:
                    return "Server URL";
                case LocalizationKeys.AddEdit_Label_DisplayNameOptional:
                    return "Display name (optional)";
                case LocalizationKeys.AddEdit_Label_ColorScheme:
                    return "Color scheme";
                case LocalizationKeys.AddEdit_Label_CustomColorHex:
                    return "Custom color (hex)";
                case LocalizationKeys.AddEdit_Check_IgnoreCertificateErrorsUnsafe:
                    return "Ignore certificate errors (unsafe)";
                case LocalizationKeys.AddEdit_Button_Save:
                    return "Save";
                case LocalizationKeys.AddEdit_Validation_Title:
                    return "Validation";
                case LocalizationKeys.AddEdit_Validation_ServerUrlRequired:
                    return "Server URL is required.";
                case LocalizationKeys.AddEdit_Validation_InvalidUrlScheme:
                    return "The URL is invalid. Please use http:// or https://.";
                case LocalizationKeys.AddEdit_Validation_DuplicateUrl:
                    return "This URL is already registered.";
                case LocalizationKeys.AddEdit_Validation_InvalidColor:
                    return "Invalid color value. Choose a palette color or enter a hex value like #A1B2C3.";
                case LocalizationKeys.AddEdit_Warn_ColorAlreadyInUse_Title:
                    return "Color already in use";
                case LocalizationKeys.AddEdit_Warn_ColorAlreadyInUse_Text:
                    return "This color is already assigned to: {0}\n\nDo you want to use it anyway?";
                case LocalizationKeys.AddEdit_TestFailed_Title:
                    return "Server test failed";
                case LocalizationKeys.AddEdit_TestFailed_Text:
                    return "The URL does not respond with an Apache Guacamole start page (or is not reachable).\n\nExample: {0}";
                case LocalizationKeys.AddEdit_Link_SetupGuideGuacamoleTestServer:
                    return "Setup guide for Guacamole Test Server with Docker";

                // App start / startup error handling
                case LocalizationKeys.AppStart_StartupError_Title:
                    return "Startup Error";
                case LocalizationKeys.AppStart_UnexpectedError_Title:
                    return "Unexpected Error";
                case LocalizationKeys.AppStart_BackgroundError_Title:
                    return "Background Error";
                case LocalizationKeys.AppStart_StartUrlRequired_Title:
                    return "Start URL Required";
                case LocalizationKeys.AppStart_WebViewError_Title:
                    return "WebView Error";
                case LocalizationKeys.AppStart_ErrorMessage_Text:
                    return "The application could not be started correctly.";
                case LocalizationKeys.AppStart_ErrorDetails_Label:
                    return "Details:";
                case LocalizationKeys.AppStart_StartUrlRequired_Text:
                    return "The application cannot continue without a valid start URL.";
                case LocalizationKeys.AppStart_UnexpectedErrorWillClose_Text:
                    return "An unexpected error occurred and the application must close.";

                // Suffixes
                case LocalizationKeys.Common_Suffix_Default:
                    return "(Default)";
                default:
                    // Intentionally not throwing - keep UI resilient.
                    if (System.Diagnostics.Debugger.IsAttached)
                        throw new NotImplementedException("Localization key not implemented: " + key.ToString());
                    else
                        return "{" + key.ToString() + "}";
            }
        }
    }
}
