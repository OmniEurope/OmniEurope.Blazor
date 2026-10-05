// The data views: column hover, frozen columns, filter combos and teardown of the data grid, the cut
// cell tooltip, the log viewer, the kanban board, the spreadsheet, the tree, the scheduler and the
// mind map, each driven on its showcase page with real input and its result asserted.

export async function boardSteps(session, results) {
  const { evaluate, waitFor, key, pause, check, send, clickOn, hover, mouse, pointOf, html5Drag } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  // 1. The data grid page: a hovered body cell tints its column, a cut cell shows its text, a sort of
  // the virtual grid scrolls it back to its start.
  await session.visit('/composants/grille', '#demo-grid-tree');
  await hover('#demo-grid-tree tbody td[data-omni-col="Previous"]');
  await waitFor('la colonne teintée au survol', "document.querySelectorAll('#demo-grid-tree .omni-data-grid__column--hover').length > 1 && document.querySelector('#demo-grid-tree th.omni-data-grid__column--hover') !== null");
  await mouse('mouseMoved', { x: 5, y: 5 }, { button: 'none' });
  await waitFor('la teinte retirée hors de la grille', "document.querySelectorAll('#demo-grid-tree .omni-data-grid__column--hover').length === 0");
  step('colonne teintée au survol puis rendue');

  await evaluate(`(() => { const cell = [...document.querySelectorAll('#demo-grid-narrow .omni-data-grid__cell--text')].find(candidate => candidate.scrollWidth > candidate.clientWidth + 1); cell.id ||= 'probe-cut-cell'; })()`);
  await hover('#probe-cut-cell');
  await waitFor('le texte coupé en bulle', "document.querySelector('.omni-title-tooltip.omni-title-tooltip--visible')?.textContent.trim().length > 0");
  await mouse('mouseMoved', { x: 5, y: 5 }, { button: 'none' });
  step('cellule coupée lue en bulle');

  const sortBefore = await evaluate("document.querySelector('#demo-grid-capped-long th[data-omni-col=\"Applicant\"]').getAttribute('aria-sort')");
  await clickOn('#demo-grid-capped-long th[data-omni-col="Applicant"] button');
  await waitFor('la grille virtuelle triée', `document.querySelector('#demo-grid-capped-long th[data-omni-col="Applicant"]').getAttribute('aria-sort') !== ${JSON.stringify(sortBefore)}`);
  await waitFor('la grille virtuelle revenue au début', "document.querySelector('#demo-grid-capped-long .omni-data-grid__viewport').scrollTop === 0");
  step('grille virtuelle triée, ramenée au début');

  // 2. The advanced grid: a frozen column while the viewport scrolls sideways, the suggestions of a
  // combo filter placed under it, and every module released when the page is left.
  await session.visit('/composants/grille-avancee', '.omni-data-grid__viewport');
  await send('Emulation.setDeviceMetricsOverride', { width: 640, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(400);
  const viewport = '.omni-data-grid:has(th[data-omni-col="Reference"]) .omni-data-grid__viewport';
  const wide = await evaluate(`(() => { const viewport = document.querySelector(${JSON.stringify(viewport)}); return viewport.scrollWidth - viewport.clientWidth; })()`);
  check(wide > 20, `La grille avancée ne déborde pas en largeur à 640 px (${wide} px).`);
  const frozenBefore = await evaluate(`document.querySelector(${JSON.stringify(viewport)} + ' tbody td[data-omni-col="Reference"]').getBoundingClientRect().left`);
  const point = await pointOf(viewport + ' tbody td[data-omni-col="Applicant"]');
  await mouse('mouseWheel', point, { button: 'none', deltaX: 200, deltaY: 0 });
  await waitFor('la grille défilée en largeur', `document.querySelector(${JSON.stringify(viewport)}).scrollLeft > 20`);
  const frozenAfter = await evaluate(`document.querySelector(${JSON.stringify(viewport)} + ' tbody td[data-omni-col="Reference"]').getBoundingClientRect().left`);
  check(Math.abs(frozenAfter - frozenBefore) < 2, `La colonne figée a bougé : ${frozenBefore} -> ${frozenAfter}.`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);
  step('colonne figée pendant le défilement latéral');

  await clickOn('.omni-data-grid th[data-omni-col="Applicant"] details[data-omni-popover] > summary');
  await waitFor('le filtre Demandeur ouvert', "document.querySelector('.omni-data-grid th[data-omni-col=\"Applicant\"] details[data-omni-popover]').open");
  await clickOn('.omni-data-grid th[data-omni-col="Applicant"] .omni-combo__input');
  await session.type('a');
  await waitFor('la liste de suggestions placée', "document.querySelector('.omni-data-grid th[data-omni-col=\"Applicant\"] .omni-combo')?.style.getPropertyValue('--omni-anchor-y').endsWith('px') === true");
  await key('Escape', 'Escape', 27);
  step('suggestions du filtre placées sous leur champ');

  // Leaving the grid releases its modules: after a second round trip with the same gestures, the page
  // and the window hold exactly as many listeners as after the first (Blazor registers its delegated
  // events once, on their first use, which is why the first round trip is the reference).
  const listeners = async () => ({ document: await session.listenersOf('document'), window: await session.listenersOf('window') });
  await session.visit('/composants/listes', '.omni-log-viewer');
  await pause(500);
  const listenersBefore = await listeners();
  await session.visit('/composants/grille-avancee', '.omni-data-grid__viewport');
  await clickOn('.omni-data-grid th[data-omni-col="Applicant"] details[data-omni-popover] > summary');
  await waitFor('le filtre Demandeur rouvert', "document.querySelector('.omni-data-grid th[data-omni-col=\"Applicant\"] details[data-omni-popover]').open");
  await key('Escape', 'Escape', 27);
  await session.visit('/composants/listes', '.omni-log-viewer');
  await pause(500);
  const listenersAfter = await listeners();
  check(JSON.stringify(listenersAfter) === JSON.stringify(listenersBefore),
    `La grille avancée quittée a laissé des écouteurs : avant ${JSON.stringify(listenersBefore)}, après ${JSON.stringify(listenersAfter)}.`);
  step('grille avancée quittée deux fois sans écouteur laissé sur la page');

  // 3. The log viewer: a wheel upwards stops following, a search walks its matches, a scroll is watched.
  const logViewport = '.omni-log-viewer .omni-log-viewer__viewport';
  await mouse('mouseWheel', await pointOf(logViewport), { button: 'none', deltaX: 0, deltaY: -200 });
  await waitFor('le suivi interrompu par la molette', "document.querySelector('.omni-log-viewer button[aria-pressed]')?.getAttribute('aria-pressed') === 'false'");
  await clickOn('.omni-log-viewer button[aria-pressed]');
  await waitFor('le suivi repris', "document.querySelector('.omni-log-viewer button[aria-pressed]')?.getAttribute('aria-pressed') === 'true'");
  await mouse('mouseWheel', await pointOf(logViewport), { button: 'none', deltaX: 120, deltaY: 0 });
  await waitFor('le journal défilé en largeur', `document.querySelector(${JSON.stringify(logViewport)}).scrollLeft > 0`);
  await clickOn('.omni-log-viewer input');
  await session.type('dossier');
  await key('Enter', 'Enter', 13);
  await waitFor('le passage trouvé', "document.querySelector('.omni-log-viewer mark, .omni-log-viewer [class*=\"match\"]') !== null");
  const next = await evaluate("[...document.querySelectorAll('.omni-log-viewer button')].some(button => /next|suivant/i.test(button.className + ' ' + (button.getAttribute('aria-label') ?? '')))");
  if (next) await clickOn('.omni-log-viewer button.omni-log-viewer__next, .omni-log-viewer button[aria-label*="suivant" i]');
  await waitFor('le suivi coupé par la recherche', "document.querySelector('.omni-log-viewer button[aria-pressed]')?.getAttribute('aria-pressed') === 'false'");
  step('journal : molette, suivi, défilement, recherche');

  // 4. The kanban board by the keyboard, then a card dragged with the mouse.
  const card = '.omni-kanban [data-omni-kanban-card]';
  await evaluate(`document.querySelector(${JSON.stringify(card)}).focus()`);
  const firstCard = await evaluate("document.activeElement.getAttribute('data-omni-kanban-card')");
  await key('ArrowRight', 'ArrowRight', 39);
  await waitFor('la carte suivante', `document.activeElement?.getAttribute('data-omni-kanban-card') !== ${JSON.stringify(firstCard)}`);
  const carried = await evaluate("document.activeElement.getAttribute('data-omni-kanban-card')");
  const columnOf = id => `document.querySelector('[data-omni-kanban-card="${id}"]')?.closest('[data-omni-kanban-column]')?.getAttribute('data-omni-kanban-column')`;
  const columnBefore = await evaluate(columnOf(carried));
  await key(' ', 'Space', 32, 0, ' ');
  await waitFor('la carte saisie', "document.querySelector('.omni-kanban[data-omni-kanban-grabbed=\"true\"]') !== null");
  await key('ArrowRight', 'ArrowRight', 39);
  await waitFor('la carte portée dans la colonne suivante', `${columnOf(carried)} !== ${JSON.stringify(columnBefore)} && document.activeElement?.getAttribute('data-omni-kanban-card') === ${JSON.stringify(carried)}`);
  await key('Enter', 'Enter', 13);
  await waitFor('la carte posée', "document.querySelector('.omni-kanban[data-omni-kanban-grabbed=\"true\"]') === null");
  const dragged = await html5Drag(card, '.omni-kanban [data-omni-kanban-column]:first-child', async () => {
    await waitFor('la carte en cours de glisser', "document.querySelector('.omni-kanban__card--dragging') !== null", 3_000);
  });
  check(dragged.items?.some(item => item.mimeType === 'text/plain'), `Le glisser ne porte pas l'identifiant de la carte : ${JSON.stringify(dragged)}.`);
  step('tableau : clavier, carte portée et posée, glisser à la souris');

  // 5. The spreadsheet: arrows move the active cell, F2 opens its editor at the end of the text.
  await session.visit('/composants/tableur', '#demo-sheet [data-omni-sheet-grid]');
  await evaluate("document.querySelector('#demo-sheet [data-omni-sheet-grid]').focus()");
  const active = () => evaluate("document.querySelector('#demo-sheet [data-omni-sheet-grid]').getAttribute('aria-activedescendant')");
  const cellBefore = await active();
  await key('ArrowDown', 'ArrowDown', 40);
  await waitFor('la cellule active descendue', `document.querySelector('#demo-sheet [data-omni-sheet-grid]').getAttribute('aria-activedescendant') !== ${JSON.stringify(cellBefore)}`);
  await key('F2', 'F2', 113);
  await waitFor('l\'éditeur de cellule', "document.activeElement?.matches('[data-omni-sheet-editor]') && document.activeElement.selectionStart === document.activeElement.value.length");
  await key('Escape', 'Escape', 27);
  await waitFor('le retour à la grille', "document.activeElement?.matches('#demo-sheet [data-omni-sheet-grid]')");
  step('tableur : flèche, F2, Échap');

  // 6. The tree: a draggable account dragged onto another.
  await session.visit('/composants/arborescence', '#demo-tree-accounts');
  await clickOn('#demo-tree-accounts .omni-tree__toggle');
  await waitFor('les comptes dépliés', "document.querySelector('#demo-tree-accounts [draggable=\"true\"]') !== null");
  const status = await evaluate("document.querySelector('p[role=\"status\"]').textContent");
  await html5Drag('#demo-tree-accounts [draggable="true"]', '#demo-tree-accounts .omni-tree__item:last-child > .omni-tree__row');
  await waitFor('le compte rangé', `document.querySelector('p[role="status"]').textContent !== ${JSON.stringify(status)}`);
  step('arborescence : compte glissé sur un autre');

  // 7. The scheduler: an appointment dragged.
  await session.visit('/composants/agenda', '.omni-scheduler [data-omni-scheduler-appointment][draggable="true"]');
  const appointment = await evaluate("document.querySelector('.omni-scheduler [data-omni-scheduler-appointment][draggable=\"true\"]').getAttribute('data-omni-scheduler-appointment')");
  const carriedData = await html5Drag('.omni-scheduler [data-omni-scheduler-appointment][draggable="true"]', '.omni-scheduler td.omni-scheduler-grid__cell');
  check(carriedData.items?.some(item => item.mimeType === 'text/plain' && item.data === appointment) && carriedData.dragOperationsMask !== undefined,
    `Le glisser ne porte pas l'identifiant du rendez-vous ${appointment} : ${JSON.stringify(carriedData)}.`);
  step('agenda : rendez-vous glissé avec son identifiant');

  // 8. The mind map: a lasso on the background, a pinch, a long press and a double click on a node.
  await session.visit('/composants/diagramme', '#demo-mindmap-canvas');
  await evaluate("document.getElementById('demo-mindmap-canvas').scrollIntoView({ block: 'center' })");
  await pause(300);
  const area = await evaluate("(() => { const box = document.getElementById('demo-mindmap-canvas').getBoundingClientRect(); return { left: box.left, top: box.top, right: box.right, bottom: box.bottom }; })()");
  const from = { x: Math.round(area.left + 6), y: Math.round(area.top + 6) };
  const to = { x: Math.round((area.left + area.right) / 2), y: Math.round((area.top + area.bottom) / 2) };
  await mouse('mouseMoved', from, { button: 'none' });
  await mouse('mousePressed', from);
  for (let index = 1; index <= 6; index++) await mouse('mouseMoved', { x: from.x + (to.x - from.x) * index / 6, y: from.y + (to.y - from.y) * index / 6 }, { buttons: 1 });
  await waitFor('le lasso tracé', "document.querySelector('#demo-mindmap .omni-mindmap__lasso')?.getAttribute('visibility') === 'visible'");
  await mouse('mouseReleased', to);
  await waitFor('les nœuds pris au lasso', "document.querySelectorAll('#demo-mindmap [data-omni-selected=\"true\"]').length > 0");
  step('carte mentale : lasso');

  const zoom = () => evaluate("document.querySelector('#demo-mindmap .omni-mindmap__viewport').getAttribute('data-zoom')");
  const zoomBefore = await zoom();
  const centre = to;
  await send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: centre.x - 20, y: centre.y, id: 1 }, { x: centre.x + 20, y: centre.y, id: 2 }] });
  for (let index = 1; index <= 5; index++) {
    await send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x: centre.x - 20 - index * 15, y: centre.y, id: 1 }, { x: centre.x + 20 + index * 15, y: centre.y, id: 2 }] });
    await pause(30);
  }
  await send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await waitFor('le zoom au pincement', `document.querySelector('#demo-mindmap .omni-mindmap__viewport').getAttribute('data-zoom') !== ${JSON.stringify(zoomBefore)}`);
  step('carte mentale : pincement');

  await send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: from.x + 4, y: from.y + 4, id: 3 }] });
  await pause(800);
  await waitFor('le menu de l\'appui long', "[...document.querySelectorAll('#demo-mindmap .omni-mindmap__menu, [role=\"menu\"]')].some(menu => menu.getClientRects().length > 0)");
  await send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await key('Escape', 'Escape', 27);
  step('carte mentale : appui long');

  // The pinch moved the nodes: the one double-clicked is one whose centre lies inside the canvas.
  await evaluate(`(() => {
    const canvas = document.getElementById('demo-mindmap-canvas').getBoundingClientRect();
    const inside = [...document.querySelectorAll('#demo-mindmap .omni-mindmap__node')].find(candidate => {
      const box = candidate.getBoundingClientRect();
      const x = box.left + box.width / 2;
      const y = box.top + box.height / 2;
      return x > canvas.left + 10 && x < canvas.right - 10 && y > canvas.top + 10 && y < canvas.bottom - 10;
    });
    if (!inside) throw new Error('Aucun nœud dans le cadre de la carte après le pincement.');
    inside.setAttribute('data-probe-node', '');
  })()`);
  const node = await pointOf('#demo-mindmap [data-probe-node]');
  await mouse('mouseMoved', node, { button: 'none' });
  await mouse('mousePressed', node, { clickCount: 1 });
  await mouse('mouseReleased', node, { clickCount: 1 });
  await mouse('mousePressed', node, { clickCount: 2 });
  await mouse('mouseReleased', node, { clickCount: 2 });
  await waitFor('le libellé du nœud à renommer', "document.activeElement?.id?.endsWith('-label') === true");
  await key('Escape', 'Escape', 27);
  step('carte mentale : double clic qui ouvre le renommage');
}
