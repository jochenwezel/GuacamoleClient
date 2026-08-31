using GuacamoleClient.Common.Updates;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace GuacClient;

[SupportedOSPlatform("windows")]
internal static class WindowsAppIntegration
{
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    public static void ApplyBestEffortFixes()
    {
        ClickOnceDeploymentInfo? deploymentInfo = ClickOnceDeploymentInfo.TryCreate();
        if (deploymentInfo == null)
        {
            TryCreateLocalDebugStartMenuShortcut();
            return;
        }

        TrySetInstalledAppsIcon(deploymentInfo);
        TryCreateRootStartMenuShortcut(deploymentInfo);
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
                ? "GuacamoleClient Avalonia Dev"
                : "GuacamoleClient Avalonia";

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
            ? "GuacamoleClient Avalonia Dev.app"
            : "GuacamoleClient Avalonia.app";

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
                ? "GuacamoleClient Avalonia Dev.appref-ms"
                : "GuacamoleClient Avalonia.appref-ms";

            string? sourceShortcut = Directory.EnumerateFiles(programsDirectory, sourceShortcutName, SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetDirectoryName(path), programsDirectory, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (sourceShortcut == null)
                return;

            string displayName = AppDisplayName.Create("avalonia", "clickonce", deploymentInfo.Channel);
            string targetShortcut = Path.Combine(programsDirectory, $"{displayName}.appref-ms");
            File.Copy(sourceShortcut, targetShortcut, overwrite: true);
        }
        catch
        {
            // Windows integration is cosmetic only; never disturb app startup.
        }
    }

    private static void TryCreateLocalDebugStartMenuShortcut()
    {
        try
        {
            string programsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string? executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(programsDirectory)
                || !Directory.Exists(programsDirectory)
                || string.IsNullOrWhiteSpace(executablePath)
                || !File.Exists(executablePath))
            {
                return;
            }

            string displayName = AppDisplayName.Create("avalonia", "local-dev", "dev");
            string targetShortcut = Path.Combine(programsDirectory, $"{displayName}.lnk");
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
            shortcut.Description = displayName;
            if (File.Exists(iconPath))
                shortcut.IconLocation = iconPath;
            shortcut.Save();
        }
        catch
        {
            // Windows integration is cosmetic only; never disturb app startup.
        }
    }

    private sealed class ClickOnceDeploymentInfo
    {
        public required string Channel { get; init; }

        public required string CurrentVersion { get; init; }

        public static ClickOnceDeploymentInfo? TryCreate()
        {
            string? isNetworkDeployed = Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed");
            if (!string.Equals(isNetworkDeployed, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                return null;

            string? currentVersion = Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion");
            string? updateLocation = Environment.GetEnvironmentVariable("ClickOnce_UpdateLocation");
            if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(updateLocation))
                return null;

            string? channel = TryDetectChannel(updateLocation);
            return channel == null
                ? null
                : new ClickOnceDeploymentInfo { Channel = channel, CurrentVersion = currentVersion };
        }

        private static string? TryDetectChannel(string updateLocation)
        {
            if (updateLocation.IndexOf("/clickonce/avalonia/stable/", StringComparison.OrdinalIgnoreCase) >= 0
                || updateLocation.IndexOf("\\clickonce\\avalonia\\stable\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "stable";
            }

            if (updateLocation.IndexOf("/clickonce/avalonia/dev/", StringComparison.OrdinalIgnoreCase) >= 0
                || updateLocation.IndexOf("\\clickonce\\avalonia\\dev\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "dev";
            }

            return null;
        }
    }
}
