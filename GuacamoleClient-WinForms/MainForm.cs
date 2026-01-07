using GuacamoleClient.Common;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        private CustomMenuStrip mainMenuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem quitToolStripMenuItem;
        private ToolStripMenuItem connectionHomeToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem testToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem fullScreenToolStripMenuItem;
        private Panel WebBrowserHostPanel;
        private ToolStripMenuItem stopFullScreenModeToolStripMenuItem;
        private ToolStripMenuItem guacamoleUserSettingsToolStripMenuItem;
        private ToolStripMenuItem guacamoleConnectionConfigurationsToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem newWindowToolStripMenuItem;
        private Timer formTitleRefreshTimer;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem connectionNameInFullScreenModeToolStripMenuItem;

        public Uri HomeUrl { get; init; }
        public Uri StartUrl { get; init; }
        private readonly HashSet<string> _trustedHosts = new HashSet<string>();

        [Obsolete("For designer support only", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MainForm() : this(new Uri("https://guacamole.apache.org/"), new Uri("https://guacamole.apache.org/")) { }

        public MainForm(Uri homeUrl, Uri startUrl)
        {
            this.HomeUrl = homeUrl;
            this.StartUrl = startUrl;
            _trustedHosts.Add(homeUrl.Host);
            InitializeComponent();
            InitializeControlFocusManagementWithKeyboardCapturingHandler();

            this.UpdateFormTitle(startUrl);
            KeyPreview = true;
            mainMenuStrip!.SetMenuStripColorsRecursive(Color.OrangeRed, Color.OrangeRed, Color.DarkRed, Color.Black, Color.DarkGray);
            testToolStripMenuItem!.Available = TEST_MENU_ENABLED;

            _tip = new ToolTip
            {
                IsBalloon = false,
                AutoPopDelay = 5000,
                InitialDelay = 0,
                ReshowDelay = 0,
                ShowAlways = true
            };

            this.WebBrowserHostPanel!.LocationChanged += (_, __) => UpdateLocationUrl();
            this.WebBrowserHostPanel!.Resize += (_, __) => UpdateControllerBounds();
            _closeTimer.Tick += (_, __) => { _closeTimer.Stop(); Close(); };
        }

        #region Debug helpers
        private string ControlName(Control c)
        {
            if (c == null)
                return "{null}";
            else if (String.IsNullOrEmpty(c.Name))
                return "{empty}";
            else
                return c.Name;
        }

        private ToolStripMenuItem HintStopWebcontrol2FocusShortcut;
        private int _openDropdowns;
        private bool IsMenuOpen
        {
            get
            {
                if (_openDropdowns > 0 || this.MainMenuStrip!.Focused || this.MainMenuStrip.ContainsFocus)
                    return true;
                else
                {
                    foreach (ToolStripMenuItem item in this.MainMenuStrip.Items)
                    {
                        if (item.Selected) return true;
                    }
                    return false;
                }
            }
        }
        #endregion

        #region Keyboard capturing + active control focus

        void InitializeControlFocusManagementWithKeyboardCapturingHandler()
        {
            HookMenuItemsRecursive(mainMenuStrip!.Items);
            this.Activated += (_, __) => RestoreFocusOnWebview2Control();
            this.Deactivate += (_, __) => DetachWebViewFocus(true);
            this.mainMenuStrip.Leave += (_, __) => RestoreFocusOnWebview2Control();
            this.HintStopWebcontrol2FocusShortcut!.Visible = false;
        }

        /// <summary>
        /// Invisible Textbox for capturing focus and while focused capturing keyboard shortcuts
        /// </summary>
        private TextBox _focusSink;

        private void RestoreFocusOnWebview2Control()
        {
            // Wenn Sie danach zurück in WebView wollen:
            WebBrowserHostPanel.Focus();
            _controller?.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
            IsKeyboardFocusBoundToWebview2Control = KeyboardCaptureMode.GrabbingEnabled_ShowKeyboardShortcutInfo;
        }

        /// <summary>
        /// Removes keyboard focus from the WebView2 control by setting focus to an alternative control within the form.
        /// </summary>
        /// <remarks>Use this method to ensure that the WebView2 control no longer receives keyboard
        /// input, which may be necessary before disposing the control or when redirecting user interaction to another
        /// part of the application (e.g. main menu). This method is intended for internal focus management and should be called only
        /// when it is necessary to explicitly change the active control.</remarks>
        private void DetachWebViewFocus(bool hideShortcutTip)
        {
            // Ein anderes Win32-Fokusziel setzen → WebView2 verliert Tastatur
            _focusSink.Focus();
            this.ActiveControl = _focusSink; // optional, hilft WinForms-intern
            if (hideShortcutTip)
                IsKeyboardFocusBoundToWebview2Control = KeyboardCaptureMode.GrabbingDisabled_HideKeyboardShortcutInfo;
            else
                IsKeyboardFocusBoundToWebview2Control = KeyboardCaptureMode.GrabbingDisabled_ShowKeyboardShortcutInfo;
        }

        private enum KeyboardCaptureMode
        {
            GrabbingEnabled_ShowKeyboardShortcutInfo,
            GrabbingDisabled_ShowKeyboardShortcutInfo,
            GrabbingDisabled_HideKeyboardShortcutInfo,
        }
        private KeyboardCaptureMode _IsKeyboardFocusBoundToWebview2Control = KeyboardCaptureMode.GrabbingDisabled_HideKeyboardShortcutInfo;
        private KeyboardCaptureMode IsKeyboardFocusBoundToWebview2Control
        {
            get { return _IsKeyboardFocusBoundToWebview2Control; }
            set
            {
                _IsKeyboardFocusBoundToWebview2Control = value;
                switch (value)
                {
                    case KeyboardCaptureMode.GrabbingEnabled_ShowKeyboardShortcutInfo:
                        this.HintStopWebcontrol2FocusShortcut!.Text = LocalizedString(LocalizationKeys.Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow);
                        this.HintStopWebcontrol2FocusShortcut!.Visible = true;
                        break;
                    case KeyboardCaptureMode.GrabbingDisabled_ShowKeyboardShortcutInfo:
                        this.HintStopWebcontrol2FocusShortcut!.Text = LocalizedString(LocalizationKeys.Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow);
                        this.HintStopWebcontrol2FocusShortcut!.Visible = true;
                        break;
                    case KeyboardCaptureMode.GrabbingDisabled_HideKeyboardShortcutInfo:
                        this.HintStopWebcontrol2FocusShortcut!.Visible = false;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        #endregion

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

        private void HookMenuItemsRecursive(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripMenuItem mi)
                {
                    mi.DropDownOpened += (_, __) =>
                    {
                        _openDropdowns++;
                        DetachWebViewFocus(false);
                    };
                    mi.DropDownClosed += (_, __) =>
                    {
                        _openDropdowns = Math.Max(0, _openDropdowns - 1);
                        //BeginInvoke((Action)RestoreLastFocus); // z.B. WebView2-Fokus zurück
                        RestoreFocusOnWebview2Control();
                    };

                    // Untermenüs rekursiv
                    if (mi.HasDropDownItems)
                        HookMenuItemsRecursive(mi.DropDownItems);
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            TitleBarHelper.ApplyTitleBarColors(this, Color.OrangeRed, Color.Black);
        }

        private void NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Öffne neuen MainForm mit der Ziel-URL
            var form = new MainForm(this.HomeUrl, new Uri(e.Uri));
            form.Show();
            // Verhindere das Öffnen im aktuellen WebView
            e.Handled = true;
        }

        public void UpdateFormTitle(Uri currentUrl) => this.UpdateFormTitle(currentUrl, String.Empty);
        public void UpdateFormTitle(Uri currentUrl, string documentTitle)
        {
            string focusWarning = String.Empty;
            if (System.Diagnostics.Debugger.IsAttached)
            {
                if (this.IsMenuOpen) focusWarning += " - " + this.ControlName(this.MainMenuStrip);
                if (!this.WebBrowserHostPanel.Focused) focusWarning += " - " + LocalizedString(LocalizationKeys.FocussedAnotherControlWarning) + this.ControlName(this.ActiveControl);
            }

            if (string.IsNullOrEmpty(documentTitle))
            {
                this.Text = $"{currentUrl.ToString()}{focusWarning} - GuacamoleClient v{Application.ProductVersion}";
                this.connectionNameInFullScreenModeToolStripMenuItem.Text = currentUrl.ToString();
            }
            else
            {
                this.Text = $"{documentTitle}{focusWarning} - {currentUrl.ToString()} - GuacamoleClient v{Application.ProductVersion}";
                this.connectionNameInFullScreenModeToolStripMenuItem.Text = documentTitle;
            }
        }

        public Uri GuacamoleSettingsUrl
        {
            get
            {
                return new Uri(this.HomeUrl, "#/settings/preferences");
            }
        }
        public Uri GuacamoleConnectionConfigurationsUrl
        {
            get
            {
                return new Uri(this.HomeUrl, "#/settings/mysql/connections");
            }
        }

        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
            mainMenuStrip = new CustomMenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            connectionHomeToolStripMenuItem = new ToolStripMenuItem();
            guacamoleUserSettingsToolStripMenuItem = new ToolStripMenuItem();
            guacamoleConnectionConfigurationsToolStripMenuItem = new ToolStripMenuItem();
            newWindowToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            quitToolStripMenuItem = new ToolStripMenuItem();
            testToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            fullScreenToolStripMenuItem = new ToolStripMenuItem();
            stopFullScreenModeToolStripMenuItem = new ToolStripMenuItem();
            connectionNameInFullScreenModeToolStripMenuItem = new ToolStripMenuItem();
            WebBrowserHostPanel = new Panel();
            formTitleRefreshTimer = new Timer(components);
            _focusSink = new TextBox();
            HintStopWebcontrol2FocusShortcut = new ToolStripMenuItem();
            mainMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, testToolStripMenuItem, viewToolStripMenuItem, connectionNameInFullScreenModeToolStripMenuItem, HintStopWebcontrol2FocusShortcut });
            mainMenuStrip.Location = new Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new Size(1264, 24);
            mainMenuStrip.TabIndex = 0;
            mainMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { connectionHomeToolStripMenuItem, guacamoleUserSettingsToolStripMenuItem, guacamoleConnectionConfigurationsToolStripMenuItem, newWindowToolStripMenuItem, toolStripSeparator1, quitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(81, 20);
            fileToolStripMenuItem.Text = "&Connection";
            // 
            // connectionHomeToolStripMenuItem
            // 
            connectionHomeToolStripMenuItem.Name = "connectionHomeToolStripMenuItem";
            connectionHomeToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Pos1 / Alt-Gr+Pos1";
            connectionHomeToolStripMenuItem.Size = new Size(330, 22);
            connectionHomeToolStripMenuItem.Text = "Connection Home";
            connectionHomeToolStripMenuItem.Click += connectionHomeToolStripMenuItem_Click;
            // 
            // guacamoleUserSettingsToolStripMenuItem
            // 
            guacamoleUserSettingsToolStripMenuItem.Name = "guacamoleUserSettingsToolStripMenuItem";
            guacamoleUserSettingsToolStripMenuItem.Size = new Size(330, 22);
            guacamoleUserSettingsToolStripMenuItem.Text = "Guacamole User Settings";
            guacamoleUserSettingsToolStripMenuItem.Click += guacamoleUserSettingsToolStripMenuItem_Click;
            // 
            // guacamoleConnectionConfigurationsToolStripMenuItem
            // 
            guacamoleConnectionConfigurationsToolStripMenuItem.Name = "guacamoleConnectionConfigurationsToolStripMenuItem";
            guacamoleConnectionConfigurationsToolStripMenuItem.Size = new Size(330, 22);
            guacamoleConnectionConfigurationsToolStripMenuItem.Text = "Connection Configurations";
            guacamoleConnectionConfigurationsToolStripMenuItem.Click += guacamoleConnectionConfigurationsToolStripMenuItem_Click;
            // 
            // newWindowToolStripMenuItem
            // 
            newWindowToolStripMenuItem.Name = "newWindowToolStripMenuItem";
            newWindowToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+N / Alt-Gr+N";
            newWindowToolStripMenuItem.Size = new Size(330, 22);
            newWindowToolStripMenuItem.Text = "New Window";
            newWindowToolStripMenuItem.Click += newWindowToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(327, 6);
            // 
            // quitToolStripMenuItem
            // 
            quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            quitToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+F4 / Alt-Gr+F4";
            quitToolStripMenuItem.Size = new Size(330, 22);
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
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fullScreenToolStripMenuItem, stopFullScreenModeToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            // 
            // fullScreenToolStripMenuItem
            // 
            fullScreenToolStripMenuItem.Name = "fullScreenToolStripMenuItem";
            fullScreenToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Insert / Alt-Gr+Insert";
            fullScreenToolStripMenuItem.Size = new Size(360, 22);
            fullScreenToolStripMenuItem.Text = "Full-Screen";
            fullScreenToolStripMenuItem.Click += fullScreenToolStripMenuItem_Click;
            // 
            // stopFullScreenModeToolStripMenuItem
            // 
            stopFullScreenModeToolStripMenuItem.Name = "stopFullScreenModeToolStripMenuItem";
            stopFullScreenModeToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Break / Alt-Gr+Break";
            stopFullScreenModeToolStripMenuItem.Size = new Size(360, 22);
            stopFullScreenModeToolStripMenuItem.Text = "Stop Full-Screen Mode";
            stopFullScreenModeToolStripMenuItem.Click += stopFullScreenModeToolStripMenuItem_Click;
            // 
            // connectionNameInFullScreenModeToolStripMenuItem
            // 
            connectionNameInFullScreenModeToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            connectionNameInFullScreenModeToolStripMenuItem.Name = "connectionNameInFullScreenModeToolStripMenuItem";
            connectionNameInFullScreenModeToolStripMenuItem.Size = new Size(405, 20);
            connectionNameInFullScreenModeToolStripMenuItem.Text = "ConnecticonnectionNameInFullScreenModeToolStripMenuItemonNameI";
            // 
            // WebBrowserHostPanel
            // 
            WebBrowserHostPanel.Dock = DockStyle.Fill;
            WebBrowserHostPanel.Location = new Point(0, 24);
            WebBrowserHostPanel.Name = "WebBrowserHostPanel";
            WebBrowserHostPanel.Size = new Size(1264, 701);
            WebBrowserHostPanel.TabIndex = 1;
            // 
            // formTitleRefreshTimer
            // 
            formTitleRefreshTimer.Tick += formTitleRefreshTimer_Tick;
            // 
            // _focusSink
            // 
            _focusSink.BorderStyle = BorderStyle.None;
            _focusSink.Location = new Point(-2000, -2000);
            _focusSink.Name = "_focusSink";
            _focusSink.Size = new Size(100, 16);
            _focusSink.TabIndex = 0;
            _focusSink.TabStop = false;
            _focusSink.Visible = false;
            // 
            // HintStopWebcontrol2FocusShortcut
            // 
            HintStopWebcontrol2FocusShortcut.Alignment = ToolStripItemAlignment.Right;
            HintStopWebcontrol2FocusShortcut.Enabled = false;
            HintStopWebcontrol2FocusShortcut.Name = "HintStopWebcontrol2FocusShortcut";
            HintStopWebcontrol2FocusShortcut.Size = new Size(98, 20);
            HintStopWebcontrol2FocusShortcut.Text = "Ctrl+Alt+Scroll";
            // 
            // MainForm
            // 
            ClientSize = new Size(1264, 725);
            Controls.Add(_focusSink);
            Controls.Add(WebBrowserHostPanel);
            Controls.Add(mainMenuStrip);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = mainMenuStrip;
            Name = "MainForm";
            Load += MainForm_Load;
            ResizeEnd += MainForm_ResizeEnd;
            KeyDown += MainForm_KeyDown;
            MouseClick += MainForm_MouseClick;
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
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
            SwitchFullScreenMode(fullScreenToolStripMenuItem.Checked);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            Icon = (Icon)resources.GetObject("$this.Icon")!;
            await InitWebView2Async();
            _core!.PermissionRequested += CoreWebView2_PermissionRequested;
            _core!.NavigationStarting += NavigationStarting;
            _core!.NavigationCompleted += NavigationCompleted;
            _core!.NewWindowRequested += NewWindowRequested;
            _core!.FaviconChanged += (_, __) => RefreshFaviconAsync();
            RefreshFaviconAsync();
        }

        private async void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            //configure timer to short timeout to refresh form controls ASAP
            this.formTitleRefreshTimer.Enabled = true;
            this.formTitleRefreshTimer.Interval = 100;
            this.formTitleRefreshTimer.Start();
        }

        private Exception? SwitchMenuItemsBasedOnShownContent_Ex = null;
        private async void SwitchMenuItemsBasedOnShownContent()
        {
            string currentHtml;

            try
            {
                currentHtml = await GetCurrentHtmlAsync();
                SwitchMenuItemsBasedOnShownContent_Ex = null;
            }
            catch (Exception ex)
            {
                if (this.Disposing || this.IsDisposed)
                {
                    //form is disposing/disposed, thrown exceptions can be ignored
                }
                else if (SwitchMenuItemsBasedOnShownContent_Ex == null)
                {
                    SwitchMenuItemsBasedOnShownContent_Ex = ex;
                    MessageBox.Show(this, $"Unexpected exception:\n{ex.ToString()}", "Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    //ignore repeated exceptions
                }
                return;
            }

            //Check for login form and show menu items accordingly
            if (GuacamoleUrlAndContentChecks.ContentIsGuacamoleLoginForm(currentHtml))
            {
                guacamoleUserSettingsToolStripMenuItem!.Available = false;
                guacamoleConnectionConfigurationsToolStripMenuItem!.Available = false;
                newWindowToolStripMenuItem!.Available = false;
            }
            else
            {
                guacamoleUserSettingsToolStripMenuItem!.Available = true;
                guacamoleConnectionConfigurationsToolStripMenuItem!.Available = true;
                newWindowToolStripMenuItem!.Available = true;
            }
        }

        private async Task<string> GetCurrentHtmlAsync()
        {
            string htmlJson = await _core!.ExecuteScriptAsync(
                "document.documentElement.outerHTML"
            );

            // ExecuteScriptAsync liefert JSON-encodierten String zurück
            return System.Text.Json.JsonSerializer.Deserialize<string>(htmlJson)!;
        }

        private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            UpdateLocationUrl(new Uri(e.Uri));
        }

        private Icon? CreateIconFromPngStream(Stream pngStream)
        {
            try
            {
                // PNG vollständig in Bytearray kopieren
                using var msPng = new MemoryStream();
                pngStream.CopyTo(msPng);
                byte[] pngBytes = msPng.ToArray();

                using var pngBitmap = new Bitmap(msPng);

                using var icoStream = new MemoryStream();

                // ICO HEADER (6 bytes)
                icoStream.Write(new byte[] { 0, 0, 1, 0, 1, 0 }, 0, 6);

                // ICON DIRECTORY ENTRY (16 bytes)
                byte width = (byte)(pngBitmap.Width >= 256 ? 0 : pngBitmap.Width);
                byte height = (byte)(pngBitmap.Height >= 256 ? 0 : pngBitmap.Height);

                icoStream.WriteByte(width);        // width
                icoStream.WriteByte(height);       // height
                icoStream.WriteByte(0);            // colors
                icoStream.WriteByte(0);            // reserved
                icoStream.Write(BitConverter.GetBytes((short)1), 0, 2);   // planes = 1
                icoStream.Write(BitConverter.GetBytes((short)32), 0, 2);  // bit depth = 32
                icoStream.Write(BitConverter.GetBytes(pngBytes.Length), 0, 4); // bytes in PNG
                icoStream.Write(BitConverter.GetBytes(22), 0, 4); // offset to PNG data

                // PNG-Daten anhängen
                icoStream.Write(pngBytes, 0, pngBytes.Length);

                icoStream.Position = 0;
                return new Icon(icoStream);
            }
            catch
            {
                return null;
            }
        }

        private async void RefreshFaviconAsync()
        {
            try
            {
                Stream iconStream = await _core!.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
                if (iconStream != null && iconStream.Length > 0)
                {
                    Icon? icon = CreateIconFromPngStream(iconStream);
                    if (icon != null)
                        this.Icon = icon;
                }
            }
            catch
            {
                // ignore
            }
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

            _core.Navigate(StartUrl.ToString());            
            RestoreFocusOnWebview2Control();
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

        private void UpdateLocationUrl()
        {
            try
            {
                if (_core == null || _core.IsSuspended) return;
                this.UpdateFormTitle(new Uri(_core!.Source), _core!.DocumentTitle);
            }
            catch
            {
                //ignore
            }
        }

        private void UpdateLocationUrl(Uri newUri)
        {
            if (_core == null) return;
            this.UpdateFormTitle(newUri, _core!.DocumentTitle);
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
                ShowHint(LocalizationKeys.HintCtrlAltF4AppWillBeClosed);
                _closeTimer.Start();
                return;
            }

            // --- Alt+F4 blocken (App bleibt offen) ---
            if (e.VirtualKey == VK_F4 && alt && !ctrl)
            {
                e.Handled = true;
                ShowHint(LocalizationKeys.HintAltF4CatchedAndIgnored);
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
                ShowHint(LocalizationKeys.HintCtrlShiftEscCatched_NotForwardableToRemoteServer);
                return;
            }

            // --- Ctrl+Alt+End: in WebView2/Guacamole typischerweise ohne Wirkung ---
            if (e.VirtualKey == VK_END && ctrl && alt)
            {
                // Wir lassen es durch – aber informieren, dass es i. d. R. nichts bewirkt.
                e.Handled = false;
                ShowHint(LocalizationKeys.HintCtrlAltEndWithoutEffect_mstsc);
                return;
            }

            // --- Win+R (OS-reserviert) -> abfangen ---
            // Windows-Taste selbst kommt hier i. d. R. nicht an; falls doch, verhindern wir lokal.
            if ((IsWinPressed() && e.VirtualKey == VK_R))
            {
                e.Handled = true;
                ShowHint(LocalizationKeys.HintWinRCatched_NotForwardableToRemoteServer);
                return;
            }

            if (fullScreenToolStripMenuItem.Checked && e.VirtualKey == (uint)Keys.Cancel && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(false);
                ShowHint(LocalizationKeys.HintCtrlAltBreakFullscreenModeOff);
                return;
            }

            // Restore window state from fullscreen on Strg+Break
            if (e.VirtualKey == (uint)Keys.Insert && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
                if (!fullScreenToolStripMenuItem.Checked)
                    ShowHint(LocalizationKeys.HintCtrlAltInsFullscreenModeOff);
                else
                    ShowHint(LocalizationKeys.HintCtrlAltInsFullscreenModeOn);
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

        // Zusätzlicher „Fallschirm“ gegen unerwünschtes Schließen via Alt+F4 → SC_CLOSE
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
                    ShowHint(LocalizationKeys.HintAltF4CatchedAndIgnored);
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

        [Obsolete("LocalizationKeys", true)]
        private void ShowHint(string text)
        {
            try { _tip.Show(text, this, 20, 20, 5000); } catch { /*best effort*/ }
        }

        private void ShowHint(LocalizationKeys localizedString)
        {
            string text = LocalizedString(localizedString);
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
            var cookies = await cookieManager.GetCookiesAsync(this.StartUrl.ToString()).ConfigureAwait(false);
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
            _core?.Navigate(this.HomeUrl.ToString());
            RestoreFocusOnWebview2Control();
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
                //this.TopMost = true;
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
            this.stopFullScreenModeToolStripMenuItem.Available = fullScreen;
            this.connectionNameInFullScreenModeToolStripMenuItem.Available = fullScreen;
            this.connectionNameInFullScreenModeToolStripMenuItem.Enabled = false;
            this.connectionNameInFullScreenModeToolStripMenuItem.ForeColor = Color.Black;
            this.connectionNameInFullScreenModeToolStripMenuItem.Font = new Font(this.connectionNameInFullScreenModeToolStripMenuItem.Font, FontStyle.Bold);
        }

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
                ShowHint(LocalizationKeys.HintCtrlAltF4AppWillBeClosed);
                _closeTimer.Start();
                return;
            }

            // --- Alt+F4 blocken (App bleibt offen) ---
            if (e.KeyCode == VK_F4 && alt && !ctrl)
            {
                e.Handled = true;
                ShowHint(LocalizationKeys.HintAltF4CatchedAndIgnored);
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
                RestoreFocusOnWebview2Control();
                return;
            }

            // --- Ctrl+Shift+Esc (lokaler Task-Manager) -> NICHT weiterleitbar ---
            if (e.KeyCode == VK_ESC && ctrl && shift)
            {
                e.Handled = true; // lokal abfangen
                ShowHint(LocalizationKeys.HintCtrlShiftEscCatched_NotForwardableToRemoteServer);
                return;
            }

            // --- Ctrl+Alt+End: in WebView2/Guacamole typischerweise ohne Wirkung ---
            if (e.KeyCode == VK_END && ctrl && alt)
            {
                // Wir lassen es durch – aber informieren, dass es i. d. R. nichts bewirkt.
                e.Handled = false;
                ShowHint(LocalizationKeys.HintCtrlAltEndWithoutEffect_mstsc);
                return;
            }

            // --- Win+R (OS-reserviert) -> abfangen ---
            // Windows-Taste selbst kommt hier i. d. R. nicht an; falls doch, verhindern wir lokal.
            if ((IsWinPressed() && e.KeyCode == VK_R))
            {
                e.Handled = true;
                ShowHint(LocalizationKeys.HintWinRCatched_NotForwardableToRemoteServer);
                return;
            }

            if (fullScreenToolStripMenuItem.Checked && e.KeyCode == Keys.Cancel && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(false);
                ShowHint(LocalizationKeys.HintCtrlAltBreakFullscreenModeOff);
                return;
            }

            // Restore window state from fullscreen on Strg+Break
            if (e.KeyCode == Keys.Insert && alt && ctrl)
            {
                e.Handled = true;
                SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
                if (!fullScreenToolStripMenuItem.Checked)
                    ShowHint(LocalizationKeys.HintCtrlAltInsFullscreenModeOff);
                else
                    ShowHint(LocalizationKeys.HintCtrlAltInsFullscreenModeOn);
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

        private void stopFullScreenModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SwitchFullScreenMode(false);
        }

        private void guacamoleConnectionConfigurationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new MainForm(this.HomeUrl, new Uri(this.GuacamoleConnectionConfigurationsUrl.ToString()));
            form.Show();
        }

        private void guacamoleUserSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new MainForm(this.HomeUrl, new Uri(this.GuacamoleSettingsUrl.ToString()));
            form.Show();
        }

        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new MainForm(this.HomeUrl, this.StartUrl);
            form.Show();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            UpdateLocationUrl();
            this.toolStripMenuItem1.Text = DateTime.Now.ToString("HH:mm:ss.fff") + " " + this._core!.DocumentTitle;
            RefreshFaviconAsync();
        }

        private Exception? formTitleRefreshTimer_Tick_Ex = null;
        private void formTitleRefreshTimer_Tick(object sender, EventArgs e)
        {
            const int postNavMinInterval = 250;
            const int maxInterval = 500;
            if (this.formTitleRefreshTimer.Interval < postNavMinInterval)
            {
                this.formTitleRefreshTimer.Interval = postNavMinInterval;
            }
            else
            {
                if (this.formTitleRefreshTimer.Interval < maxInterval)
                    this.formTitleRefreshTimer.Interval = System.Math.Min((int)(this.formTitleRefreshTimer.Interval * 5), maxInterval);
            }
            try
            {
                UpdateLocationUrl();
                SwitchMenuItemsBasedOnShownContent();
                formTitleRefreshTimer_Tick_Ex = null;
            }
            catch (Exception ex)
            {
                if (formTitleRefreshTimer_Tick_Ex == null)
                {
                    formTitleRefreshTimer_Tick_Ex = ex;
                    MessageBox.Show(this, $"Unexpected exception:\n{ex.ToString()}", "Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else // => formTitleRefreshTimer_Tick_Ex != null
                {
                    //ignore repeated exceptions
                }
            }
        }

        private enum LocalizationKeys
        {
            HintCtrlAltF4AppWillBeClosed,
            HintAltF4CatchedAndIgnored,
            HintWinRCatched_NotForwardableToRemoteServer,
            HintCtrlAltBreakFullscreenModeOff,
            HintCtrlAltInsFullscreenModeOff,
            HintCtrlAltInsFullscreenModeOn,
            HintCtrlShiftEscCatched_NotForwardableToRemoteServer,
            HintCtrlAltEndWithoutEffect_mstsc,
            FocussedAnotherControlWarning,
            Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow,
            Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow
        }

        private static string LocalizedString(LocalizationKeys key)
        {
            // German localization 
            if (System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de")
            {
                switch (key)
                {
                    case LocalizationKeys.HintCtrlAltF4AppWillBeClosed:
                        return "Strg+Alt+F4: Anwendung wird geschlossen…";
                    case LocalizationKeys.HintAltF4CatchedAndIgnored:
                        return "Alt+F4 wurde abgefangen (App bleibt offen).";
                    case LocalizationKeys.HintCtrlShiftEscCatched_NotForwardableToRemoteServer:
                        return "Strg+Umschalt+Esc wurde abgefangen (lokaler Task-Manager). Nicht an Remote weiterleitbar. Tipp: In Guacamole-Menü „Strg+Alt+Entf“ nutzen.";
                    case LocalizationKeys.HintCtrlAltEndWithoutEffect_mstsc:
                        return "Hinweis: Strg+Alt+Ende hat in diesem Setup üblicherweise keine Wirkung (mstsc-Sonderfall).";
                    case LocalizationKeys.HintWinRCatched_NotForwardableToRemoteServer:
                        return "WIN+R wurde abgefangen. Nicht an Remote weiterleitbar. Workaround: Strg+Esc drücken und dort „Ausführen“ suchen.";
                    case LocalizationKeys.HintCtrlAltBreakFullscreenModeOff:
                        return "Hinweis: Strg+Alt+Untbr >> Full Screen wurde deaktiviert";
                    case LocalizationKeys.HintCtrlAltInsFullscreenModeOff:
                        return "Hinweis: Strg+Alt+Einfügen >> Full Screen wurde deaktiviert";
                    case LocalizationKeys.HintCtrlAltInsFullscreenModeOn:
                        return "Hinweis: Strg+Alt+Einfügen >> Full Screen wurde aktiviert";
                    case LocalizationKeys.Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow:
                        return "Strg+Alt+Rollen zum Tastaturfokus freigeben";
                    case LocalizationKeys.Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow:
                        return "Strg+Alt+Rollen zum Tastaturfokus einfangen";
                    case LocalizationKeys.FocussedAnotherControlWarning:
                        return "ACHTUNG: Eingabe-Fokus liegt aktuell bei ";
                }
            }

            // Fallback: English localization 
            switch (key)
            {
                case LocalizationKeys.HintCtrlAltF4AppWillBeClosed:
                    return "Ctrl+Alt+F4: Application will be closed…";
                case LocalizationKeys.HintAltF4CatchedAndIgnored:
                    return "Alt+F4 catched (app stays open).";
                case LocalizationKeys.HintCtrlShiftEscCatched_NotForwardableToRemoteServer:
                    return "Ctrl+SHIFT+Esc catched (local Task-Manager). Not forwardable to remote. Hint: use \"Ctrl+Alt+Del\" in Guacamole menu.";
                case LocalizationKeys.HintCtrlAltEndWithoutEffect_mstsc:
                    return "Hinweis: Ctrl+Alt+End usually without effect in this environment (special behaviour of mstsc).";
                case LocalizationKeys.HintWinRCatched_NotForwardableToRemoteServer:
                    return "WIN+R catched. Not forwardable to remote. Workaround: press Ctrl+Esc and search for \"Run\".";
                case LocalizationKeys.HintCtrlAltBreakFullscreenModeOff:
                    return "Hinweis: Ctrl+Alt+Break >> Full Screen has been disabled";
                case LocalizationKeys.HintCtrlAltInsFullscreenModeOff:
                    return "Hinweis: Ctrl+Alt+Insert >> Full Screen has been disabled";
                case LocalizationKeys.HintCtrlAltInsFullscreenModeOn:
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

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            UpdateLocationUrl();
        }
    }
}