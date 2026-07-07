using GuacamoleClient.Common.Updates;
using NUnit.Framework;
using System.Text.Json;

namespace GuacamoleCommon.Tests;

public class ClickOnceCleanupScannerTests
{
    [Test]
    public void ShouldScan_ReturnsFalse_ForLocalDebugMajorZeroVersion()
    {
        var appInfo = CreateAppInfo("0.1.0");

        Assert.That(ClickOnceCleanupScanner.ShouldScan(appInfo), Is.False);
    }

    [Test]
    public void ShouldScan_ReturnsTrue_ForOfficialClickOnceVersion()
    {
        var appInfo = CreateAppInfo("2026.7.7");

        Assert.That(ClickOnceCleanupScanner.ShouldScan(appInfo), Is.True);
    }

    [Test]
    public void DiscoverCandidates_ReturnsOnlyOlderMatchingSiblingDirectories()
    {
        string root = CreateTempDirectory();
        string current = Path.Combine(root, "current");
        string older = Path.Combine(root, "older");
        string sameVersion = Path.Combine(root, "same");
        string otherChannel = Path.Combine(root, "other-channel");
        Directory.CreateDirectory(current);
        Directory.CreateDirectory(older);
        Directory.CreateDirectory(sameVersion);
        Directory.CreateDirectory(otherChannel);
        WriteAppInfo(current, CreateAppInfo("2026.7.7"));
        WriteAppInfo(older, CreateAppInfo("2026.7.6"));
        WriteAppInfo(sameVersion, CreateAppInfo("2026.7.7"));
        WriteAppInfo(otherChannel, CreateAppInfo("2026.7.6", channel: "dev"));
        var scanner = new ClickOnceCleanupScanner(CreateAppInfo("2026.7.7"), current);

        IReadOnlyList<ClickOnceCleanupCandidate> candidates = scanner.DiscoverCandidates(
            new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Path, Is.EqualTo(older));
            Assert.That(candidates[0].Version, Is.EqualTo("2026.7.6"));
        });
    }

    [Test]
    public void DiscoverCandidates_ReturnsNoCandidates_ForMajorZeroBuild()
    {
        string root = CreateTempDirectory();
        string current = Path.Combine(root, "current");
        string older = Path.Combine(root, "older");
        Directory.CreateDirectory(current);
        Directory.CreateDirectory(older);
        WriteAppInfo(current, CreateAppInfo("0.2.0"));
        WriteAppInfo(older, CreateAppInfo("0.1.0"));
        var scanner = new ClickOnceCleanupScanner(CreateAppInfo("0.2.0"), current);

        IReadOnlyList<ClickOnceCleanupCandidate> candidates = scanner.DiscoverCandidates(DateTimeOffset.UtcNow);

        Assert.That(candidates, Is.Empty);
    }

    private static AppInfo CreateAppInfo(string version, string channel = "stable")
        => new()
        {
            AppId = "winforms",
            Channel = channel,
            DeploymentType = "clickonce",
            Version = version,
            UpdatesUrl = AppInfo.DefaultUpdatesUrl
        };

    private static void WriteAppInfo(string directory, AppInfo appInfo)
    {
        string json = JsonSerializer.Serialize(appInfo, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(directory, "app-info.json"), json);
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "GuacamoleClientTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
