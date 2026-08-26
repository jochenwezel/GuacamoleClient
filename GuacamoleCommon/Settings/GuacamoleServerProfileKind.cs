namespace GuacamoleClient.Common.Settings
{
    /// <summary>
    /// Identifies the type of page represented by a server profile.
    /// </summary>
    public enum GuacamoleServerProfileKind
    {
        /// <summary>
        /// Represents an Apache Guacamole server.
        /// </summary>
        GuacamoleServer = 0,

        /// <summary>
        /// Represents the trusted monitor-layout prototype page.
        /// </summary>
        MonitorLayoutPrototype = 1
    }
}
