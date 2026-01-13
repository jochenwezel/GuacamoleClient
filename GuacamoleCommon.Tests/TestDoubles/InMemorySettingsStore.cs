using System.Threading.Tasks;
using GuacamoleClient.Common.Settings;

namespace GuacamoleCommon.Tests.TestDoubles;

internal sealed class InMemorySettingsStore : IGuacamoleSettingsStore
{
    private GuacamoleSettingsDocument _doc;

    public InMemorySettingsStore(GuacamoleSettingsDocument doc)
    {
        _doc = doc;
    }

    public string SettingsFilePath => "in-memory";

    public Task<GuacamoleSettingsDocument> LoadAsync() => Task.FromResult(_doc);

    public Task SaveAsync(GuacamoleSettingsDocument document)
    {
        _doc = document;
        return Task.CompletedTask;
    }
}
