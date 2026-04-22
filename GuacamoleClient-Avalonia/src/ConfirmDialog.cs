using Avalonia.Controls;
using Avalonia.Layout;
using GuacamoleClient.Common.Localization;
using System.Threading.Tasks;

namespace GuacClient;

internal static class ConfirmDialog
{
    public static async Task<bool> ShowYesNoAsync(Window owner, string title, string message)
    {
        bool result = false;

        var yesButton = new Button { Content = LocalizationProvider.Get(LocalizationKeys.Common_Button_Yes) };
        var noButton = new Button { Content = LocalizationProvider.Get(LocalizationKeys.Common_Button_No), IsDefault = true, IsCancel = true };

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        yesButton.Click += (_, __) =>
        {
            result = true;
            dialog.Close();
        };
        noButton.Click += (_, __) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { yesButton, noButton }
                }
            }
        };

        if (owner != null)
            dialog.Icon = owner.Icon;

        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();
        return result;
    }
}
