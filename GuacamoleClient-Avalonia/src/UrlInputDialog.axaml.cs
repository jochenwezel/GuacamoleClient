using Avalonia.Controls;
using System;

namespace GuacClient;

public partial class UrlInputDialog : Window
{
    public UrlInputDialog()
    {
        InitializeComponent();
    }

    private void Ok_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var text = this.FindControl<TextBox>("Input")!.Text?.Trim();
        if (IsValidUrl(text))
        {
            Close(text);
        }
        else
        {
            _ = MessageBoxSimple.Show(this, "Ungültige URL", "Bitte eine absolute http/https-URL angeben.");
        }
    }

    public static bool IsValidUrl(string? value)
        => !string.IsNullOrWhiteSpace(value)
        && Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
