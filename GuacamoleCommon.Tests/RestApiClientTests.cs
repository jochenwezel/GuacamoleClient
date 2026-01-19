using NUnit.Framework;
using GuacamoleClient.RestClient;

namespace Guacamole.Client.Tests;

/// <summary>
/// Tests for <see cref="GuacamoleApiClient"/>.
/// </summary>
[TestFixture]
public class RestApiClientTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(25);


    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "guacadmin", "guacadmin", true, true)]
    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "admin", "admin", true, true)]
    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "demo", "demo", true, true)]
    public async Task DetermineConnectionsUrl(string url, string user, string pass, bool expectNonEmpty, bool ignoreCertificateErrors)
    {
        var client = new GuacamoleApiClient(ignoreCertificateErrors: ignoreCertificateErrors, DefaultTimeout);
        try
        {
            var result = await client.DetermineConnectionsManagementUrlAsync(url, user, pass);
            if (result == null)
            {
                Assert.Fail("Failure: null result - login failed?");
            }
            if (expectNonEmpty)
            {
                Assert.That(result, Is.Not.Null.And.Not.Empty);
                Assert.That(result, Does.Contain("/#/settings/"));
                Assert.That(result, Does.EndWith("/connections"));
            }
            else
            {
                Assert.That(result, Is.Null.Or.Empty);
            }
        }
        catch (HttpRequestException ex)
        {
            Assert.Inconclusive($"HTTP error (server down, TLS, proxy, etc.): {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Assert.Inconclusive($"Timeout: {ex.Message}");
        }
    }

    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "guacadmin", "guacadmin", null, true, true)]
    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "demo", "demo", null, true, true, 1)]
    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "invalid", "invalid", typeof(System.Security.Authentication.InvalidCredentialException), false, true, 1)]
    public async Task GuacamoleLoginToken(string url, string user, string pass, Type expectedExceptionType, bool expectNonEmpty, bool ignoreCertificateErrors, int loopCount = 1)
    {
        var client = new GuacamoleApiClient(ignoreCertificateErrors: ignoreCertificateErrors, DefaultTimeout);
        for (int i = 0; i < loopCount; i++)
        {
            System.Console.WriteLine($"--- Iteration {i + 1} of {loopCount} ---");

            var baseUri = new Uri(url);
            if (expectedExceptionType != null)
            {
                Assert.ThrowsAsync(expectedExceptionType, async () =>
                {
                    await client.AuthenticateAsync(baseUri, user, pass);
                });
                continue;
            }
            else
            {
                try
                {
                    var token = await client.AuthenticateAsync(baseUri, user, pass);
                    if (token != null)
                    {
                        System.Console.WriteLine($"## GuacamoleLoginToken");
                        System.Console.WriteLine($"Auth token: {token.AuthToken}");
                        System.Console.WriteLine($"User context: {token.ToString()}");
                    }
                    else
                    {
                        System.Console.WriteLine("No token received.");
                    }
                    if (expectNonEmpty)
                    {
                        Assert.That(token, Is.Not.Null);
                        Assert.That(token!.AuthToken, Is.Not.Null.And.Not.Empty);
                        Assert.That(token!.UserName, Is.EqualTo(user));
                        Assert.That(token!.DataSource, Is.Not.Null.And.Not.Empty);
                        Assert.That(token!.AvailableDataSources, Is.Not.Null);
                        Assert.That(token!.AvailableDataSources![0], Is.Not.Null.And.Not.Empty);
                    }
                    else
                    {
                        Assert.That(token, Is.Null);
                    }
                }
                catch (HttpRequestException ex)
                {
                    Assert.Inconclusive($"HTTP error (server down, TLS, proxy, etc.): {ex.Message}");
                }
                catch (TaskCanceledException ex)
                {
                    Assert.Inconclusive($"Timeout: {ex.Message}");
                }
            }
        }
    }

    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "guacadmin", "guacadmin", null, true)]
    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "admin", "admin", null, true)]
    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "demo", "demo", null, true)]
    public async Task ConnectionGroups(string url, string user, string pass, bool? expectNonEmpty, bool ignoreCertificateErrors)
    {
        var client = new GuacamoleApiClient(ignoreCertificateErrors: ignoreCertificateErrors, DefaultTimeout);
        Uri baseUri = GuacamoleApiClient.NormalizeBaseUri(url);
        var AuthToken = await AuthenticateAsync(client, baseUri, user, pass, System.Threading.CancellationToken.None);
        var result = await client.GetConnectionGroupsAsync(baseUri, AuthToken!.AuthToken!, AuthToken.AvailableDataSources!);

        if (result == null)
        {
            Assert.Fail("Failure: null result - login failed?");
        }
        else if (!expectNonEmpty.HasValue)
        {
            // unknown expectation, just print results
            if (result == null)
                System.Console.WriteLine("No connection groups found, but considered as ACCEPTED since there really might be no configured groups.");
            else
            {
                System.Console.WriteLine($"Found {result.Count} connection groups.");
                foreach (var group in result)
                {
                    System.Console.WriteLine($"- {group.Name} (ID: {group.Identifier}, Type: {group.Type}, ActiveConnections: {group.ActiveConnections})");
                }
            }
        }
        else if (expectNonEmpty.GetValueOrDefault())
        {
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(0));
            System.Console.WriteLine($"Found {result.Count} connection groups.");
            foreach (var group in result)
            {
                System.Console.WriteLine($"- {group.Name} (ID: {group.Identifier}, Type: {group.Type}, ActiveConnections: {group.ActiveConnections})");
            }
        }
        else
        {
            Assert.That(result, Is.Null);
        }
    }


    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "guacadmin", "guacadmin", true, true)]
    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "admin", "admin", true, true)]
    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "demo", "demo", true, true)]
    public async Task Connections(string url, string user, string pass, bool expectNonEmpty, bool ignoreCertificateErrors)
    {
        var client = new GuacamoleApiClient(ignoreCertificateErrors: ignoreCertificateErrors, DefaultTimeout);
        Uri baseUri = GuacamoleApiClient.NormalizeBaseUri(url);
        var AuthToken = await AuthenticateAsync(client, baseUri, user, pass, System.Threading.CancellationToken.None);
        var result = await client.GetConnectionsAsync(baseUri, AuthToken!.AuthToken!, AuthToken.AvailableDataSources!);

        if (result == null)
        {
            Assert.Fail("Failure: null result - login failed?");
        }
        else if (expectNonEmpty)
        {
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(0));
            System.Console.WriteLine($"Found {result.Count} connection groups.");
            foreach (var connectionItem in result)
            {
                System.Console.WriteLine($"- {connectionItem.Name} (ID: {connectionItem.Identifier}, Parent-ID: {connectionItem.ParentIdentifier}, Type: {connectionItem.Protocol}, ActiveConnections: {connectionItem.ActiveConnections})");
            }
        }
        else
        {
            Assert.That(result, Is.Null);
        }
    }


    [TestCase("https://services10.mgmt.compumaster.de/guacamole/#/settings/postgresql/connections", "demo", "demo", true, true)]
    public async Task AuthRefresh(string url, string user, string pass, bool expectNonEmpty, bool ignoreCertificateErrors)
    {
        var client = new GuacamoleApiClient(ignoreCertificateErrors: ignoreCertificateErrors, DefaultTimeout);
        Uri baseUri = GuacamoleApiClient.NormalizeBaseUri(url);
        var AuthToken = await AuthenticateAsync(client, baseUri, user, pass, System.Threading.CancellationToken.None);
        var result = await client.AuthenticateAsync(baseUri, AuthToken!);

        if (result == null)
        {
            Assert.Fail("Failure: null result - login failed?");
        }
        else
        {
            System.Console.WriteLine(result.ToString());
        }
    }

    private async Task<UserLoginContext?> AuthenticateAsync(GuacamoleApiClient client, Uri baseUri, string username, string password, System.Threading.CancellationToken ct)
    {
        return await client.AuthenticateAsync(baseUri, username, password, ct).ConfigureAwait(false);
    }
}
