import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

// Execute the production bridge scripts, including target selection, without WebView2.
const source = readFileSync(new URL('../../GuacamoleClient-WinForms/MainForm.Clipboard.cs', import.meta.url), 'utf8');
const selection = source.match(/private const string ClipboardTargetSelectionJavaScript = """\r?\n([\s\S]*?)\r?\n\s*""";/)[1];

function script(name, { requestId = 'paste-1', text = 'clipboard text', shift = false } = {}) {
    const method = source.slice(source.indexOf(`private static string ${name}(`));
    const raw = method.match(/return \$\$"""\r?\n([\s\S]*?)\r?\n\s*""";/)[1];
    return raw.replaceAll('{{serializedRequestId}}', JSON.stringify(requestId))
        .replaceAll('{{serializedText}}', JSON.stringify(text))
        .replaceAll('{{serializedShift}}', JSON.stringify(shift))
        .replaceAll('{{ClipboardTargetSelectionJavaScript}}', selection);
}

function bridge({ stringWriter = true, reject = false, failSend = false, failPaste = false } = {}) {
    const wire = [];
    const messages = [];
    const streams = [];
    const client = {
        createClipboardStream(mimetype) {
            const index = streams.length;
            wire.push(['clipboard', String(index), mimetype]);
            const stream = {
                index,
                sendBlob(data) {
                    if (failSend) throw new Error('send failed');
                    wire.push(['blob', String(index), data]);
                    if (reject) stream.onack?.({ code: 0x0100 });
                },
                sendEnd() { wire.push(['end', String(index)]); }
            };
            streams.push(stream);
            return stream;
        },
        sendKeyEvent(pressed, keysym) {
            // Match Guacamole.Tunnel serialization: booleans become "true"/"false".
            if (failPaste && keysym === 0x0076 && pressed === 1) throw new Error('key failed');
            wire.push(['key', String(keysym), String(pressed)]);
        }
    };
    const target = { client, clientState: { connectionState: 'CONNECTED' } };
    const scope = { clientGroup: { clients: [target] }, focusedClient: target };
    const requests = { 'paste-1': { target, client } };
    const Guacamole = {};
    if (stringWriter) {
        Guacamole.StringWriter = function(stream) {
            stream.onack = status => this.onack?.(status);
            this.sendText = text => stream.sendBlob(Buffer.from(text, 'utf8').toString('base64'));
            this.sendEnd = () => stream.sendEnd();
        };
    }
    const element = {};
    const window = {
        Guacamole,
        angular: { element: () => ({ scope: () => scope }) },
        chrome: { webview: { postMessage: message => messages.push(message) } },
        btoa: text => Buffer.from(text, 'binary').toString('base64'),
        __guacamoleClientNativeClipboardRequests: requests
    };
    const context = vm.createContext({ window, document: { querySelector: () => element }, TextEncoder });
    return {
        wire, messages, streams, scope, target, requests, window,
        run(name, values) { vm.runInContext(script(name, values), context); },
        phases() { return messages.map(message => message.phase); },
        text() { return Buffer.concat(wire.filter(item => item[0] === 'blob').map(item => Buffer.from(item[2], 'base64'))).toString('utf8'); }
    };
}

test('RDP text transfer ends without any success ACK and then sends numeric key states', () => {
    const b = bridge();
    b.run('BuildClipboardStartScript');
    assert.ok(b.phases().includes('stream-sent'));
    assert.deepEqual(b.wire.map(item => item[0]), ['clipboard', 'blob', 'end']);
    b.run('BuildClipboardFinalizeScript');
    assert.deepEqual(b.wire.slice(3), [
        ['key', '65507', '1'], ['key', '118', '1'],
        ['key', '118', '0'], ['key', '65507', '0']
    ]);
    assert.equal(b.phases().at(-1), 'completed');
    assert.equal(Object.keys(b.window.__guacamoleClientNativeClipboardTransfers).length, 0);
    assert.equal(Object.keys(b.requests).length, 0);
});

for (const stringWriter of [true, false]) {
    test(`Unicode, multiline and script syntax survive ${stringWriter ? 'StringWriter' : 'UTF-8 fallback'}`, () => {
        const b = bridge({ stringWriter });
        const text = 'Grüße 東京 😀\r\n"quoted" \\ </script>\u2028\u2029';
        b.run('BuildClipboardStartScript', { text });
        assert.equal(b.text(), text);
        assert.equal(b.wire.at(-1)[0], 'end');
    });
}

test('large fallback transfer queues every byte before ending and pasting', () => {
    const b = bridge({ stringWriter: false });
    const text = '😀 東京\r\n'.repeat(5000);
    b.run('BuildClipboardStartScript', { text });
    assert.ok(b.streams.length === 1 && b.wire.filter(item => item[0] === 'blob').length > 2);
    assert.equal(b.text(), text);
    b.run('BuildClipboardFinalizeScript');
    const firstKey = b.wire.findIndex(item => item[0] === 'key');
    assert.equal(b.wire[firstKey - 1][0], 'end');
});

test('explicit stream rejection blocks paste even if finalization is attempted', () => {
    const b = bridge({ reject: true });
    b.run('BuildClipboardStartScript');
    assert.ok(!b.phases().includes('stream-sent'));
    b.run('BuildClipboardFinalizeScript');
    assert.ok(!b.wire.some(item => item[0] === 'key'));
    assert.equal(b.phases().at(-1), 'failed');
});

test('send failure never reports stream-sent or dispatches paste', () => {
    const b = bridge({ failSend: true });
    b.run('BuildClipboardStartScript');
    b.run('BuildClipboardFinalizeScript');
    b.run('BuildClipboardCancelScript');
    assert.ok(!b.phases().includes('stream-sent'));
    assert.ok(!b.wire.some(item => item[0] === 'key'));
    assert.equal(b.wire.filter(item => item[0] === 'end').length, 1);
});

test('a rejection received before finalization still suppresses paste', () => {
    const b = bridge();
    b.run('BuildClipboardStartScript');
    b.streams[0].onack({ code: 0x030F });
    b.run('BuildClipboardFinalizeScript');
    assert.ok(!b.wire.some(item => item[0] === 'key'));
});

test('a request from a replaced connection cannot create a stream after reconnect', () => {
    const b = bridge();
    b.requests['paste-1'].client = { ...b.target.client };
    b.run('BuildClipboardStartScript');
    assert.equal(b.streams.length, 0);
    assert.equal(b.messages.at(-1).reason, 'session-changed');
});

test('switching sessions between transfer and paste suppresses remote keys', () => {
    const b = bridge();
    b.run('BuildClipboardStartScript');
    const other = { client: { ...b.target.client }, clientState: { connectionState: 'CONNECTED' } };
    b.scope.clientGroup.clients = [other];
    b.scope.focusedClient = other;
    b.run('BuildClipboardFinalizeScript');
    assert.ok(!b.wire.some(item => item[0] === 'key'));
    assert.equal(b.messages.at(-1).reason, 'session-changed');
    assert.equal(b.wire.filter(item => item[0] === 'end').length, 1);
});

test('disconnecting before finalization suppresses remote keys', () => {
    const b = bridge();
    b.run('BuildClipboardStartScript');
    b.target.clientState.connectionState = 'DISCONNECTED';
    b.run('BuildClipboardFinalizeScript');
    assert.ok(!b.wire.some(item => item[0] === 'key'));
});

test('cancelling an ended transfer does not end it twice or paste', () => {
    const b = bridge();
    b.run('BuildClipboardStartScript');
    b.run('BuildClipboardCancelScript');
    b.run('BuildClipboardFinalizeScript');
    assert.equal(b.wire.filter(item => item[0] === 'end').length, 1);
    assert.ok(!b.wire.some(item => item[0] === 'key'));
});

test('Ctrl+Shift+V releases all three keys with numeric states', () => {
    const b = bridge();
    b.run('BuildClipboardStartScript', { shift: true });
    b.run('BuildClipboardFinalizeScript');
    assert.deepEqual(b.wire.filter(item => item[0] === 'key'), [
        ['key', '65507', '1'], ['key', '65505', '1'], ['key', '86', '1'],
        ['key', '86', '0'], ['key', '65505', '0'], ['key', '65507', '0']
    ]);
});

test('a key dispatch failure releases Control and reports failure', () => {
    const b = bridge({ failPaste: true });
    b.run('BuildClipboardStartScript');
    b.run('BuildClipboardFinalizeScript');
    assert.deepEqual(b.wire.at(-1), ['key', '65507', '0']);
    assert.equal(b.messages.at(-1).reason, 'remote-paste-dispatch-failed');
});

test('repeated pastes complete independently without waiting for ACKs', () => {
    const b = bridge();
    for (let i = 0; i < 5; i++) {
        const requestId = `paste-${i}`;
        b.requests[requestId] = { target: b.target, client: b.target.client };
        b.run('BuildClipboardStartScript', { requestId, text: `text ${i}` });
        b.run('BuildClipboardFinalizeScript', { requestId });
    }
    assert.equal(b.phases().filter(phase => phase === 'completed').length, 5);
    assert.equal(b.wire.filter(item => item[0] === 'end').length, 5);
});
