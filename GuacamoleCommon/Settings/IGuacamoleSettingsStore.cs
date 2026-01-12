using System.Threading.Tasks;

namespace GuacamoleClient.Common.Settings
{
    public interface IGuacamoleSettingsStore
    {
        Task<GuacamoleSettingsDocument> LoadAsync();
        Task SaveAsync(GuacamoleSettingsDocument document);
        string SettingsFilePath { get; }
    }
}
