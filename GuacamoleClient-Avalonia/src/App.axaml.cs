using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Globalization;
using System.IO;

namespace GuacClient;
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
#if !NATIVE_BROWSER_PILOT
        WorkAroundLinuxCefGlueTextShapingCrash();
#endif

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                Icon = LoadApplicationIcon()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static WindowIcon? LoadApplicationIcon()
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "guac.ico");
        return File.Exists(iconPath) ? new WindowIcon(iconPath) : null;
    }

#if !NATIVE_BROWSER_PILOT
    private static void WorkAroundLinuxCefGlueTextShapingCrash()
    {
        if (!OperatingSystem.IsLinux())
            return;

        var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 12, 0, CultureInfo.CurrentCulture, 100);
        TextShaper.Current.ShapeText("GuacamoleClient".AsMemory(), options);
    }
#endif
}
