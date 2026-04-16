using InfoBox;
using GuacamoleClient.Common.Localization;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly HashSet<Keys> _activeModifierKeys = new();
        private readonly HashSet<Keys> _heldRemoteModifiers = new();
        private readonly List<Keys> _modifierOnlyChordOrder = new();
        private bool _modifierOnlyChordHadNonModifier;

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
            _activeModifierKeys.Clear();
            _heldRemoteModifiers.Clear();
            _modifierOnlyChordOrder.Clear();
            _modifierOnlyChordHadNonModifier = false;
            HideTransientHint();
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
                    UpdateModifierState(key, true);
                    SyncHeldRemoteModifiersToTrackedState();
                    if (TryHandleRemoteSpecialShortcutKeyDown(key))
                        return (IntPtr)1;
                }
                else if (msg == NativeKeyboardMethods.WM_KEYUP || msg == NativeKeyboardMethods.WM_SYSKEYUP)
                {
                    UpdateModifierState(key, false);
                    SyncHeldRemoteModifiersToTrackedState();
                    if (TryHandleRemoteSpecialShortcutKeyUp(key))
                        return (IntPtr)1;
                }
            }

            return NativeKeyboardMethods.CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }

        private bool TryHandleRemoteSpecialShortcutKeyDown(Keys key)
        {
            if (!IsModifierKey(key) && _activeModifierKeys.Count > 0)
                _modifierOnlyChordHadNonModifier = true;

            Keys? controlKey = GetTrackedControlKey();
            Keys? altKey = GetTrackedAltKey();
            Keys? shiftKey = GetTrackedShiftKey();
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
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlAltDel, LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltDel_Sent, FormatControlKeyName(controlKey), FormatAltKeyName(altKey)), controlKey, altKey);

            if (ctrl && shift && key == Keys.Escape)
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlShiftEsc, LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlShiftEsc_Sent, FormatControlKeyName(controlKey), FormatShiftKeyName(shiftKey)), controlKey, null, shiftKey);

            if (ctrl && alt && key == Keys.End)
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.CtrlAltEnd, LocalizationProvider.Get(LocalizationKeys.Hint_RemoteCtrlAltEnd_Sent, FormatControlKeyName(controlKey), FormatAltKeyName(altKey)), controlKey, altKey);

            if (alt && !ctrl && key == Keys.F4)
                return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.AltF4, LocalizationProvider.Get(LocalizationKeys.Hint_RemoteAltF4_Sent, FormatAltKeyName(altKey)), null, altKey);

            if (alt && !ctrl && key == Keys.Tab)
                return TriggerRemoteAltTab(altKey ?? Keys.LMenu);

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
                    return TriggerRemoteSpecialKey(RemoteSpecialKeyCommand.Windows, LocalizationProvider.Get(LocalizationKeys.Hint_RemoteWindowsKey_Sent, FormatWindowsKeyName(releasedWindowsKey)));

                TriggerRemoteModifierRelease(releasedWindowsKey);
                return true;
            }

            if (_heldRemoteModifiers.Contains(key))
            {
                TriggerRemoteModifierRelease(key);
                return IsModifierKey(key);
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

        private bool TriggerRemoteAltTab(Keys altKey)
        {
            BeginInvoke(async () =>
            {
                try
                {
                    await EnsureRemoteModifierHeldAsync(altKey).ConfigureAwait(true);
                    await SendRemoteKeyPulseAsync(Keys.Tab).ConfigureAwait(true);
                    ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_RemoteAltTab_Sent, FormatAltKeyName(altKey)));
                }
                catch (Exception ex)
                {
                    ShowMessageBoxNonModal(ex.ToString(), "Remote Alt+Tab", InformationBoxButtons.OK, InformationBoxIcon.Error);
                }
            });
            return true;
        }

        private void TriggerRemoteModifierRelease(Keys key)
        {
            BeginInvoke(async () =>
            {
                try
                {
                    await ReleaseRemoteModifierAsync(key).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    ShowMessageBoxNonModal(ex.ToString(), "Remote modifier key", InformationBoxButtons.OK, InformationBoxIcon.Error);
                }
            });
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
                        ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_RemoteWindowsPause_NotReliable, FormatWindowsKeyName(windowsKey)));
                    }
                    else
                    {
                        bool sent = await TrySendRemoteWindowsCombinationAsync(windowsKey, key).ConfigureAwait(true);
                        if (sent)
                            ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_RemoteWindowsCombination_Sent, FormatWindowsKeyName(windowsKey), key));
                        else
                            ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_RemoteWindowsCombination_NotMapped, FormatWindowsKeyName(windowsKey), key));
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

        private void HideTransientHint()
        {
            try { _tip.Hide(this); } catch { }
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

            await EnsureRemoteModifierHeldAsync(windowsKey).ConfigureAwait(false);
            await SendRemoteKeyPulseAsync(key).ConfigureAwait(false);
            return true;
        }

        private async Task EnsureRemoteModifierHeldAsync(Keys key)
        {
            if (_heldRemoteModifiers.Contains(key))
                return;

            _heldRemoteModifiers.Add(key);
            await DispatchSyntheticKeyboardSequenceAsync(BuildModifierKeyboardEventJson("keydown", key, _heldRemoteModifiers)).ConfigureAwait(false);
        }

        private async Task ReleaseRemoteModifierAsync(Keys key)
        {
            if (!_heldRemoteModifiers.Contains(key))
                return;

            _heldRemoteModifiers.Remove(key);
            await DispatchSyntheticKeyboardSequenceAsync(BuildModifierKeyboardEventJson("keyup", key, _heldRemoteModifiers)).ConfigureAwait(false);
        }

        private async Task SendRemoteKeyPulseAsync(Keys key)
        {
            var mapped = MapKeyToDomDefinition(key);
            if (mapped == null)
                throw new NotSupportedException($"No DOM key mapping implemented for {key}.");

            await DispatchSyntheticKeyboardSequenceAsync(
                BuildKeyboardEventJson("keydown", mapped.Value.key, mapped.Value.code, _heldRemoteModifiers),
                BuildKeyboardEventJson("keyup", mapped.Value.key, mapped.Value.code, _heldRemoteModifiers)
            );
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

        private void UpdateModifierState(Keys key, bool isDown)
        {
            if (!TryNormalizeModifierKey(key, out Keys normalizedKey))
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
                    bool onlyWindowsKeys = _modifierOnlyChordOrder.Count > 0 && _modifierOnlyChordOrder.All(k => k is Keys.LWin or Keys.RWin);

                    if (!_modifierOnlyChordHadNonModifier && _modifierOnlyChordOrder.Count > 0 && !onlyWindowsKeys)
                    {
                        var chord = _modifierOnlyChordOrder.ToArray();
                        BeginInvoke(async () =>
                        {
                            try
                            {
                                await SendModifierOnlyChordAsync(chord).ConfigureAwait(true);
                            }
                            catch (Exception ex)
                            {
                                ShowMessageBoxNonModal(ex.ToString(), "Remote modifier keys", InformationBoxButtons.OK, InformationBoxIcon.Error);
                            }
                        });
                    }

                    _modifierOnlyChordOrder.Clear();
                    _modifierOnlyChordHadNonModifier = false;
                }
            }

            ShowActiveModifierHint();
        }

        private void ShowActiveModifierHint()
        {
            string text = FormatActiveModifierState();
            if (string.IsNullOrWhiteSpace(text))
            {
                HideTransientHint();
                return;
            }

            ShowTransientHint(LocalizationProvider.Get(LocalizationKeys.Hint_ActiveModifiers, text));
        }

        private void SyncHeldRemoteModifiersToTrackedState()
        {
            if (_heldRemoteModifiers.Count == 0)
                return;

            var staleKeys = _heldRemoteModifiers.Where(key => !_activeModifierKeys.Contains(key)).ToArray();
            if (staleKeys.Length == 0)
                return;

            foreach (Keys staleKey in staleKeys)
                _heldRemoteModifiers.Remove(staleKey);

            BeginInvoke(async () =>
            {
                try
                {
                    foreach (Keys staleKey in staleKeys)
                    {
                        await DispatchSyntheticKeyboardSequenceAsync(
                            BuildModifierKeyboardEventJson("keyup", staleKey, _heldRemoteModifiers)
                        ).ConfigureAwait(true);
                    }
                }
                catch (Exception ex)
                {
                    ShowMessageBoxNonModal(ex.ToString(), "Remote modifier sync", InformationBoxButtons.OK, InformationBoxIcon.Error);
                }
            });
        }

        private string FormatActiveModifierState()
        {
            if (_activeModifierKeys.Count == 0)
                return string.Empty;

            var names = new List<string>();

            bool leftCtrl = _activeModifierKeys.Contains(Keys.LControlKey);
            bool rightCtrl = _activeModifierKeys.Contains(Keys.RControlKey);
            bool leftAlt = _activeModifierKeys.Contains(Keys.LMenu);
            bool rightAlt = _activeModifierKeys.Contains(Keys.RMenu);

            if (rightAlt && leftCtrl && !leftAlt && !rightCtrl)
                names.Add("AltGr");
            else
            {
                if (leftCtrl) names.Add("LCtrl");
                if (rightCtrl) names.Add("RCtrl");
                if (leftAlt) names.Add("LAlt");
                if (rightAlt) names.Add("RAlt");
            }

            if (_activeModifierKeys.Contains(Keys.LShiftKey)) names.Add("LShift");
            if (_activeModifierKeys.Contains(Keys.RShiftKey)) names.Add("RShift");
            if (_activeModifierKeys.Contains(Keys.LWin)) names.Add("LWin");
            if (_activeModifierKeys.Contains(Keys.RWin)) names.Add("RWin");

            return string.Join("+", names.Distinct());
        }

        private static bool TryNormalizeModifierKey(Keys key, out Keys normalizedKey)
        {
            normalizedKey = key switch
            {
                Keys.ControlKey => Keys.LControlKey,
                Keys.Menu => Keys.LMenu,
                Keys.ShiftKey => Keys.LShiftKey,
                Keys.LControlKey or Keys.RControlKey or
                Keys.LMenu or Keys.RMenu or
                Keys.LShiftKey or Keys.RShiftKey or
                Keys.LWin or Keys.RWin => key,
                _ => Keys.None,
            };

            return normalizedKey != Keys.None;
        }

        private async Task SendModifierOnlyChordAsync(IReadOnlyList<Keys> modifierKeys)
        {
            if (modifierKeys.Count == 0)
                return;

            var events = new List<string>();
            var pressed = new HashSet<Keys>();

            foreach (Keys key in modifierKeys)
            {
                pressed.Add(key);
                events.Add(BuildModifierKeyboardEventJson("keydown", key, pressed));
            }

            for (int i = modifierKeys.Count - 1; i >= 0; i--)
            {
                Keys key = modifierKeys[i];
                pressed.Remove(key);
                events.Add(BuildModifierKeyboardEventJson("keyup", key, pressed));
            }

            await DispatchSyntheticKeyboardSequenceAsync(events.ToArray());
        }

        private static string BuildModifierKeyboardEventJson(string type, Keys key, IReadOnlyCollection<Keys> pressedAfterEvent)
        {
            var mapped = MapModifierDomDefinition(key);
            return BuildKeyboardEventJson(type, mapped.key, mapped.code, pressedAfterEvent, mapped.location);
        }

        private static string BuildKeyboardEventJson(string type, string key, string code, IReadOnlyCollection<Keys> activeModifiers, int location = 0)
        {
            var payload = new
            {
                type,
                key,
                code,
                ctrlKey = activeModifiers.Contains(Keys.LControlKey) || activeModifiers.Contains(Keys.RControlKey),
                altKey = activeModifiers.Contains(Keys.LMenu) || activeModifiers.Contains(Keys.RMenu),
                shiftKey = activeModifiers.Contains(Keys.LShiftKey) || activeModifiers.Contains(Keys.RShiftKey),
                metaKey = activeModifiers.Contains(Keys.LWin) || activeModifiers.Contains(Keys.RWin),
                location
            };

            return JsonSerializer.Serialize(payload);
        }

        private static (string key, string code, int location) MapModifierDomDefinition(Keys key)
            => key switch
            {
                Keys.LControlKey => ("Control", "ControlLeft", 1),
                Keys.RControlKey => ("Control", "ControlRight", 2),
                Keys.LMenu => ("Alt", "AltLeft", 1),
                Keys.RMenu => ("Alt", "AltRight", 2),
                Keys.LShiftKey => ("Shift", "ShiftLeft", 1),
                Keys.RShiftKey => ("Shift", "ShiftRight", 2),
                Keys.LWin => ("Meta", "MetaLeft", 1),
                Keys.RWin => ("Meta", "MetaRight", 2),
                _ => throw new NotSupportedException($"No DOM modifier mapping implemented for {key}.")
            };

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

        private Keys? GetTrackedControlKey()
            => _activeModifierKeys.Contains(Keys.RControlKey) ? Keys.RControlKey :
               _activeModifierKeys.Contains(Keys.LControlKey) ? Keys.LControlKey :
               IsKeyCurrentlyDown(Keys.RControlKey) ? Keys.RControlKey :
               IsKeyCurrentlyDown(Keys.LControlKey) ? Keys.LControlKey :
               IsKeyCurrentlyDown(Keys.ControlKey) ? Keys.LControlKey : null;

        private Keys? GetTrackedAltKey()
            => _activeModifierKeys.Contains(Keys.RMenu) ? Keys.RMenu :
               _activeModifierKeys.Contains(Keys.LMenu) ? Keys.LMenu :
               IsKeyCurrentlyDown(Keys.RMenu) ? Keys.RMenu :
               IsKeyCurrentlyDown(Keys.LMenu) ? Keys.LMenu :
               IsKeyCurrentlyDown(Keys.Menu) ? Keys.LMenu : null;

        private Keys? GetTrackedShiftKey()
            => _activeModifierKeys.Contains(Keys.RShiftKey) ? Keys.RShiftKey :
               _activeModifierKeys.Contains(Keys.LShiftKey) ? Keys.LShiftKey :
               IsKeyCurrentlyDown(Keys.RShiftKey) ? Keys.RShiftKey :
               IsKeyCurrentlyDown(Keys.LShiftKey) ? Keys.LShiftKey :
               IsKeyCurrentlyDown(Keys.ShiftKey) ? Keys.LShiftKey : null;

        private static bool IsModifierKey(Keys key)
            => key is Keys.LWin or Keys.RWin or Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or Keys.Menu or Keys.LMenu or Keys.RMenu or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey;
    }
}
