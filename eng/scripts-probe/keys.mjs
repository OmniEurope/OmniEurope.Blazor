// The keyboard paths of PLAN-017 (audit of 2026-10-07), each driven with real keys on its showcase page:
// a tree item selected once by Enter or Space on its button, the select bar and the scheduler's views
// as radio groups (one tab stop, arrows, Home, End), and a chart legend drawn in the SVG reached by Tab
// and pressed by Enter or Space without scrolling the page; a grid row in edit mode saved by Enter and
// cancelled by Escape, at its read height (PLAN-001 lot 5); an accordion whose headers answer the arrows,
// Home and End, whose help mark and own controls never toggle a row, and whose whole header is clickable.

export async function keySteps(session, results) {
  const { evaluate, waitFor, key, type, check, clickOn } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const enter = () => key('Enter', 'Enter', 13, 0, '\r');
  const space = () => key(' ', 'Space', 32, 0, ' ');
  const tab = () => key('Tab', 'Tab', 9);
  const focused = () => evaluate("document.activeElement?.textContent.trim() ?? ''");

  // 1. Tree: Enter, then Space, on the button of Antwerp each toggle its selection once.
  await session.visit('/components/tree', '.omni-tree');
  const anvers = "[...document.querySelectorAll('.omni-tree__select')].find(button => button.textContent.trim() === 'Antwerp')";
  const anversSelected = `${anvers}.closest('[role=treeitem]').getAttribute('aria-selected') === 'true'`;
  const before = await evaluate(anversSelected);
  await evaluate(`${anvers}.focus()`);
  await enter();
  await waitFor('Antwerp basculé une fois par Entrée', `(${anversSelected}) === ${!before}`);
  await space();
  await waitFor('Antwerp rebasculé une fois par Espace', `(${anversSelected}) === ${before}`);
  step('arborescence : Entrée puis Espace basculent une seule fois');

  // 2. Select bar: one tab stop; the arrows pick the next and the previous option, End the last, Home the first.
  await session.visit('/components/select-bar', '#selectbar-period');
  const bar = "document.querySelector('#selectbar-period[data-omni-roving]')";
  const radios = `[...${bar}.querySelectorAll('[role=radio]:not([disabled])')]`;
  check(await evaluate(`${bar}.querySelectorAll('[role=radio][tabindex="0"]').length === 1`), 'La barre de choix n\'a pas un seul arrêt de tabulation.');
  await evaluate(`${bar}.querySelector('[role=radio][tabindex="0"]').focus()`);
  const start = await evaluate(`${radios}.indexOf(document.activeElement)`);
  const count = await evaluate(`${radios}.length`);
  const checkedAt = index => `${radios}[${index}].getAttribute('aria-checked') === 'true' && document.activeElement === ${radios}[${index}] && ${radios}[${index}].tabIndex === 0`;
  await key('ArrowRight', 'ArrowRight', 39);
  await waitFor('l\'option suivante choisie par la flèche', checkedAt((start + 1) % count));
  await key('ArrowLeft', 'ArrowLeft', 37);
  await waitFor('l\'option précédente choisie par la flèche', checkedAt(start));
  await key('End', 'End', 35);
  await waitFor('la dernière option choisie par Fin', checkedAt(count - 1));
  await key('Home', 'Home', 36);
  await waitFor('la première option choisie par Début', checkedAt(0));
  await key('Home', 'Home', 36);
  await key('ArrowDown', 'ArrowDown', 40, 2);
  check(await evaluate(checkedAt(0)), 'Une flèche avec Ctrl a changé l\'option choisie.');
  await key('a', 'KeyA', 65, 0, 'a');
  check(await evaluate(checkedAt(0)), 'Une lettre a changé l\'option choisie.');
  step('barre de choix : un arrêt de tabulation, flèches, Début et Fin');

  // 3. Scheduler: its views are a radio group of the same keys.
  await session.visit('/components/scheduler', '.omni-scheduler');
  const views = "document.querySelector('.omni-scheduler [role=radiogroup][data-omni-roving]')";
  await evaluate(`${views}.querySelector('[role=radio][tabindex="0"]').focus()`);
  const view = await focused();
  await key('ArrowRight', 'ArrowRight', 39);
  await waitFor('la vue suivante choisie par la flèche', `document.activeElement.getAttribute('aria-checked') === 'true' && document.activeElement.textContent.trim() !== ${JSON.stringify(view)}`);
  await key('ArrowLeft', 'ArrowLeft', 37);
  await waitFor('la vue de départ reprise', `document.activeElement.getAttribute('aria-checked') === 'true' && document.activeElement.textContent.trim() === ${JSON.stringify(view)}`);
  step('agenda : les vues suivent les flèches');

  // 4. Chart legend on the right: Tab reaches an entry, Enter hides its series, Space shows it again, the
  // page does not scroll; a key that is neither leaves it alone.
  await session.visit('/components/line-charts', '#chart-legend-right.omni-chart');
  const entry = "document.querySelector('#chart-legend-right g.omni-chart__legend-entry--toggle')";
  await evaluate(`${entry}.closest('.omni-chart').scrollIntoView({ block: 'center' })`);
  await evaluate(`(() => { const all = [...document.querySelectorAll('a[href], button, [tabindex="0"]')]; all[all.indexOf(${entry}) - 1].focus(); })()`);
  await tab();
  await waitFor('l\'entrée de légende atteinte par Tab', `document.activeElement === ${entry}`);
  const scroll = await evaluate('window.scrollY');
  await enter();
  await waitFor('la série masquée par Entrée', `${entry}.getAttribute('aria-pressed') === 'false' && ${entry}.classList.contains('omni-chart__legend-entry--hidden')`);
  await space();
  await waitFor('la série rendue par Espace', `${entry}.getAttribute('aria-pressed') === 'true'`);
  check(await evaluate(`window.scrollY === ${scroll}`), 'Espace sur l\'entrée de légende a fait défiler la page.');
  await key('x', 'KeyX', 88, 0, 'x');
  check(await evaluate(`${entry}.getAttribute('aria-pressed') === 'true'`), 'Une autre touche a basculé l\'entrée de légende.');
  await clickOn('#chart-legend-right g.omni-chart__legend-entry--toggle');
  await waitFor('la série masquée au clic', `${entry}.getAttribute('aria-pressed') === 'false'`);
  await clickOn('#chart-legend-right g.omni-chart__legend-entry--toggle');
  await waitFor('la série rendue au clic', `${entry}.getAttribute('aria-pressed') === 'true'`);
  step('légende à droite : Tab, Entrée, Espace sans défilement, clic');

  // 5. Grid row in edit mode (G-D29-03, G-D26-04): the row keeps its read height; the value typed in its
  // field is saved by Enter, the focus coming back to the row; Escape cancels the next edit.
  await session.visit('/components/legacy/grille', '#demo-grid-edit');
  const editRow = "document.querySelector('#demo-grid-edit tbody tr[data-omni-row-index=\"0\"]')";
  const editing = `${editRow}.classList.contains('omni-data-grid__row--editing')`;
  const fieldFocused = `${editRow}.contains(document.activeElement) && document.activeElement.matches('input')`;
  await evaluate(`${editRow}.scrollIntoView({ block: 'center' })`);
  const readHeight = await evaluate(`${editRow}.getBoundingClientRect().height`);
  await evaluate(`${editRow}.querySelector('td.omni-data-grid__actions button').click()`);
  await waitFor('la ligne en édition, son champ focalisé', `${editing} && ${fieldFocused}`);
  const editHeight = await evaluate(`${editRow}.getBoundingClientRect().height`);
  check(Math.abs(editHeight - readHeight) < 0.5, `La ligne en édition fait ${editHeight} px au lieu de ${readHeight} px en lecture.`);
  await type('Sonde');
  await enter();
  await waitFor('la ligne enregistrée par Entrée avec la valeur tapée', `!(${editing}) && ${editRow}.querySelector('td[data-omni-col="Applicant"]').textContent.trim() === 'Sonde'`);
  check(await evaluate(`${editRow}.contains(document.activeElement) || document.activeElement === ${editRow}`), 'Le focus n\'est pas revenu à la ligne enregistrée.');
  await evaluate(`${editRow}.querySelector('td.omni-data-grid__actions button').click()`);
  await waitFor('la ligne de nouveau en édition', `${editing} && ${fieldFocused}`);
  await key('Escape', 'Escape', 27);
  await waitFor('la ligne annulée par Échap', `!(${editing})`);
  step('grille : hauteur de lecture gardée en édition, Entrée enregistre, Échap annule');

  // 6. Accordion: Down, Up, Home and End move between the headers (wrapping, the page not scrolling,
  // nothing opened by the move), Enter and Space toggle, a click on the value toggles its row and closes
  // its neighbour, a click or a key on the help mark and a click on an own control toggle nothing.
  await session.visit('/components/accordion', '#demo-accordion-gates');
  const gates = '#demo-accordion-gates';
  const toggles = `[...document.querySelectorAll('${gates} .omni-accordion__toggle')]`;
  const states = () => evaluate(`${toggles}.map(button => button.getAttribute('aria-expanded')).join()`);
  const activeIs = id => `document.activeElement.id === '${id}'`;
  await evaluate(`document.querySelector('${gates}').scrollIntoView({ block: 'center' })`);
  await evaluate("document.querySelector('#demo-accordion-reliability').focus()");
  await key('ArrowDown', 'ArrowDown', 40);
  await waitFor('Bas atteint l\'en-tête suivant', activeIs('demo-accordion-security'));
  check(await states() === 'true,false,false,false', 'Bas a ouvert ou fermé une ligne.');
  await key('End', 'End', 35);
  await waitFor('Fin atteint le dernier en-tête', activeIs('demo-accordion-coverage'));
  const accordionScroll = await evaluate('window.scrollY');
  await key('ArrowDown', 'ArrowDown', 40);
  await waitFor('Bas sur le dernier revient au premier', activeIs('demo-accordion-reliability'));
  await key('ArrowUp', 'ArrowUp', 38);
  await waitFor('Haut sur le premier va au dernier', activeIs('demo-accordion-coverage'));
  await key('Home', 'Home', 36);
  await waitFor('Début atteint le premier en-tête', activeIs('demo-accordion-reliability'));
  check(await evaluate(`window.scrollY === ${accordionScroll}`), 'Une flèche, Début ou Fin a fait défiler la page.');
  await space();
  await waitFor('Espace ferme la ligne ouverte', `${toggles}[0].getAttribute('aria-expanded') === 'false'`);
  await enter();
  await waitFor('Entrée ouvre la ligne', `${toggles}[0].getAttribute('aria-expanded') === 'true'`);
  await clickOn(`${gates} .omni-accordion__item:nth-child(3) .omni-accordion__summary`);
  await waitFor('un clic sur la valeur ouvre la ligne et ferme sa voisine', `${toggles}.map(button => button.getAttribute('aria-expanded')).join() === 'false,false,true,false'`);
  await clickOn(`${gates} .omni-accordion__help .omni-tooltip__trigger`);
  await waitFor('le focus sur la marque d\'aide', "document.activeElement.classList.contains('omni-tooltip__trigger')");
  await enter();
  await space();
  check(await states() === 'false,false,true,false', 'La marque d\'aide a basculé une ligne.');
  await clickOn('#demo-accordion-details');
  await waitFor('le contrôle propre compte son clic', "document.querySelector('#demo-accordion-clicks').textContent.includes('1')");
  check(await evaluate("document.querySelector('#demo-accordion-run').getAttribute('aria-expanded') === 'false'"), 'Un contrôle propre a basculé sa ligne.');
  step('accordéon : flèches, Début, Fin, Entrée, Espace, clic sur la valeur, aide et contrôle propre sans bascule');
}
