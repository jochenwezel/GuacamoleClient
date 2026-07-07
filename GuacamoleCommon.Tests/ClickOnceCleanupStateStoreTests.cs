using GuacamoleClient.Common.Updates;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class ClickOnceCleanupStateStoreTests
{
    [Test]
    public async Task SaveOrDeleteIfEmptyAsync_DeletesStateFile_WhenNoCandidatesRemain()
    {
        string dir = CreateTempDirectory();
        string path = Path.Combine(dir, ClickOnceCleanupStateStore.FileName);
        await File.WriteAllTextAsync(path, "{}");
        var store = new ClickOnceCleanupStateStore(path);

        await store.SaveOrDeleteIfEmptyAsync(new ClickOnceCleanupState());

        Assert.That(File.Exists(path), Is.False);
    }

    [Test]
    public async Task AddPendingCandidateAsync_ReplacesCandidate_ForSameAppChannelDeploymentAndPath()
    {
        string dir = CreateTempDirectory();
        var store = new ClickOnceCleanupStateStore(Path.Combine(dir, ClickOnceCleanupStateStore.FileName));
        var older = CreateCandidate(Path.Combine(dir, "old"), "1.0.0");
        var newer = CreateCandidate(Path.Combine(dir, ".", "old"), "1.1.0");

        await store.AddPendingCandidateAsync(older);
        await store.AddPendingCandidateAsync(newer);
        ClickOnceCleanupState state = await store.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(state.PendingCandidates, Has.Count.EqualTo(1));
            Assert.That(state.PendingCandidates[0].Version, Is.EqualTo("1.1.0"));
        });
    }

    [Test]
    public void RemoveExpiredPendingCandidates_RemovesOnlyCandidatesOlderThanMaxAge()
    {
        DateTimeOffset now = new(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);
        var state = new ClickOnceCleanupState
        {
            PendingCandidates =
            [
                CreateCandidate("C:\\Apps\\old", "1.0.0", now.AddHours(-25)),
                CreateCandidate("C:\\Apps\\fresh", "1.1.0", now.AddHours(-23))
            ]
        };

        int removed = ClickOnceCleanupStateStore.RemoveExpiredPendingCandidates(state, now, TimeSpan.FromDays(1));

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(state.PendingCandidates.Single().Path, Does.Contain("fresh"));
        });
    }

    [Test]
    public void PathsOverlap_ReturnsTrue_ForSamePathAndChildPaths()
    {
        string root = Path.Combine(Path.GetTempPath(), "GuacamoleClient", "Apps", "current");

        Assert.Multiple(() =>
        {
            Assert.That(ClickOnceCleanupStateStore.PathsOverlap(root, root + Path.DirectorySeparatorChar), Is.True);
            Assert.That(ClickOnceCleanupStateStore.PathsOverlap(Path.Combine(root, "child"), root), Is.True);
            Assert.That(ClickOnceCleanupStateStore.PathsOverlap(root, Path.Combine(root, "child")), Is.True);
        });
    }

    [Test]
    public void PathsOverlap_ReturnsFalse_ForSiblingPaths()
    {
        string root = Path.Combine(Path.GetTempPath(), "GuacamoleClient", "Apps");

        Assert.That(
            ClickOnceCleanupStateStore.PathsOverlap(
                Path.Combine(root, "old"),
                Path.Combine(root, "current")),
            Is.False);
    }

    private static ClickOnceCleanupCandidate CreateCandidate(
        string path,
        string version,
        DateTimeOffset? createdUtc = null)
        => new()
        {
            AppId = "winforms",
            Channel = "stable",
            DeploymentType = "clickonce",
            Version = version,
            Path = path,
            CreatedUtc = createdUtc ?? DateTimeOffset.UtcNow
        };

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "GuacamoleClientTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
