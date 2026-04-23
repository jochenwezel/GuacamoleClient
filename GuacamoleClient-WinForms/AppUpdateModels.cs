using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GuacamoleClient.WinForms
{
    internal sealed class AppUpdatesDocument
    {
        [JsonPropertyName("apps")]
        public Dictionary<string, AppUpdatesApp> Apps { get; init; } = new();
    }

    internal sealed class AppUpdatesApp
    {
        [JsonPropertyName("releasesUrl")]
        public string? ReleasesUrl { get; init; }

        [JsonPropertyName("channels")]
        public Dictionary<string, AppUpdatesChannel> Channels { get; init; } = new();
    }

    internal sealed class AppUpdatesChannel
    {
        [JsonPropertyName("deploymentType")]
        public string? DeploymentType { get; init; }

        [JsonPropertyName("channel")]
        public string? Channel { get; init; }

        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("manifestUrl")]
        public string? ManifestUrl { get; init; }

        [JsonPropertyName("updatePageUrl")]
        public string? UpdatePageUrl { get; init; }

        [JsonPropertyName("releasesUrl")]
        public string? ReleasesUrl { get; init; }
    }
}
