using System;
using System.Collections.Generic;

namespace GuacamoleClient.Common.Settings
{
    /// <summary>
    /// JSON root document.
    /// </summary>
    public sealed class GuacamoleSettingsDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public List<GuacamoleServerProfile> ServerProfiles { get; set; } = new List<GuacamoleServerProfile>();

        public Guid? DefaultServerId { get; set; }
    }
}
