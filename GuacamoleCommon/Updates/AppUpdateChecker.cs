using GuacamoleClient.Common.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GuacamoleClient.Common.Updates
{
    public sealed class AppUpdateChecker
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        private readonly AppInfo _appInfo;
        private readonly string _statePath;

        public AppUpdateChecker(AppInfo appInfo, string settingsAppName)
        {
            _appInfo = appInfo ?? throw new ArgumentNullException(nameof(appInfo));
            _statePath = Path.Combine(GuacamoleSettingsPaths.GetDefaultSettingsDirectory(settingsAppName), "update-check-state.json");
        }

        public async Task<AppUpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            AppUpdatesDocument document = await LoadUpdatesDocumentAsync(cancellationToken).ConfigureAwait(false);
            if (!document.Apps.TryGetValue(_appInfo.AppId, out AppUpdatesApp? app))
                return AppUpdateCheckResult.NotAvailable(_appInfo);

            if (!app.Channels.TryGetValue(_appInfo.Channel, out AppUpdatesChannel? channel))
                return AppUpdateCheckResult.NotAvailable(_appInfo);

            string? latestVersion = channel.Version;
            if (string.IsNullOrWhiteSpace(latestVersion))
                return AppUpdateCheckResult.NotAvailable(_appInfo);

            bool isNewer = IsNewerVersion(latestVersion, _appInfo.CurrentVersion);
            string updateUrl = ResolveUpdateUrl(app, channel);
            return new AppUpdateCheckResult(_appInfo, latestVersion, updateUrl, isNewer);
        }

        public async Task<bool> IsSkippedAsync(string version, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(_statePath))
                    return false;

                string json = await File.ReadAllTextAsync(_statePath, cancellationToken).ConfigureAwait(false);
                AppUpdateCheckState? state = JsonSerializer.Deserialize<AppUpdateCheckState>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return string.Equals(state?.AppId, _appInfo.AppId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(state?.Channel, _appInfo.Channel, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(state?.SkippedVersion, version, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public async Task SkipVersionAsync(string version, CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
                var state = new AppUpdateCheckState
                {
                    AppId = _appInfo.AppId,
                    Channel = _appInfo.Channel,
                    SkippedVersion = version
                };
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_statePath, json, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Best effort only.
            }
        }

        public void StartUpdate(AppUpdateCheckResult result)
        {
            if (string.IsNullOrWhiteSpace(result.UpdateUrl))
                return;

            Process.Start(new ProcessStartInfo(result.UpdateUrl) { UseShellExecute = true });
        }

        private async Task<AppUpdatesDocument> LoadUpdatesDocumentAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(_appInfo.UpdatesUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<AppUpdatesDocument>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken).ConfigureAwait(false)
                ?? new AppUpdatesDocument();
        }

        private static string ResolveUpdateUrl(AppUpdatesApp app, AppUpdatesChannel channel)
        {
            if (!string.IsNullOrWhiteSpace(channel.ManifestUrl))
                return MakeAbsolute(channel.ManifestUrl);

            if (!string.IsNullOrWhiteSpace(channel.UpdatePageUrl))
                return MakeAbsolute(channel.UpdatePageUrl);

            if (!string.IsNullOrWhiteSpace(channel.ReleasesUrl))
                return MakeAbsolute(channel.ReleasesUrl);

            return MakeAbsolute(app.ReleasesUrl ?? "https://github.com/jochenwezel/GuacamoleClient/releases");
        }

        private static string MakeAbsolute(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absoluteUri))
                return absoluteUri.ToString();

            return new Uri(new Uri("https://jochenwezel.github.io/GuacamoleClient/"), url).ToString();
        }

        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            if (Version.TryParse(NormalizeVersion(latestVersion), out Version? latest)
                && Version.TryParse(NormalizeVersion(currentVersion), out Version? current))
            {
                return latest > current;
            }

            return string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }

        private static string NormalizeVersion(string version)
        {
            version = version.Trim().TrimStart('v', 'V');
            string[] parts = version.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? version : version + ".0";
        }

        private sealed class AppUpdateCheckState
        {
            public string? AppId { get; init; }
            public string? Channel { get; init; }
            public string? SkippedVersion { get; init; }
        }
    }

    public sealed record AppUpdateCheckResult(
        AppInfo AppInfo,
        string LatestVersion,
        string UpdateUrl,
        bool IsUpdateAvailable)
    {
        public static AppUpdateCheckResult NotAvailable(AppInfo appInfo)
            => new(appInfo, appInfo.CurrentVersion, string.Empty, false);
    }
}
