using GuacamoleClient.Common.Updates;
using NUnit.Framework;

namespace GuacamoleCommon.Tests;

public class AutomaticUpdateCheckSessionTests
{
    [Test]
    public void TryStart_AllowsOnlyFirstAutomaticCheck()
    {
        var session = new AutomaticUpdateCheckSession();

        Assert.That(session.TryStart(), Is.True);
        Assert.That(session.TryStart(), Is.False);
    }
}
