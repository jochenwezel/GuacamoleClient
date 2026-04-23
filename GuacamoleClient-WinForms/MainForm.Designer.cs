using GuacamoleClient.Common;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        private CustomMenuStrip mainMenuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem quitToolStripMenuItem;
        private ToolStripMenuItem connectionHomeToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem testToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem sendKeyCombinationToolStripMenuItem;
        private ToolStripMenuItem fullScreenToolStripMenuItem;
        private Panel WebBrowserHostPanel;
        private ToolStripMenuItem stopFullScreenModeToolStripMenuItem;
        private ToolStripMenuItem guacamoleUserSettingsToolStripMenuItem;
        private ToolStripMenuItem guacamoleConnectionConfigurationsToolStripMenuItem;
        private ToolStripMenuItem newWindowToolStripMenuItem;
        private ToolStripMenuItem openAnotherGuacamoleServerToolStripMenuItem;
        private Timer formTitleRefreshTimer;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem connectionNameInFullScreenModeToolStripMenuItem;
        private ToolStripMenuItem HintStopWebcontrol2FocusShortcut;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem openGuacamoleMenuToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem setupGuideHelpToolStripMenuItem;
        private ToolStripMenuItem rdpSessionResizeHelpToolStripMenuItem;
        private ToolStripSeparator helpToolStripSeparator;
        private ToolStripMenuItem projectWebsiteHelpToolStripMenuItem;
        private ToolStripMenuItem updateWebsiteHelpToolStripMenuItem;
        private ToolStripMenuItem checkForUpdatesHelpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;

        /// <summary>
        /// Invisible Textbox for capturing focus and while focused capturing keyboard shortcuts
        /// </summary>
        private TextBox _focusSink;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }



        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
            mainMenuStrip = new CustomMenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            connectionHomeToolStripMenuItem = new ToolStripMenuItem();
            newWindowToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            guacamoleUserSettingsToolStripMenuItem = new ToolStripMenuItem();
            guacamoleConnectionConfigurationsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            openAnotherGuacamoleServerToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            quitToolStripMenuItem = new ToolStripMenuItem();
            testToolStripMenuItem = new ToolStripMenuItem();
            authorizationUserContextToolStripMenuItem = new ToolStripMenuItem();
            restApiClientRequestsLogToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            fullScreenToolStripMenuItem = new ToolStripMenuItem();
            stopFullScreenModeToolStripMenuItem = new ToolStripMenuItem();
            sendKeyCombinationToolStripMenuItem = new ToolStripMenuItem();
            sendRemoteCtrlAltDelToolStripMenuItem = new ToolStripMenuItem();
            sendRemoteCtrlAltEndToolStripMenuItem = new ToolStripMenuItem();
            sendRemoteCtrlAltBackspaceToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            openGuacamoleMenuToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            setupGuideHelpToolStripMenuItem = new ToolStripMenuItem();
            rdpSessionResizeHelpToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripSeparator = new ToolStripSeparator();
            projectWebsiteHelpToolStripMenuItem = new ToolStripMenuItem();
            updateWebsiteHelpToolStripMenuItem = new ToolStripMenuItem();
            checkForUpdatesHelpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            connectionNameInFullScreenModeToolStripMenuItem = new ToolStripMenuItem();
            HintStopWebcontrol2FocusShortcut = new ToolStripMenuItem();
            WebBrowserHostPanel = new Panel();
            formTitleRefreshTimer = new Timer(components);
            _focusSink = new TextBox();
            mainMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, testToolStripMenuItem, viewToolStripMenuItem, sendKeyCombinationToolStripMenuItem, helpToolStripMenuItem, connectionNameInFullScreenModeToolStripMenuItem, HintStopWebcontrol2FocusShortcut });
            mainMenuStrip.Location = new Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new Size(1264, 24);
            mainMenuStrip.TabIndex = 0;
            mainMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { connectionHomeToolStripMenuItem, newWindowToolStripMenuItem, toolStripSeparator3, guacamoleUserSettingsToolStripMenuItem, guacamoleConnectionConfigurationsToolStripMenuItem, toolStripSeparator2, openAnotherGuacamoleServerToolStripMenuItem, toolStripSeparator1, quitToolStripMenuItem });
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
            // newWindowToolStripMenuItem
            // 
            newWindowToolStripMenuItem.Name = "newWindowToolStripMenuItem";
            newWindowToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+N";
            newWindowToolStripMenuItem.Size = new Size(254, 22);
            newWindowToolStripMenuItem.Text = "New Window";
            newWindowToolStripMenuItem.Click += newWindowToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(251, 6);
            // 
            // guacamoleUserSettingsToolStripMenuItem
            // 
            guacamoleUserSettingsToolStripMenuItem.Name = "guacamoleUserSettingsToolStripMenuItem";
            guacamoleUserSettingsToolStripMenuItem.Size = new Size(254, 22);
            guacamoleUserSettingsToolStripMenuItem.Text = "Guacamole User Settings";
            guacamoleUserSettingsToolStripMenuItem.Click += guacamoleUserSettingsToolStripMenuItem_Click;
            // 
            // guacamoleConnectionConfigurationsToolStripMenuItem
            // 
            guacamoleConnectionConfigurationsToolStripMenuItem.Name = "guacamoleConnectionConfigurationsToolStripMenuItem";
            guacamoleConnectionConfigurationsToolStripMenuItem.Size = new Size(254, 22);
            guacamoleConnectionConfigurationsToolStripMenuItem.Text = "Connection Configurations";
            guacamoleConnectionConfigurationsToolStripMenuItem.Click += guacamoleConnectionConfigurationsToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(251, 6);
            // 
            // openAnotherGuacamoleServerToolStripMenuItem
            // 
            openAnotherGuacamoleServerToolStripMenuItem.Name = "openAnotherGuacamoleServerToolStripMenuItem";
            openAnotherGuacamoleServerToolStripMenuItem.Size = new Size(254, 22);
            openAnotherGuacamoleServerToolStripMenuItem.Text = "Open another Guacamole server...";
            openAnotherGuacamoleServerToolStripMenuItem.Click += openAnotherGuacamoleServerToolStripMenuItem_Click;
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
            testToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { authorizationUserContextToolStripMenuItem, restApiClientRequestsLogToolStripMenuItem });
            testToolStripMenuItem.Name = "testToolStripMenuItem";
            testToolStripMenuItem.Size = new Size(40, 20);
            testToolStripMenuItem.Text = "Test";
            testToolStripMenuItem.Click += testToolStripMenuItem_Click;
            // 
            // authorizationUserContextToolStripMenuItem
            // 
            authorizationUserContextToolStripMenuItem.Name = "authorizationUserContextToolStripMenuItem";
            authorizationUserContextToolStripMenuItem.Size = new Size(228, 22);
            authorizationUserContextToolStripMenuItem.Text = "Authorization & User Context";
            authorizationUserContextToolStripMenuItem.Click += authorizationUserContextToolStripMenuItem_Click;
            // 
            // restApiClientRequestsLogToolStripMenuItem
            // 
            restApiClientRequestsLogToolStripMenuItem.Name = "restApiClientRequestsLogToolStripMenuItem";
            restApiClientRequestsLogToolStripMenuItem.Size = new Size(228, 22);
            restApiClientRequestsLogToolStripMenuItem.Text = "REST API Client Requests Log";
            restApiClientRequestsLogToolStripMenuItem.Click += restApiClientRequestsLogToolStripMenuItem_Click;
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
            fullScreenToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Insert";
            fullScreenToolStripMenuItem.Size = new Size(280, 22);
            fullScreenToolStripMenuItem.Text = "Full-Screen";
            fullScreenToolStripMenuItem.Click += fullScreenToolStripMenuItem_Click;
            // 
            // stopFullScreenModeToolStripMenuItem
            // 
            stopFullScreenModeToolStripMenuItem.Name = "stopFullScreenModeToolStripMenuItem";
            stopFullScreenModeToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Break";
            stopFullScreenModeToolStripMenuItem.Size = new Size(280, 22);
            stopFullScreenModeToolStripMenuItem.Text = "Stop Full-Screen Mode";
            stopFullScreenModeToolStripMenuItem.Click += stopFullScreenModeToolStripMenuItem_Click;
            // 
            // sendKeyCombinationToolStripMenuItem
            // 
            sendKeyCombinationToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { sendRemoteCtrlAltDelToolStripMenuItem, sendRemoteCtrlAltEndToolStripMenuItem, sendRemoteCtrlAltBackspaceToolStripMenuItem, toolStripSeparator4, openGuacamoleMenuToolStripMenuItem });
            sendKeyCombinationToolStripMenuItem.Name = "sendKeyCombinationToolStripMenuItem";
            sendKeyCombinationToolStripMenuItem.Size = new Size(137, 20);
            sendKeyCombinationToolStripMenuItem.Text = "Send key combination";
            // 
            // sendRemoteCtrlAltDelToolStripMenuItem
            // 
            sendRemoteCtrlAltDelToolStripMenuItem.Name = "sendRemoteCtrlAltDelToolStripMenuItem";
            sendRemoteCtrlAltDelToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+End";
            sendRemoteCtrlAltDelToolStripMenuItem.Size = new Size(247, 22);
            sendRemoteCtrlAltDelToolStripMenuItem.Text = "Send Ctrl+Alt+Del";
            sendRemoteCtrlAltDelToolStripMenuItem.Click += sendRemoteCtrlAltDelToolStripMenuItem_Click;
            // 
            // sendRemoteCtrlAltEndToolStripMenuItem
            // 
            sendRemoteCtrlAltEndToolStripMenuItem.Name = "sendRemoteCtrlAltEndToolStripMenuItem";
            sendRemoteCtrlAltEndToolStripMenuItem.Size = new Size(247, 22);
            sendRemoteCtrlAltEndToolStripMenuItem.Text = "Send Ctrl+Alt+End";
            sendRemoteCtrlAltEndToolStripMenuItem.Click += sendRemoteCtrlAltEndToolStripMenuItem_Click;
            // 
            // sendRemoteCtrlAltBackspaceToolStripMenuItem
            // 
            sendRemoteCtrlAltBackspaceToolStripMenuItem.Name = "sendRemoteCtrlAltBackspaceToolStripMenuItem";
            sendRemoteCtrlAltBackspaceToolStripMenuItem.Size = new Size(247, 22);
            sendRemoteCtrlAltBackspaceToolStripMenuItem.Text = "Send Ctrl+Alt+Backspace";
            sendRemoteCtrlAltBackspaceToolStripMenuItem.Click += sendRemoteCtrlAltBackspaceToolStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(244, 6);
            // 
            // openGuacamoleMenuToolStripMenuItem
            // 
            openGuacamoleMenuToolStripMenuItem.Name = "openGuacamoleMenuToolStripMenuItem";
            openGuacamoleMenuToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Shift";
            openGuacamoleMenuToolStripMenuItem.Size = new Size(247, 22);
            openGuacamoleMenuToolStripMenuItem.Text = "Open Guacamole menu";
            openGuacamoleMenuToolStripMenuItem.Click += openGuacamoleMenuToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { setupGuideHelpToolStripMenuItem, rdpSessionResizeHelpToolStripMenuItem, helpToolStripSeparator, projectWebsiteHelpToolStripMenuItem, updateWebsiteHelpToolStripMenuItem, checkForUpdatesHelpToolStripMenuItem, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // setupGuideHelpToolStripMenuItem
            // 
            setupGuideHelpToolStripMenuItem.Name = "setupGuideHelpToolStripMenuItem";
            setupGuideHelpToolStripMenuItem.Size = new Size(220, 22);
            setupGuideHelpToolStripMenuItem.Text = "Setup guide for Guacamole Test Server with Docker";
            setupGuideHelpToolStripMenuItem.Click += setupGuideHelpToolStripMenuItem_Click;
            // 
            // rdpSessionResizeHelpToolStripMenuItem
            // 
            rdpSessionResizeHelpToolStripMenuItem.Name = "rdpSessionResizeHelpToolStripMenuItem";
            rdpSessionResizeHelpToolStripMenuItem.Size = new Size(220, 22);
            rdpSessionResizeHelpToolStripMenuItem.Text = "RDP session resizing";
            rdpSessionResizeHelpToolStripMenuItem.Click += rdpSessionResizeHelpToolStripMenuItem_Click;
            // 
            // helpToolStripSeparator
            // 
            helpToolStripSeparator.Name = "helpToolStripSeparator";
            helpToolStripSeparator.Size = new Size(217, 6);
            // 
            // projectWebsiteHelpToolStripMenuItem
            // 
            projectWebsiteHelpToolStripMenuItem.Name = "projectWebsiteHelpToolStripMenuItem";
            projectWebsiteHelpToolStripMenuItem.Size = new Size(220, 22);
            projectWebsiteHelpToolStripMenuItem.Text = "Project website";
            projectWebsiteHelpToolStripMenuItem.Click += projectWebsiteHelpToolStripMenuItem_Click;
            // 
            // updateWebsiteHelpToolStripMenuItem
            //
            updateWebsiteHelpToolStripMenuItem.Name = "updateWebsiteHelpToolStripMenuItem";
            updateWebsiteHelpToolStripMenuItem.Size = new Size(220, 22);
            updateWebsiteHelpToolStripMenuItem.Text = "Update website";
            updateWebsiteHelpToolStripMenuItem.Visible = false;
            updateWebsiteHelpToolStripMenuItem.Click += updateWebsiteHelpToolStripMenuItem_Click;
            //
            // checkForUpdatesHelpToolStripMenuItem
            //
            checkForUpdatesHelpToolStripMenuItem.Name = "checkForUpdatesHelpToolStripMenuItem";
            checkForUpdatesHelpToolStripMenuItem.Size = new Size(220, 22);
            checkForUpdatesHelpToolStripMenuItem.Text = "Check for Updates";
            checkForUpdatesHelpToolStripMenuItem.Click += checkForUpdatesHelpToolStripMenuItem_Click;
            //
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(220, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // connectionNameInFullScreenModeToolStripMenuItem
            // 
            connectionNameInFullScreenModeToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            connectionNameInFullScreenModeToolStripMenuItem.Name = "connectionNameInFullScreenModeToolStripMenuItem";
            connectionNameInFullScreenModeToolStripMenuItem.Size = new Size(405, 20);
            connectionNameInFullScreenModeToolStripMenuItem.Text = "ConnecticonnectionNameInFullScreenModeToolStripMenuItemonNameI";
            // 
            // HintStopWebcontrol2FocusShortcut
            // 
            HintStopWebcontrol2FocusShortcut.Alignment = ToolStripItemAlignment.Right;
            HintStopWebcontrol2FocusShortcut.Name = "HintStopWebcontrol2FocusShortcut";
            HintStopWebcontrol2FocusShortcut.Size = new Size(274, 20);
            HintStopWebcontrol2FocusShortcut.Text = "Ctrl+Alt+Backspace to capture/release keyboard";
            HintStopWebcontrol2FocusShortcut.Click += HintStopWebcontrol2FocusShortcut_Click;
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

        private ToolStripSeparator toolStripSeparator3;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem authorizationUserContextToolStripMenuItem;
        private ToolStripMenuItem restApiClientRequestsLogToolStripMenuItem;
        private ToolStripMenuItem sendRemoteCtrlAltDelToolStripMenuItem;
        private ToolStripMenuItem sendRemoteCtrlAltEndToolStripMenuItem;
        private ToolStripMenuItem sendRemoteCtrlAltBackspaceToolStripMenuItem;
    }
}
