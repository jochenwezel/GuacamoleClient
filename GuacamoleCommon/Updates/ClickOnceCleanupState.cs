using GuacamoleClient.Common.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GuacamoleClient.Common.Updates
{
    /// <summary>
    /// Stores pending ClickOnce cleanup candidates.
    /// </summary>
    public sealed class ClickOnceCleanupStateStore
    {
        /// <summary>
        /// Gets the file name used for ClickOnce cleanup state.
        /// </summary>
        public const string FileName = "clickonce-cleanup-state.json";

        private readonly string _statePath;

        /// <summary>
        /// Initializes a new ClickOnce cleanup state store.
        /// </summary>
        /// <param name="statePath">The JSON file path used to store cleanup state.</param>
        public ClickOnceCleanupStateStore(string statePath)
        {
            if (string.IsNullOrWhiteSpace(statePath))
                throw new ArgumentException("State path must not be empty.", nameof(statePath));

            _statePath = statePath;
        }

        /// <summary>
        /// Gets the JSON file path used to store cleanup state.
        /// </summary>
        public string StatePath => _statePath;

        /// <summary>
        /// Creates a cleanup state store below the user application data directory.
        /// </summary>
        /// <param name="settingsAppName">The settings application name used below the user application data directory.</param>
        /// <returns>The cleanup state store for the specified settings application name.</returns>
        public static ClickOnceCleanupStateStore CreateForSettingsAppName(string settingsAppName)
            => new(Path.Combine(GuacamoleSettingsPaths.GetDefaultSettingsDirectory(settingsAppName), FileName));

        /// <summary>
        /// Adds or replaces a pending cleanup candidate.
        /// </summary>
        /// <param name="candidate">The cleanup candidate to store.</param>
        /// <param name="cancellationToken">A token that cancels the write operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task AddPendingCandidateAsync(
            ClickOnceCleanupCandidate candidate,
            CancellationToken cancellationToken = default)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            ClickOnceCleanupState state = await LoadAsync(cancellationToken).ConfigureAwait(false);
            state.PendingCandidates.RemoveAll(existing => AreSameCandidate(existing, candidate));
            state.PendingCandidates.Add(candidate);
            await SaveOrDeleteIfEmptyAsync(state, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads cleanup state.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the read operation.</param>
        /// <returns>The loaded cleanup state, or an empty state when no state exists.</returns>
        public async Task<ClickOnceCleanupState> LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(_statePath))
                    return new ClickOnceCleanupState();

                string json = await File.ReadAllTextAsync(_statePath, cancellationToken).ConfigureAwait(false);
                ClickOnceCleanupState? state = JsonSerializer.Deserialize<ClickOnceCleanupState>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return state?.Normalize() ?? new ClickOnceCleanupState();
            }
            catch
            {
                return new ClickOnceCleanupState();
            }
        }

        /// <summary>
        /// Saves cleanup state or deletes the state file when it is empty.
        /// </summary>
        /// <param name="state">The cleanup state to persist.</param>
        /// <param name="cancellationToken">A token that cancels the write operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SaveOrDeleteIfEmptyAsync(
            ClickOnceCleanupState state,
            CancellationToken cancellationToken = default)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            state = state.Normalize();
            if (state.IsEmpty)
            {
                if (File.Exists(_statePath))
                    File.Delete(_statePath);

                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
            string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_statePath, json, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes expired pending candidates.
        /// </summary>
        /// <param name="state">The cleanup state to update.</param>
        /// <param name="nowUtc">The current UTC timestamp.</param>
        /// <param name="maxAge">The maximum age allowed for pending candidates.</param>
        /// <returns>The number of removed pending candidates.</returns>
        public static int RemoveExpiredPendingCandidates(
            ClickOnceCleanupState state,
            DateTimeOffset nowUtc,
            TimeSpan maxAge)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return state.PendingCandidates.RemoveAll(candidate => nowUtc - candidate.CreatedUtc > maxAge);
        }

        /// <summary>
        /// Determines whether two paths point to the same file system location.
        /// </summary>
        /// <param name="left">The first path to compare.</param>
        /// <param name="right">The second path to compare.</param>
        /// <returns><see langword="true"/> when both paths resolve to the same location.</returns>
        public static bool AreSamePath(string? left, string? right)
        {
            string? normalizedLeft = TryNormalizePath(left);
            string? normalizedRight = TryNormalizePath(right);
            return normalizedLeft != null
                && normalizedRight != null
                && string.Equals(normalizedLeft, normalizedRight, PathComparison);
        }

        /// <summary>
        /// Determines whether a candidate path contains, or is contained by, the current application path.
        /// </summary>
        /// <param name="candidatePath">The cleanup candidate path.</param>
        /// <param name="currentPath">The current application path.</param>
        /// <returns><see langword="true"/> when the paths overlap.</returns>
        public static bool PathsOverlap(string? candidatePath, string? currentPath)
        {
            string? candidate = TryNormalizePath(candidatePath);
            string? current = TryNormalizePath(currentPath);
            if (candidate == null || current == null)
                return false;

            return IsSameOrChildPath(candidate, current) || IsSameOrChildPath(current, candidate);
        }

        private static bool AreSameCandidate(ClickOnceCleanupCandidate left, ClickOnceCleanupCandidate right)
            => string.Equals(left.AppId, right.AppId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Channel, right.Channel, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.DeploymentType, right.DeploymentType, StringComparison.OrdinalIgnoreCase)
                && AreSamePath(left.Path, right.Path);

        private static string? TryNormalizePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSameOrChildPath(string path, string possibleParent)
        {
            if (string.Equals(path, possibleParent, PathComparison))
                return true;

            string parentWithSeparator = possibleParent + Path.DirectorySeparatorChar;
            return path.StartsWith(parentWithSeparator, PathComparison);
        }

        private static StringComparison PathComparison
            => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    /// <summary>
    /// Represents ClickOnce cleanup state persisted for the current user.
    /// </summary>
    public sealed class ClickOnceCleanupState
    {
        /// <summary>
        /// Gets the cleanup state schema version.
        /// </summary>
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; init; } = 1;

        /// <summary>
        /// Gets pending cleanup candidates.
        /// </summary>
        [JsonPropertyName("pendingCandidates")]
        public List<ClickOnceCleanupCandidate> PendingCandidates { get; init; } = new();

        /// <summary>
        /// Gets ignored cleanup candidates.
        /// </summary>
        [JsonPropertyName("ignoredCandidates")]
        public List<ClickOnceIgnoredCleanupCandidate> IgnoredCandidates { get; init; } = new();

        /// <summary>
        /// Gets a value indicating whether the state contains no candidates.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => PendingCandidates.Count == 0 && IgnoredCandidates.Count == 0;

        /// <summary>
        /// Normalizes nullable state collections and schema values.
        /// </summary>
        /// <returns>The normalized cleanup state.</returns>
        public ClickOnceCleanupState Normalize()
            => new()
            {
                SchemaVersion = SchemaVersion <= 0 ? 1 : SchemaVersion,
                PendingCandidates = PendingCandidates.Where(candidate => candidate != null).ToList(),
                IgnoredCandidates = IgnoredCandidates.Where(candidate => candidate != null).ToList()
            };
    }

    /// <summary>
    /// Represents an application directory that may be removed after a ClickOnce update.
    /// </summary>
    public sealed class ClickOnceCleanupCandidate
    {
        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        [JsonPropertyName("appId")]
        public required string AppId { get; init; }

        /// <summary>
        /// Gets the release channel.
        /// </summary>
        [JsonPropertyName("channel")]
        public required string Channel { get; init; }

        /// <summary>
        /// Gets the deployment type.
        /// </summary>
        [JsonPropertyName("deploymentType")]
        public required string DeploymentType { get; init; }

        /// <summary>
        /// Gets the application version installed in the candidate directory.
        /// </summary>
        [JsonPropertyName("version")]
        public required string Version { get; init; }

        /// <summary>
        /// Gets the application directory path.
        /// </summary>
        [JsonPropertyName("path")]
        public required string Path { get; init; }

        /// <summary>
        /// Gets the UTC timestamp when the user confirmed the update.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTimeOffset CreatedUtc { get; init; }
    }

    /// <summary>
    /// Represents a ClickOnce cleanup candidate the user chose to ignore.
    /// </summary>
    public sealed class ClickOnceIgnoredCleanupCandidate
    {
        /// <summary>
        /// Gets the ignored application directory path.
        /// </summary>
        [JsonPropertyName("path")]
        public required string Path { get; init; }

        /// <summary>
        /// Gets the UTC timestamp when the user ignored the candidate.
        /// </summary>
        [JsonPropertyName("ignoredUtc")]
        public DateTimeOffset IgnoredUtc { get; init; }
    }
}
