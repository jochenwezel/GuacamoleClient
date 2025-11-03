using SharpHook;
using SharpHook.Data; 

namespace GuacamoleClient.Common
{
    public class SharpHooks
    {
        static async void ExampleGlobalKeyListenerAsync()
        {
            var hook = new EventLoopGlobalHook(GlobalHookType.Keyboard);

            hook.KeyPressed += (s, e) =>
            {
                // Modifier jetzt in e.Mask (EventMask-Flags)
                var m = e.RawEvent.Mask;
                bool ctrl = (m & EventMask.Ctrl) != 0;
                bool alt = (m & EventMask.Alt) != 0;
                bool shift = (m & EventMask.Shift) != 0;
                bool meta = (m & EventMask.Meta) != 0; // ⌘ auf macOS, Win/Super sonst (wenn durchgereicht)

                // Links/Rechts weiterhin über KeyCode
                bool leftCtrl = e.Data.KeyCode == KeyCode.VcLeftControl;
                bool rightCtrl = e.Data.KeyCode == KeyCode.VcRightControl;
                bool leftAlt = e.Data.KeyCode == KeyCode.VcLeftAlt;
                bool rightAlt = e.Data.KeyCode == KeyCode.VcRightAlt;
                bool leftMeta = e.Data.KeyCode == KeyCode.VcLeftMeta;
                bool rightMeta = e.Data.KeyCode == KeyCode.VcRightMeta;

                // AltGr (Windows): meist RightAlt + „virtuelles“ Control
                bool altGr = rightAlt && ctrl;

                // Primärmodifier (Cmd auf macOS, Ctrl sonst)
                bool primary = OperatingSystem.IsMacOS() ? meta : ctrl;

                // Ihre Logik …
                // Beispiel-Shortcut: primary+K
                if (primary && e.RawEvent.Keyboard.KeyCode == KeyCode.VcK && e.RawEvent.Type == EventType.KeyPressed)
                {
                    // …
                    e.SuppressEvent = true; // optional: Event unterdrücken (Win/macOS)
                }
            };

            await hook.RunAsync();
        }
    }
}