// Drives, in the published showcase and in a real Chromium through CDP, the script paths no other
// probe reaches: the filter popovers of the data grid (grid/filter-menus.js), the fold of the page
// header's badges and actions (omni-page-header.js), the visual face of the HTML editor
// (omni-html-editor.js and its html-editor/ parts) and the frozen scale of the appearance window
// (omni-dialog.js). It fails on any console error or Content Security
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
  // Where the page stands when the wait runs out: without it a timeout says nothing of its cause.
  const state = await evaluate("JSON.stringify({ path: location.pathname, blazor: typeof Blazor, title: document.title, text: (document.querySelector('main') ?? document.body).innerText.slice(0, 160) })").catch(error => String(error.message));
  throw new Error(`Attente dépassée : ${description}. Page : ${state}. Console : ${consoleErrors.join(' | ') || 'aucune erreur'}.`);
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
// #showcase-theme is rendered by the application itself; #app is already in the static page, and a
// navigation asked before the router listens changes the address and leaves the home page shown.
await waitFor('le runtime Blazor', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null");

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

// 4. Frozen scale of the appearance window (omni-dialog.js): the window freezes its measures when it
// opens, and a row that arrives afterwards (the moving field setting, offered under Givre only) must
// move what follows it instead of being drawn over it, then leave no hole when it goes.
const appearanceWindow = '.omni-appearance-window';
const windowRows = () => evaluate(`(() => {
  const dialog = document.querySelector(${JSON.stringify(appearanceWindow)});
  const rows = [...dialog.querySelectorAll('.omni-appearance-settings__row')].map(row => row.getBoundingClientRect());
  const content = dialog.querySelector('.omni-dialog__content').getBoundingClientRect();
  // The deepest intersection of two rows, whatever their layout (stacked or side by side).
  let overlap = 0;
  for (const [index, row] of rows.entries()) {
    for (const other of rows.slice(index + 1)) {
      overlap = Math.max(overlap, Math.min(Math.min(row.right, other.right) - Math.max(row.left, other.left), Math.min(row.bottom, other.bottom) - Math.max(row.top, other.top)));
    }
  }
  const lowest = Math.max(...rows.map(row => row.bottom));
  return { rows: rows.length, overlap: Math.round(overlap), room: Math.round(content.bottom - lowest), switches: dialog.querySelectorAll('[role="switch"]').length };
})()`);
const pickTheme = name => evaluate(`(() => {
  const select = document.querySelector(${JSON.stringify(`${appearanceWindow} select`)});
  select.value = [...select.options].find(option => option.textContent.trim().startsWith(${JSON.stringify(name)})).value;
  select.dispatchEvent(new Event('change', { bubbles: true }));
})()`);
await evaluate("Blazor.navigateTo('/composants/themes')");
await waitFor('la démonstration des thèmes', "document.getElementById('demo-appearance-window') !== null");
await evaluate("document.getElementById('demo-appearance-window').scrollIntoView({ block: 'center' })");
await pause(500);
await click(await centerOf('#demo-appearance-window'));
await waitFor('le gel de la fenêtre d\'apparence', `document.querySelector(${JSON.stringify(appearanceWindow)})?.style.height.endsWith('px') === true`);
const frozen = await windowRows();
check(frozen.switches === 0 && frozen.overlap <= 0, `Fenêtre d'apparence inattendue à l'ouverture : ${JSON.stringify(frozen)}.`);
await pickTheme('Givre');
await waitFor('la ligne du fond animé', `document.querySelector(${JSON.stringify(`${appearanceWindow} [role="switch"]`)}) !== null`);
await pause(300);
const grown = await windowRows();
check(grown.rows === frozen.rows + 1, `La ligne du fond animé n'est pas arrivée : ${JSON.stringify(grown)}.`);
check(grown.overlap <= 0 && grown.room === frozen.room, `La ligne arrivée après le gel recouvre ce qui la suit : ${JSON.stringify(grown)}.`);
await pickTheme('Essentiel');
await waitFor('le départ de la ligne du fond animé', `document.querySelector(${JSON.stringify(`${appearanceWindow} [role="switch"]`)}) === null`);
await pause(300);
const shrunk = await windowRows();
check(shrunk.rows === frozen.rows && shrunk.overlap <= 0 && shrunk.room === frozen.room, `La ligne partie laisse un trou : ${JSON.stringify(shrunk)}.`);
results.push('fenêtre d\'apparence gelée : une ligne arrive et repart sans recouvrement ni trou');

// A new look retakes the measures: under Octet the labels are spaced capitals in a heavier face, and a
// width frozen for the previous theme would let them run over their button. The scale stays frozen.
const windowLabels = () => evaluate(`(() => {
  const dialog = document.querySelector(${JSON.stringify(appearanceWindow)});
  const box = dialog.getBoundingClientRect();
  const tight = [...dialog.querySelectorAll('.omni-button')].filter(button => button.textContent.trim()).map(button => {
    const edge = button.getBoundingClientRect();
    const range = document.createRange();
    range.selectNodeContents(button);
    const ink = range.getBoundingClientRect();
    return { text: button.textContent.trim(), before: Math.round(ink.left - edge.left), after: Math.round(edge.right - ink.right), overflow: button.scrollWidth - button.clientWidth };
  }).filter(label => label.overflow > 0 || label.after < label.before - 2);
  return { tight, width: Math.round(box.width), height: Math.round(box.height), spacing: getComputedStyle(dialog.querySelector('.omni-button')).letterSpacing };
})()`);
await pickTheme('Octet');
await waitFor('les libellés espacés d\'Octet', `getComputedStyle(document.querySelector(${JSON.stringify(`${appearanceWindow} .omni-button`)})).letterSpacing !== 'normal'`);
await pause(300);
const spaced = await windowLabels();
check(spaced.tight.length === 0, `Libellés à l'étroit dans leur bouton sous Octet : ${JSON.stringify(spaced.tight)}.`);
const textSize = await evaluate('document.documentElement.dataset.oeTextSize ?? ""');
await evaluate(`[...document.querySelectorAll(${JSON.stringify(`${appearanceWindow} .omni-button`)})].filter(button => !button.textContent.trim())[1].click()`);
await waitFor('le changement de taille du texte', `(document.documentElement.dataset.oeTextSize ?? "") !== ${JSON.stringify(textSize)}`);
await pause(300);
const scaled = await windowLabels();
check(scaled.width === spaced.width && scaled.height === spaced.height, `La fenêtre gelée a suivi la taille du texte : ${spaced.width} x ${spaced.height} puis ${scaled.width} x ${scaled.height}.`);
results.push(`libellés à leur place après un changement de thème (${spaced.spacing}), fenêtre inchangée quand la taille du texte change`);
await evaluate(`[...document.querySelectorAll(${JSON.stringify(`${appearanceWindow} .omni-button`)})].filter(button => !button.textContent.trim())[0].click()`);
await pickTheme('Essentiel');
await pause(300);

await pause(300);
const csp = await evaluate('window.__omniCsp');
socket.close();

check(csp.length === 0, `Violations CSP : ${csp.join(' | ')}`);
check(consoleErrors.length === 0, `Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
console.log(`Sonde modules validée : ${results.join(', ')} ; aucune violation CSP, console sans erreur.`);
