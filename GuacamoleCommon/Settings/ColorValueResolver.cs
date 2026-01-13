using System;
using System.Text.RegularExpressions;

namespace GuacamoleClient.Common.Settings
{
    public static class ColorValueResolver
    {
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
