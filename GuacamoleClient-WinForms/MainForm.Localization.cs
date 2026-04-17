using GuacamoleClient.Common.Localization;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        /// <summary>
        /// Localize UI elements
        /// </summary>
        public void InitializeLocalization()
        {
            this.openAnotherGuacamoleServerToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_OpenAnotherGuacamoleServer);
            this.fileToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_Connection);
            this.viewToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_View);
            this.sendKeyCombinationToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_SendKeyCombination);
            this.fullScreenToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_ViewFullScreen);
            this.quitToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_Quit);
            this.newWindowToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_NewWindow);
            this.connectionHomeToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_ConnectionHome);
            this.guacamoleUserSettingsToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_GuacamoleUserSettings);
            this.guacamoleConnectionConfigurationsToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_GuacamoleConnectionConfigurations);
            this.sendRemoteCtrlAltDelToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltDel);
            this.sendRemoteCtrlAltEndToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltEnd);
            this.sendRemoteCtrlAltBackspaceToolStripMenuItem.Text = LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltBackspace);
            this.sendRemoteCtrlAltDelToolStripMenuItem.ShortcutKeyDisplayString = LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_SendCtrlAltDelToolStripMenuItem);
            this.connectionHomeToolStripMenuItem.ShortcutKeyDisplayString = LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_ConnectionHome);
            this.newWindowToolStripMenuItem.ShortcutKeyDisplayString = LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_NewWindowToolStripMenuItem);
            this.quitToolStripMenuItem.ShortcutKeyDisplayString = LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_QuitToolStripMenuItem);
            this.fullScreenToolStripMenuItem.ShortcutKeyDisplayString = LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_FullScreenToolStripMenuItem);
            this.stopFullScreenModeToolStripMenuItem.ShortcutKeyDisplayString = LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_StopFullScreenModeToolStripMenuItem);
            this.HintStopWebcontrol2FocusShortcut.Text = LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_HintStopWebcontrol2FocusShortcut);
        }

        /// <summary>
        /// Show tooltip message on top of form.
        /// </summary>
        public void ShowHint(LocalizationKeys localizedString)
        {
            string text = LocalizedString(localizedString);
            try { _tip.Show(text, this, 20, 20, 5000); } catch { /*best effort*/ }
        }

        internal static string LocalizedString(LocalizationKeys key)
            => LocalizationProvider.Get(key);
    }
}
