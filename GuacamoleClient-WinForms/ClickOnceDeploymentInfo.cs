using System;

namespace GuacamoleClient.WinForms
{
    internal sealed class ClickOnceDeploymentInfo
    {
        public required string Channel { get; init; }
        public required string CurrentVersion { get; init; }
        public required string UpdateLocation { get; init; }

        public static ClickOnceDeploymentInfo? TryCreate()
        {
            string? isNetworkDeployed = Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed");
            if (!string.Equals(isNetworkDeployed, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                return null;

            string? currentVersion = Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion");
            string? updateLocation = Environment.GetEnvironmentVariable("ClickOnce_UpdateLocation");
            if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(updateLocation))
                return null;

            string? channel = TryDetectChannel(updateLocation);
            if (channel == null)
                return null;

            return new ClickOnceDeploymentInfo
            {
                Channel = channel,
                CurrentVersion = currentVersion,
                UpdateLocation = updateLocation
            };
        }

        private static string? TryDetectChannel(string updateLocation)
        {
            if (updateLocation.IndexOf("/clickonce/stable/", StringComparison.OrdinalIgnoreCase) >= 0
                || updateLocation.IndexOf("\\clickonce\\stable\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "stable";
            }

            if (updateLocation.IndexOf("/clickonce/dev/", StringComparison.OrdinalIgnoreCase) >= 0
                || updateLocation.IndexOf("\\clickonce\\dev\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "dev";
            }

            return null;
        }
    }
}
