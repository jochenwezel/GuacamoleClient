# Monitor Layout Prototype

This dependency-free static page visualizes monitor information supplied by a native GuacamoleClient host. It is an independent test surface for issues #59, #60, and #61 and does not connect to Apache Guacamole.

## Local preview

Serve the repository root with any static HTTP server and open:

`/docs/monitor-layout-prototype/`

Opening `index.html` directly may work for the visual demo, but browsers commonly restrict JavaScript modules on `file:` URLs.

## Native bridge

The page announces readiness and its supported capabilities to WebView2 with this message:

```json
{
  "type": "guacamoleClient.monitorLayoutPrototype.ready",
  "version": 1,
  "capabilities": {
    "monitorLayoutVersions": [1],
    "acceptsLegacyDisplayMetrics": true
  }
}
```

The native host responds only after receiving this capability message. Existing Guacamole pages never send this message and therefore continue to use their unchanged `width`, `height`, and `dpi` handshake and subsequent `size` instructions.

The prototype accepts either a full `monitors` list or classic metrics in `legacyDisplay`. Classic metrics are normalized into one primary monitor for visualization. The current examples and complete set of supported fields are defined in `createDemoLayout()` and `createClassicDisplay()` in `monitor-layout.mjs`.

WebView2 hosts can use `PostWebMessageAsJson()`. Other trusted hosts can call the deliberately narrow entry point:

```javascript
window.GuacamoleMonitorPrototype.receiveLayout(payload);
```

The Avalonia/CefGlue host registers the single-purpose `GuacamoleMonitorHost` callback. The page waits for `cefglue.checkObjectBound()` and invokes that callback with the same serialized capability message. The native callback still validates the active profile and current origin before returning monitor data through `receiveLayout()`.

The native client remains responsible for validating the configured page type and trusted origin before sending monitor information.

## Production path

The GitHub Pages workflow copies this directory to:

`https://jochenwezel.github.io/GuacamoleClient/monitor-layout-prototype/`
