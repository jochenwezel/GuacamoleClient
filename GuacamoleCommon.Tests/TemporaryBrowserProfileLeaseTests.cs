using System;
using System.IO;
using GuacamoleClient.Common.Settings;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class TemporaryBrowserProfileLeaseTests
{
    [Test]
    public void SharedProfile_IsDeletedOnlyAfterLastLeaseIsDisposed()
    {
        string directoryPath = CreateTestDirectoryPath();
        var firstLease = TemporaryBrowserProfileLease.CreateForDirectory(directoryPath);
        var secondLease = firstLease.Share();

        firstLease.Dispose();

        Assert.That(Directory.Exists(directoryPath), Is.True);

        secondLease.Dispose();

        Assert.That(Directory.Exists(directoryPath), Is.False);
    }

    [Test]
    public void Share_AfterLeaseWasDisposed_ThrowsObjectDisposedException()
    {
        string directoryPath = CreateTestDirectoryPath();
        var lease = TemporaryBrowserProfileLease.CreateForDirectory(directoryPath);
        lease.Dispose();

        Assert.That(() => lease.Share(), Throws.TypeOf<ObjectDisposedException>());
    }

    private static string CreateTestDirectoryPath()
        => Path.Combine(Path.GetTempPath(), "GuacamoleCommon.Tests", Guid.NewGuid().ToString("N"));
}
