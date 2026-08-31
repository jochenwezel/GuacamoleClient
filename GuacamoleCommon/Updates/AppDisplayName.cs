using System;

namespace GuacamoleClient.Common.Updates
{
    internal static class AppDisplayName
    {
        internal static string Create(string appId, string deploymentType, string channel)
        {
            string platform = appId.Equals("avalonia", StringComparison.OrdinalIgnoreCase)
                ? "Avalonia"
                : "WinForms";

            string edition = deploymentType.Equals("local-dev", StringComparison.OrdinalIgnoreCase)
                ? " Local Debug"
                : channel.Equals("stable", StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : channel.Equals("dev", StringComparison.OrdinalIgnoreCase)
                        ? " Dev"
                        : channel.Equals("test", StringComparison.OrdinalIgnoreCase)
                            ? " Test"
                            : $" {channel}";

            return $"GuacamoleClient{edition} ({platform})";
        }
    }
}
