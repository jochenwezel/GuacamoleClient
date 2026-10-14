# Clipboard bridge regression tests

Run from the repository root with Node.js 18 or later:

```powershell
node --test tests/clipboard-forwarding/clipboard-bridge.test.mjs
```

The tests execute the JavaScript embedded in `MainForm.Clipboard.cs` in an isolated
context with Guacamole stream and tunnel doubles. They verify stream-end ordering
without success acknowledgements, numeric protocol key states, UTF-8 fallback
chunking, rejection, cancellation, session changes, and repeated pastes.

These tests complement `GuacamoleCommon.Tests`. Live WebView2/Windows clipboard and
RDP validation is tracked in `task_progress.md`.
