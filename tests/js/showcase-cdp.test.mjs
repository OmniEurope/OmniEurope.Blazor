import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createCdpLink, listenToSocket } from '../../eng/ShowcaseCdp.mjs';

// The CDP link of the showcase probes reads a message only when it comes from the debugging endpoint its
// socket was opened on; a message of another origin breaks the link instead of being read. A live Edge
// answering through the same link is checked by the probes themselves (eng/Test-ShowcaseHost.ps1).

class FakeSocket extends EventTarget {
  constructor(url) {
    super();
    this.url = url;
    this.sent = [];
  }

  send(text) {
    this.sent.push(JSON.parse(text));
  }

  receive(origin, message) {
    this.dispatchEvent(new MessageEvent('message', { origin, data: JSON.stringify(message) }));
  }
}

const socketUrl = 'ws://127.0.0.1:9222/devtools/page/ABC';

test('listenToSocket reads a message of the socket origin and reports any other origin unread', () => {
  const socket = new FakeSocket(socketUrl);
  const read = [];
  const foreign = [];
  listenToSocket(socket, message => read.push(message), origin => foreign.push(origin));

  socket.receive('ws://127.0.0.1:9222', { id: 1, result: {} });
  socket.receive('https://evil.example', { id: 2, result: {} });

  assert.deepEqual(read, [{ id: 1, result: {} }]);
  assert.deepEqual(foreign, ['https://evil.example']);
});

test('createCdpLink answers a command from the socket origin and breaks on a foreign one', async () => {
  const socket = new FakeSocket(socketUrl);
  const link = createCdpLink(socket, { commandTimeout: 5_000 });

  const answered = link.sendCdp('Runtime.evaluate', { expression: '6*7' });
  socket.receive('ws://127.0.0.1:9222', { id: socket.sent[0].id, result: { value: 42 } });
  assert.deepEqual(await answered, { value: 42 });

  const waiting = link.sendCdp('Runtime.evaluate', { expression: '1' });
  socket.receive('https://evil.example', { id: socket.sent[1].id, result: { value: 1 } });
  await assert.rejects(waiting, /origine inattendue \(https:\/\/evil\.example\)/);
  await assert.rejects(link.sendCdp('Page.enable'), /origine inattendue/);
});
