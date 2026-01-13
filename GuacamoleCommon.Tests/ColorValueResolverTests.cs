using System;
using GuacamoleClient.Common.Settings;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class ColorValueResolverTests
{
    [Test]
    public void ResolveToHex_PaletteKey_Works()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ColorValueResolver.ResolveToHex("Red"), Is.EqualTo(GuacamoleColorPalette.GetHexOrThrow("Red")));
            Assert.That(ColorValueResolver.ResolveToHex("OrangeRed"), Is.EqualTo(GuacamoleColorPalette.GetHexOrThrow("OrangeRed")));
            foreach (var kvp in GuacamoleColorPalette.Colors)
            {
                Assert.That(ColorValueResolver.ResolveToHex(kvp.Key), Is.EqualTo(kvp.Value), $"Palette key {kvp.Key} should resolve to {kvp.Value}");
            }
        });
    }

    [Test]
    public void ResolveToHex_CustomHex_Normalizes()
    {
        Assert.That(ColorValueResolver.ResolveToHex("#a1b2c3"), Is.EqualTo("#A1B2C3"));
        Assert.That(ColorValueResolver.ResolveToHex("a1b2c3"), Is.EqualTo("#A1B2C3"));
    }

    [Test]
    public void ResolveToHex_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => ColorValueResolver.ResolveToHex("not-a-color"));
    }
}
