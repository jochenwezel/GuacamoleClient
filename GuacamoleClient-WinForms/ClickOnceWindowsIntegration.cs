using Microsoft.Win32;
using System;
using System.IO;

namespace GuacamoleClient.WinForms
{
    internal static class ClickOnceWindowsIntegration
    {
        private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

        public static void ApplyBestEffortFixes(ClickOnceDeploymentInfo deploymentInfo)
        {
            TrySetInstalledAppsIcon(deploymentInfo);
        }

        private static void TrySetInstalledAppsIcon(ClickOnceDeploymentInfo deploymentInfo)
        {
            try
            {
                string? executablePath = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                    return;

                using RegistryKey? uninstallRoot = Registry.CurrentUser.OpenSubKey(UninstallRegistryPath, writable: false);
                if (uninstallRoot == null)
                    return;

                string expectedDisplayName = deploymentInfo.Channel.Equals("dev", StringComparison.OrdinalIgnoreCase)
                    ? "GuacamoleClient Dev"
                    : "GuacamoleClient";

                foreach (string subKeyName in uninstallRoot.GetSubKeyNames())
                {
                    using RegistryKey? readKey = uninstallRoot.OpenSubKey(subKeyName, writable: false);
                    if (readKey == null || !IsMatchingClickOnceEntry(readKey, deploymentInfo, expectedDisplayName))
                        continue;

                    using RegistryKey? writeKey = Registry.CurrentUser.OpenSubKey(
                        $@"{UninstallRegistryPath}\{subKeyName}",
                        writable: true);

                    writeKey?.SetValue("DisplayIcon", executablePath, RegistryValueKind.String);
                }
            }
            catch
            {
                // Windows integration is cosmetic only; never disturb app startup.
            }
        }

        private static bool IsMatchingClickOnceEntry(
            RegistryKey key,
            ClickOnceDeploymentInfo deploymentInfo,
            string expectedDisplayName)
        {
            string? displayName = key.GetValue("DisplayName") as string;
            if (!string.Equals(displayName, expectedDisplayName, StringComparison.OrdinalIgnoreCase))
                return false;

            string? displayVersion = key.GetValue("DisplayVersion") as string;
            if (!string.Equals(displayVersion, deploymentInfo.CurrentVersion, StringComparison.OrdinalIgnoreCase))
                return false;

            string? uninstallString = key.GetValue("UninstallString") as string;
            if (string.IsNullOrWhiteSpace(uninstallString)
                || uninstallString.IndexOf("dfshim.dll,ShArpMaintain", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            string expectedApplicationIdentity = deploymentInfo.Channel.Equals("dev", StringComparison.OrdinalIgnoreCase)
                ? "GuacamoleClient Dev.app"
                : "GuacamoleClient.app";

            return uninstallString.IndexOf(expectedApplicationIdentity, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
