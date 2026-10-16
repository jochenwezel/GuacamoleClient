# Avalonia 12 native browser migration probe

This standalone application tests the browser boundary before changing the shipping
Guacamole client. It uses Avalonia 12.1.4 and Avalonia.Controls.WebView 12.1.1. It
does not reference CefGlue, WebViewControl, or the existing app projects.

## Run the browser checks

```sh
dotnet build tools/NativeWebViewProbe/NativeWebViewProbe.csproj -c Debug
python tools/NativeWebViewProbe/run-probe.py tools/NativeWebViewProbe/bin/Debug/net8.0/NativeWebViewProbe.dll --output artifacts/nativewebview-probe/new-run
```

Use a new output directory for each two-phase run. The runner first writes
synthetic profile markers, closes the application, and then starts a new process
to check persistence. It fails on failed checks, premature exits, or timeouts.
The loopback port defaults to 49171 and can be changed with `--port`.

On Linux, install the documented NativeWebView prerequisites, then run the Python
command inside `dbus-run-session -- xvfb-run -a` if no interactive display is
available. The workflow runs the probe on real macOS and Linux runners. Xvfb
tests exercise the GTK/X11 backend; they do not establish Wayland or GPU behavior.

The optional `Dockerfile.linux` expects a `linux-publish` directory from a
framework-dependent Linux publish next to the Dockerfile. Container execution
requires the normal WebKit sandbox prerequisites; do not weaken sandboxing to
turn a failed test into a pass.

## Checks

- Real native browser creation, HTTP navigation, JavaScript results, and messages.
- Shared state for sibling windows of the same profile, and isolation between
  different profiles on the same HTTP origin.
- Private-session sharing, closing the originating window, and private-state
  disappearance after a process restart.
- Persistent cookie and local-storage survival after a process restart.
- On Windows, PNG capture through the supported WebView2 COM projection, and
  trusted Ctrl+F4 down/up events through the DevTools keyboard API.

The Windows projection uses `CoreWebView2.CreateFromComICoreWebView2` with the
public handle from `NativeWebView.TryGetPlatformHandle`. It does not inspect
private Avalonia or CEF members. The additional Microsoft package and projection
are compiled only when building on Windows.

The HTML fixture exposes a textarea, keyboard-event log, and canvas marker.
The application places an Avalonia overlay above the browser for visual
inspection. After the checks, the app writes periodic `input-<phase>.json` files
so actual desktop input can be checked separately. The Python runner closes the
app immediately after automated checks; launch the executable directly with
`--phase seed --output <new-directory>` for interactive testing.

## Evidence and limits

Results include package versions, operating system, architecture, individual
outcomes, and explicit limitations. Windows browser PNGs and macOS desktop PNGs
are separate evidence. The workflow uploads only synthetic results, screenshots,
and logs, excluding browser profile directories.

Passing local fixtures does not establish a working Guacamole/RDP session,
operating-system shortcut capture, AltGr/IME correctness, clipboard integration,
fullscreen behavior, or complete absence of sensitive private-session disk
artifacts. The WPE configuration API in 12.1.1 does not expose an ephemeral
data-store switch; a separate directory must not be mistaken for memory-only
storage. Hardware tests and the application migration remain separate work.

The macOS probe uses fixed, dedicated WKWebsiteDataStore identifiers ending in
`5aa1` and `5aa2`. They are probe-specific and do not reuse the shipping app's
browser data. WKWebView manages persistent stores outside the result directory;
use a disposable macOS account/runner for repeatable independent runs.

## References

- [Avalonia NativeWebView](https://docs.avaloniaui.net/controls/web/nativewebview)
- [Native browser environment options](https://docs.avaloniaui.net/controls/web/webview-environment)
- [WebView2 COM projection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.createfromcomicorewebview2)
- [Existing browser replacement issue](https://github.com/jochenwezel/GuacamoleClient/issues/14)
- [Profile isolation requirements](https://github.com/jochenwezel/GuacamoleClient/issues/62)
