using GuacamoleClient.Common.Settings;

namespace GuacClient;

internal sealed class NativeProfileSessions(string root)
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Entry> _privateProfiles = [];

    internal Lease Acquire(Guid profileId, bool persistent)
    {
        if (persistent)
        {
            string path = Path.Combine(root, "Persistent", profileId.ToString("N"));
            Directory.CreateDirectory(path);
            return new Lease(path, false, () => { });
        }
        lock (_sync)
        {
            if (!_privateProfiles.TryGetValue(profileId, out var entry))
            {
                string path = Path.Combine(root, "Private", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);
                entry = new Entry(path);
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
        GuacamoleBrowserCache.DeleteDirectoryIfExists(entry.Path);
        if (Directory.Exists(entry.Path))
            _ = RetryCleanupAsync(entry.Path);
    }

    private static async Task RetryCleanupAsync(string path)
    {
        // Native browser subprocesses can keep files locked briefly after detachment.
        for (int attempt = 0; attempt < 30 && Directory.Exists(path); attempt++)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            GuacamoleBrowserCache.DeleteDirectoryIfExists(path);
        }
    }

    private sealed class Entry(string path)
    {
        internal string Path { get; } = path;
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
