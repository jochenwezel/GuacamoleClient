using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GuacamoleClient.Common.Settings
{
    public static class GuacamoleBrowserCache
    {
        public static string GetProfileCacheDirectory(string appName, Guid profileId)
            => Path.Combine(GuacamoleSettingsPaths.GetDefaultSettingsDirectory(appName), "BrowserProfiles", profileId.ToString("N"));

        public static string CreateTemporaryCacheDirectory(string appName, Guid? profileId = null)
        {
            string profilePart = profileId?.ToString("N") ?? "no-profile";
            string path = Path.Combine(Path.GetTempPath(), appName, "BrowserProfiles", profilePart, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        public static void EnsureProfileCacheDirectory(string appName, Guid profileId)
            => Directory.CreateDirectory(GetProfileCacheDirectory(appName, profileId));

        public static void DeleteProfileCacheDirectory(string appName, Guid profileId)
            => DeleteDirectoryIfExists(GetProfileCacheDirectory(appName, profileId));

        public static void DeleteDisabledProfileCaches(string appName, IEnumerable<GuacamoleServerProfile> profiles)
        {
            if (profiles == null)
                return;

            foreach (var profile in profiles.Where(p => !p.LocalCacheEnabled))
                DeleteProfileCacheDirectory(appName, profile.Id);
        }

        public static void DeleteDirectoryIfExists(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return;

            TryDeleteDirectoryContents(path);
            TryDeleteDirectory(path);
        }

        private static void TryDeleteDirectoryContents(string path)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                    TryDeleteFile(file);

                foreach (var directory in Directory.EnumerateDirectories(path))
                    DeleteDirectoryIfExists(directory);
            }
            catch
            {
                // Cache cleanup is best effort. Locked WebView files are retried on later app starts.
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // Locked files are expected while a WebView is still alive.
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, recursive: false);
            }
            catch
            {
                // Non-empty/locked directories are left for the next startup cleanup.
            }
        }
    }
}
