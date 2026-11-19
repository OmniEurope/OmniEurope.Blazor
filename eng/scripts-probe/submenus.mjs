// The submenus of the menus (OmniSubMenu), driven on the overflow menu page and the context menu page
// with real mouse and keyboard events at 1280 and 375 px: the keyboard opens a submenu, moves inside it
// and closes it with the focus back on its entry; a real click on the entry leaves the menu open; a real
// click on a leaf closes the whole menu, gives the focus back to the trigger and runs the action; the
// submenu box lies fully inside the window.

export async function subMenuSteps(session, step) {
  const { evaluate, waitFor, key, pause, check, send, clickOn, hover } = session;
  const activeId = () => evaluate('document.activeElement?.id ?? ""');
  const visible = id => `(() => { const element = document.getElementById(${JSON.stringify(id)}); return element !== null && element.getClientRects().length > 0 && getComputedStyle(element).visibility !== 'hidden'; })()`;
  const noMenuVisible = "[...document.querySelectorAll('[role=\"menu\"]')].every(menu => menu.getClientRects().length === 0)";
  const insideWindow = id => evaluate(`(() => { const box = document.getElementById(${JSON.stringify(id)}).getBoundingClientRect(); return { left: box.left, top: box.top, right: box.right, bottom: box.bottom, width: innerWidth, height: innerHeight }; })()`);
  const expanded = id => evaluate(`document.getElementById(${JSON.stringify(id)})?.getAttribute('aria-expanded')`);
  const checkInside = async (id, where) => {
    const box = await insideWindow(id);
    check(box.left >= 0 && box.top >= 0 && box.right <= box.width && box.bottom <= box.height, `Sous-menu ${id} hors de la fenêtre ${where} : ${JSON.stringify(box)}.`);
  };
  const arrowDown = () => key('ArrowDown', 'ArrowDown', 40);
  const arrowRight = () => key('ArrowRight', 'ArrowRight', 39);
  const arrowLeft = () => key('ArrowLeft', 'ArrowLeft', 37);
  const escape = () => key('Escape', 'Escape', 27);

  for (const width of [1280, 375]) {
    const where = `à ${width} px`;
    await send('Emulation.setDeviceMetricsOverride', { width, height: 800, deviceScaleFactor: 1, mobile: false });
    await session.visit('/components/overflow-menu', '#submenu-menu');
    await pause(200);

    // The keyboard: the entry reached by ArrowDown, ArrowRight opens its list on the first item, ArrowDown
    // moves inside it only, ArrowLeft closes it with the focus back on its entry.
    await clickOn('#submenu-menu');
    await waitFor(`le menu ouvert ${where}`, visible('submenu-menu-menu'));
    await waitFor(`le focus sur la première entrée ${where}`, "document.activeElement?.id === 'submenu-edit'");
    await arrowDown();
    await waitFor(`le focus sur Insert ${where}`, "document.activeElement?.id === 'submenu-insert'");
    check(await expanded('submenu-insert') === 'false', `Insert annonce déjà son menu ouvert ${where}.`);
    await arrowRight();
    await waitFor(`le sous-menu Insert ouvert ${where}`, visible('submenu-insert-menu'));
    await waitFor(`le focus sur le premier élément du sous-menu ${where}`, "document.activeElement?.id === 'submenu-insert-before'");
    check(await expanded('submenu-insert') === 'true', `Insert n'annonce pas son menu ouvert ${where}.`);
    const named = await evaluate("document.getElementById('submenu-insert-menu').getAttribute('aria-labelledby') === 'submenu-insert' && document.getElementById('submenu-insert-menu').getAttribute('role') === 'menu'");
    check(named, `Le sous-menu n'est pas un role=menu nommé par son entrée ${where}.`);
    await checkInside('submenu-insert-menu', where);
    await arrowDown();
    await waitFor(`ArrowDown dans le sous-menu ${where}`, "document.activeElement?.id === 'submenu-insert-after'");
    await arrowDown();
    await arrowDown();
    await waitFor(`le sous-menu qui boucle sur lui-même ${where}`, "document.activeElement?.id === 'submenu-insert-before'");
    await arrowLeft();
    await waitFor(`le sous-menu refermé, le focus sur son entrée ${where}`, `document.activeElement?.id === 'submenu-insert' && ${visible('submenu-insert-menu')} === false`);
    check(await expanded('submenu-insert') === 'false', `Insert annonce encore son menu ouvert ${where}.`);
    check(await evaluate(visible('submenu-menu-menu')), `Le menu s'est fermé avec la flèche arrière ${where}.`);
    step(`sous-menu au clavier : flèche vers la fin ouvre, flèche bas reste dans le niveau, flèche arrière ferme et rend le focus ${where}`);

    // Escape in a submenu closes that level alone; at the top level it closes the menu as before.
    await arrowRight();
    await waitFor(`le sous-menu rouvert ${where}`, "document.activeElement?.id === 'submenu-insert-before'");
    await escape();
    await waitFor(`Échap qui ne ferme que le sous-menu ${where}`, `document.activeElement?.id === 'submenu-insert' && ${visible('submenu-insert-menu')} === false`);
    check(await evaluate(visible('submenu-menu-menu')), `Échap dans un sous-menu a fermé tout le menu ${where}.`);
    await escape();
    await waitFor(`Échap au premier niveau qui ferme le menu, le focus sur le déclencheur ${where}`, `document.activeElement?.id === 'submenu-menu' && ${noMenuVisible}`);
    step(`Échap : le sous-menu seul, puis le menu, le focus sur le déclencheur ${where}`);

    // The pointer: hovering opens the list without taking the focus, a real click on the entry leaves the
    // menu open (aria-expanded stays true), opening another entry closes the first.
    await clickOn('#submenu-menu');
    await waitFor(`le menu rouvert ${where}`, visible('submenu-menu-menu'));
    await hover('#submenu-type');
    await waitFor(`le sous-menu Change type ouvert au survol ${where}`, visible('submenu-type-menu'));
    const focusInside = await evaluate("document.getElementById('submenu-type-menu').contains(document.activeElement)");
    check(!focusInside, `Le survol a pris le focus dans le sous-menu ${where}.`);
    await clickOn('#submenu-type');
    await pause(300);
    check(await expanded('submenu-type') === 'true', `Le clic sur l'entrée a refermé son sous-menu ${where}.`);
    check(await evaluate(visible('submenu-menu-menu')) && await evaluate(visible('submenu-type-menu')), `Le clic sur l'entrée a fermé le menu ${where}.`);
    check(await evaluate("document.getElementById('submenu-last').textContent.trim() === 'none'"), `Le clic sur l'entrée a levé une action ${where}.`);
    await checkInside('submenu-type-menu', where);
    await clickOn('#submenu-insert');
    await waitFor(`Insert ouvert, Change type refermé ${where}`, `${visible('submenu-insert-menu')} && ${visible('submenu-type-menu')} === false`);
    check(await expanded('submenu-type') === 'false' && await expanded('submenu-insert') === 'true', `L'ouverture d'Insert n'a pas refermé Change type ${where}.`);
    await checkInside('submenu-insert-menu', where);
    step(`sous-menu à la souris : survol sans focus, clic qui laisse ouvert, frère refermé ${where}`);

    // A real click on a leaf: the whole menu closes, the focus is back on the trigger, the action ran.
    await clickOn('#submenu-insert-after');
    await waitFor(`tout le menu fermé ${where}`, noMenuVisible);
    await waitFor(`le focus sur le déclencheur ${where}`, "document.activeElement?.id === 'submenu-menu'");
    await waitFor(`l'action de la feuille ${where}`, "document.getElementById('submenu-last').textContent.trim() === 'Insert after'");
    check(await expanded('submenu-menu') === 'false', `Le déclencheur annonce encore son menu ouvert ${where}.`);
    step(`feuille d'un sous-menu : menu entièrement fermé, focus sur le déclencheur, action visible ${where}`);

    // The context menu: its Move to entry opens by the keyboard, the leaf closes the menu and runs.
    await session.visit('/components/context-menus', '#context-menu-zone');
    await clickOn('#context-menu-zone', { button: 'right' });
    await waitFor(`le menu contextuel ${where}`, "[...document.querySelectorAll('[role=\"menu\"]')].some(menu => menu.getClientRects().length > 0)");
    await waitFor(`le focus dans le menu contextuel ${where}`, "document.activeElement?.getAttribute('role') === 'menuitem'");
    await evaluate("document.getElementById('context-move').focus()");
    await arrowRight();
    await waitFor(`le sous-menu Move to ouvert ${where}`, visible('context-move-menu'));
    await waitFor(`le focus sur son premier élément ${where}`, "document.activeElement?.id === 'context-move-projects'");
    await checkInside('context-move-menu', where);
    await clickOn('#context-move-reports');
    await waitFor(`le menu contextuel entièrement fermé ${where}`, noMenuVisible);
    await waitFor(`l'action du menu contextuel ${where}`, "document.body.innerText.includes('Move to Reports')");
    step(`menu contextuel : sous-menu ouvert au clavier, feuille qui ferme tout et lève l'action ${where}`);
  }

  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(200);
}
