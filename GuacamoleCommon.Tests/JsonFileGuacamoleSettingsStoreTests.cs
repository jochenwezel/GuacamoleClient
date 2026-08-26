using System;
using System.IO;
using System.Threading.Tasks;
using GuacamoleClient.Common.Settings;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class JsonFileGuacamoleSettingsStoreTests
{
    [Test]
    public async Task SaveLoad_Roundtrip_PreservesProfiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "GuacamoleCommon.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");

        var store = new JsonFileGuacamoleSettingsStore(path);
        var doc = new GuacamoleSettingsDocument();
        var p = new GuacamoleServerProfile("https://example.invalid/guacamole/", "Prod", "#A1B2C3", true, true);
        doc.ServerProfiles.Add(p);
        doc.DefaultServerId = p.Id;
    
        await store.SaveAsync(doc);
        var loaded = await store.LoadAsync();

        Assert.That(loaded.ServerProfiles, Has.Count.EqualTo(1));
        Assert.That(loaded.ServerProfiles[0].Url, Is.EqualTo(p.Url));
        Assert.That(loaded.ServerProfiles[0].DisplayName, Is.EqualTo(p.DisplayName));
        Assert.That(loaded.ServerProfiles[0].PrimaryColorValue, Is.EqualTo(p.PrimaryColorValue));
        Assert.That(loaded.ServerProfiles[0].IgnoreCertificateErrors, Is.True);
        Assert.That(loaded.DefaultServerId, Is.EqualTo(p.Id));
    }

    [Test]
    public async Task Load_ProfileWithoutKind_DefaultsToGuacamoleServer()
    {
        var dir = Path.Combine(Path.GetTempPath(), "GuacamoleCommon.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        await File.WriteAllTextAsync(path, "{\"ServerProfiles\":[{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"Url\":\"https://example.invalid/guacamole/\"}]}");

        var store = new JsonFileGuacamoleSettingsStore(path);
        var loaded = await store.LoadAsync();

        Assert.That(loaded.ServerProfiles, Has.Count.EqualTo(1));
        Assert.That(loaded.ServerProfiles[0].ProfileKind, Is.EqualTo(GuacamoleServerProfileKind.GuacamoleServer));
    }

    [Test]
    public async Task SaveLoad_Roundtrip_PreservesPrototypeKind()
    {
        var dir = Path.Combine(Path.GetTempPath(), "GuacamoleCommon.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        var profile = new GuacamoleServerProfile("https://example.invalid/prototype/", "Prototype", "Green", false, false)
        {
            ProfileKind = GuacamoleServerProfileKind.MonitorLayoutPrototype
        };
        var document = new GuacamoleSettingsDocument();
        document.ServerProfiles.Add(profile);

        var store = new JsonFileGuacamoleSettingsStore(path);
        await store.SaveAsync(document);
        var loaded = await store.LoadAsync();

        Assert.That(loaded.ServerProfiles[0].ProfileKind, Is.EqualTo(GuacamoleServerProfileKind.MonitorLayoutPrototype));
    }
}
