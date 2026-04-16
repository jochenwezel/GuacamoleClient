using InfoBox;
using Microsoft.Web.WebView2.Core;
using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        private const int RemoteShortcutHintDurationMs = 3000;

        private enum RemoteSpecialKeyCommand
        {
            Windows,
            CtrlAltDel,
            CtrlAltEnd,
            CtrlShiftEsc,
            AltF4,
            AltTab,
            WinR,
            WinPause,
        }

        private IntPtr _keyboardHookHandle = IntPtr.Zero;
        private NativeKeyboardMethods.LowLevelKeyboardProc? _keyboardHookProc;
        private bool _windowsChordActive;
        private bool _windowsChordHadCombination;
        private Keys _activeWindowsKey = Keys.LWin;

        private void UpdateKeyboardHookState()
        {
            bool shouldBeActive = IsKeyboardFocusBoundToWebview2Control == KeyboardCaptureMode.GrabbingEnabled_ShowKeyboardShortcutInfo;
            if (shouldBeActive)
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
            _windowsChordActive = false;
            _windowsChordHadCombination = false;
            _activeWindowsKey = Keys.LWin;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsKeyboardFocusBoundToWebview2Control == KeyboardCaptureMode.GrabbingEnabled_ShowKeyboardShortcutInfo)
            {
                int msg = wParam.ToInt32();
                var info = Marshal.PtrToStructure<NativeKeyboardMethods.KBDLLHOOKSTRUCT>(lParam);
                Keys key = (Keys)info.vkCode;

                if (msg == NativeKeyboardMethods.WM_KEYDOWN || msg == NativeKeyboardMethods.WM_SYSKEYDOWN)
                {
                    if (TryHandleRemoteSpecialShortcutKeyDown(key))
                        return (IntPtr)1;
                }
                else if (msg == NativeKeyboardMethods.WM_KEYUP || msg == NativeKeyboardMethods.WM_SYSKEYUP)
                {
                    if (TryHandleRemoteSpecialShortcutKeyUp(key))
                        return (IntPtr)1;
                }
            }

            return NativeKeyboardMethods.CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }

        private bool TryHandleRemoteSpecialShortcutKeyDown(Keys key)
        {
            Keys? controlKey = GetActiveControlKey();
            Keys? altKey = GetActiveAltKey();
            Keys? shiftKey = GetActiveShiftKey();
            bool ctrl = controlKey.HasValue;
            bool alt = altKey.HasValue;
            bool shift = shiftKey.HasValue;

            if (ctrl && alt && key == Keys.Back)
                return false; // existing local focus release shortcut keeps current behavior

            if (key is Keys.LWin or Keys.RWin)
            {
                _windowsChordActive = true;
                _windowsChordHadCombination = false;
                _activeWindowsKey = key;
                return true;
            }

            if (ctrl && alt && key == Keys.Delete)
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlAltDel, $"Caught {FormatControlKeyName(controlKey)}+{FormatAltKeyName(altKey)}+Del and sent it to the remote session.", controlKey, altKey);

            if (ctrl && shift && key == Keys.Escape)
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlShiftEsc, $"Caught {FormatControlKeyName(controlKey)}+{FormatShiftKeyName(shiftKey)}+Esc and sent it to the remote session.", controlKey, null, shiftKey);

            if (ctrl && alt && key == Keys.End)
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlAltEnd, $"Caught {FormatControlKeyName(controlKey)}+{FormatAltKeyName(altKey)}+End and sent it to the remote session.", controlKey, altKey);

            if (alt && !ctrl && key == Keys.F4)
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.AltF4, $"Caught {FormatAltKeyName(altKey)}+F4 and sent it to the remote session.", null, altKey);

            if (alt && !ctrl && key == Keys.Tab)
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.AltTab, $"Caught {FormatAltKeyName(altKey)}+Tab and sent it to the remote session.", null, altKey);

            if (_windowsChordActive && !IsModifierKey(key))
            {
                _windowsChordHadCombination = true;
                return TriggerRemoteWindowsCombination(key);
            }

            return false;
        }

        private bool TryHandleRemoteSpecialShortcutKeyUp(Keys key)
        {
            if (_windowsChordActive && key == _activeWindowsKey)
            {
                bool sendStandaloneWindows = !_windowsChordHadCombination;
                _windowsChordActive = false;
                _windowsChordHadCombination = false;
                Keys releasedWindowsKey = _activeWindowsKey;
                _activeWindowsKey = Keys.LWin;

                if (sendStandaloneWindows)
                    return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.Windows, $"Caught {FormatWindowsKeyName(releasedWindowsKey)} and sent it to the remote session.");

                return true;
            }

            return key is Keys.LWin or Keys.RWin;
        }

        private bool TriggerRemoteSpecialKey(RemoteSpecialKeyCommand command, string tooltipText, Keys? controlKey = null, Keys? altKey = null, Keys? shiftKey = null)
        {
            Keys windowsKey = _activeWindowsKey;
            BeginInvoke(async () =>
            {
                try
                {
                    await SendRemoteSpecialKeyAsync(command, windowsKey, controlKey, altKey, shiftKey).ConfigureAwait(true);
                    ShowTransientHint(tooltipText);
                }
                catch (Exception ex)
                {
                    ShowMessageBoxNonModal(ex.ToString(), "Remote special key", InformationBoxButtons.OK, InformationBoxIcon.Error);
                }
            });
            return true;
        }

        private bool TriggerRemoteWindowsCombination(Keys key)
        {
            Keys windowsKey = _activeWindowsKey;
            BeginInvoke(async () =>
            {
                try
                {
                    if (key == Keys.Pause)
                    {
                        await SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand.WinPause, windowsKey).ConfigureAwait(true);
                        ShowTransientHint($"Caught {FormatWindowsKeyName(windowsKey)}+Pause. The remote Pause/Break mapping is currently not reliable and may have no effect.");
                    }
                    else
                    {
                        bool sent = await TrySendRemoteWindowsCombinationAsync(windowsKey, key).ConfigureAwait(true);
                        if (sent)
                            ShowTransientHint($"Caught {FormatWindowsKeyName(windowsKey)}+{key} and sent it to the remote session.");
                        else
                            ShowTransientHint($"Caught {FormatWindowsKeyName(windowsKey)}+{key}, but this key is currently not mapped for DOM forwarding.");
                    }
                }
                catch (Exception ex)
                {
                    ShowMessageBoxNonModal(ex.ToString(), "Remote Windows combination", InformationBoxButtons.OK, InformationBoxIcon.Error);
                }
            });
            return true;
        }

        private bool ShouldRouteSpecialKeysToRemote()
            => IsKeyboardFocusBoundToWebview2Control == KeyboardCaptureMode.GrabbingEnabled_ShowKeyboardShortcutInfo;

        private void ShowTransientHint(string text)
        {
            try { _tip.Show(text, this, 20, 20, RemoteShortcutHintDurationMs); } catch { }
        }

        private async Task SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand command, Keys windowsKey = Keys.LWin, Keys? controlKey = null, Keys? altKey = null, Keys? shiftKey = null)
        {
            switch (command)
            {
                case RemoteSpecialKeyCommand.Windows:
                    var meta = MapWindowsKeyDomDefinition(windowsKey);
                    await DispatchSyntheticKeyboardSequenceAsync(
                        $$"""{"type":"keydown","key":"Meta","code":"{{meta.code}}","metaKey":true,"location":{{meta.location}}}""",
                        $$"""{"type":"keyup","key":"Meta","code":"{{meta.code}}","metaKey":false,"location":{{meta.location}}}"""
                    );
                    return;
                case RemoteSpecialKeyCommand.CtrlAltDel:
                    var ctrlForDel = MapControlKeyDomDefinition(controlKey ?? Keys.LControlKey);
                    var altForDel = MapAltKeyDomDefinition(altKey ?? Keys.LMenu);
                    await DispatchSyntheticKeyboardSequenceAsync(
                        $$"""{"type":"keydown","key":"Control","code":"{{ctrlForDel.code}}","ctrlKey":true,"location":{{ctrlForDel.location}}}""",
                        $$"""{"type":"keydown","key":"Alt","code":"{{altForDel.code}}","ctrlKey":true,"altKey":true,"location":{{altForDel.location}}}""",
                        """{ "type": "keydown", "key": "Delete", "code": "Delete", "ctrlKey": true, "altKey": true }""",
                        """{ "type": "keyup", "key": "Delete", "code": "Delete", "ctrlKey": true, "altKey": true }""",
                        $$"""{"type":"keyup","key":"Alt","code":"{{altForDel.code}}","ctrlKey":true,"altKey":false,"location":{{altForDel.location}}}""",
                        $$"""{"type":"keyup","key":"Control","code":"{{ctrlForDel.code}}","ctrlKey":false,"location":{{ctrlForDel.location}}}"""
                    );
                    return;
                case RemoteSpecialKeyCommand.CtrlAltEnd:
                    var ctrlForEnd = MapControlKeyDomDefinition(controlKey ?? Keys.LControlKey);
                    var altForEnd = MapAltKeyDomDefinition(altKey ?? Keys.LMenu);
                    await DispatchSyntheticKeyboardSequenceAsync(
                        $$"""{"type":"keydown","key":"Control","code":"{{ctrlForEnd.code}}","ctrlKey":true,"location":{{ctrlForEnd.location}}}""",
                        $$"""{"type":"keydown","key":"Alt","code":"{{altForEnd.code}}","ctrlKey":true,"altKey":true,"location":{{altForEnd.location}}}""",
                        """{ "type": "keydown", "key": "End", "code": "End", "ctrlKey": true, "altKey": true }""",
                        """{ "type": "keyup", "key": "End", "code": "End", "ctrlKey": true, "altKey": true }""",
                        $$"""{"type":"keyup","key":"Alt","code":"{{altForEnd.code}}","ctrlKey":true,"altKey":false,"location":{{altForEnd.location}}}""",
                        $$"""{"type":"keyup","key":"Control","code":"{{ctrlForEnd.code}}","ctrlKey":false,"location":{{ctrlForEnd.location}}}"""
                    );
                    return;
                case RemoteSpecialKeyCommand.CtrlShiftEsc:
                    var ctrlForEsc = MapControlKeyDomDefinition(controlKey ?? Keys.LControlKey);
                    var shiftForEsc = MapShiftKeyDomDefinition(shiftKey ?? Keys.LShiftKey);
                    await DispatchSyntheticKeyboardSequenceAsync(
                        $$"""{"type":"keydown","key":"Control","code":"{{ctrlForEsc.code}}","ctrlKey":true,"location":{{ctrlForEsc.location}}}""",
                        $$"""{"type":"keydown","key":"Shift","code":"{{shiftForEsc.code}}","ctrlKey":true,"shiftKey":true,"location":{{shiftForEsc.location}}}""",
                        """{ "type": "keydown", "key": "Escape", "code": "Escape", "ctrlKey": true, "shiftKey": true }""",
                        """{ "type": "keyup", "key": "Escape", "code": "Escape", "ctrlKey": true, "shiftKey": true }""",
                        $$"""{"type":"keyup","key":"Shift","code":"{{shiftForEsc.code}}","ctrlKey":true,"shiftKey":false,"location":{{shiftForEsc.location}}}""",
                        $$"""{"type":"keyup","key":"Control","code":"{{ctrlForEsc.code}}","ctrlKey":false,"location":{{ctrlForEsc.location}}}"""
                    );
                    return;
                case RemoteSpecialKeyCommand.AltF4:
                    var altForF4 = MapAltKeyDomDefinition(altKey ?? Keys.LMenu);
                    await DispatchSyntheticKeyboardSequenceAsync(
                        $$"""{"type":"keydown","key":"Alt","code":"{{altForF4.code}}","altKey":true,"location":{{altForF4.location}}}""",
                        """{ "type": "keydown", "key": "F4", "code": "F4", "altKey": true }""",
                        """{ "type": "keyup", "key": "F4", "code": "F4", "altKey": true }""",
                        $$"""{"type":"keyup","key":"Alt","code":"{{altForF4.code}}","altKey":false,"location":{{altForF4.location}}}"""
                    );
                    return;
                case RemoteSpecialKeyCommand.AltTab:
                    var altForTab = MapAltKeyDomDefinition(altKey ?? Keys.LMenu);
                    await DispatchSyntheticKeyboardSequenceAsync(
                        $$"""{"type":"keydown","key":"Alt","code":"{{altForTab.code}}","altKey":true,"location":{{altForTab.location}}}""",
                        """{ "type": "keydown", "key": "Tab", "code": "Tab", "altKey": true }""",
                        """{ "type": "keyup", "key": "Tab", "code": "Tab", "altKey": true }""",
                        $$"""{"type":"keyup","key":"Alt","code":"{{altForTab.code}}","altKey":false,"location":{{altForTab.location}}}"""
                    );
                    return;
                case RemoteSpecialKeyCommand.WinR:
                    await TrySendRemoteWindowsCombinationAsync(windowsKey, Keys.R);
                    return;
                case RemoteSpecialKeyCommand.WinPause:
                    await SendRemoteWindowsPauseTestSequenceAsync(windowsKey);
                    return;
                default:
                    throw new NotSupportedException($"Unsupported remote special key command: {command}");
            }
        }

        private async Task<bool> TrySendRemoteWindowsCombinationAsync(Keys windowsKey, Keys key)
        {
            var mapped = MapKeyToDomDefinition(key);
            if (mapped == null)
                return false;

            var meta = MapWindowsKeyDomDefinition(windowsKey);
            await DispatchSyntheticKeyboardSequenceAsync(
                $$"""{"type":"keydown","key":"Meta","code":"{{meta.code}}","metaKey":true,"location":{{meta.location}}}""",
                $$"""{"type":"keydown","key":"{{mapped.Value.key}}","code":"{{mapped.Value.code}}","metaKey":true}""",
                $$"""{"type":"keyup","key":"{{mapped.Value.key}}","code":"{{mapped.Value.code}}","metaKey":true}""",
                $$"""{"type":"keyup","key":"Meta","code":"{{meta.code}}","metaKey":false,"location":{{meta.location}}}"""
            );
            return true;
        }

        private static (string code, int location) MapWindowsKeyDomDefinition(Keys key)
            => key == Keys.RWin ? ("MetaRight", 2) : ("MetaLeft", 1);

        private static (string code, int location) MapControlKeyDomDefinition(Keys key)
            => key == Keys.RControlKey ? ("ControlRight", 2) : ("ControlLeft", 1);

        private static (string code, int location) MapAltKeyDomDefinition(Keys key)
            => key == Keys.RMenu ? ("AltRight", 2) : ("AltLeft", 1);

        private static (string code, int location) MapShiftKeyDomDefinition(Keys key)
            => key == Keys.RShiftKey ? ("ShiftRight", 2) : ("ShiftLeft", 1);

        private static string FormatWindowsKeyName(Keys key)
            => key == Keys.RWin ? "RWin" : "LWin";

        private static string FormatControlKeyName(Keys? key)
            => key == Keys.RControlKey ? "RCtrl" : "LCtrl";

        private static string FormatAltKeyName(Keys? key)
            => key == Keys.RMenu ? "RAlt" : "LAlt";

        private static string FormatShiftKeyName(Keys? key)
            => key == Keys.RShiftKey ? "RShift" : "LShift";

        private static (string key, string code)? MapKeyToDomDefinition(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)('A' + (key - Keys.A));
                return (c.ToString().ToLowerInvariant(), $"Key{c}");
            }

            if (key >= Keys.D0 && key <= Keys.D9)
            {
                char c = (char)('0' + (key - Keys.D0));
                return (c.ToString(), $"Digit{c}");
            }

            return key switch
            {
                Keys.Tab => ("Tab", "Tab"),
                Keys.Space => (" ", "Space"),
                Keys.Enter => ("Enter", "Enter"),
                Keys.Escape => ("Escape", "Escape"),
                Keys.Left => ("ArrowLeft", "ArrowLeft"),
                Keys.Right => ("ArrowRight", "ArrowRight"),
                Keys.Up => ("ArrowUp", "ArrowUp"),
                Keys.Down => ("ArrowDown", "ArrowDown"),
                Keys.Home => ("Home", "Home"),
                Keys.End => ("End", "End"),
                Keys.Delete => ("Delete", "Delete"),
                Keys.Insert => ("Insert", "Insert"),
                Keys.CapsLock => ("CapsLock", "CapsLock"),
                Keys.Scroll => ("ScrollLock", "ScrollLock"),
                Keys.PrintScreen => ("PrintScreen", "PrintScreen"),
                Keys.PageUp => ("PageUp", "PageUp"),
                Keys.PageDown => ("PageDown", "PageDown"),
                Keys.Pause => ("Pause", "Pause"),
                Keys.Apps => ("ContextMenu", "ContextMenu"),
                Keys.VolumeMute => ("AudioVolumeMute", "AudioVolumeMute"),
                Keys.VolumeDown => ("AudioVolumeDown", "AudioVolumeDown"),
                Keys.VolumeUp => ("AudioVolumeUp", "AudioVolumeUp"),
                Keys.MediaNextTrack => ("MediaTrackNext", "MediaTrackNext"),
                Keys.MediaPreviousTrack => ("MediaTrackPrevious", "MediaTrackPrevious"),
                Keys.MediaPlayPause => ("MediaPlayPause", "MediaPlayPause"),
                Keys.MediaStop => ("MediaStop", "MediaStop"),
                Keys.BrowserBack => ("BrowserBack", "BrowserBack"),
                Keys.BrowserForward => ("BrowserForward", "BrowserForward"),
                Keys.BrowserRefresh => ("BrowserRefresh", "BrowserRefresh"),
                Keys.BrowserStop => ("BrowserStop", "BrowserStop"),
                Keys.BrowserSearch => ("BrowserSearch", "BrowserSearch"),
                Keys.BrowserFavorites => ("BrowserFavorites", "BrowserFavorites"),
                Keys.BrowserHome => ("BrowserHome", "BrowserHome"),
                Keys.LaunchMail => ("LaunchMail", "LaunchMail"),
                Keys.SelectMedia => ("LaunchMediaPlayer", "LaunchMediaPlayer"),
                Keys.LaunchApplication1 => ("LaunchApplication1", "LaunchApplication1"),
                Keys.LaunchApplication2 => ("LaunchApplication2", "LaunchApplication2"),
                Keys.F1 => ("F1", "F1"),
                Keys.F2 => ("F2", "F2"),
                Keys.F3 => ("F3", "F3"),
                Keys.F4 => ("F4", "F4"),
                Keys.F5 => ("F5", "F5"),
                Keys.F6 => ("F6", "F6"),
                Keys.F7 => ("F7", "F7"),
                Keys.F8 => ("F8", "F8"),
                Keys.F9 => ("F9", "F9"),
                Keys.F10 => ("F10", "F10"),
                Keys.F11 => ("F11", "F11"),
                Keys.F12 => ("F12", "F12"),
                _ => null,
            };
        }

        private async Task SendRemoteWindowsPauseTestSequenceAsync(Keys windowsKey)
        {
            var meta = MapWindowsKeyDomDefinition(windowsKey);

            // Known exception: the host catches Win+Pause correctly, but Guacamole/the remote
            // session does not currently interpret Pause/Break reliably. We keep this as a
            // contained probe sequence for future investigation instead of treating it as a
            // generally supported Win+<key> combination.
            await DispatchSyntheticKeyboardSequenceAsync(
                $$"""{"type":"keydown","key":"Meta","code":"{{meta.code}}","metaKey":true,"location":{{meta.location}}}""",

                """{ "type": "keydown", "key": "Pause", "code": "Pause", "metaKey": true }""",
                """{ "type": "keyup", "key": "Pause", "code": "Pause", "metaKey": true }""",

                """{ "type": "keydown", "key": "Pause", "code": "Cancel", "metaKey": true }""",
                """{ "type": "keyup", "key": "Pause", "code": "Cancel", "metaKey": true }""",

                """{ "type": "keydown", "key": "Break", "code": "Pause", "metaKey": true }""",
                """{ "type": "keyup", "key": "Break", "code": "Pause", "metaKey": true }""",

                $$"""{"type":"keyup","key":"Meta","code":"{{meta.code}}","metaKey":false,"location":{{meta.location}}}"""
            );
        }

        private async void sendRemoteWindowsKeyToolStripMenuItem_Click(object sender, EventArgs e) => await SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand.Windows);
        private async void sendRemoteCtrlAltDelToolStripMenuItem_Click(object sender, EventArgs e) => await SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand.CtrlAltDel);
        private async void sendRemoteCtrlAltEndToolStripMenuItem_Click(object sender, EventArgs e) => await SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand.CtrlAltEnd);
        private async void sendRemoteCtrlShiftEscToolStripMenuItem_Click(object sender, EventArgs e) => await SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand.CtrlShiftEsc);
        private async void sendRemoteAltF4ToolStripMenuItem_Click(object sender, EventArgs e) => await SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand.AltF4);
        private async void sendRemoteWinRToolStripMenuItem_Click(object sender, EventArgs e) => await SendRemoteSpecialKeyAsync(RemoteSpecialKeyCommand.WinR);

        private static class NativeKeyboardMethods
        {
            public const int WH_KEYBOARD_LL = 13;
            public const int WM_KEYDOWN = 0x0100;
            public const int WM_KEYUP = 0x0101;
            public const int WM_SYSKEYDOWN = 0x0104;
            public const int WM_SYSKEYUP = 0x0105;

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

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string? lpModuleName);
        }

        private static bool IsKeyCurrentlyDown(Keys key) => (NativeKeyboardMethods.GetAsyncKeyState((int)key) & 0x8000) != 0;

        private static Keys? GetActiveControlKey()
            => IsKeyCurrentlyDown(Keys.RControlKey) ? Keys.RControlKey :
               IsKeyCurrentlyDown(Keys.LControlKey) ? Keys.LControlKey :
               IsKeyCurrentlyDown(Keys.ControlKey) ? Keys.LControlKey : null;

        private static Keys? GetActiveAltKey()
            => IsKeyCurrentlyDown(Keys.RMenu) ? Keys.RMenu :
               IsKeyCurrentlyDown(Keys.LMenu) ? Keys.LMenu :
               IsKeyCurrentlyDown(Keys.Menu) ? Keys.LMenu : null;

        private static Keys? GetActiveShiftKey()
            => IsKeyCurrentlyDown(Keys.RShiftKey) ? Keys.RShiftKey :
               IsKeyCurrentlyDown(Keys.LShiftKey) ? Keys.LShiftKey :
               IsKeyCurrentlyDown(Keys.ShiftKey) ? Keys.LShiftKey : null;

        private static bool IsModifierKey(Keys key)
            => key is Keys.LWin or Keys.RWin or Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or Keys.Menu or Keys.LMenu or Keys.RMenu or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey;
    }
}
