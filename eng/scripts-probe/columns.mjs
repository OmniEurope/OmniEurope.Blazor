// PLAN-019 lot 4: the columns of the advanced grid moved, hidden and reset by a reader, in a real browser.
// A header dragged onto another one lands before it, with the insertion line drawn on the way, keeps its
// elements (the cells are keyed by column) and does not sort; Escape cancels a drag, and a frozen header
// accepts no free column. The chooser of the header bar moves a column from the keyboard, the focus
// staying on its arrow, and hides one, the reference staying (Hideable="false"). The layout survives a
// reload through the StateKey preference; the reset brings the declared layout back, and the chooser
// fits a 375 px screen. Every change is announced by the grid's live region. The grid's preference is
// removed at the end, so the parts that follow find the declared layout.

import { probeOptions } from '../ShowcaseCdp.mjs';

const page = '/components/legacy/grille-avancee';
const grid = '.omni-data-grid:has(th[data-omni-col="Reference"])';
const header = key => `${grid} thead th[data-omni-col="${key}"]`;
const declared = ['Reference', 'Applicant', 'Country', 'Filed', 'Amount'];
const headerKeys = `[...document.querySelectorAll(${JSON.stringify(`${grid} thead tr.omni-data-grid__header-row th[data-omni-col]`)})].map(cell => cell.getAttribute('data-omni-col'))`;
const storedGrid = `(() => {
  const key = Object.keys(localStorage).find(name => name.startsWith('omni:pref:') && name.endsWith(':omni.grid.showcase-grid-advanced'));
  return key ? JSON.parse(JSON.parse(localStorage.getItem(key)).Value) : null;
})()`;
const clearGrid = "(() => { for (const key of Object.keys(localStorage)) if (key.endsWith(':omni.grid.showcase-grid-advanced')) localStorage.removeItem(key); })()";
const announced = `(document.querySelector(${JSON.stringify(`${grid} > [role="status"][aria-live="polite"]`)})?.textContent.trim() ?? '')`;
const titleOf = key => `document.querySelector(${JSON.stringify(`${header(key)} .omni-data-grid__title`)}).textContent.trim()`;
const chooserToggle = `${grid} .omni-data-grid__bar--header .omni-data-grid__chooser-toggle`;
const chooserPanel = `${grid} .omni-data-grid__chooser .omni-popover__panel`;
const focusInPanel = `document.querySelector(${JSON.stringify(chooserPanel)})?.contains(document.activeElement) === true`;
const arrow = (key, step) => `${chooserPanel} button[data-omni-col="${key}"][data-omni-move="${step}"]`;

export async function columnSteps(session, results) {
  const { evaluate, waitFor, check, send, mouse, key, clickOn, pointOf, pause } = session;
  const { siteUrl } = probeOptions('Test-ShowcaseScriptsProbe.mjs');
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const cspClean = async when => {
    const csp = await evaluate('window.__omniCsp');
    check(csp.length === 0, `Violations CSP ${when} : ${csp.join(' | ')}`);
  };
  const order = () => evaluate(headerKeys);
  const expectOrder = (expected, what) => waitFor(what, `JSON.stringify(${headerKeys}) === ${JSON.stringify(JSON.stringify(expected))}`);
  // Reloads the application at its root (the static host has no fallback for deep links), then opens the grid.
  const reload = async () => {
    await cspClean('avant le rechargement');
    await send('Page.navigate', { url: siteUrl });
    await waitFor('le runtime Blazor rechargé', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null");
    await session.visit(page, header('Amount'));
    await pause(300);
  };
  // A point inside a header, at a fraction of its width: the start quarter lands before it.
  const pointIn = (key, fraction) => evaluate(`(() => {
    const box = document.querySelector(${JSON.stringify(header(key))}).getBoundingClientRect();
    return { x: Math.round(box.left + box.width * ${fraction}), y: Math.round(box.top + box.height / 2) };
  })()`);
  const press = async from => {
    await mouse('mouseMoved', from, { button: 'none' });
    await mouse('mousePressed', from);
  };
  const moveTo = async (from, to) => {
    for (let index = 1; index <= 8; index++) {
      await mouse('mouseMoved', { x: from.x + (to.x - from.x) * index / 8, y: from.y + (to.y - from.y) * index / 8 }, { buttons: 1 });
      await pause(20);
    }
  };
  const openChooser = async () => {
    await clickOn(chooserToggle);
    await waitFor('le sélecteur de colonnes ouvert', `document.querySelector(${JSON.stringify(chooserPanel)}) !== null`);
    await waitFor('le focus porté dans le sélecteur ouvert', focusInPanel);
  };

  // 0. The declared layout, from a clean preference.
  await session.visit('/components/legacy/badges', '.omni-badge');
  await evaluate(clearGrid);
  await reload();
  check(JSON.stringify(await order()) === JSON.stringify(declared), `Ordre de départ inattendu : ${await order()}.`);
  check(await evaluate(`document.querySelectorAll(${JSON.stringify(`${grid} thead th[data-omni-reorder]`)}).length`) === declared.length, 'Les en-têtes ne sont pas offerts au glisser.');

  // 1. A drag of Montant onto the start of Demandeur: the dragged header fades, the insertion line is drawn
  // on Demandeur's start edge, and the release moves the column without sorting it. Its header and its
  // cells are the same elements as before the move (keyed by column).
  await evaluate(`(() => {
    document.querySelector(${JSON.stringify(header('Amount'))}).__omniProbe = 'montant';
    document.querySelector(${JSON.stringify(`${grid} tbody tr[data-omni-row-index] td[data-omni-col="Amount"]`)}).__omniProbe = 'cellule';
  })()`);
  const amount = await pointOf(`${header('Amount')} .omni-data-grid__title`);
  const applicantStart = await pointIn('Applicant', 0.2);
  await press(amount);
  await moveTo(amount, applicantStart);
  await waitFor('la ligne d\'insertion avant Demandeur', `document.querySelector(${JSON.stringify(header('Applicant'))}).classList.contains('omni-data-grid__drop--before')`);
  const during = await evaluate(`(() => {
    const line = getComputedStyle(document.querySelector(${JSON.stringify(header('Applicant'))}), '::before');
    return { dragged: document.querySelector(${JSON.stringify(header('Amount'))}).classList.contains('omni-data-grid__dragged'), width: line.width, background: line.backgroundColor, content: line.content };
  })()`);
  check(during.dragged && during.content === '""' && parseFloat(during.width) >= 2 && during.background !== 'rgba(0, 0, 0, 0)', `Repère du glisser absent : ${JSON.stringify(during)}.`);
  await mouse('mouseReleased', applicantStart);
  await expectOrder(['Reference', 'Amount', 'Applicant', 'Country', 'Filed'], 'Montant placé avant Demandeur');
  const after = await evaluate(`(() => ({
    header: document.querySelector(${JSON.stringify(header('Amount'))}).__omniProbe ?? null,
    cell: document.querySelector(${JSON.stringify(`${grid} tbody tr[data-omni-row-index] td[data-omni-col="Amount"]`)}).__omniProbe ?? null,
    sort: document.querySelector(${JSON.stringify(header('Amount'))}).getAttribute('aria-sort'),
    marks: document.querySelectorAll(${JSON.stringify(`${grid} .omni-data-grid__drop--before, ${grid} .omni-data-grid__drop--after, ${grid} .omni-data-grid__dragged`)}).length
  }))()`);
  check(after.header === 'montant' && after.cell === 'cellule', `L'en-tête ou la cellule déplacés ont été recréés : ${JSON.stringify(after)}.`);
  check(after.sort === null && after.marks === 0, `Le lâcher a trié la colonne ou laissé un repère : ${JSON.stringify(after)}.`);
  const amountTitle = await evaluate(titleOf('Amount'));
  await waitFor('l\'annonce du déplacement', `${announced}.includes(${JSON.stringify(amountTitle)}) && ${announced}.includes('2') && ${announced}.includes('5')`);
  await waitFor('l\'ordre enregistré', `JSON.stringify(${storedGrid}?.columnOrder ?? null) === ${JSON.stringify(JSON.stringify(['Reference', 'Amount', 'Applicant', 'Country', 'Filed']))}`);
  step(`glisser d'en-tête : Montant avant Demandeur, repère tracé, sans tri, éléments gardés, annoncé (« ${await evaluate(announced)} »)`);

  // 2. Escape cancels a drag, and a free header has no place among the frozen ones.
  const country = await pointOf(`${header('Country')} .omni-data-grid__title`);
  const filedEnd = await pointIn('Filed', 0.8);
  await press(country);
  await moveTo(country, filedEnd);
  await waitFor('la ligne d\'insertion après Dépôt', `document.querySelector(${JSON.stringify(header('Filed'))}).classList.contains('omni-data-grid__drop--after')`);
  await key('Escape', 'Escape', 27);
  check(await evaluate(`document.querySelectorAll(${JSON.stringify(`${grid} .omni-data-grid__drop--after, ${grid} .omni-data-grid__dragged`)}).length`) === 0, 'Échap a laissé les repères du glisser.');
  await mouse('mouseReleased', filedEnd);
  await pause(300);
  check(JSON.stringify(await order()) === JSON.stringify(['Reference', 'Amount', 'Applicant', 'Country', 'Filed']), `Échap n'a pas annulé le glisser : ${await order()}.`);
  check(await evaluate(`document.querySelector(${JSON.stringify(header('Filed'))}).getAttribute('aria-sort')`) === null, 'Le relâchement après Échap a trié la colonne.');
  const applicant = await pointOf(`${header('Applicant')} .omni-data-grid__title`);
  const referenceStart = await pointIn('Reference', 0.2);
  await press(applicant);
  await moveTo(applicant, referenceStart);
  check(await evaluate(`document.querySelector(${JSON.stringify(header('Reference'))}).matches('.omni-data-grid__drop--before, .omni-data-grid__drop--after')`) === false, 'Une colonne libre est proposée parmi les figées.');
  await mouse('mouseReleased', referenceStart);
  await pause(300);
  check(JSON.stringify(await order()) === JSON.stringify(['Reference', 'Amount', 'Applicant', 'Country', 'Filed']), `Une colonne libre est passée parmi les figées : ${await order()}.`);
  step('glisser annulé par Échap, colonne figée refusée à une colonne libre');

  // 3. The order survives a reload.
  await reload();
  await expectOrder(['Reference', 'Amount', 'Applicant', 'Country', 'Filed'], 'l\'ordre retrouvé après rechargement');
  step('ordre retrouvé après rechargement');

  // 4. From the keyboard: Enter opens the chooser, Enter on an arrow moves the column, and the focus stays
  // on that column's arrows, moving to the other one once the column reaches the frozen boundary.
  await evaluate(`document.querySelector(${JSON.stringify(chooserToggle)}).focus()`);
  await key('Enter', 'Enter', 13, 0, '\r');
  await waitFor('le sélecteur ouvert au clavier', `document.querySelector(${JSON.stringify(chooserPanel)}) !== null`);
  // The popover finishes opening once its script has put the focus on its first control: an arrow focused
  // before that would lose the focus to it.
  await waitFor('le focus porté dans le sélecteur ouvert au clavier', focusInPanel);
  check(await evaluate(`document.querySelector(${JSON.stringify(arrow('Reference', 1))}).disabled && document.querySelector(${JSON.stringify(arrow('Amount', -1))}).disabled`), 'Une flèche franchit la limite des colonnes figées.');
  await evaluate(`document.querySelector(${JSON.stringify(arrow('Country', -1))}).focus()`);
  await key('Enter', 'Enter', 13, 0, '\r');
  await expectOrder(['Reference', 'Amount', 'Country', 'Applicant', 'Filed'], 'Pays déplacé à gauche au clavier');
  await waitFor('le focus gardé sur la flèche de Pays', `document.activeElement?.matches(${JSON.stringify(arrow('Country', -1))}) === true`);
  await key('Enter', 'Enter', 13, 0, '\r');
  await expectOrder(['Reference', 'Country', 'Amount', 'Applicant', 'Filed'], 'Pays déplacé une seconde fois');
  await waitFor('le focus passé à l\'autre flèche de Pays', `document.activeElement?.matches(${JSON.stringify(arrow('Country', 1))}) === true`);
  const countryTitle = await evaluate(titleOf('Country'));
  await waitFor('l\'annonce du déplacement au clavier', `${announced}.includes(${JSON.stringify(countryTitle)}) && ${announced}.includes('2')`);
  step('déplacement au clavier depuis le sélecteur, focus gardé sur la colonne, limite figée respectée');

  // 5. Hiding from the chooser: the box of Dépôt hides the column, the reference cannot be hidden.
  check(await evaluate(`document.querySelector(${JSON.stringify(`${chooserPanel} [data-omni-chooser-col="Reference"] input[type="checkbox"]`)}).disabled`), 'La référence (Hideable="false") peut être masquée.');
  await clickOn(`${chooserPanel} [data-omni-chooser-col="Filed"] label`);
  await expectOrder(['Reference', 'Country', 'Amount', 'Applicant'], 'Dépôt masqué');
  check(await evaluate(`document.querySelectorAll(${JSON.stringify(`${grid} [data-omni-col="Filed"]:is(th, td, col)`)}).length`) === 0, 'Dépôt reste dans le tableau.');
  await waitFor('le masquage enregistré', `${storedGrid}?.columnVisibility?.Filed === false`);
  await waitFor('l\'annonce du masquage', `${announced}.length > 0`);
  await key('Escape', 'Escape', 27);
  await waitFor('le sélecteur refermé', `document.querySelector(${JSON.stringify(chooserPanel)}) === null`);
  step(`colonne masquée depuis le sélecteur (« ${await evaluate(announced)} »), référence non masquable`);

  // 6. Hidden and moved columns survive a reload.
  await reload();
  await expectOrder(['Reference', 'Country', 'Amount', 'Applicant'], 'la disposition retrouvée après rechargement');
  step('masquage et ordre retrouvés après rechargement');

  // 7. The chooser at 375 px: its panel stays on the screen.
  await send('Emulation.setDeviceMetricsOverride', { width: 375, height: 812, deviceScaleFactor: 1, mobile: true });
  await pause(400);
  await openChooser();
  await pause(300);
  const narrow = await evaluate(`(() => {
    const panel = document.querySelector(${JSON.stringify(chooserPanel)}).getBoundingClientRect();
    const rows = [...document.querySelectorAll(${JSON.stringify(`${chooserPanel} .omni-data-grid__chooser-item`)})].map(row => row.getBoundingClientRect());
    return { left: panel.left, right: panel.right, width: innerWidth, rows: rows.length, rowsInside: rows.every(row => row.left >= panel.left - 1 && row.right <= panel.right + 1) };
  })()`);
  check(narrow.left >= 0 && narrow.right <= narrow.width && narrow.rows === declared.length && narrow.rowsInside, `Sélecteur hors de l'écran à 375 px : ${JSON.stringify(narrow)}.`);
  step(`sélecteur lisible à 375 px (${Math.round(narrow.left)} à ${Math.round(narrow.right)} px)`);

  // 8. The reset brings the declared layout back and keeps it saved.
  await clickOn(`${chooserPanel} .omni-data-grid__chooser-reset`);
  await expectOrder(declared, 'la disposition déclarée rétablie');
  await waitFor('la disposition enregistrée sans ordre', `(${storedGrid}) !== null && ${storedGrid}.columnOrder === null && ${storedGrid}.columnVisibility === null`);
  await waitFor('l\'annonce de la réinitialisation', `${announced}.length > 0`);
  await key('Escape', 'Escape', 27);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await reload();
  await expectOrder(declared, 'la disposition déclarée après rechargement');
  await cspClean('après la réinitialisation');
  step('réinitialisation : disposition déclarée rétablie et gardée après rechargement');

  // The grid's preference removed for whatever follows; leaving the page detaches the drag.
  await evaluate(clearGrid);
  await session.visit('/components/legacy/badges', '.omni-badge');
}
