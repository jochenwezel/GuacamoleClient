using Avalonia.Input;
using GuacClient;
using NUnit.Framework;
using System.Text.Json;

namespace GuacamoleClient.NativeBrowserPilot.Tests;

public class NativeBrowserPolicyTests
{
    private string _root = "";

    [SetUp]
    public void CreateRoot() => _root = Path.Combine(Path.GetTempPath(), "NativeBrowserPilot.Tests", Guid.NewGuid().ToString("N"));

    [TearDown]
    public void RemoveRoot()
    {
        string allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "NativeBrowserPilot.Tests")) + Path.DirectorySeparatorChar;
        string target = Path.GetFullPath(_root);
        Assert.That(target.StartsWith(allowed, StringComparison.OrdinalIgnoreCase), Is.True);
        if (Directory.Exists(target)) Directory.Delete(target, true);
    }

    [Test]
    public void PrivateSibling_KeepsContextUntilLastWindowCloses()
    {
        // Closing the originating window must not invalidate its sibling's login.
        var sessions = new NativeProfileSessions(_root);
        Guid id = Guid.NewGuid();
        var first = sessions.Acquire(id, false);
        var sibling = sessions.Acquire(id, false);
        string path = first.DirectoryPath;
        Assert.That(sibling.DirectoryPath, Is.EqualTo(path));
        first.Dispose();
        first.Dispose();
        Assert.That(Directory.Exists(path), Is.True);
        sibling.Dispose();
        Assert.That(Directory.Exists(path), Is.False);
        using var reopened = sessions.Acquire(id, false);
        Assert.That(reopened.DirectoryPath, Is.Not.EqualTo(path));
    }

    [Test]
    public void PrivateProfiles_AndPersistentPolicy_AreIndependent()
    {
        // A secure profile must not inherit a persistent context from window order.
        var sessions = new NativeProfileSessions(_root);
        Guid id = Guid.NewGuid();
        using var persistent = sessions.Acquire(id, true);
        using var secure = sessions.Acquire(id, false);
        using var other = sessions.Acquire(Guid.NewGuid(), false);
        Assert.That(new[] { persistent.DirectoryPath, secure.DirectoryPath, other.DirectoryPath }.Distinct().Count(), Is.EqualTo(3));
        Assert.That(secure.IsPrivate, Is.True);
        Assert.That(persistent.IsPrivate, Is.False);
        persistent.Dispose();
        Assert.That(Directory.Exists(persistent.DirectoryPath), Is.True);
        using var reopened = sessions.Acquire(id, true);
        Assert.That(reopened.DirectoryPath, Is.EqualTo(persistent.DirectoryPath));
    }

    [Test]
    public void StartupCleanup_KeepsAnotherOwnersActivePrivateSession()
    {
        // A second process must not erase browser data still leased by the first.
        var firstOwner = new NativeProfileSessions(_root);
        using var first = firstOwner.Acquire(Guid.NewGuid(), false);
        File.WriteAllText(Path.Combine(first.DirectoryPath, "active-data.txt"), "synthetic");
        var secondOwner = new NativeProfileSessions(_root);
        using var second = secondOwner.Acquire(Guid.NewGuid(), false);
        Assert.That(File.Exists(Path.Combine(first.DirectoryPath, "active-data.txt")), Is.True);
        Assert.That(second.DirectoryPath, Is.Not.EqualTo(first.DirectoryPath));
    }

    [Test]
    public void StartupCleanup_RemovesMarkedAbandonedSession_AndKeepsOtherData()
    {
        // Process termination releases the marker handle even when browser files
        // prevented final-window deletion. Only marked GUID directories are ours.
        string abandoned = Path.Combine(_root, "Private", Guid.NewGuid().ToString("N"));
        string legacy = Path.Combine(_root, "Private", Guid.NewGuid().ToString("N"));
        string unrelated = Path.Combine(_root, "Private", "unrelated");
        string persistent = Path.Combine(_root, "Persistent", Guid.NewGuid().ToString("N"));
        foreach (string path in new[] { abandoned, legacy, unrelated, persistent })
        {
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "synthetic-data.txt"), "synthetic");
        }
        foreach (string path in new[] { abandoned, unrelated, persistent })
            File.WriteAllText(Path.Combine(path, NativeProfileSessions.SessionLockFileName), "");
        _ = new NativeProfileSessions(_root);
        Assert.That(Directory.Exists(abandoned), Is.False);
        foreach (string path in new[] { legacy, unrelated, persistent })
            Assert.That(File.Exists(Path.Combine(path, "synthetic-data.txt")), Is.True);
    }

    [Test]
    public void StartupCleanup_RetriesAbandonedFilesAfterBrowserLockDisappears()
    {
        // A browser may outlive its parent briefly. Failure to delete must leave
        // the marker available so the next launch can try the same orphan again.
        string abandoned = Path.Combine(_root, "Private", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(abandoned);
        File.WriteAllText(Path.Combine(abandoned, NativeProfileSessions.SessionLockFileName), "");
        using (var browser = new FileStream(Path.Combine(abandoned, "browser-data"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        {
            _ = new NativeProfileSessions(_root);
            Assert.That(Directory.Exists(abandoned), Is.True);
            Assert.That(File.Exists(Path.Combine(abandoned, NativeProfileSessions.SessionLockFileName)), Is.True);
        }
        _ = new NativeProfileSessions(_root);
        Assert.That(Directory.Exists(abandoned), Is.False);
    }

    [TestCase(Key.LeftCtrl, 162, "ControlLeft", 1)]
    [TestCase(Key.RightCtrl, 163, "ControlRight", 2)]
    [TestCase(Key.LeftAlt, 164, "AltLeft", 1)]
    [TestCase(Key.RightAlt, 165, "AltRight", 2)]
    [TestCase(Key.LeftShift, 160, "ShiftLeft", 1)]
    [TestCase(Key.RightShift, 161, "ShiftRight", 2)]
    public void ModifierEvents_PreserveSide(Key key, int virtualKey, string code, int location)
    {
        // Left/right identity must survive transport to the native browser protocol.
        using var json = JsonDocument.Parse(NativeKeyEventBuilder.Build(key, virtualKey, "rawKeyDown", [key]));
        Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo(code));
        Assert.That(json.RootElement.GetProperty("location").GetInt32(), Is.EqualTo(location));
    }

    [Test]
    public void ShortcutEvents_DoNotInsertText_AndReleaseModifiers()
    {
        // Ctrl/Alt/Meta are CDP bits 2/1/4, unlike Avalonia or CEF enum values.
        using var down = JsonDocument.Parse(NativeKeyEventBuilder.Build(Key.F4, 115, "rawKeyDown", [Key.LeftCtrl, Key.LeftAlt, Key.LWin, Key.RightShift]));
        Assert.That(down.RootElement.GetProperty("modifiers").GetInt32(), Is.EqualTo(15));
        Assert.That(down.RootElement.GetProperty("text").GetString(), Is.Empty);
        using var up = JsonDocument.Parse(NativeKeyEventBuilder.Build(Key.RightCtrl, 163, "keyUp", []));
        Assert.That(up.RootElement.GetProperty("modifiers").GetInt32(), Is.Zero);
        Assert.That(up.RootElement.GetProperty("text").GetString(), Is.Empty);
    }

    [Test]
    public void CharacterEvents_ContainEscapedCharacterAndStablePhysicalCode()
    {
        using var json = JsonDocument.Parse(NativeKeyEventBuilder.Build(Key.A, 65, "char", [Key.LeftShift], 'A'));
        Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("KeyA"));
        Assert.That(json.RootElement.GetProperty("text").GetString(), Is.EqualTo("A"));
        Assert.That(json.RootElement.GetProperty("unmodifiedText").GetString(), Is.EqualTo("a"));
    }

    [TestCase("https://example.com/guacamole/", "https://example.com/guacamole/client", true)]
    [TestCase("https://example.com/", "https://example.com:443/", true)]
    [TestCase("https://example.com/", "https://example.com:444/", false)]
    [TestCase("https://example.com/", "http://example.com/", false)]
    [TestCase("https://example.com/", "https://example.com@attacker.example/", false)]
    [TestCase("https://example.com/", "https://example.com.attacker.example/", false)]
    [TestCase("file:///tmp/a", "file:///tmp/b", false)]
    [TestCase("https://example.com/", "not-a-url", false)]
    public void BridgeAndCertificateExceptions_AreRestrictedToExactWebOrigin(string configured, string requested, bool allowed)
    {
        Assert.That(NativeBridgeOrigin.IsSameOrigin(configured, requested), Is.EqualTo(allowed));
    }
}
