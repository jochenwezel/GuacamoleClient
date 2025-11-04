using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm : Form
    {
        private const bool TEST_MENU_ENABLED = false;

        private readonly ToolTip _tip;
        private readonly Timer _closeTimer = new() { Interval = 1200 }; // sanftes Close nach Hinweis

        private CoreWebView2Environment? _env;
        private CoreWebView2Controller? _controller;
        private CoreWebView2? _core;

        private bool _altF4Detected;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem quitToolStripMenuItem;
        private ToolStripMenuItem connectionHomeToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem testToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem fullScreenToolStripMenuItem;
        private Panel WebBrowserHostPanel;

        public string StartUrl { get; init; }
        private readonly HashSet<string> _trustedHosts = new HashSet<string>();


        [Obsolete("For designer support only", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MainForm() : this(new Uri("https://guacamole.apache.org/")) { }

        public MainForm(Uri startUrl)
        {
            this.StartUrl = startUrl.ToString();
            _trustedHosts.Add(startUrl.Host);
            InitializeComponent();

            Text = $"GuacamoleClient v{Application.ProductVersion} - {startUrl.ToString()}";
            KeyPreview = true;
            SetMenuStripBackgroundColorRecursive(menuStrip1!, Color.Red);
            testToolStripMenuItem!.Available = TEST_MENU_ENABLED;

            _tip = new ToolTip
            {
                IsBalloon = false,
                AutoPopDelay = 5000,
                InitialDelay = 0,
                ReshowDelay = 0,
                ShowAlways = true
            };

            this.WebBrowserHostPanel!.Resize += (_, __) => UpdateControllerBounds();
            _closeTimer.Tick += (_, __) => { _closeTimer.Stop(); Close(); };
        }

        private void SetMenuStripBackgroundColorRecursive(MenuStrip item, Color newColor)
        {
            item.BackColor = newColor;
            foreach (ToolStripItem child in item.Items)
                if (child is ToolStripMenuItem m2)
                    SetMenuStripBackgroundColorRecursive(m2, newColor);
                else
                    child.BackColor = newColor;
        }

        private void SetMenuStripBackgroundColorRecursive(ToolStripMenuItem item, Color newColor)
        {
            item.BackColor = newColor;
            foreach (ToolStripItem child in item.DropDownItems)
                if (child is ToolStripMenuItem m2)
                    SetMenuStripBackgroundColorRecursive(m2, newColor);
                else
                    child.BackColor = newColor;
        }

        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            connectionHomeToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            quitToolStripMenuItem = new ToolStripMenuItem();
            testToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            fullScreenToolStripMenuItem = new ToolStripMenuItem();
            WebBrowserHostPanel = new Panel();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, testToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1264, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { connectionHomeToolStripMenuItem, toolStripSeparator1, quitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(81, 20);
            fileToolStripMenuItem.Text = "&Connection";
            // 
            // connectionHomeToolStripMenuItem
            // 
            connectionHomeToolStripMenuItem.Name = "connectionHomeToolStripMenuItem";
            connectionHomeToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Pos1";
            connectionHomeToolStripMenuItem.Size = new Size(254, 22);
            connectionHomeToolStripMenuItem.Text = "Connection Home";
            connectionHomeToolStripMenuItem.Click += connectionHomeToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(251, 6);
            // 
            // quitToolStripMenuItem
            // 
            quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            quitToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+F4";
            quitToolStripMenuItem.Size = new Size(254, 22);
            quitToolStripMenuItem.Text = "&Quit";
            quitToolStripMenuItem.Click += quitToolStripMenuItem_Click;
            // 
            // testToolStripMenuItem
            // 
            testToolStripMenuItem.Name = "testToolStripMenuItem";
            testToolStripMenuItem.Size = new Size(40, 20);
            testToolStripMenuItem.Text = "Test";
            testToolStripMenuItem.Click += testToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fullScreenToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            // 
            // fullScreenToolStripMenuItem
            // 
            fullScreenToolStripMenuItem.Name = "fullScreenToolStripMenuItem";
            fullScreenToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Insert";
            fullScreenToolStripMenuItem.Size = new Size(219, 22);
            fullScreenToolStripMenuItem.Text = "Full-Screen";
            fullScreenToolStripMenuItem.Click += fullScreenToolStripMenuItem_Click;
            // 
            // WebBrowserHostPanel
            // 
            WebBrowserHostPanel.Dock = DockStyle.Fill;
            WebBrowserHostPanel.Location = new Point(0, 24);
            WebBrowserHostPanel.Name = "WebBrowserHostPanel";
            WebBrowserHostPanel.Size = new Size(1264, 737);
            WebBrowserHostPanel.TabIndex = 1;
            // 
            // MainForm
            // 
            ClientSize = new Size(1264, 761);
            Controls.Add(WebBrowserHostPanel);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Load += MainForm_Load;
            ResizeEnd += MainForm_ResizeEnd;
            KeyDown += MainForm_KeyDown;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        private void MainForm_ResizeEnd(object? sender, EventArgs e)
        {
            if (!fullScreenToolStripMenuItem.Checked && this.WindowState == FormWindowState.Normal)
            {
                _previousBounds = this.Bounds;
            }
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            if (this.DesignMode) return;
            if (!fullScreenToolStripMenuItem.Checked && this.WindowState == FormWindowState.Normal)
                _previousBounds = this.Bounds;
            else
                _previousBounds = new Rectangle(0, 0, 1280, 800);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            Icon = (Icon)resources.GetObject("$this.Icon")!;
            await InitWebView2Async();
            _core!.PermissionRequested += CoreWebView2_PermissionRequested;
        }

        private async Task InitWebView2Async()
        {
            _env = await CoreWebView2Environment.CreateAsync();
            _controller = await _env.CreateCoreWebView2ControllerAsync(this.WebBrowserHostPanel!.Handle);
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

        private void CoreWebView2_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            // Nur für vertrauenswürdige Origins und nur für die Zwischenablage erlauben
            var uri = new Uri(e.Uri);
            bool isTrusted = uri.Scheme == Uri.UriSchemeHttps && _trustedHosts.Contains(uri.Host);

            if (isTrusted && (
                e.PermissionKind == CoreWebView2PermissionKind.ClipboardRead
            // Manche SDKs haben zusätzlich/alternativ:
            // || e.PermissionKind == CoreWebView2PermissionKind.ClipboardReadWrite
            ))
            {
                e.State = CoreWebView2PermissionState.Allow;
                e.Handled = true; // verhindert den Standard-Dialog
                return;
            }

            // Für alles andere: explizit ablehnen (oder ignorieren → Standardverhalten)
            // e.State = CoreWebView2PermissionState.Deny; e.Handled = true;
        }

        private void UpdateControllerBounds()
        {
            if (_controller == null) return;
            var r = this.WebBrowserHostPanel!.ClientRectangle;
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
            const uint VK_F4 = (uint)Keys.F4;
            const uint VK_END = (uint)Keys.End;
            const uint VK_ESC = (uint)Keys.Escape;
            const uint VK_R = (uint)Keys.R;
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

            if (fullScreenToolStripMenuItem.Checked && e.VirtualKey == (uint)Keys.Cancel && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(false);
                ShowHint("Hinweis: Strg+Alt+Break >> Full Screen wird deaktiviert");
                return;
            }

            // Restore window state from fullscreen on Strg+Break
            if (e.VirtualKey == (uint)Keys.Insert && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
                if (!fullScreenToolStripMenuItem.Checked)
                    ShowHint("Hinweis: Strg+Alt+Insert >> Full Screen wird deaktiviert");
                else
                    ShowHint("Hinweis: Strg+Alt+Insert >> Full Screen wird aktiviert");
                return;
            }

            // Go to guacamole home screen
            if (e.VirtualKey == (uint)Keys.Home && alt && ctrl)
            {
                e.Handled = true;
                connectionHomeToolStripMenuItem.PerformClick();
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

        private void ShowHint(string text)
        {
            try { _tip.Show(text, this, 20, 20, 5000); } catch { /*best effort*/ }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern short GetKeyState(int nVirtKey);
        }

        public async Task<string?> GetGuacamoleAuthTokenAsync()
        {
            var cookieManager = _core!.CookieManager;
            var cookies = await cookieManager.GetCookiesAsync(this.StartUrl).ConfigureAwait(false);
            foreach (var c in cookies)
            {
                Console.WriteLine($"{c.Name} = {c.Value} ; HttpOnly={c.IsHttpOnly} ; Secure={c.IsSecure} ; SameSite={c.SameSite}");
                if (c.Name == "GUAC_AUTH" || c.Name == "guac.token")
                {
                    string token = c.Value;
                    // token verwenden
                    return token;
                }
            }
            return null;
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void connectionHomeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _core?.Navigate(StartUrl);
            _controller?.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
        }

        private async void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string json = await TestAsync();
            MessageBox.Show(this, $"Guacamole Auth Token:\n{json}", "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            var token = await GetGuacamoleAuthTokenAsync();
            MessageBox.Show(this, $"Guacamole Auth Token:\n{token}", "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task<string> TestAsync()
        {
            var json = await _core!.ExecuteScriptAsync(@"
(() => JSON.stringify({
  keys: Object.keys(localStorage),
  guac_auth: localStorage.getItem('GUAC_AUTH') || null
}))()");
            //            MessageBox.Show(this, $"Guacamole Auth Token:\n{json}", "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Dump aller Keys + GUAC_AUTH anzeigen
            var script = @"
(() => {
  const ls = (typeof localStorage !== 'undefined') ? localStorage : null;
  const ss = (typeof sessionStorage !== 'undefined') ? sessionStorage : null;
  const res = {
    origin: location.origin,
    path: location.pathname,
    localKeys: ls ? Object.keys(ls) : [],
    sessionKeys: ss ? Object.keys(ss) : [],
    guacLocal: ls ? ls.getItem('GUAC_AUTH') : null,
    guacSession: ss ? ss.getItem('GUAC_AUTH') : null
  };
  return JSON.stringify(res);
})()";
            var json2 = await _core!.ExecuteScriptAsync(script);
            // json ist ein C#-String mit Anführungszeichen – ggf. unescapen/parsen:
            var payload = System.Text.Json.JsonDocument.Parse(json2.Trim('"').Replace("\\\"", "\""));
            // -> payload.RootElement.GetProperty("guacLocal").GetString()
            //            MessageBox.Show(this, $"Guacamole Auth Token:\n{json2}", "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return json2;
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
        }

        private Rectangle _previousBounds;
        private void SwitchFullScreenMode(bool fullScreen)
        {
            if (!fullScreenToolStripMenuItem.Checked && this.WindowState == FormWindowState.Normal)
            {
                _previousBounds = this.Bounds; //take note of current size
            }
            fullScreenToolStripMenuItem.Checked = fullScreen;
            if (fullScreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                //this.StartPosition = FormStartPosition.CenterScreen;
                this.TopMost = true;
                //this.TopLevel = true;
                Screen screen = Screen.FromControl(this);
                Rectangle r = screen.Bounds;
                this.WindowState = FormWindowState.Normal;
                this.SetDesktopBounds(r.X, r.Y, r.Width, r.Height);
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
                this.TopMost = false;
                //this.TopLevel = true;
                this.FormBorderStyle = FormBorderStyle.Sizable;

                this.WindowState = FormWindowState.Normal;
                this.Bounds = _previousBounds;
                //Screen screen = Screen.FromControl(this);
                //Rectangle r = screen.Bounds;
            }
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {

            // Nur KeyDown / SystemKeyDown interessieren
            if (e.Handled)
                return;

            // VK-Konstanten
            const Keys VK_F4 = Keys.F4;
            const Keys VK_END = Keys.End;
            const Keys VK_ESC = Keys.Escape;
            const Keys VK_R = Keys.R;
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
                ShowHint("STRG+ALT+F4: Anwendung wird geschlossen …");
                _closeTimer.Start();
                return;
            }

            // --- Alt+F4 blocken (App bleibt offen) ---
            if (e.KeyCode == VK_F4 && alt && !ctrl)
            {
                e.Handled = true;
                ShowHint("ALT+F4 wurde abgefangen (App bleibt offen).");
                return;
            }

            // --- Ctrl+F4 durchlassen ---
            if (e.KeyCode == VK_F4 && ctrl && !alt)
            {
                e.Handled = false; // weiter an WebView/Seite
                return;
            }

            // --- Ctrl+Shift+Esc (lokaler Task-Manager) -> NICHT weiterleitbar ---
            if (e.KeyCode == VK_ESC && ctrl && shift)
            {
                e.Handled = true; // lokal abfangen
                ShowHint("STRG+UMSCHALT+ESC wurde abgefangen (lokaler Task-Manager). Nicht an Remote weiterleitbar. Tipp: In Guacamole-Menü „Strg+Alt+Entf“ nutzen.");
                return;
            }

            // --- Ctrl+Alt+End: in WebView2/Guacamole typischerweise ohne Wirkung ---
            if (e.KeyCode == VK_END && ctrl && alt)
            {
                // Wir lassen es durch – aber informieren, dass es i. d. R. nichts bewirkt.
                e.Handled = false;
                ShowHint("Hinweis: STRG+ALT+ENDE hat in diesem Setup üblicherweise keine Wirkung (mstsc-Sonderfall).");
                return;
            }

            // --- Win+R (OS-reserviert) -> abfangen ---
            // Windows-Taste selbst kommt hier i. d. R. nicht an; falls doch, verhindern wir lokal.
            if ((IsWinPressed() && e.KeyCode == VK_R))
            {
                e.Handled = true;
                ShowHint("WIN+R wurde abgefangen. Nicht an Remote weiterleitbar. Workaround: STRG+ESC öffnen und dort „Ausführen“ suchen.");
                return;
            }

            if (fullScreenToolStripMenuItem.Checked && e.KeyCode == Keys.Cancel && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(false);
                ShowHint("Hinweis: Strg+Alt+Break >> Full Screen wird deaktiviert");
                return;
            }

            // Restore window state from fullscreen on Strg+Break
            if (e.KeyCode == Keys.Insert && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
                if (!fullScreenToolStripMenuItem.Checked)
                    ShowHint("Hinweis: Strg+Alt+Insert >> Full Screen wird deaktiviert");
                else
                    ShowHint("Hinweis: Strg+Alt+Insert >> Full Screen wird aktiviert");
                return;
            }

            // Go to guacamole home screen
            if (e.KeyCode == Keys.Home && alt && ctrl)
            {
                e.Handled = true;
                connectionHomeToolStripMenuItem.PerformClick();
                return;
            }

            // Standard: Alles andere mit Ctrl/Alt/Shift/AltGr durchlassen
            e.Handled = false;
        }
    }
}
