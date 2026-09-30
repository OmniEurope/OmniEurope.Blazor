// PLAN-010 lot 3: walks the published showcase in the long languages (German, Finnish, Greek,
// Hungarian) and fails on every text that leaves its box. It runs in a real Chromium through CDP
// because the check is about laid out text, which bUnit does not have.
//
// A text leaves its box when the rectangles of a text node reach past the padding box of the element
// that lays it out (its nearest ancestor that is not inline): the text then spills over a border or a
// neighbour, or is cut without a mark when that element hides its overflow. A text cut on purpose
// with an ellipsis (text-overflow: ellipsis) is not a failure, nor is text inside a scroll container
// that scrolls it. The exemptions are listed in EXEMPT below, each with its reason. Every
// demonstration of the gallery is visited (read from the gallery page, so a new one is covered
// without editing this file), plus the home page, the customizer and the documentation. Before the
// walk, the probe checks itself on a box it overflows on purpose, so a detector that stopped seeing
// anything fails instead of passing.
//
// Usage: node Test-ShowcaseLanguagesProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// The browser (started with --remote-debugging-port) and the static server are the caller's.

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
const LANGUAGES = (options.get('--languages') ?? 'de,fi,el,hu').split(',');
// The widths walked: a desktop window and a phone, where a long word has the least room.
const WIDTHS = (options.get('--widths') ?? '1280,390').split(',').map(Number);
const CULTURE_KEY = 'omnieurope.showcase.culture';

// Selectors exempted from the check, each with the reason its text may leave its box. Text inside
// one of them is not measured.
const EXEMPT = [
  // Text for screen readers only is clipped to one pixel by construction.
  ['.omni-visually-hidden', 'texte réservé aux technologies d\'assistance, rogné à 1 px par construction'],
  // Source code keeps its lines: a code block scrolls, it does not wrap.
  ['pre, code', 'code source : ses lignes défilent, elles ne passent pas à la ligne'],
  // SVG text is placed by coordinates, not laid out in a box: a chart label has no padding box to
  // leave. Its fit is the chart layout's business (OmniChartContext), tested in bUnit.
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

await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
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

  for (const language of LANGUAGES) {
    await evaluate(`localStorage.setItem(${JSON.stringify(CULTURE_KEY)}, ${JSON.stringify(language)})`);
    await send('Emulation.setDeviceMetricsOverride', { width: WIDTHS[0], height: 900, deviceScaleFactor: 1, mobile: false });
    await send('Page.navigate', { url: siteUrl });
    await waitFor(`la vitrine en ${language}`, `${ready} && document.documentElement.lang === ${JSON.stringify(language)}`, 20_000);
    await pause(1000);
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
      return seen;
    })()`);
    if (!selfCheck) throw new Error(`Langues : le détecteur ne voit pas un débordement provoqué exprès (${language}), la sonde ne prouverait rien.`);

    if (paths.length === 0) {
      await evaluate("Blazor.navigateTo('/composants')");
      await waitFor('la galerie', "document.querySelectorAll('a[href*=\"composants/\"]').length > 0");
      const demoPaths = await evaluate(`[...new Set([...document.querySelectorAll('a[href*="composants/"]')].map(link => new URL(link.href).pathname))]`);
      paths = ['/', '/personnalisation', '/documentation', ...demoPaths];
    }

    for (const width of WIDTHS) {
      await send('Emulation.setDeviceMetricsOverride', { width, height: 900, deviceScaleFactor: 1, mobile: false });
      for (const path of paths) {
        await evaluate(`Blazor.navigateTo(${JSON.stringify(path)})`);
        await waitFor(`la page ${path} (${language}, ${width} px)`, `location.pathname === ${JSON.stringify(path)} && document.querySelector('main') !== null`);
        await pause(900);
        const found = await evaluate(`window.__omniTextOverflow(${JSON.stringify(exemptSelectors)})`);
        measuredPages++;
        for (const entry of found) failures.push({ language, width, path, ...entry });
      }
    }

    csp.push(...await evaluate('window.__omniCsp'));
  }
} finally {
  // The next probe of the pass starts in French, as the host's browser does.
  try { await evaluate(`localStorage.removeItem(${JSON.stringify(CULTURE_KEY)})`); } catch { /* page gone */ }
  socket.close();
}

if (measuredPages === 0) {
  console.error('Langues : aucune page mesurée ; la sonde ne prouve rien.');
  process.exitCode = 1;
}
if (failures.length > 0) {
  const grouped = new Map();
  for (const failure of failures) {
    const key = `${failure.element} ${failure.clipped ? 'rogné' : 'déborde'}`;
    const group = grouped.get(key) ?? { count: 0, languages: new Set(), widths: new Set(), paths: new Set(), beyond: 0, sample: failure.text };
    group.count++;
    group.languages.add(failure.language);
    group.widths.add(failure.width);
    group.paths.add(failure.path);
    group.beyond = Math.max(group.beyond, failure.beyond);
    grouped.set(key, group);
  }
  const lines = [...grouped]
    .sort((left, right) => right[1].count - left[1].count)
    .map(([key, group]) => `  ${key} x${group.count}, jusqu'à ${group.beyond} px, ${[...group.languages].join('/')} à ${[...group.widths].join('/')} px sur ${[...group.paths].slice(0, 4).join(', ')}${group.paths.size > 4 ? ` (+${group.paths.size - 4})` : ''} : « ${group.sample} »`);
  console.error(`Langues : ${failures.length} texte(s) sortent de leur boîte (${grouped.size} forme(s), ${measuredPages} pages mesurées en ${LANGUAGES.join(', ')} à ${WIDTHS.join(' et ')} px) :\n${lines.join('\n')}`);
  process.exitCode = 1;
}
if (csp.length > 0) {
  console.error(`Violations CSP : ${csp.join(' | ')}`);
  process.exitCode = 1;
}
if (consoleErrors.length > 0) {
  console.error(`Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
  process.exitCode = 1;
}
if (!process.exitCode) {
  console.log(`Sonde des langues validée : ${measuredPages} pages mesurées en ${LANGUAGES.join(', ')} à ${WIDTHS.join(' et ')} px, aucun texte ne sort de sa boîte ; aucune violation CSP, console sans erreur.`);
}
