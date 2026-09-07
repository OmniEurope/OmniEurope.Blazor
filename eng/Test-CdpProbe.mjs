const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const endpoint = options.get('--endpoint');
const selector = options.get('--selector');
const outputSelector = options.get('--output') ?? selector;
const expected = options.get('--expected');
const assertSelector = options.get('--assert-selector');
const assertAttribute = options.get('--assert-attribute');
const assertExpected = options.get('--assert-expected');
const assertLanguage = options.get('--assert-language');
const assertTitle = options.get('--assert-title');
if (!endpoint || !selector || !expected) {
  throw new Error('Usage: node Test-CdpProbe.mjs --endpoint <url> --selector <css> --output <css> --expected <text>');
}

const deadline = Date.now() + 20_000;
let target;
while (Date.now() < deadline) {
  try {
    const targets = await fetch(`${endpoint}/json/list`).then(response => response.json());
    target = targets.find(candidate => candidate.type === 'page' && candidate.webSocketDebuggerUrl);
    if (target) break;
  } catch {
    // The browser or WebView2 host may still be starting.
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

await send('Runtime.enable');
await send('Log.enable');

let ready = false;
while (Date.now() < deadline) {
  try {
    ready = await evaluate(`Boolean(document.querySelector(${JSON.stringify(selector)}) && document.querySelector(${JSON.stringify(outputSelector)}))`);
    if (ready) break;
  } catch (error) {
    if (!String(error.message).includes('context was destroyed')) throw error;
  }
  await new Promise(resolve => setTimeout(resolve, 100));
}
if (!ready) throw new Error(`La sonde ${selector} n'est pas apparue après hydratation.`);

let observed = '';
const resultDeadline = Date.now() + 10_000;
while (Date.now() < resultDeadline) {
  await evaluate(`document.querySelector(${JSON.stringify(selector)}).click()`);
  await new Promise(resolve => setTimeout(resolve, 100));
  observed = await evaluate(`document.querySelector(${JSON.stringify(outputSelector)})?.textContent?.trim() ?? ''`);
  if (String(observed).includes(expected)) break;
}
await new Promise(resolve => setTimeout(resolve, 250));

let asserted;
if (assertSelector || assertAttribute || assertExpected) {
  if (!assertSelector || !assertAttribute || assertExpected == null) {
    throw new Error('The optional assertion requires --assert-selector, --assert-attribute and --assert-expected.');
  }
  asserted = await evaluate(`document.querySelector(${JSON.stringify(assertSelector)})?.getAttribute(${JSON.stringify(assertAttribute)}) ?? ''`);
}
const observedLanguage = assertLanguage == null ? null : await evaluate('document.documentElement.lang');
const observedTitle = assertTitle == null ? null : await evaluate('document.title');

socket.close();
if (!String(observed).includes(expected)) {
  throw new Error(`Résultat interactif inattendu : ${JSON.stringify(observed)} (attendu : ${JSON.stringify(expected)}).`);
}
if (consoleErrors.length > 0) {
  throw new Error(`Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
}
if (assertSelector && String(asserted) !== assertExpected) {
  throw new Error(`Attribut interactif inattendu : ${assertSelector}[${assertAttribute}]=${JSON.stringify(asserted)} (attendu : ${JSON.stringify(assertExpected)}).`);
}
if (assertLanguage != null && String(observedLanguage) !== assertLanguage) {
  throw new Error(`Langue du document inattendue : ${JSON.stringify(observedLanguage)} (attendu : ${JSON.stringify(assertLanguage)}).`);
}
if (assertTitle != null && String(observedTitle) !== assertTitle) {
  throw new Error(`Titre du document inattendu : ${JSON.stringify(observedTitle)} (attendu : ${JSON.stringify(assertTitle)}).`);
}

const assertionSummary = [
  assertSelector ? `${assertSelector}[${assertAttribute}]=${JSON.stringify(asserted)}` : '',
  assertLanguage != null ? `lang=${JSON.stringify(observedLanguage)}` : '',
  assertTitle != null ? `title=${JSON.stringify(observedTitle)}` : ''
].filter(Boolean).map(value => `, ${value}`).join('');
console.log(`Sonde CDP validée : ${selector}, résultat ${JSON.stringify(observed)}${assertionSummary}, console sans erreur.`);
