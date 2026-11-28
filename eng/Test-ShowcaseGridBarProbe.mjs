// Measures, in the published showcase and in a real Chromium through CDP, the header bar of a data grid
// that carries the column chooser, in a narrow card that stretches the grid taller than its rows (a
// request of Aetheus, 2026-11-28: a dashboard tile of about 400 px grows its grid with flex: 1, and the
// bar of one button stood about 250 px high above the column titles). The bar must keep the height of
// its content, the extra height going to the rows' viewport. It fails on any console error or Content
// Security Policy violation, the showcase being served with `style-src 'self'`.
//
// Usage: node Test-ShowcaseGridBarProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// The browser (started with --remote-debugging-port) and the static server are the caller's.

import { check, openShowcaseSession, pause, probeOptions } from './ShowcaseCdp.mjs';

const { endpoint, siteUrl } = probeOptions('Test-ShowcaseGridBarProbe.mjs');
const { evaluate, waitFor, start, visit, finish } = await openShowcaseSession(endpoint, 'GridBar');
const results = [];

const grid = '.omni-data-grid:has(> .omni-data-grid__frame > .omni-data-grid__bar--header .omni-data-grid__chooser)';
await start(siteUrl);
await visit('/components/data-grid', grid);
await waitFor('les lignes de la grille', `document.querySelectorAll(${JSON.stringify(`${grid} tbody tr`)}).length > 0`);

// The boxes of the bar, its chooser button, the viewport and the frame. The narrow card is set through
// the CSS object model, which the strict style-src allows (no style attribute written in markup).
const measure = () => evaluate(`(() => {
  const root = document.querySelector(${JSON.stringify(grid)});
  const frame = root.querySelector(':scope > .omni-data-grid__frame');
  const bar = frame.querySelector(':scope > .omni-data-grid__bar--header');
  const toggle = bar.querySelector('.omni-data-grid__chooser-toggle');
  const viewport = frame.querySelector(':scope > .omni-data-grid__viewport');
  const style = getComputedStyle(bar);
  const height = element => element.getBoundingClientRect().height;
  return {
    root: height(root), frame: height(frame), bar: height(bar), toggle: height(toggle), viewport: height(viewport),
    padding: parseFloat(style.paddingBlockStart) + parseFloat(style.paddingBlockEnd),
    minimum: parseFloat(style.minBlockSize) || 0,
    frameBottom: frame.getBoundingClientRect().bottom, viewportBottom: viewport.getBoundingClientRect().bottom,
    border: parseFloat(getComputedStyle(frame).borderBlockEndWidth)
  };
})()`);

// The boxes once two readings 300 ms apart agree: the grid script settles its columns after a resize.
const settled = async () => {
  let previous = await measure();
  for (let tries = 0; tries < 20; tries++) {
    await pause(300);
    const next = await measure();
    if (JSON.stringify(next) === JSON.stringify(previous)) return next;
    previous = next;
  }
  throw new Error(`La grille ne se stabilise pas : ${JSON.stringify(previous)}.`);
};

await evaluate(`(() => { const root = document.querySelector(${JSON.stringify(grid)}); root.style.inlineSize = '400px'; root.scrollIntoView({ block: 'start' }); })()`);
const natural = await settled();

// The card grows the grid 300 px past its content, as a flex: 1 item of a taller tile does.
await evaluate(`document.querySelector(${JSON.stringify(grid)}).style.blockSize = '${Math.round(natural.root + 300)}px'`);
const stretched = await settled();

check(stretched.frame > natural.frame + 40, `La grille étirée n'a pas grandi son cadre (${natural.frame} puis ${stretched.frame} px) : la mesure ne prouverait rien.`);
const content = Math.max(stretched.toggle, stretched.minimum) + stretched.padding;
check(stretched.bar <= content + 1, `Barre d'en-tête de ${Math.round(stretched.bar)} px pour un contenu de ${Math.round(content)} px (bouton ${Math.round(stretched.toggle)} px) dans une carte de 400 px étirée.`);
check(Math.abs(stretched.bar - natural.bar) < 1, `La barre d'en-tête a changé de hauteur quand la grille s'est étirée : ${natural.bar} puis ${stretched.bar} px.`);
results.push(`barre ${Math.round(natural.bar)} px avant et ${Math.round(stretched.bar)} px après un étirement de 300 px (bouton ${Math.round(stretched.toggle)} px)`);

// All the extra height of the frame went to the viewport, down to the frame's lower edge (the grid is
// virtualized: a taller viewport draws more rows, so the table's own height is no measure here).
check(Math.abs(stretched.frameBottom - stretched.border - stretched.viewportBottom) < 1, `La zone des lignes ne descend pas au bas du cadre : ${stretched.viewportBottom} px pour ${stretched.frameBottom - stretched.border} px.`);
check(Math.abs((stretched.viewport - natural.viewport) - (stretched.frame - natural.frame)) < 1, `La zone des lignes n'a pas reçu toute la hauteur gagnée : zone ${natural.viewport} puis ${stretched.viewport} px, cadre ${natural.frame} puis ${stretched.frame} px.`);
results.push(`zone des lignes ${Math.round(natural.viewport)} -> ${Math.round(stretched.viewport)} px jusqu'au bas du cadre`);

await evaluate(`(() => { const root = document.querySelector(${JSON.stringify(grid)}); root.style.removeProperty('block-size'); root.style.removeProperty('inline-size'); })()`);

// 2. The same grid paged (virtualization switched off; the switch is the page's, not under test, so a DOM click is enough): the pager and the paging
// summary sit beside the frame in the grid's root. Stretched in the same card, every block of the root
// keeps its height and the frame alone takes the 300 px (request of Aetheus, 2026-11-28: a root that
// shared them gave the pager and the summary empty bands of their own).
await evaluate("document.querySelector('.omni-switch[aria-label=\"Virtualization\"]').click()");
await waitFor('la virtualisation coupée', `document.querySelector('.omni-switch[aria-label="Virtualization"]').getAttribute('aria-checked') === 'false'`);
await waitFor('le pager et le résumé de la grille paginée', `document.querySelector(${JSON.stringify(`${grid} > .omni-pager`)}) !== null && document.querySelector(${JSON.stringify(`${grid} > .omni-data-grid__summary`)}) !== null`);
const blocks = () => evaluate(`(() => {
  const root = document.querySelector(${JSON.stringify(grid)});
  const inFlow = [...root.children].filter(child => getComputedStyle(child).position !== 'absolute' && child.getClientRects().length > 0);
  return { root: root.getBoundingClientRect().height, blocks: inFlow.map(child => ({ name: child.classList[0], height: child.getBoundingClientRect().height })) };
})()`);
const settledBlocks = async () => {
  let previous = await blocks();
  for (let tries = 0; tries < 20; tries++) {
    await pause(300);
    const next = await blocks();
    if (JSON.stringify(next) === JSON.stringify(previous)) return next;
    previous = next;
  }
  throw new Error(`La grille paginée ne se stabilise pas : ${JSON.stringify(previous)}.`);
};
await evaluate(`(() => { const root = document.querySelector(${JSON.stringify(grid)}); root.style.inlineSize = '400px'; root.scrollIntoView({ block: 'start' }); })()`);
const pagedNatural = await settledBlocks();
await evaluate(`document.querySelector(${JSON.stringify(grid)}).style.blockSize = '${Math.round(pagedNatural.root + 300)}px'`);
const pagedStretched = await settledBlocks();
const names = pagedNatural.blocks.map(block => block.name);
check(names.includes('omni-pager') && names.includes('omni-data-grid__summary') && names.includes('omni-data-grid__frame'), `Blocs inattendus dans la grille paginée : ${names.join(', ')}.`);
check(JSON.stringify(names) === JSON.stringify(pagedStretched.blocks.map(block => block.name)), `Les blocs ont changé à l'étirement : ${names.join(', ')}.`);
for (const [index, block] of pagedNatural.blocks.entries()) {
  const after = pagedStretched.blocks[index].height;
  const expected = block.name === 'omni-data-grid__frame' ? block.height + 300 : block.height;
  check(Math.abs(after - expected) < 1, `${block.name} : ${Math.round(block.height)} px, puis ${Math.round(after)} px dans la carte étirée de 300 px (attendu ${Math.round(expected)} px).`);
}
results.push(`grille paginée : ${pagedNatural.blocks.filter(block => block.name !== 'omni-data-grid__frame').map(block => `${block.name} ${Math.round(block.height)} px`).join(', ')} inchangés, cadre +300 px`);
await evaluate(`(() => { const root = document.querySelector(${JSON.stringify(grid)}); root.style.removeProperty('block-size'); root.style.removeProperty('inline-size'); })()`);
await finish();
console.log(`Sonde barre de grille validée : ${results.join(', ')} ; aucune violation CSP, console sans erreur.`);
