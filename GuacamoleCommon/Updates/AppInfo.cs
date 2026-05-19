using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuacamoleClient.Common.Updates
{
    public sealed class AppInfo
    {
        public const string DefaultUpdatesUrl = "https://jochenwezel.github.io/GuacamoleClient/app-updates.json";

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; init; } = 1;

        [JsonPropertyName("appId")]
        public string AppId { get; init; } = "winforms";

        [JsonPropertyName("deploymentType")]
        public string DeploymentType { get; init; } = "local-dev";

        [JsonPropertyName("channel")]
        public string Channel { get; init; } = "stable";

        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("updatesUrl")]
        public string UpdatesUrl { get; init; } = DefaultUpdatesUrl;

        public string CurrentVersion => string.IsNullOrWhiteSpace(Version) ? "0.0.0" : Version;

        public bool SuppressAutomaticUpdateChecks
            => IsPackageManagerManagedDeployment(DeploymentType);

        public static bool IsPackageManagerManagedDeployment(string? deploymentType)
        {
            if (string.IsNullOrWhiteSpace(deploymentType))
                return false;

            return deploymentType.Equals("linux-deb", StringComparison.OrdinalIgnoreCase)
                || deploymentType.Equals("linux-rpm", StringComparison.OrdinalIgnoreCase)
                || deploymentType.Equals("snap", StringComparison.OrdinalIgnoreCase)
                || deploymentType.Equals("flatpak", StringComparison.OrdinalIgnoreCase);
        }

        public static AppInfo Load(AppInfo fallback)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "app-info.json");
                if (File.Exists(path))
                {
                    AppInfo? appInfo = JsonSerializer.Deserialize<AppInfo>(
                        File.ReadAllText(path),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (appInfo != null)
                        return appInfo.Normalize(fallback);
                }
            }
            catch
            {
                // App metadata must never prevent startup.
            }

            return fallback.Normalize(fallback);
        }

        private AppInfo Normalize(AppInfo fallback)
        {
            return new AppInfo
            {
                SchemaVersion = SchemaVersion <= 0 ? 1 : SchemaVersion,
                AppId = string.IsNullOrWhiteSpace(AppId) ? fallback.AppId : AppId,
                DeploymentType = string.IsNullOrWhiteSpace(DeploymentType) ? fallback.DeploymentType : DeploymentType,
                Channel = string.IsNullOrWhiteSpace(Channel) ? fallback.Channel : Channel,
                Version = string.IsNullOrWhiteSpace(Version) ? fallback.Version : Version,
                UpdatesUrl = string.IsNullOrWhiteSpace(UpdatesUrl) ? fallback.UpdatesUrl : UpdatesUrl
            };
        }
    }
}
