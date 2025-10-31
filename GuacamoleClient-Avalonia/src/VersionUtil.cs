using System.Diagnostics;
using System.Reflection;

public static class VersionUtil
{
    private static Assembly Assembly => Assembly.GetEntryAssembly() ?? typeof(VersionUtil).Assembly;

    public static string InformationalVersion()
        => Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
           ?? Assembly.GetName().Version?.ToString() ?? "0.0.0";

    public static string FileVersion()
        => Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
           ?? Assembly.GetName().Version?.ToString() ?? "0.0.0";

    public static string AssemblyVersion()
        => Assembly.GetName().Version?.ToString() ?? "0.0.0";

    // Achtung: Bei Single-File-Publish kann Location leer sein.
    public static string? ProductVersionFromFile()
    {
        var loc = Assembly.Location;
        if (string.IsNullOrEmpty(loc)) return null;
        return FileVersionInfo.GetVersionInfo(loc).ProductVersion;
    }
}
