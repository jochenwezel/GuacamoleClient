using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GuacamoleClient.Common.Updates
{
    /// <summary>
    /// Discovers older ClickOnce application directories.
    /// </summary>
    public sealed class ClickOnceCleanupScanner
    {
        private readonly AppInfo _currentAppInfo;
        private readonly string _currentAppDirectory;

        /// <summary>
        /// Initializes a new ClickOnce cleanup scanner.
        /// </summary>
        /// <param name="currentAppInfo">The current application metadata.</param>
        /// <param name="currentAppDirectory">The current application directory.</param>
        public ClickOnceCleanupScanner(AppInfo currentAppInfo, string currentAppDirectory)
        {
            _currentAppInfo = currentAppInfo ?? throw new ArgumentNullException(nameof(currentAppInfo));
            _currentAppDirectory = currentAppDirectory ?? throw new ArgumentNullException(nameof(currentAppDirectory));
        }

        /// <summary>
        /// Discovers older cleanup candidates next to the current ClickOnce application directory.
        /// </summary>
        /// <param name="createdUtc">The UTC timestamp assigned to discovered candidates.</param>
        /// <returns>The discovered cleanup candidates.</returns>
        public IReadOnlyList<ClickOnceCleanupCandidate> DiscoverCandidates(DateTimeOffset createdUtc)
        {
            var candidates = new List<ClickOnceCleanupCandidate>();
            if (!ShouldScan(_currentAppInfo) || !Directory.Exists(_currentAppDirectory))
                return candidates;

            string? parentDirectory = Directory.GetParent(_currentAppDirectory)?.FullName;
            if (string.IsNullOrWhiteSpace(parentDirectory) || !Directory.Exists(parentDirectory))
                return candidates;

            foreach (string directory in EnumerateCandidateDirectories(parentDirectory))
            {
                if (ClickOnceCleanupStateStore.PathsOverlap(directory, _currentAppDirectory))
                    continue;

                AppInfo? appInfo = TryLoadAppInfo(directory);
                if (appInfo == null || !IsSameApp(appInfo, _currentAppInfo))
                    continue;

                if (!IsOlderVersion(appInfo.CurrentVersion, _currentAppInfo.CurrentVersion))
                    continue;

                candidates.Add(new ClickOnceCleanupCandidate
                {
                    AppId = appInfo.AppId,
                    Channel = appInfo.Channel,
                    DeploymentType = appInfo.DeploymentType,
                    Version = appInfo.CurrentVersion,
                    Path = directory,
                    CreatedUtc = createdUtc
                });
            }

            return candidates;
        }

        /// <summary>
        /// Determines whether ClickOnce sibling scanning is enabled for the specified application metadata.
        /// </summary>
        /// <param name="appInfo">The application metadata to inspect.</param>
        /// <returns><see langword="true"/> when the application is an official ClickOnce build.</returns>
        public static bool ShouldScan(AppInfo appInfo)
            => appInfo != null
                && IsClickOnceApp(appInfo)
                && TryGetMajorVersion(appInfo.CurrentVersion, out int major)
                && major != 0;

        /// <summary>
        /// Determines whether one version is older than another version.
        /// </summary>
        /// <param name="candidateVersion">The candidate version.</param>
        /// <param name="currentVersion">The current version.</param>
        /// <returns><see langword="true"/> when the candidate version is older.</returns>
        public static bool IsOlderVersion(string candidateVersion, string currentVersion)
        {
            if (Version.TryParse(NormalizeVersion(candidateVersion), out Version? candidate)
                && Version.TryParse(NormalizeVersion(currentVersion), out Version? current))
            {
                return candidate < current;
            }

            return string.Compare(candidateVersion, currentVersion, StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static IEnumerable<string> EnumerateCandidateDirectories(string parentDirectory)
        {
            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(parentDirectory);
            }
            catch
            {
                yield break;
            }

            foreach (string directory in directories)
            {
                if (File.Exists(Path.Combine(directory, "app-info.json")))
                    yield return directory;
            }
        }

        /// <summary>
        /// Loads application metadata from an application directory.
        /// </summary>
        /// <param name="directory">The application directory containing <c>app-info.json</c>.</param>
        /// <returns>The loaded application metadata, or <see langword="null"/> when it cannot be loaded.</returns>
        public static AppInfo? TryLoadAppInfo(string directory)
        {
            string path = Path.Combine(directory, "app-info.json");
            if (!File.Exists(path))
                return null;

            try
            {
                return JsonSerializer.Deserialize<AppInfo>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSameApp(AppInfo candidate, AppInfo current)
            => string.Equals(candidate.AppId, current.AppId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(candidate.Channel, current.Channel, StringComparison.OrdinalIgnoreCase)
                && string.Equals(candidate.DeploymentType, current.DeploymentType, StringComparison.OrdinalIgnoreCase);

        private static bool IsClickOnceApp(AppInfo appInfo)
            => string.Equals(appInfo.DeploymentType, "clickonce", StringComparison.OrdinalIgnoreCase);

        private static bool TryGetMajorVersion(string version, out int major)
        {
            major = 0;
            string normalizedVersion = NormalizeVersion(version);
            if (Version.TryParse(normalizedVersion, out Version? parsed))
            {
                major = parsed.Major;
                return true;
            }

            int separatorIndex = normalizedVersion.IndexOf('.');
            string majorPart = separatorIndex >= 0 ? normalizedVersion[..separatorIndex] : normalizedVersion;
            return int.TryParse(majorPart, out major);
        }

        private static string NormalizeVersion(string version)
        {
            version = (version ?? string.Empty).Trim().TrimStart('v', 'V');
            string[] parts = version.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? version : version + ".0";
        }
    }
}
