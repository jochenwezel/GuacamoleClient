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
        var p = new GuacamoleServerProfile
        {
            Id = Guid.NewGuid(),
            Url = "https://example.invalid/guacamole/",
            DisplayName = "Prod",
            ColorValue = "#A1B2C3",
            IgnoreCertificateErrors = true,
            IsDefault = true
        };
        doc.ServerProfiles.Add(p);
        doc.DefaultServerId = p.Id;
    
        await store.SaveAsync(doc);
        var loaded = await store.LoadAsync();

        Assert.That(loaded.ServerProfiles, Has.Count.EqualTo(1));
        Assert.That(loaded.ServerProfiles[0].Url, Is.EqualTo(p.Url));
        Assert.That(loaded.ServerProfiles[0].DisplayName, Is.EqualTo(p.DisplayName));
        Assert.That(loaded.ServerProfiles[0].ColorValue, Is.EqualTo(p.ColorValue));
        Assert.That(loaded.ServerProfiles[0].IgnoreCertificateErrors, Is.True);
        Assert.That(loaded.DefaultServerId, Is.EqualTo(p.Id));
    }
}
