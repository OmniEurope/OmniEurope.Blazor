// The boot splash, menus, dialogs, popovers, tooltips, disclosures, tabs (their wheel scope included),
// scroll strips, the shell's theme snapshot and self-hiding scrollbar, the page header's history back,
// the appearance window and the form helpers of omniInterop.js, each driven on its showcase page as a
// reader would, with the state the page shows after it asserted.

export async function overlaySteps(session, results) {
  const { evaluate, waitFor, key, pause, check, send, clickOn, hover, mouse } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const focusedText = () => evaluate('document.activeElement?.textContent.trim() ?? ""');

  // 0. The boot splash the served page carries is faded out and removed once the application rendered.
  const servedPage = await evaluate("fetch('/', { cache: 'no-store' }).then(response => response.text())");
  check(servedPage.includes('id="omni-boot-splash"'), 'La page servie n\'a pas d\'écran de démarrage : son retrait ne prouverait rien.');
  await waitFor('l\'écran de démarrage retiré', "document.getElementById('omni-boot-splash') === null");
  step('écran de démarrage de la page retiré après le premier rendu');

  // 1. Menus: the overflow menu by the keyboard, the context menu closed by a press outside.
  await session.visit('/composants/superpositions', '#demo-overflow-menu');
  await clickOn('#demo-overflow-menu');
  await waitFor('le menu ouvert', `[...document.querySelectorAll('.omni-overflow-menu:has(> #demo-overflow-menu) [role="menu"]')].some(menu => menu.getClientRects().length > 0)`);
  await waitFor('le focus dans le menu', "document.activeElement?.getAttribute('role') === 'menuitem'");
  // The window narrows while the menu is open: the menu is placed again, still on screen.
  await send('Emulation.setDeviceMetricsOverride', { width: 1000, height: 800, deviceScaleFactor: 1, mobile: false });
  await pause(200);
  const menuBox = await evaluate(`(() => { const menu = [...document.querySelectorAll('.omni-overflow-menu:has(> #demo-overflow-menu) [role="menu"]')].find(candidate => candidate.getClientRects().length > 0); const box = menu.getBoundingClientRect(); return { left: box.left, right: box.right, width: innerWidth }; })()`);
  check(menuBox.left >= 0 && menuBox.right <= menuBox.width, `Menu hors de la fenêtre après redimensionnement : ${JSON.stringify(menuBox)}.`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(200);
  const first = await focusedText();
  await key('ArrowDown', 'ArrowDown', 40);
  await waitFor('le focus sur l\'entrée suivante', `document.activeElement?.getAttribute('role') === 'menuitem' && document.activeElement.textContent.trim() !== ${JSON.stringify(first)}`);
  await key('Escape', 'Escape', 27);
  await waitFor('le menu fermé, le focus rendu', "document.activeElement?.classList.contains('omni-overflow-menu__trigger')");
  await clickOn('#demo-overflow-menu');
  await waitFor('le menu rouvert', "document.activeElement?.getAttribute('role') === 'menuitem'");
  await key('Tab', 'Tab', 9);
  await waitFor('Tab qui ferme le menu', `[...document.querySelectorAll('.omni-overflow-menu:has(> #demo-overflow-menu) [role="menu"]')].every(menu => menu.getClientRects().length === 0)`);
  step('menu au clavier : flèche, Échap, Tab');

  await clickOn('#demo-context-menu', { button: 'right' });
  await waitFor('le menu contextuel', `[...document.querySelectorAll('[role="menu"]')].some(menu => menu.getClientRects().length > 0)`);
  await clickOn('main h1, h1');
  await waitFor('le menu contextuel fermé par un appui dehors', `[...document.querySelectorAll('[role="menu"]')].every(menu => menu.getClientRects().length === 0)`);
  step('menu contextuel fermé par un appui dehors');

  // 2. Modal dialogs: Tab kept inside, a press on the veil of a sticky dialog that keeps it open, a
  // focus sentinel that sends the focus back in, and a free width written through the CSSOM.
  await clickOn('#demo-dialog-sticky');
  await waitFor('la boîte collante ouverte', "document.querySelector('.omni-overlay section.omni-dialog') !== null");
  await pause(200);
  for (let index = 0; index < 4; index++) await key('Tab', 'Tab', 9);
  check(await evaluate("document.querySelector('.omni-overlay section.omni-dialog').contains(document.activeElement)"), 'Tab est sorti de la boîte modale.');
  const veil = await evaluate(`(() => { const dialog = document.querySelector('.omni-overlay section.omni-dialog').getBoundingClientRect(); return { x: Math.max(4, Math.round(dialog.left / 2)), y: Math.round(dialog.top + dialog.height / 2) }; })()`);
  await mouse('mouseMoved', veil, { button: 'none' });
  await mouse('mousePressed', veil);
  await mouse('mouseReleased', veil);
  await pause(300);
  check(await evaluate("document.querySelector('.omni-overlay section.omni-dialog') !== null"), 'Un appui sur le voile a fermé la boîte collante.');
  await evaluate("document.querySelector('.omni-overlay [data-focus-sentinel]').focus()");
  await waitFor('la sentinelle qui renvoie le focus', "document.activeElement && !document.activeElement.hasAttribute('data-focus-sentinel') && document.querySelector('.omni-overlay section.omni-dialog').contains(document.activeElement)");
  await key('Escape', 'Escape', 27);
  await pause(300);
  if (await evaluate("document.querySelector('.omni-overlay section.omni-dialog') !== null")) await clickOn('.omni-overlay .omni-dialog__close');
  await waitFor('la boîte collante fermée', "document.querySelector('.omni-overlay section.omni-dialog') === null");
  step('boîte modale : Tab gardé, voile sans effet, sentinelle, fermeture');

  await clickOn('#demo-dialog-size-free');
  await waitFor('la largeur libre posée', "document.querySelector('section.omni-dialog[data-omni-dialog-width-ready]')?.style.getPropertyValue('--omni-dialog-width') === '30rem'");
  check(await evaluate("!/(^|;)\\s*(?!--)[a-z-]+\\s*:/i.test(document.querySelector('section.omni-dialog').getAttribute('style') ?? '')"), 'La largeur libre a écrit une propriété ordinaire.');
  // The free width removed while the dialog is open: the custom property and its ready mark leave, the
  // dialog stays open and takes the medium size, wider than 30rem.
  const freeWidth = await evaluate("document.querySelector('section.omni-dialog').getBoundingClientRect().width");
  await clickOn('#demo-dialog-size-back');
  await waitFor('la largeur libre retirée, le dialogue ouvert', "(() => { const dialog = document.querySelector('section.omni-dialog'); return dialog !== null && document.getElementById('demo-dialog-size-back') === null && dialog.style.getPropertyValue('--omni-dialog-width') === '' && !dialog.hasAttribute('data-omni-dialog-width-ready'); })()");
  await waitFor('la taille moyenne reprise', `document.querySelector('section.omni-dialog').getBoundingClientRect().width > ${freeWidth} + 100`);
  await clickOn('.omni-overlay .omni-dialog__close');
  await waitFor('la boîte à largeur libre fermée', "document.querySelector('section.omni-dialog') === null");
  step('largeur libre de la boîte, retirée pendant l\'ouverture');

  // 3. Popovers: placed inside the window when opened, released when closed.
  await clickOn('.omni-popover > button[aria-haspopup="dialog"]');
  await waitFor('la bulle ouverte', "document.querySelector('.omni-popover > button[aria-haspopup=\"dialog\"]').getAttribute('aria-expanded') === 'true'");
  const placed = await evaluate(`(() => { const panel = [...document.querySelectorAll('.omni-popover [role="dialog"], .omni-popover__panel')].find(element => element.getClientRects().length > 0); const box = panel.getBoundingClientRect(); return { left: box.left, right: box.right, top: box.top, bottom: box.bottom, width: innerWidth, height: innerHeight }; })()`);
  check(placed.left >= 0 && placed.right <= placed.width && placed.top >= 0 && placed.bottom <= placed.height, `Bulle hors de la fenêtre : ${JSON.stringify(placed)}.`);
  await waitFor('le focus dans la bulle', "document.activeElement?.closest('.omni-popover__panel') !== null");
  await key('Escape', 'Escape', 27);
  await waitFor('la bulle fermée', "document.querySelector('.omni-popover > button[aria-haspopup=\"dialog\"]').getAttribute('aria-expanded') === 'false'");
  await clickOn('.omni-popover > button[aria-haspopup="dialog"]');
  await waitFor('la bulle rouverte', "document.querySelector('.omni-popover > button[aria-haspopup=\"dialog\"]').getAttribute('aria-expanded') === 'true'");
  await send('Emulation.setDeviceMetricsOverride', { width: 1100, height: 850, deviceScaleFactor: 1, mobile: false });
  await pause(200);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await clickOn('main h1, h1');
  await waitFor('la bulle fermée par un appui dehors', "document.querySelector('.omni-popover > button[aria-haspopup=\"dialog\"]').getAttribute('aria-expanded') === 'false'");
  step('bulle placée dans la fenêtre, fermée par Échap puis par un appui dehors');

  // 4. Tooltips: placed under the pointer, kept inside the window when « Show more » grows them.
  await session.visit('/composants/retours', '.omni-tooltip--expandable');
  await hover('.omni-tooltip .omni-tooltip__trigger');
  await waitFor('la bulle d\'aide placée sous le pointeur', "document.querySelector('.omni-tooltip.omni-tooltip--tracked')?.style.getPropertyValue('--omni-tooltip-x').endsWith('px') === true");
  await hover('.omni-tooltip--expandable .omni-tooltip__trigger');
  await pause(300);
  await clickOn('.omni-tooltip--expandable .omni-tooltip__more');
  await waitFor('l\'aide dépliée', "document.querySelector('.omni-tooltip--expandable .omni-tooltip__more')?.getAttribute('aria-expanded') === 'true'");
  const tip = await evaluate(`(() => { const box = document.querySelector('.omni-tooltip--expandable .omni-tooltip__content').getBoundingClientRect(); return { left: box.left, right: box.right, top: box.top, bottom: box.bottom, width: innerWidth, height: innerHeight }; })()`);
  check(tip.left >= 0 && tip.right <= tip.width + 1 && tip.top >= 0 && tip.bottom <= tip.height + 1, `Aide dépliée hors de la fenêtre : ${JSON.stringify(tip)}.`);
  await mouse('mouseMoved', { x: 5, y: 5 }, { button: 'none' });
  await hover('button[title], [data-omni-title]');
  await waitFor('l\'aide de l\'attribut title', "document.querySelector('.omni-title-tooltip.omni-title-tooltip--visible')?.textContent.trim().length > 0");
  await mouse('mouseMoved', { x: 5, y: 5 }, { button: 'none' });
  step('bulle d\'aide placée, dépliée dans la fenêtre ; aide d\'un attribut title');

  // 5. A compact multiple choice: a click inside keeps it open, Escape and a press outside close it.
  await session.visit('/composants/selection', '#demo-tags-compact');
  await clickOn('#demo-tags-compact > summary');
  await waitFor('le choix compact ouvert', "document.getElementById('demo-tags-compact').open");
  await clickOn('#demo-tags-compact input[type="checkbox"], #demo-tags-compact [role="option"], #demo-tags-compact label');
  await pause(200);
  check(await evaluate("document.getElementById('demo-tags-compact').open"), 'Un clic dans le panneau l\'a fermé.');
  await key('Escape', 'Escape', 27);
  await waitFor('Échap qui ferme le choix compact', "!document.getElementById('demo-tags-compact').open");
  await clickOn('#demo-tags-compact > summary');
  await waitFor('le choix compact rouvert', "document.getElementById('demo-tags-compact').open");
  await clickOn('main h1, h1');
  await waitFor('l\'appui dehors qui ferme le choix compact', "!document.getElementById('demo-tags-compact').open");
  step('choix compact : clic dedans, Échap, appui dehors');

  // 6. Tabs by the keyboard.
  await session.visit('/composants/navigation', '[role="tab"]');
  await evaluate("document.querySelector('[role=\"tab\"][aria-selected=\"true\"]').focus()");
  const tabBefore = await focusedText();
  await key('ArrowRight', 'ArrowRight', 39);
  await waitFor('l\'onglet suivant', `document.activeElement?.getAttribute('role') === 'tab' && document.activeElement.textContent.trim() !== ${JSON.stringify(tabBefore)}`);
  await key('Home', 'Home', 36);
  await waitFor('le premier onglet', `document.activeElement === document.activeElement?.closest('[role="tablist"]').querySelector('[role="tab"]')`);
  step('onglets au clavier');

  // The shell demo's sidebar watches its overflow; leaving the page releases it. Its first theme scope
  // keeps the snapshot of its look (SnapshotKey), rewritten when the appearance changes.
  const snapshotKey = 'omnieurope.showcase.shell-theme';
  await evaluate(`localStorage.removeItem(${JSON.stringify(snapshotKey)})`);
  await session.visit('/composants/coquille', '#shell-demo-sidebar');
  const snapshot = `JSON.parse(localStorage.getItem(${JSON.stringify(snapshotKey)}) ?? 'null')`;
  await waitFor('l\'instantané du thème écrit', `${snapshot}?.appearance === 'light' && ${snapshot}.density === 'compact' && ${snapshot}.version === 1`);
  await clickOn('#shell-demo-appearance');
  await waitFor('l\'instantané réécrit en sombre', `${snapshot}?.appearance === 'dark'`);
  await clickOn('#shell-demo-appearance');
  await waitFor('l\'instantané revenu en clair', `${snapshot}?.appearance === 'light'`);
  step('instantané du thème écrit puis réécrit à chaque apparence');

  // The main element that scrolls on its own shows its scrollbar while it moves, then hides it again.
  const scrollMain = '#shell-demo-scroll-main';
  await mouse('mouseWheel', await session.pointOf(`${scrollMain} p`), { button: 'none', deltaX: 0, deltaY: 60 });
  await waitFor('la barre montrée pendant le défilement', `document.querySelector(${JSON.stringify(scrollMain)}).scrollTop > 0 && document.querySelector(${JSON.stringify(scrollMain)}).classList.contains('omni-main--scrolling')`);
  await waitFor('la barre cachée après le défilement', `!document.querySelector(${JSON.stringify(scrollMain)}).classList.contains('omni-main--scrolling')`, 5_000);
  step('barre de défilement montrée le temps du défilement');

  // Tabs whose selected panel scrolls on its own: a wheel turn over the tab strip, outside the
  // panel but inside the named frame, scrolls the panel.
  const tabsPanel = "[...document.querySelectorAll('#shell-demo-tabs-scope .omni-tabs__panel')].find(panel => !panel.hidden)";
  check(await evaluate(`${tabsPanel}.scrollHeight > ${tabsPanel}.clientHeight + 10`), 'Le panneau des onglets ne déborde pas : la molette ne prouverait rien.');
  await mouse('mouseWheel', await session.pointOf('#shell-demo-tabs-scope [role="tab"]'), { button: 'none', deltaX: 0, deltaY: 80 });
  await waitFor('le panneau défilé par la molette sur la barre d\'onglets', `${tabsPanel}.scrollTop > 20`);
  step('molette sur la barre d\'onglets qui fait défiler le panneau');

  // 7. A bound fieldset that reports its toggle, and a scroll strip driven by its chevrons on a phone.
  await session.visit('/composants/mise-en-page', '#demo-fieldset-bound');
  const stateBefore = await evaluate("document.getElementById('demo-fieldset-state').textContent");
  await clickOn('#demo-fieldset-bound summary');
  await waitFor('l\'état du groupe rapporté', `document.getElementById('demo-fieldset-state').textContent !== ${JSON.stringify(stateBefore)}`);
  step('groupe repliable lié');

  await send('Emulation.setDeviceMetricsOverride', { width: 375, height: 812, deviceScaleFactor: 1, mobile: true });
  await pause(500);
  const strip = '.omni-stack--scroll, [class*="omni-stack-scroll"]';
  const end = await evaluate(`(() => { const button = [...document.querySelectorAll('.omni-stack-scroll__button--end')].find(candidate => candidate.getClientRects().length > 0 && !candidate.disabled); if (!button) return null; button.id ||= 'probe-scroll-end'; return button.id; })()`);
  check(end !== null, `Aucun chevron de défilement visible à 375 px (${strip}).`);
  const scroller = await evaluate(`(() => { const button = document.getElementById(${JSON.stringify(end)}); const host = button.parentElement; const viewport = [...host.querySelectorAll('*')].find(element => element.scrollWidth > element.clientWidth + 1); viewport.id ||= 'probe-scroll-viewport'; return viewport.id; })()`);
  const before = await evaluate(`document.getElementById(${JSON.stringify(scroller)}).scrollLeft`);
  await clickOn(`#${end}`);
  await waitFor('la bande défilée vers la fin', `Math.abs(document.getElementById(${JSON.stringify(scroller)}).scrollLeft) > ${Math.abs(before) + 10}`);
  const start = await evaluate(`(() => { const button = document.getElementById(${JSON.stringify(end)}).parentElement.querySelector('.omni-stack-scroll__button--start'); button.id ||= 'probe-scroll-start'; return button.id; })()`);
  await waitFor('le chevron de début actif', `!document.getElementById(${JSON.stringify(start)}).disabled && document.getElementById(${JSON.stringify(start)}).getClientRects().length > 0`);
  const middle = await evaluate(`document.getElementById(${JSON.stringify(scroller)}).scrollLeft`);
  await clickOn(`#${start}`);
  await waitFor('la bande revenue vers le début', `Math.abs(document.getElementById(${JSON.stringify(scroller)}).scrollLeft) < ${Math.abs(middle)}`);
  step('bande défilante menée par ses chevrons à 375 px');

  // 8. The page header's title strip on a phone: the demo title is short, so the probe lengthens it the
  // way a late value would; the chevron then scrolls it and the module watches that scroll.
  await session.visit('/composants/pages', '#pages-demo-shell .omni-page-header__title');
  const title = '#pages-demo-shell .omni-page-header__title';
  const shortTitle = await evaluate(`document.querySelector(${JSON.stringify(title)}).textContent`);
  await evaluate(`document.querySelector(${JSON.stringify(title)}).textContent = 'srv-paris-01-production-europe-ouest-grappe-principale-noeud-de-calcul-numero-quarante-deux'`);
  await waitFor('le chevron de fin du titre', `document.querySelector('#pages-demo-shell .omni-page-header__scroll--end')?.getClientRects().length > 0`);
  await clickOn('#pages-demo-shell .omni-page-header__scroll--end');
  await waitFor('le titre défilé', `Math.abs(document.querySelector(${JSON.stringify(title)}).scrollLeft) > 10`);
  await waitFor('le chevron de début du titre', `document.querySelector('#pages-demo-shell .omni-page-header__scroll--start')?.getClientRects().length > 0`);
  await evaluate(`document.querySelector(${JSON.stringify(title)}).textContent = ${JSON.stringify(shortTitle)}`);
  step('titre d\'en-tête long défilé par son chevron à 375 px');
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);

  // The header's back button names no destination: it steps back in the history, to the page the
  // reader came from.
  await session.visit('/composants/badges', '.omni-badge');
  await session.visit('/composants/pages', '#pages-demo-shell .omni-page-header__back');
  await clickOn('#pages-demo-shell .omni-page-header__back');
  await waitFor('le retour à la page précédente', "location.pathname === '/composants/badges'");
  step('retour sans destination : un pas en arrière dans l\'historique');

  // 9. The appearance window: the control size and the Black hole theme, whose canvas the scope draws.
  await session.visit('/composants/themes', '#demo-appearance-window');
  await clickOn('#demo-appearance-window');
  await waitFor('la fenêtre d\'apparence', "document.querySelector('.omni-appearance-window input[type=\"range\"][aria-label]') !== null");
  await evaluate(`(() => { const range = [...document.querySelectorAll('.omni-appearance-window input[type="range"]')][1]; range.value = '7'; range.dispatchEvent(new Event('input', { bubbles: true })); range.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await waitFor('la taille des contrôles posée', "document.documentElement.dataset.oeControlSize === '7'");
  await evaluate(`(() => { const select = document.querySelector('.omni-appearance-window select'); select.value = [...select.options].at(-1).value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await waitFor('le fond de Trou noir', "document.querySelector('.omni-theme-scope__canvas') !== null", 10_000);
  // The drawing follows the window size, the page visibility and the palette, and stops with the theme.
  await send('Emulation.setDeviceMetricsOverride', { width: 1200, height: 860, deviceScaleFactor: 1, mobile: false });
  await pause(200);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await evaluate("document.dispatchEvent(new Event('visibilitychange'))");
  await evaluate(`(() => { const select = [...document.querySelectorAll('.omni-appearance-window select')][1]; select.value = [...select.options][1].value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await pause(300);
  await evaluate(`(() => { const select = document.querySelector('.omni-appearance-window select'); select.value = [...select.options][0].value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await waitFor('le fond de Trou noir retiré', "document.querySelector('.omni-theme-scope__canvas') === null", 10_000);

  // The window is moved by its header.
  const header = await session.pointOf('section.omni-dialog [data-omni-dialog-drag-handle]');
  const grip = { x: header.x - 60, y: header.y };
  const left = await evaluate("document.querySelector('section.omni-dialog').getBoundingClientRect().left");
  await mouse('mouseMoved', grip, { button: 'none' });
  await mouse('mousePressed', grip);
  for (let index = 1; index <= 5; index++) await mouse('mouseMoved', { x: grip.x - index * 20, y: grip.y + index * 10 }, { buttons: 1 });
  await mouse('mouseReleased', { x: grip.x - 100, y: grip.y + 50 });
  await waitFor('la fenêtre déplacée', `Math.abs(document.querySelector('section.omni-dialog').getBoundingClientRect().left - ${left}) > 40`);
  await clickOn('.omni-appearance-window .omni-dialog__close');
  await waitFor('la fenêtre fermée', "document.querySelector('.omni-appearance-window') === null || document.querySelector('.omni-appearance-window').getClientRects().length === 0");
  step('fenêtre d\'apparence : taille des contrôles, Trou noir et retour, déplacée, fermée');

  // 10. Form helpers: the first invalid field focused on submit, a text copied by its button.
  await session.visit('/composants/validation', '#validation-name');
  await evaluate("document.getElementById('validation-name').closest('form').querySelector('button[type=\"submit\"]').scrollIntoView({ block: 'center' })");
  await clickOn('#validation-name ~ * button[type="submit"], form:has(#validation-name) button[type="submit"]');
  await waitFor('le premier champ invalide focalisé', "document.activeElement?.id === 'validation-name' || document.activeElement?.closest('[aria-invalid=\"true\"]') !== null");
  step('premier champ invalide focalisé');

  const origin = new URL(await evaluate('location.href')).origin;
  await send('Browser.grantPermissions', { origin, permissions: ['clipboardReadWrite', 'clipboardSanitizedWrite'] });
  await send('Emulation.setFocusEmulationEnabled', { enabled: true });
  await session.visit('/composants/editeur', '.omni-code-block__copy');
  await clickOn('.omni-code-block__copy');
  await waitFor('le texte copié', "navigator.clipboard.readText().then(text => text.includes('dotnet test'))");
  await send('Emulation.setFocusEmulationEnabled', { enabled: false });
  step('bloc de code copié dans le presse-papiers');
}
