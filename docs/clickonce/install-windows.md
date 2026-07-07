# GuacamoleClient - Windows Installation

## Windows clients

Both Windows clients are supported long-term.

- WinForms is recommended for most Windows users. It uses Microsoft WebView2, so browser security updates come through Windows/Edge.
- Avalonia is the same app family as Linux and macOS. It uses bundled Chromium/CEF, so browser updates arrive with GuacamoleClient releases.

## Windows delivery options

- WinForms ClickOnce is the recommended installer for regular Windows users.
- WinForms portable ZIP is useful for manual testing or systems where an installer is not wanted.
- WinForms framework-dependent ZIP is available for users who already manage the required .NET runtime themselves.
- WinForms MSIX is published as an experimental package artifact. It is not the recommended path until signing and installation trust are solved.
- Avalonia ZIP is available for Windows users who want the cross-platform client variant.

## WinForms ClickOnce

ClickOnce is the primary installation path for the recommended WinForms client on Windows.

### Stable

- Installation URL: https://jochenwezel.github.io/GuacamoleClient/clickonce/stable/GuacamoleClient.application

Use the stable channel for regular installations. Updates are delivered through the same ClickOnce feed.

### Dev

- Installation URL: https://jochenwezel.github.io/GuacamoleClient/clickonce/dev/GuacamoleClient.application

Use the dev channel only for preview builds and testing.

### Windows Security Prompts

For self-signed open-source builds, Windows may show the publisher as unknown and display a security warning before installation.
The installation can still continue after confirming the prompt.

### Auto-Updates

After installation, ClickOnce checks the same feed for updates. Stable installations update from the stable feed, and dev installations update from the dev feed.
