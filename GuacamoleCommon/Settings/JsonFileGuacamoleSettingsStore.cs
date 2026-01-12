using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GuacamoleClient.Common.Settings
{
    public sealed class JsonFileGuacamoleSettingsStore : IGuacamoleSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public JsonFileGuacamoleSettingsStore(string settingsFilePath)
        {
            SettingsFilePath = settingsFilePath ?? throw new ArgumentNullException(nameof(settingsFilePath));
        }

        public string SettingsFilePath { get; }

        public async Task<GuacamoleSettingsDocument> LoadAsync()
        {
            if (!File.Exists(SettingsFilePath))
                return new GuacamoleSettingsDocument();

            var json = await File.ReadAllTextAsync(SettingsFilePath).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                return new GuacamoleSettingsDocument();

            var doc = JsonSerializer.Deserialize<GuacamoleSettingsDocument>(json, JsonOpts);
            return doc ?? new GuacamoleSettingsDocument();
        }

        public async Task SaveAsync(GuacamoleSettingsDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(document, JsonOpts);
            await File.WriteAllTextAsync(SettingsFilePath, json).ConfigureAwait(false);
        }
    }
}
