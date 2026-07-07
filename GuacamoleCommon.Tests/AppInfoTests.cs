using GuacamoleClient.Common.Updates;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class AppInfoTests
{
    [TestCase("linux-deb-repository")]
    [TestCase("linux-rpm")]
    [TestCase("snap")]
    [TestCase("flatpak")]
    public void SuppressAutomaticUpdateChecks_IsTrue_ForPackageManagerDeployments(string deploymentType)
    {
        var appInfo = new AppInfo { DeploymentType = deploymentType };

        Assert.That(appInfo.SuppressAutomaticUpdateChecks, Is.True);
    }

    [TestCase("zip")]
    [TestCase("clickonce")]
    [TestCase("portable")]
    [TestCase("local-dev")]
    [TestCase("linux-deb-direct")]
    public void SuppressAutomaticUpdateChecks_IsFalse_ForSelfManagedDeployments(string deploymentType)
    {
        var appInfo = new AppInfo { DeploymentType = deploymentType };

        Assert.That(appInfo.SuppressAutomaticUpdateChecks, Is.False);
    }
}
