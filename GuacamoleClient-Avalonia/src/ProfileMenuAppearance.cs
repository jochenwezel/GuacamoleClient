using Avalonia.Controls;
using Avalonia.Media;
using GuacamoleClient.Common.Settings;

namespace GuacClient;

internal static class ProfileMenuAppearance
{
    internal static void Apply(Menu menu, GuacamoleColorScheme scheme)
    {
        var background = Brush.Parse(scheme.PrimaryColorHexValue);
        var foreground = Brush.Parse(scheme.TextColorHexValue);
        var hoverBackground = Brush.Parse(scheme.HoverBackgroundColorHexValue);
        var hoverForeground = Brush.Parse(scheme.HoverTextColorHexValue);
        var selectedBackground = Brush.Parse(scheme.SelectedItemBackgroundColorHexValue);
        var selectedForeground = Brush.Parse(scheme.SelectedItemTextColorHexValue);
        // Derive disabled text from the contrasting profile text: preset inactive grays
        // can disappear on gray profile backgrounds.
        var disabledForeground = new SolidColorBrush(Color.Parse(scheme.TextColorHexValue), 0.65);
        var separator = new SolidColorBrush(Color.Parse(scheme.TextColorHexValue), 0.5);

        // Keep Fluent overrides local to this menu, including its nested popup templates.
        // Updating the existing dictionary also refreshes DynamicResource values in open popups.
        var resources = menu.Resources;
        resources["MenuFlyoutPresenterBackground"] = background;
        resources["MenuFlyoutPresenterBorderBrush"] = separator;
        resources["ProfileMenuSeparatorBrush"] = separator;
        resources["MenuFlyoutItemBackground"] = background;
        resources["MenuFlyoutItemForeground"] = foreground;
        resources["MenuFlyoutItemBackgroundPointerOver"] = hoverBackground;
        resources["MenuFlyoutItemForegroundPointerOver"] = hoverForeground;
        resources["MenuFlyoutItemBackgroundPressed"] = selectedBackground;
        resources["MenuFlyoutItemForegroundPressed"] = selectedForeground;
        resources["MenuFlyoutItemBackgroundDisabled"] = background;
        resources["MenuFlyoutItemForegroundDisabled"] = disabledForeground;
        resources["MenuFlyoutItemKeyboardAcceleratorTextForeground"] = foreground;
        resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver"] = hoverForeground;
        resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed"] = selectedForeground;
        resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled"] = disabledForeground;
        resources["MenuFlyoutSubItemChevron"] = foreground;
        resources["MenuFlyoutSubItemChevronPointerOver"] = hoverForeground;
        resources["MenuFlyoutSubItemChevronPressed"] = selectedForeground;
        resources["MenuFlyoutSubItemChevronDisabled"] = disabledForeground;
        resources["MenuFlyoutSubItemChevronSubMenuOpened"] = foreground;

        menu.Background = background;
        menu.Foreground = foreground;
        menu.Classes.Add("profile-colors");
    }
}
