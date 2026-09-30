// Drives, in the published showcase and in a real Chromium through CDP, the script paths no other
// probe reaches: the filter popovers of the data grid (grid/filter-menus.js), the fold of the page
// header's badges and actions (omni-page-header.js) and the visual face of the HTML editor
// (omni-html-editor.js and its html-editor/ parts). It fails on any console error or Content Security
// Policy violation, the showcase being served with `style-src 'self'`.
//
// Usage: node Test-ShowcaseModulesProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// The browser (started with --remote-debugging-port) and the static server are the caller's.

const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const endpoint = options.get('--endpoint');
const siteUrl = options.get('--url');
if (!endpoint || !siteUrl) {
  throw new Error('Usage: node Test-ShowcaseModulesProbe.mjs --endpoint <cdp url> --url <showcase root url>');
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




const mouse = (type, point, extra = {}) => send('Input.dispatchMouseEvent', { type, x: point.x, y: point.y, button: 'left', clickCount: 1, ...extra });
const click = async point => {
  await mouse('mouseMoved', point, { button: 'none' });
  await mouse('mousePressed', point);
  await mouse('mouseReleased', point);
};
const key = async (name, code, virtualKey, modifiers = 0) => {
  await send('Input.dispatchKeyEvent', { type: 'rawKeyDown', key: name, code, windowsVirtualKeyCode: virtualKey, modifiers });
  await send('Input.dispatchKeyEvent', { type: 'keyUp', key: name, code, windowsVirtualKeyCode: virtualKey, modifiers });
};
const centerOf = selector => evaluate(`(() => { const box = document.querySelector(${JSON.stringify(selector)}).getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`);
const results = [];

await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
await send('Page.addScriptToEvaluateOnNewDocument', {
  source: "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
});
// Starts from the shipped look: a probe that ran before in this browser may have left its theme in
// the showcase storage.
await send('Page.navigate', { url: siteUrl });
await waitFor('la vitrine', "document.readyState === 'complete'");
await evaluate("(() => { try { localStorage.removeItem('omnieurope.showcase.theme'); } catch { } })()");
await send('Page.navigate', { url: siteUrl });
await waitFor('le runtime Blazor', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.querySelector('main, #app') !== null");

// 1. Filter popovers of the advanced grid: opened by a real click, placed by the script, one open at a
// time, closed by Escape and by a press outside.
const popover = column => `.omni-data-grid td[data-omni-col="${column}"] details[data-omni-popover]`;
const isOpen = column => evaluate(`document.querySelector(${JSON.stringify(popover(column))}).open`);
await evaluate("Blazor.navigateTo('/composants/grille-avancee')");
await waitFor('les filtres de la grille avancée', `document.querySelectorAll('.omni-data-grid details[data-omni-popover]').length >= 2`);
await evaluate(`document.querySelector(${JSON.stringify(popover('Reference'))}).scrollIntoView({ block: 'center' })`);
await pause(500);
await click(await centerOf(`${popover('Reference')} > summary`));
await waitFor('l\'ouverture du filtre Référence', `document.querySelector(${JSON.stringify(popover('Reference'))}).open`);
await pause(200);
const placed = await evaluate(`(() => {
  const details = document.querySelector(${JSON.stringify(popover('Reference'))});
  const panel = details.querySelector(':scope > .omni-data-grid__popover-panel').getBoundingClientRect();
  const trigger = details.querySelector(':scope > summary').getBoundingClientRect();
  return { x: details.style.getPropertyValue('--omni-popover-x'), y: details.style.getPropertyValue('--omni-popover-y'), inline: details.hasAttribute('style') && /(^|;)\\s*(?!--)[a-z-]+\\s*:/i.test(details.getAttribute('style')), left: panel.left, top: panel.top, right: panel.right, bottom: panel.bottom, width: panel.width, height: panel.height, triggerLeft: trigger.left, triggerBottom: trigger.bottom, triggerTop: trigger.top, viewport: { width: innerWidth, height: innerHeight } };
})()`);
check(placed.x.endsWith('px') && placed.y.endsWith('px'), `Le panneau n'a pas été placé par le script : ${JSON.stringify(placed)}.`);
check(!placed.inline, 'Le script a écrit autre chose qu\'une propriété personnalisée sur le panneau.');
check(placed.width > 0 && placed.height > 0 && placed.left >= 0 && placed.right <= placed.viewport.width && placed.top >= 0 && placed.bottom <= placed.viewport.height, `Panneau hors de la fenêtre : ${JSON.stringify(placed)}.`);
check(Math.abs(placed.left - parseFloat(placed.x)) <= 1 && Math.abs(placed.top - parseFloat(placed.y)) <= 1, `Le panneau n'est pas là où le script l'a placé : ${JSON.stringify(placed)}.`);
check(placed.top >= placed.triggerBottom - 1 || placed.bottom <= placed.triggerTop + 1, `Le panneau recouvre son déclencheur : ${JSON.stringify(placed)}.`);
results.push(`filtre Référence ouvert et placé (${placed.x}, ${placed.y})`);

await click(await centerOf(`${popover('Amount')} > summary`));
await waitFor('l\'ouverture du filtre Montant', `document.querySelector(${JSON.stringify(popover('Amount'))}).open`);
check(!(await isOpen('Reference')), 'Deux panneaux de filtre restent ouverts en même temps.');
results.push('un seul panneau ouvert');

await key('Escape', 'Escape', 27);
await waitFor('la fermeture par Échap', `document.querySelectorAll('.omni-data-grid details[data-omni-popover][open]').length === 0`);
results.push('Échap ferme');

await click(await centerOf(`${popover('Reference')} > summary`));
await waitFor('la réouverture du filtre Référence', `document.querySelector(${JSON.stringify(popover('Reference'))}).open`);
await click(await evaluate(`(() => { const box = document.querySelector('.omni-data-grid caption, .omni-data-grid__caption, h1').getBoundingClientRect(); return { x: box.left + 4, y: box.top + box.height / 2 }; })()`));
await waitFor('la fermeture par un appui dehors', `document.querySelectorAll('.omni-data-grid details[data-omni-popover][open]').length === 0`);
results.push('appui dehors ferme');

// 2. Fold of the page header: no demo header is crowded, so the probe lengthens the title the way a
// late value would, and the module, which watches the content, must fold the badges and actions
// behind "Show more" at desktop width, then unfold them once the title is short again.
const frame = '.omni-page-header__frame';
const headerState = () => evaluate(`(() => {
  const frame = document.querySelector(${JSON.stringify(frame)});
  return { compact: frame.hasAttribute('data-compact'), toggle: getComputedStyle(frame.querySelector('.omni-page-header__toggle')).display, details: getComputedStyle(frame.querySelector('.omni-page-header__details')).display, expanded: frame.querySelector('.omni-page-header__toggle').getAttribute('aria-expanded') };
})()`);
await evaluate("Blazor.navigateTo('/composants/pages')");
await waitFor('l\'en-tête de page de la vitrine', `document.querySelector(${JSON.stringify(`${frame} .omni-page-header__details`)}) !== null && document.querySelector(${JSON.stringify(`${frame} .omni-page-header__title`)}) !== null`);
await evaluate(`document.querySelector(${JSON.stringify(frame)}).scrollIntoView({ block: 'center' })`);
await pause(800);
const roomy = await headerState();
check(!roomy.compact && roomy.toggle === 'none' && roomy.details !== 'none', `En-tête déjà replié à 1280 px : le test ne prouverait rien (${JSON.stringify(roomy)}).`);
const shortTitle = await evaluate(`document.querySelector(${JSON.stringify(`${frame} .omni-page-header__title`)}).textContent`);
await evaluate(`document.querySelector(${JSON.stringify(`${frame} .omni-page-header__title`)}).firstChild.data = ${JSON.stringify('srv-paris-01 '.repeat(20).trim())}`);
await waitFor('le repli de l\'en-tête', `document.querySelector(${JSON.stringify(frame)}).hasAttribute('data-compact')`);
const folded = await headerState();
check(folded.toggle !== 'none' && folded.details === 'none', `En-tête marqué replié, mais pas replié à l'écran : ${JSON.stringify(folded)}.`);
await click(await centerOf(`${frame} .omni-page-header__toggle`));
await waitFor('l\'ouverture par « Voir plus »', `document.querySelector(${JSON.stringify(`${frame} .omni-page-header__toggle`)}).getAttribute('aria-expanded') === 'true'`);
await pause(300);
const opened = await headerState();
check(opened.compact && opened.details !== 'none', `« Voir plus » ne montre pas les badges et actions repliés : ${JSON.stringify(opened)}.`);
await click(await centerOf(`${frame} .omni-page-header__toggle`));
await waitFor('la fermeture par « Voir plus »', `document.querySelector(${JSON.stringify(`${frame} .omni-page-header__toggle`)}).getAttribute('aria-expanded') === 'false'`);
await evaluate(`document.querySelector(${JSON.stringify(`${frame} .omni-page-header__title`)}).firstChild.data = ${JSON.stringify(shortTitle)}`);
await waitFor('le dépliage de l\'en-tête', `!document.querySelector(${JSON.stringify(frame)}).hasAttribute('data-compact')`);
const unfolded = await headerState();
check(unfolded.toggle === 'none' && unfolded.details !== 'none', `En-tête resté replié après le retour du titre court : ${JSON.stringify(unfolded)}.`);
results.push('en-tête replié par un titre long, ouvert par « Voir plus », déplié au retour du titre court');

// 3. Visual face of the HTML editor: text typed in the surface and a toolbar command reach .NET, read
// back in the source editor bound to the same value.
const surface = '#editor-body';
const source = '#editor-source';
await evaluate("Blazor.navigateTo('/composants/editeur')");
await waitFor('l\'éditeur HTML de la vitrine', `document.querySelector(${JSON.stringify(surface)})?.isContentEditable === true && document.querySelector(${JSON.stringify(source)}) !== null`);
await evaluate(`document.querySelector(${JSON.stringify(surface)}).scrollIntoView({ block: 'center' })`);
await pause(800);
const marker = 'sondemodules';
check(!(await evaluate(`document.querySelector(${JSON.stringify(source)}).value.includes(${JSON.stringify(marker)})`)), 'Le repère est déjà dans la source : le test ne prouverait rien.');
await click(await centerOf(surface));
await key('End', 'End', 35, 2);
await send('Input.insertText', { text: ` ${marker}` });
await waitFor('le texte saisi dans la source', `document.querySelector(${JSON.stringify(source)}).value.includes(${JSON.stringify(marker)})`);
results.push('saisie dans la surface reçue par .NET');

// The browser writes bold as <b> or <strong>; either is the command applied to the selection.
const boldMarker = `/<(b|strong)>${marker}<\\/\\1>/`;
check(!(await evaluate(`${boldMarker}.test(document.querySelector(${JSON.stringify(source)}).value)`)), 'Le repère est déjà en gras : le test ne prouverait rien.');
for (let index = 0; index < marker.length; index++) {
  await key('ArrowLeft', 'ArrowLeft', 37, 8);
}
const boldButton = `document.querySelector(${JSON.stringify(surface)}).closest('.omni-html-editor').querySelector('[role="toolbar"] button[data-command="bold"]')`;
await click(await evaluate(`(() => { const box = ${boldButton}.getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`));
await waitFor('le gras dans la source', `${boldMarker}.test(document.querySelector(${JSON.stringify(source)}).value)`);
await waitFor('l\'état enfoncé du bouton Gras', `${boldButton}.getAttribute('aria-pressed') === 'true'`);
results.push('commande Gras appliquée à la sélection, bouton enfoncé');

await pause(300);
const csp = await evaluate('window.__omniCsp');
socket.close();

check(csp.length === 0, `Violations CSP : ${csp.join(' | ')}`);
check(consoleErrors.length === 0, `Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
console.log(`Sonde modules validée : ${results.join(', ')} ; aucune violation CSP, console sans erreur.`);
