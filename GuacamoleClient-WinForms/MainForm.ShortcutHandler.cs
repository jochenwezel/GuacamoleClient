using GuacamoleClient.Common;
using GuacamoleClient.Common.Localization;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
            const uint VK_SCROLL = (uint)Keys.Scroll;
            //const int VK_F4 = 0x73;
            //const int VK_END = 0x23;
            //const int VK_ESC = 0x1B;
            //const int VK_R = 0x52; 

            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;     // AltGr erscheint hier als Ctrl+Alt
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool lwin = (Control.ModifierKeys & Keys.LWin) == Keys.LWin;
            bool rwin = (Control.ModifierKeys & Keys.RWin) == Keys.RWin;
            bool win = (lwin || rwin);

            // --- App-Policy: Ctrl+Alt+F4 schließt die App ---
            if (e.VirtualKey == VK_F4 && ctrl && alt)
            {
                e.Handled = true;
                ShowHint(LocalizationKey.Hint_CtrlAltF4_AppWillBeClosed);
                _closeTimer.Start();
                return;
            }

            // --- Alt+F4 blocken (App bleibt offen) ---
            if (e.VirtualKey == VK_F4 && alt && !ctrl)
            {
                e.Handled = true;
                ShowHint(LocalizationKey.Hint_AltF4_CatchedAndIgnored);
                return;
            }

            // --- Ctrl+F4 durchlassen ---
            if (e.VirtualKey == VK_F4 && ctrl && !alt)
            {
                e.Handled = false; // weiter an WebView/Seite
                return;
            }

            // --- App-Policy: Ctrl+Alt+Scroll gibt Tastaturfokus frei ---
            if (e.VirtualKey == VK_SCROLL && ctrl && alt)
            {
                e.Handled = true;
                DetachWebViewFocus(false);
                return;
            }

            // --- Ctrl+Shift+Esc (lokaler Task-Manager) -> NICHT weiterleitbar ---
            if (e.VirtualKey == VK_ESC && ctrl && shift)
            {
                e.Handled = true; // lokal abfangen
                ShowHint(LocalizationKey.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer);
                return;
            }

            // --- Ctrl+Alt+End: in WebView2/Guacamole typischerweise ohne Wirkung ---
            if (e.VirtualKey == VK_END && ctrl && alt)
            {
                // Wir lassen es durch – aber informieren, dass es i. d. R. nichts bewirkt.
                e.Handled = false;
                ShowHint(LocalizationKey.Hint_CtrlAltEnd_WithoutEffect_mstsc);
                return;
            }

            // --- Win+R (OS-reserviert) -> abfangen ---
            // Windows-Taste selbst kommt hier i. d. R. nicht an; falls doch, verhindern wir lokal.
            if ((IsWinPressed() && e.VirtualKey == VK_R))
            {
                e.Handled = true;
                ShowHint(LocalizationKey.Hint_WinR_Catched_NotForwardableToRemoteServer);
                return;
            }

            if (fullScreenToolStripMenuItem.Checked && e.VirtualKey == (uint)Keys.Cancel && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(false);
                ShowHint(LocalizationKey.Hint_CtrlAltBreak_FullscreenModeOff);
                return;
            }

            // Restore window state from fullscreen on Strg+Break
            if (e.VirtualKey == (uint)Keys.Insert && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
                if (!fullScreenToolStripMenuItem.Checked)
                    ShowHint(LocalizationKey.Hint_CtrlAltIns_FullscreenModeOff);
                else
                    ShowHint(LocalizationKey.Hint_CtrlAltIns_FullscreenModeOn);
                return;
            }

            // Go to guacamole home screen
            if (e.VirtualKey == (uint)Keys.Home && alt && ctrl)
            {
                e.Handled = true;
                connectionHomeToolStripMenuItem.PerformClick();
                return;
            }

            // New Window
            if (e.VirtualKey == (uint)Keys.N && alt && ctrl)
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
            const Keys VK_SCROLL = Keys.Scroll;
            //const int VK_F4 = 0x73;
            //const int VK_END = 0x23;
            //const int VK_ESC = 0x1B;
            //const int VK_R = 0x52; 

            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;     // AltGr erscheint hier als Ctrl+Alt
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool lwin = (Control.ModifierKeys & Keys.LWin) == Keys.LWin;
            bool rwin = (Control.ModifierKeys & Keys.RWin) == Keys.RWin;
            bool win = (lwin || rwin);

            // --- App-Policy: Ctrl+Alt+F4 schließt die App ---
            if (e.KeyCode == VK_F4 && ctrl && alt)
            {
                e.Handled = true;
                ShowHint(LocalizationKey.Hint_CtrlAltF4_AppWillBeClosed);
                _closeTimer.Start();
                return;
            }

            // --- Alt+F4 blocken (App bleibt offen) ---
            if (e.KeyCode == VK_F4 && alt && !ctrl)
            {
                e.Handled = true;
                ShowHint(LocalizationKey.Hint_AltF4_CatchedAndIgnored);
                return;
            }

            // --- Ctrl+F4 durchlassen ---
            if (e.KeyCode == VK_F4 && ctrl && !alt)
            {
                e.Handled = false; // weiter an WebView/Seite
                return;
            }

            // --- App-Policy: Ctrl+Alt+Scroll gibt Tastaturfokus frei ---
            if (e.KeyCode == VK_SCROLL && ctrl && alt)
            {
                e.Handled = true;
                SetFocusToWebview2Control();
                return;
            }

            // --- Ctrl+Shift+Esc (lokaler Task-Manager) -> NICHT weiterleitbar ---
            if (e.KeyCode == VK_ESC && ctrl && shift)
            {
                e.Handled = true; // lokal abfangen
                ShowHint(LocalizationKey.Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer);
                return;
            }

            // --- Ctrl+Alt+End: in WebView2/Guacamole typischerweise ohne Wirkung ---
            if (e.KeyCode == VK_END && ctrl && alt)
            {
                // Wir lassen es durch – aber informieren, dass es i. d. R. nichts bewirkt.
                e.Handled = false;
                ShowHint(LocalizationKey.Hint_CtrlAltEnd_WithoutEffect_mstsc);
                return;
            }

            // --- Win+R (OS-reserviert) -> abfangen ---
            // Windows-Taste selbst kommt hier i. d. R. nicht an; falls doch, verhindern wir lokal.
            if ((IsWinPressed() && e.KeyCode == VK_R))
            {
                e.Handled = true;
                ShowHint(LocalizationKey.Hint_WinR_Catched_NotForwardableToRemoteServer);
                return;
            }

            if (fullScreenToolStripMenuItem.Checked && e.KeyCode == Keys.Cancel && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(false);
                ShowHint(LocalizationKey.Hint_CtrlAltBreak_FullscreenModeOff);
                return;
            }

            // Restore window state from fullscreen on Strg+Break
            if (e.KeyCode == Keys.Insert && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
                if (!fullScreenToolStripMenuItem.Checked)
                    ShowHint(LocalizationKey.Hint_CtrlAltIns_FullscreenModeOff);
                else
                    ShowHint(LocalizationKey.Hint_CtrlAltIns_FullscreenModeOn);
                return;
            }

            // Go to guacamole home screen
            if (e.KeyCode == Keys.Home && alt && ctrl)
            {
                e.Handled = true;
                connectionHomeToolStripMenuItem.PerformClick();
                return;
            }

            // New window
            if (e.KeyCode == Keys.N && alt && ctrl)
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
                    ShowHint(LocalizationKey.Hint_AltF4_CatchedAndIgnored);
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
