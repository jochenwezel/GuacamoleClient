using System;
using System.IO;

namespace GuacamoleClient.Common.Settings
{
    public static class GuacamoleSettingsPaths
    {
        public static string GetDefaultSettingsDirectory(string appName = "GuacamoleClient")
        {
            // Cross-platform: ApplicationData
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(baseDir))
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(baseDir, appName);
        }

        public static string GetSettingsFilePath(string appName = "GuacamoleClient")
            => Path.Combine(GetDefaultSettingsDirectory(appName), "settings.json");
    }
}
