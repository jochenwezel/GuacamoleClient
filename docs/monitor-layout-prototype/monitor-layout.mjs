export const MESSAGE_TYPE = "guacamoleClient.monitorLayout";
export const READY_MESSAGE_TYPE = "guacamoleClient.monitorLayoutPrototype.ready";
export const SCHEMA_VERSION = 1;

const finiteNumber = (value) => typeof value === "number" && Number.isFinite(value);
const positiveNumber = (value) => finiteNumber(value) && value > 0;

export function createDemoLayout() {
  return {
    type: MESSAGE_TYPE,
    version: SCHEMA_VERSION,
    coordinateSpace: "physicalPixels",
    revision: "demo-1",
    capturedAtUtc: new Date().toISOString(),
    primaryMonitorId: "DISPLAY-1",
    legacyDisplay: { width: 4480, height: 1440, dpi: 120 },
    monitors: [
      {
        id: "DISPLAY-2",
        x: -2560,
        y: -360,
        width: 2560,
        height: 1440,
        scalePercent: 150,
        deviceScaleFactor: 140,
        orientation: 0,
        physicalWidthMm: 597,
        physicalHeightMm: 336,
        primary: false
      },
      {
        id: "DISPLAY-1",
        x: 0,
        y: 0,
        width: 1920,
        height: 1080,
        scalePercent: 100,
        deviceScaleFactor: 100,
        orientation: 0,
        physicalWidthMm: 527,
        physicalHeightMm: 296,
        primary: true
      }
    ]
  };
}

export function createClassicDisplay() {
  return {
    type: MESSAGE_TYPE,
    version: SCHEMA_VERSION,
    coordinateSpace: "guacamoleOptimalDisplayPixels",
    revision: "classic-demo-1",
    capturedAtUtc: new Date().toISOString(),
    legacyDisplay: { width: 1920, height: 1080, dpi: 96 }
  };
}

export function createReadyMessage() {
  return {
    type: READY_MESSAGE_TYPE,
    version: SCHEMA_VERSION,
    capabilities: {
      monitorLayoutVersions: [SCHEMA_VERSION],
      acceptsLegacyDisplayMetrics: true
    }
  };
}

export function normalizeLayout(payload) {
  const errors = [];
  const warnings = [];

  if (!payload || typeof payload !== "object" || Array.isArray(payload)) {
    return { valid: false, errors: ["Payload must be a JSON object."], warnings, layout: null };
  }
  if (payload.type !== MESSAGE_TYPE) errors.push(`Unsupported message type: ${String(payload.type ?? "missing")}.`);
  if (payload.version !== SCHEMA_VERSION) errors.push(`Unsupported schema version: ${String(payload.version ?? "missing")}.`);
  const hasMonitorLayout = Array.isArray(payload.monitors) && payload.monitors.length > 0;
  const hasLegacyDisplay = payload.legacyDisplay
    && positiveNumber(payload.legacyDisplay.width)
    && positiveNumber(payload.legacyDisplay.height)
    && positiveNumber(payload.legacyDisplay.dpi);
  if (!hasMonitorLayout && !hasLegacyDisplay) errors.push("A monitor list or valid legacy display metrics are required.");
  if (errors.length) return { valid: false, errors, warnings, layout: null };

  const monitorSources = hasMonitorLayout ? payload.monitors : [{
    id: "LEGACY-DISPLAY",
    x: 0,
    y: 0,
    width: payload.legacyDisplay.width,
    height: payload.legacyDisplay.height,
    primary: true,
    scalePercent: Math.round(payload.legacyDisplay.dpi / 96 * 100)
  }];
  const ids = new Set();
  const monitors = monitorSources.map((source, index) => {
    const id = typeof source.id === "string" && source.id.trim() ? source.id.trim() : `MONITOR-${index + 1}`;
    if (ids.has(id)) errors.push(`Monitor identifier ${id} is duplicated.`);
    ids.add(id);
    if (![source.x, source.y].every(finiteNumber)) errors.push(`Monitor ${id} has invalid coordinates.`);
    if (![source.width, source.height].every(positiveNumber)) errors.push(`Monitor ${id} has invalid dimensions.`);
    if (source.orientation !== undefined && ![0, 90, 180, 270].includes(source.orientation)) warnings.push(`Monitor ${id} has unsupported orientation ${source.orientation}.`);
    if (source.scalePercent !== undefined && (!finiteNumber(source.scalePercent) || source.scalePercent < 100 || source.scalePercent > 500)) warnings.push(`Monitor ${id} has an unusual desktop scale factor.`);
    return {
      id,
      x: Number(source.x),
      y: Number(source.y),
      width: Number(source.width),
      height: Number(source.height),
      primary: source.primary === true || id === payload.primaryMonitorId,
      orientation: source.orientation ?? 0,
      scalePercent: source.scalePercent ?? null,
      deviceScaleFactor: source.deviceScaleFactor ?? null,
      physicalWidthMm: source.physicalWidthMm ?? null,
      physicalHeightMm: source.physicalHeightMm ?? null
    };
  });
  if (errors.length) return { valid: false, errors, warnings, layout: null };

  const primaryMonitors = monitors.filter((monitor) => monitor.primary);
  if (primaryMonitors.length !== 1) warnings.push(`Expected exactly one primary monitor but found ${primaryMonitors.length}.`);

  const left = Math.min(...monitors.map((monitor) => monitor.x));
  const top = Math.min(...monitors.map((monitor) => monitor.y));
  const right = Math.max(...monitors.map((monitor) => monitor.x + monitor.width));
  const bottom = Math.max(...monitors.map((monitor) => monitor.y + monitor.height));
  const overlaps = findOverlaps(monitors);
  overlaps.forEach(([first, second]) => warnings.push(`Monitor ${first} overlaps monitor ${second}.`));

  const layout = {
    type: MESSAGE_TYPE,
    version: SCHEMA_VERSION,
    inputMode: hasMonitorLayout ? "monitorLayout" : "legacyDisplay",
    coordinateSpace: typeof payload.coordinateSpace === "string" ? payload.coordinateSpace : (hasMonitorLayout ? "unspecified" : "guacamoleOptimalDisplayPixels"),
    revision: payload.revision ?? null,
    capturedAtUtc: payload.capturedAtUtc ?? null,
    legacyDisplay: hasLegacyDisplay ? {
      width: payload.legacyDisplay.width,
      height: payload.legacyDisplay.height,
      dpi: payload.legacyDisplay.dpi
    } : null,
    primaryMonitorId: primaryMonitors[0]?.id ?? payload.primaryMonitorId ?? null,
    boundingRectangle: { x: left, y: top, width: right - left, height: bottom - top },
    monitors
  };
  return { valid: true, errors, warnings, layout };
}

export function findOverlaps(monitors) {
  const overlaps = [];
  for (let first = 0; first < monitors.length; first += 1) {
    for (let second = first + 1; second < monitors.length; second += 1) {
      const a = monitors[first];
      const b = monitors[second];
      if (a.x < b.x + b.width && a.x + a.width > b.x && a.y < b.y + b.height && a.y + a.height > b.y) {
        overlaps.push([a.id, b.id]);
      }
    }
  }
  return overlaps;
}

export function calculateRenderGeometry(layout, availableWidth, availableHeight, padding = 0) {
  const usableWidth = Math.max(1, availableWidth - padding * 2);
  const usableHeight = Math.max(1, availableHeight - padding * 2);
  const scale = Math.min(usableWidth / layout.boundingRectangle.width, usableHeight / layout.boundingRectangle.height);
  return {
    scale,
    width: layout.boundingRectangle.width * scale,
    height: layout.boundingRectangle.height * scale,
    monitorRects: layout.monitors.map((monitor) => ({
      id: monitor.id,
      left: (monitor.x - layout.boundingRectangle.x) * scale,
      top: (monitor.y - layout.boundingRectangle.y) * scale,
      width: monitor.width * scale,
      height: monitor.height * scale
    }))
  };
}
