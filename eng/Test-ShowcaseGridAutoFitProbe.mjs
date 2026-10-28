// Drives the fit to content of a data grid column in the published showcase, in a real Chromium
// through CDP with trusted input events: a double click on the column edge, the long value of a
// virtualized row far off screen, the column that opts out, a drag then Enter on the handle. It fails
// on any console error or Content Security Policy violation, the showcase being served with
// `style-src 'self'`.
//
// Usage: node Test-ShowcaseGridAutoFitProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// The browser (started with --remote-debugging-port) and the static server are the caller's.


import { createJsCoverage } from './JsCoverage.mjs';

const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const endpoint = options.get('--endpoint');
const siteUrl = options.get('--url');
if (!endpoint || !siteUrl) {
  throw new Error('Usage: node Test-ShowcaseGridAutoFitProbe.mjs --endpoint <cdp url> --url <showcase root url>');
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

const sendCdp = (method, params = {}) => {
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


const grid = '#demo-grid-capped-long';
const longText = 'Établissement public de coopération intercommunale du Grand Est';
const mouse = (type, point, extra = {}) => send('Input.dispatchMouseEvent', { type, x: point.x, y: point.y, button: 'left', clickCount: 1, ...extra });
const headerWidth = key => evaluate(`document.querySelector('${grid} th[data-omni-col="${key}"]').getBoundingClientRect().width`);
const handleCenter = key => evaluate(`(() => { const box = document.querySelector('${grid} th[data-omni-col="${key}"] .omni-data-grid__resize-handle').getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`);
const doubleClick = async point => {
  await mouse('mouseMoved', point, { button: 'none' });
  await mouse('mousePressed', point, { clickCount: 1 });
  await mouse('mouseReleased', point, { clickCount: 1 });
  await mouse('mousePressed', point, { clickCount: 2 });
  await mouse('mouseReleased', point, { clickCount: 2 });
};
// Width the long value needs in a cell of this grid: the text laid out with the cell's own font and
// padding, measured on a canvas, independently of omni-grid.js and without touching the page's styles.
const neededWidth = () => evaluate(`(() => {
  const cell = document.querySelector('${grid} tbody td[data-omni-col="Applicant"]');
  const style = getComputedStyle(cell);
  const context = document.createElement('canvas').getContext('2d');
  context.font = [style.fontStyle, style.fontWeight, style.fontSize, style.fontFamily].join(" ");
  return context.measureText(${JSON.stringify(longText)}).width + parseFloat(style.paddingInlineStart) + parseFloat(style.paddingInlineEnd);
})()`);
const hasLongText = `[...document.querySelectorAll('${grid} tbody td')].some(cell => cell.textContent.trim() === ${JSON.stringify(longText)})`;

const coverage = createJsCoverage(sendCdp, 'AutoFit');
const send = coverage.send;
await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await coverage.start();
await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
await send('Page.addScriptToEvaluateOnNewDocument', {
  source: "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
});
// Starts from the shipped look: the probe that ran before in this browser (the contrast probe) leaves
// its last theme in the showcase storage, and the widths measured below depend on the theme's font.
await send('Page.navigate', { url: siteUrl });
await waitFor('la vitrine', "document.readyState === 'complete'");
await evaluate("(() => { try { localStorage.removeItem('omnieurope.showcase.theme'); } catch { } })()");
await send('Page.navigate', { url: siteUrl });
// #showcase-theme is rendered by the application itself; #app is already in the static page, and a
// navigation asked before the router listens changes the address and leaves the home page shown.
await waitFor('le runtime Blazor', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null");
await pause(1500);
await evaluate("Blazor.navigateTo('/components/data-grid-variants')");
await waitFor('la grille virtualisée de la vitrine', `document.querySelectorAll('${grid} tbody tr[data-omni-row-index]').length > 3`);
await evaluate(`document.querySelector('${grid}').scrollIntoView({ block: 'center' })`);
await pause(300);
const results = [];

// Where the gesture is offered: Demandeur follows the grid, Référence opts out; both keep the drag.
const handles = await evaluate(`(() => {
  const read = key => { const handle = document.querySelector('${grid} th[data-omni-col="' + key + '"] .omni-data-grid__resize-handle'); return { autofit: handle.hasAttribute('data-omni-autofit'), drag: handle.hasAttribute('data-omni-drag') }; };
  return { reference: read('Reference'), applicant: read('Applicant') };
})()`);
check(handles.applicant.autofit && handles.applicant.drag, `Poignée Demandeur inattendue : ${JSON.stringify(handles.applicant)}.`);
check(!handles.reference.autofit && handles.reference.drag, `Poignée Référence inattendue : ${JSON.stringify(handles.reference)}.`);
results.push('options grille et colonne');

// The long value lives in row 7777, never rendered at this point.
check(!(await evaluate(hasLongText)), 'La valeur longue est déjà dans la page : le test ne prouverait rien.');
const needed = await neededWidth();
const before = await headerWidth('Applicant');
check(before < needed, `La colonne est déjà assez large (${before} px pour ${needed} px) : le test ne prouverait rien.`);

const referenceBefore = await headerWidth('Reference');
await doubleClick(await handleCenter('Reference'));
await pause(500);
const referenceAfter = await headerWidth('Reference');
check(Math.abs(referenceAfter - referenceBefore) < 1, `Référence (AutoFit="false") a changé au double clic : ${referenceBefore} puis ${referenceAfter} px.`);
results.push('colonne retirée inchangée');

await doubleClick(await handleCenter('Applicant'));
await waitFor('l\'ajustement de Demandeur', `document.querySelector('${grid} th[data-omni-col="Applicant"]').getBoundingClientRect().width >= ${needed}`);
await pause(500);
const fitted = await headerWidth('Applicant');
check(fitted >= needed && fitted <= needed + 8, `Largeur ajustée ${fitted} px pour un besoin de ${needed} px.`);
results.push(`double clic ${Math.round(before)} -> ${Math.round(fitted)} px (besoin ${Math.round(needed)} px)`);

// Scroll the long row into the window: its cell is not clipped.
await evaluate(`(() => { const viewport = document.querySelector('${grid} .omni-data-grid__viewport'); viewport.scrollTop = viewport.scrollHeight * 7776 / 10000; })()`);
await waitFor('la ligne D-07777', hasLongText);
const clip = await evaluate(`(() => { const cell = [...document.querySelectorAll('${grid} tbody td')].find(candidate => candidate.textContent.trim() === ${JSON.stringify(longText)}); return { scroll: cell.scrollWidth, client: cell.clientWidth }; })()`);
check(clip.scroll <= clip.client, `La valeur longue reste coupée : ${clip.scroll} px pour ${clip.client} px.`);
results.push('ligne 7777 entière');

// The drag still works on its own, then Enter on the focused handle fits the column again.
const start = await handleCenter('Applicant');
await mouse('mouseMoved', start, { button: 'none' });
await mouse('mousePressed', start);
for (let step = 1; step <= 10; step++) {
  await mouse('mouseMoved', { x: start.x - (step * 20), y: start.y }, { buttons: 1 });
  await pause(16);
}
await mouse('mouseReleased', { x: start.x - 200, y: start.y });
await pause(500);
const dragged = await headerWidth('Applicant');
check(dragged < fitted - 150, `Le glisser n'a pas réduit la colonne : ${fitted} puis ${dragged} px.`);
await evaluate(`document.querySelector('${grid} th[data-omni-col="Applicant"] .omni-data-grid__resize-handle').focus()`);
await send('Input.dispatchKeyEvent', { type: 'rawKeyDown', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
await send('Input.dispatchKeyEvent', { type: 'keyUp', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
await waitFor('l\'ajustement au clavier', `document.querySelector('${grid} th[data-omni-col="Applicant"]').getBoundingClientRect().width >= ${needed}`);
results.push(`glisser ${Math.round(dragged)} px puis Entrée ${Math.round(await headerWidth('Applicant'))} px`);

// A second fit of a column already fitted keeps its width: the measurement reads the content, never
// the width the column has now.
const refitBefore = await headerWidth('Applicant');
await doubleClick(await handleCenter('Applicant'));
await pause(800);
const refitAfter = await headerWidth('Applicant');
check(Math.abs(refitAfter - refitBefore) < 1, `Un nouvel ajustement a changé la largeur : ${refitBefore} puis ${refitAfter} px.`);
results.push(`nouvel ajustement stable à ${Math.round(refitAfter)} px`);

// The default, with no gesture and no width from the host: #demo-grid-fit sits in a 22rem frame that
// gives each of its two columns less than the longest package name. Name sizes itself to its
// longest value, header included, and shows it whole; Description (Truncate) keeps its share, ends in
// an ellipsis and shows its whole value in the tooltip; a width declared by the host or chosen by
// dragging still wins over the content.
const fit = '#demo-grid-fit';
await evaluate(`document.querySelector('${fit}').scrollIntoView({ block: 'center' })`);
await waitFor('la grille à la largeur du contenu', `document.querySelectorAll('${fit} tbody td[data-omni-col="Name"]').length === 3`);
await pause(600);
const fitCells = key => evaluate(`[...document.querySelectorAll('${fit} tbody td[data-omni-col="${key}"]')].map(cell => ({ text: cell.textContent.trim(), scroll: cell.scrollWidth, client: cell.clientWidth, width: cell.getBoundingClientRect().width }))`);
const fitWidth = key => evaluate(`document.querySelector('${fit} th[data-omni-col="${key}"]').getBoundingClientRect().width`);
const neededFor = async text => evaluate(`(() => {
  const cell = document.querySelector('${fit} tbody td[data-omni-col="Name"]');
  const style = getComputedStyle(cell);
  const context = document.createElement('canvas').getContext('2d');
  context.font = [style.fontStyle, style.fontWeight, style.fontSize, style.fontFamily].join(" ");
  return context.measureText(${JSON.stringify(text)}).width + parseFloat(style.paddingInlineStart) + parseFloat(style.paddingInlineEnd);
})()`);
const names = await fitCells('Name');
const longestName = names.map(cell => cell.text).sort((left, right) => right.length - left.length)[0];
const neededName = await neededFor(longestName);
const frameShare = await evaluate(`document.querySelector('${fit} table').getBoundingClientRect().width / 2`);
check(frameShare + 10 < neededName, `Le cadre donne ${frameShare} px à chaque colonne pour un besoin de ${neededName} px : le test ne prouverait rien.`);
check(names.every(cell => cell.scroll <= cell.client + 1 && cell.width >= neededName - 1), `Une valeur de Paquet est coupée : ${JSON.stringify(names)} pour un besoin de ${neededName} px.`);
const descriptions = await fitCells('Description');
check(descriptions.every(cell => cell.scroll > cell.client + 20), `La colonne Truncate n'est pas coupée : ${JSON.stringify(descriptions)}.`);
results.push(`largeur au contenu : « ${longestName} » entier (${Math.round(names[0].width)} px pour un besoin de ${Math.round(neededName)} px, ${Math.round(frameShare)} px dans la part), Truncate coupée (${Math.round(descriptions[0].client)} px pour ${descriptions[0].scroll} px)`);

// The cut cell shows its whole value in the package tooltip.
const descriptionPoint = await evaluate(`(() => { const box = document.querySelector('${fit} tbody td[data-omni-col="Description"]').getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`);
await mouse('mouseMoved', { x: descriptionPoint.x - 40, y: descriptionPoint.y }, { button: 'none' });
await mouse('mouseMoved', descriptionPoint, { button: 'none' });
await waitFor('l\'infobulle de la cellule coupée', `document.querySelector('.omni-title-tooltip')?.textContent.trim() === ${JSON.stringify(descriptions[0].text)}`);
results.push('infobulle de la cellule coupée');

// The frame shrinks below what the columns need: the table keeps their widths and scrolls sideways
// instead of squeezing the Truncate column to nothing.
const squeeze = await evaluate(`(async () => {
  const box = document.querySelector('${fit}').closest('#demo-grid-fit-box');
  const viewport = document.querySelector('${fit} .omni-data-grid__viewport');
  box.style.maxInlineSize = '8rem';
  await new Promise(resolve => setTimeout(resolve, 500));
  const state = {
    name: document.querySelector('${fit} th[data-omni-col="Name"]').getBoundingClientRect().width,
    description: document.querySelector('${fit} th[data-omni-col="Description"]').getBoundingClientRect().width,
    client: viewport.clientWidth,
    scroll: viewport.scrollWidth
  };
  box.style.maxInlineSize = '';
  await new Promise(resolve => setTimeout(resolve, 500));
  return state;
})()`);
check(squeeze.name >= neededName - 1 && squeeze.description >= 120 && squeeze.scroll > squeeze.client, `Cadre trop étroit : ${JSON.stringify(squeeze)}.`);
results.push(`cadre trop étroit : défilement latéral (${Math.round(squeeze.scroll)} px pour ${Math.round(squeeze.client)} px), Truncate gardée à ${Math.round(squeeze.description)} px`);

// A grid that is hidden measures nothing: a change of its content while it is out of the layout (a
// hidden tab) must not shrink its columns to their padding, and they are intact once it is shown.
const nameFitted = await fitWidth('Name');
const hidden = await evaluate(`(async () => {
  const box = document.querySelector('${fit}').closest('#demo-grid-fit-box');
  const cell = document.querySelector('${fit} tbody td[data-omni-col="Name"]');
  box.style.display = 'none';
  const marker = document.createTextNode('');
  cell.append(marker);
  await new Promise(resolve => setTimeout(resolve, 400));
  // Read while the grid is still hidden: the shown grid would grow back by itself when it is laid out again.
  const whileHidden = document.querySelector('${fit} col[data-omni-col="Name"]').style.getPropertyValue('--omni-col-width');
  marker.remove();
  box.style.display = '';
  await new Promise(resolve => setTimeout(resolve, 600));
  return { whileHidden, shown: document.querySelector('${fit} th[data-omni-col="Name"]').getBoundingClientRect().width };
})()`);
const hiddenWidth = hidden.shown;
check(parseFloat(hidden.whileHidden) === nameFitted && Math.abs(hiddenWidth - nameFitted) < 1, `Une grille masquée a perdu sa largeur : ${nameFitted} px, puis ${hidden.whileHidden} masquée et ${hiddenWidth} px affichée.`);
results.push(`grille masquée pendant un changement de contenu : largeur gardée (${Math.round(hiddenWidth)} px)`);

// A width chosen by dragging wins over the content, and the fit does not give it back.
const nameHandle = await evaluate(`(() => { const box = document.querySelector('${fit} th[data-omni-col="Name"] .omni-data-grid__resize-handle').getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`);
await mouse('mouseMoved', nameHandle, { button: 'none' });
await mouse('mousePressed', nameHandle);
for (let step = 1; step <= 6; step++) {
  await mouse('mouseMoved', { x: nameHandle.x - (step * 10), y: nameHandle.y }, { buttons: 1 });
  await pause(16);
}
await mouse('mouseReleased', { x: nameHandle.x - 60, y: nameHandle.y });
await pause(700);
const nameDragged = await fitWidth('Name');
const draggedCells = await fitCells('Name');
check(nameDragged < nameFitted - 40, `Le glisser n'a pas réduit Paquet : ${nameFitted} puis ${nameDragged} px.`);
check(draggedCells.filter(cell => cell.text === longestName).every(cell => cell.scroll > cell.client + 20), `Le contenu a repris la largeur choisie : ${JSON.stringify(draggedCells)}.`);
results.push(`largeur choisie au glisser gardée (${Math.round(nameFitted)} -> ${Math.round(nameDragged)} px, valeurs coupées)`);

// A width declared by the host wins as well: Demandeur of the narrow grid is 11rem and stays cut.
await evaluate(`document.querySelector('#demo-grid-narrow').scrollIntoView({ block: 'center' })`);
await pause(600);
const declared = await evaluate(`(() => { const cell = document.querySelector('#demo-grid-narrow tbody td[data-omni-col="Applicant"]'); return { scroll: cell.scrollWidth, client: cell.clientWidth, width: cell.getBoundingClientRect().width, rem: parseFloat(getComputedStyle(document.documentElement).fontSize) }; })()`);
check(Math.abs(declared.width - 11 * declared.rem) < 2 && declared.scroll > declared.client, `La largeur déclarée n'est pas tenue : ${JSON.stringify(declared)}.`);
results.push(`largeur déclarée tenue (${Math.round(declared.width)} px, valeur coupée)`);

await pause(300);
const csp = await evaluate('window.__omniCsp');
const probes = await evaluate(`document.querySelectorAll('${grid} table:not(.omni-data-grid__table), ${grid} div[aria-hidden="true"], ${fit} table:not(.omni-data-grid__table), ${fit} div[aria-hidden="true"]').length`);
await coverage.finish();
socket.close();

check(csp.length === 0, `Violations CSP : ${csp.join(' | ')}`);
check(probes === 0, `${probes} sonde(s) de mesure restée(s) dans la grille.`);
check(consoleErrors.length === 0, `Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
console.log(`Sonde ajustement de colonne validée : ${results.join(', ')} ; aucune violation CSP, aucune sonde restée, console sans erreur.`);
