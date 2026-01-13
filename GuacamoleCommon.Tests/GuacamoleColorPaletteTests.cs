using System;
using GuacamoleClient.Common.Settings;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class GuacamoleColorPaletteTests
{
    [Test]
    public void ResolveToHex_PaletteKey_Works()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GuacamoleColorPalette.ResolveToHex("Red"), Is.EqualTo(GuacamoleColorPalette.GetHexOrThrow("Red")));
            Assert.That(GuacamoleColorPalette.ResolveToHex("OrangeRed"), Is.EqualTo(GuacamoleColorPalette.GetHexOrThrow("OrangeRed")));
            foreach (var kvp in GuacamoleColorPalette.Colors)
            {
                Assert.That(GuacamoleColorPalette.ResolveToHex(kvp.Key), Is.EqualTo(kvp.Value), $"Palette key {kvp.Key} should resolve to {kvp.Value}");
            }
        });
    }

    [Test]
    public void ResolveToHex_CustomHex_Normalizes()
    {
        Assert.That(GuacamoleColorPalette.ResolveToHex("#a1b2c3"), Is.EqualTo("#A1B2C3"));
        Assert.That(GuacamoleColorPalette.ResolveToHex("a1b2c3"), Is.EqualTo("#A1B2C3"));
    }

    [Test]
    public void ResolveToHex_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => GuacamoleColorPalette.ResolveToHex("not-a-color"));
    }

    public static IEnumerable<string> ResolveAllPaletteKeysInServerProfilesApplyDependingColors_KnownColorKeys_KeyList() => GuacamoleColorPalette.Keys;

    [Test]
    public void ResolveAllPaletteKeysInServerProfilesApplyDependingColors_KnownColorKeys_Works([ValueSource("ResolveAllPaletteKeysInServerProfilesApplyDependingColors_KnownColorKeys_KeyList")] string paletteKey)
    {
        string key = paletteKey;
        var profile = new GuacamoleServerProfile("https://example.invalid/guacamole/", "TestProfile", key, false, false);
        Assert.That(GuacamoleColorPalette.ResolveToHex(profile.PrimaryColorValue), Is.EqualTo(GuacamoleColorPalette.GetHexOrThrow(key)));
        var colorScheme = profile.LookupColorScheme();
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.TextColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.InactiveTextColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.HoverTextColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.HoverBackgroundColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.SelectedItemTextColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.SelectedItemBackgroundColorValue), Throws.Nothing);
    }

    [Test]
    public void ResolveAllPaletteKeysInServerProfilesApplyDependingColors_CustomColor_Works()
    {
        string key = "#123ABC";
        var profile = new GuacamoleServerProfile("https://example.invalid/guacamole/", "TestProfile", key, false, false);
        Assert.That(GuacamoleColorPalette.ResolveToHex(profile.PrimaryColorValue), Is.EqualTo(key));
        var colorScheme = profile.LookupColorScheme();
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.TextColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.InactiveTextColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.HoverTextColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.HoverBackgroundColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.SelectedItemTextColorValue), Throws.Nothing);
        Assert.That(() => GuacamoleColorPalette.ResolveToHex(colorScheme.SelectedItemBackgroundColorValue), Throws.Nothing);
    }
}
