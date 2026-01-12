using System;
using System.Drawing;
using GuacamoleClient.Common.Settings;

namespace GuacamoleClient.WinForms
{
    internal static class UiColorHelpers
    {
        public static Color ResolveProfilePrimaryColor(GuacamoleServerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var hex = ColorValueResolver.ResolveToHex(profile.ColorValue);
            return ParseHexColor(hex);
        }

        public static Color ParseHexColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) throw new ArgumentException("hex required", nameof(hex));
            var v = hex.Trim();
            if (v.StartsWith("#")) v = v.Substring(1);
            if (v.Length != 6) throw new FormatException("Expected #RRGGBB");

            int r = Convert.ToInt32(v.Substring(0, 2), 16);
            int g = Convert.ToInt32(v.Substring(2, 2), 16);
            int b = Convert.ToInt32(v.Substring(4, 2), 16);
            return Color.FromArgb(r, g, b);
        }
    }
}
