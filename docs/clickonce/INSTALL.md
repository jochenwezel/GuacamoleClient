# GuacamoleClient - Installation

GuacamoleClient is published through different installation paths depending on the operating system and application variant.

- [Windows installation](./install-windows.md)
- [Linux installation](./install-linux.md)
- [macOS installation](./install-mac.md)

## Recommended Clients

- Windows: use the WinForms ClickOnce client for the primary Windows installation path.
- Linux: use the Avalonia client. The WinForms client does not run on Linux.
- macOS: use the Avalonia client. Native installation guidance is still being expanded.

## Update Channels

Stable releases are published from GitHub Releases. Preview and development builds can be published from workflow runs for testing.

Direct Linux `.deb` downloads can still use the in-app update check to guide users back to the installation page. Future repository-managed Linux packages should suppress automatic startup update checks because updates should be delivered by the system package manager.

Package-manager based repository support is planned as follow-up work, especially for Linux distributions.
