using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia;
using Avalonia.Threading;
using GuacamoleClient.Common.Localization;
using GuacamoleClient.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WebViewControl;
using Xilium.CefGlue;

namespace GuacClient
{
    public partial class MainWindow : Window
    {
        private const int RemoteShortcutHintDurationMs = 3000;
        private static bool s_browserCacheConfigured;
        private const string ProjectWebsiteUrl = "https://github.com/jochenwezel/GuacamoleClient";
        private const string ProjectIssuesUrl = "https://github.com/jochenwezel/GuacamoleClient/issues";
        private const string RdpResizeDetailsUrl = "https://github.com/jochenwezel/GuacamoleClient/blob/main/README.md#faq-known-issues-typical-trouble-shooting";
        private const string SetupGuideUrl = "https://github.com/jochenwezel/GuacamoleClient/blob/main/docs/SetupTestGuacamoleServer.md";

        private enum RemoteSpecialKeyCommand
        {
            CtrlAltDel,
            CtrlAltEnd,
            CtrlAltBackspace,
            CtrlShiftEsc,
            AltF4,
        }

        private readonly HashSet<string> _trustedHosts = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Key> _activeModifierKeys = new();
        private readonly HashSet<Key> _heldRemoteModifiers = new();
        private readonly HashSet<Key> _hookHandledKeyDowns = new();
        private readonly HashSet<Key> _hookHandledKeyUps = new();
        private readonly List<Key> _modifierOnlyChordOrder = new();
        private readonly DispatcherTimer _hintTimer = new() { Interval = TimeSpan.FromMilliseconds(RemoteShortcutHintDurationMs) };
        private IntPtr _keyboardHookHandle = IntPtr.Zero;
        private NativeKeyboardMethods.LowLevelKeyboardProc? _keyboardHookProc;

        private WebView _web = default!;
        private Menu _mainMenu = default!;
        private Border _hintOverlay = default!;
        private TextBlock _hintOverlayText = default!;
        private MenuItem _connectionMenuItem = default!;
        private MenuItem _manageServersMenuItem = default!;
        private MenuItem _connectionHomeMenuItem = default!;
        private MenuItem _guacamoleUserSettingsMenuItem = default!;
        private MenuItem _guacamoleConnectionConfigurationsMenuItem = default!;
        private Separator _connectionSeparatorTop = default!;
        private Separator _connectionSeparatorMiddle = default!;
        private Separator _connectionSeparatorBottom = default!;
        private MenuItem _newWindowMenuItem = default!;
        private MenuItem _viewMenuItem = default!;
        private MenuItem _sendKeyCombinationMenuItem = default!;
        private MenuItem _quitMenuItem = default!;
        private MenuItem _enterFullScreenMenuItem = default!;
        private MenuItem _exitFullScreenMenuItem = default!;
        private MenuItem _sendRemoteCtrlAltDelMenuItem = default!;
        private MenuItem _sendRemoteCtrlAltEndMenuItem = default!;
        private MenuItem _sendRemoteCtrlAltBackspaceMenuItem = default!;
        private MenuItem _openGuacamoleMenuMenuItem = default!;
        private MenuItem _helpMenuItem = default!;
        private MenuItem _setupGuideHelpMenuItem = default!;
        private MenuItem _rdpSessionResizeHelpMenuItem = default!;
        private MenuItem _projectWebsiteHelpMenuItem = default!;
        private MenuItem _aboutMenuItem = default!;
        private MenuItem _keyboardCaptureStatusMenuItem = default!;
        private MenuItem _keyboardHintMenuItem = default!;

        private bool _keyboardCaptureEnabled = true;
        private bool _windowsChordActive;
        private bool _windowsChordHadCombination;
        private Key _activeWindowsKey = Key.LWin;
        private bool _modifierOnlyChordHadNonModifier;
        private bool _modifierOnlyChordHandledLocally;
        private bool _guacamoleMenuShortcutActive;
        private bool _closeRequested;
        private GuacamoleSettingsManager _settingsManager = default!;
        private GuacamoleServerProfile? _activeProfile;
        private readonly Guid? _initialProfileId;
        private readonly string? _initialUrlOverride;
        private string? _temporaryCacheDirectory;

        public MainWindow()
            : this(null, null)
        {
        }

        public MainWindow(Guid? initialProfileId, string? initialUrlOverride = null)
        {
            _initialProfileId = initialProfileId;
            _initialUrlOverride = initialUrlOverride;
            _settingsManager = AvaloniaSettingsManagerFactory.LoadAsync().GetAwaiter().GetResult();
            ConfigureBrowserCacheBeforeWebViewCreation();
            InitializeComponent();

            _web = this.FindControl<WebView>("Web")!;
            _mainMenu = this.FindControl<Menu>("MainMenu")!;
            _hintOverlay = this.FindControl<Border>("HintOverlay")!;
            _hintOverlayText = this.FindControl<TextBlock>("HintOverlayText")!;
            _connectionMenuItem = this.FindControl<MenuItem>("ConnectionMenuItem")!;
            _manageServersMenuItem = this.FindControl<MenuItem>("ManageServersMenuItem")!;
            _connectionHomeMenuItem = this.FindControl<MenuItem>("ConnectionHomeMenuItem")!;
            _guacamoleUserSettingsMenuItem = this.FindControl<MenuItem>("GuacamoleUserSettingsMenuItem")!;
            _guacamoleConnectionConfigurationsMenuItem = this.FindControl<MenuItem>("GuacamoleConnectionConfigurationsMenuItem")!;
            _connectionSeparatorTop = this.FindControl<Separator>("ConnectionSeparatorTop")!;
            _connectionSeparatorMiddle = this.FindControl<Separator>("ConnectionSeparatorMiddle")!;
            _connectionSeparatorBottom = this.FindControl<Separator>("ConnectionSeparatorBottom")!;
            _newWindowMenuItem = this.FindControl<MenuItem>("NewWindowMenuItem")!;
            _viewMenuItem = this.FindControl<MenuItem>("ViewMenuItem")!;
            _sendKeyCombinationMenuItem = this.FindControl<MenuItem>("SendKeyCombinationMenuItem")!;
            _quitMenuItem = this.FindControl<MenuItem>("QuitMenuItem")!;
            _enterFullScreenMenuItem = this.FindControl<MenuItem>("EnterFullScreenMenuItem")!;
            _exitFullScreenMenuItem = this.FindControl<MenuItem>("ExitFullScreenMenuItem")!;
            _sendRemoteCtrlAltDelMenuItem = this.FindControl<MenuItem>("SendRemoteCtrlAltDelMenuItem")!;
            _sendRemoteCtrlAltEndMenuItem = this.FindControl<MenuItem>("SendRemoteCtrlAltEndMenuItem")!;
            _sendRemoteCtrlAltBackspaceMenuItem = this.FindControl<MenuItem>("SendRemoteCtrlAltBackspaceMenuItem")!;
            _openGuacamoleMenuMenuItem = this.FindControl<MenuItem>("OpenGuacamoleMenuMenuItem")!;
            _helpMenuItem = this.FindControl<MenuItem>("HelpMenuItem")!;
            _setupGuideHelpMenuItem = this.FindControl<MenuItem>("SetupGuideHelpMenuItem")!;
            _rdpSessionResizeHelpMenuItem = this.FindControl<MenuItem>("RdpSessionResizeHelpMenuItem")!;
            _projectWebsiteHelpMenuItem = this.FindControl<MenuItem>("ProjectWebsiteHelpMenuItem")!;
            _aboutMenuItem = this.FindControl<MenuItem>("AboutMenuItem")!;
            _keyboardCaptureStatusMenuItem = this.FindControl<MenuItem>("KeyboardCaptureStatusMenuItem")!;
            _keyboardHintMenuItem = this.FindControl<MenuItem>("KeyboardHintMenuItem")!;

            _web.TitleChanged += UpdateWindowTitleFromWebView;
            _web.Navigated += Web_Navigated;

            InitializeLocalization();

            _hintTimer.Tick += (_, __) => HideTransientHint();

            _manageServersMenuItem.Click += ManageServersMenuItem_Click;
            _connectionHomeMenuItem.Click += ConnectionHomeMenuItem_Click;
            _guacamoleUserSettingsMenuItem.Click += GuacamoleUserSettingsMenuItem_Click;
            _guacamoleConnectionConfigurationsMenuItem.Click += GuacamoleConnectionConfigurationsMenuItem_Click;
            _newWindowMenuItem.Click += NewWindowMenuItem_Click;
            _quitMenuItem.Click += QuitMenuItem_Click;
            _enterFullScreenMenuItem.Click += EnterFullScreenMenuItem_Click;
            _exitFullScreenMenuItem.Click += ExitFullScreenMenuItem_Click;
            _sendRemoteCtrlAltDelMenuItem.Click += SendRemoteCtrlAltDelMenuItem_Click;
            _sendRemoteCtrlAltEndMenuItem.Click += SendRemoteCtrlAltEndMenuItem_Click;
            _sendRemoteCtrlAltBackspaceMenuItem.Click += SendRemoteCtrlAltBackspaceMenuItem_Click;
            _openGuacamoleMenuMenuItem.Click += OpenGuacamoleMenuMenuItem_Click;
            _setupGuideHelpMenuItem.Click += SetupGuideHelpMenuItem_Click;
            _rdpSessionResizeHelpMenuItem.Click += RdpSessionResizeHelpMenuItem_Click;
            _projectWebsiteHelpMenuItem.Click += ProjectWebsiteHelpMenuItem_Click;
            _aboutMenuItem.Click += AboutMenuItem_Click;
            _keyboardCaptureStatusMenuItem.Click += KeyboardCaptureStatusMenuItem_Click;
            this.Opened += async (_, __) =>
            {
                await EnsureAndLoadUrlAsync();
                UpdateKeyboardHookState();
                UpdateViewMenuState();
            };
            this.Activated += (_, __) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    RefreshTrackedModifierStateFromPhysicalKeyboard();
                    UpdateKeyboardHookState();
                    if (_keyboardCaptureEnabled)
                        FocusKeyboardCaptureTarget();
                }, DispatcherPriority.Input);
            };
            this.PropertyChanged += (_, e) =>
            {
                if (e.Property == WindowStateProperty)
                    UpdateViewMenuState();
            };
            this.Deactivated += (_, __) =>
            {
                ResetTrackedKeyboardState();
                RemoveKeyboardHook();
            };
            this.Closed += (_, __) =>
            {
                RemoveKeyboardHook();
                GuacamoleBrowserCache.DeleteDirectoryIfExists(_temporaryCacheDirectory);
                if (_activeProfile?.LocalCacheEnabled == false)
                    GuacamoleBrowserCache.DeleteProfileCacheDirectory("GuacamoleClient-Avalonia", _activeProfile.Id);
            };
            this.AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel, true);
            this.AddHandler(KeyUpEvent, OnWindowKeyUp, RoutingStrategies.Tunnel, true);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeLocalization()
        {
            _connectionMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_Connection);
            _manageServersMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_OpenAnotherGuacamoleServer);
            _connectionHomeMenuItem.Header = BuildMenuHeaderWithShortcut(
                LocalizationProvider.Get(LocalizationKeys.Menu_ConnectionHome),
                LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_ConnectionHome));
            _connectionHomeMenuItem.InputGesture = null;
            _guacamoleUserSettingsMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_GuacamoleUserSettings);
            _guacamoleConnectionConfigurationsMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_GuacamoleConnectionConfigurations);
            _newWindowMenuItem.Header = BuildMenuHeaderWithShortcut(
                LocalizationProvider.Get(LocalizationKeys.Menu_NewWindow),
                LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_NewWindowToolStripMenuItem));
            _newWindowMenuItem.InputGesture = null;
            _viewMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_View);
            _sendKeyCombinationMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_SendKeyCombination);
            _quitMenuItem.Header = BuildMenuHeaderWithShortcut(
                LocalizationProvider.Get(LocalizationKeys.Menu_Quit),
                LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_QuitToolStripMenuItem));
            _quitMenuItem.InputGesture = null;
            _enterFullScreenMenuItem.Header = BuildMenuHeaderWithShortcut(
                LocalizationProvider.Get(LocalizationKeys.Menu_ViewFullScreen),
                LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_FullScreenToolStripMenuItem));
            _enterFullScreenMenuItem.InputGesture = null;
            _enterFullScreenMenuItem.ToggleType = MenuItemToggleType.CheckBox;
            _exitFullScreenMenuItem.Header = BuildMenuHeaderWithShortcut(
                LocalizationProvider.Get(LocalizationKeys.Menu_ViewWindowMode),
                LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_StopFullScreenModeToolStripMenuItem));
            _exitFullScreenMenuItem.InputGesture = null;

            _sendRemoteCtrlAltDelMenuItem.Header = BuildMenuHeaderWithShortcut(
                LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltDel),
                LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_SendCtrlAltDelToolStripMenuItem));
            _sendRemoteCtrlAltDelMenuItem.InputGesture = null;
            _sendRemoteCtrlAltEndMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltEnd);
            _sendRemoteCtrlAltBackspaceMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltBackspace);
            _openGuacamoleMenuMenuItem.Header = BuildMenuHeaderWithShortcut(
                LocalizationProvider.Get(LocalizationKeys.Menu_OpenGuacamoleMenu),
                LocalizationProvider.Get(LocalizationKeys.ShortcutKeystroke_OpenGuacamoleMenuToolStripMenuItem));
            _openGuacamoleMenuMenuItem.InputGesture = null;
            _helpMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_Help);
            _setupGuideHelpMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.AddEdit_Link_SetupGuideGuacamoleTestServer);
            _rdpSessionResizeHelpMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_HelpRdpResize);
            _projectWebsiteHelpMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Help_ProjectWebsite_Link);
            _aboutMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_About);
            _keyboardHintMenuItem.Header = string.Empty;
            _keyboardHintMenuItem.IsEnabled = false;
            UpdateKeyboardCaptureStatusUi();
            UpdateViewMenuState();
            UpdateConnectionMenuState();
        }

        private static object BuildMenuHeaderWithShortcut(string text, string shortcutText)
        {
            return new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,150"),
                Children =
                {
                    new TextBlock
                    {
                        Text = text,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = shortcutText,
                        Opacity = 0.85,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        [Grid.ColumnProperty] = 1
                    }
                }
            };
        }

        private void UpdateKeyboardCaptureStatusUi()
        {
            _keyboardCaptureStatusMenuItem.Header = LocalizationProvider.Get(
                _keyboardCaptureEnabled
                    ? LocalizationKeys.Tip_CtrlAltBackspace_StopKeyboardGrabbingOfGuacamoleWindow
                    : LocalizationKeys.Tip_CtrlAltBackspace_StartKeyboardGrabbingOfGuacamoleWindow);
        }

        private void UpdateKeyboardHintUi(string? text)
        {
            _keyboardHintMenuItem.Header = text ?? string.Empty;
            _keyboardHintMenuItem.IsVisible = !string.IsNullOrWhiteSpace(text);
        }

        private void UpdateViewMenuState()
        {
            bool isFullScreen = WindowState == WindowState.FullScreen;
            _enterFullScreenMenuItem.IsEnabled = true;
            _enterFullScreenMenuItem.IsChecked = isFullScreen;
            _exitFullScreenMenuItem.IsVisible = isFullScreen;
            _exitFullScreenMenuItem.IsEnabled = isFullScreen;
        }

        private void UpdateConnectionMenuState()
        {
            // WinForms toggles these entries based on login state and available admin URL.
            // Avalonia does not yet capture the login context from the embedded browser,
            // so we keep the items visible for now and only maintain the separator layout.
            _connectionSeparatorTop.IsVisible = _newWindowMenuItem.IsVisible;
            _connectionSeparatorMiddle.IsVisible = _guacamoleUserSettingsMenuItem.IsVisible || _guacamoleConnectionConfigurationsMenuItem.IsVisible;
            _connectionSeparatorBottom.IsVisible = _manageServersMenuItem.IsVisible;
        }

        private void UpdateKeyboardHookState()
        {
            if (!OperatingSystem.IsWindows())
                return;

            if (IsActive)
                EnsureKeyboardHookInstalled();
            else
                RemoveKeyboardHook();
        }

        private void EnsureKeyboardHookInstalled()
        {
            if (_keyboardHookHandle != IntPtr.Zero)
                return;

            _keyboardHookProc = KeyboardHookCallback;
            IntPtr moduleHandle = NativeKeyboardMethods.GetModuleHandle(null);
            _keyboardHookHandle = NativeKeyboardMethods.SetWindowsHookEx(NativeKeyboardMethods.WH_KEYBOARD_LL, _keyboardHookProc, moduleHandle, 0);
            if (_keyboardHookHandle == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to install low-level keyboard hook.");
        }

        private void RemoveKeyboardHook()
        {
            if (_keyboardHookHandle == IntPtr.Zero)
                return;

            NativeKeyboardMethods.UnhookWindowsHookEx(_keyboardHookHandle);
            _keyboardHookHandle = IntPtr.Zero;
            _keyboardHookProc = null;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsActive)
            {
                int msg = wParam.ToInt32();
                var info = Marshal.PtrToStructure<NativeKeyboardMethods.KBDLLHOOKSTRUCT>(lParam);
                Key key = MapVirtualKeyToAvaloniaKey((int)info.vkCode);

                if (key != Key.None)
                {
                    if (msg == NativeKeyboardMethods.WM_KEYDOWN || msg == NativeKeyboardMethods.WM_SYSKEYDOWN)
                    {
                        if (TryHandleHookKeyDown(key))
                        {
                            _hookHandledKeyDowns.Add(key);
                            return (IntPtr)1;
                        }
                    }
                    else if (msg == NativeKeyboardMethods.WM_KEYUP || msg == NativeKeyboardMethods.WM_SYSKEYUP)
                    {
                        if (TryHandleHookKeyUp(key))
                        {
                            _hookHandledKeyUps.Add(key);
                            return (IntPtr)1;
                        }
                    }
                }
            }

            return NativeKeyboardMethods.CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }

        private bool TryHandleHookKeyDown(Key key)
        {
            Key? controlKey = GetCurrentControlKey();
            Key? altKey = GetCurrentAltKey();
            Key? shiftKey = GetCurrentShiftKey();

            bool ctrl = controlKey.HasValue;
            bool alt = altKey.HasValue;
            bool shift = shiftKey.HasValue;
            bool hostCtrlAlt = ctrl && alt && !IsAltGrCombination();

            if (hostCtrlAlt && key == Key.F4)
            {
                Dispatcher.UIThread.Post(() => _ = CloseApplicationWithHintAsync());
                return true;
            }

            if (hostCtrlAlt && key == Key.Insert)
            {
                Dispatcher.UIThread.Post(() =>
                    SetFullScreenMode(WindowState != WindowState.FullScreen, WindowState == WindowState.FullScreen
                        ? LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff)
                        : LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn)));
                return true;
            }

            if (hostCtrlAlt && key == Key.Back)
            {
                Dispatcher.UIThread.Post(ToggleKeyboardCapture);
                return true;
            }

            if (!_keyboardCaptureEnabled)
                return false;

            if (hostCtrlAlt && key == Key.Home)
            {
                Dispatcher.UIThread.Post(GoToConnectionHome);
                return true;
            }

            if (hostCtrlAlt && key == Key.N)
            {
                Dispatcher.UIThread.Post(OpenNewWindow);
                return true;
            }

            if (hostCtrlAlt && key == Key.Pause && WindowState == WindowState.FullScreen)
            {
                Dispatcher.UIThread.Post(() =>
                    SetFullScreenMode(false, LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff)));
                return true;
            }

            if (ctrl && alt && shift && !_guacamoleMenuShortcutActive)
            {
                _guacamoleMenuShortcutActive = true;
                _modifierOnlyChordHandledLocally = true;
                Dispatcher.UIThread.Post(() => _ = SendOpenGuacamoleMenuChordSafeAsync());
                return true;
            }

            if (key is Key.LWin or Key.RWin)
            {
                _windowsChordActive = true;
                _windowsChordHadCombination = false;
                _activeWindowsKey = key;
                return true;
            }

            if (ctrl && alt && key == Key.End)
            {
                Dispatcher.UIThread.Post(() =>
                    _ = SendRemoteSpecialKeySafeAsync(
                        RemoteSpecialKeyCommand.CtrlAltDel,
                        LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltEnd_AsCtrlAltDel_Sent, FormatControlKeyName(controlKey), FormatAltKeyName(altKey)),
                        controlKey: controlKey,
                        altKey: altKey));
                return true;
            }

            if (ctrl && alt && key == Key.Delete)
            {
                Dispatcher.UIThread.Post(() =>
                    _ = SendRemoteSpecialKeySafeAsync(
                        RemoteSpecialKeyCommand.CtrlAltDel,
                        LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltDel_Sent, FormatControlKeyName(controlKey), FormatAltKeyName(altKey)),
                        controlKey: controlKey,
                        altKey: altKey));
                return true;
            }

            if (ctrl && shift && key == Key.Escape)
            {
                Dispatcher.UIThread.Post(() =>
                    _ = SendRemoteSpecialKeySafeAsync(
                        RemoteSpecialKeyCommand.CtrlShiftEsc,
                        LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlShiftEsc_Sent, FormatControlKeyName(controlKey), FormatShiftKeyName(shiftKey)),
                        controlKey: controlKey,
                        shiftKey: shiftKey));
                return true;
            }

            if (alt && !ctrl && key == Key.F4)
            {
                Dispatcher.UIThread.Post(() =>
                    _ = SendRemoteSpecialKeySafeAsync(
                        RemoteSpecialKeyCommand.AltF4,
                        LocalizationProvider.Get(LocalizationKeys.Hint_RemoteAltF4_Sent, FormatAltKeyName(altKey)),
                        altKey: altKey));
                return true;
            }

            if (alt && !ctrl && key == Key.Tab)
            {
                Dispatcher.UIThread.Post(() => _ = TriggerRemoteAltTabAsync(altKey ?? Key.LeftAlt));
                return true;
            }

            if (_windowsChordActive && !IsModifierKey(key))
            {
                _windowsChordHadCombination = true;
                Dispatcher.UIThread.Post(() => _ = TriggerRemoteWindowsCombinationAsync(key));
                return true;
            }

            return false;
        }

        private bool TryHandleHookKeyUp(Key key)
        {
            Key? controlKey = GetCurrentControlKey();
            Key? altKey = GetCurrentAltKey();
            Key? shiftKey = GetCurrentShiftKey();
            if (_guacamoleMenuShortcutActive && !(controlKey.HasValue && altKey.HasValue && shiftKey.HasValue))
                _guacamoleMenuShortcutActive = false;

            if (!_keyboardCaptureEnabled)
                return false;

            if (_windowsChordActive && key == _activeWindowsKey)
            {
                bool sendStandaloneWindows = !_windowsChordHadCombination;
                _windowsChordActive = false;
                _windowsChordHadCombination = false;
                Key releasedWindowsKey = _activeWindowsKey;
                _activeWindowsKey = Key.LWin;

                if (sendStandaloneWindows)
                    Dispatcher.UIThread.Post(() => _ = TriggerStandaloneWindowsKeyAsync(releasedWindowsKey));
                else
                    Dispatcher.UIThread.Post(() => _ = ReleaseRemoteModifierSafeAsync(releasedWindowsKey));

                return true;
            }

            return key is Key.LWin or Key.RWin;
        }

        private async Task EnsureAndLoadUrlAsync()
        {
            var initialProfile = AvaloniaSettingsManagerFactory.FindById(_settingsManager, _initialProfileId);
            var profile = initialProfile ?? _settingsManager.GetDefaultOrFirstOrNull();

            if (profile == null && !UrlInputDialog.IsValidUrl(_initialUrlOverride))
            {
                var selected = await ShowChooseServerDialogAsync().ConfigureAwait(true);
                if (selected == null)
                {
                    await MessageBoxSimple.Show(
                        this,
                        LocalizationProvider.Get(LocalizationKeys.AppStart_StartUrlRequired_Title),
                        LocalizationProvider.Get(LocalizationKeys.AppStart_StartUrlRequired_Text));
                    Close();
                    return;
                }
                profile = selected;
            }

            var url = _initialUrlOverride ?? profile?.Url;
            if (!UrlInputDialog.IsValidUrl(url))
                return;

            _activeProfile = profile;
            _trustedHosts.Clear();
            _trustedHosts.Add(new Uri(url!).Host);
            ApplyProfileAppearance();

            try
            {
                _web.Address = url!;
                UpdateWindowTitle(url!, _web.Title);
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(
                    this,
                    LocalizationProvider.Get(LocalizationKeys.AppStart_WebViewError_Title),
                    ex.Message);
                Close();
            }
        }

        private void ConfigureBrowserCacheBeforeWebViewCreation()
        {
            if (s_browserCacheConfigured)
                return;

            var profile = AvaloniaSettingsManagerFactory.FindById(_settingsManager, _initialProfileId)
                ?? _settingsManager.GetDefaultOrFirstOrNull();

            ConfigureBrowserCacheForProfile(profile);
            s_browserCacheConfigured = true;
        }

        private void ConfigureBrowserCacheForProfile(GuacamoleServerProfile? profile)
        {
            if (profile?.LocalCacheEnabled == true)
            {
                GuacamoleBrowserCache.DeleteDirectoryIfExists(_temporaryCacheDirectory);
                _temporaryCacheDirectory = null;
                GuacamoleBrowserCache.EnsureProfileCacheDirectory("GuacamoleClient-Avalonia", profile.Id);
                WebView.Settings.CachePath = GuacamoleBrowserCache.GetProfileCacheDirectory("GuacamoleClient-Avalonia", profile.Id);
                WebView.Settings.PersistCache = true;
            }
            else
            {
                _temporaryCacheDirectory ??= GuacamoleBrowserCache.CreateTemporaryCacheDirectory("GuacamoleClient-Avalonia", profile?.Id);
                WebView.Settings.CachePath = _temporaryCacheDirectory;
                WebView.Settings.PersistCache = false;
            }
        }

        private async void ManageServersMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            var selected = await ShowChooseServerDialogAsync().ConfigureAwait(true);
            if (selected == null)
                return;

            var window = new MainWindow(selected.Id, selected.Url)
            {
                Icon = this.Icon
            };
            window.Show();
        }

        private void ConnectionHomeMenuItem_Click(object? sender, RoutedEventArgs e)
            => GoToConnectionHome();

        private void GuacamoleUserSettingsMenuItem_Click(object? sender, RoutedEventArgs e)
            => OpenGuacamoleUserSettings();

        private void GuacamoleConnectionConfigurationsMenuItem_Click(object? sender, RoutedEventArgs e)
            => OpenGuacamoleConnectionConfigurations();

        private void NewWindowMenuItem_Click(object? sender, RoutedEventArgs e)
            => OpenNewWindow();

        private void QuitMenuItem_Click(object? sender, RoutedEventArgs e)
            => Close();

        private void EnterFullScreenMenuItem_Click(object? sender, RoutedEventArgs e)
            => SetFullScreenMode(WindowState != WindowState.FullScreen,
                WindowState == WindowState.FullScreen
                    ? LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff)
                    : LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn));

        private void ExitFullScreenMenuItem_Click(object? sender, RoutedEventArgs e)
            => SetFullScreenMode(false, LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff));

        private async void SendRemoteCtrlAltDelMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            await SendRemoteSpecialKeySafeAsync(
                RemoteSpecialKeyCommand.CtrlAltDel,
                LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltDel_Sent, "LCtrl", "LAlt"));
        }

        private async void SendRemoteCtrlAltEndMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            await SendRemoteSpecialKeySafeAsync(
                RemoteSpecialKeyCommand.CtrlAltEnd,
                LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltEnd_Sent, "LCtrl", "LAlt"));
        }

        private async void SendRemoteCtrlAltBackspaceMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            await SendRemoteSpecialKeySafeAsync(
                RemoteSpecialKeyCommand.CtrlAltBackspace,
                LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltBackspace_Sent, "LCtrl", "LAlt"));
        }

        private void OpenGuacamoleMenuMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(50).ConfigureAwait(true);
                _web.Focus();
                await SendOpenGuacamoleMenuChordSafeAsync().ConfigureAwait(true);
            }, DispatcherPriority.Background);
        }

        private async void RdpSessionResizeHelpMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            await MessageBoxSimple.Show(
                this,
                LocalizationProvider.Get(LocalizationKeys.Help_RdpResize_Title),
                LocalizationProvider.Get(LocalizationKeys.Help_RdpResize_Text),
                (LocalizationProvider.Get(LocalizationKeys.Help_RdpResize_Link), RdpResizeDetailsUrl));
        }

        private async void SetupGuideHelpMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            if (Uri.TryCreate(SetupGuideUrl, UriKind.Absolute, out var uri))
                await Launcher.LaunchUriAsync(uri);
        }

        private async void ProjectWebsiteHelpMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            if (Uri.TryCreate(ProjectWebsiteUrl, UriKind.Absolute, out var uri))
                await Launcher.LaunchUriAsync(uri);
        }

        private async void AboutMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            var detailsText = LocalizationProvider.Get(
                LocalizationKeys.Help_About_Text,
                "Avalonia",
                VersionUtil.InformationalVersion(),
                RuntimeInformation.FrameworkDescription,
                RuntimeInformation.OSDescription,
                RuntimeInformation.ProcessArchitecture.ToString());
            var licenseText = LocalizationProvider.Get(LocalizationKeys.Help_About_License_Text);
            var thirdPartyText = LocalizationProvider.Get(LocalizationKeys.Help_About_Avalonia_ThirdParty_Text);
            var text = string.Join("\n\n", detailsText, licenseText, thirdPartyText);

            await MessageBoxSimple.Show(
                this,
                LocalizationProvider.Get(LocalizationKeys.Help_About_Title),
                text,
                (LocalizationProvider.Get(LocalizationKeys.Help_ProjectWebsite_Link), ProjectWebsiteUrl),
                (LocalizationProvider.Get(LocalizationKeys.Help_ReportBug_Link), ProjectIssuesUrl));
        }

        private void KeyboardCaptureStatusMenuItem_Click(object? sender, RoutedEventArgs e)
            => ToggleKeyboardCapture();

        private async Task SendRemoteSpecialKeySafeAsync(RemoteSpecialKeyCommand command, string successHint, Key windowsKey = Key.LWin, Key? controlKey = null, Key? altKey = null, Key? shiftKey = null)
        {
            try
            {
                await SendRemoteSpecialKeyAsync(command, windowsKey, controlKey, altKey, shiftKey).ConfigureAwait(true);
                ShowTransientHint(successHint);
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(this, LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), ex.ToString());
            }
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (_hookHandledKeyDowns.Remove(e.Key))
            {
                e.Handled = true;
                return;
            }

            UpdateModifierState(e.Key, true);
            SyncHeldRemoteModifiersToTrackedState();

            if (TryHandleLocalKeyDown(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (_keyboardCaptureEnabled && TryHandleRemoteSpecialShortcutKeyDown(e.Key))
            {
                e.Handled = true;
                return;
            }
        }

        private void OnWindowKeyUp(object? sender, KeyEventArgs e)
        {
            if (_hookHandledKeyUps.Remove(e.Key))
            {
                e.Handled = true;
                return;
            }

            UpdateModifierState(e.Key, false);
            SyncHeldRemoteModifiersToTrackedState();

            if (_keyboardCaptureEnabled && TryHandleRemoteSpecialShortcutKeyUp(e.Key))
            {
                e.Handled = true;
                return;
            }
        }

        private bool TryHandleLocalKeyDown(Key key)
        {
            bool ctrl = GetCurrentControlKey().HasValue;
            bool alt = GetCurrentAltKey().HasValue;

            if (ctrl && alt && key == Key.Back)
            {
                ToggleKeyboardCapture();
                return true;
            }

            if (!_keyboardCaptureEnabled && ctrl && !alt && key == Key.Q)
            {
                Close();
                return true;
            }

            if (ctrl && alt && key == Key.F4 && !IsAltGrCombination())
            {
                _ = CloseApplicationWithHintAsync();
                return true;
            }

            if (ctrl && alt && key == Key.Insert)
            {
                SetFullScreenMode(WindowState != WindowState.FullScreen, WindowState == WindowState.FullScreen
                    ? LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOff)
                    : LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltIns_FullscreenModeOn));
                return true;
            }

            if (ctrl && alt && key == Key.Home)
            {
                GoToConnectionHome();
                return true;
            }

            if (ctrl && alt && key == Key.N)
            {
                OpenNewWindow();
                return true;
            }

            if (ctrl && alt && key == Key.Pause && WindowState == WindowState.FullScreen)
            {
                SetFullScreenMode(false, LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltBreak_FullscreenModeOff));
                return true;
            }

            return false;
        }

        private void SetFullScreenMode(bool enabled, string? hint = null)
        {
            WindowState = enabled ? WindowState.FullScreen : WindowState.Normal;
            UpdateViewMenuState();
            _web.Focus();
            if (!string.IsNullOrWhiteSpace(hint))
                ShowTransientHint(hint);
        }

        private void ToggleKeyboardCapture()
        {
            _keyboardCaptureEnabled = !_keyboardCaptureEnabled;
            ResetTrackedKeyboardState();
            UpdateKeyboardHookState();
            UpdateKeyboardCaptureStatusUi();
            FocusKeyboardCaptureTarget();
            ShowTransientHint(LocalizationProvider.Get(
                _keyboardCaptureEnabled
                    ? LocalizationKeys.Tip_CtrlAltBackspace_StopKeyboardGrabbingOfGuacamoleWindow
                    : LocalizationKeys.Tip_CtrlAltBackspace_StartKeyboardGrabbingOfGuacamoleWindow));
        }

        private void FocusKeyboardCaptureTarget()
        {
            Activate();
            Dispatcher.UIThread.Post(() => _web.Focus(), DispatcherPriority.Input);
            Dispatcher.UIThread.Post(() => _web.Focus(), DispatcherPriority.Background);
        }

        private void GoToConnectionHome()
        {
            string? url = GetConfiguredHomeUrl();
            if (!UrlInputDialog.IsValidUrl(url))
                return;

            _web.Address = url;
            UpdateWindowTitle(url!, _web.Title);
            _web.Focus();
        }

        private void OpenNewWindow()
        {
            string? homeUrl = GetConfiguredHomeUrl();
            if (!UrlInputDialog.IsValidUrl(homeUrl))
                return;

            var window = new MainWindow(_activeProfile?.Id, homeUrl)
            {
                Icon = this.Icon
            };
            window.Show();
        }

        private string? GetConfiguredHomeUrl()
            => _activeProfile?.Url ?? _initialUrlOverride;

        private async Task<GuacamoleServerProfile?> ShowChooseServerDialogAsync()
        {
            var dialog = new ChooseServerDialog(_settingsManager)
            {
                Icon = Icon
            };
            return await dialog.ShowDialog<GuacamoleServerProfile?>(this).ConfigureAwait(true);
        }

        private void OpenGuacamoleUserSettings()
        {
            var url = GetConfiguredHomeUrl();
            if (!UrlInputDialog.IsValidUrl(url))
                return;

            _web.Address = new Uri(new Uri(url!), "#/settings/preferences").ToString();
            UpdateWindowTitle(_web.Address, _web.Title);
            _web.Focus();
        }

        private void OpenGuacamoleConnectionConfigurations()
        {
            var url = GetConfiguredHomeUrl();
            if (!UrlInputDialog.IsValidUrl(url))
                return;

            _web.Address = new Uri(new Uri(url!), "#/settings/connections").ToString();
            UpdateWindowTitle(_web.Address, _web.Title);
            _web.Focus();
        }

        private void ApplyProfileAppearance()
        {
            if (_activeProfile == null)
                return;

            var scheme = _activeProfile.LookupColorScheme();
            Background = Brush.Parse(scheme.PrimaryColorHexValue);
            Foreground = Brush.Parse(scheme.TextColorHexValue);
            _mainMenu.Background = Brush.Parse(scheme.PrimaryColorHexValue);
            _mainMenu.Foreground = Brush.Parse(scheme.TextColorHexValue);
            Resources["ProfileMenuBackgroundBrush"] = Brush.Parse(scheme.PrimaryColorHexValue);
            Resources["ProfileMenuForegroundBrush"] = Brush.Parse(scheme.TextColorHexValue);
            Resources["ProfileMenuHoverBackgroundBrush"] = Brush.Parse(scheme.HoverBackgroundColorHexValue);
            Resources["ProfileMenuHoverForegroundBrush"] = Brush.Parse(scheme.HoverTextColorHexValue);
            Resources["ProfileMenuSelectedBackgroundBrush"] = Brush.Parse(scheme.SelectedItemBackgroundColorHexValue);
            Resources["ProfileMenuSelectedForegroundBrush"] = Brush.Parse(scheme.SelectedItemTextColorHexValue);
            TitleBarHelper.ApplyTitleBarColors(this, scheme);
            UpdateConnectionMenuState();
        }

        private void UpdateWindowTitle(string currentUrl, string? documentTitle)
        {
            if (string.IsNullOrWhiteSpace(documentTitle))
                Title = $"{currentUrl} - GuacamoleClient v{VersionUtil.InformationalVersion()}";
            else
                Title = $"{documentTitle} - {currentUrl} - GuacamoleClient v{VersionUtil.InformationalVersion()}";
        }

        private void UpdateWindowTitleFromWebView()
        {
            Dispatcher.UIThread.Post(() =>
            {
                string currentUrl = string.IsNullOrWhiteSpace(_web.Address)
                    ? GetConfiguredHomeUrl() ?? string.Empty
                    : _web.Address;
                if (!string.IsNullOrWhiteSpace(currentUrl))
                    UpdateWindowTitle(currentUrl, _web.Title);
            });
        }

        private void Web_Navigated(string url, string frameName)
        {
            Dispatcher.UIThread.Post(() => UpdateWindowTitle(url, _web.Title));
        }

        private async Task CloseApplicationWithHintAsync()
        {
            if (_closeRequested)
                return;

            _closeRequested = true;
            ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltF4_AppWillBeClosed));
            await Task.Delay(600).ConfigureAwait(true);
            Close();
        }

        private async Task SendOpenGuacamoleMenuChordSafeAsync()
        {
            try
            {
                await SendModifierOnlyChordAsync(new[] { Key.LeftCtrl, Key.LeftAlt, Key.LeftShift }).ConfigureAwait(true);
                ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_GuacamoleMenu_Toggled));
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(this, LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), ex.ToString());
            }
        }

        private bool TryHandleRemoteSpecialShortcutKeyDown(Key key)
        {
            if (!IsModifierKey(key) && _activeModifierKeys.Count > 0)
                _modifierOnlyChordHadNonModifier = true;

            Key? controlKey = GetTrackedControlKey();
            Key? altKey = GetTrackedAltKey();
            Key? shiftKey = GetTrackedShiftKey();
            bool ctrl = controlKey.HasValue;
            bool alt = altKey.HasValue;
            bool shift = shiftKey.HasValue;

            if (key is Key.LWin or Key.RWin)
            {
                _windowsChordActive = true;
                _windowsChordHadCombination = false;
                _activeWindowsKey = key;
                return true;
            }

            if (ctrl && alt && key == Key.Delete)
            {
                _ = SendRemoteSpecialKeySafeAsync(
                    RemoteSpecialKeyCommand.CtrlAltDel,
                    LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltDel_Sent, FormatControlKeyName(controlKey), FormatAltKeyName(altKey)),
                    controlKey: controlKey,
                    altKey: altKey);
                return true;
            }

            if (ctrl && alt && key == Key.End)
            {
                _ = SendRemoteSpecialKeySafeAsync(
                    RemoteSpecialKeyCommand.CtrlAltDel,
                    LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltEnd_AsCtrlAltDel_Sent, FormatControlKeyName(controlKey), FormatAltKeyName(altKey)),
                    controlKey: controlKey,
                    altKey: altKey);
                return true;
            }

            if (ctrl && shift && key == Key.Escape)
            {
                _ = SendRemoteSpecialKeySafeAsync(
                    RemoteSpecialKeyCommand.CtrlShiftEsc,
                    LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlShiftEsc_Sent, FormatControlKeyName(controlKey), FormatShiftKeyName(shiftKey)),
                    controlKey: controlKey,
                    shiftKey: shiftKey);
                return true;
            }

            if (alt && !ctrl && key == Key.F4)
            {
                _ = SendRemoteSpecialKeySafeAsync(
                    RemoteSpecialKeyCommand.AltF4,
                    LocalizationProvider.Get(LocalizationKeys.Hint_RemoteAltF4_Sent, FormatAltKeyName(altKey)),
                    altKey: altKey);
                return true;
            }

            if (alt && !ctrl && key == Key.Tab)
            {
                _ = TriggerRemoteAltTabAsync(altKey ?? Key.LeftAlt);
                return true;
            }

            if (ctrl && alt && shift && !_guacamoleMenuShortcutActive)
            {
                _guacamoleMenuShortcutActive = true;
                _modifierOnlyChordHandledLocally = true;
                _ = SendOpenGuacamoleMenuChordSafeAsync();
                return true;
            }

            if (_windowsChordActive && !IsModifierKey(key))
            {
                _windowsChordHadCombination = true;
                _ = TriggerRemoteWindowsCombinationAsync(key);
                return true;
            }

            return false;
        }

        private bool TryHandleRemoteSpecialShortcutKeyUp(Key key)
        {
            Key? controlKey = GetTrackedControlKey();
            Key? altKey = GetTrackedAltKey();
            Key? shiftKey = GetTrackedShiftKey();
            if (_guacamoleMenuShortcutActive && !(controlKey.HasValue && altKey.HasValue && shiftKey.HasValue))
                _guacamoleMenuShortcutActive = false;

            if (_windowsChordActive && key == _activeWindowsKey)
            {
                bool sendStandaloneWindows = !_windowsChordHadCombination;
                _windowsChordActive = false;
                _windowsChordHadCombination = false;
                Key releasedWindowsKey = _activeWindowsKey;
                _activeWindowsKey = Key.LWin;

                if (sendStandaloneWindows)
                {
                    _ = TriggerStandaloneWindowsKeyAsync(releasedWindowsKey);
                    return true;
                }

                _ = ReleaseRemoteModifierSafeAsync(releasedWindowsKey);
                return true;
            }

            if (_heldRemoteModifiers.Contains(key))
            {
                _ = ReleaseRemoteModifierSafeAsync(key);
                return IsModifierKey(key);
            }

            return key is Key.LWin or Key.RWin;
        }

        private async Task TriggerStandaloneWindowsKeyAsync(Key windowsKey)
        {
            try
            {
                await SendNativeKeyTapAsync(windowsKey, new[] { windowsKey }, Array.Empty<Key>()).ConfigureAwait(true);
                ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_RemoteWindowsKey_Sent, FormatWindowsKeyName(windowsKey)));
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(this, LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), ex.ToString());
            }
        }

        private async Task TriggerRemoteAltTabAsync(Key altKey)
        {
            try
            {
                await EnsureRemoteModifierHeldAsync(altKey).ConfigureAwait(true);
                await SendRemoteKeyPulseAsync(Key.Tab).ConfigureAwait(true);
                ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_RemoteAltTab_Sent, FormatAltKeyName(altKey)));
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(this, LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), ex.ToString());
            }
        }

        private async Task TriggerRemoteWindowsCombinationAsync(Key key)
        {
            try
            {
                bool sent = await TrySendRemoteWindowsCombinationAsync(_activeWindowsKey, key).ConfigureAwait(true);
                if (sent)
                    ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_RemoteWindowsCombination_Sent, FormatWindowsKeyName(_activeWindowsKey), key));
                else
                    ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_RemoteWindowsCombination_NotMapped, FormatWindowsKeyName(_activeWindowsKey), key));
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(this, LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), ex.ToString());
            }
        }

        private void ShowTransientHint(string text)
        {
            UpdateKeyboardHintUi(text);
            _hintOverlayText.Text = text;
            _hintOverlay.IsVisible = true;
            _hintTimer.Stop();
            _hintTimer.Start();
        }

        private void HideTransientHint()
        {
            _hintTimer.Stop();
            _hintOverlay.IsVisible = false;
            _hintOverlayText.Text = string.Empty;
            if (_activeModifierKeys.Count == 0)
                UpdateKeyboardHintUi(null);
        }

        private async Task SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand command, Key windowsKey = Key.LWin, Key? controlKey = null, Key? altKey = null, Key? shiftKey = null)
        {
            switch (command)
            {
                case RemoteSpecialKeyCommand.CtrlAltDel:
                    await SendNativeChordAsync(new[] { controlKey ?? Key.LeftCtrl, altKey ?? Key.LeftAlt }, Key.Delete).ConfigureAwait(false);
                    return;
                case RemoteSpecialKeyCommand.CtrlAltEnd:
                    await SendNativeChordAsync(new[] { controlKey ?? Key.LeftCtrl, altKey ?? Key.LeftAlt }, Key.End).ConfigureAwait(false);
                    return;
                case RemoteSpecialKeyCommand.CtrlAltBackspace:
                    await SendNativeChordAsync(new[] { controlKey ?? Key.LeftCtrl, altKey ?? Key.LeftAlt }, Key.Back).ConfigureAwait(false);
                    return;
                case RemoteSpecialKeyCommand.CtrlShiftEsc:
                    await SendNativeChordAsync(new[] { controlKey ?? Key.LeftCtrl, shiftKey ?? Key.LeftShift }, Key.Escape).ConfigureAwait(false);
                    return;
                case RemoteSpecialKeyCommand.AltF4:
                    await SendNativeChordAsync(new[] { altKey ?? Key.LeftAlt }, Key.F4).ConfigureAwait(false);
                    return;
                default:
                    throw new NotSupportedException($"Unsupported remote special key command: {command}");
            }
        }

        private async Task<bool> TrySendRemoteWindowsCombinationAsync(Key windowsKey, Key key)
        {
            if (MapAvaloniaKeyToVirtualKey(key) == 0)
                return false;

            await EnsureRemoteModifierHeldAsync(windowsKey).ConfigureAwait(false);
            await SendRemoteKeyPulseAsync(key).ConfigureAwait(false);
            return true;
        }

        private async Task EnsureRemoteModifierHeldAsync(Key key)
        {
            if (_heldRemoteModifiers.Contains(key))
                return;

            _heldRemoteModifiers.Add(key);
            await SendNativeKeyDownAsync(key, _heldRemoteModifiers).ConfigureAwait(false);
        }

        private async Task ReleaseRemoteModifierAsync(Key key)
        {
            if (!_heldRemoteModifiers.Contains(key))
                return;

            _heldRemoteModifiers.Remove(key);
            await SendNativeKeyUpAsync(key, _heldRemoteModifiers).ConfigureAwait(false);
        }

        private async Task ReleaseRemoteModifierSafeAsync(Key key)
        {
            try
            {
                await ReleaseRemoteModifierAsync(key).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(this, LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), ex.ToString());
            }
        }

        private async Task SendRemoteKeyPulseAsync(Key key)
        {
            if (MapAvaloniaKeyToVirtualKey(key) == 0)
                throw new NotSupportedException($"No DOM key mapping implemented for {key}.");

            await SendNativeKeyTapAsync(key, _heldRemoteModifiers, _heldRemoteModifiers).ConfigureAwait(false);
        }

        private async Task SendNativeChordAsync(IReadOnlyCollection<Key> modifiers, Key key)
        {
            foreach (Key modifier in modifiers)
                await SendNativeKeyDownAsync(modifier, modifiers).ConfigureAwait(false);

            await SendNativeKeyTapAsync(key, modifiers, modifiers).ConfigureAwait(false);

            foreach (Key modifier in modifiers.Reverse())
                await SendNativeKeyUpAsync(modifier, modifiers.Where(m => m != modifier).ToArray()).ConfigureAwait(false);
        }

        private async Task SendNativeKeyTapAsync(Key key, IReadOnlyCollection<Key> modifiersForKeyDown, IReadOnlyCollection<Key> modifiersForKeyUp)
        {
            await SendNativeKeyDownAsync(key, modifiersForKeyDown).ConfigureAwait(false);
            await SendNativeKeyCharAsync(key, modifiersForKeyDown).ConfigureAwait(false);
            await SendNativeKeyUpAsync(key, modifiersForKeyUp).ConfigureAwait(false);
        }

        private Task SendNativeKeyDownAsync(Key key, IReadOnlyCollection<Key> activeModifiers)
        {
            SendNativeKeyEvent(CreateCefKeyEvent(CefKeyEventType.RawKeyDown, key, activeModifiers));
            return Task.CompletedTask;
        }

        private Task SendNativeKeyUpAsync(Key key, IReadOnlyCollection<Key> activeModifiersAfterRelease)
        {
            SendNativeKeyEvent(CreateCefKeyEvent(CefKeyEventType.KeyUp, key, activeModifiersAfterRelease));
            return Task.CompletedTask;
        }

        private Task SendNativeKeyCharAsync(Key key, IReadOnlyCollection<Key> activeModifiers)
        {
            char? character = TryGetCharacterForKey(key, activeModifiers);
            if (!character.HasValue)
                return Task.CompletedTask;

            var keyEvent = CreateCefKeyEvent(CefKeyEventType.Char, key, activeModifiers);
            keyEvent.Character = character.Value;
            keyEvent.UnmodifiedCharacter = char.ToLowerInvariant(character.Value);
            SendNativeKeyEvent(keyEvent);
            return Task.CompletedTask;
        }

        private void SendNativeKeyEvent(CefKeyEvent keyEvent)
        {
            _web.Focus();
            object browser = GetUnderlyingBrowser()
                ?? throw new InvalidOperationException("Underlying browser is not available.");

            MethodInfo sendKeyEventMethod = browser.GetType().GetMethod(
                "SendKeyEvent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(CefKeyEvent) },
                modifiers: null)
                ?? throw new InvalidOperationException("Native SendKeyEvent(CefKeyEvent) is not available.");

            sendKeyEventMethod.Invoke(browser, new object[] { keyEvent });
        }

        private object? GetUnderlyingBrowser()
        {
            PropertyInfo? property = _web.GetType().GetProperty("UnderlyingBrowser", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetValue(_web);
        }

        private CefKeyEvent CreateCefKeyEvent(CefKeyEventType eventType, Key key, IReadOnlyCollection<Key> activeModifiers)
        {
            int virtualKey = MapAvaloniaKeyToVirtualKey(key);
            if (virtualKey == 0)
                throw new NotSupportedException($"No native key mapping implemented for {key}.");

            int nativeKeyCode = BuildNativeKeyCode(virtualKey, key, eventType);

            var keyEvent = new CefKeyEvent
            {
                EventType = eventType,
                WindowsKeyCode = virtualKey,
                NativeKeyCode = nativeKeyCode,
                Modifiers = BuildCefEventFlags(key, activeModifiers),
                FocusOnEditableField = true,
                IsSystemKey = key is Key.LeftAlt or Key.RightAlt or Key.F10 || activeModifiers.Contains(Key.LeftAlt) || activeModifiers.Contains(Key.RightAlt)
            };

            return keyEvent;
        }

        private static int BuildNativeKeyCode(int virtualKey, Key key, CefKeyEventType eventType)
        {
            uint scanCode = NativeKeyboardMethods.MapVirtualKey((uint)virtualKey, NativeKeyboardMethods.MAPVK_VK_TO_VSC);
            bool isExtended = key is Key.RightAlt or Key.RightCtrl or Key.Insert or Key.Delete or
                              Key.Home or Key.End or Key.PageUp or Key.PageDown or
                              Key.Left or Key.Right or Key.Up or Key.Down or
                              Key.LWin or Key.RWin or Key.Apps;

            int nativeKeyCode = (int)(scanCode << 16);

            if (isExtended)
                nativeKeyCode |= 1 << 24;

            if (eventType == CefKeyEventType.KeyUp)
                nativeKeyCode |= unchecked((int)0xC0000000);

            return nativeKeyCode;
        }

        private static char? TryGetCharacterForKey(Key key, IReadOnlyCollection<Key> activeModifiers)
        {
            bool ctrl = activeModifiers.Contains(Key.LeftCtrl) || activeModifiers.Contains(Key.RightCtrl);
            bool alt = activeModifiers.Contains(Key.LeftAlt) || activeModifiers.Contains(Key.RightAlt);
            bool win = activeModifiers.Contains(Key.LWin) || activeModifiers.Contains(Key.RWin);

            if (win || (ctrl && alt))
                return null;

            bool shift = activeModifiers.Contains(Key.LeftShift) || activeModifiers.Contains(Key.RightShift);

            if (key >= Key.A && key <= Key.Z)
            {
                char c = (char)('a' + (key - Key.A));
                return shift ? char.ToUpperInvariant(c) : c;
            }

            if (key >= Key.D0 && key <= Key.D9)
                return (char)('0' + (key - Key.D0));

            return key switch
            {
                Key.Space => ' ',
                _ => null,
            };
        }

        private void UpdateModifierState(Key key, bool isDown)
        {
            if (!TryNormalizeModifierKey(key, out Key normalizedKey))
                return;

            if (isDown && _activeModifierKeys.Count == 0)
            {
                _modifierOnlyChordOrder.Clear();
                _modifierOnlyChordHadNonModifier = false;
            }

            if (isDown)
            {
                _activeModifierKeys.Add(normalizedKey);
                if (!_modifierOnlyChordOrder.Contains(normalizedKey))
                    _modifierOnlyChordOrder.Add(normalizedKey);
            }
            else
            {
                _activeModifierKeys.Remove(normalizedKey);
                if (_activeModifierKeys.Count == 0)
                {
                    bool onlyWindowsKeys = _modifierOnlyChordOrder.Count > 0 && _modifierOnlyChordOrder.All(k => k is Key.LWin or Key.RWin);

                    if (_keyboardCaptureEnabled && !_modifierOnlyChordHandledLocally && !_modifierOnlyChordHadNonModifier && _modifierOnlyChordOrder.Count > 0 && !onlyWindowsKeys)
                    {
                        Key[] chord = _modifierOnlyChordOrder.ToArray();
                        _ = SendModifierOnlyChordSafeAsync(chord);
                    }

                    _modifierOnlyChordOrder.Clear();
                    _modifierOnlyChordHadNonModifier = false;
                    _modifierOnlyChordHandledLocally = false;
                    _guacamoleMenuShortcutActive = false;
                }
            }

            ShowActiveModifierHint();
        }

        private async Task SendModifierOnlyChordSafeAsync(IReadOnlyList<Key> modifierKeys)
        {
            try
            {
                await SendModifierOnlyChordAsync(modifierKeys).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(this, LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), ex.ToString());
            }
        }

        private void ShowActiveModifierHint()
        {
            if (!_keyboardCaptureEnabled)
            {
                HideTransientHint();
                return;
            }

            string text = FormatActiveModifierState();
            if (string.IsNullOrWhiteSpace(text))
            {
                HideTransientHint();
                return;
            }

            UpdateKeyboardHintUi(LocalizationProvider.Get(LocalizationKeys.Hint_ActiveModifiers, text));
        }

        private void SyncHeldRemoteModifiersToTrackedState()
        {
            if (_heldRemoteModifiers.Count == 0)
                return;

            Key[] staleKeys = _heldRemoteModifiers.Where(key => !_activeModifierKeys.Contains(key)).ToArray();
            if (staleKeys.Length == 0)
                return;

            foreach (Key staleKey in staleKeys)
                _heldRemoteModifiers.Remove(staleKey);

            _ = ReleaseStaleRemoteModifiersAsync(staleKeys);
        }

        private async Task ReleaseStaleRemoteModifiersAsync(IReadOnlyList<Key> staleKeys)
        {
            foreach (Key staleKey in staleKeys)
            {
                try
                {
                    await SendNativeKeyUpAsync(staleKey, _heldRemoteModifiers).ConfigureAwait(true);
                }
                catch
                {
                }
            }
        }

        private string FormatActiveModifierState()
        {
            if (_activeModifierKeys.Count == 0)
                return string.Empty;

            var names = new List<string>();

            bool leftCtrl = _activeModifierKeys.Contains(Key.LeftCtrl);
            bool rightCtrl = _activeModifierKeys.Contains(Key.RightCtrl);
            bool leftAlt = _activeModifierKeys.Contains(Key.LeftAlt);
            bool rightAlt = _activeModifierKeys.Contains(Key.RightAlt);

            if (rightAlt && leftCtrl && !leftAlt && !rightCtrl)
                names.Add("AltGr");
            else
            {
                if (leftCtrl) names.Add("LCtrl");
                if (rightCtrl) names.Add("RCtrl");
                if (leftAlt) names.Add("LAlt");
                if (rightAlt) names.Add("RAlt");
            }

            if (_activeModifierKeys.Contains(Key.LeftShift)) names.Add("LShift");
            if (_activeModifierKeys.Contains(Key.RightShift)) names.Add("RShift");
            if (_activeModifierKeys.Contains(Key.LWin)) names.Add("LWin");
            if (_activeModifierKeys.Contains(Key.RWin)) names.Add("RWin");

            return string.Join("+", names.Distinct());
        }

        private void ResetTrackedKeyboardState()
        {
            Key[] heldKeys = _heldRemoteModifiers.ToArray();
            _heldRemoteModifiers.Clear();
            _activeModifierKeys.Clear();
            _hookHandledKeyDowns.Clear();
            _hookHandledKeyUps.Clear();
            _modifierOnlyChordOrder.Clear();
            _modifierOnlyChordHadNonModifier = false;
            _modifierOnlyChordHandledLocally = false;
            _guacamoleMenuShortcutActive = false;
            _windowsChordActive = false;
            _windowsChordHadCombination = false;
            _activeWindowsKey = Key.LWin;
            HideTransientHint();

            if (heldKeys.Length == 0)
                return;

            _ = ReleaseAllRemoteModifiersAsync(heldKeys);
        }

        private async Task ReleaseAllRemoteModifiersAsync(IReadOnlyList<Key> heldKeys)
        {
            foreach (Key key in heldKeys)
            {
                try
                {
                    await SendNativeKeyUpAsync(key, Array.Empty<Key>()).ConfigureAwait(true);
                }
                catch
                {
                }
            }
        }

        private async Task SendModifierOnlyChordAsync(IReadOnlyList<Key> modifierKeys)
        {
            if (modifierKeys.Count == 0)
                return;

            var pressed = new HashSet<Key>();

            foreach (Key key in modifierKeys)
            {
                pressed.Add(key);
                await SendNativeKeyDownAsync(key, pressed).ConfigureAwait(false);
            }

            for (int i = modifierKeys.Count - 1; i >= 0; i--)
            {
                Key key = modifierKeys[i];
                pressed.Remove(key);
                await SendNativeKeyUpAsync(key, pressed).ConfigureAwait(false);
            }
        }

        private bool IsAltGrCombination()
        {
            bool leftCtrl = _activeModifierKeys.Contains(Key.LeftCtrl) || IsKeyCurrentlyDown(Key.LeftCtrl);
            bool rightCtrl = _activeModifierKeys.Contains(Key.RightCtrl) || IsKeyCurrentlyDown(Key.RightCtrl);
            bool leftAlt = _activeModifierKeys.Contains(Key.LeftAlt) || IsKeyCurrentlyDown(Key.LeftAlt);
            bool rightAlt = _activeModifierKeys.Contains(Key.RightAlt) || IsKeyCurrentlyDown(Key.RightAlt);
            return leftCtrl && rightAlt && !rightCtrl && !leftAlt;
        }

        private Key? GetTrackedControlKey()
            => _activeModifierKeys.Contains(Key.RightCtrl) ? Key.RightCtrl :
               _activeModifierKeys.Contains(Key.LeftCtrl) ? Key.LeftCtrl : null;

        private Key? GetTrackedAltKey()
            => _activeModifierKeys.Contains(Key.RightAlt) ? Key.RightAlt :
               _activeModifierKeys.Contains(Key.LeftAlt) ? Key.LeftAlt : null;

        private Key? GetTrackedShiftKey()
            => _activeModifierKeys.Contains(Key.RightShift) ? Key.RightShift :
               _activeModifierKeys.Contains(Key.LeftShift) ? Key.LeftShift : null;

        private Key? GetCurrentControlKey()
            => GetTrackedControlKey()
               ?? (IsKeyCurrentlyDown(Key.RightCtrl) ? Key.RightCtrl :
                   IsKeyCurrentlyDown(Key.LeftCtrl) ? Key.LeftCtrl : null);

        private Key? GetCurrentAltKey()
            => GetTrackedAltKey()
               ?? (IsKeyCurrentlyDown(Key.RightAlt) ? Key.RightAlt :
                   IsKeyCurrentlyDown(Key.LeftAlt) ? Key.LeftAlt : null);

        private Key? GetCurrentShiftKey()
            => GetTrackedShiftKey()
               ?? (IsKeyCurrentlyDown(Key.RightShift) ? Key.RightShift :
                   IsKeyCurrentlyDown(Key.LeftShift) ? Key.LeftShift : null);

        private void RefreshTrackedModifierStateFromPhysicalKeyboard()
        {
            _activeModifierKeys.Clear();

            AddModifierIfPhysicallyDown(Key.LeftCtrl);
            AddModifierIfPhysicallyDown(Key.RightCtrl);
            AddModifierIfPhysicallyDown(Key.LeftAlt);
            AddModifierIfPhysicallyDown(Key.RightAlt);
            AddModifierIfPhysicallyDown(Key.LeftShift);
            AddModifierIfPhysicallyDown(Key.RightShift);
        }

        private void AddModifierIfPhysicallyDown(Key key)
        {
            if (IsKeyCurrentlyDown(key))
                _activeModifierKeys.Add(key);
        }

        private static bool IsModifierKey(Key key)
            => key is Key.LeftCtrl or Key.RightCtrl or
                   Key.LeftAlt or Key.RightAlt or
                   Key.LeftShift or Key.RightShift or
                   Key.LWin or Key.RWin;

        private static bool TryNormalizeModifierKey(Key key, out Key normalizedKey)
        {
            normalizedKey = key switch
            {
                Key.LeftCtrl or Key.RightCtrl or
                Key.LeftAlt or Key.RightAlt or
                Key.LeftShift or Key.RightShift or
                Key.LWin or Key.RWin => key,
                _ => Key.None,
            };

            return normalizedKey != Key.None;
        }

        private static string FormatWindowsKeyName(Key key)
            => key == Key.RWin ? "RWin" : "LWin";

        private static string FormatControlKeyName(Key? key)
            => key == Key.RightCtrl ? "RCtrl" : "LCtrl";

        private static string FormatAltKeyName(Key? key)
            => key == Key.RightAlt ? "RAlt" : "LAlt";

        private static string FormatShiftKeyName(Key? key)
            => key == Key.RightShift ? "RShift" : "LShift";

        private static CefEventFlags BuildCefEventFlags(Key key, IReadOnlyCollection<Key> activeModifiers)
        {
            CefEventFlags flags = CefEventFlags.None;

            if (activeModifiers.Contains(Key.LeftCtrl) || activeModifiers.Contains(Key.RightCtrl))
                flags |= CefEventFlags.ControlDown;
            if (activeModifiers.Contains(Key.LeftAlt) || activeModifiers.Contains(Key.RightAlt))
                flags |= CefEventFlags.AltDown;
            if (activeModifiers.Contains(Key.LeftShift) || activeModifiers.Contains(Key.RightShift))
                flags |= CefEventFlags.ShiftDown;
            if (activeModifiers.Contains(Key.LWin) || activeModifiers.Contains(Key.RWin))
                flags |= CefEventFlags.CommandDown;

            if (activeModifiers.Contains(Key.LeftCtrl) && activeModifiers.Contains(Key.RightAlt) &&
                !activeModifiers.Contains(Key.RightCtrl) && !activeModifiers.Contains(Key.LeftAlt))
            {
                flags |= CefEventFlags.AltGrDown;
            }

            if (key is Key.LeftCtrl or Key.LeftAlt or Key.LeftShift or Key.LWin)
                flags |= CefEventFlags.IsLeft;
            else if (key is Key.RightCtrl or Key.RightAlt or Key.RightShift or Key.RWin)
                flags |= CefEventFlags.IsRight;

            return flags;
        }
        private static bool IsKeyCurrentlyDown(Key key)
        {
            if (!OperatingSystem.IsWindows())
                return false;

            int vk = key switch
            {
                Key.LeftCtrl => NativeKeyboardMethods.VK_LCONTROL,
                Key.RightCtrl => NativeKeyboardMethods.VK_RCONTROL,
                Key.LeftAlt => NativeKeyboardMethods.VK_LMENU,
                Key.RightAlt => NativeKeyboardMethods.VK_RMENU,
                Key.LeftShift => NativeKeyboardMethods.VK_LSHIFT,
                Key.RightShift => NativeKeyboardMethods.VK_RSHIFT,
                _ => 0,
            };

            return vk != 0 && (NativeKeyboardMethods.GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        private static Key MapVirtualKeyToAvaloniaKey(int vk)
        {
            if (vk >= 0x41 && vk <= 0x5A)
                return Key.A + (vk - 0x41);

            if (vk >= 0x30 && vk <= 0x39)
                return Key.D0 + (vk - 0x30);

            if (vk >= 0x70 && vk <= 0x7B)
                return Key.F1 + (vk - 0x70);

            return vk switch
            {
                NativeKeyboardMethods.VK_CANCEL => Key.Pause,
                NativeKeyboardMethods.VK_BACK => Key.Back,
                NativeKeyboardMethods.VK_TAB => Key.Tab,
                NativeKeyboardMethods.VK_PAUSE => Key.Pause,
                NativeKeyboardMethods.VK_ESCAPE => Key.Escape,
                NativeKeyboardMethods.VK_END => Key.End,
                NativeKeyboardMethods.VK_HOME => Key.Home,
                NativeKeyboardMethods.VK_LEFT => Key.Left,
                NativeKeyboardMethods.VK_UP => Key.Up,
                NativeKeyboardMethods.VK_RIGHT => Key.Right,
                NativeKeyboardMethods.VK_DOWN => Key.Down,
                NativeKeyboardMethods.VK_INSERT => Key.Insert,
                NativeKeyboardMethods.VK_DELETE => Key.Delete,
                NativeKeyboardMethods.VK_PRIOR => Key.PageUp,
                NativeKeyboardMethods.VK_NEXT => Key.PageDown,
                NativeKeyboardMethods.VK_APPS => Key.Apps,
                NativeKeyboardMethods.VK_LWIN => Key.LWin,
                NativeKeyboardMethods.VK_RWIN => Key.RWin,
                NativeKeyboardMethods.VK_LCONTROL => Key.LeftCtrl,
                NativeKeyboardMethods.VK_RCONTROL => Key.RightCtrl,
                NativeKeyboardMethods.VK_LMENU => Key.LeftAlt,
                NativeKeyboardMethods.VK_RMENU => Key.RightAlt,
                NativeKeyboardMethods.VK_LSHIFT => Key.LeftShift,
                NativeKeyboardMethods.VK_RSHIFT => Key.RightShift,
                _ => Key.None,
            };
        }

        private static int MapAvaloniaKeyToVirtualKey(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
                return 0x41 + (key - Key.A);

            if (key >= Key.D0 && key <= Key.D9)
                return 0x30 + (key - Key.D0);

            if (key >= Key.F1 && key <= Key.F12)
                return 0x70 + (key - Key.F1);

            return key switch
            {
                Key.Back => NativeKeyboardMethods.VK_BACK,
                Key.Tab => NativeKeyboardMethods.VK_TAB,
                Key.Pause => NativeKeyboardMethods.VK_PAUSE,
                Key.Space => NativeKeyboardMethods.VK_SPACE,
                Key.Enter => NativeKeyboardMethods.VK_RETURN,
                Key.Escape => NativeKeyboardMethods.VK_ESCAPE,
                Key.PageUp => NativeKeyboardMethods.VK_PRIOR,
                Key.PageDown => NativeKeyboardMethods.VK_NEXT,
                Key.End => NativeKeyboardMethods.VK_END,
                Key.Home => NativeKeyboardMethods.VK_HOME,
                Key.Left => NativeKeyboardMethods.VK_LEFT,
                Key.Up => NativeKeyboardMethods.VK_UP,
                Key.Right => NativeKeyboardMethods.VK_RIGHT,
                Key.Down => NativeKeyboardMethods.VK_DOWN,
                Key.Insert => NativeKeyboardMethods.VK_INSERT,
                Key.Delete => NativeKeyboardMethods.VK_DELETE,
                Key.Apps => NativeKeyboardMethods.VK_APPS,
                Key.LWin => NativeKeyboardMethods.VK_LWIN,
                Key.RWin => NativeKeyboardMethods.VK_RWIN,
                Key.LeftCtrl => NativeKeyboardMethods.VK_LCONTROL,
                Key.RightCtrl => NativeKeyboardMethods.VK_RCONTROL,
                Key.LeftAlt => NativeKeyboardMethods.VK_LMENU,
                Key.RightAlt => NativeKeyboardMethods.VK_RMENU,
                Key.LeftShift => NativeKeyboardMethods.VK_LSHIFT,
                Key.RightShift => NativeKeyboardMethods.VK_RSHIFT,
                _ => 0,
            };
        }

        private static class NativeKeyboardMethods
        {
            public const uint MAPVK_VK_TO_VSC = 0;
            public const int WH_KEYBOARD_LL = 13;
            public const int WM_KEYDOWN = 0x0100;
            public const int WM_KEYUP = 0x0101;
            public const int WM_SYSKEYDOWN = 0x0104;
            public const int WM_SYSKEYUP = 0x0105;
            public const int VK_CANCEL = 0x03;
            public const int VK_BACK = 0x08;
            public const int VK_TAB = 0x09;
            public const int VK_PAUSE = 0x13;
            public const int VK_ESCAPE = 0x1B;
            public const int VK_SPACE = 0x20;
            public const int VK_PRIOR = 0x21;
            public const int VK_NEXT = 0x22;
            public const int VK_END = 0x23;
            public const int VK_HOME = 0x24;
            public const int VK_LEFT = 0x25;
            public const int VK_UP = 0x26;
            public const int VK_RIGHT = 0x27;
            public const int VK_DOWN = 0x28;
            public const int VK_INSERT = 0x2D;
            public const int VK_DELETE = 0x2E;
            public const int VK_RETURN = 0x0D;
            public const int VK_LWIN = 0x5B;
            public const int VK_RWIN = 0x5C;
            public const int VK_APPS = 0x5D;
            public const int VK_LSHIFT = 0xA0;
            public const int VK_RSHIFT = 0xA1;
            public const int VK_LCONTROL = 0xA2;
            public const int VK_RCONTROL = 0xA3;
            public const int VK_LMENU = 0xA4;
            public const int VK_RMENU = 0xA5;

            public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

            [StructLayout(LayoutKind.Sequential)]
            public struct KBDLLHOOKSTRUCT
            {
                public uint vkCode;
                public uint scanCode;
                public uint flags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern short GetAsyncKeyState(int vKey);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint MapVirtualKey(uint uCode, uint uMapType);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string? lpModuleName);
        }
    }
}
