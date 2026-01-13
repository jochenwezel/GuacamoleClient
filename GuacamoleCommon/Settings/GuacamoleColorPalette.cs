using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GuacamoleClient.Common.Settings
{
    /// <summary>
    /// Fixed color palette (>=16 colors) as hex strings. Intended for deterministic, cross-platform UI.
    /// </summary>
    public static class GuacamoleColorPalette
    {
        // NOTE: Keys are user-facing in UI; values are normalized as #RRGGBB.
        // NOTE: Update GuacamoleColorScheme.GetPaletteBrightness when adding new colors.
        public static readonly IReadOnlyDictionary<string, string> Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Red", "#D32F2F" },
            { "DarkRed", "#B71C1C" },
            { "Pink", "#C2185B" },
            { "Purple", "#7B1FA2" },
            { "DeepPurple", "#512DA8" },
            { "Indigo", "#303F9F" },
            { "Blue", "#1976D2" },
            { "LightBlue", "#0288D1" },
            { "Cyan", "#0097A7" },
            { "DarkCyan", "#006064" },
            { "Teal", "#00796B" },
            { "Green", "#388E3C" },
            { "LightGreen", "#689F38" },
            { "Lime", "#AFB42B" },
            { "Yellow", "#FBC02D" },
            { "Orange", "#F57C00" },
            { "DeepOrange", "#E64A19" },
            { "Brown", "#5D4037" },
            { "LightGray", "#D1D1D1" },
            { "DarkGray", "#A9A9A9" },
            { "Gray", "#616161" },
            { "BlueGray", "#455A64" },
            { "White", "#FFFFFF" },
            { "Black", "#000000" },
            { "OrangeRed", "#FF4500" }
        };

        public static IEnumerable<string> Keys => Colors.Keys;

        public static string GetHexOrThrow(string key)
        {
            if (Colors.TryGetValue(key, out var hex)) return hex;
            throw new ArgumentOutOfRangeException(nameof(key), $"Unknown color palette key: {key}");
        }

        private static readonly Regex HexRegex = new Regex("^[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

        /// <summary>
        /// Resolve a color value to a normalized hex string (#RRGGBB).
        /// Accepts palette keys (e.g. "OrangeRed") or hex strings ("#RRGGBB" or "RRGGBB").
        /// </summary>
        public static string ResolveToHex(string? colorValue)
        {
            if (string.IsNullOrWhiteSpace(colorValue))
                return GuacamoleColorPalette.GetHexOrThrow("OrangeRed");

            var v = colorValue.Trim();
            if (v.StartsWith("#")) v = v.Substring(1);

            // Palette key?
            if (GuacamoleColorPalette.Colors.TryGetValue(v, out var paletteHex))
                return paletteHex;

            // Hex?
            if (HexRegex.IsMatch(v))
                return "#" + v.ToUpperInvariant();

            throw new FormatException("Invalid color value " + colorValue + ". Use palette key or hex (e.g. #A1B2C3).");
        }

        public static bool TryResolveToHex(string? colorValue, out string hex)
        {
            try
            {
                hex = ResolveToHex(colorValue);
                return true;
            }
            catch
            {
                hex = string.Empty;
                return false;
            }
        }
    }
}
