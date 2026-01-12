using System;
using System.Collections.Generic;

namespace GuacamoleClient.Common.Settings
{
    /// <summary>
    /// Fixed color palette (>=16 colors) as hex strings. Intended for deterministic, cross-platform UI.
    /// </summary>
    public static class GuacamoleColorPalette
    {
        // NOTE: Keys are user-facing in UI; values are normalized as #RRGGBB.
        public static readonly IReadOnlyDictionary<string, string> Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Red", "#D32F2F" },
            { "Pink", "#C2185B" },
            { "Purple", "#7B1FA2" },
            { "DeepPurple", "#512DA8" },
            { "Indigo", "#303F9F" },
            { "Blue", "#1976D2" },
            { "LightBlue", "#0288D1" },
            { "Cyan", "#0097A7" },
            { "Teal", "#00796B" },
            { "Green", "#388E3C" },
            { "LightGreen", "#689F38" },
            { "Lime", "#AFB42B" },
            { "Yellow", "#FBC02D" },
            { "Orange", "#F57C00" },
            { "DeepOrange", "#E64A19" },
            { "Brown", "#5D4037" },
            { "Grey", "#616161" },
            { "BlueGrey", "#455A64" },
        };

        public static IEnumerable<string> Keys => Colors.Keys;

        public static string GetHexOrThrow(string key)
        {
            if (Colors.TryGetValue(key, out var hex)) return hex;
            throw new ArgumentOutOfRangeException(nameof(key), $"Unknown color palette key: {key}");
        }
    }
}
