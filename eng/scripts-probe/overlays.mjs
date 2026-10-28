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
  await session.visit('/components/overflow-menu', '#overflow-menu-0');
  await clickOn('#overflow-menu-0');
  await waitFor('le menu ouvert', `[...document.querySelectorAll('.omni-overflow-menu:has(> #overflow-menu-0) [role="menu"]')].some(menu => menu.getClientRects().length > 0)`);
  await waitFor('le focus dans le menu', "document.activeElement?.getAttribute('role') === 'menuitem'");
  // The window narrows while the menu is open: the menu is placed again, still on screen.
  await send('Emulation.setDeviceMetricsOverride', { width: 1000, height: 800, deviceScaleFactor: 1, mobile: false });
  await pause(200);
  const menuBox = await evaluate(`(() => { const menu = [...document.querySelectorAll('.omni-overflow-menu:has(> #overflow-menu-0) [role="menu"]')].find(candidate => candidate.getClientRects().length > 0); const box = menu.getBoundingClientRect(); return { left: box.left, right: box.right, width: innerWidth }; })()`);
  check(menuBox.left >= 0 && menuBox.right <= menuBox.width, `Menu hors de la fenêtre après redimensionnement : ${JSON.stringify(menuBox)}.`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(200);
  const first = await focusedText();
  await key('ArrowDown', 'ArrowDown', 40);
  await waitFor('le focus sur l\'entrée suivante', `document.activeElement?.getAttribute('role') === 'menuitem' && document.activeElement.textContent.trim() !== ${JSON.stringify(first)}`);
  await key('Escape', 'Escape', 27);
  await waitFor('le menu fermé, le focus rendu', "document.activeElement?.classList.contains('omni-overflow-menu__trigger')");
  await clickOn('#overflow-menu-0');
  await waitFor('le menu rouvert', "document.activeElement?.getAttribute('role') === 'menuitem'");
  await key('Tab', 'Tab', 9);
  await waitFor('Tab qui ferme le menu', `[...document.querySelectorAll('.omni-overflow-menu:has(> #overflow-menu-0) [role="menu"]')].every(menu => menu.getClientRects().length === 0)`);
  step('menu au clavier : flèche, Échap, Tab');

  await session.visit('/components/context-menus', '#context-menu-zone');
  await clickOn('#context-menu-zone', { button: 'right' });
  await waitFor('le menu contextuel', `[...document.querySelectorAll('[role="menu"]')].some(menu => menu.getClientRects().length > 0)`);
  await clickOn('main h1, h1');
  await waitFor('le menu contextuel fermé par un appui dehors', `[...document.querySelectorAll('[role="menu"]')].every(menu => menu.getClientRects().length === 0)`);
  step('menu contextuel fermé par un appui dehors');

  // 2. Modal dialogs: Tab kept inside, a press on the veil of a sticky dialog that keeps it open, a
  // focus sentinel that sends the focus back in, and a free width written through the CSSOM.
  await session.visit('/components/dialogs', '#dialog-sticky');
  await clickOn('#dialog-sticky');
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

  await clickOn('#dialog-size-free');
  await waitFor('la largeur libre posée', "document.querySelector('section.omni-dialog[data-omni-dialog-width-ready]')?.style.getPropertyValue('--omni-dialog-width') === '30rem'");
  check(await evaluate("!/(^|;)\\s*(?!--)[a-z-]+\\s*:/i.test(document.querySelector('section.omni-dialog').getAttribute('style') ?? '')"), 'La largeur libre a écrit une propriété ordinaire.');
  // The free width removed while the dialog is open: the custom property and its ready mark leave, the
  // dialog stays open and takes the medium size, wider than 30rem.
  const freeWidth = await evaluate("document.querySelector('section.omni-dialog').getBoundingClientRect().width");
  await clickOn('#dialog-size-back');
  await waitFor('la largeur libre retirée, le dialogue ouvert', "(() => { const dialog = document.querySelector('section.omni-dialog'); return dialog !== null && document.getElementById('dialog-size-back') === null && dialog.style.getPropertyValue('--omni-dialog-width') === '' && !dialog.hasAttribute('data-omni-dialog-width-ready'); })()");
  await waitFor('la taille moyenne reprise', `document.querySelector('section.omni-dialog').getBoundingClientRect().width > ${freeWidth} + 100`);
  await clickOn('.omni-overlay .omni-dialog__close');
  await waitFor('la boîte à largeur libre fermée', "document.querySelector('section.omni-dialog') === null");
  step('largeur libre de la boîte, retirée pendant l\'ouverture');

  // 2b. One notification stack, one card width: a short and a long message sent to the same stack are
  // drawn as wide as each other (the stack widens to its longest message), at 1280 and 390 px.
  await session.visit('/components/legacy/superpositions', '#demo-notify-short');
  await clickOn('#demo-notify-short');
  await clickOn('#demo-notify-long');
  await waitFor('les deux notifications de la pile', "document.querySelectorAll('.omni-notification-region .omni-notification').length === 2");
  const stackWidths = await evaluate(`[...document.querySelectorAll('.omni-notification-region .omni-notification')].map(card => ({ long: card.classList.contains('omni-notification--long'), width: Math.round(card.getBoundingClientRect().width * 100) / 100 }))`);
  check(stackWidths.some(card => card.long) && stackWidths.some(card => !card.long), `La pile ne montre pas une carte courte et une longue : ${JSON.stringify(stackWidths)}.`);
  check(Math.abs(stackWidths[0].width - stackWidths[1].width) <= 0.5, `Les cartes d'une même pile n'ont pas la même largeur : ${JSON.stringify(stackWidths)}.`);
  await send('Emulation.setDeviceMetricsOverride', { width: 390, height: 844, deviceScaleFactor: 1, mobile: false });
  await pause(200);
  const narrowWidths = await evaluate(`[...document.querySelectorAll('.omni-notification-region .omni-notification')].map(card => Math.round(card.getBoundingClientRect().width * 100) / 100)`);
  check(narrowWidths.length === 2 && Math.abs(narrowWidths[0] - narrowWidths[1]) <= 0.5, `Les cartes d'une même pile n'ont pas la même largeur à 390 px : ${JSON.stringify(narrowWidths)}.`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  for (let remaining = 2; remaining > 0; remaining--) {
    await clickOn('.omni-notification-region .omni-notification__dismiss');
    await waitFor('une notification fermée', `document.querySelectorAll('.omni-notification-region .omni-notification').length === ${remaining - 1}`);
  }
  step('notifications courte et longue d\'une même pile : même largeur à 1280 et 390 px');

  // 3. Popovers: placed inside the window when opened, released when closed.
  await session.visit('/components/popover', '.omni-popover > button[aria-haspopup="dialog"]');
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
  await session.visit('/components/legacy/retours', '.omni-tooltip--expandable');
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
  await session.visit('/components/multi-select', '#multiselect-compact');
  await clickOn('#multiselect-compact > summary');
  await waitFor('le choix compact ouvert', "document.getElementById('multiselect-compact').open");
  await clickOn('#multiselect-compact input[type="checkbox"], #multiselect-compact [role="option"], #multiselect-compact label');
  await pause(200);
  check(await evaluate("document.getElementById('multiselect-compact').open"), 'Un clic dans le panneau l\'a fermé.');
  await key('Escape', 'Escape', 27);
  await waitFor('Échap qui ferme le choix compact', "!document.getElementById('multiselect-compact').open");
  await clickOn('#multiselect-compact > summary');
  await waitFor('le choix compact rouvert', "document.getElementById('multiselect-compact').open");
  await clickOn('main h1, h1');
  await waitFor('l\'appui dehors qui ferme le choix compact', "!document.getElementById('multiselect-compact').open");
  step('choix compact : clic dedans, Échap, appui dehors');

  // 6. Tabs by the keyboard.
  await session.visit('/components/legacy/navigation', '[role="tab"]');
  await evaluate("document.querySelector('[role=\"tab\"][aria-selected=\"true\"]').focus()");
  const tabBefore = await focusedText();
  await key('ArrowRight', 'ArrowRight', 39);
  await waitFor('l\'onglet suivant', `document.activeElement?.getAttribute('role') === 'tab' && document.activeElement.textContent.trim() !== ${JSON.stringify(tabBefore)}`);
  await key('Home', 'Home', 36);
  await waitFor('le premier onglet', `document.activeElement === document.activeElement?.closest('[role="tablist"]').querySelector('[role="tab"]')`);
  step('onglets au clavier');

  // The shell demo's sidebar watches its overflow; leaving the page releases it. Its first theme scope
  // keeps the snapshot of its look (SnapshotKey), rewritten when the appearance changes.
  const snapshotKey = 'omnieurope.example.shell-theme';
  await evaluate(`localStorage.removeItem(${JSON.stringify(snapshotKey)})`);
  await session.visit('/components/page-shell', '#shell-sidebar');
  const snapshot = `JSON.parse(localStorage.getItem(${JSON.stringify(snapshotKey)}) ?? 'null')`;
  await waitFor('l\'instantané du thème écrit', `${snapshot}?.appearance === 'light' && ${snapshot}.density === 'compact' && ${snapshot}.version === 1`);
  await clickOn('#shell-sidebar-appearance');
  await waitFor('l\'instantané réécrit en sombre', `${snapshot}?.appearance === 'dark'`);
  await clickOn('#shell-sidebar-appearance');
  await waitFor('l\'instantané revenu en clair', `${snapshot}?.appearance === 'light'`);
  step('instantané du thème écrit puis réécrit à chaque apparence');

  // The main element that scrolls on its own shows its scrollbar while it moves, then hides it again.
  const scrollMain = '#page-shell-main';
  // The miniature sits on a wide canvas that scrolls sideways: bring the column into the frame before the wheel turns.
  await evaluate(`document.querySelector(${JSON.stringify(`${scrollMain} p`)}).scrollIntoView({ block: 'nearest', inline: 'center' })`);
  await mouse('mouseWheel', await session.pointOf(`${scrollMain} p`), { button: 'none', deltaX: 0, deltaY: 60 });
  await waitFor('la barre montrée pendant le défilement', `document.querySelector(${JSON.stringify(scrollMain)}).scrollTop > 0 && document.querySelector(${JSON.stringify(scrollMain)}).classList.contains('omni-main--scrolling')`);
  await waitFor('la barre cachée après le défilement', `!document.querySelector(${JSON.stringify(scrollMain)}).classList.contains('omni-main--scrolling')`, 5_000);
  step('barre de défilement montrée le temps du défilement');

  // Tabs whose selected panel scrolls on its own: a wheel turn over the tab strip, outside the
  // panel but inside the named frame, scrolls the panel.
  const tabsPanel = "[...document.querySelectorAll('#page-shell-tabs-scope .omni-tabs__panel')].find(panel => !panel.hidden)";
  check(await evaluate(`${tabsPanel}.scrollHeight > ${tabsPanel}.clientHeight + 10`), 'Le panneau des onglets ne déborde pas : la molette ne prouverait rien.');
  await mouse('mouseWheel', await session.pointOf('#page-shell-tabs-scope [role="tab"]'), { button: 'none', deltaX: 0, deltaY: 80 });
  await waitFor('le panneau défilé par la molette sur la barre d\'onglets', `${tabsPanel}.scrollTop > 20`);
  step('molette sur la barre d\'onglets qui fait défiler le panneau');

  // 7. A bound fieldset that reports its toggle, and a scroll strip driven by its chevrons on a phone.
  await session.visit('/components/fieldsets', '#demo-fieldset-bound');
  const stateBefore = await evaluate("document.getElementById('demo-fieldset-state').textContent");
  await clickOn('#demo-fieldset-bound summary');
  await waitFor('l\'état du groupe rapporté', `document.getElementById('demo-fieldset-state').textContent !== ${JSON.stringify(stateBefore)}`);
  step('groupe repliable lié');

  // The scroll strip lives in the overflow specimens of the stacks page.
  await session.visit('/components/stacks', '.omni-stack-scroll');

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
  await session.visit('/components/details', '#details-shell .omni-page-header__title');
  const title = '#details-shell .omni-page-header__title';
  const shortTitle = await evaluate(`document.querySelector(${JSON.stringify(title)}).textContent`);
  await evaluate(`document.querySelector(${JSON.stringify(title)}).textContent = 'srv-paris-01-production-europe-ouest-grappe-principale-noeud-de-calcul-numero-quarante-deux'`);
  await waitFor('le chevron de fin du titre', `document.querySelector('#details-shell .omni-page-header__scroll--end')?.getClientRects().length > 0`);
  await clickOn('#details-shell .omni-page-header__scroll--end');
  await waitFor('le titre défilé', `Math.abs(document.querySelector(${JSON.stringify(title)}).scrollLeft) > 10`);
  await waitFor('le chevron de début du titre', `document.querySelector('#details-shell .omni-page-header__scroll--start')?.getClientRects().length > 0`);
  await evaluate(`document.querySelector(${JSON.stringify(title)}).textContent = ${JSON.stringify(shortTitle)}`);
  step('titre d\'en-tête long défilé par son chevron à 375 px');
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);

  // The header's back button names no destination: it steps back in the history, to the page the
  // reader came from.
  await session.visit('/components/badges', '.omni-badge');
  await session.visit('/components/details', '#details-shell .omni-page-header__back');
  await clickOn('#details-shell .omni-page-header__back');
  await waitFor('le retour à la page précédente', "location.pathname === '/components/badges'");
  step('retour sans destination : un pas en arrière dans l\'historique');

  // The pinned actions of the page header, with the markup of a pipeline run page (icon, back button,
  // two badges, a split button in the actions, a "⋮" menu of four entries, a pinned "Run again"): on a
  // phone the pinned button is in sight without any click, on a row of its own under line 1 and flush
  // with the inline end of the header, and the "⋮" trigger stays on line 1 at the end of the title row,
  // whether "Show more" is closed or open (recette R-30, R-31, R-40).
  const pinnedBox = `(() => {
    const header = document.getElementById('details-pinned');
    const rect = element => { const box = element.getBoundingClientRect(); return { top: box.top, bottom: box.bottom, left: box.left, right: box.right, width: box.width, height: box.height }; };
    const row = header.querySelector('.omni-page-header__row');
    const back = header.querySelector('.omni-page-header__back');
    const title = header.querySelector('.omni-page-header__title');
    const pinned = document.getElementById('details-pinned-relaunch');
    const trigger = header.querySelector('.omni-page-header__menu .omni-overflow-menu__trigger');
    const toggle = header.querySelector('.omni-page-header__toggle');
    return { viewport: innerWidth, row: rect(row), back: rect(back), title: rect(title), pinned: rect(pinned), pinnedShown: pinned.getClientRects().length > 0, trigger: rect(trigger), toggleShown: toggle.getClientRects().length > 0, expanded: toggle.getAttribute('aria-expanded') };
  })()`;
  for (const width of [375, 320]) {
    await send('Emulation.setDeviceMetricsOverride', { width, height: 812, deviceScaleFactor: 1, mobile: true });
    await session.visit('/components/details', '#details-pinned-relaunch');
    await evaluate("document.getElementById('details-pinned').scrollIntoView({ block: 'center' })");
    await pause(400);
    for (const state of ['closed', 'open']) {
      if (state === 'open') {
        await clickOn('#details-pinned .omni-page-header__toggle');
        await waitFor('« Voir plus » ouvert', "document.querySelector('#details-pinned .omni-page-header__toggle')?.getAttribute('aria-expanded') === 'true'");
        await pause(300);
      }

      const box = await evaluate(pinnedBox);
      const where = `à ${width} px, « Voir plus » ${state}`;
      check(box.toggleShown, `Le bouton « Voir plus » est absent ${where} : ${JSON.stringify(box)}.`);
      check(box.expanded === (state === 'open' ? 'true' : 'false'), `État de « Voir plus » inattendu ${where} : ${box.expanded}.`);
      check(box.pinnedShown && box.pinned.width > 0 && box.pinned.left >= 0 && box.pinned.right <= box.viewport, `Le bouton épinglé n'est pas visible sans clic ${where} : ${JSON.stringify(box.pinned)}.`);
      check(Math.abs(box.pinned.right - box.row.right) <= 1, `Le bouton épinglé n'est pas aligné à la fin de l'en-tête ${where} : ${box.pinned.right} contre ${box.row.right}.`);
      check(box.pinned.top >= box.trigger.bottom - 1, `Le bouton épinglé n'est pas sous la ligne 1 ${where} : ${box.pinned.top} contre ${box.trigger.bottom}.`);
      check(Math.abs(box.trigger.top - box.back.top) <= 2 && box.trigger.top < box.pinned.top, `Le menu ⋮ n'est pas sur la ligne du titre ${where} : ${box.trigger.top} contre ${box.back.top}.`);
      check(box.trigger.left >= box.title.right - 1 && Math.abs(box.trigger.right - box.row.right) <= 1, `Le menu ⋮ n'est pas à l'extrémité de la ligne du titre ${where} : ${JSON.stringify({ trigger: box.trigger, title: box.title, row: box.row })}.`);
    }

    await clickOn('#details-pinned .omni-page-header__toggle');
  }

  step('actions épinglées de l\'en-tête visibles sans clic sous la ligne 1 et menu ⋮ au bout de la ligne du titre à 375 et 320 px, « Voir plus » fermé et ouvert');
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);

  // 9. The appearance window: the control size and the Black hole theme, whose canvas the scope draws.
  await session.visit('/components/theme', '#demo-appearance-window');
  await clickOn('#demo-appearance-window');
  await waitFor('la fenêtre d\'apparence', "document.querySelector('.omni-appearance-window input[type=\"range\"][aria-label]') !== null");
  await evaluate(`(() => { const range = [...document.querySelectorAll('.omni-appearance-window input[type="range"]')][1]; range.value = '7'; range.dispatchEvent(new Event('input', { bubbles: true })); range.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await waitFor('la taille des contrôles posée', "document.documentElement.dataset.oeControlSize === '7'");
  // The probe browser draws WebGL in software, which omni-black-hole.js refuses
  // (failIfMajorPerformanceCaveat) for the CSS field: the request loses that flag here so the drawing
  // path runs, and its slow frames bring the guard that holds the field still.
  await evaluate(`(() => { const original = HTMLCanvasElement.prototype.getContext; window.__omniGetContext = original; HTMLCanvasElement.prototype.getContext = function (type, options) { return original.call(this, type, type === 'webgl' && options ? { ...options, failIfMajorPerformanceCaveat: false } : options); }; })()`);
  await evaluate(`(() => { const select = document.querySelector('.omni-appearance-window select'); select.value = [...select.options].at(-1).value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  // The canvas is drawn before the scope starts the script on it: the request keeps its relaxed flag
  // until the first frame sizes the canvas to the window, else start() can still meet the plain one.
  await waitFor('le fond de Trou noir', "(() => { const canvas = document.querySelector('.omni-theme-scope__canvas'); return canvas !== null && canvas.width === Math.round(innerWidth * Math.min(Math.max(devicePixelRatio || 1, 1), 2)); })()", 10_000);
  await evaluate('HTMLCanvasElement.prototype.getContext = window.__omniGetContext');
  // The field follows the system's wish for less motion as it changes.
  await send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }] });
  await pause(200);
  await send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: '' }] });
  // The drawing follows the window size, the page visibility and the palette, and stops with the theme.
  await send('Emulation.setDeviceMetricsOverride', { width: 1200, height: 860, deviceScaleFactor: 1, mobile: false });
  await pause(200);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await evaluate("document.dispatchEvent(new Event('visibilitychange'))");
  await evaluate(`(() => { const select = [...document.querySelectorAll('.omni-appearance-window select')][1]; select.value = [...select.options][1].value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await pause(300);
  await evaluate(`(() => { const select = document.querySelector('.omni-appearance-window select'); select.value = [...select.options][0].value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await waitFor('le fond de Trou noir retiré', "document.querySelector('.omni-theme-scope__canvas') === null", 10_000);

  // The window never overflows, whatever the theme (the Retro theme widens its buttons: uppercase,
  // weight 800, offset shadows). Every theme of the list, in light and dark, at text size 1, 5 and 10, in
  // compact and comfortable density, at 1280 and 375 px: the window does not scroll sideways and no
  // button clips its own text. The sweep runs inside the page through the window's own controls and
  // waits two frames after each change, so it stays short; a dark-only theme keeps its fixed mode.
  const sweepWindow = async () => {
    const settle = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(resolve, 40))));
    const windowOf = () => document.querySelector('.omni-appearance-window');
    const themeSelect = () => windowOf().querySelector('select');
    const modeButtons = () => [...windowOf().querySelectorAll('.omni-appearance-window__modes button[role="radio"]')];
    const densityItems = () => [...windowOf().querySelector('[role="radiogroup"]:not(.omni-appearance-window__modes)').querySelectorAll('.omni-select-bar__item')];
    const textSize = () => windowOf().querySelectorAll('input[type="range"]')[0];
    const change = (element, value) => { element.value = value; element.dispatchEvent(new Event('input', { bubbles: true })); element.dispatchEvent(new Event('change', { bubbles: true })); };
    const nameOf = button => button.getAttribute('aria-label') || button.textContent.trim() || button.className;
    const measure = () => {
      const dialog = windowOf();
      const problems = [];
      if (dialog.scrollWidth > dialog.clientWidth) {
        // Named by its widest culprit: the element whose right edge passes the dialog's visible width.
        const limit = dialog.getBoundingClientRect().left + dialog.clientLeft + dialog.clientWidth;
        const culprit = [...dialog.querySelectorAll('*')].filter(element => element.getBoundingClientRect().right > limit + 1).at(-1);
        const named = culprit ? `${culprit.tagName.toLowerCase()}.${String(culprit.className.baseVal ?? culprit.className).trim().split(/\s+/).slice(0, 2).join('.')}` : 'aucun élément';
        problems.push(`fenêtre ${dialog.scrollWidth}>${dialog.clientWidth} (${named})`);
      }
      const footer = dialog.querySelector('.omni-appearance-window__footer');
      if (footer && footer.scrollWidth > footer.clientWidth) problems.push(`pied ${footer.scrollWidth}>${footer.clientWidth}`);
      // A button is cut when its content does not fit it: an element of it reaching past its box, or a
      // label wider than its line. The button's own scrollWidth is not read: the close button widens its
      // hit area with an absolute ::before beyond its box on purpose, and that counts in scrollWidth.
      for (const button of dialog.querySelectorAll('button')) {
        if (button.getClientRects().length === 0) continue;
        const box = button.getBoundingClientRect();
        const outside = [...button.querySelectorAll('*')].find(element => { const own = element.getBoundingClientRect(); return own.width > 0 && (own.left < box.left - 1 || own.right > box.right + 1); });
        const label = [...button.querySelectorAll('.omni-button__content')].find(element => element.scrollWidth > element.clientWidth + 1);
        if (outside || label) problems.push(`bouton « ${nameOf(button)} » ${outside ? 'déborde' : `libellé ${label.scrollWidth}>${label.clientWidth}`}`);
      }

      return problems;
    };

    const themes = [...themeSelect().options].map(option => ({ value: option.value, text: option.text }));
    const initialMode = modeButtons().findIndex(button => button.getAttribute('aria-checked') === 'true');
    const failures = [];
    let combinations = 0;
    for (const theme of themes) {
      change(themeSelect(), theme.value);
      await settle();
      for (const mode of [0, 1]) {
        const button = modeButtons()[mode];
        if (!button.disabled) {
          button.click();
          await settle();
        }

        const shown = modeButtons().findIndex(candidate => candidate.getAttribute('aria-checked') === 'true');
        for (const level of [1, 5, 10]) {
          change(textSize(), String(level));
          await settle();
          for (const density of [0, 1]) {
            densityItems()[density].click();
            await settle();
            combinations++;
            const problems = measure();
            if (problems.length > 0) failures.push(`${theme.text} / mode ${shown} / texte ${level} / densité ${density} / ${innerWidth} px : ${problems.slice(0, 4).join(', ')}`);
          }
        }
      }
    }

    change(themeSelect(), themes[0].value);
    change(textSize(), '5');
    densityItems()[1].click();
    if (initialMode >= 0 && !modeButtons()[initialMode].disabled) modeButtons()[initialMode].click();
    await settle();
    return { themes: themes.length, combinations, failures };
  };
  for (const width of [1280, 375]) {
    await send('Emulation.setDeviceMetricsOverride', { width, height: width < 600 ? 812 : 900, deviceScaleFactor: 1, mobile: width < 600 });
    await pause(300);
    const swept = await evaluate(`(${sweepWindow})()`);
    check(swept.themes >= 15, `La liste des thèmes de la fenêtre n'en compte que ${swept.themes} : le balayage ne prouverait pas les 15 thèmes.`);
    check(swept.combinations === swept.themes * 12, `Balayage incomplet à ${width} px : ${swept.combinations} combinaisons mesurées sur ${swept.themes * 12}.`);
    check(swept.failures.length === 0, `La fenêtre d'apparence déborde (${swept.failures.length} cas sur ${swept.combinations}) : ${swept.failures.slice(0, 6).join(' | ')}`);
  }

  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);
  step('fenêtre d\'apparence sans débordement : chaque thème, clair et sombre, texte 1, 5 et 10, densité compacte et aérée, à 1280 et 375 px');

  // The window is moved by its header.
  const header =await session.pointOf('section.omni-dialog [data-omni-dialog-drag-handle]');
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
  await session.visit('/components/validation', '#signup-name');
  await evaluate("document.getElementById('signup-name').closest('form').querySelector('button[type=\"submit\"]').scrollIntoView({ block: 'center' })");
  await clickOn('form:has(#signup-name) button[type="submit"]');
  await waitFor('le premier champ invalide focalisé', "document.activeElement?.id === 'signup-name' || document.activeElement?.closest('[aria-invalid=\"true\"]') !== null");
  step('premier champ invalide focalisé');

  const origin = new URL(await evaluate('location.href')).origin;
  await send('Browser.grantPermissions', { origin, permissions: ['clipboardReadWrite', 'clipboardSanitizedWrite'] });
  await send('Emulation.setFocusEmulationEnabled', { enabled: true });
  await session.visit('/components/legacy/editeur', '.omni-code-block__copy');
  await clickOn('.omni-code-block__copy');
  await waitFor('le texte copié', "navigator.clipboard.readText().then(text => text.includes('dotnet test'))");
  await send('Emulation.setFocusEmulationEnabled', { enabled: false });
  step('bloc de code copié dans le presse-papiers');
}
