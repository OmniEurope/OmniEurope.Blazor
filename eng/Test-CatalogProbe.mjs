const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const endpoint = options.get('--endpoint');
const url = options.get('--url');
if (!endpoint || !url) {
  throw new Error('Usage: node Test-CatalogProbe.mjs --endpoint <url> --url <catalog-url>');
}

const deadline = Date.now() + 30_000;
let target;
while (Date.now() < deadline) {
  try {
    const targets = await fetch(`${endpoint}/json/list`).then(response => response.json());
    target = targets.find(candidate => candidate.type === 'page' && candidate.webSocketDebuggerUrl);
    if (target) break;
  } catch {
    // Chromium may still be starting.
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
    consoleErrors.push(message.params.exceptionDetails.text);
  } else if (message.method === 'Log.entryAdded' && message.params.entry.level === 'error') {
    const source = message.params.entry.url ? ` (${message.params.entry.url})` : '';
    consoleErrors.push(`${message.params.entry.text}${source}`);
  } else if (message.method === 'Runtime.consoleAPICalled' && message.params.type === 'error') {
    consoleErrors.push(message.params.args.map(argument => argument.value ?? argument.description ?? '').join(' '));
  }
});

function send(method, params = {}) {
  const id = ++commandId;
  socket.send(JSON.stringify({ id, method, params }));
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
}

async function evaluate(expression) {
  const response = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
  if (response.exceptionDetails) throw new Error(response.exceptionDetails.text);
  return response.result.value;
}

async function waitFor(expression, message, timeout = 15_000) {
  const limit = Date.now() + timeout;
  while (Date.now() < limit) {
    try {
      if (await evaluate(expression)) return;
    } catch (error) {
      if (!String(error.message).includes('context was destroyed')) throw error;
    }
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  throw new Error(message);
}

await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await send('Page.addScriptToEvaluateOnNewDocument', {
  source: `
    globalThis.__omniCspViolations = [];
    addEventListener('securitypolicyviolation', event => {
      globalThis.__omniCspViolations.push({
        directive: event.effectiveDirective,
        blockedUri: event.blockedURI,
        sourceFile: event.sourceFile,
        lineNumber: event.lineNumber
      });
    });
  `
});
await send('Page.navigate', { url });
await waitFor(
  `Boolean(document.querySelector('#catalog-open-dialog') && document.querySelector('#catalog-notify') && document.querySelectorAll('.catalog-section').length === 5)`,
  'Le catalogue hydraté et ses cinq familles ne sont pas apparus.'
);

await evaluate(`Array.from(document.querySelectorAll('.catalog-section')).forEach(section => section.scrollIntoView({ block: 'center' }))`);

const dialogDeadline = Date.now() + 10_000;
while (Date.now() < dialogDeadline && !(await evaluate(`Boolean(document.querySelector('[role="dialog"]'))`))) {
  await evaluate(`document.querySelector('#catalog-open-dialog').click()`);
  await new Promise(resolve => setTimeout(resolve, 100));
}
await waitFor(
  `Boolean(document.querySelector('[role="dialog"][aria-modal="true"]') && document.querySelector('[role="dialog"]').contains(document.activeElement))`,
  'Le dialogue ne s\'est pas ouvert avec un focus contenu.'
);
await evaluate(`document.querySelector('.omni-dialog__close').click()`);
await waitFor(`!document.querySelector('[role="dialog"]')`, 'Le dialogue ne s\'est pas fermé.');

const notificationDeadline = Date.now() + 10_000;
while (Date.now() < notificationDeadline && !(await evaluate(`Boolean(document.querySelector('.omni-notification'))`))) {
  await evaluate(`document.querySelector('#catalog-notify').click()`);
  await new Promise(resolve => setTimeout(resolve, 100));
}
await waitFor(
  `Boolean(document.querySelector('.omni-notification[role="status"], .omni-notification[role="alert"]'))`,
  'La notification accessible ne s\'est pas affichée.'
);
await evaluate(`document.querySelector('.omni-notification__dismiss').click()`);
await new Promise(resolve => setTimeout(resolve, 500));

await evaluate(`(() => {
  const editor = document.querySelector('#catalog-editor');
  editor.value = 'alpha beta';
  editor.dispatchEvent(new Event('input', { bubbles: true }));
  editor.focus();
  editor.setSelectionRange(6, 10);
  editor.closest('.omni-html-editor').querySelector('[role="toolbar"] button').click();
})()`);
await waitFor(
  `(() => { const editor = document.querySelector('#catalog-editor'); return editor.value === 'alpha <strong>beta</strong>' && editor.selectionStart === 14 && editor.selectionEnd === 18; })()`,
  'La commande gras de l\'éditeur n\'a pas enveloppé et restauré la sélection réelle.'
);
await evaluate(`(() => {
  const editor = document.querySelector('#catalog-editor');
  editor.setSelectionRange(0, 0);
  editor.closest('.omni-html-editor').querySelectorAll('[role="toolbar"] button')[1].click();
})()`);
await waitFor(
  `(() => { const editor = document.querySelector('#catalog-editor'); return editor.value.startsWith('<em></em>') && editor.selectionStart === 4 && editor.selectionEnd === 4; })()`,
  'La commande sans sélection de l\'éditeur n\'a pas conservé un curseur déterministe.'
);

const cspViolations = await evaluate(`globalThis.__omniCspViolations ?? []`);
socket.close();

if (consoleErrors.length > 0) {
  throw new Error(`Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
}
if (cspViolations.length > 0) {
  throw new Error(`Violations CSP navigateur : ${JSON.stringify(cspViolations)}`);
}

console.log('Catalogue CDP validé : cinq familles, dialogue, focus, notification, sélection éditeur, console et CSP client.');
