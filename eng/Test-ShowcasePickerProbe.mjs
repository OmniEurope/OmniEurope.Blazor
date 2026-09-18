// Drives the date, time and date and time pickers of the published showcase in a real Chromium
// through CDP, with trusted input events: the calendar grid from the keyboard (arrows, page keys,
// Home, End, Enter), the time columns, a choice by mouse, Escape and a press outside, one panel open
// at a time, the dark mode and the density. It fails on any console error or Content Security Policy
// violation, the showcase being served with `style-src 'self'`.
//
// Usage: node Test-ShowcasePickerProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// The browser (started with --remote-debugging-port) and the static server are the caller's.

const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const endpoint = options.get('--endpoint');
const siteUrl = options.get('--url');
if (!endpoint || !siteUrl) {
  throw new Error('Usage: node Test-ShowcasePickerProbe.mjs --endpoint <cdp url> --url <showcase root url>');
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

const waitFor = async (description, expression, timeout = 10_000) => {
  const until = Date.now() + timeout;
  while (Date.now() < until) {
    try {
      if (await evaluate(`Boolean(${expression})`)) return;
    } catch (error) {
      if (!String(error.message).includes('context was destroyed')) throw error;
    }
    await pause(50);
  }
  const state = await evaluate(`JSON.stringify({ active: document.activeElement?.outerHTML?.slice(0, 160), panels: document.querySelectorAll('.omni-calendar').length })`).catch(() => '');
  throw new Error(`Attente dépassée : ${description}. État : ${state}`);
};

const check = (condition, message) => {
  if (!condition) throw new Error(message);
};

const keyCodes = { ArrowLeft: 37, ArrowUp: 38, ArrowRight: 39, ArrowDown: 40, PageUp: 33, PageDown: 34, End: 35, Home: 36, Escape: 27, Enter: 13, Tab: 9 };
const key = async (name, modifiers = 0) => {
  const text = name === 'Enter' ? '\r' : undefined;
  await send('Input.dispatchKeyEvent', { type: text ? 'keyDown' : 'rawKeyDown', key: name, code: name, text, modifiers, windowsVirtualKeyCode: keyCodes[name], nativeVirtualKeyCode: keyCodes[name] });
  await send('Input.dispatchKeyEvent', { type: 'keyUp', key: name, code: name, modifiers, windowsVirtualKeyCode: keyCodes[name], nativeVirtualKeyCode: keyCodes[name] });
};
const click = async selector => {
  const point = await evaluate(`(() => {
    const element = document.querySelector(${JSON.stringify(selector)});
    if (!element) return null;
    element.scrollIntoView({ block: 'center' });
    const box = element.getBoundingClientRect();
    return { x: box.left + box.width / 2, y: box.top + box.height / 2 };
  })()`);
  check(point, `Élément introuvable pour le clic : ${selector}.`);
  await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: point.x, y: point.y });
  await send('Input.dispatchMouseEvent', { type: 'mousePressed', x: point.x, y: point.y, button: 'left', clickCount: 1 });
  await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: point.x, y: point.y, button: 'left', clickCount: 1 });
};

const active = 'document.activeElement';
const activeDate = `${active}?.getAttribute('data-date')`;
const value = id => `document.getElementById('${id}').value`;
const datePanel = '.omni-date--date .omni-calendar';
const timePanel = '.omni-date--time .omni-calendar';
const momentPanel = '.omni-date--datetime .omni-calendar';
const dateToggle = '#inputs-date ~ .omni-date__toggle';
const timeToggle = '#inputs-start ~ .omni-date__toggle';
const momentToggle = '#inputs-appointment ~ .omni-date__toggle';
const results = [];

await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
await send('Page.addScriptToEvaluateOnNewDocument', {
  source: "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
});
await send('Page.navigate', { url: siteUrl });
await waitFor('le runtime Blazor', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.querySelector('main, #app') !== null", 20_000);
await pause(1500);
await evaluate("Blazor.navigateTo('/composants/saisie-avancee')");
await waitFor('les sélecteurs de la vitrine', `document.getElementById('inputs-date') && document.getElementById('inputs-start') && document.getElementById('inputs-appointment')`, 15_000);
check(await evaluate(`${value('inputs-date')} === '02/03/2026'`), `Valeur initiale de la date inattendue : ${await evaluate(value('inputs-date'))}.`);

// ---- Date: the grid from the keyboard ----
await click(dateToggle);
await waitFor('le calendrier ouvert', `document.querySelector('${datePanel}') !== null`);
await waitFor('le focus sur le jour choisi', `${activeDate} === '2026-03-02'`);
const opened = await evaluate(`(() => {
  const panel = document.querySelector('${datePanel}');
  return {
    title: panel.querySelector('.omni-calendar__title').textContent,
    headers: [...panel.querySelectorAll('[role=columnheader]')].map(cell => cell.textContent).join(' '),
    expanded: document.querySelector('${dateToggle}').getAttribute('aria-expanded'),
    layer: getComputedStyle(panel).position + ' ' + getComputedStyle(panel).zIndex
  };
})()`);
check(opened.title === 'mars 2026', `Titre du mois inattendu : ${opened.title}.`);
check(opened.headers === 'lun mar mer jeu ven sam dim', `Jours de la semaine inattendus : ${opened.headers}.`);
check(opened.expanded === 'true', 'Le bouton ne dit pas que le calendrier est ouvert.');
const scrollBefore = await evaluate('document.scrollingElement.scrollTop');
for (const [name, expected] of [['ArrowRight', '2026-03-03'], ['ArrowDown', '2026-03-10'], ['ArrowLeft', '2026-03-09'], ['ArrowUp', '2026-03-02'], ['PageDown', '2026-04-02'], ['Home', '2026-03-30'], ['End', '2026-04-05'], ['PageUp', '2026-03-05']]) {
  await key(name);
  await waitFor(`${name} vers ${expected}`, `${activeDate} === '${expected}'`);
}
const scrollAfter = await evaluate('document.scrollingElement.scrollTop');
check(scrollAfter === scrollBefore, `Les touches du calendrier ont fait défiler la page : ${scrollBefore} puis ${scrollAfter}.`);
check(await evaluate(`document.querySelector('${datePanel} .omni-calendar__title').textContent === 'mars 2026'`), 'Le titre n\'a pas suivi la page précédente.');
await key('Enter');
await waitFor('la date choisie à Entrée', `${value('inputs-date')} === '05/03/2026'`);
await waitFor('la fermeture après le choix', `document.querySelector('${datePanel}') === null`);
await waitFor('le focus rendu au bouton', `${active} === document.querySelector('${dateToggle}')`);
check(await evaluate(`document.getElementById('inputs-picked').textContent.includes('05/03/2026')`), 'Le modèle n\'a pas reçu la date choisie.');
results.push('date au clavier (flèches, pages, Début, Fin, Entrée)');

// Escape from the keyboard: the toggle has the focus, Enter opens, Escape closes and gives it back.
await key('Enter');
await waitFor('le calendrier rouvert au clavier', `document.querySelector('${datePanel}') !== null && ${activeDate} === '2026-03-05'`);
await key('Escape');
await waitFor('la fermeture par Échap', `document.querySelector('${datePanel}') === null`);
await waitFor('le focus rendu au bouton après Échap', `${active} === document.querySelector('${dateToggle}')`);
results.push('Échap et retour du focus');

// A press outside closes it, and a day chosen with the mouse.
await click(dateToggle);
await waitFor('le calendrier rouvert', `document.querySelector('${datePanel}') !== null`);
await click('h1, h2');
await waitFor('la fermeture par un appui dehors', `document.querySelector('${datePanel}') === null`);
await click(dateToggle);
await waitFor('le calendrier rouvert', `document.querySelector('${datePanel}') !== null`);
await click(`${datePanel} [data-date='2026-03-18']`);
await waitFor('la date choisie à la souris', `${value('inputs-date')} === '18/03/2026'`);
results.push('appui dehors, choix à la souris');

// ---- Time: the columns ----
await click(timeToggle);
await waitFor('le panneau d\'heure', `document.querySelector('${timePanel}') !== null`);
await waitFor('le focus sur l\'heure choisie', `${active}?.textContent === '14' && ${active}.closest('[data-omni-part=hour]') !== null`);
const hourList = await evaluate(`(() => { const list = document.querySelector('${timePanel} [data-omni-part=hour]'); const item = list.querySelector('[aria-selected=true]'); const a = list.getBoundingClientRect(), b = item.getBoundingClientRect(); return { top: b.top - a.top, bottom: a.bottom - b.bottom, scrolled: list.scrollTop }; })()`);
check(hourList.scrolled > 0 && hourList.top > 0 && hourList.bottom > 0, `L'heure choisie n'est pas centrée dans sa colonne : ${JSON.stringify(hourList)}.`);
await key('ArrowDown');
await waitFor('l\'heure suivante', `${value('inputs-start')} === '15:30' && ${active}?.textContent === '15'`);
await key('Tab');
await waitFor('le focus dans les minutes', `${active}?.closest('[data-omni-part=minute]') !== null && ${active}.textContent === '30'`);
await key('ArrowDown');
await waitFor('la minute suivante', `${value('inputs-start')} === '15:35'`);
await key('End');
await waitFor('la dernière minute', `${value('inputs-start')} === '15:55'`);

// One panel at a time: opening the date closes the time.
await click(dateToggle);
await waitFor('un seul panneau ouvert', `document.querySelector('${datePanel}') !== null && document.querySelector('${timePanel}') === null`);
await key('Escape');
await waitFor('la fermeture du calendrier', `document.querySelectorAll('.omni-calendar').length === 0`);
await click(timeToggle);
await waitFor('le panneau d\'heure rouvert', `document.querySelector('${timePanel}') !== null`);
await click(`${timePanel} .omni-calendar__foot .omni-button--primary`);
await waitFor('la fermeture par Valider', `document.querySelector('${timePanel}') === null`);
await waitFor('le focus rendu au bouton de l\'heure', `${active} === document.querySelector('${timeToggle}')`);
check(await evaluate(`document.getElementById('inputs-picked').textContent.includes('heure : 15:55')`), 'Le modèle n\'a pas reçu l\'heure choisie.');
results.push('heure au clavier (flèches, Tab, Fin), un seul panneau, Valider');

// ---- Date and time ----
await click(momentToggle);
await waitFor('le panneau date et heure', `document.querySelector('${momentPanel} [role=grid]') !== null && document.querySelectorAll('${momentPanel} [role=listbox]').length === 2`);
const sideBySide = await evaluate(`(() => { const grid = document.querySelector('${momentPanel} .omni-picker__cal').getBoundingClientRect(); const time = document.querySelector('${momentPanel} .omni-time').getBoundingClientRect(); return time.left >= grid.right && Math.abs(time.top - grid.top) < 4; })()`);
check(sideBySide, 'Le calendrier et les colonnes ne sont pas côte à côte.');
await click(`${momentPanel} [data-date='2026-03-20']`);
await waitFor('le jour choisi, heure gardée', `${value('inputs-appointment')} === '20/03/2026 14:30'`);
await click(`${momentPanel} [data-omni-part=hour] [role=option]:nth-child(10)`);
await waitFor('l\'heure choisie, jour gardé', `${value('inputs-appointment')} === '20/03/2026 09:30'`);
await click(`${momentPanel} .omni-calendar__foot .omni-button--primary`);
await waitFor('la fermeture du panneau date et heure', `document.querySelector('${momentPanel}') === null`);
results.push('date et heure côte à côte, souris, Valider');

// ---- Density: the day cells, the time items and the field follow it ----
const measure = async density => {
  await evaluate(`document.getElementById('showcase-theme').setAttribute('data-omni-density', '${density}')`);
  await click(momentToggle);
  await waitFor(`le panneau en densité ${density}`, `document.querySelector('${momentPanel}') !== null`);
  const sizes = await evaluate(`(() => ({
    day: document.querySelector('${momentPanel} .omni-calendar__day').getBoundingClientRect().height,
    item: document.querySelector('${momentPanel} .omni-time__item').getBoundingClientRect().height,
    field: document.getElementById('inputs-appointment').getBoundingClientRect().height,
    pad: getComputedStyle(document.querySelector('${momentPanel}')).paddingTop
  }))()`);
  await key('Escape');
  await waitFor('la fermeture', `document.querySelector('${momentPanel}') === null`);
  return sizes;
};
const compact = await measure('compact');
const comfortable = await measure('comfortable');
const spacious = await measure('spacious');
await evaluate("document.getElementById('showcase-theme').setAttribute('data-omni-density', 'comfortable')");
for (const part of ['day', 'item', 'field']) {
  check(compact[part] < comfortable[part] && comfortable[part] < spacious[part], `${part} ne suit pas la densité : ${compact[part]}, ${comfortable[part]}, ${spacious[part]}.`);
}
check(compact.day === 26 && comfortable.day === 36 && spacious.day === 42, `Cases du calendrier inattendues : ${compact.day}, ${comfortable.day}, ${spacious.day}.`);
results.push(`densité (case ${compact.day}/${comfortable.day}/${spacious.day} px, heure ${compact.item}/${comfortable.item}/${spacious.item} px, marge ${compact.pad}/${comfortable.pad}/${spacious.pad})`);

// ---- Light then dark: the panel is on the floating layer and repaints with the mode ----
const paint = async () => {
  await click(dateToggle);
  await waitFor('le calendrier', `document.querySelector('${datePanel}') !== null`);
  const colours = await evaluate(`(() => {
    const panel = document.querySelector('${datePanel}');
    const chosen = panel.querySelector('[aria-selected=true]');
    const today = panel.querySelector('[aria-current=date]');
    return { panel: getComputedStyle(panel).backgroundColor, text: getComputedStyle(panel).color, chosen: chosen ? getComputedStyle(chosen).backgroundColor : null, mode: document.documentElement.getAttribute('data-omni-theme'), today: today !== null };
  })()`);
  await key('Escape');
  await waitFor('la fermeture', `document.querySelector('${datePanel}') === null`);
  return colours;
};
const light = await paint();
await click('.omni-header .omni-button');
await waitFor('le mode sombre', "document.documentElement.getAttribute('data-omni-theme') === 'dark'");
await pause(300);
const dark = await paint();
check(light.panel !== dark.panel && light.text !== dark.text, `Le panneau ne change pas avec le mode : ${JSON.stringify(light)} / ${JSON.stringify(dark)}.`);
check(light.chosen && light.chosen !== 'rgba(0, 0, 0, 0)' && dark.chosen && dark.chosen !== 'rgba(0, 0, 0, 0)', 'Le jour choisi n\'est pas rempli.');
await click('.omni-header .omni-button');
results.push(`clair ${light.panel} et sombre ${dark.panel}`);

await pause(300);
const csp = await evaluate('window.__omniCsp');
const inlineStyles = await evaluate(`document.querySelectorAll('.omni-date [style], .omni-date[style]').length`);
socket.close();

check(csp.length === 0, `Violations CSP : ${csp.join(' | ')}`);
check(inlineStyles === 0, `${inlineStyles} élément(s) des sélecteurs portent un attribut style.`);
check(consoleErrors.length === 0, `Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
console.log(`Sonde des sélecteurs validée : ${results.join(' ; ')} ; aucune violation CSP, aucun attribut style, console sans erreur.`);
