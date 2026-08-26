import { MESSAGE_TYPE, calculateRenderGeometry, createClassicDisplay, createDemoLayout, createReadyMessage, normalizeLayout } from "./monitor-layout.mjs";

const elements = Object.fromEntries([
  "connectionState", "connectionStateText", "monitorCount", "desktopBounds", "inputMode", "layoutRevision", "desktopCanvas", "stage", "stageHint",
  "monitorList", "schemaBadge", "warningList", "coordinateSpace", "viewportSize", "devicePixelRatio", "lastUpdate", "rawJson",
  "normalizedJson", "loadClassicButton", "loadDemoButton", "copyReportButton", "showBoundingBox", "toast"
].map((id) => [id, document.getElementById(id)]));

let currentPayload = null;
let currentResult = null;
let currentSource = "demo";
let toastTimer = 0;

function receiveLayout(payload, source = "native host") {
  const parsedPayload = typeof payload === "string" ? safeParse(payload) : payload;
  currentPayload = parsedPayload;
  currentResult = normalizeLayout(parsedPayload);
  currentSource = source;
  render();
}

function safeParse(value) {
  try { return JSON.parse(value); }
  catch { return { type: "invalid-json", version: 0, monitors: [] }; }
}

function render() {
  const { valid, errors, warnings, layout } = currentResult;
  const isDemo = currentSource.includes("demo");
  elements.rawJson.textContent = JSON.stringify(currentPayload, null, 2);
  elements.normalizedJson.textContent = layout ? JSON.stringify(layout, null, 2) : "No normalized layout available.";
  elements.warningList.replaceChildren(...[...errors, ...warnings].map(createWarning));
  elements.schemaBadge.textContent = `Schema v${currentPayload?.version ?? "?"}`;
  elements.connectionState.dataset.state = valid ? (isDemo ? "demo" : "connected") : "error";
  elements.connectionStateText.textContent = valid ? (isDemo ? "Demo data" : "Native host connected") : "Invalid payload";
  elements.stageHint.textContent = isDemo
    ? "Waiting for the native host. Demonstration data is shown so the prototype remains useful in a normal browser."
    : `Monitor layout received from ${currentSource}.`;

  if (!valid || !layout) {
    elements.desktopCanvas.replaceChildren();
    elements.monitorList.replaceChildren();
    elements.monitorCount.textContent = "0";
    return;
  }

  elements.monitorCount.textContent = String(layout.monitors.length);
  elements.desktopBounds.textContent = `${layout.boundingRectangle.width} × ${layout.boundingRectangle.height} at ${formatPoint(layout.boundingRectangle.x, layout.boundingRectangle.y)}`;
  elements.inputMode.textContent = layout.inputMode === "legacyDisplay" ? "Classic display" : "Monitor layout";
  elements.layoutRevision.textContent = String(layout.revision ?? "not supplied");
  elements.coordinateSpace.textContent = layout.coordinateSpace;
  elements.devicePixelRatio.textContent = String(window.devicePixelRatio);
  elements.lastUpdate.textContent = new Date().toLocaleTimeString();
  elements.monitorList.replaceChildren(...layout.monitors.map((monitor, index) => createMonitorListItem(monitor, index)));
  renderDesktop(layout);
}

function renderDesktop(layout) {
  const geometry = calculateRenderGeometry(layout, elements.stage.clientWidth, elements.stage.clientHeight, 70);
  elements.desktopCanvas.style.width = `${geometry.width}px`;
  elements.desktopCanvas.style.height = `${geometry.height}px`;
  elements.desktopCanvas.classList.toggle("hide-bounds", !elements.showBoundingBox.checked);
  elements.desktopCanvas.replaceChildren(...layout.monitors.map((monitor, index) => {
    const rect = geometry.monitorRects[index];
    const monitorElement = document.createElement("div");
    monitorElement.className = `monitor${monitor.primary ? " primary" : ""}`;
    monitorElement.dataset.monitorId = monitor.id;
    Object.assign(monitorElement.style, { left: `${rect.left}px`, top: `${rect.top}px`, width: `${rect.width}px`, height: `${rect.height}px` });

    const label = document.createElement("div");
    label.className = "monitor-label";
    label.innerHTML = `<strong>Monitor ${index + 1}${monitor.primary ? " · Primary" : ""}</strong>${monitor.width} × ${monitor.height} · ${monitor.scalePercent ?? "?"}%`;
    const taskbar = document.createElement("div");
    taskbar.className = "dummy-taskbar";
    monitorElement.append(label, createDummyWindow(monitorElement, index), taskbar);
    return monitorElement;
  }));
}

function createDummyWindow(monitorElement, index) {
  const windowElement = document.createElement("div");
  windowElement.className = "dummy-window";
  windowElement.style.left = `${18 + index * 8}%`;
  windowElement.style.top = `${28 + index * 6}%`;
  windowElement.style.width = "38%";
  windowElement.style.height = "35%";
  windowElement.dataset.maximized = "false";
  windowElement.innerHTML = `<div class="dummy-window-title"><span>${index % 2 ? "Diagnostics" : "Remote application"}</span><button type="button" aria-label="Maximize or restore window">□</button></div><div class="dummy-window-content">Drag this window or use □ to test the monitor boundary.</div>`;
  const titleBar = windowElement.querySelector(".dummy-window-title");
  const maximizeButton = windowElement.querySelector("button");
  titleBar.addEventListener("pointerdown", (event) => startDrag(event, windowElement, monitorElement));
  maximizeButton.addEventListener("click", () => toggleMaximize(windowElement));
  return windowElement;
}

function startDrag(event, windowElement, monitorElement) {
  if (event.target.closest("button") || windowElement.dataset.maximized === "true") return;
  event.preventDefault();
  const startX = event.clientX;
  const startY = event.clientY;
  const startLeft = windowElement.offsetLeft;
  const startTop = windowElement.offsetTop;
  event.currentTarget.setPointerCapture(event.pointerId);
  const move = (moveEvent) => {
    const maxLeft = monitorElement.clientWidth - windowElement.offsetWidth;
    const maxTop = monitorElement.clientHeight - windowElement.offsetHeight - monitorElement.querySelector(".dummy-taskbar").offsetHeight;
    windowElement.style.left = `${Math.max(0, Math.min(maxLeft, startLeft + moveEvent.clientX - startX))}px`;
    windowElement.style.top = `${Math.max(0, Math.min(maxTop, startTop + moveEvent.clientY - startY))}px`;
  };
  const end = () => {
    event.currentTarget.removeEventListener("pointermove", move);
    event.currentTarget.removeEventListener("pointerup", end);
    event.currentTarget.removeEventListener("pointercancel", end);
  };
  event.currentTarget.addEventListener("pointermove", move);
  event.currentTarget.addEventListener("pointerup", end);
  event.currentTarget.addEventListener("pointercancel", end);
}

function toggleMaximize(windowElement) {
  const maximized = windowElement.dataset.maximized === "true";
  if (!maximized) {
    windowElement.dataset.restoreStyle = windowElement.getAttribute("style") ?? "";
    Object.assign(windowElement.style, { left: "0", top: "0", width: "100%", height: "calc(100% - max(7px, 3.3%))" });
  } else {
    windowElement.setAttribute("style", windowElement.dataset.restoreStyle ?? "");
  }
  windowElement.dataset.maximized = String(!maximized);
  windowElement.querySelector("button").textContent = maximized ? "□" : "❐";
}

function createMonitorListItem(monitor, index) {
  const item = document.createElement("div");
  item.className = "monitor-item";
  const physical = monitor.physicalWidthMm && monitor.physicalHeightMm ? ` · ${monitor.physicalWidthMm} × ${monitor.physicalHeightMm} mm` : "";
  item.innerHTML = `<div class="monitor-number">${index + 1}</div><div><h3>${escapeHtml(monitor.id)}</h3><p>${monitor.width} × ${monitor.height} px at ${formatPoint(monitor.x, monitor.y)} · ${monitor.scalePercent ?? "?"}% · ${monitor.orientation}°${physical}</p></div>${monitor.primary ? '<span class="primary-badge">Primary</span>' : ""}`;
  return item;
}

function createWarning(message) {
  const element = document.createElement("div");
  element.className = "warning";
  element.textContent = message;
  return element;
}

function formatPoint(x, y) { return `(${x}, ${y})`; }
function escapeHtml(value) { const node = document.createElement("span"); node.textContent = value; return node.innerHTML; }
function updateViewport() { elements.viewportSize.textContent = `${window.innerWidth} × ${window.innerHeight} CSS px`; if (currentResult?.layout) renderDesktop(currentResult.layout); }
function showToast(message) { clearTimeout(toastTimer); elements.toast.textContent = message; elements.toast.classList.add("visible"); toastTimer = window.setTimeout(() => elements.toast.classList.remove("visible"), 1800); }

async function copyReport() {
  const report = JSON.stringify({ source: currentSource, browser: { viewportWidth: window.innerWidth, viewportHeight: window.innerHeight, devicePixelRatio: window.devicePixelRatio }, received: currentPayload, result: currentResult }, null, 2);
  try { await navigator.clipboard.writeText(report); showToast("Diagnostic report copied"); }
  catch { showToast("Clipboard access is unavailable"); }
}

window.GuacamoleMonitorPrototype = Object.freeze({ receiveLayout: (payload) => receiveLayout(payload, "native host") });
window.addEventListener("message", (event) => { if (event.source === window && event.data?.type === MESSAGE_TYPE) receiveLayout(event.data, "window message"); });

async function announceCapabilities() {
  const readyMessage = createReadyMessage();
  if (window.chrome?.webview) {
    window.chrome.webview.addEventListener("message", (event) => {
      if (event.data?.type === MESSAGE_TYPE) receiveLayout(event.data, "WebView2");
    });
    window.chrome.webview.postMessage(readyMessage);
    return;
  }

  if (window.cefglue?.checkObjectBound) {
    try {
      await window.cefglue.checkObjectBound("GuacamoleMonitorHost");
      await window.GuacamoleMonitorHost.invoke(JSON.stringify(readyMessage));
    } catch {
      // The standalone-browser state remains active when no native host is bound.
    }
  }
}

elements.loadClassicButton.addEventListener("click", () => receiveLayout(createClassicDisplay(), "classic demo"));
elements.loadDemoButton.addEventListener("click", () => receiveLayout(createDemoLayout(), "demo"));
elements.copyReportButton.addEventListener("click", copyReport);
elements.showBoundingBox.addEventListener("change", () => elements.desktopCanvas.classList.toggle("hide-bounds", !elements.showBoundingBox.checked));
new ResizeObserver(updateViewport).observe(elements.stage);
window.addEventListener("resize", updateViewport);

receiveLayout(createDemoLayout(), "demo");
updateViewport();
announceCapabilities();
