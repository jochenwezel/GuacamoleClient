using GuacamoleClient.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GuacamoleCommon.Tests
{
public class GuacamolePageIdentificationTests
{
    [Test]
    public void ContentIsMonitorLayoutPrototypeStartPage_AcceptsOnlyVersionedPrototypeMarker()
    {
        const string prototypeHtml = "<html><head><meta name=\"guacamole-client-profile-kind\" content=\"monitor-layout-prototype-v1\"></head></html>";

        Assert.That(GuacamoleUrlAndContentChecks.ContentIsMonitorLayoutPrototypeStartPage(prototypeHtml), Is.True);
        Assert.That(GuacamoleUrlAndContentChecks.ContentIsMonitorLayoutPrototypeStartPage("<guac-modal></guac-modal>"), Is.False);
        Assert.That(GuacamoleUrlAndContentChecks.ContentIsGuacamoleStartPage(prototypeHtml), Is.False);
    }
        [Test]
        public void ContentIsGuacamoleLoginForm()
        {
            // Arrange
            string htmlContent;

            htmlContent = @"
                <html>
                    <head><title>Guacamole Login</title></head>
                    <body>
                        <form id='login-form'>
                            <input type='text' name='username' />
                            <input type='password' name='password' />
                            <button type='submit'>Login</button>
                        </form>
                    </body>
                </html>";
            Assert.That(GuacamoleUrlAndContentChecks.ContentIsGuacamoleLoginForm(htmlContent), Is.False);

            htmlContent = @"
                <html>
                    <head><title>Guacamole Login</title></head>
                    <body>
                        <form id='login-form' class=""login-fields"">
                            <input id=""guac-field-user"" type='text' name=""username"" />
                            <input type='password' name=""password"" />
                            <button type='submit'>Login</button>
                        </form>
                    </body>
                </html>";
            Assert.That(GuacamoleUrlAndContentChecks.ContentIsGuacamoleLoginForm(htmlContent), Is.True);
        }

        [Test]
        public void IsValidUrlAndAcceptedScheme([Values(
            "https://rdp.compumaster.de",
            "https://rdp.compumaster.de/",
            "https://rdp.compumaster.de/invalid-url",
            "https://webconnect.informatik.hu-berlin.de",
            "https://webconnect.informatik.hu-berlin.de/guacamole",
            "https://webconnect.informatik.hu-berlin.de/guacamole/",
            "https://guacamole.sandiego.edu/guacamole/"
            )] string serviceUrl)
        {
            Assert.That(GuacamoleUrlAndContentChecks.IsValidUrlAndAcceptedScheme(serviceUrl), Is.True);
        }

        [Test]
        public void IsInvalidUrlAndAcceptedScheme([Values(
            "rdp.compumaster.de",
            "file://webconnect.informatik.hu-berlin.de",
            "/webconnect.informatik.hu-berlin.de/guacamole",
            "C:\\webconnect.informatik.hu-berlin.de\\guacamole"
            )] string serviceUrl)
        {
            Assert.That(GuacamoleUrlAndContentChecks.IsValidUrlAndAcceptedScheme(serviceUrl), Is.False);
        }

        [Obsolete]
        [Test, Ignore("Test incomplete: must receive HTML result AFTER Javascript executed")]
        public void IsGuacamoleResponseWithLoginForm([Values(
            "https://rdp.compumaster.de",
            "https://rdp.compumaster.de/",
            "https://webconnect.informatik.hu-berlin.de",
            "https://webconnect.informatik.hu-berlin.de/guacamole",
            "https://webconnect.informatik.hu-berlin.de/guacamole/",
            "https://guacamole.sandiego.edu/guacamole/"
            )] string serviceUrl)
        {
            Assert.That(GuacamoleUrlAndContentChecks.IsGuacamoleResponseWithLoginForm(serviceUrl, false), Is.True);
        }

        [Test]
        public void IsGuacamoleResponseWithStartPage([Values(
            "https://rdp.compumaster.de",
            "https://rdp.compumaster.de/",
            "https://webconnect.informatik.hu-berlin.de",
            "https://webconnect.informatik.hu-berlin.de/guacamole",
            "https://webconnect.informatik.hu-berlin.de/guacamole/",
            "https://guacamole.sandiego.edu/guacamole/"
            )] string serviceUrl)
        {
            Assert.That(GuacamoleUrlAndContentChecks.IsGuacamoleResponseWithStartPage(serviceUrl, false), Is.True);
        }

        [Test]
        public void IsNoGuacamoleResponseWithStartPage([Values(
            "https://www.compumaster.de",
            "https://rdp.compumaster.de/invalid-url",
            "https://www.google.com"
            )] string serviceUrl)
        {
            Assert.That(GuacamoleUrlAndContentChecks.IsGuacamoleResponseWithStartPage(serviceUrl, false), Is.False);
        }
    }
}
