using System;
using System.Data.Common;
using System.Net;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuacamoleClient.RestClient;

/// <summary>
/// Minimal Guacamole REST client used to determine the UI URL for the Connections admin view.
/// </summary>
/// <inheritdoc path="https://github.com/ridvanaltun/guacamole-rest-api-documentation/"/>
/// <remarks>Typical requests are e.g. <code>
/// GET https://guacamole.sample-company.com/guacamole/api/session/data/mysql/connectionGroups HTTP/1.1
/// Guacamole-Token: E4D416F73B7E312CC1BE5034A0039C442C4E6677C25B4E31ECC209D1FB1B97FB
/// Host: guacamole.sample-company.com
/// </code></remarks>
public sealed class GuacamoleApiClient
{
    private readonly HttpClient _http;

    public GuacamoleApiClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public GuacamoleApiClient(bool ignoreCertificateErrors, TimeSpan? timeout = null) : this(CreateHttpClient(ignoreCertificateErrors, timeout))
    {
    }


    public static HttpClient CreateHttpClient(bool ignoreCertificateErrors, TimeSpan? timeout = null)
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false
        };

        if (ignoreCertificateErrors)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var client = new HttpClient(handler, disposeHandler: true);
        if (timeout is not null)
        {
            client.Timeout = timeout.Value;
        }

        return client;
    }

    /// <summary>
    /// Removes any fragment ("#/...") from a Guacamole UI URL.
    /// </summary>
    public static Uri NormalizeBaseUri(string serverOrUiUrl)
    {
        var uri = new Uri(serverOrUiUrl, UriKind.Absolute);
        var builder = new UriBuilder(uri)
        {
            Fragment = "" // everything after # is client-side only
        };
        return builder.Uri;
    }

    public Task<UserLoginContextWithPrimaryConnectionDataSource?> AuthenticateAndLookupExtendedDataAsync(Uri baseUri, string username, string password, CancellationToken ct = default)
    {
        Task<UserLoginContext?> authResultTask = AuthenticateAsync(baseUri, username, password, ct);
        return AuthenticateAndLookupExtendedDataAsync(baseUri, authResultTask, ct);
    }

    public Task<UserLoginContextWithPrimaryConnectionDataSource?> AuthenticateAndLookupExtendedDataAsync(Uri baseUri, UserLoginContext token, CancellationToken ct = default)
    {
        Task<UserLoginContext?> authResultTask = AuthenticateAsync(baseUri, token, ct);
        return AuthenticateAndLookupExtendedDataAsync(baseUri, authResultTask, ct);
    }

    public Task<UserLoginContextWithPrimaryConnectionDataSource?> AuthenticateAndLookupExtendedDataAsync(Uri baseUri, string token, CancellationToken ct = default)
    {
        Task<UserLoginContext?> authResultTask = AuthenticateAsync(baseUri, token, ct);
        return AuthenticateAndLookupExtendedDataAsync(baseUri, authResultTask, ct);
    }

    private async Task<UserLoginContextWithPrimaryConnectionDataSource?> AuthenticateAndLookupExtendedDataAsync(Uri baseUri, Task<UserLoginContext?> authResultTask, CancellationToken ct = default)
    {
        UserLoginContext? result = await authResultTask;
        if (result == null)
            return null;
        string? primaryDs = null;
        if ((result.AvailableDataSources != null) && (result.AvailableDataSources.Length > 0))
            primaryDs = await LookupPrimaryConnectionsDataSourceAsync(baseUri, result.AuthToken!, result.AvailableDataSources, ct);
        string? connSettingsUrl = null;
        if (primaryDs != null)
        {
            Uri uri = new Uri(baseUri, $"#/settings/{Uri.EscapeDataString(primaryDs)}/connections");
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.TryAddWithoutValidation("Guacamole-Token", result.AuthToken);

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.OK)
                connSettingsUrl = uri.ToString();
            if (resp.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
                connSettingsUrl = null;
        }
        return new UserLoginContextWithPrimaryConnectionDataSource
        {
            AuthToken = result.AuthToken,
            UserName = result.UserName,
            DataSource = result.DataSource,
            AvailableDataSources = result.AvailableDataSources,
            PrimaryConnectionsDataSource = primaryDs ?? "",
            ConnectionsConfigUri = connSettingsUrl
        };
    }

    /// <summary>
    /// Authenticate and return authToken, or null if credentials are rejected.
    /// </summary>
    public Task<UserLoginContext?> AuthenticateAsync(Uri baseUri, string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentNullException(nameof(password));
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = password
        });
        return AuthenticateAsync(baseUri, content, $"user {username}", ct);
    }

    /// <summary>
    /// Authenticate and return authToken, or null if credentials are rejected.
    /// </summary>
    public Task<UserLoginContext?> AuthenticateAsync(Uri baseUri, UserLoginContext token, CancellationToken ct = default)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = token.AuthToken!,
        });
        return AuthenticateAsync(baseUri, content, $"token {token.AuthToken}", ct);
    }


    /// <summary>
    /// Authenticate and return authToken, or null if credentials are rejected.
    /// </summary>
    public Task<UserLoginContext?> AuthenticateAsync(Uri baseUri, string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentNullException(nameof(token));
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = token,
        });
        return AuthenticateAsync(baseUri, content, $"token {token}", ct);
    }

    /// <summary>
    /// Authenticate and return authToken, or null if credentials are rejected.
    /// </summary>
    private async Task<UserLoginContext?> AuthenticateAsync(Uri baseUri, FormUrlEncodedContent content, string loginFailureAuthInfo, CancellationToken ct = default)
    {
        if (baseUri == null)
            throw new ArgumentNullException(nameof(baseUri));
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        var tokenUri = new Uri(baseUri, "api/tokens");
        Uri current = tokenUri;
        const int maxHops = 1;

        for (int hop = 0; hop < maxHops; hop++)
        {
            using var resp = await _http.PostAsync(tokenUri, content, ct).ConfigureAwait(false);

            LogHttpStep(hop, current, resp);

            // Klassisch: Credentials abgelehnt
            if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new System.Security.Authentication.InvalidCredentialException($"Failed login at {baseUri.ToString()} with {loginFailureAuthInfo}");

            // Redirect?
            if ((int)resp.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                if (resp.Headers.Location is null)
                    throw new HttpRequestException($"Redirect without location (status {(int)resp.StatusCode})");

                current = resp.Headers.Location.IsAbsoluteUri
                    ? resp.Headers.Location
                    : new Uri(current, resp.Headers.Location);

                // Bei 303 wird i. d. R. auf GET gewechselt; für Diagnose reicht Logging meist schon.
                continue;
            }

            // Fehlerbody für alle Nicht-Erfolge lesen (bevor EnsureSuccessStatusCode wirft)
            if (!resp.IsSuccessStatusCode)
            {
                var apiErr = await TryReadApiErrorAsync(resp, ct).ConfigureAwait(false);
                var key = apiErr?.TranslatableMessage?.Key;
                var msg = apiErr?.Message ?? resp.ReasonPhrase ?? "Request failed.";

                // Ihr konkreter Fall (auch wenn Server evtl. 400 liefert)
                if (string.Equals(key, "LOGIN.ERROR_TOO_MANY_ATTEMPTS", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("Too many failed authentication attempts", StringComparison.OrdinalIgnoreCase))
                {
                    throw new TooManyLoginAttemptsException(
                        message: msg,
                        statusCode: resp.StatusCode,
                        errorKey: key,
                        retryAfter: GetRetryAfter(resp));
                }

                // Falls der Server wirklich 429 sendet:
                if ((int)resp.StatusCode == 429)
                {
                    throw new TooManyLoginAttemptsException(
                        message: msg,
                        statusCode: resp.StatusCode,
                        errorKey: key,
                        retryAfter: GetRetryAfter(resp));
                }

                // Generischer API-Fehler
                throw new ApiException(msg, resp.StatusCode, key);
            }

            // Erfolg: JSON parsen
            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            var result = new UserLoginContext();
            if (doc.RootElement.TryGetProperty("authToken", out var tokenEl))
                result.AuthToken = tokenEl.GetString();
            if (doc.RootElement.TryGetProperty("username", out var loggedInUsername))
                result.UserName = loggedInUsername.GetString();
            if (doc.RootElement.TryGetProperty("dataSource", out var dataSource))
                result.DataSource = dataSource.GetString();

            if (doc.RootElement.TryGetProperty("availableDataSources", out var ads) && ads.ValueKind == JsonValueKind.Array)
                result.AvailableDataSources = ads.EnumerateArray().Select(x => x.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray()!;
            else
                result.AvailableDataSources = Array.Empty<string>();

            result.Validate();
            return result;
        }

        throw new HttpRequestException($"Too many Redirects (>{maxHops}) at {tokenUri}");
    }

    private static async Task<ApiErrorDto?> TryReadApiErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        string body;
        try
        {
            body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ApiErrorDto>(
                body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null; // Body war nicht JSON oder anderes Format
        }
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage resp)
    {
        var ra = resp.Headers.RetryAfter;
        if (ra is null) return null;
        if (ra.Delta.HasValue) return ra.Delta.Value;
        if (ra.Date.HasValue) return ra.Date.Value - DateTimeOffset.UtcNow;
        return null;
    }

    private static void LogHttpStep(int hop, Uri uri, HttpResponseMessage resp)
    {
        // Minimal sinnvolle Daten:
        // - Statuscode
        // - Location (bei Redirect)
        // - Retry-After (bei 429)
        // - RateLimit-Header (falls vorhanden)
        // - Server/Date, Request-Id/Correlation-Id falls vorhanden

        var location = resp.Headers.Location?.ToString();
        var retryAfter = resp.Headers.RetryAfter?.ToString();

        string? GetHeader(string name)
            => resp.Headers.TryGetValues(name, out var v) ? string.Join(",", v)
             : resp.Content.Headers.TryGetValues(name, out var vc) ? string.Join(",", vc)
             : null;

        var msg =
            $"HTTP hop={hop} url={uri} status={(int)resp.StatusCode} {resp.ReasonPhrase}"
            + (location is not null ? $" location={location}" : "")
            + (retryAfter is not null ? $" retry-after={retryAfter}" : "")
            + (GetHeader("X-Request-Id") is string rid ? $" x-request-id={rid}" : "")
            + (GetHeader("Traceparent") is string tp ? $" traceparent={tp}" : "")
            + (GetHeader("RateLimit-Remaining") is string rlr ? $" ratelimit-remaining={rlr}" : "")
            + (GetHeader("RateLimit-Reset") is string rls ? $" ratelimit-reset={rls}" : "")
            + (GetHeader("X-RateLimit-Remaining") is string xrlr ? $" x-ratelimit-remaining={xrlr}" : "");

        msg += System.Environment.NewLine + resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        //System.Diagnostics.Trace.WriteLine(msg);
        System.Console.WriteLine(msg);
    }

    /// <summary>
    /// Determine the UI URL for the Connections admin view.
    /// Returns null if login fails or the user cannot access connections for any datasource.
    /// </summary>
    public async Task<string?> DetermineConnectionsManagementUrlAsync(
        string serverOrUiUrl,
        string authToken,
        IEnumerable<string>? dataSourceCandidates = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serverOrUiUrl))
            throw new ArgumentNullException(nameof(serverOrUiUrl));
        if (string.IsNullOrWhiteSpace(authToken))
            throw new ArgumentNullException(nameof(authToken));
        if (dataSourceCandidates == null)
            return null;

        var baseUri = NormalizeBaseUri(serverOrUiUrl);
        foreach (var ds in dataSourceCandidates)
        {
            if (await CanListConnectionsAsync(baseUri, authToken, ds, ct).ConfigureAwait(false))
            {
                var uiBase = baseUri.ToString().TrimEnd('/');
                return $"{uiBase}/#/settings/{ds}/connections";
            }
        }
        return null;
    }

    /// <summary>
    /// Determine the DataSource for the Connections admin view.
    /// Returns null if login fails or the user cannot access connections for any datasource.
    /// </summary>
    public async Task<string?> LookupPrimaryConnectionsDataSourceAsync(
        string serverOrUiUrl,
        string authToken,
        IEnumerable<string>? dataSourceCandidates = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serverOrUiUrl))
            throw new ArgumentNullException(nameof(serverOrUiUrl));
        var baseUri = NormalizeBaseUri(serverOrUiUrl);
        return await LookupPrimaryConnectionsDataSourceAsync(baseUri, authToken, dataSourceCandidates, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Determine the DataSource for the Connections admin view.
    /// Returns null if login fails or the user cannot access connections for any datasource.
    /// </summary>
    public async Task<string?> LookupPrimaryConnectionsDataSourceAsync(
        Uri baseUri,
        string authToken,
        IEnumerable<string>? dataSourceCandidates = null,
        CancellationToken ct = default)
    {
        if (baseUri==null)
            throw new ArgumentNullException(nameof(baseUri));
        if (string.IsNullOrWhiteSpace(authToken))
            throw new ArgumentNullException(nameof(authToken));
        if (dataSourceCandidates == null)
            return null;

        foreach (var ds in dataSourceCandidates)
        {
            if (await CanListConnectionsAsync(baseUri, authToken, ds, ct).ConfigureAwait(false))
            {
                return ds;
            }
        }
        return null;
    }

    /// <summary>
    /// Determine the UI URL for the Connections admin view.
    /// Returns null if login fails or the user cannot access connections for any datasource.
    /// </summary>
    [Obsolete("Use overload with authToken parameter to avoid re-authentication.")]
    public async Task<string?> DetermineConnectionsManagementUrlAsync(
        string serverOrUiUrl,
        string username,
        string password,
        IEnumerable<string>? dataSourceCandidates = null,
        CancellationToken ct = default)
    {
        var baseUri = NormalizeBaseUri(serverOrUiUrl);
        var token = await AuthenticateAsync(baseUri, username, password, ct).ConfigureAwait(false);
        if (token == null)
            throw new System.Security.Authentication.InvalidCredentialException($"Failed login at {serverOrUiUrl} with user {username}");

        return await DetermineConnectionsManagementUrlAsync(serverOrUiUrl, token!.AuthToken!, token.AvailableDataSources, ct);
    }

    private async Task<bool> CanListConnectionsAsync(Uri baseUri, string token, string dataSource, CancellationToken ct)
    {
        // The web UI commonly calls this endpoint.
        var uri = new Uri(baseUri, $"api/session/data/{Uri.EscapeDataString(dataSource)}/connections");

        using var req = new HttpRequestMessage(HttpMethod.Get, uri);

        // Some setups accept token as header; sending both is the most compatible.
        req.Headers.TryAddWithoutValidation("Guacamole-Token", token);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.OK) return true;
        if (resp.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound) return false;

        return false;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<GuacConnectionGroup>?> GetConnectionGroupsAsync(
        Uri baseUri,
        string token,
        string[] availableDataSources,
        CancellationToken ct = default)
    {
        string? ds = await LookupPrimaryConnectionsDataSourceAsync(baseUri, token, availableDataSources, ct);
        if (ds==null)
            return null;
        return await GetConnectionGroupsAsync(
            baseUri,
            token,
            ds!,
            ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GuacConnectionGroup>> GetConnectionGroupsAsync(
        Uri baseUri,
        string token,
        string dataSource,
        CancellationToken ct = default)
    {
        var map = await GetMapAsync<GuacConnectionGroup>(
            baseUri,
            token,
            $"api/session/data/{Uri.EscapeDataString(dataSource)}/connectionGroups",
            ct).ConfigureAwait(false);

        // Keys are usually the same as Identifier; we keep both.
        return map
            .Select(kvp =>
            {
                kvp.Value.MapKey = kvp.Key;
                return kvp.Value;
            })
            .OrderBy(x => x.ParentIdentifier == "ROOT" ? 0 : 1)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<GuacConnection>?> GetConnectionsAsync(
        Uri baseUri,
        string token,
        string[] availableDataSources,
        CancellationToken ct = default)
    {
        string? ds = await LookupPrimaryConnectionsDataSourceAsync(baseUri, token, availableDataSources, ct);
        if (ds == null)
            return null;
        return await GetConnectionsAsync(
            baseUri,
            token,
            ds!,
            ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GuacConnection>> GetConnectionsAsync(
        Uri baseUri,
        string token,
        string dataSource,
        CancellationToken ct = default)
    {
        var map = await GetMapAsync<GuacConnection>(
            baseUri,
            token,
            $"api/session/data/{Uri.EscapeDataString(dataSource)}/connections",
            ct).ConfigureAwait(false);

        return map
            .Select(kvp =>
            {
                kvp.Value.MapKey = kvp.Key;
                return kvp.Value;
            })
            .OrderBy(x => x.ParentIdentifier == "ROOT" ? 0 : 1)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ---------- Core JSON helper ----------

    private async Task<Dictionary<string, T>> GetMapAsync<T>(
        Uri baseUri,
        string token,
        string relativePath,
        CancellationToken ct)
        where T : class, new()
    {
        // Some setups accept token in query; some rely on header. Sending both is most compatible.
        var uri = new Uri(baseUri, $"{relativePath}?token={Uri.EscapeDataString(token)}");

        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        req.Headers.TryAddWithoutValidation("Guacamole-Token", token);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);

        if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            return new Dictionary<string, T>();

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        // Guacamole returns a JSON object keyed by identifier: { "13": { ... }, "15": { ... } }
        var map = JsonSerializer.Deserialize<Dictionary<string, T>>(json, JsonOptions);
        return map ?? new Dictionary<string, T>();
    }
}