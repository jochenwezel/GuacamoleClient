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
        Hint_RemoteWindowsKey_Sent,
        Hint_RemoteWindowsCombination_Sent,
        Hint_RemoteWindowsCombination_NotMapped,
        Hint_RemoteWindowsPause_NotReliable,
        Hint_RemoteCtrlAltDel_Sent,
        Hint_RemoteCtrlShiftEsc_Sent,
        Hint_RemoteCtrlAltEnd_Sent,
        Hint_RemoteAltF4_Sent,
        Hint_RemoteAltTab_Sent,
        Hint_ActiveModifiers,
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
        Menu_SendKeyCombination,
        Menu_ViewFullScreen,
        Menu_Quit,
        Menu_NewWindow,
        Menu_ConnectionHome,
        Menu_GuacamoleUserSettings,
        Menu_GuacamoleConnectionConfigurations,
        Menu_SendWindowsKey,
        Menu_SendCtrlAltDel,
        Menu_SendCtrlAltEnd,
        Menu_SendCtrlShiftEsc,
        Menu_SendAltF4,
        Menu_SendWinR,

        // Choose/Manage servers dialog
        ChooseServer_Column_Name,
        ChooseServer_Column_Url,
        ChooseServer_Column_Color,
        ChooseServer_Title,
        ChooseServer_Button_Open,
        ChooseServer_Button_Add,
        ChooseServer_Button_Edit,
        ChooseServer_Button_Remove,
        ChooseServer_Button_SetDefault,
        ChooseServer_Button_Close,
        ChooseServer_ConfirmRemove_Title,
        ChooseServer_ConfirmRemove_Text,

        // Add/Edit dialog
        AddEdit_ModeAddServer_Title,
        AddEdit_ModeEditServer_Title,
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
        AddEdit_Link_SetupGuideGuacamoleTestServer,

        // App start / startup error handling
        AppStart_StartupError_Title,
        AppStart_UnexpectedError_Title,
        AppStart_BackgroundError_Title,
        AppStart_StartUrlRequired_Title,
        AppStart_WebViewError_Title,
        AppStart_ErrorMessage_Text,
        AppStart_ErrorDetails_Label,
        AppStart_StartUrlRequired_Text,
        AppStart_UnexpectedErrorWillClose_Text,

        // Common buttons and labels
        /// <summary>
        /// Cancel button text used in various dialogs.
        /// </summary>
        Common_Button_Cancel,
        /// <summary>
        /// Suffix appended to default server profile names to indicate that they are the default choice.
        /// </summary>
        Common_Suffix_Default,
    }
}
