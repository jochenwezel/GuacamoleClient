using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace GuacShim.ControllerHost
{
    public partial class MainForm : Form
    {
        private readonly Panel _hostPanel;
        private readonly ToolTip _tip;
        private readonly Timer _closeTimer = new() { Interval = 1200 }; // sanftes Close nach Hinweis

        private CoreWebView2Environment? _env;
        private CoreWebView2Controller? _controller;
        private CoreWebView2? _core;

        private bool _altF4Detected;

        public string StartUrl { get; set; }

        public MainForm() : this("https://guacamole.apache.org/") { }

        public MainForm(string startUrl)
        {
            this.StartUrl = startUrl;

            Text = "GuacamoleClient";
            Width = 1280;
            Height = 800;
            KeyPreview = true;

            _hostPanel = new Panel { Dock = DockStyle.Fill };
            Controls.Add(_hostPanel);

            _tip = new ToolTip
            {
                IsBalloon = false,
                AutoPopDelay = 5000,
                InitialDelay = 0,
                ReshowDelay = 0,
                ShowAlways = true
            };

            Load += MainForm_Load;
            _hostPanel.Resize += (_, __) => UpdateControllerBounds();
            _closeTimer.Tick += (_, __) => { _closeTimer.Stop(); Close(); };
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            if (this.DesignMode) return;
            await InitWebView2Async();
        }

        private async Task InitWebView2Async()
        {
            _env = await CoreWebView2Environment.CreateAsync();
            _controller = await _env.CreateCoreWebView2ControllerAsync(_hostPanel.Handle);
            _controller.IsVisible = true;
            UpdateControllerBounds();

            _controller.AcceleratorKeyPressed += Controller_AcceleratorKeyPressed;

            _core = _controller.CoreWebView2;
            _core.Settings.IsStatusBarEnabled = false;
            _core.Settings.AreDefaultContextMenusEnabled = true;
            _core.Settings.AreDevToolsEnabled = false;

            _core.Navigate(StartUrl);
            _controller.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
        }

        private void UpdateControllerBounds()
        {
            if (_controller == null) return;
            var r = _hostPanel.ClientRectangle;
            _controller.Bounds = new Rectangle(r.X, r.Y, r.Width, r.Height);
        }

        // ========== Tastatur-Logik ==========
        // Ziel: Alles durchlassen außer explizit behandelte (OS-reservierte) Kombinationen.

        private void Controller_AcceleratorKeyPressed(object? sender, CoreWebView2AcceleratorKeyPressedEventArgs e)
        {
            // Nur KeyDown / SystemKeyDown interessieren
            if (e.KeyEventKind != CoreWebView2KeyEventKind.KeyDown &&
                e.KeyEventKind != CoreWebView2KeyEventKind.SystemKeyDown)
                return;

            // VK-Konstanten
            const int VK_F4 = 0x73;
            const int VK_END = 0x23;
            const int VK_ESC = 0x1B;
            const int VK_R = 0x52;

            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;     // AltGr erscheint hier als Ctrl+Alt
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            // --- App-Policy: Ctrl+Alt+F4 schließt die App ---
            if (e.VirtualKey == VK_F4 && ctrl && alt)
            {
                e.Handled = true;
                ShowHint("STRG+ALT+F4: Anwendung wird geschlossen …");
                _closeTimer.Start();
                return;
            }

            // --- Alt+F4 blocken (App bleibt offen) ---
            if (e.VirtualKey == VK_F4 && alt && !ctrl)
            {
                e.Handled = true;
                ShowHint("ALT+F4 wurde abgefangen (App bleibt offen).");
                return;
            }

            // --- Ctrl+F4 durchlassen ---
            if (e.VirtualKey == VK_F4 && ctrl && !alt)
            {
                e.Handled = false; // weiter an WebView/Seite
                return;
            }

            // --- Ctrl+Shift+Esc (lokaler Task-Manager) -> NICHT weiterleitbar ---
            if (e.VirtualKey == VK_ESC && ctrl && shift)
            {
                e.Handled = true; // lokal abfangen
                ShowHint("STRG+UMSCHALT+ESC wurde abgefangen (lokaler Task-Manager). Nicht an Remote weiterleitbar. Tipp: In Guacamole-Menü „Strg+Alt+Entf“ nutzen.");
                return;
            }

            // --- Ctrl+Alt+End: in WebView2/Guacamole typischerweise ohne Wirkung ---
            if (e.VirtualKey == VK_END && ctrl && alt)
            {
                // Wir lassen es durch – aber informieren, dass es i. d. R. nichts bewirkt.
                e.Handled = false;
                ShowHint("Hinweis: STRG+ALT+ENDE hat in diesem Setup üblicherweise keine Wirkung (mstsc-Sonderfall).");
                return;
            }

            // --- Win+R (OS-reserviert) -> abfangen ---
            // Windows-Taste selbst kommt hier i. d. R. nicht an; falls doch, verhindern wir lokal.
            if ((IsWinPressed() && e.VirtualKey == VK_R))
            {
                e.Handled = true;
                ShowHint("WIN+R wurde abgefangen. Nicht an Remote weiterleitbar. Workaround: STRG+ESC öffnen und dort „Ausführen“ suchen.");
                return;
            }

            // Standard: Alles andere mit Ctrl/Alt/Shift/AltGr durchlassen
            e.Handled = false;
        }

        // Zusätzlicher „Fallschirm“ gegen unerwünschtes Schließen via Alt+F4 → SC_CLOSE
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
                    ShowHint("ALT+F4 wurde abgefangen (App bleibt offen).");
                    return; // blockieren
                }
            }

            base.WndProc(ref m);
        }

        // Hilfsfunktion: Prüfung, ob (evtl.) eine Win-Taste aktiv ist.
        // In AcceleratorKeyPressed bekommen wir Win i. d. R. gar nicht.
        private static bool IsWinPressed()
        {
            // Minimalprüfung: Abfrage per GetKeyState (links/rechts) – optional.
            short GetKeyState(int vk) => NativeMethods.GetKeyState(vk);
            return (GetKeyState(0x5B) < 0) || (GetKeyState(0x5C) < 0); // VK_LWIN, VK_RWIN
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            SuspendLayout();
            // 
            // MainForm
            // 
            ClientSize = new Size(844, 425);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            ResumeLayout(false);

        }

        private void ShowHint(string text)
        {
            try { _tip.Show(text, this, 20, 20, 5000); } catch { /*best effort*/ }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern short GetKeyState(int nVirtKey);
        }
    }
}
