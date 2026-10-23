// PLAN-010 lot 3: walks the published showcase in the long languages (German, Finnish, Greek,
// Hungarian) and fails on every text that leaves its box. It runs in a real Chromium through CDP
// because the check is about laid out text, which bUnit does not have.
//
// A text leaves its box when the rectangles of a text node reach past the padding box of the element
// that lays it out (its nearest ancestor that is not inline): the text then spills over a border or a
// neighbour, or is cut without a mark when that element hides its overflow. A text cut on purpose
// with an ellipsis (text-overflow: ellipsis) is not a failure, nor is text inside a scroll container
// that scrolls it. The exemptions are listed in EXEMPT below, each with its reason. The page itself
// must not scroll sideways: a box that keeps its text whole (a badge) can still push its row past the
// window. Only the page as rendered is measured: menus, lists and dialogs stay closed. Every
// demonstration of the gallery is visited (read from the gallery page, so a new one is covered
// without editing this file), plus the home page, the customizer and the documentation. Before the
// walk, the probe checks itself on a box it overflows on purpose, so a detector that stopped seeing
// anything fails instead of passing.
//
// Usage: node Test-ShowcaseLanguagesProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// [--shard i/n --shard-file <path>] is the parallel host's own (eng/ProbeShards.mjs): every n-th
// language x width pair from the i-th, each walked whole (self-check, every page); the raw results go to the shard file, merged by eng/Merge-ProbeShards.mjs.
// The browser (started with --remote-debugging-port) and the static server are the caller's.

import { createJsCoverage } from './JsCoverage.mjs';
import { parseShard, reportLanguages, shardSlice, writeShardResult } from './ProbeShards.mjs';

const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const endpoint = options.get('--endpoint');
const siteUrl = options.get('--url');
if (!endpoint || !siteUrl) {
  throw new Error('Usage: node Test-ShowcaseLanguagesProbe.mjs --endpoint <cdp url> --url <showcase root url>');
}

// The languages whose words run longest against French, in the order they are walked.
const ALL_LANGUAGES = (options.get('--languages') ?? 'de,fi,el,hu').split(',');
const shard = parseShard(options.get('--shard'), options.get('--shard-file'));
// The widths walked: a desktop window and a phone, where a long word has the least room.
const WIDTHS = (options.get('--widths') ?? '1280,390').split(',').map(Number);
// The walk: each language with its widths, all of them unless a shard takes only some pairs.
const ALL_PAIRS = ALL_LANGUAGES.flatMap(language => WIDTHS.map(width => `${language}@${width}`));
const PAIRS = shardSlice(ALL_PAIRS, shard);
const PLAN = ALL_LANGUAGES
  .map(language => ({ language, widths: WIDTHS.filter(width => PAIRS.includes(`${language}@${width}`)) }))
  .filter(step => step.widths.length > 0);
const LANGUAGES = PLAN.map(step => step.language);
const SHARD_WIDTHS = WIDTHS.filter(width => PLAN.some(step => step.widths.includes(width)));
const CULTURE_KEY = 'omnieurope.showcase.culture';
const THEME_KEY = 'omnieurope.showcase.theme';
// OMNI_LANGUAGES_THEME is the customizer entry to measure under, written before every page load, for example
// '{"theme":"Néon","mode":"Dark"}'. Unset, the probe measures the shipped theme, as the fresh browser of the host
// draws it; the entry is how a theme another probe left in the storage of a shared browser is reproduced.
const THEME_ENTRY = process.env.OMNI_LANGUAGES_THEME ?? null;
const THEME_MODE = THEME_ENTRY === null ? null : String(JSON.parse(THEME_ENTRY).mode ?? 'Light').toLowerCase();

// Selectors exempted from the check, each with the reason its text may leave its box. Text inside
// one of them is not measured.
const EXEMPT = [
  // Text for screen readers only is clipped to one pixel by construction.
  ['.omni-visually-hidden', 'texte réservé aux technologies d\'assistance, rogné à 1 px par construction'],
  // Source code keeps its lines: a code block scrolls, it does not wrap.
  ['pre, code', 'code source : ses lignes défilent, elles ne passent pas à la ligne'],
  // SVG text is placed by coordinates, not laid out in a box: a chart label has no padding box to
  // leave. Whether a long label fits its chart is not measured, here or anywhere else.
  ['svg', 'texte SVG placé par coordonnées, sans boîte de mise en page']
];

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
      if (await evaluate(`Boolean(${expression})`)) return;
    } catch (error) {
      if (!String(error.message).includes('context was destroyed')) throw error;
    }
    await pause(100);
  }
  throw new Error(`Attente dépassée : ${description}.`);
};

// Runs in the page: returns every text node of the scope that reaches past the padding box of the
// element laying it out, with the element, the overflow in pixels and the start of the text.
const detectorSource = `
window.__omniTextOverflow = exempt => {
  const scope = document.getElementById('showcase-theme');
  const exemptSelector = exempt.join(', ');
  const layoutBox = node => {
    let element = node.parentElement;
    while (element && getComputedStyle(element).display.startsWith('inline') && getComputedStyle(element).display !== 'inline-block' && getComputedStyle(element).display !== 'inline-flex' && getComputedStyle(element).display !== 'inline-grid') {
      element = element.parentElement;
    }
    return element;
  };
  const scrolls = element => {
    for (let current = element; current && current !== scope; current = current.parentElement) {
      const style = getComputedStyle(current);
      if (['auto', 'scroll'].includes(style.overflowX) && current.scrollWidth > current.clientWidth) return true;
    }
    return false;
  };
  const describe = element => element.tagName.toLowerCase()
    + (typeof element.className === 'string' && element.className.trim() ? '.' + element.className.trim().split(/\\s+/).join('.') : '');
  const found = [];
  const walker = document.createTreeWalker(scope, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const text = node.textContent.trim();
    if (!text) continue;
    const parent = node.parentElement;
    if (!parent || (exemptSelector && parent.closest(exemptSelector))) continue;
    const range = document.createRange();
    range.selectNodeContents(node);
    const rects = [...range.getClientRects()].filter(rect => rect.width > 0 && rect.height > 0);
    if (rects.length === 0) continue;
    const box = layoutBox(node);
    if (!box) continue;
    const style = getComputedStyle(box);
    if (style.visibility === 'hidden' || Number(style.opacity) === 0) continue;
    if (style.textOverflow === 'ellipsis') continue;
    const outer = box.getBoundingClientRect();
    const left = outer.left + box.clientLeft;
    const top = outer.top + box.clientTop;
    const right = left + box.clientWidth;
    const bottom = top + box.clientHeight;
    if (box.clientWidth === 0 || box.clientHeight === 0) continue;
    // A label taken out of view but kept for assistive technologies (a clip-path over a one pixel
    // box, as a collapsed OmniStack does to its buttons' text) is hidden on purpose, not cut.
    if (box.clientWidth <= 1 && style.clipPath !== 'none') continue;
    // Across, one pixel is already a spill. Down, the rectangle of a text is the font's whole content
    // area, taller than a tight line-height: a few pixels past the box are the glyphs' leading, not a
    // spill. A text that wraps into a box too short loses at least half a line, which is the bar.
    const across = Math.max(...rects.map(rect => Math.max(rect.right - right, left - rect.left)));
    const down = Math.max(...rects.map(rect => Math.max(rect.bottom - bottom, top - rect.top)));
    const halfLine = parseFloat(getComputedStyle(parent).fontSize) / 2;
    const beyond = across > 1 ? across : down > halfLine ? down : 0;
    if (beyond === 0) continue;
    if (scrolls(box)) continue;
    const clipped = style.overflowX !== 'visible' || style.overflowY !== 'visible';
    found.push({ element: describe(box), beyond: Math.round(beyond), clipped, text: text.slice(0, 40) });
  }
  return found;
};`;

const coverage = createJsCoverage(sendCdp, 'Languages');
const send = coverage.send;
await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await coverage.start();
await send('Page.addScriptToEvaluateOnNewDocument', {
  source: "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
});

const exemptSelectors = EXEMPT.map(([selector]) => selector);
const ready = "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null";
const csp = [];
const failures = [];
let measuredPages = 0;
let paths = [];
try {
  await send('Page.navigate', { url: siteUrl });
  await waitFor('le runtime Blazor', ready, 20_000);

  for (const { language, widths } of PLAN) {
    await evaluate(`localStorage.setItem(${JSON.stringify(CULTURE_KEY)}, ${JSON.stringify(language)})`);
    if (THEME_ENTRY !== null) await evaluate(`localStorage.setItem(${JSON.stringify(THEME_KEY)}, ${JSON.stringify(THEME_ENTRY)})`);
    await send('Emulation.setDeviceMetricsOverride', { width: widths[0], height: 900, deviceScaleFactor: 1, mobile: false });
    await send('Page.navigate', { url: siteUrl });
    await waitFor(`la vitrine en ${language}`, `${ready} && document.documentElement.lang === ${JSON.stringify(language)}`, 20_000);
    await pause(1000);
    if (THEME_MODE !== null) {
      const drawn = await evaluate("document.getElementById('showcase-theme').getAttribute('data-omni-theme-resolved')");
      if (drawn !== THEME_MODE) throw new Error(`Langues : le thème demandé (${THEME_ENTRY}) n'est pas appliqué, la page est en mode ${drawn}.`);
    }
    await evaluate(detectorSource);

    // The detector must see a text it overflows on purpose, or it proves nothing.
    const selfCheck = await evaluate(`(() => {
      const box = document.createElement('div');
      box.className = 'omni-probe-overflow';
      box.textContent = 'Donaudampfschifffahrtsgesellschaftskapitän';
      box.style.setProperty('inline-size', '40px');
      box.style.setProperty('white-space', 'nowrap');
      document.getElementById('showcase-theme').append(box);
      const seen = window.__omniTextOverflow([]).some(entry => entry.element === 'div.omni-probe-overflow');
      box.remove();
      // The sideways measure must see a row wider than the window as well.
      const row = document.createElement('div');
      row.style.setProperty('inline-size', '5000px');
      row.style.setProperty('block-size', '1px');
      document.getElementById('showcase-theme').append(row);
      const wide = document.documentElement.scrollWidth - document.documentElement.clientWidth > 1;
      row.remove();
      return seen && wide;
    })()`);
    if (!selfCheck) throw new Error(`Langues : le détecteur ne voit pas un débordement provoqué exprès (${language}), la sonde ne prouverait rien.`);

    if (paths.length === 0) {
      await evaluate("Blazor.navigateTo('/composants')");
      await waitFor('la galerie', "document.querySelectorAll('a[href*=\"composants/\"]').length > 0");
      const demoPaths = await evaluate(`[...new Set([...document.querySelectorAll('a[href*="composants/"]')].map(link => new URL(link.href).pathname))]`);
      paths = ['/', '/personnalisation', '/documentation', ...demoPaths];
    }

    for (const width of widths) {
      await send('Emulation.setDeviceMetricsOverride', { width, height: 900, deviceScaleFactor: 1, mobile: false });
      for (const path of paths) {
        await evaluate(`Blazor.navigateTo(${JSON.stringify(path)})`);
        await waitFor(`la page ${path} (${language}, ${width} px)`, `location.pathname === ${JSON.stringify(path)} && document.querySelector('main') !== null`);
        await pause(900);
        const found = await evaluate(`window.__omniTextOverflow(${JSON.stringify(exemptSelectors)})`);
        measuredPages++;
        for (const entry of found) failures.push({ language, width, path, ...entry });
        const measureSideways = () => evaluate('document.documentElement.scrollWidth - document.documentElement.clientWidth');
        let sideways = await measureSideways();
        if (sideways > 1) {
          // A notification sliding in widens the page for a moment: only a width that stays counts.
          await pause(600);
          sideways = await measureSideways();
        }
        if (sideways > 1) {
          // Names the widest element past the window, so the failure says what to fix.
          const culprit = await evaluate(`(() => {
            const edge = document.documentElement.clientWidth;
            const past = [...document.querySelectorAll('#showcase-theme *')]
              .map(element => ({ element, right: element.getBoundingClientRect().right }))
              .filter(entry => entry.right > edge + 1)
              .sort((left, right) => right.right - left.right);
            // The outermost element past the window is the one whose box overflows; its descendants follow it.
            const outermost = past.map(entry => entry.element).filter(element => !past.some(other => other.element !== element && other.element.contains(element)));
            return outermost.slice(0, 3).map(element => element.tagName.toLowerCase()
              + (typeof element.className === 'string' && element.className.trim() ? '.' + element.className.trim().split(/\\s+/).join('.') : '')
              + ' « ' + element.textContent.trim().slice(0, 30) + ' »').join(', ') || 'élément introuvable';
          })()`);
          failures.push({ language, width, path, element: 'page', beyond: sideways, clipped: false, text: `défilement horizontal de la page, par ${culprit}` });
        }
      }
    }

    csp.push(...await evaluate('window.__omniCsp'));
  }
} finally {
  // The next probe of the pass starts in French, as the host's browser does.
  try { await evaluate(`localStorage.removeItem(${JSON.stringify(CULTURE_KEY)})`); } catch { /* page gone */ }
  if (THEME_ENTRY !== null) { try { await evaluate(`localStorage.removeItem(${JSON.stringify(THEME_KEY)})`); } catch { /* page gone */ } }
  await coverage.finish();
  socket.close();
}

const result = { measuredPages, languages: LANGUAGES, widths: SHARD_WIDTHS, paths, failures, csp, consoleErrors };
if (shard) await writeShardResult(shard, { ...result, allLanguages: ALL_LANGUAGES, allWidths: WIDTHS, allPairs: ALL_PAIRS, pairs: PAIRS });
process.exitCode = reportLanguages(result, { scope: shard ? `Fragment ${shard.label} de la sonde des langues` : null });
