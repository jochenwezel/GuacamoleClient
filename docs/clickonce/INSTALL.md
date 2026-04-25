# GuacamoleClient - Installation

## Windows ClickOnce

ClickOnce is the primary installation path for the WinForms client on Windows.

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

## Linux Debian / Ubuntu

The Avalonia client is the Linux-capable client. For the current quick-win package flow, download the `.deb` package from GitHub Releases and install it locally.

### Direct .deb Download

Create a temporary download directory:

```bash
mkdir -p ~/Downloads/guacamoleclient-test
cd ~/Downloads/guacamoleclient-test
```

Download the package from the release page. Replace the version and release URL when testing another build:

```bash
wget https://github.com/jochenwezel/GuacamoleClient/releases/download/codex/issue-12-linux-deb-deployment/guacamoleclient-avalonia_0.0.0.22_amd64.deb
```

Install the downloaded package with `apt`:

```bash
sudo apt install ./guacamoleclient-avalonia_0.0.0.22_amd64.deb
```

Start the application from the desktop application menu or from a terminal:

```bash
guacamoleclient
```

If Chromium/CEF fails to start the GPU process in a VM or remote desktop session, start with GPU acceleration disabled:

```bash
guacamoleclient --disable-gpu
```

The same fallback can be enabled through an environment variable:

```bash
GUACAMOLECLIENT_DISABLE_GPU=1 guacamoleclient
```

### Inspect The Installation

List the files installed by the package:

```bash
dpkg -L guacamoleclient-avalonia
```

Show package metadata:

```bash
apt show ./guacamoleclient-avalonia_0.0.0.22_amd64.deb
dpkg -s guacamoleclient-avalonia
```

Check the desktop launcher and icon:

```bash
ls -l /usr/share/applications/guacamoleclient.desktop
ls -l /usr/share/icons/hicolor/256x256/apps/guacamoleclient.png
```

### Uninstall

Remove the package:

```bash
sudo apt remove guacamoleclient-avalonia
```

Purge package configuration files if needed:

```bash
sudo apt purge guacamoleclient-avalonia
```

### dpkg Fallback

Prefer `apt install ./package.deb` because it resolves dependencies. If testing with `dpkg` directly, repair missing dependencies afterwards:

```bash
sudo dpkg -i guacamoleclient-avalonia_0.0.0.22_amd64.deb
sudo apt --fix-broken install
```

### Future Repository-Based Installation

A package repository is not available yet. Once a zero-cost repository/channel strategy is chosen, Debian/Ubuntu installations should support the familiar flow:

```bash
sudo apt update
sudo apt install guacamoleclient-avalonia
```
