using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GuacamoleClient.Common.Settings;
using GuacClient;
using NUnit.Framework;
using System.Runtime.InteropServices;

[assembly: AvaloniaTestApplication(typeof(GuacamoleClient.Avalonia.Tests.MenuTestApplication))]

namespace GuacamoleClient.Avalonia.Tests;

/// <summary>Configures Fluent menus with Skia rendering on a headless platform.</summary>
public class MenuTestApplication : Application
{
    /// <summary>Creates the isolated menu test application.</summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<MenuTestApplication>()
        .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });

    /// <inheritdoc/>
    public override void Initialize() => Styles.Add(new FluentTheme());
}

public class ProfileMenuAppearanceTests
{
    [AvaloniaTest]
    public void OpenPopups_UpdateSurfacesStrokesAndStates_WhenProfileChanges()
    {
        // Exercise actual Fluent templates and resource updates across two open popup levels.
        var (window, menu, parent, nested, separator, nestedSeparator, disabled) = CreateMenu();
        var unrelated = new MenuItem { Header = "Unrelated menu" };
        var otherWindow = new Window { Content = new Menu { Items = { unrelated } } };
        otherWindow.Show();
        var originalBrush = unrelated.Background;
        try
        {
            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
            {
                window.RequestedThemeVariant = theme;
                foreach (var color in new[] { "Yellow", "Blue", "White", "Black", "LightGray", "DarkGray" })
                {
                    var scheme = new GuacamoleColorScheme(color);
                    ProfileMenuAppearance.Apply(menu, scheme);
                    parent.IsSubMenuOpen = true;
                    nested.IsSubMenuOpen = true;
                    Flush();
                    AssertPopup(parent, scheme);
                    AssertPopup(nested, scheme);
                    AssertStroke(separator, scheme);
                    AssertStroke(nestedSeparator, scheme);
                    AssertBrush(Layout(disabled).Background, scheme.PrimaryColorHexValue);
                    AssertBrush(Header(disabled).Foreground, scheme.TextColorHexValue, 0.65);
                    AssertBrush(nested.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>()
                        .Single(p => p.Name == "PART_ChevronPath").Fill, scheme.TextColorHexValue);
                    Assert.That(unrelated.Background, Is.SameAs(originalBrush), "Keep resources local to the main menu.");
                    var normal = (MenuItem)parent.Items[0]!;
                    normal.IsSelected = true;
                    Flush();
                    AssertBrush(Layout(normal).Background, scheme.HoverBackgroundColorHexValue);
                    AssertBrush(Header(normal).Foreground, scheme.HoverTextColorHexValue);
                    normal.IsSelected = false;
                    Flush();
                    AssertBrush(Layout(normal).Background, scheme.PrimaryColorHexValue);
                    AssertBrush(Header(normal).Foreground, scheme.TextColorHexValue);
                    AssertBrush(normal.GetVisualDescendants().OfType<TextBlock>()
                        .Single(p => p.Name == "PART_InputGestureText").Foreground, scheme.TextColorHexValue);
                    AssertPopupPixels(parent, separator, scheme);
                    nested.IsSelected = true;
                    Flush();
                    AssertBrush(Layout(nested).Background, scheme.HoverBackgroundColorHexValue);
                    AssertBrush(nested.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>()
                        .Single(p => p.Name == "PART_ChevronPath").Fill, scheme.HoverTextColorHexValue);
                    SavePopup(parent, $"{theme.Key}-{color}-nested-selected");
                    nested.IsSelected = false;
                    Flush();
                    SavePopup(parent, $"{theme.Key}-{color}-popup");
                }
                parent.IsSubMenuOpen = false;
                Flush();
                parent.IsSubMenuOpen = true;
                Flush();
                AssertPopup(parent, new GuacamoleColorScheme("DarkGray"));
            }
        }
        finally { window.Close(); otherWindow.Close(); }
    }

    [AvaloniaTest]
    public void PointerAndPressedStates_UseProfileColors()
    {
        // Real pointer input catches Fluent states which override MenuItem.Background.
        var (window, menu, parent, nested, _, _, _) = CreateMenu();
        try
        {
            foreach (var color in new[] { "Blue", "Yellow", "LightGray" })
            {
                var scheme = new GuacamoleColorScheme(color);
                ProfileMenuAppearance.Apply(menu, scheme);
                parent.IsSubMenuOpen = true;
                Flush();
                var item = (MenuItem)parent.Items[0]!;
                var root = TopLevel.GetTopLevel(item)!;
                var position = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), root)!.Value;
                root.MouseMove(position);
                AssertBrush(Layout(item).Background, scheme.HoverBackgroundColorHexValue);
                AssertBrush(Header(item).Foreground, scheme.HoverTextColorHexValue);
                SavePopup(parent, $"{color}-hover");
                root.MouseDown(position, MouseButton.Left);
                AssertBrush(Layout(item).Background, scheme.SelectedItemBackgroundColorHexValue);
                AssertBrush(Header(item).Foreground, scheme.SelectedItemTextColorHexValue);
                SavePopup(parent, $"{color}-pressed");
                root.MouseUp(position, MouseButton.Left);
                parent.IsSubMenuOpen = true;
                nested.IsSubMenuOpen = true;
                Flush();
                position = nested.TranslatePoint(new Point(nested.Bounds.Width / 2, nested.Bounds.Height / 2), root)!.Value;
                root.MouseMove(position);
                root.MouseDown(position, MouseButton.Left);
                AssertBrush(Layout(nested).Background, scheme.SelectedItemBackgroundColorHexValue);
                AssertBrush(nested.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>()
                    .Single(p => p.Name == "PART_ChevronPath").Fill, scheme.SelectedItemTextColorHexValue);
                SavePopup(parent, $"{color}-nested-pressed");
                root.MouseUp(position, MouseButton.Left);
                parent.IsSubMenuOpen = false;
                Flush();
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaTest]
    public void MenuWithoutProfile_UsesFluentDefaults()
    {
        // The main menu also exists before a server profile has been chosen.
        var (window, _, parent, _, separator, _, disabled) = CreateMenu();
        try
        {
            parent.IsSubMenuOpen = true;
            Flush();
            Assert.That(((Border)Popup(parent).Child!).Background, Is.Not.Null);
            Assert.That(separator.Background, Is.Not.Null);
            Assert.That(Header(disabled).Foreground, Is.Not.Null);
        }
        finally { window.Close(); }
    }

    private static (Window, Menu, MenuItem, MenuItem, Separator, Separator, MenuItem) CreateMenu()
    {
        var separator = new Separator();
        var nestedSeparator = new Separator();
        var nested = new MenuItem { Header = "Nested menu", Items = {
            new MenuItem { Header = "Nested action" }, nestedSeparator, new MenuItem { Header = "Other action" } } };
        var disabled = new MenuItem { Header = "Disabled action", IsEnabled = false };
        var parent = new MenuItem { Header = "Connection", Items = {
            new MenuItem { Header = "Home", InputGesture = new KeyGesture(Key.H, KeyModifiers.Control) }, separator, nested, disabled } };
        var menu = new Menu { Items = { parent } };
        menu.Styles.Add((Styles)AvaloniaXamlLoader.Load(new Uri("avares://GuacamoleClient.Avalonia.Tests/ProfileMenuStyles.axaml")));
        var window = new Window { Width = 800, Height = 450, Content = new DockPanel { LastChildFill = false, Children = { menu } } };
        DockPanel.SetDock(menu, Dock.Top);
        window.Show();
        return (window, menu, parent, nested, separator, nestedSeparator, disabled);
    }

    private static void Flush()
    {
        // Popup placement and layout may schedule another pass after the first frame.
        for (var pass = 0; pass < 3; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    private static Popup Popup(MenuItem item) => item.GetVisualDescendants().OfType<Popup>().Single(p => p.Name == "PART_Popup");
    private static Border Layout(MenuItem item) => item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_LayoutRoot");
    private static ContentPresenter Header(MenuItem item) => item.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_HeaderPresenter");

    private static void AssertPopup(MenuItem item, GuacamoleColorScheme scheme)
    {
        var border = (Border)Popup(item).Child!;
        AssertBrush(border.Background, scheme.PrimaryColorHexValue);
        Assert.That(border.GetVisualDescendants().OfType<ItemsPresenter>().Single().Margin.Top, Is.GreaterThan(0), "Keep Fluent popup spacing.");
        AssertBrush(border.BorderBrush, scheme.TextColorHexValue, 0.5);
    }

    private static void AssertStroke(Separator separator, GuacamoleColorScheme scheme)
    {
        var line = separator.GetVisualDescendants().OfType<Border>().Single();
        AssertBrush(line.Background, scheme.TextColorHexValue, 0.5);
        Assert.That(separator.Margin.Top + separator.Margin.Bottom, Is.GreaterThan(0), "Keep space around the stroke.");
        Assert.That(line.Bounds.Height, Is.EqualTo(1), "Do not paint the whole separator row as a stroke.");
    }

    private static void AssertBrush(IBrush? brush, string hex, double opacity = 1)
    {
        Assert.That(brush, Is.InstanceOf<ISolidColorBrush>());
        var solid = (ISolidColorBrush)brush!;
        Assert.That(solid.Color, Is.EqualTo(Color.Parse(hex)));
        Assert.That(solid.Opacity, Is.EqualTo(opacity));
    }

    private static void AssertPopupPixels(MenuItem item, Separator separator, GuacamoleColorScheme scheme)
    {
        // Check rendered pixels in the separator margin and popup edge, where the original
        // unprofiled presenter leaked through even when the menu items were colored correctly.
        var border = (Border)Popup(item).Child!;
        var root = TopLevel.GetTopLevel(border)!;
        using var frame = root.CaptureRenderedFrame();
        Assert.That(frame, Is.Not.Null);
        Assert.That(frame!.Format, Is.EqualTo(PixelFormat.Rgba8888).Or.EqualTo(PixelFormat.Bgra8888));
        using var buffer = frame.Lock();
        var margin = separator.TranslatePoint(new Point(separator.Bounds.Width / 2, -separator.Margin.Top / 2), root)!.Value;
        var edge = border.TranslatePoint(new Point(2, border.Bounds.Height / 2), root)!.Value;
        foreach (var point in new[] { margin, edge })
        {
            var x = (int)(point.X * root.RenderScaling);
            var y = (int)(point.Y * root.RenderScaling);
            var pixel = unchecked((uint)Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4));
            var actual = Color.FromUInt32(pixel);
            if (frame.Format == PixelFormat.Rgba8888)
                actual = Color.FromArgb(actual.A, actual.B, actual.G, actual.R);
            Assert.That(actual, Is.EqualTo(Color.Parse(scheme.PrimaryColorHexValue)),
                "Popup edges and separator spacing must render in the profile background.");
        }
    }

    private static void SavePopup(MenuItem item, string name)
    {
        // Optional screenshots support visual review without storing generated images in Git.
        var directory = Environment.GetEnvironmentVariable("GUACAMOLE_MENU_SCREENSHOTS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        using var frame = TopLevel.GetTopLevel(Popup(item).Child!)!.CaptureRenderedFrame();
        Assert.That(frame, Is.Not.Null);
        frame!.Save(Path.Combine(directory, name + ".png"));
    }
}
