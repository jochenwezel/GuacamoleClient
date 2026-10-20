using Avalonia.Input;
using System.Text.Json;

namespace GuacClient;

internal static class NativeKeyEventBuilder
{
    internal static string Build(Key key, int virtualKey, string type, IReadOnlyCollection<Key> modifiers, char? character = null)
    {
        if (virtualKey == 0) throw new NotSupportedException($"No native key mapping for {key}.");
        int flags = (modifiers.Any(k => k is Key.LeftAlt or Key.RightAlt) ? 1 : 0)
            | (modifiers.Any(k => k is Key.LeftCtrl or Key.RightCtrl) ? 2 : 0)
            | (modifiers.Any(k => k is Key.LWin or Key.RWin) ? 4 : 0)
            | (modifiers.Any(k => k is Key.LeftShift or Key.RightShift) ? 8 : 0);
        string name = key switch
        {
            Key.LeftCtrl or Key.RightCtrl => "Control",
            Key.LeftAlt or Key.RightAlt => "Alt",
            Key.LeftShift or Key.RightShift => "Shift",
            Key.LWin or Key.RWin => "Meta",
            Key.Return => "Enter", Key.Back => "Backspace", Key.Space => " ",
            Key.Left => "ArrowLeft", Key.Right => "ArrowRight", Key.Up => "ArrowUp", Key.Down => "ArrowDown",
            Key.Apps => "ContextMenu", _ => key.ToString()
        };
        if (key >= Key.A && key <= Key.Z)
            name = (flags & 8) != 0 ? key.ToString() : key.ToString().ToLowerInvariant();
        if (key >= Key.D0 && key <= Key.D9) name = ((int)key - (int)Key.D0).ToString();
        string code = key switch
        {
            Key.LeftCtrl => "ControlLeft", Key.RightCtrl => "ControlRight",
            Key.LeftAlt => "AltLeft", Key.RightAlt => "AltRight",
            Key.LeftShift => "ShiftLeft", Key.RightShift => "ShiftRight",
            Key.LWin => "MetaLeft", Key.RWin => "MetaRight",
            Key.Return => "Enter", Key.Back => "Backspace", Key.Space => "Space",
            _ when key >= Key.A && key <= Key.Z => "Key" + key,
            _ when key >= Key.D0 && key <= Key.D9 => "Digit" + ((int)key - (int)Key.D0),
            _ => name
        };
        int location = key switch
        {
            Key.LeftCtrl or Key.LeftAlt or Key.LeftShift or Key.LWin => 1,
            Key.RightCtrl or Key.RightAlt or Key.RightShift or Key.RWin => 2,
            _ => 0
        };
        // Include text only for actual character events; shortcut down/up events
        // must never insert a character into the remote desktop.
        return JsonSerializer.Serialize(new
        {
            type, key = character?.ToString() ?? name, code, modifiers = flags,
            windowsVirtualKeyCode = virtualKey, nativeVirtualKeyCode = virtualKey,
            location, isSystemKey = key is Key.LeftAlt or Key.RightAlt or Key.F10 || (flags & 1) != 0,
            text = character?.ToString() ?? "", unmodifiedText = character is { } c ? char.ToLowerInvariant(c).ToString() : ""
        });
    }
}
