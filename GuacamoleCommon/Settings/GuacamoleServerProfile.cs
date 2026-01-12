using System;

namespace GuacamoleClient.Common.Settings
{
    /// <summary>
    /// A saved Guacamole server profile (URL + UI color scheme + certificate handling).
    /// Platform-agnostic model intended to be shared between WinForms and Avalonia.
    /// </summary>
    public sealed class GuacamoleServerProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The base URL of the Guacamole server (e.g. https://remote.example.com/guacamole/)
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Optional display name. If not set, UI should display <see cref="Url"/>.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Color value either from palette key (e.g. "Red") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </summary>
        public string ColorValue { get; set; } = "Red";

        /// <summary>
        /// If true, certificate errors should be ignored for this profile only.
        /// </summary>
        public bool IgnoreCertificateErrors { get; set; }

        /// <summary>
        /// Default server which is used at application start.
        /// </summary>
        public bool IsDefault { get; set; }

        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

        public string GetDisplayText() => string.IsNullOrWhiteSpace(DisplayName) ? Url : DisplayName!;
    }
}
