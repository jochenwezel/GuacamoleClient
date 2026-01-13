using System;
using System.ComponentModel;
using System.ComponentModel.Design;

namespace GuacamoleClient.Common.Settings
{
    /// <summary>
    /// A saved Guacamole server profile (URL + UI color scheme + certificate handling).
    /// Platform-agnostic model intended to be shared between WinForms and Avalonia.
    /// </summary>
    public sealed class GuacamoleServerProfile
    {
        /// <summary>
        /// Initializes a new instance of the GuacamoleServerProfile class for serialization purposes only.
        /// </summary>
        /// <remarks>This constructor is obsolete and should not be used directly in application code. It is intended
        /// solely for use by serialization frameworks. Attempting to use this constructor will result in a compilation error
        /// due to the Obsolete attribute.</remarks>
        [Obsolete("For serialization only", true)]
        public GuacamoleServerProfile()
        {
        }

        /// <summary>
        /// For internal Clone method only: Initializes a new instance of the GuacamoleServerProfile class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier to assign to the server profile.</param>
        private GuacamoleServerProfile(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Initializes a new instance of the GuacamoleServerProfile class with the specified server URL, display name,
        /// primary color, certificate error handling preference, and default profile status.
        /// </summary>
        /// <param name="url">The URL of the Guacamole server to connect to. This should be a valid HTTP or HTTPS endpoint.</param>
        /// <param name="displayName">The display name for the server profile, used to identify it in user interfaces.</param>
        /// <param name="primaryColor">The primary color associated with the server profile, typically used for UI theming. Specify as a
        /// hexadecimal color string (e.g., "#FF5733").</param>
        /// <param name="ignoreCertificateErrors">A value indicating whether SSL certificate errors should be ignored when connecting to the server. Set to
        /// <see langword="true"/> to allow connections with invalid certificates; otherwise, <see langword="false"/>.</param>
        /// <param name="isDefaultServerProfile">A value indicating whether this profile should be treated as the default server profile. Set to <see
        /// langword="true"/> to mark as default; otherwise, <see langword="false"/>.</param>
        public GuacamoleServerProfile(string url, string displayName, string primaryColor, bool ignoreCertificateErrors, bool isDefaultServerProfile)
        {
            Url = url;
            DisplayName = displayName;
            PrimaryColorValue = primaryColor;
            IgnoreCertificateErrors = ignoreCertificateErrors;
            IsDefault = isDefaultServerProfile;
        }

        /// <summary>
        /// Creates a copy of the current server profile with updated connection details and display settings.
        /// </summary>
        /// <remarks>Use this method to create a modified copy of an existing profile without altering the
        /// original. This is useful for scenarios where profile changes should be isolated or reversible.</remarks>
        /// <param name="url">The new server URL to assign to the cloned profile. Cannot be null or empty.</param>
        /// <param name="displayName">The display name to assign to the cloned profile. Used for identification in user interfaces.</param>
        /// <param name="primaryColor">The primary color value to assign to the cloned profile, typically used for UI theming. Should be a valid
        /// color string.</param>
        /// <param name="ignoreCertificateErrors">Specifies whether the cloned profile should ignore SSL certificate errors when connecting to the server. Set
        /// to <see langword="true"/> to bypass certificate validation; otherwise, <see langword="false"/>.</param>
        /// <returns>A new <see cref="GuacamoleServerProfile"/> instance containing the updated values. The original profile
        /// remains unchanged.</returns>
        public GuacamoleServerProfile CloneAndUpdate(string url, string displayName, string primaryColor, bool ignoreCertificateErrors)
        {
            var clone = this.Clone();
            clone.Url = url;
            clone.DisplayName = displayName;
            clone.PrimaryColorValue = primaryColor;
            clone.IgnoreCertificateErrors = ignoreCertificateErrors;
            return clone;
        }

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
        /// Primary color used in UI for this server profile
        /// </summary>
        /// <remarks>
        /// Color value either from palette key (e.g. "OrangeRed") or a custom hex string ("#RRGGBB" or "RRGGBB").
        /// </remarks>
        public string PrimaryColorValue { get; set; } = "OrangeRed";

        /// <summary>
        /// Creates a new color scheme instance based on the current primary color value.
        /// </summary>
        /// <returns>A <see cref="GuacamoleColorScheme"/> initialized with the current primary color value.</returns>
        public GuacamoleColorScheme LookupColorScheme()
        {
            return new GuacamoleColorScheme(PrimaryColorValue);
        }

        /// <summary>
        /// If true, certificate errors should be ignored for this profile only.
        /// </summary>
        public bool IgnoreCertificateErrors { get; set; }

        /// <summary>
        /// Default server profile which is used at application start.
        /// </summary>
        public bool IsDefault { get; internal set; }

        /// <summary>
        /// Gets or sets the date and time, in Coordinated Universal Time (UTC), when the entity was created.
        /// </summary>
        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the date and time, in Coordinated Universal Time (UTC), when the entity was last updated.
        /// </summary>
        public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Returns the display text for this item, using the display name if available; otherwise, returns the URL.
        /// </summary>
        /// <returns>A string containing the display name if it is not null or whitespace; otherwise, the URL.</returns>
        public string GetDisplayText() => string.IsNullOrWhiteSpace(DisplayName) ? Url : DisplayName!;

        /// <summary>
        /// Creates a new GuacamoleServerProfile instance that is a copy of the current profile.
        /// </summary>
        /// <remarks>The cloned profile will have identical property values, except for any properties
        /// that are not copied by this method. Changes to the cloned instance do not affect the original
        /// profile.</remarks>
        /// <returns>A new GuacamoleServerProfile object with the same property values as the current instance.</returns>
        public GuacamoleServerProfile Clone()
        {
            return new GuacamoleServerProfile(this.Id)
            {
                Url = this.Url,
                DisplayName = this.DisplayName,
                PrimaryColorValue = this.PrimaryColorValue,
                IgnoreCertificateErrors = this.IgnoreCertificateErrors,
                IsDefault = this.IsDefault,
                CreatedUtc = this.CreatedUtc,
                UpdatedUtc = this.UpdatedUtc
            };
        }
    }
}