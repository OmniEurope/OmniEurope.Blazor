// The keyboard paths of PLAN-017 (audit of 2026-10-07), each driven with real keys on its showcase page:
// a tree item selected once by Enter or Space on its button, the select bar and the scheduler's views
// as radio groups (one tab stop, arrows, Home, End), and a chart legend drawn in the SVG reached by Tab
// and pressed by Enter or Space without scrolling the page.

export async function keySteps(session, results) {
  const { evaluate, waitFor, key, check, clickOn } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const enter = () => key('Enter', 'Enter', 13, 0, '\r');
  const space = () => key(' ', 'Space', 32, 0, ' ');
  const tab = () => key('Tab', 'Tab', 9);
  const focused = () => evaluate("document.activeElement?.textContent.trim() ?? ''");

  // 1. Tree: Enter, then Space, on the button of Anvers each toggle its selection once.
  await session.visit('/composants/arborescence', '.omni-tree');
  const anvers = "[...document.querySelectorAll('.omni-tree__select')].find(button => button.textContent.trim() === 'Anvers')";
  const anversSelected = `${anvers}.closest('[role=treeitem]').getAttribute('aria-selected') === 'true'`;
  const before = await evaluate(anversSelected);
  await evaluate(`${anvers}.focus()`);
  await enter();
  await waitFor('Anvers basculé une fois par Entrée', `(${anversSelected}) === ${!before}`);
  await space();
  await waitFor('Anvers rebasculé une fois par Espace', `(${anversSelected}) === ${before}`);
  step('arborescence : Entrée puis Espace basculent une seule fois');

  // 2. Select bar: one tab stop; the arrows pick the next and the previous option, End the last, Home the first.
  await session.visit('/composants/choix', '.omni-select-bar');
  const bar = "document.querySelector('.omni-select-bar[data-omni-roving]')";
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
  await session.visit('/composants/agenda', '.omni-scheduler');
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
  await session.visit('/composants/graphiques-avances', '.omni-chart');
  const entry = "document.querySelector('g.omni-chart__legend-entry--toggle')";
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
  await clickOn('g.omni-chart__legend-entry--toggle');
  await waitFor('la série masquée au clic', `${entry}.getAttribute('aria-pressed') === 'false'`);
  await clickOn('g.omni-chart__legend-entry--toggle');
  await waitFor('la série rendue au clic', `${entry}.getAttribute('aria-pressed') === 'true'`);
  step('légende à droite : Tab, Entrée, Espace sans défilement, clic');
}
