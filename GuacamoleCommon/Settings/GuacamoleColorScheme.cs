
namespace GuacamoleClient.Common.Settings
{
    public class GuacamoleColorScheme
    {
        internal static PaletteBrightness GetPaletteBrightness(string primaryColor)
        {
            switch (primaryColor)
            {
                case "Green":
                case "DeepOrange":
                case "Red":
                case "DeepPurple":
                case "DarkRed":
                case "Indigo":
                case "Blue":
                case "Brown":
                case "DarkCyan":
                case "Teal":
                    return PaletteBrightness.LightTextOnDarkBackground;

                case "DarkGray":
                case "Gray":
                case "BlueGray":
                    return PaletteBrightness.LightTextOnDarkGrayBackground;

                case "Pink":
                case "LightBlue":
                case "Cyan":
                case "Purple":
                case "LightGreen":
                case "Lime":
                case "Yellow":
                case "Orange":
                case "OrangeRed":
                    return PaletteBrightness.DarkTextOnLightBackground;

                case "LightGray":
                    return PaletteBrightness.DarkTextOnLightGrayBackground;

                case "White":
                    return PaletteBrightness.DarkTextOnVeryLightBackground;

                case "Black":
                    return PaletteBrightness.LightTextOnVeryDarkBackground;

                default:
                    // TODO: identify light vs. dark colors more reliably
                    return PaletteBrightness.DarkTextOnLightBackground;
            }
        }

        public GuacamoleColorScheme(string primaryColor)
        {
            PrimaryColorValue = primaryColor;
            BrightnessOfPrimaryColor = GetPaletteBrightness(primaryColor);
            switch (BrightnessOfPrimaryColor)
            {
                case PaletteBrightness.LightTextOnDarkBackground:
                    TextColorValue = "White";
                    InactiveTextColorValue = "LightGray";
                    HoverBackgroundColorValue = "DarkCyan";
                    HoverTextColorValue = "White";
                    SelectedItemBackgroundColorValue = "White";
                    SelectedItemTextColorValue = "Black";
                    break;
                case PaletteBrightness.DarkTextOnLightBackground:
                    TextColorValue = "Black";
                    InactiveTextColorValue = "Gray";
                    HoverBackgroundColorValue = "LightBlue";
                    HoverTextColorValue = "Black";
                    SelectedItemBackgroundColorValue = "White";
                    SelectedItemTextColorValue = "Black";
                    break;
                case PaletteBrightness.DarkTextOnLightGrayBackground:
                    TextColorValue = "Black";
                    InactiveTextColorValue = "DarkGray";
                    HoverBackgroundColorValue = "DarkCyan";
                    HoverTextColorValue = "White";
                    SelectedItemBackgroundColorValue = "Cyan";
                    SelectedItemTextColorValue = "Black";
                    break;
                case PaletteBrightness.LightTextOnDarkGrayBackground:
                    TextColorValue = "White";
                    InactiveTextColorValue = "LightGray";
                    HoverBackgroundColorValue = "DarkCyan";
                    HoverTextColorValue = "White";
                    SelectedItemBackgroundColorValue = "Cyan";
                    SelectedItemTextColorValue = "Black";
                    break;
                case PaletteBrightness.DarkTextOnVeryLightBackground:
                    TextColorValue = "Black";
                    InactiveTextColorValue = "Gray";
                    HoverBackgroundColorValue = "LightBlue";
                    HoverTextColorValue = "Black";
                    SelectedItemBackgroundColorValue = "Cyan";
                    SelectedItemTextColorValue = "Black";
                    break;
                case PaletteBrightness.LightTextOnVeryDarkBackground:
                    TextColorValue = "White";
                    InactiveTextColorValue = "LightGray";
                    HoverBackgroundColorValue = "DarkCyan";
                    HoverTextColorValue = "White";
                    SelectedItemBackgroundColorValue = "Cyan";
                    SelectedItemTextColorValue = "Black";
                    break;
                default:
                    throw new NotImplementedException("Not implemented: " + BrightnessOfPrimaryColor.ToString());
            }
        }

        /// <summary>
        /// Primary color used in UI for this server profile
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string PrimaryColorValue { get; init; } = "OrangeRed";

        /// <summary>
        /// Primary color used in UI for this server profile
        /// </summary>
        public string PrimaryColorHexValue { get { return GuacamoleColorPalette.ResolveToHex(PrimaryColorValue); } }

        /// <summary>
        /// Text fore color for regular/active controls on top of primary color background
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string TextColorValue { get; init; } = "Black";

        /// <summary>
        /// Text fore color for regular/active controls on top of primary color background
        /// </summary>
        public string TextColorHexValue { get { return GuacamoleColorPalette.ResolveToHex(TextColorValue); } }

        /// <summary>
        /// Text fore color for inactive controls on top of primary color background
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string InactiveTextColorValue { get; init; } = "Gray";

        /// <summary>
        /// Text fore color for inactive controls on top of primary color background
        /// </summary>
        public string InactiveTextColorHexValue { get { return GuacamoleColorPalette.ResolveToHex(InactiveTextColorValue); } }

        /// <summary>
        /// Primary color substitution for a hovering menu item in UI for this server profile
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string HoverBackgroundColorValue { get; init; } = "LightBlue";

        /// <summary>
        /// Primary color substitution for a hovering menu item in UI for this server profile
        /// </summary>
        public string HoverBackgroundColorHexValue { get { return GuacamoleColorPalette.ResolveToHex(HoverBackgroundColorValue); } }

        /// <summary>
        /// Text fore color for a hovering menu item in UI for this server profile
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string HoverTextColorValue { get; init; } = "Black";

        /// <summary>
        /// Text fore color for a hovering menu item in UI for this server profile
        /// </summary>
        public string HoverTextColorHexValue { get { return GuacamoleColorPalette.ResolveToHex(HoverTextColorValue); } }

        /// <summary>
        /// Primary color substitution for a selected menu item in UI for this server profile
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string SelectedItemBackgroundColorValue { get; init; } = "White";

        /// <summary>
        /// Primary color substitution for a selected menu item in UI for this server profile
        /// </summary>
        public string SelectedItemBackgroundColorHexValue { get { return GuacamoleColorPalette.ResolveToHex(SelectedItemBackgroundColorValue); } }

        /// <summary>
        /// Text fore color for a selected menu item in UI for this server profile
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string SelectedItemTextColorValue { get; init; } = "Black";

        /// <summary>
        /// Text fore color for a selected menu item in UI for this server profile
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string SelectedItemTextColorHexValue { get { return GuacamoleColorPalette.ResolveToHex(SelectedItemTextColorValue); } }

        public PaletteBrightness BrightnessOfPrimaryColor { get; init; }

        public enum PaletteBrightness
        {
            LightTextOnDarkBackground,
            LightTextOnVeryDarkBackground,
            LightTextOnDarkGrayBackground,
            DarkTextOnLightBackground,
            DarkTextOnVeryLightBackground,
            DarkTextOnLightGrayBackground,
        }
    }
}
