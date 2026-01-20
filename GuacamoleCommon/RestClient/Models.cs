using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuacamoleClient.RestClient;

public sealed class GuacConnectionGroup
{
    /// <summary>
    /// Not part of Guacamole JSON; filled from dictionary key if useful
    /// </summary>
    [JsonIgnore]
    public string? MapKey { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }

    /// <summary>
    /// ROOT or ID of parent connection group
    /// </summary>
    [JsonPropertyName("parentIdentifier")]
    public string? ParentIdentifier { get; set; }

    /// <summary>
    /// e.g. "ORGANIZATIONAL"
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; } 

    [JsonPropertyName("activeConnections")]
    public int ActiveConnections { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement>? Attributes { get; set; }
}

public sealed class GuacConnection
{
    /// <summary>
    /// Not part of Guacamole JSON; filled from dictionary key if useful
    /// </summary>
    [JsonIgnore]
    public string? MapKey { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }

    /// <summary>
    /// ROOT or ID of parent connection group
    /// </summary>
    [JsonPropertyName("parentIdentifier")]
    public string? ParentIdentifier { get; set; }

    /// <summary>
    /// "rdp", "ssh", ...
    /// </summary>
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("activeConnections")]
    public int ActiveConnections { get; set; }

    /// <summary>
    /// // null sometimes
    /// </summary>
    [JsonPropertyName("lastActive")]
    public long? LastActiveEpochMs { get; set; } 

    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement>? Attributes { get; set; }
}

// ---------- Optional helpers for attribute access ----------

public static class GuacAttributeExtensions
{
    public static string? GetString(this Dictionary<string, JsonElement>? attrs, string key)
    {
        if (attrs == null || !attrs.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Null) return null;
        if (el.ValueKind == JsonValueKind.String) return el.GetString();
        return el.ToString();
    }

    public static int? GetInt32(this Dictionary<string, JsonElement>? attrs, string key)
    {
        if (attrs == null || !attrs.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Null) return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)) return v;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
        return null;
    }
}

// ---------- API Error Models ----------

public sealed class ApiErrorDto
{
    public string? Message { get; set; }
    public TranslatableMessageDto? TranslatableMessage { get; set; }
    public string? Type { get; set; }          // z.B. "BAD_REQUEST"
    public int? StatusCode { get; set; }       // kann null sein
}

public sealed class TranslatableMessageDto
{
    public string? Key { get; set; }           // z.B. "LOGIN.ERROR_TOO_MANY_ATTEMPTS"
    public Dictionary<string, object?>? Variables { get; set; }
}

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ErrorKey { get; }

    public ApiException(string message, HttpStatusCode statusCode, string? errorKey = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ErrorKey = errorKey;
    }
}

public sealed class TooManyLoginAttemptsException : ApiException
{
    public TimeSpan? RetryAfter { get; }

    public TooManyLoginAttemptsException(string message, HttpStatusCode statusCode, string? errorKey, TimeSpan? retryAfter)
        : base(message, statusCode, errorKey)
        => RetryAfter = retryAfter;
}

/// <summary>
/// Represents the context for a user's login session, including authentication token, user name, and data source
/// information.
/// </summary>
/// <remarks>Use this class to store and validate authentication details for a user session. The context provides
/// properties for the authentication token, user name, the selected data source, and a list of available data sources.
/// Call the Validate method to ensure that the required authentication information is present before proceeding with
/// operations that require a valid user context.</remarks>
public class UserLoginContext
{
    public UserLoginContext()
    { }

    /// <summary>
    /// The authentication token for the user session.
    /// </summary>
    public string? AuthToken { get; set; }
    /// <summary>
    /// The user name associated with the login session.
    /// </summary>
    public string? UserName { get; set; }
    /// <summary>
    /// The data source which the user originates from.
    /// </summary>
    /// <remarks>Please note: the list of available registered connections might be located in this or one of the other available data sources.</remarks>
    public string? DataSource { get; set; }
    /// <summary>
    /// A list of available data sources for the user. Data sources represent different authentication backends or user directories, but also directories for registered connections, etc.
    /// </summary>
    public string[]? AvailableDataSources { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AuthToken))
            throw new System.Security.Authentication.InvalidCredentialException("Login failed, url or password credentials invalid, AuthToken is null or empty.");
        if (string.IsNullOrWhiteSpace(UserName))
            throw new InvalidOperationException("UserName is null or empty.");
    }

    public override string ToString()
    {
        return "AuthToken: " + AuthToken + ", UserName: " + UserName + ", DataSource: " + DataSource + ", AvailableDataSources: " + string.Join(",", (AvailableDataSources ?? new string[] { }));
    }
}

/// <summary>
/// The user login context including the primary data source for registered connections.
/// </summary>
public class UserLoginContextWithPrimaryConnectionDataSource : UserLoginContext
{
    /// <summary>
    /// The primary data source which contains the registered connections for the user.
    /// </summary>
    public string? PrimaryConnectionsDataSource { get; set; }
    public override string ToString()
    {
        return base.ToString() + ", PrimaryConnectionDataSource: " + PrimaryConnectionsDataSource;
    }
}
