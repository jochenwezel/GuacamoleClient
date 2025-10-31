using Microsoft.Win32;
using System.Runtime.Versioning;

namespace GuacClient;

[SupportedOSPlatform("windows")] // optional: Analyzer-Hinweis unterdrücken
public sealed class WindowsRegistryStartUrlStore : IStartUrlStore
{
    private const string RegPath = @"Software\CompuMaster\GuacamoleLauncher";
    private const string RegValue = "StartUrl";

    public string? Load()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: false);
            return key?.GetValue(RegValue) as string;
        }
        catch { return null; }
    }

    public void Save(string url)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegPath, true);
        key.SetValue(RegValue, url, RegistryValueKind.String);
    }

    public void Delete()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
        try { key?.DeleteValue(RegValue, false); } catch { /* ignore */ }
    }
}
