# GuacamoleClient - Linux Installation

## Debian / Ubuntu

The Avalonia client is the Linux-capable client. The WinForms client does not run on Linux.

Current direct Linux packages target Linux x64 only.

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

The package already includes .NET and the embedded Chromium/CEF browser. Required Linux desktop libraries are installed automatically by apt as package dependencies.

Directly downloaded `.deb` packages are not tied to an APT repository yet. The client can therefore use its in-app update check to guide users back to the installation page when a newer direct-download package is available.

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

## Fedora / Red Hat

Use the Linux x64 RPM package on Fedora/RHEL-compatible distributions:

Open the Dev channel on https://jochenwezel.github.io/GuacamoleClient/
Copy the Fedora / Red Hat command block from there.

The package already includes .NET and the embedded Chromium/CEF browser. Required Linux desktop libraries are installed automatically by `dnf` as package dependencies.

## Other Linux distributions

Use the portable Linux x64 tarball on glibc-based desktop distributions when no native package is available.

Open the Dev channel on https://jochenwezel.github.io/GuacamoleClient/
Copy the Other Linux distributions command block from there.

The generated command installs the application for the current user like this:

```bash
mkdir -p "$HOME/.local/opt" "$HOME/.local/bin"
tar -xzf guacamoleclient-avalonia-linux-x64-<version>.tar.gz -C "$HOME/.local/opt"
ln -sfn "$HOME/.local/opt/guacamoleclient-avalonia-linux-x64-<version>/guacamoleclient" "$HOME/.local/bin/guacamoleclient"
"$HOME/.local/bin/guacamoleclient"
```

The tarball already includes .NET and the embedded Chromium/CEF browser. The target system still needs common Linux desktop libraries such as GTK 3, NSS, ALSA, CUPS, Mesa/GL/GBM, fontconfig, and X11/XCB libraries. Package names vary by distribution.

The per-user tarball installation uses `~/.local/opt` for the extracted application files and `~/.local/bin` for the launcher symlink. Many desktop Linux distributions already include `~/.local/bin` in `PATH`; if not, start the client with the full path or add that directory to your shell profile.

## Snap / Flatpak / AppImage

Universal package formats are being evaluated for a later release.

## Future Repository-Based Installation

A package repository is not available yet. Once a zero-cost repository/channel strategy is chosen, Debian/Ubuntu installations should support the familiar flow:

```bash
sudo apt update
sudo apt install guacamoleclient-avalonia
```

Repository-managed packages should use a distinct deployment marker and suppress automatic startup update checks because updates are delivered by `apt`.
