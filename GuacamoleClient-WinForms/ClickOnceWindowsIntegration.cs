using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal static class ClickOnceWindowsIntegration
    {
        private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

        public static void ApplyBestEffortFixes(ClickOnceDeploymentInfo deploymentInfo)
        {
            TrySetInstalledAppsIcon(deploymentInfo);
            TryCreateRootStartMenuShortcut(deploymentInfo);
        }

        public static void ApplyLocalDevBestEffortFixes()
        {
            TryCreateLocalDevStartMenuShortcut();
        }

        private static void TrySetInstalledAppsIcon(ClickOnceDeploymentInfo deploymentInfo)
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "guac.ico");
                if (!File.Exists(iconPath))
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

                    writeKey?.SetValue("DisplayIcon", iconPath, RegistryValueKind.String);
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

        private static void TryCreateRootStartMenuShortcut(ClickOnceDeploymentInfo deploymentInfo)
        {
            try
            {
                string programsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                if (string.IsNullOrWhiteSpace(programsDirectory) || !Directory.Exists(programsDirectory))
                    return;

                string sourceShortcutName = deploymentInfo.Channel.Equals("dev", StringComparison.OrdinalIgnoreCase)
                    ? "GuacamoleClient Dev.appref-ms"
                    : "GuacamoleClient.appref-ms";

                string? sourceShortcut = Directory.EnumerateFiles(programsDirectory, sourceShortcutName, SearchOption.AllDirectories)
                    .Where(path => !string.Equals(Path.GetDirectoryName(path), programsDirectory, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (sourceShortcut == null)
                    return;

                string targetShortcutName = deploymentInfo.Channel.Equals("dev", StringComparison.OrdinalIgnoreCase)
                    ? "GuacamoleClient Dev (WinForms).appref-ms"
                    : "GuacamoleClient (WinForms).appref-ms";

                string targetShortcut = Path.Combine(programsDirectory, targetShortcutName);
                File.Copy(sourceShortcut, targetShortcut, overwrite: true);
            }
            catch
            {
                // Start menu integration is cosmetic only; never disturb app startup.
            }
        }

        private static void TryCreateLocalDevStartMenuShortcut()
        {
            try
            {
                string programsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                if (string.IsNullOrWhiteSpace(programsDirectory) || !Directory.Exists(programsDirectory))
                    return;

                string executablePath = Environment.ProcessPath ?? Application.ExecutablePath;
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                    return;

                string targetShortcut = Path.Combine(programsDirectory, "GuacamoleClient Local Dev (WinForms).lnk");
                string iconPath = Path.Combine(AppContext.BaseDirectory, "guac.ico");
                string workingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return;

                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null)
                    return;

                dynamic shortcut = shell.CreateShortcut(targetShortcut);
                shortcut.TargetPath = executablePath;
                shortcut.WorkingDirectory = workingDirectory;
                shortcut.Description = "GuacamoleClient Local Dev (WinForms)";
                if (File.Exists(iconPath))
                    shortcut.IconLocation = iconPath;
                shortcut.Save();
            }
            catch
            {
                // Start menu integration is cosmetic only; never disturb app startup.
            }
        }
    }
}
