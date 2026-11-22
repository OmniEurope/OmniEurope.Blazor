// PLAN-001 recette line 1: the boot of an OE application from the package's own preference cache. The showcase
// head loads omni-boot.js in its documented default form (no attribute, no key of the host); a dark theme with a
// non-default text size, control size and density is stored through the real appearance window, which writes
// OmniPreferences' cache under omni:pref: and, through the outermost theme scope, the boot copy in that same
// entry. Then the page is reloaded with a frame recorder installed at document start (CDP
// Page.addScriptToEvaluateOnNewDocument): requestAnimationFrame runs right before each frame is painted, so every
// recorded frame is a frame the visitor saw. Every frame of the boot splash and of the first page must already
// have the stored attributes and colours, without one frame in the default look, and the page must never be
// blank. Last, a page whose localStorage throws boots in the default look without any console error. The
// preferences are cleared at the end, so the parts that follow start from the shipped look.

import { probeOptions } from '../ShowcaseCdp.mjs';

const themesPage = '/components/theme';
const windowButton = '#demo-appearance-window';
const english = { Sombre: 'Dark' };
const modeButton = mode => [mode, english[mode]].map(title => `.omni-appearance-window .omni-appearance-window__mode[title="${title}"]`).join(', ');
const clearPreferences = "(() => { for (const key of Object.keys(localStorage)) if (key.startsWith('omni:pref')) localStorage.removeItem(key); })()";
const storedEntry = `(() => {
  const key = Object.keys(localStorage).find(name => name.startsWith('omni:pref:') && name.endsWith(':omni.appearance'));
  if (!key) return null;
  const entry = JSON.parse(localStorage.getItem(key));
  return { key, value: JSON.parse(entry.Value), boot: entry.Boot ? { ...entry.Boot, value: JSON.parse(entry.Boot.value), light: undefined, dark: entry.Boot.dark ? Object.keys(entry.Boot.dark).length : null } : null };
})()`;
const blockStorage = "Object.defineProperty(window, 'localStorage', { configurable: true, get() { throw new DOMException('Accès au stockage refusé.', 'SecurityError'); } });";
// The frame recorder, installed before any script of the page runs. Each frame: the root attributes, the surface
// token and background of the splash, of the root and of the outermost theme scope, as they are painted.
const recorder = `(() => {
  const frames = [];
  window.__omniBootFrames = frames;
  const read = (element, name) => element ? getComputedStyle(element).getPropertyValue(name).trim() : null;
  const tick = () => {
    const root = document.documentElement;
    const splash = document.getElementById('omni-boot-splash');
    const scope = document.querySelector('.omni-theme-scope');
    frames.push({
      at: Math.round(performance.now()),
      boot: root.hasAttribute('data-omni-theme-boot'),
      rootTheme: root.getAttribute('data-omni-theme'),
      rootResolved: root.getAttribute('data-omni-theme-resolved'),
      rootDensity: root.getAttribute('data-omni-density'),
      textSize: root.getAttribute('data-oe-text-size'),
      controlSize: root.getAttribute('data-oe-control-size'),
      rootSurface: read(root, '--omni-color-surface'),
      splash: splash !== null && splash.isConnected,
      splashSurface: read(splash, '--omni-color-surface'),
      splashBackground: splash ? getComputedStyle(splash).backgroundColor : null,
      scope: scope !== null,
      scopeResolved: scope ? scope.getAttribute('data-omni-theme-resolved') : null,
      scopeDensity: scope ? scope.getAttribute('data-omni-density') : null,
      scopeSurface: read(scope, '--omni-color-surface'),
      scopeBackground: scope ? getComputedStyle(scope).backgroundColor : null
    });
    const settled = frames.length > 30 && frames.slice(-30).every(frame => !frame.splash && frame.scope);
    if (!settled && frames.length < 2000) requestAnimationFrame(tick); else window.__omniBootDone = true;
  };
  requestAnimationFrame(tick);
})();`;

export async function bootSteps(session, results) {
  const { evaluate, waitFor, check, send, clickOn, pause } = session;
  const { siteUrl } = probeOptions('Test-ShowcaseScriptsProbe.mjs');
  const step = session.stepRecorder(results);
  const ready = "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null";
  // Reloads the application at its root with the recorder, and returns every frame painted until it settled.
  const recordedBoot = async () => {
    const { identifier } = await send('Page.addScriptToEvaluateOnNewDocument', { source: recorder });
    try {
      await send('Page.navigate', { url: siteUrl });
      await waitFor('la vitrine rechargée', ready);
      await waitFor('l\'enregistrement des images terminé', 'window.__omniBootDone === true', 30_000);
      return await evaluate('window.__omniBootFrames');
    }
    finally {
      await send('Page.removeScriptToEvaluateOnNewDocument', { identifier });
    }
  };

  // 1. The head loads the boot script in its default form: the package's own file, no attribute.
  await evaluate(clearPreferences);
  await send('Page.navigate', { url: siteUrl });
  await waitFor('la vitrine', ready);
  const tag = await evaluate("(() => { const scripts = [...document.head.querySelectorAll('script[src]')].filter(script => script.getAttribute('src').endsWith('omni-boot.js')); return scripts.map(script => ({ src: script.getAttribute('src'), attributes: script.getAttributeNames() })); })()");
  check(tag.length === 1 && tag[0].src === '_content/OmniEurope.Blazor/omni-boot.js' && tag[0].attributes.join(',') === 'src',
    `Script d'amorçage de la tête inattendu : ${JSON.stringify(tag)}.`);
  check(await evaluate('typeof window.OmniBoot?.settleTheme === "function"'), 'omni-boot.js ne s\'est pas exécuté.');

  // 2. The default look, as a first visit paints it: the reference no stored frame may show.
  const shipped = await recordedBoot();
  const shippedSurface = shipped.find(frame => frame.splash)?.splashSurface;
  check(shippedSurface && shipped.every(frame => !frame.boot), `Premier affichage par défaut inattendu : ${JSON.stringify(shipped.slice(0, 3))}.`);

  // 3. The look stored through the real appearance window: dark mode, Halo, text 7, controls 6, compact.
  await session.visit(themesPage, windowButton);
  // The keys already there (an earlier step left the snapshot of the shell example, its SnapshotKey) are not
  // this step's: only what the appearance window writes is checked against the preference cache.
  const keysBefore = await evaluate('Object.keys(localStorage)');
  await clickOn(windowButton);
  await waitFor('la fenêtre d\'apparence', `document.querySelector(${JSON.stringify(modeButton('Sombre'))}) !== null`);
  await clickOn(modeButton('Sombre'));
  const themeChosen = await evaluate(`(() => {
    const select = document.querySelector(".omni-appearance-window select[aria-label='Thème'], .omni-appearance-window select[aria-label='Theme']");
    const option = select && [...select.options].find(item => item.textContent.trim() === 'Halo');
    if (!option) return false;
    select.value = option.value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
  })()`);
  check(themeChosen, 'Le thème Halo est introuvable dans la fenêtre d\'apparence.');
  const press = label => evaluate(`document.querySelector(${JSON.stringify(label.map(text => `.omni-appearance-window button[aria-label="${text}"]`).join(', '))}).click()`);
  for (const _ of [1, 2]) {
    await press(['Augmenter la taille du texte', 'Increase the text size']);
    await pause(150);
  }
  await press(['Augmenter la taille des contrôles', 'Increase the size of the controls']);
  await pause(150);
  check(await evaluate(`(() => { const item = [...document.querySelectorAll('.omni-appearance-window .omni-select-bar__item')].find(button => ['Compacte', 'Compact'].includes(button.textContent.trim())); if (!item) return false; item.click(); return true; })()`),
    'La densité compacte est introuvable dans la fenêtre d\'apparence.');
  await waitFor('l\'apparence et sa copie d\'amorçage écrites dans le cache',
    `(() => { const stored = ${storedEntry}; return stored !== null && stored.value.appearance === 'Dark' && stored.value.preset === 'Halo' && stored.value.textSize === 7 && stored.value.controlSize === 6 && stored.value.density === 'Compact' && stored.boot !== null && stored.boot.value.density === 'Compact' && stored.boot.value.preset === 'Halo'; })()`);
  await clickOn('.omni-appearance-window .omni-appearance-window__apply');
  const stored = await evaluate(storedEntry);
  check(stored.boot.appearance === 'dark' && stored.boot.density === 'compact' && stored.boot.textSize === 7 && stored.boot.controlSize === 6 && stored.boot.dark > 0,
    `Copie d'amorçage inattendue : ${JSON.stringify(stored.boot)}.`);
  const keys = await evaluate(`Object.keys(localStorage).filter(key => !key.startsWith('omni:pref') && key !== 'omnieurope.showcase.theme' && !${JSON.stringify(keysBefore)}.includes(key))`);
  check(keys.length === 0, `Clés écrites hors du cache des préférences : ${keys.join(', ')}.`);
  step(`apparence enregistrée par la fenêtre (sombre, Halo, texte 7, contrôles 6, compacte) sous ${stored.key}, copie d'amorçage de ${stored.boot.dark} jetons dans la même entrée, aucune autre clé`);

  // 4. The reload: every painted frame, from the first, is in the stored look.
  const frames = await recordedBoot();
  const last = frames[frames.length - 1];
  const storedSurface = last.scopeSurface;
  check(storedSurface && storedSurface !== shippedSurface, `Surface du thème enregistré identique à celle par défaut : ${storedSurface}.`);
  const splashFrames = frames.filter(frame => frame.splash);
  const pageFrames = frames.filter(frame => frame.scope);
  check(splashFrames.length > 0 && pageFrames.length > 0, `Images enregistrées incomplètes : ${splashFrames.length} de l'écran de démarrage, ${pageFrames.length} de la page.`);
  const wrong = frames.filter(frame =>
    !(frame.splash || frame.scope)
    || frame.textSize !== '7' || frame.controlSize !== '6'
    || (frame.splash && (!frame.boot || frame.rootTheme !== 'dark' || frame.rootResolved !== 'dark' || frame.rootDensity !== 'compact'
      || frame.splashSurface !== storedSurface || frame.rootSurface !== storedSurface))
    || (frame.scope && (frame.scopeResolved !== 'dark' || frame.scopeDensity !== 'compact' || frame.scopeSurface !== storedSurface)));
  check(wrong.length === 0, `${wrong.length} image(s) hors de l'apparence enregistrée, la première : ${JSON.stringify(wrong[0])} (attendu surface ${storedSurface}).`);
  check(frames.every(frame => frame.splashSurface !== shippedSurface && frame.scopeSurface !== shippedSurface), 'Une image montre la surface du thème par défaut.');
  check(splashFrames.every(frame => frame.splashBackground === splashFrames[0].splashBackground), 'Le fond de l\'écran de démarrage change pendant l\'amorçage.');
  check(!last.boot, 'La copie d\'amorçage est restée sur la racine après le départ de l\'écran de démarrage.');
  step(`rechargement : ${frames.length} images relevées (${splashFrames.length} de l'écran de démarrage, ${pageFrames.length} de la page) toutes dans l'apparence enregistrée dès la première (sombre, compacte, texte 7, contrôles 6, surface ${storedSurface} au lieu de ${shippedSurface}, fond de l'écran de démarrage ${splashFrames[0].splashBackground}), aucune image vide ni par défaut, copie rendue après le départ de l'écran`);

  // 5. localStorage refused: the boot reads nothing, the page starts in the default look, no console error.
  const { identifier } = await send('Page.addScriptToEvaluateOnNewDocument', { source: blockStorage });
  try {
    await send('Page.navigate', { url: siteUrl });
    await waitFor('la vitrine sans stockage', ready);
    check(await evaluate("(() => { try { localStorage.length; return false; } catch { return true; } })()"), 'Le stockage n\'est pas refusé : le repli ne serait pas éprouvé.');
    check(await evaluate("!document.documentElement.hasAttribute('data-omni-theme-boot') && typeof window.OmniBoot?.handOverTheme === 'function'"), 'L\'amorçage a posé une copie sans stockage.');
    await pause(500);
    step('stockage refusé : amorçage sans copie, page dans l\'apparence par défaut, console sans erreur');
  }
  finally {
    await send('Page.removeScriptToEvaluateOnNewDocument', { identifier });
  }

  await send('Page.navigate', { url: siteUrl });
  await waitFor('la vitrine', ready);
  await evaluate(clearPreferences);
  await send('Page.navigate', { url: siteUrl });
  await waitFor('la vitrine', ready);
}
