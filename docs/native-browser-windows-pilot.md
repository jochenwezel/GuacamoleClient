# Avalonia 12 Windows native browser pilot

The separate `GuacamoleClient-NativeBrowserPilot` app integrates the existing client UI with Avalonia 12.1.4, NativeWebView 12.1.1 and Windows WebView2. It does not replace the shipping Avalonia 11/CEF app. It is deliberately outside the release solution and packaging workflows.

## Run

Requires Windows, .NET SDK 8 and Microsoft Edge WebView2 Runtime.

```powershell
dotnet run --project GuacamoleClient-NativeBrowserPilot/GuacamoleClient-NativeBrowserPilot.csproj
```

Create a test profile in the pilot's settings. Settings and browser data live under `%LOCALAPPDATA%\GuacamoleClient-NativeBrowserPilot`; `GUACAMOLECLIENT_PILOT_DATA_DIRECTORY` can override that root. Existing client settings and CEF cookies are not imported. The pilot has its own window title, skips installed-app shortcut registration and disables automatic update checks.

Persistent profiles use a stable directory per profile ID. Private profiles use a random directory shared by sibling windows within this process and enable WebView2 InPrivate mode. Closing one sibling releases only its lease. Closing the last sibling retires the context and attempts directory deletion, with retries for browser file locks while the process remains alive. Each private context holds an exclusive marker file handle. At startup, the pilot retries deletion of marked GUID directories whose marker can be exclusively opened. Active contexts in another process remain protected; persistent directories and unmarked directories from older pilots remain untouched. If a locked browser file prevents complete deletion, the marker is restored for a later attempt. The pilot does not promise immediate or forensic erasure. A later private session receives a new directory. Changing profile identity or persistence policy requires a new window.

The Windows adapter uses public WebView2 APIs for screenshots, trusted key delivery through CDP, certificate exceptions and the existing monitor prototype's script bridge. Certificate exceptions apply only to the explicitly configured profile origin. Bridge messages must originate from the current top-level web origin; the existing monitor request validation still applies.

Native browser surfaces cover Avalonia controls layered over the same rectangle. Transient hints therefore occupy a dedicated footer. Menu popups remain usable. The pilot reuses the merged profile menu/separator styles.

## Repeatable checks

Run sequentially because the projects share GuacamoleCommon outputs:

```powershell
dotnet build GuacamoleClient-NativeBrowserPilot/GuacamoleClient-NativeBrowserPilot.csproj
dotnet test GuacamoleClient.NativeBrowserPilot.Tests/GuacamoleClient.NativeBrowserPilot.Tests.csproj
dotnet build GuacamoleClient-Avalonia/GuacamoleClient-Avalonia.csproj
dotnet test GuacamoleCommon.Tests/GuacamoleCommon.Tests.csproj
dotnet test GuacamoleClient.Avalonia.Tests/GuacamoleClient.Avalonia.Tests.csproj
```

The dedicated Windows CI workflow builds the pilot, rejects CEF dependencies in its dependency manifest and runs policy/protocol and common tests.

For synthetic UI testing, start the loopback fixture and create a private pilot profile pointing at `http://127.0.0.1:49173/`:

```powershell
python tools/NativeBrowserPilotFixture.py --output artifacts/native-browser-pilot/local-ui
```

The recorder writes only synthetic textarea contents and browser keyboard events to `input.json`. It does not emulate Guacamole or a remote desktop.

## Verified on Windows, 2026-10-19

- Pilot and existing CEF app build successfully. The pilot reports four inherited `TextBox.Watermark` deprecation warnings.
- 18 pilot policy/protocol tests pass: profile lifetime/isolation, modifier identity, protocol flags/text and exact-origin validation.
- 153 shared tests pass, six are skipped by the existing suite; three existing Avalonia menu tests pass.
- Integrated app navigates to the local fixture and accepts pasted Unicode text.
- Alt+F4 reaches the focused browser as one F4 down/up pair while the client stays open. A pilot-only hook fix consumes the paired physical key-up to prevent it racing ahead of asynchronous native injection.
- Ctrl+Alt+End delivers Delete down/up with Ctrl+Alt. Ctrl+Alt+F4 closes the client locally.
- Ctrl+Alt+Insert enters and exits fullscreen. The Actions screenshot flow saves a valid PNG containing the fixture canvas.
- Transient shortcut hints remain visible in the menu and dedicated footer. Profile-colored menus render with the shared separator styles.

## Real Guacamole checks, 2026-10-20

The services10 VM was intentionally powered off rather than unavailable because of a host outage. It was started on SRVM11 following the user's instruction. Its separately stopped Portainer demo stack was reactivated after adding the required ownership header and separating conflicting Guacamole/Termix Traefik router names. The guacd service also needed an outbound-capable network to reach external RDP targets; it remains explicitly disabled for Traefik routing. No Windows target credentials are stored in the temporary test connection.

Eight checks passed using the actual linked MainWindow and native adapter against the real Guacamole 1.6.0 frontend:

- Frontend navigation and authentication using the test server's REST API.
- Private sibling windows share the Guacamole authentication state.
- Closing the original window leaves the sibling authenticated against the server API.
- A fresh private session has no token from the previous session.
- A real Guacamole SSH connection reaches its sandbox and renders a display.
- The browser captures that frontend as a PNG.
- Persistent authentication survives a fresh client process.

The user then completed the interactive NLA authentication for DEV-HHBUDGETWEB. The embedded client reported CONNECTED and displayed the real Windows desktop. The connection configuration contains no saved Windows credentials. Keyboard and clipboard checks are pending while the user prepares the language settings in that session.

The private-session cleanup follow-up passes 21 policy/protocol tests, including active-owner protection, abandoned-directory cleanup and retry after a browser file lock. An additional real-process probe confirms that an independent live process protects its directory and that process termination releases the marker for cleanup on the next startup. Real browser shutdown/restart testing with the new marker rule remains pending; the user's active RDP process still runs the earlier binary.

Local test evidence and the integration harness are under `artifacts/native-browser-pilot/services10-20261020`. Test browser data uses a separate short directory under the pilot data root. The initial deeply nested test data path failed native browser initialization; the shorter path passed repeated runs. The exact path-length constraint has not been isolated.

## Remaining migration gates

Check real Windows remote shortcuts, AltGr/layout/IME input, clipboard in both directions, monitor bridge behavior, reconnect and RDP-session lifetime before proposing a production switch. A connected desktop alone does not verify these behaviors. Verify final-process browser cleanup/restart with the new marker rule; legacy unmarked directories require separate cleanup after older pilot processes have closed.

The earlier cross-platform probe is retained on branch `prototype/avalonia12-nativewebview-tests`, including its report at `docs/avalonia12-nativewebview-probe-results.md`. Its private-session sharing failures on macOS/Linux require separate platform implementations and tests. This Windows pilot does not enable those platforms.

Related work: native browser replacement #14, profile/cache lifetime #62, and modifier-only language/RTL shortcuts #69. The latter is a separate feature request and is not implemented by this pilot.
