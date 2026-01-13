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
            viewToolStripMenuItem = new ToolStripMenuItem();
            fullScreenToolStripMenuItem = new ToolStripMenuItem();
            stopFullScreenModeToolStripMenuItem = new ToolStripMenuItem();
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
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, testToolStripMenuItem, viewToolStripMenuItem, connectionNameInFullScreenModeToolStripMenuItem, HintStopWebcontrol2FocusShortcut });
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
            connectionHomeToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Pos1 / Alt-Gr+Pos1";
            connectionHomeToolStripMenuItem.Size = new Size(330, 22);
            connectionHomeToolStripMenuItem.Text = "Connection Home";
            connectionHomeToolStripMenuItem.Click += connectionHomeToolStripMenuItem_Click;
            // 
            // newWindowToolStripMenuItem
            // 
            newWindowToolStripMenuItem.Name = "newWindowToolStripMenuItem";
            newWindowToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+N / Alt-Gr+N";
            newWindowToolStripMenuItem.Size = new Size(330, 22);
            newWindowToolStripMenuItem.Text = "New Window";
            newWindowToolStripMenuItem.Click += newWindowToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(327, 6);
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
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(327, 6);
            // 
            // openAnotherGuacamoleServerToolStripMenuItem
            // 
            openAnotherGuacamoleServerToolStripMenuItem.Name = "openAnotherGuacamoleServerToolStripMenuItem";
            openAnotherGuacamoleServerToolStripMenuItem.Size = new Size(330, 22);
            openAnotherGuacamoleServerToolStripMenuItem.Text = "Open another Guacamole server...";
            openAnotherGuacamoleServerToolStripMenuItem.Click += openAnotherGuacamoleServerToolStripMenuItem_Click;
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
            // HintStopWebcontrol2FocusShortcut
            // 
            HintStopWebcontrol2FocusShortcut.Alignment = ToolStripItemAlignment.Right;
            HintStopWebcontrol2FocusShortcut.Name = "HintStopWebcontrol2FocusShortcut";
            HintStopWebcontrol2FocusShortcut.Size = new Size(248, 20);
            HintStopWebcontrol2FocusShortcut.Text = "Ctrl+Alt+Scroll to capture/release keyboard";
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
    }
}
