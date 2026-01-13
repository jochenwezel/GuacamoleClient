using System;
using GuacamoleClient.Common.Settings;
using GuacamoleCommon.Tests.TestDoubles;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class GuacamoleSettingsManagerTests
{
    [Test]
    public void Default_IsAutoAssigned_WhenMissing()
    {
        var doc = new GuacamoleSettingsDocument();
        var p1 = new GuacamoleServerProfile("https://a", null!, "OrangeRed", false, false);
        var p2 = new GuacamoleServerProfile("https://b", null!, "OrangeRed", false, false);
        doc.ServerProfiles.Add(p1);
        doc.ServerProfiles.Add(p2);
        doc.DefaultServerId = null;

        var mgr = new GuacamoleSettingsManager(new InMemorySettingsStore(doc), doc);
        var def = mgr.GetDefaultOrNull();

        Assert.That(def, Is.Not.Null);
        Assert.That(def!.Id, Is.EqualTo(p1.Id));
        Assert.That(def.IsDefault, Is.True);
        Assert.That(doc.DefaultServerId, Is.EqualTo(p1.Id));
    }

    [Test]
    public void UrlExists_DetectsDuplicates_AndHonorsExceptId()
    {
        var p1 = new GuacamoleServerProfile("https://x", null!, "OrangeRed", false, false);
        var doc = new GuacamoleSettingsDocument();
        doc.ServerProfiles.Add(p1);

        var mgr = new GuacamoleSettingsManager(new InMemorySettingsStore(doc), doc);

        Assert.That(mgr.UrlExists("https://x"), Is.True);
        Assert.That(mgr.UrlExists("https://x", exceptId: p1.Id), Is.False);
        Assert.That(mgr.UrlExists("https://y"), Is.False);
    }
}