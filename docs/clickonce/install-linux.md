# GuacamoleClient - Linux Installation

## Debian / Ubuntu

The Avalonia client is the Linux-capable client. The WinForms client does not run on Linux.

For the current quick-win package flow, download the `.deb` package from GitHub Releases and install it locally.

### Direct .deb Download

Create a temporary download directory:

```bash
mkdir -p ~/Downloads/guacamoleclient-test
cd ~/Downloads/guacamoleclient-test
```

Download the package from the release page. Replace the version and release URL when testing another build:

```bash
wget https://github.com/jochenwezel/GuacamoleClient/releases/download/__AVALONIA_DEB_RELEASE_TAG__/guacamoleclient-avalonia___AVALONIA_DEB_VERSION___amd64.deb
```

Install the downloaded package with `apt`:

```bash
sudo apt install ./guacamoleclient-avalonia___AVALONIA_DEB_VERSION___amd64.deb
```

Start the application from the desktop application menu or from a terminal:

```bash
guacamoleclient
```

On Linux, GuacamoleClient automatically retries early Chromium/CEF GPU startup crashes with GPU acceleration disabled. If the fallback succeeds, this preference is stored for future starts.

To force GPU acceleration off manually, start with:

```bash
guacamoleclient --disable-gpu
```

The same mode can be enabled through an environment variable:

```bash
GUACAMOLECLIENT_DISABLE_GPU=1 guacamoleclient
```

To force a normal GPU-enabled startup while testing, use:

```bash
guacamoleclient --enable-gpu
```

### Inspect The Installation

List the files installed by the package:

```bash
dpkg -L guacamoleclient-avalonia
```

Show package metadata:

```bash
apt show ./guacamoleclient-avalonia___AVALONIA_DEB_VERSION___amd64.deb
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
sudo dpkg -i guacamoleclient-avalonia___AVALONIA_DEB_VERSION___amd64.deb
sudo apt --fix-broken install
```

## Fedora / Red Hat / openSUSE

RPM packaging is planned. Until then, use the Linux zip release artifact when available.

## Arch / Manjaro

Native package instructions are planned. Until then, use the Linux zip release artifact when available.

## Snap / Flatpak / AppImage

Universal package formats are being evaluated for a later release.

## Future Repository-Based Installation

A package repository is not available yet. Once a zero-cost repository/channel strategy is chosen, Debian/Ubuntu installations should support the familiar flow:

```bash
sudo apt update
sudo apt install guacamoleclient-avalonia
```
