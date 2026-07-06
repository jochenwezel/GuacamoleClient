using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System.Globalization;

namespace GuacClient;
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        WorkAroundLinuxCefGlueTextShapingCrash();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static void WorkAroundLinuxCefGlueTextShapingCrash()
    {
        if (!OperatingSystem.IsLinux())
            return;

        var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 12, 0, CultureInfo.CurrentCulture, 100);
        TextShaper.Current.ShapeText("GuacamoleClient".AsMemory(), options);
    }
}
