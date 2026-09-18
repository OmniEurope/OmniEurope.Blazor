// PLAN-008 T22, "Contrôle": walks the published showcase in compact then in spacious density and
// fails if an element with a size of its own keeps the same height. It runs in a real Chromium
// through CDP because the check is about applied CSS, which bUnit does not have.
//
// An element "with a size of its own" is one a rule of the package stylesheet gives a block size, a
// minimum block size or vertical padding: those are the declarations density exists to scale. What
// only sizes text (font-size, line-height) does not make an element a candidate, which is the text
// exemption of the plan. The other exemptions are listed in EXEMPT below, each with its reason.
// Every demonstration of the gallery is visited (the list is read from the gallery page, so a new
// demonstration is covered without editing this file), plus the home page and the customizer.
//
// Usage: node Test-ShowcaseDensityProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// The browser (started with --remote-debugging-port) and the static server are the caller's.

const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}

const endpoint = options.get('--endpoint');
const siteUrl = options.get('--url');
if (!endpoint || !siteUrl) {
  throw new Error('Usage: node Test-ShowcaseDensityProbe.mjs --endpoint <cdp url> --url <showcase root url>');
}

// Selectors exempted from the check, each with the reason it may keep its height. Anything that
// matches one of them, or sits inside one, is not measured.
const EXEMPT = [
  // Text: a line of text keeps the height of its font, and density sets fonts on controls, not on
  // prose. Text for screen readers only is one pixel by construction.
  ['.omni-visually-hidden', 'texte réservé aux technologies d\'assistance, 1 px par construction'],
  // Icon glyphs are text marks: the mockup keeps them at a fixed size in every density (the date
  // toggle's glyph stays 1.05rem) while the box around them (disc, button, toggle) follows it. This
  // is read as part of the plan's text exemption; the owner may overrule it.
  ['.omni-icon, .omni-date__toggle svg', 'glyphe d\'icône, marque de texte gardée fixe par la maquette ; sa boîte suit la densité'],
  // Status dots: a mark the size of a letter, not a control; the plan exempts them. The status strip
  // is a row of them (or, in its segment form, a status bar).
  ['.omni-status, .omni-status-strip', 'pastille de statut : repère de la taille d\'une lettre, pas un contrôle'],
  // Progress bars: the track is a hairline whose thickness is the drawing, the plan exempts them.
  ['.omni-progress, .omni-loading-bar, [role="progressbar"]', 'barre de progression : épaisseur fixe par dessin'],
  // Separators: a rule of one border width.
  ['hr, [role="separator"], .omni-separator', 'séparateur : un trait d\'une épaisseur de bordure'],
  // STD-BTN: these targets keep 44 px whatever the density, by rule. Beyond this list, any
  // interactive element measured at exactly 44 px in both densities is taken as a target held at the
  // STD-BTN minimum (see TARGET below).
  ['.omni-pager__button, .omni-notification__dismiss', 'cible de 44 px exigée par STD-BTN'],
  // A section with a Density of its own keeps it by contract (T21): the page density does not reach it.
  ['[data-omni-density]:not(#showcase-theme)', 'densité propre au composant (T21), qui l\'emporte sur celle de la page']
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

// Runs in the page: tags every candidate of the scope and returns its height in the density asked.
const measureSource = `
window.__omniDensityMeasure = (density, exempt) => {
  const scope = document.getElementById('showcase-theme');
  scope.setAttribute('data-omni-density', density);
  void scope.offsetHeight;
  const blockProperties = ['height', 'min-height', 'block-size', 'min-block-size', 'padding-top', 'padding-bottom', 'padding-block-start', 'padding-block-end'];
  // A declaration that sets nothing (padding: 0, height: auto) gives the element no size of its own.
  const neutral = new Set(['', '0', '0px', 'auto', 'none', 'initial', 'inherit', 'unset']);
  const sheet = [...document.styleSheets].find(candidate => (candidate.href ?? '').includes('omnieurope.blazor'));
  const rules = [];
  const walk = list => {
    for (const rule of list) {
      if (rule instanceof CSSStyleRule) {
        if (blockProperties.some(property => !neutral.has(rule.style.getPropertyValue(property).trim()))) rules.push(rule.selectorText);
        if (rule.cssRules?.length) walk(rule.cssRules);
      } else if (rule.cssRules) {
        walk(rule.cssRules);
      }
    }
  };
  walk(sheet.cssRules);
  const exemptSelector = exempt.join(', ');
  const found = new Map();
  for (const selectorText of rules) {
    for (const selector of selectorText.split(/,(?![^(]*\\))/)) {
      const trimmed = selector.trim();
      if (trimmed.includes('::') || /:(hover|active|focus|focus-visible|focus-within|checked|disabled)\\b/.test(trimmed)) continue;
      let elements;
      try { elements = scope.querySelectorAll(trimmed); } catch { continue; }
      for (const element of elements) {
        if (element.closest(exemptSelector)) continue;
        if (!found.has(element)) found.set(element, trimmed);
      }
    }
  }
  const result = {};
  let index = 0;
  for (const [element, selector] of found) {
    const box = element.getBoundingClientRect();
    const style = getComputedStyle(element);
    if (style.display === 'none' || style.visibility === 'hidden' || box.width === 0 || box.height === 0) continue;
    const key = element.getAttribute('data-omni-density-probe') ?? String(index++ + '-' + Math.random().toString(36).slice(2, 7));
    element.setAttribute('data-omni-density-probe', key);
    const interactive = element.matches('button, select, input, a[href], [role="option"], [role="tab"], [role="menuitem"], [tabindex]');
    result[key] = { height: Math.round(box.height * 100) / 100, selector, interactive, tag: element.tagName.toLowerCase() + (element.className && typeof element.className === 'string' ? '.' + element.className.trim().split(/\\s+/).join('.') : '') };
  }
  return result;
};`;

await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
await send('Page.addScriptToEvaluateOnNewDocument', {
  source: "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
});
await send('Page.navigate', { url: siteUrl });
await waitFor('le runtime Blazor', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null", 20_000);
await pause(1500);
await evaluate(measureSource);

await evaluate("Blazor.navigateTo('/composants')");
await waitFor('la galerie', "document.querySelectorAll('a[href*=\"composants/\"]').length > 0");
const demoPaths = await evaluate(`[...new Set([...document.querySelectorAll('a[href*="composants/"]')].map(link => new URL(link.href).pathname))]`);
const paths = ['/', '/personnalisation', ...demoPaths];

// STD-BTN: the minimum target, in CSS pixels.
const TARGET = 44;
const exemptSelectors = EXEMPT.map(([selector]) => selector);
const failures = [];
let measured = 0;
for (const path of paths) {
  await evaluate(`Blazor.navigateTo(${JSON.stringify(path)})`);
  await waitFor(`la page ${path}`, `location.pathname === ${JSON.stringify(path)} && document.querySelector('main') !== null`);
  await pause(900);
  const compact = await evaluate(`window.__omniDensityMeasure('compact', ${JSON.stringify(exemptSelectors)})`);
  await pause(150);
  const spacious = await evaluate(`window.__omniDensityMeasure('spacious', ${JSON.stringify(exemptSelectors)})`);
  await evaluate("document.getElementById('showcase-theme').setAttribute('data-omni-density', 'comfortable')");
  for (const [key, small] of Object.entries(compact)) {
    const large = spacious[key];
    if (!large) continue;
    measured++;
    const heldTarget = small.interactive && Math.abs(small.height - TARGET) < 0.5 && Math.abs(large.height - TARGET) < 0.5;
    if (Math.abs(large.height - small.height) < 0.5 && !heldTarget) {
      failures.push({ path, selector: small.selector, element: small.tag, height: small.height });
    }
  }
}

const csp = await evaluate('window.__omniCsp');
socket.close();

if (failures.length > 0) {
  const grouped = new Map();
  for (const failure of failures) {
    const groupKey = `${failure.selector} (${failure.height} px)`;
    const group = grouped.get(groupKey) ?? { count: 0, paths: new Set(), element: failure.element };
    group.count++;
    group.paths.add(failure.path);
    grouped.set(groupKey, group);
  }
  const lines = [...grouped].map(([selector, group]) => `  ${selector} x${group.count} sur ${[...group.paths].join(', ')} (${group.element})`);
  console.error(`Densité : ${failures.length} élément(s) à dimension propre gardent la même hauteur en compacte et en aérée (${measured} mesurés, ${paths.length} pages) :\n${lines.join('\n')}`);
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
  console.log(`Sonde de densité validée : ${measured} éléments à dimension propre mesurés sur ${paths.length} pages, tous changent de hauteur entre compacte et aérée ; aucune violation CSP, console sans erreur.`);
}
