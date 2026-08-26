import assert from "node:assert/strict";
import test from "node:test";
import { MESSAGE_TYPE, READY_MESSAGE_TYPE, calculateRenderGeometry, createReadyMessage, normalizeLayout } from "../../docs/monitor-layout-prototype/monitor-layout.mjs";

const payload = (monitors) => ({ type: MESSAGE_TYPE, version: 1, coordinateSpace: "physicalPixels", monitors });

test("normalizes a layout with negative coordinates", () => {
  const result = normalizeLayout(payload([
    { id: "left", x: -1920, y: 0, width: 1920, height: 1080 },
    { id: "primary", x: 0, y: -360, width: 2560, height: 1440, primary: true }
  ]));
  assert.equal(result.valid, true);
  assert.deepEqual(result.layout.boundingRectangle, { x: -1920, y: -360, width: 4480, height: 1440 });
});

test("rejects unsupported message versions", () => {
  const result = normalizeLayout({ type: MESSAGE_TYPE, version: 2, monitors: [{ id: "one", x: 0, y: 0, width: 800, height: 600 }] });
  assert.equal(result.valid, false);
  assert.match(result.errors[0], /schema version/i);
});

test("reports overlapping monitors", () => {
  const result = normalizeLayout(payload([
    { id: "one", x: 0, y: 0, width: 1000, height: 800, primary: true },
    { id: "two", x: 900, y: 0, width: 1000, height: 800 }
  ]));
  assert.equal(result.valid, true);
  assert.ok(result.warnings.some((warning) => warning.includes("overlaps")));
});

test("scales and translates monitor rectangles into the available stage", () => {
  const result = normalizeLayout(payload([
    { id: "left", x: -1000, y: 0, width: 1000, height: 800 },
    { id: "right", x: 0, y: 0, width: 1000, height: 800, primary: true }
  ]));
  const geometry = calculateRenderGeometry(result.layout, 1000, 500);
  assert.equal(geometry.scale, 0.5);
  assert.deepEqual(geometry.monitorRects[0], { id: "left", left: 0, top: 0, width: 500, height: 400 });
  assert.deepEqual(geometry.monitorRects[1], { id: "right", left: 500, top: 0, width: 500, height: 400 });
});

test("rejects duplicate identifiers and invalid dimensions", () => {
  const result = normalizeLayout(payload([
    { id: "same", x: 0, y: 0, width: 100, height: 100 },
    { id: "same", x: 100, y: 0, width: 0, height: 100 }
  ]));
  assert.equal(result.valid, false);
  assert.ok(result.errors.some((error) => error.includes("duplicated")));
  assert.ok(result.errors.some((error) => error.includes("invalid dimensions")));
});

test("normalizes classic width height and DPI as one monitor", () => {
  const result = normalizeLayout({
    type: MESSAGE_TYPE,
    version: 1,
    legacyDisplay: { width: 1366, height: 768, dpi: 120 }
  });
  assert.equal(result.valid, true);
  assert.equal(result.layout.inputMode, "legacyDisplay");
  assert.deepEqual(result.layout.boundingRectangle, { x: 0, y: 0, width: 1366, height: 768 });
  assert.equal(result.layout.monitors[0].scalePercent, 125);
  assert.equal(result.layout.monitors[0].primary, true);
});

test("advertises monitor layout only through an explicit capability handshake", () => {
  assert.deepEqual(createReadyMessage(), {
    type: READY_MESSAGE_TYPE,
    version: 1,
    capabilities: { monitorLayoutVersions: [1], acceptsLegacyDisplayMetrics: true }
  });
});
