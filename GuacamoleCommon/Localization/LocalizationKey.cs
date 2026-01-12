namespace GuacamoleClient.Common.Localization
{
    /// <summary>
    /// Keys used to retrieve localized UI strings.
    /// Platform-agnostic (shared by WinForms and Avalonia).
    /// </summary>
    public enum LocalizationKey
    {
        // MainForm hints / tips
        Hint_CtrlAltF4_AppWillBeClosed,
        Hint_AltF4_CatchedAndIgnored,
        Hint_WinR_Catched_NotForwardableToRemoteServer,
        Hint_CtrlAltBreak_FullscreenModeOff,
        Hint_CtrlAltIns_FullscreenModeOff,
        Hint_CtrlAltIns_FullscreenModeOn,
        Hint_CtrlShiftEsc_Catched_NotForwardableToRemoteServer,
        Hint_CtrlAltEnd_WithoutEffect_mstsc,
        FocussedAnotherControlWarning,
        Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow,
        Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow,

        // Menu
        Menu_OpenAnotherGuacamoleServer,

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
