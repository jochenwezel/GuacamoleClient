namespace GuacamoleClient.Common.Localization
{
    /// <summary>
    /// Specifies the keys used to identify localized strings for user interface hints, tips, and warnings.
    /// </summary>
    /// <remarks>These keys are typically used to retrieve localized messages related to keyboard
    /// shortcuts, application behavior, and user guidance. The enumeration values correspond to specific scenarios
    /// where user feedback or instructions may be displayed in the application.</remarks>
    public enum LocalizationKeys
    {
        // MainForm hints / tips
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
        /// the Ctrl+Alt+Backspace shortcut.
        /// </summary>
        Tip_CtrlAltBackspace_StopKeyboardGrabbingOfGuacamoleWindow,
        /// <summary>
        /// Gets a tooltip string that instructs the user to startkeyboard grabbing in the Guacamole window using
        /// the Ctrl+Alt+Backspace shortcut.
        /// </summary>
        Tip_CtrlAltBackspace_StartKeyboardGrabbingOfGuacamoleWindow,

        // Shortcut keystroke descriptions
        ShortcutKeystroke_ConnectionHome,
        ShortcutKeystroke_NewWindowToolStripMenuItem,
        ShortcutKeystroke_QuitToolStripMenuItem,
        ShortcutKeystroke_FullScreenToolStripMenuItem,
        ShortcutKeystroke_StopFullScreenModeToolStripMenuItem,
        ShortcutKeystroke_HintStopWebcontrol2FocusShortcut,

        // Menu
        Menu_OpenAnotherGuacamoleServer,
        Menu_Connection,
        Menu_View,
        Menu_ViewFullScreen,
        Menu_Quit,
        Menu_NewWindow,
        Menu_ConnectionHome,
        Menu_GuacamoleUserSettings,
        Menu_GuacamoleConnectionConfigurations,

        // Choose server dialog
        ChooseServer_Title,
        ChooseServer_Column_Name,
        ChooseServer_Column_Url,
        ChooseServer_Column_Color,
        ChooseServer_Button_OpenNewWindow,
        ChooseServer_Button_Manage,
        ChooseServer_Button_SetDefault,
        Common_Button_Cancel,

        // Manage servers dialog
        ManageServers_Title,
        ManageServers_Button_Add,
        ManageServers_Button_Edit,
        ManageServers_Button_Remove,
        ManageServers_Button_SetDefault,
        ManageServers_Button_Close,
        ManageServers_ConfirmRemove_Title,
        ManageServers_ConfirmRemove_Text,

        // Add/Edit dialog
        AddServer_Title,
        EditServer_Title,
        AddEdit_Label_ServerUrl,
        AddEdit_Label_DisplayNameOptional,
        AddEdit_Label_ColorScheme,
        AddEdit_Label_CustomColorHex,
        AddEdit_Check_IgnoreCertificateErrorsUnsafe,
        AddEdit_Button_Save,
        AddEdit_Validation_Title,
        AddEdit_Validation_ServerUrlRequired,
        AddEdit_Validation_InvalidUrlScheme,
        AddEdit_Validation_DuplicateUrl,
        AddEdit_Validation_InvalidColor,
        AddEdit_Warn_ColorAlreadyInUse_Title,
        AddEdit_Warn_ColorAlreadyInUse_Text,
        AddEdit_TestFailed_Title,
        AddEdit_TestFailed_Text,

        // Suffixes
        Common_Suffix_Default,
    }
}
