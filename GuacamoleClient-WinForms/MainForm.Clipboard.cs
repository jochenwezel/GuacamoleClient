using GuacamoleClient.Common;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        private const string ClipboardBridgeMessageType = "guacamoleClient.clipboardForwarding";
        private static readonly TimeSpan ClipboardStreamCreationTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan ClipboardTransferTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ClipboardGestureLifetime = TimeSpan.FromSeconds(3);
        private readonly SemaphoreSlim _clipboardPasteGate = new(1, 1);
        private readonly Dictionary<string, PendingClipboardPaste> _pendingClipboardPastes = new(StringComparer.Ordinal);
        private readonly Queue<long> _nativeClipboardPasteGestures = new();
        private static readonly object ClipboardDiagnosticsLock = new();
        private static bool _clipboardDiagnosticsInitialized;

        private async Task InitializeClipboardForwardingBridgeAsync()
        {
            if (_webview2_core == null)
                return;

            InitializeClipboardDiagnostics();
            _webview2_core.WebMessageReceived += ClipboardForwarding_WebMessageReceived;
            FormClosed += (_, __) => CancelPendingClipboardPastes("window-closed");

            try
            {
                string scriptId = await _webview2_core.AddScriptToExecuteOnDocumentCreatedAsync(
                    BuildClipboardKeyboardHookScript(HomeUrl.GetLeftPart(UriPartial.Authority))).ConfigureAwait(true);
                Trace.TraceInformation("Installed document-start clipboard bridge script ({0}).", scriptId);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Could not install the native clipboard keyboard bridge ({0}).", ex.GetType().Name);
            }
        }

        private static void InitializeClipboardDiagnostics()
        {
            lock (ClipboardDiagnosticsLock)
            {
                if (_clipboardDiagnosticsInitialized)
                    return;

                try
                {
                    string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    if (string.IsNullOrWhiteSpace(localApplicationData))
                        return;

                    string logDirectory = Path.Combine(localApplicationData, "GuacamoleClient", "Logs");
                    Directory.CreateDirectory(logDirectory);
                    string logPath = Path.Combine(logDirectory, $"clipboard-forwarding-{Environment.ProcessId}.log");
                    Trace.Listeners.Add(new TextWriterTraceListener(logPath, "ClipboardForwarding"));
                    Trace.AutoFlush = true;
                    _clipboardDiagnosticsInitialized = true;
                    Trace.TraceInformation("Clipboard forwarding diagnostics initialized.");
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Could not initialize clipboard diagnostics file ({0}).", ex.GetType().Name);
                }
            }
        }

        private void RecordNativeClipboardPasteGesture()
        {
            long now = Environment.TickCount64;
            while (_nativeClipboardPasteGestures.Count > 0
                && now - _nativeClipboardPasteGestures.Peek() > ClipboardGestureLifetime.TotalMilliseconds)
                _nativeClipboardPasteGestures.Dequeue();

            _nativeClipboardPasteGestures.Enqueue(now);
        }

        private bool ConsumeNativeClipboardPasteGesture()
        {
            long now = Environment.TickCount64;
            while (_nativeClipboardPasteGestures.Count > 0
                && now - _nativeClipboardPasteGestures.Peek() > ClipboardGestureLifetime.TotalMilliseconds)
                _nativeClipboardPasteGestures.Dequeue();

            if (_nativeClipboardPasteGestures.Count == 0)
                return false;

            _nativeClipboardPasteGestures.Dequeue();
            return true;
        }

        private async Task ProcessLocalClipboardPasteAsync(string requestId, bool shiftPressed, PendingClipboardPaste pending)
        {
            bool transferSucceeded = false;
            bool pasteGateAcquired = false;

            try
            {
                Trace.TraceInformation("Local Ctrl+V paste triggered by the Guacamole page.");

                if (_webview2_core == null
                    || !IsTrustedClipboardBridgeSource(_webview2_core.Source)
                    || !BrowserScreenshot.IsConnectionPage(_webview2_core.Source))
                {
                    Trace.TraceWarning("Native clipboard request rejected because the current page is not the configured Guacamole connection.");
                    return;
                }

                await _clipboardPasteGate.WaitAsync().ConfigureAwait(true);
                pasteGateAcquired = true;
                if (!IsClipboardRequestPending(requestId, pending))
                    return;

                string? text = await ReadLocalClipboardTextAsync().ConfigureAwait(true);
                if (!IsClipboardRequestPending(requestId, pending))
                    return;

                if (text == null)
                {
                    Trace.TraceWarning("Native clipboard read failed; the clipboard has no readable text format.");
                    return;
                }

                if (text.Length == 0)
                {
                    Trace.TraceInformation("Native clipboard text is empty; no remote paste was dispatched.");
                    return;
                }

                Trace.TraceInformation("Native clipboard text read succeeded ({0} characters).", text.Length);

                if (_webview2_core == null
                    || !IsTrustedClipboardBridgeSource(_webview2_core.Source)
                    || !BrowserScreenshot.IsConnectionPage(_webview2_core.Source))
                {
                    Trace.TraceWarning("Clipboard paste cancelled because the Guacamole page changed before transfer.");
                    return;
                }

                await _webview2_core.ExecuteScriptAsync(BuildClipboardStartScript(requestId, text, shiftPressed)).ConfigureAwait(true);

                bool streamCreated = await pending.StreamCreated.Task
                    .WaitAsync(ClipboardStreamCreationTimeout)
                    .ConfigureAwait(true);

                if (!streamCreated)
                {
                    Trace.TraceWarning("Guacamole clipboard stream could not be created.");
                    return;
                }

                string? failure = await pending.TransferCompleted.Task
                    .WaitAsync(ClipboardTransferTimeout)
                    .ConfigureAwait(true);

                if (failure != null)
                    Trace.TraceWarning("Native clipboard transfer failed ({0}).", failure);
                else
                    transferSucceeded = true;
            }
            catch (TimeoutException)
            {
                Trace.TraceWarning("Native clipboard forwarding timed out.");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Native clipboard forwarding failed ({0}).", ex.GetType().Name);
            }
            finally
            {
                _pendingClipboardPastes.Remove(requestId);
                if (!transferSucceeded)
                    _ = CancelClipboardBridgeRequestAsync(requestId);

                if (pasteGateAcquired)
                    _clipboardPasteGate.Release();
            }
        }

        private async Task<string?> ReadLocalClipboardTextAsync()
        {
            const int maxAttempts = 5;
            const int retryDelayMilliseconds = 40;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (!Clipboard.ContainsText())
                        return null;

                    return Clipboard.GetText(TextDataFormat.UnicodeText);
                }
                catch (ExternalException ex) when (attempt < maxAttempts)
                {
                    Trace.TraceInformation("Native clipboard read was temporarily unavailable (attempt {0}/{1}, {2}).", attempt, maxAttempts, ex.GetType().Name);
                    await Task.Delay(retryDelayMilliseconds).ConfigureAwait(true);
                }
                catch (ExternalException ex)
                {
                    Trace.TraceWarning("Native clipboard read failed after retries ({0}).", ex.GetType().Name);
                    return null;
                }
                catch (InvalidOperationException ex) when (attempt < maxAttempts)
                {
                    Trace.TraceInformation("Native clipboard read was temporarily unavailable (attempt {0}/{1}, {2}).", attempt, maxAttempts, ex.GetType().Name);
                    await Task.Delay(retryDelayMilliseconds).ConfigureAwait(true);
                }
                catch (InvalidOperationException ex)
                {
                    Trace.TraceWarning("Native clipboard read failed after retries ({0}).", ex.GetType().Name);
                    return null;
                }
            }

            return null;
        }

        private bool IsClipboardRequestPending(string requestId, PendingClipboardPaste pending)
            => _pendingClipboardPastes.TryGetValue(requestId, out PendingClipboardPaste? current)
                && ReferenceEquals(current, pending);

        private static bool IsValidClipboardRequestId(string requestId)
        {
            foreach (char character in requestId)
            {
                if (!char.IsAsciiLetterOrDigit(character) && character != '-')
                    return false;
            }

            return true;
        }

        private static bool ReadBoolean(JsonElement element, string propertyName)
            => element.TryGetProperty(propertyName, out JsonElement value)
                && value.ValueKind == JsonValueKind.True;

        private static int ReadInteger(JsonElement element, string propertyName)
            => element.TryGetProperty(propertyName, out JsonElement value)
                && value.TryGetInt32(out int number)
                    ? number
                    : -1;

        private static string ReadScalar(JsonElement element, string propertyName)
            => element.TryGetProperty(propertyName, out JsonElement value)
                && value.ValueKind is JsonValueKind.String or JsonValueKind.Number
                    ? value.ToString()
                    : "unknown";

        private void ClipboardForwarding_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (!IsTrustedClipboardBridgeSource(e.Source))
                return;

            try
            {
                using var message = JsonDocument.Parse(e.WebMessageAsJson);
                JsonElement root = message.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("type", out JsonElement type)
                    || type.ValueKind != JsonValueKind.String
                    || type.GetString() != ClipboardBridgeMessageType
                    || !root.TryGetProperty("requestId", out JsonElement requestIdValue)
                    || requestIdValue.ValueKind != JsonValueKind.String)
                    return;

                string requestId = requestIdValue.GetString()!;
                if (!root.TryGetProperty("phase", out JsonElement phaseValue)
                    || phaseValue.ValueKind != JsonValueKind.String)
                    return;

                string phase = phaseValue.GetString()!;
                if (phase == "bridge-ready")
                {
                    Trace.TraceInformation("Document-start clipboard bridge is active on the configured server origin.");
                    return;
                }

                if (phase == "paste-not-forwarded")
                {
                    ConsumeNativeClipboardPasteGesture();
                    string reason = root.TryGetProperty("reason", out JsonElement reasonValue)
                        && reasonValue.ValueKind == JsonValueKind.String
                        ? reasonValue.GetString()!
                        : "default-handler";
                    string selectionDetails = "selection=unavailable";
                    if (root.TryGetProperty("selection", out JsonElement selection)
                        && selection.ValueKind == JsonValueKind.Object)
                    {
                        bool angular = ReadBoolean(selection, "angularPresent");
                        bool guacamole = ReadBoolean(selection, "guacamoleGlobalPresent");
                        bool scope = ReadBoolean(selection, "scopeFound");
                        int clientCount = ReadInteger(selection, "clientCount");
                        int connectedCount = ReadInteger(selection, "connectedClientCount");
                        string clientState = ReadScalar(selection, "clientState");
                        string connectedState = ReadScalar(selection, "connectedState");
                        string waitingState = ReadScalar(selection, "waitingState");
                        bool clientApi = ReadBoolean(selection, "clientApiAvailable");
                        string classApi = ReadScalar(selection, "clientConstructorStateApi");
                        selectionDetails = $"angular={angular}, guacamole={guacamole}, scope={scope}, clients={clientCount}, connected={connectedCount}, api={clientApi}, state={clientState}, connectedState={connectedState}, waitingState={waitingState}, constructorStateApi={classApi}";
                    }
                    Trace.TraceInformation("Ctrl+V remains with the browser handler ({0}; {1}).", reason, selectionDetails);
                    return;
                }

                if (phase == "paste-triggered")
                {
                    if (requestId.Length is 0 or > 64
                        || !IsValidClipboardRequestId(requestId)
                        || !root.TryGetProperty("shiftPressed", out JsonElement shiftValue)
                        || shiftValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                        return;

                    if (_pendingClipboardPastes.ContainsKey(requestId))
                    {
                        Trace.TraceInformation("Duplicate native clipboard request ignored.");
                        return;
                    }

                    if (!ConsumeNativeClipboardPasteGesture())
                    {
                        Trace.TraceWarning("Rejected clipboard request without a recent native Ctrl+V gesture.");
                        return;
                    }

                    var pendingPaste = new PendingClipboardPaste();
                    _pendingClipboardPastes.Add(requestId, pendingPaste);
                    _ = ProcessLocalClipboardPasteAsync(requestId, shiftValue.GetBoolean(), pendingPaste);
                    return;
                }

                if (!_pendingClipboardPastes.TryGetValue(requestId, out PendingClipboardPaste? pending))
                    return;

                switch (phase)
                {
                    case "session-selected":
                        Trace.TraceInformation("Guacamole clipboard session selected.");
                        break;

                    case "stream-created":
                        string writerMode = root.TryGetProperty("writerMode", out JsonElement writerModeValue)
                            && writerModeValue.ValueKind == JsonValueKind.String
                            ? writerModeValue.GetString()!
                            : "unknown";
                        int streamIndex = ReadInteger(root, "streamIndex");
                        Trace.TraceInformation("Guacamole text/plain clipboard stream created (writer={0}, stream={1}).", writerMode, streamIndex);
                        pending.StreamCreated.TrySetResult(true);
                        break;

                    case "stream-ack":
                        if (!root.TryGetProperty("statusCode", out JsonElement statusValue)
                            || !statusValue.TryGetInt32(out int statusCode)
                            || statusCode >= 0x0100)
                        {
                            Trace.TraceWarning("Guacamole clipboard stream was rejected.");
                            CompleteClipboardTransfer(pending, "stream-acknowledgement-error");
                        }
                        break;

                    case "stream-sent":
                        if (pending.FinalizationStarted || pending.TransferCompleted.Task.IsCompleted)
                            break;

                        pending.FinalizationStarted = true;
                        Trace.TraceInformation("Guacamole clipboard text and end instruction queued; dispatching remote paste.");
                        _ = FinalizeClipboardTransferAsync(requestId, pending);
                        break;

                    case "blob-sent":
                        Trace.TraceInformation("Guacamole clipboard blob queued ({0} base64 characters).", ReadInteger(root, "base64Length"));
                        break;

                    case "completed":
                        Trace.TraceInformation("Guacamole clipboard stream and remote Ctrl+V were queued.");
                        pending.TransferCompleted.TrySetResult(null);
                        break;

                    case "remote-paste-dispatched":
                        Trace.TraceInformation("Remote Ctrl+V dispatched after clipboard stream end.");
                        break;

                    case "failed":
                        string reason = ReadClipboardBridgeFailureReason(root);
                        Trace.TraceWarning("Guacamole clipboard bridge reported failure ({0}).", reason);
                        CompleteClipboardTransfer(pending, reason);
                        break;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed or unrelated page messages.
            }
        }

        private async Task FinalizeClipboardTransferAsync(string requestId, PendingClipboardPaste pending)
        {
            try
            {
                if (!IsClipboardRequestPending(requestId, pending) || pending.TransferCompleted.Task.IsCompleted)
                    return;

                if (_webview2_core == null || !IsTrustedClipboardBridgeSource(_webview2_core.Source))
                {
                    CompleteClipboardTransfer(pending, "untrusted-page");
                    return;
                }

                await _webview2_core.ExecuteScriptAsync(BuildClipboardFinalizeScript(requestId)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Guacamole clipboard finalization failed ({0}).", ex.GetType().Name);
                CompleteClipboardTransfer(pending, "finalization-failed");
            }
        }

        private static void CompleteClipboardTransfer(PendingClipboardPaste pending, string failureReason)
        {
            pending.StreamCreated.TrySetResult(false);
            pending.TransferCompleted.TrySetResult(failureReason);
        }

        private static string ReadClipboardBridgeFailureReason(JsonElement message)
        {
            if (!message.TryGetProperty("reason", out JsonElement reasonValue)
                || reasonValue.ValueKind != JsonValueKind.String)
                return "bridge-failure";

            return reasonValue.GetString() switch
            {
                "no-active-session" => "no-active-session",
                "session-changed" => "session-changed",
                "guacamole-api-unavailable" => "guacamole-api-unavailable",
                "stream-creation-failed" => "stream-creation-failed",
                "stream-send-failed" => "stream-send-failed",
                "stream-acknowledgement-error" => "stream-acknowledgement-error",
                "clipboard-stream-timeout" => "clipboard-stream-timeout",
                "remote-paste-dispatch-failed" => "remote-paste-dispatch-failed",
                _ => "bridge-failure"
            };
        }

        private bool IsTrustedClipboardBridgeSource(string source)
        {
            if (!Uri.TryCreate(source, UriKind.Absolute, out Uri? sourceUri))
                return false;

            return IsSameOrigin(sourceUri, HomeUrl);
        }

        private static bool IsSameOrigin(Uri left, Uri right)
            => string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
                && left.Port == right.Port;

        private void CancelPendingClipboardPastes(string reason)
        {
            _nativeClipboardPasteGestures.Clear();
            if (_pendingClipboardPastes.Count == 0)
                return;

            Trace.TraceInformation("Cancelling {0} pending clipboard forwarding request(s) ({1}).", _pendingClipboardPastes.Count, reason);
            foreach (PendingClipboardPaste pending in _pendingClipboardPastes.Values)
            {
                CompleteClipboardTransfer(pending, reason);
            }

            _pendingClipboardPastes.Clear();
        }

        private async Task CancelClipboardBridgeRequestAsync(string requestId)
        {
            try
            {
                if (_webview2_core != null && IsTrustedClipboardBridgeSource(_webview2_core.Source))
                    await _webview2_core.ExecuteScriptAsync(BuildClipboardCancelScript(requestId)).ConfigureAwait(true);
            }
            catch
            {
                // The page may already have navigated or the connection may have closed.
            }
        }

        private static string BuildClipboardKeyboardHookScript(string allowedOrigin)
        {
            string serializedOrigin = JsonSerializer.Serialize(allowedOrigin);
            return $$"""
                (() => {
                    if (window.top !== window || location.origin !== {{serializedOrigin}})
                        return;

                    {{ClipboardTargetSelectionJavaScript}}

                    const webview = window.chrome && window.chrome.webview;
                    if (!webview || typeof webview.postMessage !== 'function')
                        return;

                    webview.postMessage({
                        type: 'guacamoleClient.clipboardForwarding',
                        requestId: 'bridge-ready',
                        phase: 'bridge-ready'
                    });

                    const requests = window.__guacamoleClientNativeClipboardRequests ||
                        (window.__guacamoleClientNativeClipboardRequests = Object.create(null));
                    const suppressedPasteKeyUps = new Set();
                    const suppressedPasteEvents = [];
                    let requestCounter = 0;

                    const isVisibleEditableTarget = (target) => {
                        if (!(target instanceof Element))
                            return false;

                        const editable = target.closest('input, textarea, select, [contenteditable="true"], [contenteditable=""]');
                        if (!editable)
                            return false;

                        const style = window.getComputedStyle(editable);
                        const bounds = editable.getBoundingClientRect();
                        return style.display !== 'none'
                            && style.visibility !== 'hidden'
                            && Number(style.opacity) > 0
                            && bounds.width > 1
                            && bounds.height > 1
                            && bounds.right > 0
                            && bounds.bottom > 0;
                    };

                    const reportDefaultPaste = (reason) => webview.postMessage({
                        type: 'guacamoleClient.clipboardForwarding',
                        requestId: 'ignored',
                        phase: 'paste-not-forwarded',
                        reason,
                        selection: window.__guacamoleClientClipboardSelectionDiagnostics || null
                    });

                    document.addEventListener('keydown', (event) => {
                        if (!event.isTrusted
                            || !event.ctrlKey
                            || event.altKey
                            || event.metaKey
                            || String(event.key).toLowerCase() !== 'v'
                            || !location.hash.startsWith('#/client/'))
                            return;

                        const keyCode = event.code || 'KeyV';
                        if (event.repeat && suppressedPasteKeyUps.has(keyCode)) {
                            event.preventDefault();
                            event.stopPropagation();
                            event.stopImmediatePropagation();
                            return;
                        }

                        if (isVisibleEditableTarget(event.target)) {
                            reportDefaultPaste('editable-target');
                            return;
                        }

                        const selected = findActiveTarget();
                        if (!selected) {
                            reportDefaultPaste('no-active-session');
                            return;
                        }

                        const requestId = typeof crypto.randomUUID === 'function'
                            ? crypto.randomUUID()
                            : `${Date.now()}-${++requestCounter}-${Math.random().toString(36).slice(2)}`;
                        requests[requestId] = selected;
                        suppressedPasteKeyUps.add(keyCode);
                        const pasteToken = {};
                        suppressedPasteEvents.push(pasteToken);
                        setTimeout(() => delete requests[requestId], 35000);
                        setTimeout(() => suppressedPasteKeyUps.delete(keyCode), 5000);
                        setTimeout(() => {
                            const tokenIndex = suppressedPasteEvents.indexOf(pasteToken);
                            if (tokenIndex >= 0)
                                suppressedPasteEvents.splice(tokenIndex, 1);
                        }, 5000);

                        // Own this single paste. Suppress Guacamole's keyboard and paste handlers;
                        // the native bridge ends the clipboard stream before sending remote Ctrl+V.
                        event.preventDefault();
                        event.stopPropagation();
                        event.stopImmediatePropagation();
                        webview.postMessage({
                            type: 'guacamoleClient.clipboardForwarding',
                            requestId,
                            phase: 'paste-triggered',
                            shiftPressed: event.shiftKey
                        });
                    }, true);

                    document.addEventListener('keyup', (event) => {
                        const keyCode = event.code || 'KeyV';
                        if (!suppressedPasteKeyUps.has(keyCode))
                            return;

                        suppressedPasteKeyUps.delete(keyCode);
                        event.preventDefault();
                        event.stopPropagation();
                        event.stopImmediatePropagation();
                    }, true);

                    document.addEventListener('paste', (event) => {
                        if (suppressedPasteEvents.length === 0)
                            return;

                        suppressedPasteEvents.shift();
                        event.preventDefault();
                        event.stopPropagation();
                        event.stopImmediatePropagation();
                    }, true);
                })();
                """;
        }

        private static string BuildClipboardStartScript(string requestId, string text, bool shiftPressed)
        {
            string serializedRequestId = JsonSerializer.Serialize(requestId);
            string serializedText = ClipboardTextBridgeEncoding.SerializeForJavaScript(text);
            string serializedShift = JsonSerializer.Serialize(shiftPressed);
            return $$"""
                (() => {
                    const requestId = {{serializedRequestId}};
                    const text = {{serializedText}};
                    const shiftPressed = {{serializedShift}};
                    const post = (phase, details = {}) => window.chrome?.webview?.postMessage({
                        type: 'guacamoleClient.clipboardForwarding', requestId, phase, ...details
                    });
                    {{ClipboardTargetSelectionJavaScript}}

                    let stream = null;
                    let transfer = null;
                    try {
                        const requests = window.__guacamoleClientNativeClipboardRequests || Object.create(null);
                        const request = requests[requestId];
                        const selected = findActiveTarget();
                        if (!request || !selected || selected.client !== request.client) {
                            post('failed', { reason: 'session-changed' });
                            return;
                        }

                        if (typeof selected.client.createClipboardStream !== 'function'
                            || typeof selected.client.sendKeyEvent !== 'function') {
                            post('failed', { reason: 'guacamole-api-unavailable' });
                            return;
                        }

                        post('session-selected');
                        stream = selected.client.createClipboardStream('text/plain');
                        const Guacamole = window.Guacamole || {};
                        const StringWriter = Guacamole.StringWriter;
                        const ArrayBufferWriter = Guacamole.ArrayBufferWriter;
                        const encodedText = new TextEncoder().encode(text);
                        const blobLength = Number(ArrayBufferWriter && ArrayBufferWriter.DEFAULT_BLOB_LENGTH) || 6048;
                        const byteLength = encodedText.byteLength;
                        if (!byteLength) {
                            post('failed', { reason: 'stream-send-failed' });
                            return;
                        }

                        let writer;
                        let ackSource;
                        if (typeof StringWriter === 'function') {
                            writer = new StringWriter(stream);
                            ackSource = writer;
                        }
                        else {
                            writer = {
                                sendText: (value) => {
                                    for (let offset = 0; offset < encodedText.length; offset += blobLength) {
                                        const bytes = encodedText.subarray(offset, offset + blobLength);
                                        let binary = '';
                                        for (let i = 0; i < bytes.length; i++)
                                            binary += String.fromCharCode(bytes[i]);
                                        stream.sendBlob(window.btoa(binary));
                                    }
                                },
                                sendEnd: () => stream.sendEnd()
                            };
                            ackSource = stream;
                        }

                        const transfers = window.__guacamoleClientNativeClipboardTransfers ||
                            (window.__guacamoleClientNativeClipboardTransfers = Object.create(null));
                        transfer = transfers[requestId] = { selected, stream, writer, shiftPressed, ended: false, rejected: false };
                        ackSource.onack = (status) => {
                            const statusCode = Number(status && status.code);
                            if (!Number.isFinite(statusCode) || statusCode >= 0x0100)
                                transfer.rejected = true;
                            post('stream-ack', { statusCode });
                        };

                        // Report stream ownership before sending data so no second paste handler runs.
                        post('stream-created', {
                            writerMode: StringWriter ? 'StringWriter' : 'UTF8-sendBlob',
                            streamIndex: stream.index
                        });
                        const sendBlob = stream.sendBlob;
                        stream.sendBlob = function(data) {
                            post('blob-sent', { base64Length: data.length });
                            return sendBlob.call(stream, data);
                        };
                        writer.sendText(text);
                        stream.sendBlob = sendBlob;

                        // Match ManagedClient.setClipboard(): RDP clipboard blobs have no success ACKs.
                        // The server only advertises the new clipboard contents after receiving end.
                        writer.sendEnd();
                        transfer.ended = true;
                        if (transfer.rejected) {
                            post('failed', { reason: 'stream-acknowledgement-error' });
                            return;
                        }
                        post('stream-sent');
                    }
                    catch (_) {
                        if (transfer)
                            transfer.rejected = true;
                        if (stream && (!transfer || !transfer.ended) && typeof stream.sendEnd === 'function') {
                            try {
                                stream.sendEnd();
                                if (transfer)
                                    transfer.ended = true;
                            }
                            catch (_) { }
                        }
                        post('failed', { reason: stream ? 'stream-send-failed' : 'stream-creation-failed' });
                    }
                })();
                """;
        }

        private static string BuildClipboardFinalizeScript(string requestId)
        {
            string serializedRequestId = JsonSerializer.Serialize(requestId);
            return $$"""
                (() => {
                    const requestId = {{serializedRequestId}};
                    const post = (phase, details = {}) => window.chrome?.webview?.postMessage({
                        type: 'guacamoleClient.clipboardForwarding', requestId, phase, ...details
                    });
                    {{ClipboardTargetSelectionJavaScript}}

                    const requests = window.__guacamoleClientNativeClipboardRequests || Object.create(null);
                    const transfers = window.__guacamoleClientNativeClipboardTransfers || Object.create(null);
                    const transfer = transfers[requestId];
                    if (!transfer) {
                        post('failed', { reason: 'session-changed' });
                        return;
                    }

                    try {
                        const selected = findActiveTarget();
                        if (!selected || selected.client !== transfer.selected.client) {
                            delete transfers[requestId];
                            delete requests[requestId];
                            post('failed', { reason: 'session-changed' });
                            return;
                        }

                        if (!transfer.ended || transfer.rejected) {
                            post('failed', { reason: 'stream-send-failed' });
                            return;
                        }

                        // Guacamole serializes key states directly. guacd requires numeric 1/0.
                        const client = transfer.selected.client;
                        const control = 0xFFE3;
                        const shift = 0xFFE1;
                        const pasteKey = transfer.shiftPressed ? 0x0056 : 0x0076;
                        let controlDown = false;
                        let shiftDown = false;
                        let pasteKeyDown = false;
                        try {
                            client.sendKeyEvent(1, control);
                            controlDown = true;
                            if (transfer.shiftPressed) {
                                client.sendKeyEvent(1, shift);
                                shiftDown = true;
                            }
                            client.sendKeyEvent(1, pasteKey);
                            pasteKeyDown = true;
                        }
                        finally {
                            if (pasteKeyDown)
                                client.sendKeyEvent(0, pasteKey);
                            if (shiftDown)
                                client.sendKeyEvent(0, shift);
                            if (controlDown)
                                client.sendKeyEvent(0, control);
                        }

                        post('remote-paste-dispatched');
                        delete transfers[requestId];
                        delete requests[requestId];
                        post('completed');
                    }
                    catch (_) {
                        delete transfers[requestId];
                        delete requests[requestId];
                        post('failed', { reason: 'remote-paste-dispatch-failed' });
                    }
                })();
                """;
        }

        private static string BuildClipboardCancelScript(string requestId)
        {
            string serializedRequestId = JsonSerializer.Serialize(requestId);
            return $$"""
                (() => {
                    const requestId = {{serializedRequestId}};
                    const requests = window.__guacamoleClientNativeClipboardRequests || Object.create(null);
                    const transfers = window.__guacamoleClientNativeClipboardTransfers || Object.create(null);
                    const transfer = transfers[requestId];
                    if (transfer && !transfer.ended && transfer.writer && typeof transfer.writer.sendEnd === 'function') {
                        try { transfer.writer.sendEnd(); }
                        catch (_) { }
                    }
                    delete transfers[requestId];
                    delete requests[requestId];
                })();
                """;
        }

        private const string ClipboardTargetSelectionJavaScript = """
            function findActiveTarget() {
                const angular = window.angular;
                const Guacamole = window.Guacamole;
                const diagnostics = {
                    angularPresent: !!angular,
                    guacamoleGlobalPresent: !!Guacamole,
                    scopeFound: false,
                    clientCount: 0,
                    connectedClientCount: 0,
                    clientState: 'unknown',
                    connectedState: 'unknown',
                    waitingState: 'unknown',
                    clientApiAvailable: false,
                    clientConstructorStateApi: false
                };
                window.__guacamoleClientClipboardSelectionDiagnostics = diagnostics;

                if (!angular || typeof angular.element !== 'function')
                    return null;

                const candidates = [
                    document.querySelector('[ng-controller="clientController"]'),
                    document.querySelector('.client'),
                    document.querySelector('[ng-view]'),
                    document.body,
                    document.documentElement
                ].filter(Boolean);

                let scope = null;
                for (const candidate of candidates) {
                    let current = candidate;
                    while (current) {
                        const wrapped = angular.element(current);
                        const localScope =
                            (typeof wrapped.scope === 'function' && wrapped.scope()) ||
                            (typeof wrapped.isolateScope === 'function' && wrapped.isolateScope());
                        if (localScope && (localScope.focusedClient || localScope.clientGroup)) {
                            scope = localScope;
                            break;
                        }
                        current = current.parentElement || current.parentNode;
                    }
                    if (scope)
                        break;
                }

                if (!scope)
                    return null;

                diagnostics.scopeFound = true;

                const clients = scope.clientGroup && Array.isArray(scope.clientGroup.clients)
                    ? scope.clientGroup.clients
                    : [];
                diagnostics.clientCount = clients.length;
                const connected = clients.filter((target) => {
                    if (!target || !target.client)
                        return false;
                    const client = target.client;
                    const managedState = target.clientState && target.clientState.connectionState;
                    const clientState = typeof client.getState === 'function' ? client.getState() : client.state;
                    const states = (Guacamole && Guacamole.Client && Guacamole.Client.State)
                        || (client.constructor && client.constructor.State);
                    const apiAvailable = typeof client.createClipboardStream === 'function'
                        && typeof client.sendKeyEvent === 'function';
                    const state = managedState || clientState;
                    diagnostics.clientState = state === undefined || state === null ? 'null' : String(state);
                    diagnostics.connectedState = states && states.CONNECTED !== undefined ? String(states.CONNECTED) : 'unknown';
                    diagnostics.waitingState = states && states.WAITING !== undefined ? String(states.WAITING) : 'unknown';
                    diagnostics.clientApiAvailable = apiAvailable;
                    diagnostics.clientConstructorStateApi = !!(client.constructor && client.constructor.State);
                    if (!apiAvailable)
                        return false;

                    // The Guacamole Angular app wraps Guacamole.Client and tracks its active state separately.
                    if (managedState !== undefined && managedState !== null)
                        return managedState === 'CONNECTED' || managedState === 'WAITING';

                    if (states)
                        return state === states.CONNECTED || state === states.WAITING;

                    // Guacamole.Client state values are stable across the client API versions used by the web app.
                    return state === 2 || state === 3 || state === 'WAITING' || state === 'CONNECTED';
                });
                diagnostics.connectedClientCount = connected.length;
                const focused = scope.focusedClient ||
                    connected.find((target) => target.clientProperties && target.clientProperties.focused) || null;
                const target = focused && connected.includes(focused)
                    ? focused
                    : connected.length === 1
                        ? connected[0]
                        : null;
                return target ? { target, client: target.client } : null;
            }
            """;

        private sealed class PendingClipboardPaste
        {
            public TaskCompletionSource<bool> StreamCreated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<string?> TransferCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool FinalizationStarted { get; set; }
        }
    }
}
