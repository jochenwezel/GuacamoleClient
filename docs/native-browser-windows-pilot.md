# Avalonia 12 Windows native browser pilot

The separate `GuacamoleClient-NativeBrowserPilot` app integrates the existing client UI with Avalonia 12.1.4, NativeWebView 12.1.1 and Windows WebView2. It does not replace the shipping Avalonia 11/CEF app. It is deliberately outside the release solution and packaging workflows.

## Run

Requires Windows, .NET SDK 8 and Microsoft Edge WebView2 Runtime.

```powershell
dotnet run --project GuacamoleClient-NativeBrowserPilot/GuacamoleClient-NativeBrowserPilot.csproj
```

Create a test profile in the pilot's settings. Settings and browser data live under `%LOCALAPPDATA%\GuacamoleClient-NativeBrowserPilot`; `GUACAMOLECLIENT_PILOT_DATA_DIRECTORY` can override that root. Existing client settings and CEF cookies are not imported. The pilot has its own window title, skips installed-app shortcut registration and disables automatic update checks.

Persistent profiles use a stable directory per profile ID. Private profiles use a random directory shared by sibling windows within this process and enable WebView2 InPrivate mode. Closing one sibling releases only its lease. Closing the last sibling retires the context and attempts directory deletion, with retries for browser file locks while the process remains alive. Crashes or process exit during a lock may leave an unused directory; the pilot does not promise forensic erasure. A later private session receives a new directory. Changing profile identity or persistence policy requires a new window.

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

## Remaining migration gates

An actual Guacamole/RDP session has not been validated: the existing services10 test endpoint timed out from both the workstation and development server. Browser event delivery is not proof of remote desktop behavior. Check real remote shortcuts, AltGr/layout/IME input, clipboard in both directions, monitor bridge behavior, reconnect, and private/persistent authentication across window close/restart before proposing a production switch.

The earlier cross-platform probe is retained on branch `prototype/avalonia12-nativewebview-tests`, including its report at `docs/avalonia12-nativewebview-probe-results.md`. Its private-session sharing failures on macOS/Linux require separate platform implementations and tests. This Windows pilot does not enable those platforms.

Related work: native browser replacement #14, profile/cache lifetime #62, and modifier-only language/RTL shortcuts #69. The latter is a separate feature request and is not implemented by this pilot.
