namespace GuacClient;

internal static class NativeBridgeOrigin
{
    internal static bool IsSameOrigin(string? expected, string? actual)
        => Uri.TryCreate(expected, UriKind.Absolute, out var first)
           && Uri.TryCreate(actual, UriKind.Absolute, out var second)
           && first.Scheme is "http" or "https"
           && first.Scheme == second.Scheme
           && string.Equals(first.IdnHost, second.IdnHost, StringComparison.OrdinalIgnoreCase)
           && first.Port == second.Port;
}
