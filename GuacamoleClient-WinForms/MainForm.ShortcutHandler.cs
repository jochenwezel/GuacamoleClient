using GuacamoleClient.Common.Localization;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        // ========== Tastatur-Logik ==========
        // Ziel:
        // 1. Alles durchlassen außer explizit behandelte (OS-reservierte) Kombinationen.
        // 2. Alle typischen Shortcuts, die nicht weitergeleitet werden können, abfangen und mit entsprechendem Hinweis dem User melden

        /// <summary>
        /// Keyboard/shortcut capturing when webview2 control is focused/responsible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Controller_AcceleratorKeyPressed(object? sender, CoreWebView2AcceleratorKeyPressedEventArgs e)
        {
            if (this.IsMenuOpen) return;
            // Nur KeyDown / SystemKeyDown interessieren
            if (e.KeyEventKind != CoreWebView2KeyEventKind.KeyDown &&
                e.KeyEventKind != CoreWebView2KeyEventKind.SystemKeyDown)
                return;

            // VK-Konstanten
            const uint VK_F4 = (uint)Keys.F4;
            const uint VK_END = (uint)Keys.End;
            const uint VK_ESC = (uint)Keys.Escape;
            const uint VK_R = (uint)Keys.R;
            const uint VK_BACKSPACE = (uint)Keys.Back;
            //const int VK_F4 = 0x73;
            //const int VK_END = 0x23;
            //const int VK_ESC = 0x1B;
            //const int VK_R = 0x52; 

            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;     // AltGr erscheint hier als Ctrl+Alt
            bool altGr = IsAltGrPressed();
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool lwin = (Control.ModifierKeys & Keys.LWin) == Keys.LWin;
            bool rwin = (Control.ModifierKeys & Keys.RWin) == Keys.RWin;
            bool win = (lwin || rwin);
            bool hostCtrlAlt = ctrl && alt && !altGr;

            // --- App-Policy: Ctrl+Alt+F4 schließt die App ---
            if (e.VirtualKey == VK_F4 && hostCtrlAlt)
            {
                e.Handled = true;
                ShowHint(LocalizationKeys.Hint_CtrlAltF4_AppWillBeClosed);
                _closeTimer.Start();
                return;
            }

            // --- Alt+F4 blocken (App bleibt offen) ---
            if (e.VirtualKey == VK_F4 && alt && (!ctrl || altGr))
            {
                e.Handled = true;
                if (ShouldRouteSpecialKeysToRemote())
                    TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.AltF4, altGr ? "Caught RAlt+F4 and sent it to the remote session." : "Caught Alt+F4 and sent it to the remote session.", null, altGr ? Keys.RMenu : null);
                else
                    ShowHint(LocalizationKeys.Hint_AltF4_CatchedAndIgnored);
                return;
            }

            // --- Ctrl+F4 durchlassen ---
            if (e.VirtualKey == VK_F4 && ctrl && !alt)
            {
                e.Handled = false; // weiter an WebView/Seite
                return;
            }

            // --- App-Policy: Ctrl+Alt+Backspace gibt Tastaturfokus frei ---
            if (e.VirtualKey == VK_BACKSPACE && hostCtrlAlt)
            {
                e.Handled = true;
                DetachWebViewFocus(false);
                return;
            }

            // --- Ctrl+Shift+Esc (lokaler Task-Manager) -> NICHT weiterleitbar ---
            if (e.VirtualKey == VK_ESC && ctrl && shift)
            {
                e.Handled = true; // lokal abfangen
                if (ShouldRouteSpecialKeysToRemote())
                    TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlShiftEsc, "Caught Ctrl+Shift+Esc and sent it to the remote session.");
                else
                    ShowHint(LocalizationKeys.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer);
                return;
            }

            // --- Ctrl+Alt+End: in WebView2/Guacamole typischerweise ohne Wirkung ---
            if (e.VirtualKey == VK_END && hostCtrlAlt)
            {
                if (ShouldRouteSpecialKeysToRemote())
                {
                    e.Handled = true;
                    TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlAltEnd, "Caught Ctrl+Alt+End and sent it to the remote session.");
                }
                else
                {
                    e.Handled = false;
                    ShowHint(LocalizationKeys.Hint_CtrlAltEnd_WithoutEffect_mstsc);
                }
                return;
            }

            // --- Win+R (OS-reserviert) -> abfangen ---
            // Windows-Taste selbst kommt hier i. d. R. nicht an; falls doch, verhindern wir lokal.
            if ((IsWinPressed() && e.VirtualKey == VK_R))
            {
                e.Handled = true;
                if (ShouldRouteSpecialKeysToRemote())
                    TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.WinR, "Caught Win+R and sent it to the remote session.");
                else
                    ShowHint(LocalizationKeys.Hint_WinR_Catched_NotForwardableToRemoteServer);
                return;
            }

            if (fullScreenToolStripMenuItem.Checked && e.VirtualKey == (uint)Keys.Cancel && hostCtrlAlt)
            {
                e.Handled = true;
                SwitchFullScreenMode(false);
                ShowHint(LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff);
                return;
            }

            // Restore window state from fullscreen on Strg+Break
            if (e.VirtualKey == (uint)Keys.Insert && hostCtrlAlt)
            {
                e.Handled = true;
                SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
                if (!fullScreenToolStripMenuItem.Checked)
                    ShowHint(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff);
                else
                    ShowHint(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn);
                return;
            }

            // Go to guacamole home screen
            if (e.VirtualKey == (uint)Keys.Home && hostCtrlAlt)
            {
                e.Handled = true;
                connectionHomeToolStripMenuItem.PerformClick();
                return;
            }

            // New Window
            if (e.VirtualKey == (uint)Keys.N && hostCtrlAlt)
            {
                e.Handled = true;
                newWindowToolStripMenuItem.PerformClick();
                return;
            }


            // Standard: Alles andere mit Ctrl/Alt/Shift/AltGr durchlassen
            e.Handled = false;
        }

        /// <summary>
        /// Keyboard/shortcut capturing when form is focused/responsible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Nur KeyDown / SystemKeyDown interessieren
            if (e.Handled)
                return;
            if (this.IsMenuOpen) return;

            // VK-Konstanten
            const Keys VK_F4 = Keys.F4;
            const Keys VK_END = Keys.End;
            const Keys VK_ESC = Keys.Escape;
            const Keys VK_R = Keys.R;
            const Keys VK_N = Keys.N;
            const Keys VK_BACKSPACE = Keys.Back;
            //const int VK_F4 = 0x73;
            //const int VK_END = 0x23;
            //const int VK_ESC = 0x1B;
            //const int VK_R = 0x52; 

            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;     // AltGr erscheint hier als Ctrl+Alt
            bool altGr = IsAltGrPressed();
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool lwin = (Control.ModifierKeys & Keys.LWin) == Keys.LWin;
            bool rwin = (Control.ModifierKeys & Keys.RWin) == Keys.RWin;
            bool win = (lwin || rwin);
            bool hostCtrlAlt = ctrl && alt && !altGr;

            // --- App-Policy: Ctrl+Alt+F4 schließt die App ---
            if (e.KeyCode == VK_F4 && hostCtrlAlt)
            {
                e.Handled = true;
                ShowHint(LocalizationKeys.Hint_CtrlAltF4_AppWillBeClosed);
                _closeTimer.Start();
                return;
            }

            // --- Alt+F4 blocken (App bleibt offen) ---
            if (e.KeyCode == VK_F4 && alt && (!ctrl || altGr))
            {
                e.Handled = true;
                if (ShouldRouteSpecialKeysToRemote())
                    TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.AltF4, altGr ? "Caught RAlt+F4 and sent it to the remote session." : "Caught Alt+F4 and sent it to the remote session.", null, altGr ? Keys.RMenu : null);
                else
                    ShowHint(LocalizationKeys.Hint_AltF4_CatchedAndIgnored);
                return;
            }

            // --- Ctrl+F4 durchlassen ---
            if (e.KeyCode == VK_F4 && ctrl && !alt)
            {
                e.Handled = false; // weiter an WebView/Seite
                return;
            }

            // --- App-Policy: Ctrl+Alt+Backspace gibt Tastaturfokus frei ---
            if (e.KeyCode == VK_BACKSPACE && hostCtrlAlt)
            {
                e.Handled = true;
                SetFocusToWebview2Control();
                return;
            }

            // --- Ctrl+Shift+Esc (lokaler Task-Manager) -> NICHT weiterleitbar ---
            if (e.KeyCode == VK_ESC && ctrl && shift)
            {
                e.Handled = true; // lokal abfangen
                if (ShouldRouteSpecialKeysToRemote())
                    TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlShiftEsc, "Caught Ctrl+Shift+Esc and sent it to the remote session.");
                else
                    ShowHint(LocalizationKeys.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer);
                return;
            }

            // --- Ctrl+Alt+End: in WebView2/Guacamole typischerweise ohne Wirkung ---
            if (e.KeyCode == VK_END && hostCtrlAlt)
            {
                if (ShouldRouteSpecialKeysToRemote())
                {
                    e.Handled = true;
                    TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlAltEnd, "Caught Ctrl+Alt+End and sent it to the remote session.");
                }
                else
                {
                    e.Handled = false;
                    ShowHint(LocalizationKeys.Hint_CtrlAltEnd_WithoutEffect_mstsc);
                }
                return;
            }

            // --- Win+R (OS-reserviert) -> abfangen ---
            // Windows-Taste selbst kommt hier i. d. R. nicht an; falls doch, verhindern wir lokal.
            if ((IsWinPressed() && e.KeyCode == VK_R))
            {
                e.Handled = true;
                if (ShouldRouteSpecialKeysToRemote())
                    TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.WinR, "Caught Win+R and sent it to the remote session.");
                else
                    ShowHint(LocalizationKeys.Hint_WinR_Catched_NotForwardableToRemoteServer);
                return;
            }

            if (fullScreenToolStripMenuItem.Checked && e.KeyCode == Keys.Cancel && hostCtrlAlt)
            {
                e.Handled = true;
                SwitchFullScreenMode(false);
                ShowHint(LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff);
                return;
            }

            // Restore window state from fullscreen on Strg+Break
            if (e.KeyCode == Keys.Insert && hostCtrlAlt)
            {
                e.Handled = true;
                SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
                if (!fullScreenToolStripMenuItem.Checked)
                    ShowHint(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff);
                else
                    ShowHint(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn);
                return;
            }

            // Go to guacamole home screen
            if (e.KeyCode == Keys.Home && hostCtrlAlt)
            {
                e.Handled = true;
                connectionHomeToolStripMenuItem.PerformClick();
                return;
            }

            // New window
            if (e.KeyCode == Keys.N && hostCtrlAlt)
            {
                e.Handled = true;
                newWindowToolStripMenuItem.PerformClick();
                return;
            }

            // Standard: Alles andere mit Ctrl/Alt/Shift/AltGr durchlassen
            e.Handled = false;
        }

        private bool _altF4Detected;

        /// <summary>
        /// Zusätzlicher „Fallschirm“ gegen unerwünschtes Schließen via Alt+F4 → SC_CLOSE
        /// </summary>
        /// <param name="m"></param>
        [DebuggerStepThrough]
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSKEYDOWN = 0x0104;
            const int VK_F4 = 0x73;
            const int KF_ALTDOWN = 0x2000;

            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;

            if (m.Msg == WM_SYSKEYDOWN)
            {
                int vk = (int)m.WParam;
                int lParam = m.LParam.ToInt32();
                bool alt = (lParam & KF_ALTDOWN) != 0;

                if (vk == VK_F4 && alt)
                {
                    _altF4Detected = true;
                }
            }
            else if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_CLOSE)
            {
                if (_altF4Detected)
                {
                    _altF4Detected = false;
                    ShowHint(LocalizationKeys.Hint_AltF4_CatchedAndIgnored);
                    return; // blockieren
                }
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Hilfsfunktion: Prüfung, ob (evtl.) eine Win-Taste aktiv ist.
        /// In AcceleratorKeyPressed bekommen wir Win i. d. R. gar nicht.
        /// </summary>
        /// <returns></returns>
        private static bool IsWinPressed()
        {
            // Minimalprüfung: Abfrage per GetKeyState (links/rechts) – optional.
            short GetKeyState(int vk) => NativeMethods.GetKeyState(vk);
            return (GetKeyState(0x5B) < 0) || (GetKeyState(0x5C) < 0); // VK_LWIN, VK_RWIN
        }

        private static bool IsAltGrPressed()
        {
            short GetKeyState(int vk) => NativeMethods.GetKeyState(vk);
            bool rightAlt = GetKeyState(0xA5) < 0;     // VK_RMENU
            bool leftAlt = GetKeyState(0xA4) < 0;      // VK_LMENU
            bool leftCtrl = GetKeyState(0xA2) < 0;     // VK_LCONTROL
            bool rightCtrl = GetKeyState(0xA3) < 0;    // VK_RCONTROL

            return rightAlt && !leftAlt && leftCtrl && !rightCtrl;
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern short GetKeyState(int nVirtKey);
        }

        /// <summary>
        /// Processes a keyboard command key before it is dispatched to the control.
        /// </summary>
        /// <remarks>Override this method to implement custom keyboard handling for the control. If not
        /// overridden, the base implementation determines whether the key is processed.</remarks>
        /// <param name="msg">A Windows message representing the keyboard input to process.</param>
        /// <param name="keyData">One of the Keys values that specifies the key data for the keyboard input.</param>
        /// <returns>true if the key was processed by the control; otherwise, false.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }


    }
}
