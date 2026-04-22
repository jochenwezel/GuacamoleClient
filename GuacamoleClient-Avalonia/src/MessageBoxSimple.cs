using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Threading.Tasks;

namespace GuacClient;

public static class MessageBoxSimple
{
    public static Task Show(Window owner, string title, string message)
        => Show(owner, title, message, Array.Empty<(string Text, string Url)>());

    public static Task Show(Window owner, string title, string message, params (string Text, string Url)[] links)
    {
        Window? dialog = null;

        var messageTextBox = new TextBox
        {
            Text = message,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MinHeight = 180
        };

        var scrollViewer = new ScrollViewer
        {
            Content = messageTextBox
        };

        var okButton = new Button { Content = "OK", IsDefault = true, IsCancel = true };
        var copyButton = new Button { Content = "Copy to Clipboard" };

        okButton.Click += (_, __) => dialog?.Close();
        copyButton.Click += async (_, __) =>
        {
            if (dialog == null)
                return;

            var topLevel = TopLevel.GetTopLevel(dialog);
            if (topLevel?.Clipboard != null)
                await topLevel.Clipboard.SetTextAsync(message);
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { copyButton }
        };

        foreach (var link in links)
        {
            var linkButton = new Button { Content = link.Text };
            linkButton.Click += async (_, __) =>
            {
                if (dialog == null)
                    return;

                var topLevel = TopLevel.GetTopLevel(dialog);
                if (topLevel?.Launcher != null && Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
                    await topLevel.Launcher.LaunchUriAsync(uri);
            };
            buttonPanel.Children.Add(linkButton);
        }

        buttonPanel.Children.Add(okButton);
        Grid.SetRow(buttonPanel, 1);

        dialog = new Window
        {
            Title = title,
            Width = 760,
            Height = 420,
            MinWidth = 520,
            MinHeight = 260,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Grid
            {
                Margin = new Avalonia.Thickness(16),
                RowDefinitions = new RowDefinitions("*,Auto"),
                RowSpacing = 12,
                Children =
                {
                    scrollViewer,
                    buttonPanel
                }
            }
        };

        if (owner != null)
            dialog.Icon = owner.Icon;

        dialog.Opened += (_, __) =>
        {
            messageTextBox.Focus();
            messageTextBox.SelectionStart = 0;
            messageTextBox.SelectionEnd = messageTextBox.Text?.Length ?? 0;
        };

#pragma warning disable CS8604
        dialog.ShowDialog(owner);
#pragma warning restore CS8604
        return Task.CompletedTask;
    }
}
