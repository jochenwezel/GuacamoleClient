namespace GuacClient;

internal static class PilotPaths
{
    internal static string DataDirectory => Path.GetFullPath(
        Environment.GetEnvironmentVariable("GUACAMOLECLIENT_PILOT_DATA_DIRECTORY")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GuacamoleClient-NativeBrowserPilot"));

    internal static string SettingsFile => Path.Combine(DataDirectory, "settings.json");
}
