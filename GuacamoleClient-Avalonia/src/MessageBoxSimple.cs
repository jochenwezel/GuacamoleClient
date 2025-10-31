using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace GuacClient;

public static class MessageBoxSimple
{
    public static Task Show(Window owner, string title, string message)
    {
        var w = new Window
        {
            Title = title,
            Width = 460,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock{ Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { new Button { Content = "OK", IsDefault = true, IsCancel = true } }
                    }
                }
            }
        };
        if (owner != null) w.Icon = owner.Icon;
        w.ShowDialog(owner);
        return Task.CompletedTask;
    }
}
