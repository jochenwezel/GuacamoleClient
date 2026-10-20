using GuacamoleClient.Common.Settings;

namespace GuacClient;

internal sealed class NativeProfileSessions
{
    internal const string SessionLockFileName = ".native-pilot-session.lock";
    private readonly string _root;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Entry> _privateProfiles = [];

    internal NativeProfileSessions(string root)
    {
        _root = Path.GetFullPath(root);
        CleanupOrphanedPrivateSessions();
    }

    internal Lease Acquire(Guid profileId, bool persistent)
    {
        if (persistent)
        {
            string path = Path.Combine(_root, "Persistent", profileId.ToString("N"));
            Directory.CreateDirectory(path);
            return new Lease(path, false, () => { });
        }
        lock (_sync)
        {
            if (!_privateProfiles.TryGetValue(profileId, out var entry))
            {
                string path = Path.Combine(_root, "Private", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);
                var sessionLock = new FileStream(Path.Combine(path, SessionLockFileName), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                entry = new Entry(path, sessionLock);
                _privateProfiles.Add(profileId, entry);
            }
            entry.Users++;
            return new Lease(entry.Path, true, () => Release(profileId, entry));
        }
    }

    private void Release(Guid profileId, Entry entry)
    {
        lock (_sync)
        {
            if (--entry.Users != 0) return;
            _privateProfiles.Remove(profileId);
        }
        entry.SessionLock.Dispose();
        DeleteRetiredDirectory(entry.Path);
        if (Directory.Exists(entry.Path))
            _ = RetryCleanupAsync(entry.Path);
    }

    private void CleanupOrphanedPrivateSessions()
    {
        string privateRoot = Path.Combine(_root, "Private");
        Directory.CreateDirectory(privateRoot);
        foreach (string path in Directory.EnumerateDirectories(privateRoot))
        {
            try
            {
                if (!Guid.TryParseExact(Path.GetFileName(path), "N", out _)
                    || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    continue;
                string marker = Path.Combine(path, SessionLockFileName);
                // Unmarked directories may belong to an older still-running pilot.
                // Never infer that they are abandoned merely from their age.
                if (!File.Exists(marker)) continue;
                using (new FileStream(marker, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                // Each new session gets a new GUID, so an abandoned path is never reused.
                DeleteRetiredDirectory(path);
            }
            catch (IOException) { /* Another process or a browser still owns files. */ }
            catch (UnauthorizedAccessException) { /* Leave inaccessible data untouched. */ }
        }
    }

    private static void DeleteRetiredDirectory(string path)
    {
        GuacamoleBrowserCache.DeleteDirectoryIfExists(path);
        if (!Directory.Exists(path)) return;
        try
        {
            // Recursive deletion may remove the marker before encountering a
            // locked browser file. Restore it so a later launch can retry.
            using var marker = new FileStream(Path.Combine(path, SessionLockFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static async Task RetryCleanupAsync(string path)
    {
        // Native browser subprocesses can keep files locked briefly after detachment.
        for (int attempt = 0; attempt < 30 && Directory.Exists(path); attempt++)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            DeleteRetiredDirectory(path);
        }
    }

    private sealed class Entry(string path, FileStream sessionLock)
    {
        internal string Path { get; } = path;
        internal FileStream SessionLock { get; } = sessionLock;
        internal int Users { get; set; }
    }

    internal sealed class Lease(string path, bool isPrivate, Action release) : IDisposable
    {
        private int _disposed;
        internal string DirectoryPath { get; } = path;
        internal bool IsPrivate { get; } = isPrivate;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) release();
        }
    }
}
