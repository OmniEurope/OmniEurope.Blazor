// Drives the mind map demonstration of the published showcase in a real Chromium through CDP, with
// trusted input events: a mouse drag, a wheel zoom, keyboard commands, the context menu and a double
// click. It fails on any console error or Content Security Policy violation, the showcase being
// served with `style-src 'self'`.
//
// Usage: node Test-ShowcaseMindMapProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// The browser (started with --remote-debugging-port) and the static server are the caller's.

const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const endpoint = options.get('--endpoint');
const siteUrl = options.get('--url');
if (!endpoint || !siteUrl) {
  throw new Error('Usage: node Test-ShowcaseMindMapProbe.mjs --endpoint <cdp url> --url <showcase root url>');
}

const deadline = Date.now() + 20_000;
let target;
while (Date.now() < deadline) {
  try {
    const targets = await fetch(`${endpoint}/json/list`).then(response => response.json());
    target = targets.find(candidate => candidate.type === 'page' && candidate.webSocketDebuggerUrl);
    if (target) break;
  } catch {
    // The browser may still be starting.
  }
  await new Promise(resolve => setTimeout(resolve, 100));
}
if (!target) throw new Error(`Aucune cible CDP disponible sur ${endpoint}.`);

const socket = new WebSocket(target.webSocketDebuggerUrl);
await new Promise((resolve, reject) => {
  socket.addEventListener('open', resolve, { once: true });
  socket.addEventListener('error', () => reject(new Error('Connexion CDP impossible.')), { once: true });
});

let commandId = 0;
const pending = new Map();
const consoleErrors = [];
socket.addEventListener('message', event => {
  const message = JSON.parse(event.data);
  if (message.id && pending.has(message.id)) {
    const { resolve, reject } = pending.get(message.id);
    pending.delete(message.id);
    if (message.error) reject(new Error(message.error.message));
    else resolve(message.result);
    return;
  }

  if (message.method === 'Runtime.exceptionThrown') {
    consoleErrors.push(message.params.exceptionDetails.exception?.description ?? message.params.exceptionDetails.text);
  } else if (message.method === 'Log.entryAdded' && message.params.entry.level === 'error') {
    const source = message.params.entry.url ? ` (${message.params.entry.url})` : '';
    consoleErrors.push(`${message.params.entry.text}${source}`);
  } else if (message.method === 'Runtime.consoleAPICalled' && message.params.type === 'error') {
    consoleErrors.push(message.params.args.map(argument => argument.value ?? argument.description ?? '').join(' '));
  }
});

const send = (method, params = {}) => {
  const id = ++commandId;
  socket.send(JSON.stringify({ id, method, params }));
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
};

const evaluate = async expression => {
  const response = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
  if (response.exceptionDetails) throw new Error(response.exceptionDetails.exception?.description ?? response.exceptionDetails.text);
  return response.result.value;
};

const pause = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

const waitFor = async (description, expression, timeout = 15_000) => {
  const until = Date.now() + timeout;
  while (Date.now() < until) {
    try {
      if (await evaluate(expression)) return;
    } catch (error) {
      if (!String(error.message).includes('context was destroyed')) throw error;
    }
    await pause(100);
  }
  throw new Error(`Attente dépassée : ${description}.`);
};

const check = (condition, message) => {
  if (!condition) throw new Error(message);
};

const map = '#demo-mindmap';
const node = id => `document.querySelector('${map} [data-omni-node="${id}"]')`;
const centerOf = async id => evaluate(`(() => { const box = ${node(id)}.querySelector('rect').getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`);
const mouse = (type, point, extra = {}) => send('Input.dispatchMouseEvent', { type, x: point.x, y: point.y, button: 'left', clickCount: 1, ...extra });
const key = (keyName, code, text, modifiers = 0) => Promise.all([
  send('Input.dispatchKeyEvent', { type: text ? 'keyDown' : 'rawKeyDown', key: keyName, code, text, modifiers, windowsVirtualKeyCode: keyName.length === 1 ? keyName.toUpperCase().charCodeAt(0) : undefined }),
  send('Input.dispatchKeyEvent', { type: 'keyUp', key: keyName, code, modifiers })
]);

await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
await send('Page.addScriptToEvaluateOnNewDocument', {
  source: "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
});
await send('Page.navigate', { url: siteUrl });
await waitFor('le runtime Blazor', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.querySelector('main, #app') !== null");
await pause(1500);
await evaluate("Blazor.navigateTo('/composants/diagramme')");
await waitFor('la carte mentale de la vitrine', `document.querySelectorAll('${map} [data-omni-node]').length === 10`);
await evaluate(`document.querySelector('${map} svg').scrollIntoView({ block: 'center' })`);
await waitFor('l\'ajustement initial', `Number(document.querySelector('${map} .omni-mindmap__viewport').getAttribute('data-zoom')) > 0.2`);
const results = [];

// Drag: the node follows the pointer, its links with it, and the drop is committed by .NET.
const before = await evaluate(`${node('node_5')}.getAttribute('data-y')`);
const start = await centerOf('node_5');
await mouse('mouseMoved', start, { button: 'none' });
await mouse('mousePressed', start);
for (let step = 1; step <= 8; step++) {
  await mouse('mouseMoved', { x: start.x, y: start.y + (step * 10) }, { buttons: 1 });
  await pause(16);
}
await mouse('mouseReleased', { x: start.x, y: start.y + 80 });
await waitFor('la propriété du nœud déplacé', `document.querySelector('${map} .omni-mindmap-properties input')?.value === 'Cartons'`);
const dragged = await evaluate(`(() => {
  const moved = ${node('node_5')};
  const y = moved.getAttribute('data-y');
  const edge = document.querySelector('${map} [data-from="node_5"][data-to="node_6"] .omni-mindmap__edge-line').getAttribute('d');
  return { y, transform: moved.getAttribute('transform'), edge, selected: moved.getAttribute('data-omni-selected'), live: document.querySelector('${map} [role=status]').textContent.trim() };
})()`);
check(Number(dragged.y) > Number(before) + 100, `Le glisser n'a pas déplacé le nœud : y ${before} puis ${dragged.y}.`);
check(dragged.transform === `translate(260 ${dragged.y})`, `Transformation inattendue après le glisser : ${dragged.transform}.`);
check(dragged.edge.startsWith(`M 260 ${dragged.y} `), `Le lien n'a pas suivi le nœud : ${dragged.edge}.`);
check(dragged.selected === 'true' && dragged.live.startsWith('Cartons'), `Sélection ou annonce absente : ${JSON.stringify(dragged)}.`);
results.push(`glisser y ${before} -> ${dragged.y}`);

// Wheel: zooms about the pointer, reported to .NET once it settles.
const zoomBefore = await evaluate(`Number(document.querySelector('${map} .omni-mindmap__viewport').getAttribute('data-zoom'))`);
const canvasCenter = await evaluate(`(() => { const box = document.querySelector('${map} svg').getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`);
await send('Input.dispatchMouseEvent', { type: 'mouseWheel', x: canvasCenter.x, y: canvasCenter.y, deltaX: 0, deltaY: -120 });
await pause(400);
const zoomAfter = await evaluate(`Number(document.querySelector('${map} .omni-mindmap__viewport').getAttribute('data-zoom'))`);
check(zoomAfter > zoomBefore, `La molette n'a pas zoomé : ${zoomBefore} puis ${zoomAfter}.`);
results.push(`molette ${zoomBefore} -> ${zoomAfter}`);

// Keyboard: the canvas has the focus after the press; arrows move the selection, N adds, Ctrl+Z undoes.
check(await evaluate(`document.activeElement === document.querySelector('${map} svg')`), 'Le canevas n\'a pas le focus après le clic.');
const activeBefore = await evaluate(`document.querySelector('${map} svg').getAttribute('aria-activedescendant')`);
await key('ArrowLeft', 'ArrowLeft');
await pause(200);
const activeAfter = await evaluate(`document.querySelector('${map} svg').getAttribute('aria-activedescendant')`);
check(activeAfter && activeAfter !== activeBefore, `Les flèches n'ont pas déplacé la sélection : ${activeBefore} puis ${activeAfter}.`);
await key('n', 'KeyN', 'n');
await waitFor('le nœud ajouté au clavier', `document.querySelectorAll('${map} [data-omni-node]').length === 11`);
await key('z', 'KeyZ', 'z', 2);
await waitFor('l\'annulation au clavier', `document.querySelectorAll('${map} [data-omni-node]').length === 10`);
results.push('clavier flèches, N, Ctrl+Z');

// Context menu: opens on a node, takes the focus, closes with Escape and gives it back.
const root = await centerOf('node_1');
await mouse('mousePressed', root, { button: 'right' });
await mouse('mouseReleased', root, { button: 'right' });
await waitFor('le menu contextuel', `document.querySelector('${map} [role=menu]') !== null`);
await waitFor('le focus dans le menu', `document.activeElement?.getAttribute('role') === 'menuitem'`);
const items = await evaluate(`[...document.querySelectorAll('${map} [role=menu] [role=menuitem]')].map(item => item.textContent.trim()).join('|')`);
check(items === 'Renommer le nœud|Dupliquer|Lien|Centrer|Supprimer', `Actions du menu inattendues : ${items}.`);
await key('ArrowDown', 'ArrowDown');
await pause(100);
check(await evaluate("document.activeElement?.textContent?.trim() === 'Dupliquer'"), 'La flèche bas ne parcourt pas le menu.');
await key('Escape', 'Escape');
await waitFor('la fermeture du menu', `document.querySelector('${map} [role=menu]') === null`);
await waitFor('le retour du focus au canevas', `document.activeElement === document.querySelector('${map} svg')`);
results.push('menu contextuel');

// Double click on the background adds a node where it was clicked.
const empty = await evaluate(`(() => { const box = document.querySelector('${map} svg').getBoundingClientRect(); return { x: box.left + 30, y: box.bottom - 30 }; })()`);
await mouse('mousePressed', empty, { clickCount: 1 });
await mouse('mouseReleased', empty, { clickCount: 1 });
await mouse('mousePressed', empty, { clickCount: 2 });
await mouse('mouseReleased', empty, { clickCount: 2 });
await waitFor('le nœud ajouté par double clic', `document.querySelectorAll('${map} [data-omni-node]').length === 11`);
results.push('double clic');

await pause(300);
const csp = await evaluate('window.__omniCsp');
const inlineStyles = await evaluate(`document.querySelectorAll('${map} [style]').length`);
socket.close();

check(csp.length === 0, `Violations CSP : ${csp.join(' | ')}`);
check(inlineStyles === 0, `${inlineStyles} élément(s) de la carte portent un attribut style.`);
check(consoleErrors.length === 0, `Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
console.log(`Sonde carte mentale validée : ${results.join(', ')} ; aucune violation CSP, aucun attribut style, console sans erreur.`);
