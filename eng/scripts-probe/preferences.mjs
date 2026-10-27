// PLAN-019 lots 1 and 2: the user preferences in a real browser, through the appearance the package
// keeps (OmniAppearanceState) and the live preview of the Thèmes page, which binds nothing. A change made
// in the appearance window lands in localStorage under omni:pref: and survives a reload; the same change
// made in a second tab repaints the first one through the storage event, without any action there; with
// localStorage refused (a page whose storage throws, as blocked site data does) the page keeps working
// from memory, without any console error. Then the database path, with the showcase's demonstration
// store (?store=demo, rows in sessionStorage outside the localStorage cache): a change in the window is
// written to the store; a newer row written on the store side, as another device would, replaces the
// stale cache entry and repaints the page without a reload once the tab comes back into view, or at the
// periodic synchronization; after a reload the newer store row wins over the cache. The preferences and
// the demonstration rows are cleared at the end, so the parts that follow start from the shipped look
// without a store.

import { probeOptions } from '../ShowcaseCdp.mjs';

const themesPage = '/components/theme';
const preview = '#theme-following';
const windowButton = '#demo-appearance-window';
const modeButton = mode => `.omni-appearance-window .omni-appearance-window__mode[title="${mode}"]`;
const storedAppearance = `(() => {
  const key = Object.keys(localStorage).find(name => name.startsWith('omni:pref:') && name.endsWith(':omni.appearance'));
  if (!key) return null;
  const entry = JSON.parse(localStorage.getItem(key));
  return { key, entry, value: JSON.parse(entry.Value) };
})()`;
// The preferences of every scope, and the rows of the demonstration store.
const clearPreferences = "(() => { for (const key of Object.keys(localStorage)) if (key.startsWith('omni:pref')) localStorage.removeItem(key); for (const key of Object.keys(sessionStorage)) if (key.startsWith('omni-showcase-demo-store:')) sessionStorage.removeItem(key); })()";
// The demonstration store of the showcase (site/OmniEurope.Blazor.Showcase/Preferences/DemoPreferenceStore.cs,
// opted in by ?store=demo): its row of the appearance in sessionStorage, the cache entry of its scope in
// localStorage, and the mark of the last synchronization, which only a synchronization writes.
const demoRowKey = 'omni-showcase-demo-store:omni.appearance';
const demoCacheKey = 'omni:pref:demo:omni.appearance';
const demoState = `(() => {
  const parse = text => { if (!text) return null; const entry = JSON.parse(text); return { ...entry, Value: entry.Value === null ? null : JSON.parse(entry.Value) }; };
  return { row: parse(sessionStorage.getItem('${demoRowKey}')), cache: parse(localStorage.getItem('${demoCacheKey}')), syncMark: localStorage.getItem('omni:pref-sync:demo') };
})()`;
// Writes the store row as another device would: the appearance changed and dated after the cached one.
const writeDemoRow = (value, updatedAt) =>
  `sessionStorage.setItem('${demoRowKey}', ${JSON.stringify(JSON.stringify({ Key: 'omni.appearance', Value: JSON.stringify(value), UpdatedAt: updatedAt }))})`;
// Run before any script of the page: every read of window.localStorage throws, as with blocked site data.
const blockStorage = "Object.defineProperty(window, 'localStorage', { configurable: true, get() { throw new DOMException('Accès au stockage refusé.', 'SecurityError'); } });";

export async function preferenceSteps(session, results) {
  const { evaluate, waitFor, check, send, clickOn, pause } = session;
  const { endpoint, siteUrl } = probeOptions('Test-ShowcaseScriptsProbe.mjs');
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const cspClean = async when => {
    const csp = await evaluate('window.__omniCsp');
    check(csp.length === 0, `Violations CSP ${when} : ${csp.join(' | ')}`);
  };
  const previewMode = () => evaluate(`document.querySelector(${JSON.stringify(preview)})?.getAttribute('data-omni-theme') ?? null`);
  // Reloads the application at its root (the static host has no fallback for deep links), then opens the page.
  const reload = async (query = '') => {
    await cspClean('avant le rechargement');
    await send('Page.navigate', { url: siteUrl + query });
    await waitFor('le runtime Blazor rechargé', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null");
    await session.visit(themesPage, preview);
  };
  const openWindow = async () => {
    await clickOn(windowButton);
    await waitFor('la fenêtre d\'apparence', `document.querySelector(${JSON.stringify(modeButton('Sombre'))}) !== null`);
  };

  // 1. A change in the window that nobody binds repaints the preview and is kept in localStorage. An
  // earlier part may have used the same window: the preferences are cleared first.
  await evaluate(clearPreferences);
  await reload();
  check(await evaluate(storedAppearance) === null, 'Une préférence d\'apparence existe encore : la sonde ne prouverait pas son écriture.');
  check(await previewMode() === 'system', `Aperçu inattendu au départ : ${await previewMode()}.`);
  await openWindow();
  await clickOn(modeButton('Sombre'));
  await waitFor('l\'aperçu repeint en sombre', `document.querySelector(${JSON.stringify(preview)}).getAttribute('data-omni-theme') === 'dark'`);
  await evaluate("document.querySelector('.omni-appearance-window button[aria-label=\"Augmenter la taille du texte\"]').click()");
  await waitFor('la taille du texte posée sur la racine', "document.documentElement.dataset.oeTextSize === '6'");
  await waitFor('la préférence écrite', `${storedAppearance}?.value.textSize === 6`);
  const written = await evaluate(storedAppearance);
  check(written.key === 'omni:pref:local:omni.appearance' && written.value.appearance === 'Dark' && written.value.version === 1 && written.entry.Dirty === false,
    `Préférence écrite inattendue : ${JSON.stringify(written)}.`);
  await clickOn('.omni-appearance-window .omni-appearance-window__apply');
  step(`fenêtre d'apparence sans liaison : aperçu repeint et préférence écrite sous ${written.key}`);

  // 2. The look survives a reload: read back from localStorage before the preview draws.
  await reload();
  check(await previewMode() === 'dark', `Apparence perdue au rechargement : ${await previewMode()}.`);
  await waitFor('la taille du texte reposée après rechargement', "document.documentElement.dataset.oeTextSize === '6'");
  step('apparence retrouvée après rechargement (sombre, texte 6)');

  // 3. A second tab changes the look: the first one repaints through the storage event alone.
  const { targetId } = await send('Target.createTarget', { url: 'about:blank' });
  const second = await connectTab(endpoint, targetId);
  try {
    await second.navigate(siteUrl);
    await second.waitFor("typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null");
    await second.evaluate(`Blazor.navigateTo(${JSON.stringify(themesPage)})`);
    await second.waitFor(`document.querySelector(${JSON.stringify(windowButton)}) !== null && document.querySelector(${JSON.stringify(preview)})?.getAttribute('data-omni-theme') === 'dark'`);
    await second.evaluate(`document.querySelector(${JSON.stringify(windowButton)}).click()`);
    await second.waitFor(`document.querySelector(${JSON.stringify(modeButton('Clair'))}) !== null`);
    await second.evaluate(`document.querySelector(${JSON.stringify(modeButton('Clair'))}).click()`);
    await second.waitFor(`document.querySelector(${JSON.stringify(preview)}).getAttribute('data-omni-theme') === 'light'`);
    const secondCsp = await second.evaluate('JSON.stringify(window.__omniSecondCsp ?? [])');
    check(second.errors.length === 0, `Console du second onglet en erreur : ${second.errors.join(' | ')}`);
    check(secondCsp === '[]', `Violations CSP dans le second onglet : ${secondCsp}`);
  }
  finally {
    second.close();
    await send('Target.closeTarget', { targetId });
  }
  await waitFor('le premier onglet repeint par l\'autre', `document.querySelector(${JSON.stringify(preview)}).getAttribute('data-omni-theme') === 'light'`);
  step('changement fait dans un second onglet appliqué au premier par l\'événement storage');

  // 4. localStorage refused: the preferences live in memory for the page, without any error.
  await evaluate(clearPreferences);
  const { identifier } = await send('Page.addScriptToEvaluateOnNewDocument', { source: blockStorage });
  try {
    await reload();
    check(await evaluate("(() => { try { localStorage.length; return false; } catch { return true; } })()"), 'Le stockage n\'est pas refusé : le repli ne serait pas éprouvé.');
    check(await previewMode() === 'system', `Aperçu inattendu sans stockage : ${await previewMode()}.`);
    await openWindow();
    await clickOn(modeButton('Sombre'));
    await waitFor('l\'aperçu repeint sans stockage', `document.querySelector(${JSON.stringify(preview)}).getAttribute('data-omni-theme') === 'dark'`);
    await clickOn('.omni-appearance-window .omni-appearance-window__apply');
    // Within the page the look still holds: away and back, read from the memory copy.
    await session.visit('/components/badges', '.omni-badge');
    await session.visit(themesPage, preview);
    check(await previewMode() === 'dark', `Apparence perdue dans la page sans stockage : ${await previewMode()}.`);
    await pause(300);
    step('stockage refusé : apparence gardée en mémoire, console sans erreur');
  }
  finally {
    await send('Page.removeScriptToEvaluateOnNewDocument', { identifier });
  }

  // 5. The database path, with the demonstration store of the showcase (?store=demo): its rows live in
  // sessionStorage, outside the localStorage cache, so the probe can change "the database" behind the
  // page's back, as another device would. Its periodic synchronization is off unless &sync= asks for it.
  await reload();
  await evaluate(clearPreferences);
  await reload('?store=demo');
  check(await evaluate('window.omniShowcaseDemoStore.active') === 0, 'La page n\'a pas demandé la base de démonstration.');
  const empty = await evaluate(demoState);
  check(empty.row === null && empty.cache === null && empty.syncMark === null, `Base de démonstration non vide au départ : ${JSON.stringify(empty)}.`);
  check(await previewMode() === 'system', `Aperçu inattendu avec la base de démonstration : ${await previewMode()}.`);

  // 5a. A change in the unbound window is written to the store, and the cache entry of the store's scope
  // is confirmed with the same date.
  await openWindow();
  await clickOn(modeButton('Sombre'));
  await waitFor('l\'aperçu repeint en sombre avec la base', `document.querySelector(${JSON.stringify(preview)}).getAttribute('data-omni-theme') === 'dark'`);
  await waitFor('la préférence écrite dans la base de démonstration et confirmée dans le cache',
    `(state => state.row?.Value?.appearance === 'Dark' && state.cache?.Dirty === false && Date.parse(state.cache.UpdatedAt) === Date.parse(state.row.UpdatedAt))(${demoState})`);
  await clickOn('.omni-appearance-window .omni-appearance-window__apply');
  const stored = await evaluate(demoState);
  check(stored.cache.Value.appearance === 'Dark' && stored.row.Key === 'omni.appearance' && await evaluate("localStorage.getItem('omni:pref:local:omni.appearance') === null"),
    `Écriture inattendue avec la base : ${JSON.stringify(stored)}.`);
  step(`base de démonstration : changement de la fenêtre écrit dans la base (${demoRowKey}) et confirmé dans le cache (${demoCacheKey})`);

  // 5b. Another device writes a newer row: nothing changes until a synchronization, then the stale cache
  // entry is replaced and the page repaints without a reload. The tab coming back into view is the trigger
  // (the package listens to visibilitychange; a headless tab never leaves the view, so the event is dispatched).
  await evaluate('window.__omniDemoNoReload = true');
  const newer = new Date(Date.parse(stored.cache.UpdatedAt) + 60_000).toISOString();
  await evaluate(writeDemoRow({ ...stored.cache.Value, appearance: 'Light' }, newer));
  await pause(1500);
  check(await previewMode() === 'dark' && (await evaluate(demoState)).cache.Value.appearance === 'Dark',
    'Le cache a suivi la base sans synchronisation : l\'étape ne prouverait pas la synchronisation.');
  check(await evaluate("document.visibilityState === 'visible'"), 'Onglet non visible : le retour au premier plan ne serait pas éprouvé.');
  await evaluate("document.dispatchEvent(new Event('visibilitychange'))");
  await waitFor('l\'aperçu repeint par la base, sans rechargement', `document.querySelector(${JSON.stringify(preview)}).getAttribute('data-omni-theme') === 'light'`);
  const synced = await evaluate(demoState);
  check(synced.cache.Value.appearance === 'Light' && Date.parse(synced.cache.UpdatedAt) === Date.parse(newer) && synced.cache.Dirty === false
    && Date.parse(synced.syncMark) === Date.parse(newer) && await evaluate('window.__omniDemoNoReload === true'),
    `Synchronisation inattendue : ${JSON.stringify(synced)}.`);
  step('base de démonstration : ligne plus récente écrite côté base, cache périmé remplacé et page repeinte au retour sur l\'onglet, sans rechargement');

  // 5c. After a reload, a newer store row wins over the cache the page reads first.
  const later = new Date(Date.parse(newer) + 60_000).toISOString();
  await evaluate(writeDemoRow({ ...synced.cache.Value, appearance: 'Dark' }, later));
  check((await evaluate(demoState)).cache.Value.appearance === 'Light', 'Le cache a changé avant le rechargement.');
  await reload('?store=demo');
  await waitFor('l\'aperçu repeint par la base après rechargement', `document.querySelector(${JSON.stringify(preview)}).getAttribute('data-omni-theme') === 'dark'`);
  await waitFor('le cache remplacé par la ligne de la base', `(state => state.cache?.Value?.appearance === 'Dark' && Date.parse(state.cache.UpdatedAt) === ${Date.parse(later)})(${demoState})`);
  step('base de démonstration : après rechargement, la ligne plus récente de la base l\'emporte sur le cache');

  // 5d. The periodic synchronization (&sync=1): a newer row reaches the page with no trigger at all.
  await reload('?store=demo&sync=1');
  check(await evaluate('window.omniShowcaseDemoStore.active') === 1, 'La synchronisation périodique de démonstration n\'est pas demandée.');
  check(await previewMode() === 'dark', `Aperçu inattendu avant la synchronisation périodique : ${await previewMode()}.`);
  const latest = new Date(Date.parse(later) + 60_000).toISOString();
  const current = await evaluate(demoState);
  await evaluate(writeDemoRow({ ...current.cache.Value, appearance: 'Light' }, latest));
  await waitFor('l\'aperçu repeint par la synchronisation périodique', `document.querySelector(${JSON.stringify(preview)}).getAttribute('data-omni-theme') === 'light'`);
  check(Date.parse((await evaluate(demoState)).syncMark) === Date.parse(latest), 'Marque de synchronisation non avancée par la synchronisation périodique.');
  step('base de démonstration : synchronisation périodique, ligne plus récente appliquée sans action');

  // Away from the store before clearing, so no synchronization writes the cache back meanwhile.
  await reload();
  await evaluate(clearPreferences);

  // The shipped look again for whatever follows: nothing kept, the root attributes gone.
  await reload();
  check(await evaluate(storedAppearance) === null && await previewMode() === 'system', 'Préférences d\'apparence restées après la sonde.');
  await session.visit('/components/badges', '.omni-badge');
}

// A second tab driven through its own CDP connection: evaluation, waits, and its console errors.
async function connectTab(endpoint, targetId) {
  let target;
  for (let tries = 0; tries < 100 && !target; tries++) {
    const targets = await fetch(`${endpoint}/json/list`).then(response => response.json());
    target = targets.find(candidate => candidate.id === targetId && candidate.webSocketDebuggerUrl);
    if (!target) await new Promise(resolve => setTimeout(resolve, 100));
  }
  if (!target) throw new Error('Second onglet introuvable par CDP.');

  const socket = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener('open', resolve, { once: true });
    socket.addEventListener('error', () => reject(new Error('Connexion CDP au second onglet impossible.')), { once: true });
  });
  let id = 0;
  const pending = new Map();
  const errors = [];
  socket.addEventListener('message', event => {
    const message = JSON.parse(event.data);
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) reject(new Error(message.error.message));
      else resolve(message.result);
    } else if (message.method === 'Runtime.exceptionThrown') {
      errors.push(message.params.exceptionDetails.exception?.description ?? message.params.exceptionDetails.text);
    } else if (message.method === 'Log.entryAdded' && message.params.entry.level === 'error') {
      errors.push(message.params.entry.text);
    } else if (message.method === 'Runtime.consoleAPICalled' && message.params.type === 'error') {
      errors.push(message.params.args.map(argument => argument.value ?? argument.description ?? '').join(' '));
    }
  });
  const send = (method, params = {}) => {
    const commandId = ++id;
    socket.send(JSON.stringify({ id: commandId, method, params }));
    return new Promise((resolve, reject) => pending.set(commandId, { resolve, reject }));
  };
  const evaluate = async expression => {
    const response = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
    if (response.exceptionDetails) throw new Error(response.exceptionDetails.exception?.description ?? response.exceptionDetails.text);
    return response.result.value;
  };
  const waitFor = async (expression, timeout = 30_000) => {
    const until = Date.now() + timeout;
    while (Date.now() < until) {
      try {
        if (await evaluate(expression)) return;
      } catch (error) {
        if (!String(error.message).includes('context was destroyed') && !String(error.message).includes('Cannot find context')) throw error;
      }
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    throw new Error(`Second onglet : attente dépassée (${expression}). Console : ${errors.join(' | ') || 'aucune erreur'}.`);
  };

  await send('Runtime.enable');
  await send('Log.enable');
  await send('Page.enable');
  await send('Page.addScriptToEvaluateOnNewDocument', {
    source: "window.__omniSecondCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniSecondCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
  });
  const navigate = url => send('Page.navigate', { url });
  return { evaluate, waitFor, navigate, errors, close: () => socket.close() };
}
