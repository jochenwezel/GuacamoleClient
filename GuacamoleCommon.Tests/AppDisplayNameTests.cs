using GuacamoleClient.Common.Updates;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class AppDisplayNameTests
{
    [TestCase("winforms", "clickonce", "stable", "GuacamoleClient (WinForms)")]
    [TestCase("winforms", "clickonce", "dev", "GuacamoleClient Dev (WinForms)")]
    [TestCase("avalonia", "clickonce", "stable", "GuacamoleClient (Avalonia)")]
    [TestCase("avalonia", "clickonce", "dev", "GuacamoleClient Dev (Avalonia)")]
    [TestCase("winforms", "clickonce", "test", "GuacamoleClient Test (WinForms)")]
    [TestCase("winforms", "local-dev", "dev", "GuacamoleClient Local Debug (WinForms)")]
    [TestCase("avalonia", "local-dev", "dev", "GuacamoleClient Local Debug (Avalonia)")]
    public void Create_DistinguishesPlatformAndEdition(
        string appId,
        string deploymentType,
        string channel,
        string expected)
    {
        Assert.That(AppDisplayName.Create(appId, deploymentType, channel), Is.EqualTo(expected));
    }
}
