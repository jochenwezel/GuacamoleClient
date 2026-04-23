using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal sealed class AppInfo
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

        public string CurrentVersion => string.IsNullOrWhiteSpace(Version) ? Application.ProductVersion : Version;

        public static AppInfo Load()
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
                        return appInfo.Normalize();
                }
            }
            catch
            {
                // Fall through to ClickOnce/env fallback. App startup must stay resilient.
            }

            ClickOnceDeploymentInfo? clickOnceInfo = ClickOnceDeploymentInfo.TryCreate();
            if (clickOnceInfo != null)
            {
                return new AppInfo
                {
                    AppId = "winforms",
                    DeploymentType = "clickonce",
                    Channel = clickOnceInfo.Channel,
                    Version = clickOnceInfo.CurrentVersion,
                    UpdatesUrl = DefaultUpdatesUrl
                };
            }

            return new AppInfo().Normalize();
        }

        private AppInfo Normalize()
        {
            return new AppInfo
            {
                SchemaVersion = SchemaVersion <= 0 ? 1 : SchemaVersion,
                AppId = string.IsNullOrWhiteSpace(AppId) ? "winforms" : AppId,
                DeploymentType = string.IsNullOrWhiteSpace(DeploymentType) ? "unknown" : DeploymentType,
                Channel = string.IsNullOrWhiteSpace(Channel) ? "stable" : Channel,
                Version = Version,
                UpdatesUrl = string.IsNullOrWhiteSpace(UpdatesUrl) ? DefaultUpdatesUrl : UpdatesUrl
            };
        }
    }
}
