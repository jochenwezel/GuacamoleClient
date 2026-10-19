# Avalonia 12 / NativeWebView migration probe — 2026-10-16

Windows desktop follow-up completed on 2026-10-19; see the additional observations below.

## Decision

Keep the shipping CEF application unchanged while addressing the browser-context
and overlay requirements below. NativeWebView is a viable migration candidate,
but the tested public environment settings alone do not preserve all required
multi-window session behavior on macOS and Linux.

The standalone [probe and runner](../tools/NativeWebViewProbe/README.md) use
Avalonia 12.1.4, Avalonia.Controls.WebView 12.1.1, and .NET 8. On Windows, the probe
also uses Microsoft.Web.WebView2 1.0.4258.31 for supported COM interop.

## Executed environments and evidence

| Environment | Browser | Automated outcomes across seed and restart phases |
| --- | --- | --- |
| Local wks08, Windows build 26300, x64 | WebView2, Edge 155 user agent | 23 passed, 0 failed |
| GitHub macOS runner, Darwin 25.6.0, ARM64 | WKWebView | 17 passed, 2 failed |
| GitHub Ubuntu 24.04.5 runner, x64, Xvfb | WebKitGTK | 13 passed, 6 failed |

These are check outcomes, not a count of distinct defects. The same requirement
is checked in both phases, and Windows has two additional platform-specific
checks per phase.

- [Original cross-platform run](https://github.com/jochenwezel/GuacamoleClient/actions/runs/38904321832)
- [Refined cross-platform run with recorded cookie/storage values](https://github.com/jochenwezel/GuacamoleClient/actions/runs/38904856216), source commit `4d4a22a65f33e5b0df0a9707e1e781ecafedd678`.
- Download the `nativewebview-probe-macOS` and `nativewebview-probe-Linux` artifacts
  for JSON results, process logs, and desktop screenshots. Browser profile
  directories are excluded from the uploaded evidence.
- Local Windows evidence: `artifacts/nativewebview-probe/windows-diagnostics-20261016/`.
  Earlier local runs also passed; the refined run is the result reported here.

Both CI jobs finish with a failure status because the runner deliberately fails
on unmet migration requirements. Compilation succeeded, and the real browser
controls ran; these were not headless Avalonia mocks.

## Verified behavior

On all three platforms, browser creation, loopback HTTP navigation, JavaScript
return values, and messages from JavaScript to C# succeeded. Different test
profiles remained isolated. Private test state was absent on application restart,
and closing the first private window did not destroy state independently written
in the remaining window.

On Windows, sibling windows shared cookies and LocalStorage in both persistent
and private profiles. Persistent markers survived a complete application-process
restart. A PNG captured through WebView2 contained the HTML and canvas marker;
the image was visually inspected. `Input.dispatchKeyEvent` delivered trusted
Ctrl+F4 down/up events to the page. This establishes a possible replacement API
for direct CEF calls, not successful operating-system shortcut capture.

## Migration blockers identified

### 1. Shared private sessions on macOS and Linux

In each phase, the first private window received a synthetic cookie and
LocalStorage marker. A second private window configured for the same profile
observed `storage: null` and an empty cookie string on both macOS and Linux.

This does not meet the requirement that opening another window for the same
secure profile retains the active login session. Separate non-persistent views
provide isolation but do not establish a shared private session.

The upstream implementation is consistent with the observation: macOS selects
`WKWebsiteDataStore.NonPersistent` when constructing each adapter, and GTK calls
`webkit_web_context_new_ephemeral()` for each ephemeral adapter. The probe did
not implement an application-owned native context shared by those views.

Before migrating, establish and test a profile-scoped shared private data store
with lifetime extending until the last dependent window closes. Do not replace
this requirement with a persistent directory that is merely deleted later.

### 2. Persistent and shared cookies on Linux

With the tested GTK `BaseDataDirectory` and `BaseCacheDirectory` settings:

- LocalStorage was visible in sibling persistent windows and after process restart.
- The synthetic cookie was absent in the sibling persistent window.
- The cookie was also absent after process restart.

Repeated cookie assertions account for several failed checks. Treat cookie
storage and context ownership as explicit implementation work; a data directory
alone did not establish the desired behavior. Verify a shared cookie manager
and intentional persistent-cookie storage before relying on saved logins.

### 3. Avalonia overlays above native browser content

The probe places a crimson Avalonia `Border` labeled `OVERLAY TEST` above the
browser in the same `Grid`. It is absent in the inspected macOS and Linux desktop
screenshots, while the menu and status area outside the browser remain visible.
The same missing overlay was confirmed in the Windows desktop follow-up on
2026-10-19.

The default native embedding therefore does not preserve our in-browser-area
modifier hints. Options to evaluate include placing the hints outside the
browser area, a suitable separate popup, or rendering them within web content.
Experimental offscreen rendering was not tested. A Windows menu popup did render
above the browser, so a separate native popup is worth investigating for hints;
this does not establish that the existing in-grid overlay can be retained.

## Windows desktop follow-up — 2026-10-19

The desktop was available for automation on this attempt. The probe was rebuilt
without warnings or errors and launched visibly through the supported Windows
computer-use API. Its eleven seed-phase integration checks passed again. The
shipping application and production browser profiles were not opened or changed.

The following checks used Windows input injection through the computer-use tool,
not JavaScript-generated events or DevTools keyboard injection:

| Action | Observed result |
| --- | --- |
| `a`, then Shift+A | The browser textarea contained `aA`; matching key events were logged. |
| Right Alt+Q on the current keyboard layout | The textarea contained `aA@`; `AltGraph` and the character input were logged. |
| Ctrl+F4 | The page received trusted F4 down/up events with Ctrl set; the probe remained open. |
| Open menu, Escape, refocus browser, type `b` | Blur and focus events were logged; the value became `aA@b`, with Ctrl/Alt/Shift/Meta false on the `b` events. |
| Ctrl+Alt+End | The page received End down/up events with Ctrl and Alt set, followed by modifier release events. |
| Menu popup overlapping the browser | The popup was visible above browser content. |
| Crimson `OVERLAY TEST` border in the same Grid | The marker remained hidden behind the native browser. |
| Alt+F4 | The probe window closed and its process exited. No OS shortcut hook is implemented in the probe. |

These observations establish the basic local Windows input path and confirm that
an application-level system-shortcut protection/forwarding implementation is
still needed. They do not prove that the captured shortcuts reach an actual
remote Guacamole session. The automation's character input produced empty
`KeyboardEvent.code` values for several keys, so this test is not a substitute
for hardware scancode, IME, or all-keyboard-layout coverage.

Local synthetic evidence is preserved under
`artifacts/nativewebview-probe/windows-ui-20261019/`: the seed report and
`input-before-altf4.json` contain the browser state before the final close test.
The final recorded textarea value was `aA@b`. No probe process was left running.

## Outstanding validation

- A real authenticated Guacamole/RDP session was not exercised. All test content
  and state markers were local and synthetic; no production login was modified.
- Windows-key combinations, IME, modifier release while focus changes during a
  held chord, and end-to-end remote key delivery remain open. Basic local Ctrl+F4,
  Ctrl+Alt+End, AltGr character input, and a focus round trip were exercised on
  2026-10-19 as documented above. The earlier 2026-10-16 desktop activation failure
  no longer blocks these completed checks.
- Screenshot-to-clipboard, native browser screenshots on macOS/Linux, full-screen
  transitions, WebSocket remote sessions, downloads, and audio need coverage.
- The private-state checks do not prove absence of sensitive disk artifacts.
- Linux Xvfb coverage does not establish real Wayland, GPU, or notebook behavior.
  The WPE backend was not tested. Its exposed 12.1.1 environment options do not
  include an ephemeral-store switch.

The user's physical Mac was not required: the actual macOS execution used GitHub
Actions. No Linux notebook was required for the GTK/X11 tests. An additional
Debian test image was built on services12, but its container was not executed
because the requested CPU limit exceeded the server's single CPU. That image
was removed; it is not counted as Linux runtime evidence.

## Recommended next implementation step

Address browser-context ownership and cookie policy first, using the extracted
requirements in [issue #62](https://github.com/jochenwezel/GuacamoleClient/issues/62).
Then integrate the real keyboard hook and Guacamole session into the probe,
followed by screenshots/clipboard and replacement of the overlay hints. Keep
CEF removal conditional on those checks rather than on package restore or build
success alone. The overall replacement work is tracked by
[issue #14](https://github.com/jochenwezel/GuacamoleClient/issues/14).

## Upstream implementation references

- [macOS adapter in the tested 12.1.1 tag](https://github.com/AvaloniaUI/Avalonia.Controls.WebView/blob/12.1.1/src/Avalonia.Controls.WebView.Core/Macios/MaciosWebViewAdapter.cs)
- [GTK adapter in the tested 12.1.1 tag](https://github.com/AvaloniaUI/Avalonia.Controls.WebView/blob/12.1.1/src/Avalonia.Controls.WebView.Core/Gtk/GtkWebViewAdapter.cs)
