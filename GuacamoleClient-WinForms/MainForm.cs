using GuacamoleClient.Common;
using GuacamoleClient.Common.Localization;
using GuacamoleClient.RestClient;
using InfoBox;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm : Form
    {
        private const bool TEST_MENU_ENABLED = false;
        private const bool TEST_CONTROL_FOCUS_INFO_IN_FORM_TITLE = false; // effective only when enabled and with Debugger attached


        [Obsolete("For designer support only", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MainForm() : this(
            new GuacamoleClient.Common.Settings.GuacamoleSettingsManager(
                new GuacamoleClient.Common.Settings.JsonFileGuacamoleSettingsStore(GuacamoleClient.Common.Settings.GuacamoleSettingsPaths.GetSettingsFilePath("GuacamoleClient-Designer")),
                new GuacamoleClient.Common.Settings.GuacamoleSettingsDocument()),
            new GuacamoleClient.Common.Settings.GuacamoleServerProfile("https://guacamole.apache.org/", null!, "Gray", false, false),
            new Uri("https://guacamole.apache.org/"))
        { }

        private readonly GuacamoleClient.Common.Settings.GuacamoleSettingsManager _settings;
        public GuacamoleClient.Common.Settings.GuacamoleServerProfile ServerProfile { get; }

        public MainForm(GuacamoleClient.Common.Settings.GuacamoleSettingsManager settings, GuacamoleClient.Common.Settings.GuacamoleServerProfile serverProfile) : this(settings, serverProfile, new Uri(serverProfile.Url))
        { }

        public MainForm(GuacamoleClient.Common.Settings.GuacamoleSettingsManager settings, GuacamoleClient.Common.Settings.GuacamoleServerProfile serverProfile, Uri startUrl)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ServerProfile = serverProfile ?? throw new ArgumentNullException(nameof(serverProfile));
            this.HomeUrl = new Uri(serverProfile.Url);
            this.StartUrl = startUrl;
            _trustedHosts.Add(this.HomeUrl.Host);

            InitializeComponent();
            InitializeControlFocusManagementWithKeyboardCapturingHandler();
            InitializeLocalization();

            //Form title + menu customization
            this.UpdateFormTitle(startUrl);
            KeyPreview = true;
            ApplyProfileColors();
            UITools.SwitchToolStripVisibility(testToolStripMenuItem, TEST_MENU_ENABLED, false);

            //Assign commands to close timer
            _closeTimer.Tick += (_, __) => { _closeTimer.Stop(); Close(); };

            //Tooltip
            _tip = new ToolTip
            {
                IsBalloon = false,
                AutoPopDelay = 5000,
                InitialDelay = 0,
                ReshowDelay = 0,
                ShowAlways = true
            };

            //Browser control
            this.WebBrowserHostPanel!.LocationChanged += (_, __) => UpdateLocationUrl();
            this.WebBrowserHostPanel!.Resize += (_, __) => UpdateControllerBounds();
        }

        private readonly ToolTip _tip;

        /// <summary>
        /// The (usually disabled) timer to close the form
        /// </summary>
        private readonly Timer _closeTimer = new() { Interval = 1200 }; // sanftes Close nach Hinweis

        private CoreWebView2Environment? _webview2_env;
        private CoreWebView2Controller? _webview2_controller;
        private CoreWebView2? _webview2_core;

        /// <summary>
        /// The URL of guacamole when the user wants to go back to home screen and which is also used as base for addresses of connection and settings pages
        /// </summary>
        public Uri HomeUrl { get; init; }

        /// <summary>
        /// The start URL for the 1st browser navigation action on initialization
        /// </summary>
        public Uri StartUrl { get; init; }

        /// <summary>
        /// A list of trusted hosts (usually the host of HomeUrl) to allow features like clipboard access for Webview2 control
        /// </summary>
        private readonly HashSet<string> _trustedHosts = new HashSet<string>();


        /// <summary>
        /// Handles the creation of the window handle and applies custom title bar colors.
        /// </summary>
        /// <remarks>This method overrides the base implementation to customize the appearance of the
        /// window's title bar when the handle is created. It is typically called by the Windows Forms framework and
        /// should not be called directly.</remarks>
        /// <param name="e">An object that contains the event data associated with the handle creation event.</param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            TitleBarHelper.ApplyTitleBarColors(this, this.ServerProfile.LookupColorScheme());
        }

        private void ApplyProfileColors()
        {
            // existing implementation supports all relevant color assignments - now driven by profile
            mainMenuStrip!.SetMenuStripColorsRecursive(this.ServerProfile.LookupColorScheme());
        }

        /// <summary>
        /// Create a new form instance to provide another remote desktop window
        /// </summary>
        /// <remarks>This handler opens the requested URI in a new instance of the main form and prevents
        /// the WebView2 control from opening the new window itself.</remarks>
        /// <param name="sender">The source of the event, typically the WebView2 control.</param>
        /// <param name="e">A CoreWebView2NewWindowRequestedEventArgs object that contains the event data, including the requested URI.</param>
        private void NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Öffne neuen MainForm mit der Ziel-URL
            var form = new MainForm(_settings, this.ServerProfile, new Uri(e.Uri));
            form.Show();
            // Verhindere das Öffnen im aktuellen WebView
            e.Handled = true;
        }

        /// <summary>
        /// Updates the form's title based on the specified URL.
        /// </summary>
        /// <param name="currentUrl">The current URL used to determine the new form title.</param>
        public void UpdateFormTitle(Uri currentUrl) => this.UpdateFormTitle(currentUrl, String.Empty);

        /// <summary>
        /// Updates the form's title and the full screen mode menu item to reflect the current document and URL.
        /// </summary>
        /// <param name="currentUrl">The URI of the current document or page being displayed. Used as part of the form title if no document title
        /// is provided.</param>
        /// <param name="documentTitle">The title of the current document. If null or empty, the form title will use the URL instead.</param>
        public void UpdateFormTitle(Uri currentUrl, string documentTitle)
        {
            string focusWarning = String.Empty;
            if (TEST_CONTROL_FOCUS_INFO_IN_FORM_TITLE && System.Diagnostics.Debugger.IsAttached)
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

        /// <summary>
        /// The URL for the settings page of guacamole
        /// </summary>
        public Uri GuacamoleSettingsUrl
        {
            get
            {
                return new Uri(this.HomeUrl, "#/settings/preferences");
            }
        }

        /// <summary>
        /// The URL for the connections configuation page of guacamole
        /// </summary>
        /// <remarks>This URL might be available for guacamole admin users only.</remarks>
        public async Task<Uri?> GetGuacamoleConnectionsConfigurationUrlAsync()
        {
            var ctx = await this.GetLoginContextAsync().ConfigureAwait(false);
            var result = ctx?.ConnectionsConfigUri;
            if (string.IsNullOrEmpty(result))
                return null;
            return new Uri(result.ToString());
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
            _webview2_core!.PermissionRequested += CoreWebView2_PermissionRequested;
            _webview2_core!.NavigationStarting += NavigationStarting;
            _webview2_core!.NavigationCompleted += NavigationCompleted;
            _webview2_core!.NewWindowRequested += NewWindowRequested;
            _webview2_core!.FaviconChanged += (_, __) => RefreshFaviconAsync();
            RefreshFaviconAsync();
        }

        /// <summary>
        /// Refresh form title a few milliseconds after navigation completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                    ShowMessageBoxNonModal($"Unexpected exception:\n{ex.ToString()}", "Unexpected error", InformationBoxButtons.OK, InformationBoxIcon.Error);
                }
                else
                {
                    //ignore repeated exceptions
                }
                return;
            }

            //Check for login form and show menu items accordingly
            if (GuacamoleUrlAndContentChecks.ContentIsGuacamoleLoginForm(currentHtml)) // could also be solved by checking var ctx = await this.GetLoginContextAsync().ConfigureAwait(false);

            {
                UITools.SwitchToolStripVisibility(guacamoleUserSettingsToolStripMenuItem, false, false);
                UITools.SwitchToolStripVisibility(guacamoleConnectionConfigurationsToolStripMenuItem, false, false);
                UITools.SwitchToolStripVisibility(newWindowToolStripMenuItem, false, false);
            }
            else
            {
                var ctx = await this.GetLoginContextAsync().ConfigureAwait(true);
                UITools.SwitchToolStripVisibility(guacamoleUserSettingsToolStripMenuItem, true, false);
                UITools.SwitchToolStripVisibility(guacamoleConnectionConfigurationsToolStripMenuItem, !string.IsNullOrEmpty(ctx?.ConnectionsConfigUri), false);
                UITools.SwitchToolStripVisibility(newWindowToolStripMenuItem, true, false);
            }
            UITools.SwitchSeparatorLinesVisibility(fileToolStripMenuItem.DropDownItems);
        }

        /// <summary>
        /// Get the current HTML code
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetCurrentHtmlAsync()
        {
            string htmlJson = await _webview2_core!.ExecuteScriptAsync(
                "document.documentElement.outerHTML"
            );

            // ExecuteScriptAsync liefert JSON-encodierten String zurück
            return System.Text.Json.JsonSerializer.Deserialize<string>(htmlJson)!;
        }

        /// <summary>
        /// Update form title with new document URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            UpdateLocationUrl(new Uri(e.Uri));
        }

        /// <summary>
        /// Create a windows icon from a PNG file (website favicon)
        /// </summary>
        /// <param name="pngStream"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Assign document favicon to form
        /// </summary>
        private async void RefreshFaviconAsync()
        {
            try
            {
                Stream iconStream = await _webview2_core!.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
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

        /// <summary>
        /// Initialize the webview2 control and start navigation to StartUrl and focus control start grabbing keyboard input
        /// </summary>
        /// <returns></returns>
        private async Task InitWebView2Async()
        {
            _webview2_env = await CoreWebView2Environment.CreateAsync();
            _webview2_controller = await _webview2_env.CreateCoreWebView2ControllerAsync(this.WebBrowserHostPanel!.Handle);
            _webview2_controller.IsVisible = true;
            UpdateControllerBounds();

            _webview2_controller.AcceleratorKeyPressed += Controller_AcceleratorKeyPressed;

            _webview2_core = _webview2_controller.CoreWebView2;
            // Certificate errors handling (per server profile)
            _webview2_core.ServerCertificateErrorDetected += (s, e) =>
            {
                try
                {
                    if (ServerProfile.IgnoreCertificateErrors)
                    {
                        // Scope: this profile only. Restrict to same host as profile base URL.
                        var requested = new Uri(e.RequestUri);
                        if (string.Equals(requested.Host, this.HomeUrl.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            e.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
                            return;
                        }
                    }
                }
                catch
                {
                    // fallback to default
                }
                e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
            };
            _webview2_core.Settings.IsStatusBarEnabled = false;
            _webview2_core.Settings.AreDefaultContextMenusEnabled = true;
            _webview2_core.Settings.AreDevToolsEnabled = false;

            InitializeLoginRequestCapture();

            _webview2_core.Navigate(StartUrl.ToString());
            SetFocusToWebview2Control();
        }

        /// <summary>
        /// Captured details from the last successful login/token request (if observed)
        /// </summary>
        private GuacamoleClient.RestClient.UserLoginContextWithPrimaryConnectionDataSource? _lastUserLoginContext;

        /// <summary>
        /// Guard to avoid duplicate event handler registration
        /// </summary>
        private int _loginCaptureInitialized = 0;

        /// <summary>
        /// Capture login/token response JSON to extract auth token + additional metadata (e.g. availableDataSources)
        /// </summary>
        private void InitializeLoginRequestCapture()
        {
            if (_webview2_core == null) return;
            if (System.Threading.Interlocked.Exchange(ref _loginCaptureInitialized, 1) != 0) return;

            try
            {
                // Guacamole token endpoints vary by version / deployment
                // - /api/tokens
                // - /api/session/tokens
                // We capture both.
                var origin = this.HomeUrl.GetLeftPart(UriPartial.Authority);
                _webview2_core.AddWebResourceRequestedFilter($"{origin}/api/tokens*", CoreWebView2WebResourceContext.All);
                _webview2_core.AddWebResourceRequestedFilter($"{origin}/api/session/tokens*", CoreWebView2WebResourceContext.All);

                _webview2_core.WebResourceResponseReceived += WebView2_WebResourceResponseReceived;
            }
            catch
            {
                // ignore (SDK differences / failing filters should not break app)
            }
        }

        private async void WebView2_WebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            try
            {
                // only POST requests to known token endpoints
                if (!string.Equals(e.Request.Method, "POST", StringComparison.OrdinalIgnoreCase)) return;
                if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri)) return;
                if (!string.Equals(uri.Host, this.HomeUrl.Host, StringComparison.OrdinalIgnoreCase)) return;

                var path = uri.AbsolutePath;
                if (!path.EndsWith("/api/tokens", StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith("/api/session/tokens", StringComparison.OrdinalIgnoreCase))
                    return;

                var resp = e.Response;
                if (resp == null) return;

                _lastUserLoginContext = null; // reset previous context on new login attempt
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Assign required/authorized permissions e.g. for clipboard access
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Update form title with new document URL
        /// </summary>
        private void UpdateLocationUrl()
        {
            try
            {
                if (_webview2_core == null || _webview2_core.IsSuspended) return;
                this.UpdateFormTitle(new Uri(_webview2_core!.Source), _webview2_core!.DocumentTitle);
            }
            catch
            {
                //ignore (on disposing, an exception might occure but can be suppressed
            }
        }

        /// <summary>
        /// Update form title with new document URL
        /// </summary>
        /// <param name="newUri"></param>
        private void UpdateLocationUrl(Uri newUri)
        {
            if (_webview2_core == null) return;
            this.UpdateFormTitle(newUri, _webview2_core!.DocumentTitle);
        }

        /// <summary>
        /// Refresh webview's client size
        /// </summary>
        private void UpdateControllerBounds()
        {
            if (_webview2_controller == null) return;
            var r = this.WebBrowserHostPanel!.ClientRectangle;
            _webview2_controller.Bounds = new Rectangle(r.X, r.Y, r.Width, r.Height);
        }

        /// <summary>
        /// Close window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Go to guacamole's home screen with overview and start option for available configured remote server connections
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void connectionHomeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _webview2_core?.Navigate(this.HomeUrl.ToString());
            SetFocusToWebview2Control();
        }

        /// <summary>
        /// Switch to full screen mode and back to normal window state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchFullScreenMode(!fullScreenToolStripMenuItem.Checked);
        }

        private Rectangle _previousBounds;
        /// <summary>
        /// Switch to full screen mode and back to normal window state
        /// </summary>
        /// <param name="fullScreen"></param>
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
            UITools.SwitchToolStripVisibility(stopFullScreenModeToolStripMenuItem, fullScreen, false);
            UITools.SwitchToolStripVisibility(connectionNameInFullScreenModeToolStripMenuItem, fullScreen, false);
            this.connectionNameInFullScreenModeToolStripMenuItem.Enabled = false;
            this.connectionNameInFullScreenModeToolStripMenuItem.ForeColor = Color.Black;
            this.connectionNameInFullScreenModeToolStripMenuItem.Font = new Font(this.connectionNameInFullScreenModeToolStripMenuItem.Font, FontStyle.Bold);
        }

        /// <summary>
        /// Stop full screen mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopFullScreenModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SwitchFullScreenMode(false);
        }

        /// <summary>
        /// Open new window with connections configurations page of guacamole
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void guacamoleConnectionConfigurationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var urlTask = GetGuacamoleConnectionsConfigurationUrlAsync();
            Uri? url = await urlTask;
            if (url == null)
                return;
            var form = new MainForm(_settings, this.ServerProfile, new Uri(url.ToString()));
            form.Show();
        }

        /// <summary>
        /// Open new window with settings page of guacamole
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void guacamoleUserSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new MainForm(_settings, this.ServerProfile, new Uri(this.GuacamoleSettingsUrl.ToString()));
            form.Show();
        }

        /// <summary>
        /// Open new window instance
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new MainForm(_settings, this.ServerProfile, this.StartUrl);
            form.Show();
        }

        /// <summary>
        /// Open an additional window connected to another configured Guacamole server profile.
        /// Current window remains on its server.
        /// </summary>
        private void openAnotherGuacamoleServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dlg = new ChooseServerForm(_settings);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedProfile == null)
                return;

            var profile = dlg.SelectedProfile;
            var home = new Uri(profile.Url);
            var form = new MainForm(_settings, profile, home);
            form.Show();
        }

        /// <summary>
        /// A buffer field for last exception on form title refresh
        /// </summary>
        private Exception? formTitleRefreshTimer_Tick_Ex = null;

        /// <summary>
        /// Periodically refresh form's title bar to reflect latest document (address) information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                    ShowMessageBoxNonModal($"Unexpected exception:\n{ex.ToString()}", "Unexpected error", InformationBoxButtons.OK, InformationBoxIcon.Error);
                }
                else // => formTitleRefreshTimer_Tick_Ex != null
                {
                    //ignore repeated exceptions
                }
            }
        }

        public InformationBoxResult ShowMessageBoxNonModal(string text, string caption, InformationBoxButtons buttons, InformationBoxIcon icon)
        {
            return InformationBox.Show(text, title: caption, buttons: buttons, icon: icon, behavior: InformationBoxBehavior.Modeless, initialization: InformationBoxInitialization.FromScopeAndParameters, titleIcon: new InfoBox.InformationBoxTitleIcon(this.Icon), titleStyle: InformationBoxTitleIconStyle.Custom);
        }

        /// <summary>
        /// Immediately update form title on mouse click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            UpdateLocationUrl();
        }

        #region Debug helpers and code for testing and development

        /// <summary>
        /// A control's name
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private string ControlName(Control c)
        {
            if (c == null)
                return "{null}";
            else if (String.IsNullOrEmpty(c.Name))
                return "{empty}";
            else
                return c.Name;
        }

        /// <summary>
        /// Show cached login session details or other valuable keys to analyse how to check of is-logged-on-state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var loginContext = await GetLoginContextAsync();
            if (loginContext == null)
            {
                ShowMessageBoxNonModal($"No Guacamole Auth Token/Login context", "Test", InformationBoxButtons.OK, InformationBoxIcon.Information);
            }
            else
            {
                ShowMessageBoxNonModal($"Guacamole Auth Token/Login context:\n\n{loginContext.ToString()}", "Test", InformationBoxButtons.OK, InformationBoxIcon.Information);
            }
        }

        #endregion

        public async Task<GuacamoleClient.RestClient.UserLoginContextWithPrimaryConnectionDataSource?> GetLoginContextAsync()
        {
            string? token = await GetGuacamoleAuthTokenAsync().ConfigureAwait(false);
            if (token == null)
                return null;
            var lastLoginContext = _lastUserLoginContext;
            if (lastLoginContext == null || lastLoginContext.AuthToken != token)
            {
                var client = new GuacamoleApiClient(ignoreCertificateErrors: this.ServerProfile.IgnoreCertificateErrors, new TimeSpan(0, 0, 15));
                Uri baseUri = GuacamoleApiClient.NormalizeBaseUri(this.ServerProfile.Url);
                try
                {
                    lastLoginContext = await client.AuthenticateAndLookupExtendedDataAsync(baseUri, token);
                }
                catch
                {
                    lastLoginContext = null;
                }
                _lastUserLoginContext = lastLoginContext;
            }
            return lastLoginContext;
        }
        /// <summary>
        /// Capture the auth token of guacamole session to analyse how to check of is-logged-on-state
        /// </summary>
        /// <returns></returns>
        public async Task<string?> GetGuacamoleAuthTokenAsync()
        {
            var js = await _webview2_core!.ExecuteScriptAsync(@"
                (() => JSON.stringify({
                  guac_auth_token_local: localStorage.getItem('GUAC_AUTH_TOKEN')
                }))()");

            // 1) äußere JS-String-Escapes entfernen
            var unescaped = System.Text.Json.JsonSerializer.Deserialize<string>(js)!;
            using var doc = System.Text.Json.JsonDocument.Parse(unescaped);

            // 2) Token auslesen
            var raw = doc.RootElement.GetProperty("guac_auth_token_local").GetString();

            // 3) falls der Token selbst nochmal JSON-string-escaped ist: ein zweites Mal deserialisieren
            string? token = raw;
            if (!string.IsNullOrEmpty(token) && token.Length >= 2 && token[0] == '"' && token[^1] == '"')
            {
                token = System.Text.Json.JsonSerializer.Deserialize<string>(token);
            }

            // token ist jetzt ohne Zusatzquotes
            return token;
        }

        //var localStorage = await GetLocalStorageAsync();
        //localStorage.TryGetValue("GUAC_AUTH_TOKEN", out var authToken);
        /// <summary>
        /// Reads all localStorage key/value pairs from the WebView2 context.
        /// </summary>
        public async Task<IReadOnlyDictionary<string, string?>> GetLocalStorageAsync()
        {
            if (_webview2_core == null)
                throw new InvalidOperationException("WebView2 Core not initialized.");

            var jsResult = await _webview2_core.ExecuteScriptAsync(@"
                (() => {
                    const result = {};
                    for (let i = 0; i < localStorage.length; i++) {
                        const key = localStorage.key(i);
                        result[key] = localStorage.getItem(key);
                    }
                    return JSON.stringify(result);
                })()
            ").ConfigureAwait(false);

            // WebView2 liefert immer einen JSON-string-escaped string zurück
            var unescapedJson = System.Text.Json.JsonSerializer.Deserialize<string>(jsResult)
                                ?? "{}";

            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string?>>(
                unescapedJson,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false
                });

            return dict ?? new Dictionary<string, string?>();
        }
    }
}