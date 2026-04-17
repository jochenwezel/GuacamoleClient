using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GuacamoleClient.Common.Localization;
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

        private enum RemoteSpecialKeyCommand
        {
            CtrlAltDel,
            CtrlAltEnd,
            CtrlAltBackspace,
            CtrlShiftEsc,
            AltF4,
        }

        private readonly IStartUrlStore _store = StartUrlStoreFactory.Create();
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
        private Border _hintOverlay = default!;
        private TextBlock _hintOverlayText = default!;
        private MenuItem _connectionMenuItem = default!;
        private MenuItem _sendKeyCombinationMenuItem = default!;
        private MenuItem _resetUrlMenuItem = default!;
        private MenuItem _quitMenuItem = default!;
        private MenuItem _sendRemoteCtrlAltDelMenuItem = default!;
        private MenuItem _sendRemoteCtrlAltEndMenuItem = default!;
        private MenuItem _sendRemoteCtrlAltBackspaceMenuItem = default!;
        private MenuItem _keyboardCaptureStatusMenuItem = default!;
        private MenuItem _keyboardHintMenuItem = default!;

        private bool _keyboardCaptureEnabled = true;
        private bool _windowsChordActive;
        private bool _windowsChordHadCombination;
        private Key _activeWindowsKey = Key.LWin;
        private bool _modifierOnlyChordHadNonModifier;

        public MainWindow()
        {
            InitializeComponent();

            _web = this.FindControl<WebView>("Web")!;
            _hintOverlay = this.FindControl<Border>("HintOverlay")!;
            _hintOverlayText = this.FindControl<TextBlock>("HintOverlayText")!;
            _connectionMenuItem = this.FindControl<MenuItem>("ConnectionMenuItem")!;
            _sendKeyCombinationMenuItem = this.FindControl<MenuItem>("SendKeyCombinationMenuItem")!;
            _resetUrlMenuItem = this.FindControl<MenuItem>("ResetUrlMenuItem")!;
            _quitMenuItem = this.FindControl<MenuItem>("QuitMenuItem")!;
            _sendRemoteCtrlAltDelMenuItem = this.FindControl<MenuItem>("SendRemoteCtrlAltDelMenuItem")!;
            _sendRemoteCtrlAltEndMenuItem = this.FindControl<MenuItem>("SendRemoteCtrlAltEndMenuItem")!;
            _sendRemoteCtrlAltBackspaceMenuItem = this.FindControl<MenuItem>("SendRemoteCtrlAltBackspaceMenuItem")!;
            _keyboardCaptureStatusMenuItem = this.FindControl<MenuItem>("KeyboardCaptureStatusMenuItem")!;
            _keyboardHintMenuItem = this.FindControl<MenuItem>("KeyboardHintMenuItem")!;

            InitializeLocalization();

            _hintTimer.Tick += (_, __) => HideTransientHint();

            _resetUrlMenuItem.Click += ResetUrlMenuItem_Click;
            _quitMenuItem.Click += QuitMenuItem_Click;
            _sendRemoteCtrlAltDelMenuItem.Click += SendRemoteCtrlAltDelMenuItem_Click;
            _sendRemoteCtrlAltEndMenuItem.Click += SendRemoteCtrlAltEndMenuItem_Click;
            _sendRemoteCtrlAltBackspaceMenuItem.Click += SendRemoteCtrlAltBackspaceMenuItem_Click;
            _keyboardCaptureStatusMenuItem.Click += KeyboardCaptureStatusMenuItem_Click;
            this.Opened += async (_, __) =>
            {
                await EnsureAndLoadUrlAsync();
                UpdateKeyboardHookState();
            };
            this.Activated += (_, __) => UpdateKeyboardHookState();
            this.Deactivated += (_, __) =>
            {
                ResetTrackedKeyboardState();
                RemoveKeyboardHook();
            };
            this.Closed += (_, __) => RemoveKeyboardHook();
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
            _sendKeyCombinationMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_SendKeyCombination);
            _resetUrlMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_OpenAnotherGuacamoleServer);
            _quitMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_Quit);

            _sendRemoteCtrlAltDelMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltDel);
            _sendRemoteCtrlAltDelMenuItem.HotKey = new KeyGesture(Key.End, KeyModifiers.Control | KeyModifiers.Alt);
            _sendRemoteCtrlAltEndMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltEnd);
            _sendRemoteCtrlAltBackspaceMenuItem.Header = LocalizationProvider.Get(LocalizationKeys.Menu_SendCtrlAltBackspace);
            _keyboardHintMenuItem.Header = string.Empty;
            _keyboardHintMenuItem.IsEnabled = false;
            UpdateKeyboardCaptureStatusUi();
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

        private void UpdateKeyboardHookState()
        {
            if (!OperatingSystem.IsWindows())
                return;

            if (_keyboardCaptureEnabled && IsActive)
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
            if (nCode >= 0 && _keyboardCaptureEnabled)
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
            Key? controlKey = IsKeyCurrentlyDown(Key.RightCtrl) ? Key.RightCtrl :
                              IsKeyCurrentlyDown(Key.LeftCtrl) ? Key.LeftCtrl : null;
            Key? altKey = IsKeyCurrentlyDown(Key.RightAlt) ? Key.RightAlt :
                          IsKeyCurrentlyDown(Key.LeftAlt) ? Key.LeftAlt : null;
            Key? shiftKey = IsKeyCurrentlyDown(Key.RightShift) ? Key.RightShift :
                            IsKeyCurrentlyDown(Key.LeftShift) ? Key.LeftShift : null;

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
            var url = _store.Load();
            if (!UrlInputDialog.IsValidUrl(url))
            {
                var dlg = new UrlInputDialog { Icon = this.Icon };
                url = await dlg.ShowDialog<string?>(this);
                if (!UrlInputDialog.IsValidUrl(url))
                {
                    await MessageBoxSimple.Show(
                        this,
                        LocalizationProvider.Get(LocalizationKeys.AppStart_StartUrlRequired_Title),
                        LocalizationProvider.Get(LocalizationKeys.AppStart_StartUrlRequired_Text));
                    Close();
                    return;
                }
                _store.Save(url!);
            }

            _trustedHosts.Clear();
            _trustedHosts.Add(new Uri(url!).Host);

            try
            {
                _web.Address = url!;
                Title = $"GuacamoleClient v{VersionUtil.InformationalVersion()} - {url}";
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

        private async Task ResetUrlAndReloadAsync()
        {
            _store.Delete();
            await EnsureAndLoadUrlAsync();
        }

        private async void ResetUrlMenuItem_Click(object? sender, RoutedEventArgs e)
            => await ResetUrlAndReloadAsync();

        private void QuitMenuItem_Click(object? sender, RoutedEventArgs e)
            => Close();

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

        private void KeyboardCaptureStatusMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            _keyboardCaptureEnabled = !_keyboardCaptureEnabled;
            ResetTrackedKeyboardState();
            UpdateKeyboardHookState();
            UpdateKeyboardCaptureStatusUi();
            _web.Focus();
            ShowTransientHint(LocalizationProvider.Get(
                _keyboardCaptureEnabled
                    ? LocalizationKeys.Tip_CtrlAltBackspace_StopKeyboardGrabbingOfGuacamoleWindow
                    : LocalizationKeys.Tip_CtrlAltBackspace_StartKeyboardGrabbingOfGuacamoleWindow));
        }

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
            bool ctrl = GetTrackedControlKey().HasValue;
            bool alt = GetTrackedAltKey().HasValue;

            if (ctrl && alt && key == Key.Back)
            {
                _keyboardCaptureEnabled = !_keyboardCaptureEnabled;
                ResetTrackedKeyboardState();
                UpdateKeyboardHookState();
                UpdateKeyboardCaptureStatusUi();
                _web.Focus();
                ShowTransientHint(LocalizationProvider.Get(
                    _keyboardCaptureEnabled
                        ? LocalizationKeys.Tip_CtrlAltBackspace_StopKeyboardGrabbingOfGuacamoleWindow
                        : LocalizationKeys.Tip_CtrlAltBackspace_StartKeyboardGrabbingOfGuacamoleWindow));
                return true;
            }

            if (!_keyboardCaptureEnabled && ctrl && !alt && key == Key.U)
            {
                _ = ResetUrlAndReloadAsync();
                return true;
            }

            if (!_keyboardCaptureEnabled && ctrl && !alt && key == Key.Q)
            {
                Close();
                return true;
            }

            if (ctrl && alt && key == Key.F4 && !IsAltGrCombination())
            {
                ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_CtrlAltF4_AppWillBeClosed));
                Close();
                return true;
            }

            return false;
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

                    if (_keyboardCaptureEnabled && !_modifierOnlyChordHadNonModifier && _modifierOnlyChordOrder.Count > 0 && !onlyWindowsKeys)
                    {
                        Key[] chord = _modifierOnlyChordOrder.ToArray();
                        _ = SendModifierOnlyChordSafeAsync(chord);
                    }

                    _modifierOnlyChordOrder.Clear();
                    _modifierOnlyChordHadNonModifier = false;
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
            bool leftCtrl = _activeModifierKeys.Contains(Key.LeftCtrl);
            bool rightCtrl = _activeModifierKeys.Contains(Key.RightCtrl);
            bool leftAlt = _activeModifierKeys.Contains(Key.LeftAlt);
            bool rightAlt = _activeModifierKeys.Contains(Key.RightAlt);
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
