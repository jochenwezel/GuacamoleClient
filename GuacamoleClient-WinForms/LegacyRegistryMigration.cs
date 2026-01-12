using System;
using Microsoft.Win32;
using GuacamoleClient.Common;
using GuacamoleClient.Common.Settings;

namespace GuacamoleClient.WinForms
{
    internal static class LegacyRegistryMigration
    {
        private const string RegPath = @"Software\CompuMaster\GuacamoleLauncher";
        private const string RegValueName = "StartUrl";

        public static string? ReadLegacyStartUrlFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegPath, false))
                {
                    if (key == null) return null;
                    var val = key.GetValue(RegValueName) as string;
                    return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool TryMigrateLegacyRegistryToSettings(GuacamoleSettingsManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (manager.ServerProfiles.Count > 0) return false; // already configured

            var legacyUrl = ReadLegacyStartUrlFromRegistry();
            if (string.IsNullOrWhiteSpace(legacyUrl)) return false;
            if (!GuacamoleUrlAndContentChecks.IsValidUrlAndAcceptedScheme(legacyUrl)) return false;

            // Note: legacy config had only one URL.
            var profile = new GuacamoleServerProfile
            {
                Url = legacyUrl,
                DisplayName = null,
                ColorValue = "Red",
                IgnoreCertificateErrors = false,
                IsDefault = true,
            };
            manager.Upsert(profile);
            manager.SetDefault(profile.Id);
            return true;
        }
    }
}
