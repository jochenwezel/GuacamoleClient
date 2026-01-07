using GuacamoleClient.Common;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        /// <summary>
        /// Show tooltip message on top of form
        /// </summary>
        /// <param name="localizedString"></param>
        public void ShowHint(LocalizationKeys localizedString)
        {
            string text = LocalizedString(localizedString);
            try { _tip.Show(text, this, 20, 20, 5000); } catch { /*best effort*/ }
        }

        /// <summary>
        /// Specifies the keys used to identify localized strings for user interface hints, tips, and warnings.
        /// </summary>
        /// <remarks>These keys are typically used to retrieve localized messages related to keyboard
        /// shortcuts, application behavior, and user guidance. The enumeration values correspond to specific scenarios
        /// where user feedback or instructions may be displayed in the application.</remarks>
        public enum LocalizationKeys
        {
            Hint_CtrlAltF4_AppWillBeClosed,
            Hint_AltF4_CatchedAndIgnored,
            Hint_WinR_Catched_NotForwardableToRemoteServer,
            Hint_CtrlAltBreak_FullscreenModeOff,
            Hint_CtrlAltIns_FullscreenModeOff,
            Hint_CtrlAltIns_FullscreenModeOn,
            Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer,
            /// <summary>
            /// Gets the hint message indicating that pressing Ctrl+Alt+End has no effect in Remote Desktop sessions
            /// (well-known keyboard shortcut from mstsc).
            /// </summary>
            Hint_CtrlAltEnd_WithoutEffect_mstsc,
            FocussedAnotherControlWarning,
            /// <summary>
            /// Gets a tooltip string that instructs the user to stop keyboard grabbing in the Guacamole window using
            /// the Ctrl+Alt+Scroll shortcut.
            /// </summary>
            Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow,
            /// <summary>
            /// Gets a tooltip string that instructs the user to startkeyboard grabbing in the Guacamole window using
            /// the Ctrl+Alt+Scroll shortcut.
            /// </summary>
            Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow,
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
        private static string LocalizedString(LocalizationKeys key)
        {
            if (System.Diagnostics.Debugger.IsAttached)
                LocalizedFallbackString(key); //just call it once to ensure all fallback values are implemented

            // German localization 
            if (System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de")
            {
                switch (key)
                {
                    case LocalizationKeys.Hint_CtrlAltF4_AppWillBeClosed:
                        return "Strg+Alt+F4: Anwendung wird geschlossen…";
                    case LocalizationKeys.Hint_AltF4_CatchedAndIgnored:
                        return "Alt+F4 wurde abgefangen (App bleibt offen).";
                    case LocalizationKeys.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer:
                        return "Strg+Umschalt+Esc wurde abgefangen (lokaler Task-Manager). Nicht an Remote weiterleitbar. Tipp: In Guacamole-Menü „Strg+Alt+Entf“ nutzen.";
                    case LocalizationKeys.Hint_CtrlAltEnd_WithoutEffect_mstsc:
                        return "Hinweis: Strg+Alt+Ende hat in diesem Setup üblicherweise keine Wirkung (mstsc-Sonderfall).";
                    case LocalizationKeys.Hint_WinR_Catched_NotForwardableToRemoteServer:
                        return "WIN+R wurde abgefangen. Nicht an Remote weiterleitbar. Workaround: Strg+Esc drücken und dort „Ausführen“ suchen.";
                    case LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff:
                        return "Hinweis: Strg+Alt+Untbr >> Full Screen wurde deaktiviert";
                    case LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff:
                        return "Hinweis: Strg+Alt+Einfügen >> Full Screen wurde deaktiviert";
                    case LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn:
                        return "Hinweis: Strg+Alt+Einfügen >> Full Screen wurde aktiviert";
                    case LocalizationKeys.Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow:
                        return "Strg+Alt+Rollen zum Tastaturfokus freigeben";
                    case LocalizationKeys.Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow:
                        return "Strg+Alt+Rollen zum Tastaturfokus einfangen";
                    case LocalizationKeys.FocussedAnotherControlWarning:
                        return "ACHTUNG: Eingabe-Fokus liegt aktuell bei ";
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
                case LocalizationKeys.Hint_CtrlAltF4_AppWillBeClosed:
                    return "Ctrl+Alt+F4: Application will be closed…";
                case LocalizationKeys.Hint_AltF4_CatchedAndIgnored:
                    return "Alt+F4 catched (app stays open).";
                case LocalizationKeys.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer:
                    return "Ctrl+SHIFT+Esc catched (local Task-Manager). Not forwardable to remote. Hint: use \"Ctrl+Alt+Del\" in Guacamole menu.";
                case LocalizationKeys.Hint_CtrlAltEnd_WithoutEffect_mstsc:
                    return "Hinweis: Ctrl+Alt+End usually without effect in this environment (special behaviour of mstsc).";
                case LocalizationKeys.Hint_WinR_Catched_NotForwardableToRemoteServer:
                    return "WIN+R catched. Not forwardable to remote. Workaround: press Ctrl+Esc and search for \"Run\".";
                case LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff:
                    return "Hinweis: Ctrl+Alt+Break >> Full Screen has been disabled";
                case LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff:
                    return "Hinweis: Ctrl+Alt+Insert >> Full Screen has been disabled";
                case LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn:
                    return "Hinweis: Ctrl+Alt+Insert >> Full Screen has been enabled";
                case LocalizationKeys.Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow:
                    return "Ctrl+Alt+Scroll to release keyboard focus";
                case LocalizationKeys.Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow:
                    return "Ctrl+Alt+Scroll to capture keyboard focus";
                case LocalizationKeys.FocussedAnotherControlWarning:
                    return "ATTENTION: input focus currently at control ";
                default:
                    throw new NotImplementedException("Localization key not implemented: " + key.ToString());
            }
        }
    }
}
