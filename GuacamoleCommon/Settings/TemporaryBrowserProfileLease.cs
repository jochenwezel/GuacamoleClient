using System;
using System.IO;

namespace GuacamoleClient.Common.Settings
{
    /// <summary>
    /// Keeps a temporary browser profile alive while one or more windows use it.
    /// </summary>
    public sealed class TemporaryBrowserProfileLease : IDisposable
    {
        private readonly SharedState _state;
        private bool _disposed;

        private TemporaryBrowserProfileLease(SharedState state)
        {
            _state = state;
        }

        /// <summary>
        /// Gets the directory used by the temporary browser profile.
        /// </summary>
        public string DirectoryPath => _state.DirectoryPath;

        /// <summary>
        /// Creates another lease for the same temporary browser profile.
        /// </summary>
        /// <returns>A lease that keeps the profile directory alive independently.</returns>
        /// <exception cref="ObjectDisposedException">The current lease has already been disposed.</exception>
        public TemporaryBrowserProfileLease Share()
        {
            lock (_state.SyncRoot)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _state.ReferenceCount++;
                return new TemporaryBrowserProfileLease(_state);
            }
        }

        /// <summary>
        /// Releases this lease and deletes the temporary profile after the last lease is released.
        /// </summary>
        public void Dispose()
        {
            bool deleteDirectory;
            lock (_state.SyncRoot)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _state.ReferenceCount--;
                deleteDirectory = _state.ReferenceCount == 0;
            }

            if (deleteDirectory)
                GuacamoleBrowserCache.DeleteDirectoryIfExists(_state.DirectoryPath);
        }

        internal static TemporaryBrowserProfileLease CreateForDirectory(string directoryPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
            Directory.CreateDirectory(directoryPath);
            return new TemporaryBrowserProfileLease(new SharedState(directoryPath));
        }

        private sealed class SharedState
        {
            public SharedState(string directoryPath)
            {
                DirectoryPath = directoryPath;
            }

            public object SyncRoot { get; } = new();

            public string DirectoryPath { get; }

            public int ReferenceCount { get; set; } = 1;
        }
    }
}
