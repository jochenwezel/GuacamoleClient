using GuacamoleClient.Common.Settings;
using System.Linq;
using System.Threading.Tasks;

namespace GuacClient;

internal static class AvaloniaSettingsManagerFactory
{
    public static async Task<GuacamoleSettingsManager> LoadAsync()
    {
        var store = new JsonFileGuacamoleSettingsStore(GuacamoleSettingsPaths.GetSettingsFilePath("GuacamoleClient"));
        var manager = await GuacamoleSettingsManager.LoadAsync(store).ConfigureAwait(false);

        if (manager.ServerProfiles.Count == 0)
        {
            var legacyStore = StartUrlStoreFactory.Create();
            var legacyUrl = legacyStore.Load();
            if (UrlInputDialog.IsValidUrl(legacyUrl))
            {
                var profile = new GuacamoleServerProfile(legacyUrl!, null!, "OrangeRed", false, true);
                manager.Upsert(profile);
                manager.SetDefault(profile.Id);
                await manager.SaveAsync().ConfigureAwait(false);
            }
        }

        return manager;
    }

    public static GuacamoleServerProfile? FindById(GuacamoleSettingsManager manager, System.Guid? profileId)
        => profileId == null ? null : manager.ServerProfiles.FirstOrDefault(p => p.Id == profileId.Value);
}
