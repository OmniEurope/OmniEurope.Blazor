// The requests of a client application of 2026-10-08 (PLAN-018) that only a browser shows: a title the
// package tooltip takes away still names an icon-only button while it is focused or hovered, and the
// skip link moves the focus to the main content by script, without leaving the page.
import { probeOptions } from '../ShowcaseCdp.mjs';

export async function requestSteps(session, results) {
  const { evaluate, waitFor, key, check, hover, mouse, pause, pointOf, click, clickOn, send, type } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const tab = () => key('Tab', 'Tab', 9);

  // 1. Title tooltips (OmniTitleTooltips of the Retours page): an icon-only button the title alone names
  // lends it to aria-label while the title is away, a button named by its text gets nothing.
  await session.visit('/composants/retours', '.omni-badge');
  // The pointer of the previous steps rests over the page: content moving under it raises a pointerover,
  // and a titled element reached that way rightly takes the title tooltip from the focused button. It is
  // parked on the header first, where nothing carries a title.
  const parked = await pointOf('.omni-header');
  await mouse('mouseMoved', { x: parked.x, y: parked.y }, { button: 'none' });
  check(await evaluate(`!document.elementFromPoint(${parked.x}, ${parked.y})?.closest('[title], [data-omni-title]')`), 'Le pointeur garé est sur un élément titré.');
  // OmniTitleTooltips installs its listeners after its first render, a script round trip that the badge
  // above does not wait for: a focus made before then keeps its title and the steps below would race
  // it. Waited for by its effect on a titled element of its own, focused again until the title moves.
  await evaluate(`(() => {
    const ready = document.createElement('button');
    ready.id = 'probe-title-ready';
    ready.title = 'Prêt';
    document.querySelector('main').append(ready);
  })()`);
  // The title tooltip follows a keyboard focus only (:focus-visible); after the pointer moves above, a
  // focus made by script is not one, so a key is pressed first, as a keyboard user would.
  await key('Shift', 'ShiftLeft', 16);
  await waitFor('les infobulles de titre installées', "(() => { const ready = document.getElementById('probe-title-ready'); ready.blur(); ready.focus(); return !ready.hasAttribute('title'); })()");
  await evaluate("(() => { const ready = document.getElementById('probe-title-ready'); ready.blur(); ready.remove(); })()");
  await evaluate(`(() => {
    const before = document.createElement('button');
    before.id = 'probe-title-before';
    before.textContent = 'Avant';
    const icon = document.createElement('button');
    icon.id = 'probe-title-icon';
    icon.title = 'Imprimer';
    icon.innerHTML = '<svg aria-hidden="true" width="16" height="16"></svg>';
    const text = document.createElement('button');
    text.id = 'probe-title-text';
    text.title = 'Une précision';
    text.textContent = 'Envoyer';
    document.querySelector('main').append(before, icon, text);
    before.focus();
  })()`);
  // The page may still render (a demo settling) and take the focus back: Tab leaves from « Avant » only.
  await waitFor('le bouton « Avant » focalisé', "document.activeElement?.id === 'probe-title-before' || (document.getElementById('probe-title-before').focus(), false)");
  await tab();
  await waitFor('le bouton à icône focalisé, nommé par son titre', "document.activeElement?.id === 'probe-title-icon' && document.activeElement.getAttribute('aria-label') === 'Imprimer' && !document.activeElement.hasAttribute('title')");
  await tab();
  await waitFor('le titre rendu au bouton à icône quitté', "(() => { const icon = document.getElementById('probe-title-icon'); return icon.getAttribute('title') === 'Imprimer' && !icon.hasAttribute('aria-label'); })()");
  const textState = "JSON.stringify({ id: document.activeElement?.id, label: document.activeElement?.getAttribute('aria-label'), title: document.activeElement?.getAttribute('title') })";
  await waitFor('le bouton à texte focalisé, sans aria-label, son titre en infobulle', `(${textState}) === ${JSON.stringify(JSON.stringify({ id: 'probe-title-text', label: null, title: null }))}`);
  await hover('#probe-title-icon');
  await waitFor('le bouton à icône survolé, encore nommé', "document.getElementById('probe-title-icon').getAttribute('aria-label') === 'Imprimer'");
  await mouse('mouseMoved', { x: 5, y: 5 }, { button: 'none' });
  await waitFor('le titre rendu après le survol', "(() => { const icon = document.getElementById('probe-title-icon'); return icon.getAttribute('title') === 'Imprimer' && !icon.hasAttribute('aria-label'); })()");
  await evaluate("['probe-title-before', 'probe-title-icon', 'probe-title-text'].forEach(id => document.getElementById(id).remove())");
  step('infobulle de titre : le bouton à icône garde son nom, focalisé et survolé');

  // A chart point named by an SVG <title> child (a marker, a masked data label) takes the package
  // tooltip too: hovered, its title node is set aside and its text shown; left, the node goes back.
  await evaluate(`(() => {
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.id = 'probe-title-svg';
    svg.setAttribute('viewBox', '0 0 40 40');
    svg.setAttribute('width', '40');
    svg.setAttribute('height', '40');
    const point = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
    point.setAttribute('cx', '20'); point.setAttribute('cy', '20'); point.setAttribute('r', '18');
    const title = document.createElementNS('http://www.w3.org/2000/svg', 'title');
    title.textContent = '46 525,85 €';
    point.append(title);
    svg.append(point);
    document.querySelector('main').append(svg);
  })()`);
  await hover('#probe-title-svg circle');
  await waitFor('le point du graphique survolé, son titre écarté et montré par le paquet', "(() => { const point = document.querySelector('#probe-title-svg circle'); const box = document.querySelector('.omni-title-tooltip--visible'); return !point.querySelector('title') && point.getAttribute('data-omni-title') === '46 525,85 €' && box?.textContent.includes('46 525,85 €'); })()");
  await mouse('mouseMoved', { x: parked.x, y: parked.y }, { button: 'none' });
  await waitFor('le titre rendu au point quitté', "document.querySelector('#probe-title-svg circle title')?.textContent === '46 525,85 €'");
  await evaluate("document.getElementById('probe-title-svg').remove()");
  step("infobulle de titre : un point de graphique nommé par un titre SVG prend l'infobulle du paquet");

  // 2. Skip link of the site's layout: the first Tab stop; Enter focuses the main landmark, the address
  // unchanged.
  await session.visit('/composants/grille', '#demo-grid-tree');
  const address = await evaluate('location.href');
  await evaluate("document.activeElement?.blur(); window.scrollTo(0, 0); document.body.focus()");
  await tab();
  await waitFor('le lien d\'évitement, premier arrêt, visible au focus', "document.activeElement?.classList.contains('omni-skip-link') && document.activeElement.getBoundingClientRect().top >= 0");
  await key('Enter', 'Enter', 13, 0, '\r');
  await waitFor('le contenu principal focalisé', "document.activeElement?.tagName === 'MAIN'");
  check(await evaluate(`location.href === ${JSON.stringify(address)}`), 'Le lien d\'évitement a changé l\'adresse de la page.');
  step('lien d\'évitement : premier arrêt, Entrée donne le focus au contenu, adresse inchangée');

  // 3. The editor a row takes the focus in (recette R1-5, the grid script's focusEditor): a double click
  // on the Country cell of the second file puts the row in edit mode and focuses that cell's field, its
  // text selected whole; the pencil of the third file focuses its first field, Applicant.
  const edit = '#demo-grid-edit';
  const editorState = row => `(() => { const field = document.activeElement; const cell = field?.closest('td[data-omni-col]'); return JSON.stringify({ row: field?.closest('tr')?.dataset.omniRowIndex, column: cell?.dataset.omniCol, whole: field?.selectionStart === 0 && field?.selectionEnd === field?.value.length && field.value.length > 0 }); })() === ${JSON.stringify(JSON.stringify({ row: String(row.index), column: row.column, whole: true }))}`;
  const country = await pointOf(`${edit} tbody tr[data-omni-row-index="1"] td[data-omni-col="Country"]`);
  await mouse('mouseMoved', country, { button: 'none' });
  await mouse('mousePressed', country, { clickCount: 1 });
  await mouse('mouseReleased', country, { clickCount: 1 });
  await mouse('mousePressed', country, { clickCount: 2 });
  await mouse('mouseReleased', country, { clickCount: 2 });
  await waitFor('le pays de la ligne double-cliquée focalisé, son texte sélectionné', editorState({ index: 1, column: 'Country' }));
  await clickOn(`${edit} tbody tr[data-omni-row-index="2"] .omni-data-grid__actions button`);
  await waitFor('le premier champ de la ligne ouverte au crayon focalisé, son texte sélectionné', editorState({ index: 2, column: 'Applicant' }));
  step('édition de ligne : le champ de la cellule double-cliquée, sinon le premier, prend le focus, texte sélectionné');

  // 3b. EditTrigger Cell (#demo-grid-edit-cell, row 0 D-2001, row 1 D-2002 whose Country is empty, row 2
  // D-2003): a single click anywhere in a cell with an EditTemplate opens its row and focuses that cell's
  // field, the empty one included; the Details button inside the Country cell keeps its click and opens
  // nothing; a cell with no EditTemplate (Reference) opens nothing; on the row already in edit a click in
  // another cell only moves the focus. Tab reaches each cell through its button, the focus drawn on the
  // cell, and Enter, then Space, start the edit on that cell's field.
  const cellGrid = '#demo-grid-edit-cell';
  const rowOf = index => `${cellGrid} tbody tr[data-omni-row-index="${index}"]`;
  const cellOf = (index, column) => `${rowOf(index)} td[data-omni-col="${column}"]`;
  const isEditing = index => `(document.querySelector(${JSON.stringify(rowOf(index))})?.classList.contains('omni-data-grid__row--editing') === true)`;
  const editingRows = () => evaluate(`document.querySelectorAll(${JSON.stringify(`${cellGrid} tbody tr.omni-data-grid__row--editing`)}).length`);
  // The field holding the focus: its row, its column and, unless the value is empty, its text selected whole.
  const fieldFocus = (index, column, whole = true) => `(() => { const field = document.activeElement; const owner = field?.closest('td[data-omni-col]'); return field?.tagName === 'INPUT' && field.closest('tr')?.dataset.omniRowIndex === '${index}' && owner?.dataset.omniCol === '${column}' && (${!whole} || (field.value.length > 0 && field.selectionStart === 0 && field.selectionEnd === field.value.length)); })()`;
  // A point in the padding at the end of a cell, away from anything the cell holds.
  const paddingOf = async selector => {
    await pointOf(selector);
    return evaluate(`(() => { const box = document.querySelector(${JSON.stringify(selector)}).getBoundingClientRect(); return { x: Math.round(box.right - 4), y: Math.round(box.top + box.height / 2) }; })()`);
  };
  await evaluate(`document.querySelector(${JSON.stringify(cellGrid)}).scrollIntoView({ block: 'center' })`);
  await waitFor('la grille à édition par cellule', `document.querySelectorAll(${JSON.stringify(`${cellGrid} tbody td.omni-data-grid__cell--edit-trigger`)}).length === 6`);
  check(await evaluate(`document.querySelectorAll(${JSON.stringify(`${cellGrid} tbody td[data-omni-col="Reference"].omni-data-grid__cell--edit-trigger`)}).length`) === 0, 'Une cellule sans EditTemplate est offerte à l\'édition.');
  await click(await paddingOf(cellOf(1, 'Country')));
  await waitFor('la ligne de la cellule Pays vide ouverte par un clic dans la cellule', isEditing(1));
  await waitFor('le champ Pays vide focalisé', fieldFocus(1, 'Country', false));
  await clickOn(`${cellOf(0, 'Country')} .demo-grid-edit-cell-details`);
  await waitFor('le clic du bouton Détails compté', "document.getElementById('demo-grid-edit-cell-clicks').textContent.includes('1')");
  await pause(500);
  check(!(await evaluate(isEditing(0))) && await evaluate(isEditing(1)), 'Un clic sur le bouton de la cellule a ouvert sa ligne.');
  await clickOn(cellOf(2, 'Applicant'));
  await waitFor('la ligne du clic ouverte, la précédente refermée', `${isEditing(2)} && !${isEditing(1)}`);
  await waitFor('le champ Demandeur de la cellule cliquée focalisé, son texte sélectionné', fieldFocus(2, 'Applicant'));
  await click(await paddingOf(cellOf(2, 'Country')));
  await waitFor('le focus déplacé vers le champ Pays de la ligne déjà ouverte', fieldFocus(2, 'Country', false));
  check(await editingRows() === 1 && await evaluate(isEditing(2)), 'Le clic sur la ligne déjà ouverte a changé les lignes en édition.');
  await clickOn(cellOf(0, 'Reference'));
  await pause(500);
  check(!(await evaluate(isEditing(0))) && await evaluate(isEditing(2)), 'Une cellule sans EditTemplate a ouvert sa ligne.');
  await clickOn(`${rowOf(2)} .omni-data-grid__action--cancel`);
  await waitFor('la ligne annulée', `!${isEditing(2)}`);
  // From the last stop of the header (the resize handle of the last column, keyboard-reachable), Tab
  // enters the first row.
  await evaluate(`[...document.querySelectorAll(${JSON.stringify(`${cellGrid} thead th[data-omni-col] :is(button, summary, [tabindex="0"])`)})].filter(element => element.getClientRects().length > 0).at(-1).focus()`);
  await tab();
  await waitFor('Tab : le bouton de la cellule Demandeur de la première ligne', "document.activeElement?.classList.contains('omni-data-grid__edit-cell') && document.activeElement.closest('td')?.dataset.omniCol === 'Applicant' && document.activeElement.closest('tr')?.dataset.omniRowIndex === '0'");
  check(await evaluate("getComputedStyle(document.activeElement.closest('td')).boxShadow !== 'none'"), 'Le focus du bouton de la cellule n\'est pas dessiné sur la cellule.');
  await key('Enter', 'Enter', 13, 0, '\r');
  await waitFor('Entrée : la ligne ouverte, le champ Demandeur focalisé, son texte sélectionné', `${isEditing(0)} && ${fieldFocus(0, 'Applicant')}`);
  await clickOn(`${rowOf(0)} .omni-data-grid__action--cancel`);
  await waitFor('la ligne annulée', `!${isEditing(0)}`);
  await evaluate(`document.querySelector(${JSON.stringify(`${cellOf(1, 'Country')} .omni-data-grid__edit-cell`)}).focus()`);
  await key(' ', 'Space', 32, 0, ' ');
  await waitFor('Espace : la ligne ouverte, le champ Pays vide focalisé', `${isEditing(1)} && ${fieldFocus(1, 'Country', false)}`);
  await key('Escape', 'Escape', 27);
  await waitFor('Échap : la ligne annulée', `!${isEditing(1)}`);
  step('EditTrigger Cell : un clic dans une cellule (vide comprise) ouvre sa ligne sur son champ, le bouton de la cellule et une cellule sans EditTemplate n\'ouvrent rien, Tab, Entrée et Espace éditent, le clic sur une ligne ouverte déplace le focus');

  // 4. The initial focus of a dialog (recette R1-10, InitialFocus Panel on the intent dialogs): the panel
  // itself holds the focus as it opens, no control of the dialog is focused and nothing draws a focus
  // ring; the first Tab reaches the first focusable element (the close button), Escape gives the focus
  // back to the trigger.
  await session.visit('/composants/superpositions', '#demo-dialog-intent-accent');
  await clickOn('#demo-dialog-intent-accent');
  await waitFor("le dialogue d'intention ouvert, le panneau focalisé", "document.activeElement?.matches('.omni-overlay section.omni-dialog') === true");
  // The panel focused looks exactly as it does unfocused (outline and shadow, the library's rings being
  // drawn as box shadows), and no control inside it holds the focus.
  const ring = await evaluate("(() => { const panel = document.activeElement; const look = () => { const style = getComputedStyle(panel); return style.outlineStyle + '|' + style.outlineWidth + '|' + style.boxShadow; }; const focused = look(); const nested = !!panel.querySelector(':focus'); panel.blur(); const rest = look(); panel.focus({ preventScroll: true }); return JSON.stringify({ same: focused === rest, nested, back: panel.matches(':focus') }); })()");
  check(ring === JSON.stringify({ same: true, nested: false, back: true }), `Le panneau focalisé montre un anneau ou un contrôle a le focus : ${ring}`);
  await tab();
  await waitFor('le premier Tab sur le bouton Fermer', "document.activeElement?.classList.contains('omni-dialog__close') === true");
  await key('Escape', 'Escape', 27);
  await waitFor('le focus rendu au déclencheur', "document.activeElement?.id === 'demo-dialog-intent-accent' && document.querySelector('.omni-overlay section.omni-dialog') === null");
  step('focus initial Panel : le panneau focalisé sans anneau, Tab mène au bouton Fermer, Échap rend le focus');

  // 5. The chevron of a filterable drop-down (recette R1-11): drawn at the end of the field, a press opens
  // the whole list unfiltered and leaves the focus in the field, a second press closes it.
  await session.visit('/composants/selection', '#demo-country');
  const options = "document.querySelectorAll('#demo-country-results [role=option]').length";
  check(await evaluate("document.querySelector('#demo-country').closest('.omni-autocomplete') !== null"), "La liste des pays n'est pas un champ filtrable.");
  const chevron = await evaluate("(() => { const toggle = document.querySelector('#demo-country ~ .omni-autocomplete__toggle'); const field = document.getElementById('demo-country').getBoundingClientRect(); const box = toggle?.getBoundingClientRect(); return !!toggle && box.right <= field.right + 0.5 && box.top >= field.top - 0.5 && box.bottom <= field.bottom + 0.5 && getComputedStyle(toggle, '::before').inlineSize !== 'auto'; })()");
  check(chevron, 'Le chevron de la liste filtrable manque ou sort du champ.');
  await clickOn('#demo-country ~ .omni-autocomplete__toggle');
  await waitFor('la liste complète ouverte au chevron, le focus dans le champ', `${options} > 1 && document.activeElement?.id === 'demo-country'`);
  await clickOn('#demo-country ~ .omni-autocomplete__toggle');
  await waitFor('la liste refermée au second appui', `${options} === 0`);
  step('liste filtrable : le chevron ouvre la liste complète puis la referme, le focus reste dans le champ');

  // 6. Charts of a client's recette (R1-12, R1-13, R1-16), on the range demo widened to its 36 months:
  // no two value labels drawn overlap and a masked one is read on hover, the navigator is graduated
  // without overlap, its handles follow the density, and the chart keeps a minimum width.
  await session.visit('/composants/graphiques-avances', '.omni-range-navigator__ticks text');
  await evaluate("(() => { const start = document.querySelector('.omni-range-navigator__handle--start'); start.value = 0; start.dispatchEvent(new Event('input', { bubbles: true })); })()");
  await waitFor('les 36 mois affichés', "document.querySelectorAll('.omni-range-navigator__column--selected').length === 36");
  const charts = await evaluate(`(() => {
    const overlap = (a, b) => a.left < b.right && b.left < a.right && a.top < b.bottom && b.top < a.bottom;
    const figure = document.querySelector('.omni-range-navigator').closest('figure');
    const labels = [...figure.querySelectorAll('.omni-chart__labels text')].map(text => text.getBoundingClientRect()).filter(box => box.bottom > figure.getBoundingClientRect().top);
    let labelOverlaps = 0;
    for (let i = 0; i < labels.length; i++) for (let j = i + 1; j < labels.length; j++) if (overlap(labels[i], labels[j])) labelOverlaps++;
    const hits = [...figure.querySelectorAll('.omni-chart__label-hit title')].filter(title => title.textContent.trim().length > 0).length;
    const ticks = [...figure.querySelectorAll('.omni-range-navigator__ticks text')].map(text => text.getBoundingClientRect());
    let tickOverlaps = 0;
    for (let i = 1; i < ticks.length; i++) if (ticks[i].left < ticks[i - 1].right) tickOverlaps++;
    const track = figure.querySelector('.omni-range-navigator__track').getBoundingClientRect().height;
    // The expected height, the control height of the density plus 1rem, measured on a block of that size.
    const ruler = document.createElement('div');
    ruler.style.blockSize = 'calc(var(--omni-control-height) + 1rem)';
    figure.querySelector('.omni-range-navigator').append(ruler);
    const expected = ruler.getBoundingClientRect().height;
    ruler.remove();
    return JSON.stringify({ labelOverlaps, hitsOk: hits === figure.querySelectorAll('.omni-chart__label-hit').length, ticks: ticks.length, tickOverlaps, track, expected, trackOk: expected > 0 && Math.abs(track - expected) < 1, minWidth: figure.className.includes('omni-chart--min-') });
  })()`);
  const chartState = JSON.parse(charts);
  check(chartState.labelOverlaps === 0, `Des étiquettes de valeur se chevauchent : ${charts}`);
  check(chartState.hitsOk, `Une étiquette masquée n'a pas sa valeur au survol : ${charts}`);
  check(chartState.ticks > 2 && chartState.tickOverlaps === 0, `Graduations du navigateur absentes ou chevauchantes : ${charts}`);
  check(chartState.trackOk, `La hauteur du navigateur ne suit pas la densité : ${charts}`);
  check(chartState.minWidth, `Le graphique n'a pas de largeur minimale : ${charts}`);
  step(`graphiques : étiquettes sans chevauchement, ${chartState.ticks} graduations, navigateur à la densité, largeur minimale`);

  // 7. The stat tile keeps its label on one line (R1-15): the long label of the demo stays one line
  // high, smaller or cut, its whole text in the title once cut.
  await session.visit('/composants/pages', '#demo-stat-tile-gain');
  const tileState = "(() => { const label = document.querySelector('#demo-stat-tile-gain .omni-stat-tile__label'); const line = parseFloat(getComputedStyle(label).lineHeight) || parseFloat(getComputedStyle(label).fontSize) * 1.6; const fits = label.scrollWidth <= label.clientWidth + 1; const fit = label.getAttribute('data-fit'); return label.getBoundingClientRect().height <= line + 1 && (fits || fit === 'clip') && (fit === 'clip') === (label.getAttribute('title') === label.textContent.trim()); })()";
  await waitFor('le libellé long de la tuile sur une ligne, entier ou coupé avec son titre', tileState);
  step('tuile de statistique : libellé sur une ligne, réduit ou coupé avec son texte entier en titre');

  // 7b. The fact tile (OmniFactTile, request of 2026-10-23): three tiles of contents of different lengths in
  // one tile grid have the same height, and their icons, labels and values sit on the same verticals, within a
  // tile and from one tile to the next (the offset from the tile's left edge is the same: side by side the
  // tiles are not at the same x, stacked on a phone they are). Nothing overflows: the tile, its rows block and
  // each row hold their content (scrollWidth <= clientWidth), and every cell, title and row lies inside the
  // tile. A value cell is the exception to scrollWidth: its text cut by the ellipsis is the cut itself, so the
  // check on the cells is their rectangles, and the cut is proved by a value that is cut and read whole in the
  // package tooltip. At 1280 and 375 px, at the default text size and at text size 10 (the level the appearance
  // window writes on the root, data-oe-text-size).
  await session.visit('/composants/pages', '#demo-fact-tile-timeline');
  const factTiles = `(() => {
    const grid = document.getElementById('demo-fact-tile-grid');
    const tiles = [...grid.children];
    const problems = [];
    const near = (a, b) => Math.abs(a - b) <= 1;
    if (tiles.length !== 3 || !tiles.every(tile => tile.classList.contains('omni-fact-tile'))) problems.push('les trois tuiles de faits ne sont pas les enfants directs de la grille');
    const heights = tiles.map(tile => tile.getBoundingClientRect().height);
    if (Math.max(...heights) - Math.min(...heights) > 1) problems.push('hauteurs inégales ' + heights.map(height => height.toFixed(1)).join(' / '));
    const columns = { icon: [], label: [], value: [] };
    for (const tile of tiles) {
      const frame = tile.getBoundingClientRect();
      const inside = (element, name) => {
        const box = element.getBoundingClientRect();
        if (box.left < frame.left - 1 || box.right > frame.right + 1 || box.top < frame.top - 1 || box.bottom > frame.bottom + 1) problems.push(name + ' sort de sa tuile ' + tile.id);
      };
      for (const holder of [tile, tile.querySelector('.omni-fact-tile__rows'), ...tile.querySelectorAll('.omni-fact-tile__row')]) {
        if (holder.scrollWidth > holder.clientWidth + 1) problems.push(holder.className + ' déborde (' + holder.scrollWidth + ' > ' + holder.clientWidth + ') dans ' + tile.id);
      }
      inside(tile.querySelector('.omni-fact-tile__title'), 'le titre');
      for (const row of tile.querySelectorAll('.omni-fact-tile__row')) {
        inside(row, 'une ligne');
        for (const name of ['icon', 'label', 'value']) {
          const cell = row.querySelector('.omni-fact-tile__' + name);
          inside(cell, 'la cellule ' + name);
          columns[name].push({ offset: cell.getBoundingClientRect().left - frame.left, tile: tile.id });
        }
      }
    }
    for (const [name, cells] of Object.entries(columns)) {
      if (cells.length < 6 || !cells.every(cell => near(cell.offset, cells[0].offset))) problems.push('les cellules ' + name + ' ne sont pas sur une même verticale : ' + cells.map(cell => cell.offset.toFixed(1)).join(' / '));
    }
    const cut = [...grid.querySelectorAll('.omni-fact-tile__value')].filter(value => value.scrollWidth > value.clientWidth + 1).length;
    return JSON.stringify({ problems, cut, heights, columns: Object.fromEntries(Object.entries(columns).map(([name, cells]) => [name, cells.map(cell => Math.round(cell.offset * 10) / 10)])) });
  })()`;
  const textSizeBefore = await evaluate("document.documentElement.dataset.oeTextSize ?? ''");
  for (const width of [1280, 375]) {
    await send('Emulation.setDeviceMetricsOverride', { width, height: width < 600 ? 812 : 900, deviceScaleFactor: 1, mobile: width < 600 });
    await pause(300);
    for (const level of [textSizeBefore || '5', '10']) {
      await evaluate(`document.documentElement.dataset.oeTextSize = ${JSON.stringify(level)}`);
      await waitFor(`le texte au niveau ${level}`, `document.documentElement.dataset.oeTextSize === ${JSON.stringify(level)}`);
      await pause(200);
      const measured = JSON.parse(await evaluate(factTiles));
      check(measured.problems.length === 0, `Tuiles de faits à ${width} px, texte ${level} : ${measured.problems.join(' | ')}`);
      check(measured.cut >= 1, `Aucune valeur coupée à ${width} px, texte ${level} : la coupe par points de suspension n'est pas exercée (${JSON.stringify(measured)}).`);
    }
  }
  if (textSizeBefore === '') await evaluate('delete document.documentElement.dataset.oeTextSize');
  else await evaluate(`document.documentElement.dataset.oeTextSize = ${JSON.stringify(textSizeBefore)}`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);
  // A cut value is read whole in the package tooltip. The tile turns the tooltips on after its first render, a
  // script round trip: the pointer leaves and comes back until the box shows.
  const longValue = '2026-10-23 08:21:07 +02:00 (Europe/Paris)';
  const longValueCut = await evaluate("(() => { const value = document.querySelector('#demo-fact-tile-timeline .omni-fact-tile__value'); return value.scrollWidth > value.clientWidth + 1 && value.textContent.trim(); })()");
  check(longValueCut === longValue, `La valeur longue de la tuile n'est pas coupée à 1280 px : ${longValueCut}`);
  const tipShows = "document.querySelector('.omni-title-tooltip.omni-title-tooltip--visible')?.textContent.trim()";
  let tipText = '';
  for (let attempt = 0; attempt < 10 && tipText !== longValue; attempt++) {
    await mouse('mouseMoved', { x: 5, y: 5 }, { button: 'none' });
    await pause(150);
    await hover('#demo-fact-tile-timeline .omni-fact-tile__value');
    await pause(900);
    tipText = (await evaluate(tipShows)) ?? '';
  }
  check(tipText === longValue, `La valeur coupée n'est pas lue en entier dans l'infobulle : « ${tipText} ».`);
  await mouse('mouseMoved', { x: 5, y: 5 }, { button: 'none' });
  step('tuiles de faits : même hauteur, icônes, libellés et valeurs sur les mêmes verticales, rien ne déborde à 1280 et 375 px, texte par défaut et 10, valeur coupée lue en bulle');

  // 7c. The fact tile in a narrow tile (PLAN-004 lot L, request of 2026-10-23): four tiles on one row of a tile
  // grid (Columns 4) hold a label of 16 characters and a value of 20, and a tile of a quarter of 900 px is too
  // narrow for the three aligned columns to leave the value its room. The rows then go on two levels (the
  // label above the value, a container query on the tile, never the viewport) and nothing is cut, no ellipsis
  // active, label or value. The window is widened until the grid itself is 900 px wide (the showcase shell
  // keeps a sidebar and a side panel, so that is wider than 900 px of window, and below 1024 px of window the
  // tile grid has two columns). At 1280 px the tile is narrower still and the value, on its own line, still
  // fits. In a roomy tile (a phone, one column) the three aligned columns stay, label and value on one line.
  await session.visit('/composants/pages', '#demo-fact-tile-narrow-grid');
  const narrowGrid = `(() => {
    const grid = document.getElementById('demo-fact-tile-narrow-grid');
    const tiles = [...grid.children];
    const problems = [];
    const near = (a, b) => Math.abs(a - b) <= 1;
    const columns = getComputedStyle(grid).gridTemplateColumns.split(' ').length;
    const offsets = { icon: [], label: [], value: [] };
    let twoLevels = 0;
    for (const tile of tiles) {
      const frame = tile.getBoundingClientRect();
      for (const holder of [tile, tile.querySelector('.omni-fact-tile__rows'), ...tile.querySelectorAll('.omni-fact-tile__row')]) {
        if (holder.scrollWidth > holder.clientWidth + 1) problems.push(holder.className + ' déborde dans ' + tile.id);
      }
      for (const row of tile.querySelectorAll('.omni-fact-tile__row')) {
        const cells = Object.fromEntries(['icon', 'label', 'value'].map(name => [name, row.querySelector('.omni-fact-tile__' + name)]));
        for (const name of ['icon', 'label', 'value']) offsets[name].push(cells[name].getBoundingClientRect().left - frame.left);
        if (cells.value.getBoundingClientRect().top >= cells.label.getBoundingClientRect().bottom - 1) twoLevels++;
      }
      for (const name of ['label', 'value']) {
        const cell = tile.querySelector('.omni-fact-tile__' + name);
        const text = cell.textContent.trim();
        if (text.length !== (name === 'label' ? 16 : 20)) problems.push('le ' + name + ' de ' + tile.id + ' n\\'a pas la longueur voulue : « ' + text + ' »');
        if (cell.scrollWidth > cell.clientWidth + 1) problems.push('le ' + name + ' « ' + text + ' » est coupé dans ' + tile.id + ' (' + cell.scrollWidth + ' > ' + cell.clientWidth + ', points de suspension ' + getComputedStyle(cell).textOverflow + ')');
      }
    }
    for (const [name, cells] of Object.entries(offsets)) {
      if (!cells.every(cell => near(cell, cells[0]))) problems.push('les cellules ' + name + ' ne sont pas sur une même verticale : ' + cells.map(cell => cell.toFixed(1)).join(' / '));
    }
    return JSON.stringify({ problems, columns, tiles: tiles.length, rows: tiles.length * 2, twoLevels, gridWidth: Math.round(grid.getBoundingClientRect().width), tileWidth: Math.round(tiles[0].getBoundingClientRect().width) });
  })()`;
  let windowWidth = 1000;
  for (; windowWidth <= 1800; windowWidth += 5) {
    await send('Emulation.setDeviceMetricsOverride', { width: windowWidth, height: 900, deviceScaleFactor: 1, mobile: false });
    await pause(60);
    if (await evaluate("document.getElementById('demo-fact-tile-narrow-grid').getBoundingClientRect().width >= 900")) break;
  }
  await pause(300);
  const atNinehundred = JSON.parse(await evaluate(narrowGrid));
  check(atNinehundred.columns === 4 && atNinehundred.tiles === 4, `La grille de tuiles étroites n'a pas quatre colonnes de quatre tuiles : ${JSON.stringify(atNinehundred)}`);
  check(atNinehundred.problems.length === 0, `Tuiles de faits étroites (grille de ${atNinehundred.gridWidth} px, fenêtre de ${windowWidth} px, tuile de ${atNinehundred.tileWidth} px) : ${atNinehundred.problems.join(' | ')}`);
  check(atNinehundred.twoLevels === atNinehundred.rows, `Les lignes ne sont pas sur deux niveaux dans une tuile de ${atNinehundred.tileWidth} px : ${atNinehundred.twoLevels} sur ${atNinehundred.rows}.`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);
  const atWindow1280 = JSON.parse(await evaluate(narrowGrid));
  check(atWindow1280.problems.length === 0, `Tuiles de faits étroites à 1280 px (tuile de ${atWindow1280.tileWidth} px) : ${atWindow1280.problems.join(' | ')}`);
  await send('Emulation.setDeviceMetricsOverride', { width: 375, height: 812, deviceScaleFactor: 1, mobile: true });
  await pause(300);
  const atPhone = JSON.parse(await evaluate(narrowGrid));
  check(atPhone.problems.length === 0, `Tuiles de faits étroites à 375 px (tuile de ${atPhone.tileWidth} px) : ${atPhone.problems.join(' | ')}`);
  check(atPhone.columns === 1 && atPhone.twoLevels === 0, `Dans une tuile large (${atPhone.tileWidth} px) les lignes doivent rester sur un niveau, trois colonnes : ${JSON.stringify(atPhone)}`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);
  step(`tuiles de faits étroites : quatre tuiles sur une grille de 900 px (fenêtre de ${windowWidth} px), libellé de 16 et valeur de 20 caractères entiers sur deux niveaux, sans points de suspension ; trois colonnes gardées dans une tuile large`);

  // 7d. Columns sized to their content (PLAN-004 lot M, request of 2026-10-23): a grid with
  // ColumnSizing Content, a name, a status badge, a number, a project and a date, each column with the width of
  // its widest content. In a box of 900, 1300 and 600 px: no header on two lines, no cell or title cut (no
  // active ellipsis), the status column no wider than max(badge, header with its icons) plus the cell padding
  // (a declared width of 9rem, which the default mode would keep, is wider and fails this), the columns that
  // ask to grow (name, project) take the room that is left, the others stay at their content, no horizontal
  // scroll while the box holds the content, and below it the columns stay at their content floor and the grid
  // scrolls sideways instead of cutting. A second grid wraps its long column (Wrap) instead of cutting it.
  await send('Emulation.setDeviceMetricsOverride', { width: 1800, height: 900, deviceScaleFactor: 1, mobile: false });
  await session.visit('/composants/grille', '#demo-grid-sizing');
  const sizingState = gridId => `(() => {
    const grid = document.getElementById('${gridId}');
    const viewport = grid.querySelector('.omni-data-grid__viewport');
    const heads = [...grid.querySelectorAll('thead th[data-omni-col]')];
    const problems = [];
    const lineOf = element => parseFloat(getComputedStyle(element).lineHeight) || parseFloat(getComputedStyle(element).fontSize) * 1.5;
    const headers = heads.map(head => {
      const header = head.querySelector('.omni-data-grid__header');
      const title = head.querySelector('.omni-data-grid__title');
      if (title.getBoundingClientRect().height > lineOf(title) * 1.5) problems.push('l\\'en-tête « ' + title.textContent + ' » passe sur deux lignes');
      if (title.scrollWidth > title.clientWidth + 1) problems.push('l\\'en-tête « ' + title.textContent + ' » est coupé (' + title.scrollWidth + ' > ' + title.clientWidth + ')');
      if (header.scrollWidth > header.clientWidth + 1) problems.push('le bloc d\\'en-tête « ' + title.textContent + ' » déborde');
      const range = document.createRange();
      range.selectNodeContents(title);
      const gap = parseFloat(getComputedStyle(header).columnGap) || 0;
      const others = [...header.children].filter(child => !child.contains(title) && getComputedStyle(child).display !== 'none');
      const style = getComputedStyle(head);
      return range.getBoundingClientRect().width + others.reduce((sum, child) => sum + child.getBoundingClientRect().width, 0) + gap * others.length + parseFloat(style.paddingLeft) + parseFloat(style.paddingRight);
    });
    for (const cell of grid.querySelectorAll('tbody tr:not(.omni-data-grid__spacer) td')) {
      if (cell.scrollWidth > cell.clientWidth + 1) problems.push('la cellule « ' + cell.textContent.trim() + ' » est coupée (' + cell.scrollWidth + ' > ' + cell.clientWidth + ', points de suspension ' + getComputedStyle(cell).textOverflow + ')');
    }
    const badge = grid.querySelector('tbody td .omni-badge');
    const badgeCell = badge?.closest('td');
    const badges = [...grid.querySelectorAll('tbody td .omni-badge')].map(item => item.getBoundingClientRect().width);
    const cellStyle = badgeCell ? getComputedStyle(badgeCell) : null;
    return JSON.stringify({
      problems,
      widths: heads.map(head => Math.round(head.getBoundingClientRect().width * 10) / 10),
      headers,
      badge: cellStyle ? Math.max(...badges) + parseFloat(cellStyle.paddingLeft) + parseFloat(cellStyle.paddingRight) : 0,
      client: viewport.clientWidth,
      scroll: viewport.scrollWidth,
      table: grid.querySelector('table').getBoundingClientRect().width
    });
  })()`;
  const sizeBox = async (gridId, width) => {
    await evaluate(`document.getElementById('${gridId}').closest('.demo-grid-sizing-box').style.inlineSize = '${width}px'`);
    // The columns are measured and shared out after the box resizes: settled once three samples agree.
    let last = '';
    let same = 0;
    for (let attempt = 0; attempt < 60 && same < 3; attempt++) {
      await pause(100);
      const now = await evaluate(sizingState(gridId));
      same = now === last ? same + 1 : 0;
      last = now;
    }
    check(same >= 3, `La grille ${gridId} ne se stabilise pas dans une boîte de ${width} px : ${last}`);
    return JSON.parse(last);
  };
  const sizing = {};
  for (const width of [1300, 900, 600]) {
    sizing[width] = await sizeBox('demo-grid-sizing', width);
    const state = sizing[width];
    check(state.problems.length === 0, `Colonnes au contenu dans une boîte de ${width} px : ${state.problems.join(' | ')}`);
    // Status is the second column: its width is at most the wider of its badge cell and its header.
    check(state.widths[1] <= Math.max(state.badge, state.headers[1]) + 3, `La colonne État fait ${state.widths[1]} px dans une boîte de ${width} px, plus que son contenu (pastille ${state.badge.toFixed(1)}, en-tête ${state.headers[1].toFixed(1)}).`);
  }
  const wide = sizing[1300];
  const narrow = sizing[600];
  check(wide.scroll <= wide.client + 1, `La grille défile de côté dans une boîte de 1300 px : ${wide.scroll} > ${wide.client}.`);
  check(Math.abs(wide.widths.reduce((sum, item) => sum + item, 0) - wide.client) <= 2, `Les colonnes ne se partagent pas la largeur de 1300 px : ${wide.widths.join(' + ')} pour ${wide.client}.`);
  check(narrow.scroll > narrow.client + 1, `La grille ne défile pas de côté dans une boîte de 600 px : ${narrow.scroll} contre ${narrow.client}.`);
  // The columns that grow (name, project: indexes 0 and 3) take the room, the others keep their content.
  for (const index of [0, 3]) check(wide.widths[index] > narrow.widths[index] + 20, `La colonne ${index} qui grandit reste à ${wide.widths[index]} px (contenu ${narrow.widths[index]} px) dans une boîte de 1300 px.`);
  for (const index of [1, 2, 4]) check(Math.abs(wide.widths[index] - narrow.widths[index]) <= 2, `La colonne ${index} grandit sans l'avoir demandé : ${narrow.widths[index]} px puis ${wide.widths[index]} px.`);
  // Shrinking before the scroll: between the content floor and the wide box, the columns follow the box and
  // the grid stays still sideways; the floor itself is the sum of the contents.
  const floor = narrow.widths.reduce((sum, item) => sum + item, 0);
  const between = await sizeBox('demo-grid-sizing', Math.round(floor) + 60);
  check(between.problems.length === 0, `Colonnes au contenu dans une boîte à ${Math.round(floor) + 60} px : ${between.problems.join(' | ')}`);
  check(between.scroll <= between.client + 1, `La grille défile de côté alors que le contenu tient (${between.scroll} > ${between.client}, plancher de ${floor.toFixed(0)} px).`);
  check(between.widths[0] < wide.widths[0] - 20 && between.widths[0] >= narrow.widths[0] - 1, `La colonne du nom ne suit pas la boîte : ${narrow.widths[0]} px au plancher, ${between.widths[0]} px à ${Math.round(floor) + 60} px, ${wide.widths[0]} px à 1300 px.`);
  // A long column that asks to wrap: wrapped when the box is narrow, nothing cut, headers on one line.
  const wrapped = await sizeBox('demo-grid-sizing-wrap', 500);
  check(wrapped.problems.length === 0, `Colonne à retour à la ligne dans une boîte de 500 px : ${wrapped.problems.join(' | ')}`);
  const wrapLines = await evaluate("(() => { const cell = document.querySelector('#demo-grid-sizing-wrap tbody td:nth-child(2)'); const line = parseFloat(getComputedStyle(cell).lineHeight) || parseFloat(getComputedStyle(cell).fontSize) * 1.5; return Math.round(cell.getBoundingClientRect().height / line); })()");
  check(Number(wrapLines) >= 2, `La colonne Wrap tient sur ${wrapLines} ligne dans une boîte de 500 px.`);
  // No column asks to grow: the room left is shared out between every column, each above its content.
  const sharedNarrow = await sizeBox('demo-grid-sizing-shared', 300);
  const sharedWide = await sizeBox('demo-grid-sizing-shared', 1300);
  check(sharedWide.problems.length === 0, `Colonnes au contenu sans Grow dans une boîte de 1300 px : ${sharedWide.problems.join(' | ')}`);
  check(Math.abs(sharedWide.widths.reduce((sum, item) => sum + item, 0) - sharedWide.client) <= 2, `Sans Grow, les colonnes ne se partagent pas la largeur de 1300 px : ${sharedWide.widths.join(' + ')} pour ${sharedWide.client}.`);
  const shares = sharedWide.widths.map((width, index) => width - sharedNarrow.widths[index]);
  check(shares.every(share => share > 20) && Math.max(...shares) - Math.min(...shares) <= 2, `Sans Grow, la place restante n'est pas partagée à parts égales : ${sharedNarrow.widths.join(', ')} px au contenu, ${sharedWide.widths.join(', ')} px à 1300 px.`);
  // Every column has a template and the rows arrive after the first render, the grid busy meanwhile
  // (request of 2026-10-23): the columns, sized to their headers while empty, take the width of the cells
  // that arrive, nothing cut. A box narrower than the content leaves no room to share out, so a column
  // still at its header width is seen cut (with the content observer of columns.js off, a cell is cut).
  const templatesEmpty = await sizeBox('demo-grid-sizing-templates', 300);
  await clickOn('#demo-grid-sizing-templates-load');
  await waitFor('les lignes de la grille à modèles', "document.querySelectorAll('#demo-grid-sizing-templates tbody td[data-omni-col=\"Name\"] a').length === 5");
  const templatesFull = await sizeBox('demo-grid-sizing-templates', 300);
  check(templatesFull.problems.length === 0, `Grille à modèles chargée après coup : ${templatesFull.problems.join(' | ')} (largeurs vides ${templatesEmpty.widths.join(', ')}, chargées ${templatesFull.widths.join(', ')}).`);
  const widestName = await evaluate("Math.max(...[...document.querySelectorAll('#demo-grid-sizing-templates tbody td[data-omni-col=\"Name\"] a')].map(link => link.getBoundingClientRect().width))");
  check(templatesFull.widths[0] >= widestName, `La colonne du nom (${templatesFull.widths[0]} px) est plus étroite que son lien le plus long (${widestName.toFixed(1)} px).`);
  await evaluate("for (const box of document.querySelectorAll('.demo-grid-sizing-box')) box.style.removeProperty('inline-size')");
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(300);
  step(`colonnes au contenu : un en-tête sur une ligne, rien de coupé, la colonne État au plus à sa pastille ou à son en-tête (${narrow.widths[1]} px) dans des boîtes de 600, 900 et 1300 px, les colonnes Grow prennent la place restante, sans défilement latéral tant que le contenu tient (plancher de ${floor.toFixed(0)} px), sans Grow la place partagée à parts égales (+${Math.min(...shares)} px par colonne), grille à modèles chargée après coup à la largeur de ses cellules (nom ${templatesFull.widths[0]} px), colonne Wrap sur ${wrapLines} lignes dans 500 px`);

  // 8. FillAvailableHeight in a preview box that scrolls itself (request of 2026-10-12): with settings
  // taller than the visible part of the box above it, nothing is left to the grid, which drew 0px, its
  // frame 2px, every row cut. In both scroll modes and at 390 and 1280 px, the grid keeps its floor
  // (a viewport of MinHeight, 12rem, under its whole header bar), and once the box is scrolled to it its
  // cells and its bar buttons really show (intersected with every clipping ancestor and the window),
  // also under a host rule of a flex basis of 0 (display flex, flex 1 1 0, min-block-size 0). With room
  // left, the grid still ends at the bottom of the box and the box does not scroll.
  await session.visit('/composants/grille', '#demo-grid-fill-box-grid');
  const fillBox = "document.getElementById('demo-grid-fill-box')";
  const fillBoxState = `(() => {
    const box = ${fillBox};
    const grid = document.getElementById('demo-grid-fill-box-grid');
    const frame = grid.querySelector(':scope > .omni-data-grid__frame');
    const viewport = grid.querySelector('.omni-data-grid__viewport');
    const bar = grid.querySelector('.omni-data-grid__bar--header');
    const shown = element => {
      const own = element.getBoundingClientRect();
      let rect = { top: own.top, bottom: own.bottom, left: own.left, right: own.right };
      for (let node = element.parentElement; node; node = node.parentElement) {
        const style = getComputedStyle(node);
        if (style.overflowX === 'visible' && style.overflowY === 'visible') continue;
        const outer = node.getBoundingClientRect();
        const top = outer.top + parseFloat(style.borderTopWidth), left = outer.left + parseFloat(style.borderLeftWidth);
        rect = { top: Math.max(rect.top, top), bottom: Math.min(rect.bottom, top + node.clientHeight), left: Math.max(rect.left, left), right: Math.min(rect.right, left + node.clientWidth) };
      }
      rect = { top: Math.max(rect.top, 0), bottom: Math.min(rect.bottom, innerHeight), left: Math.max(rect.left, 0), right: Math.min(rect.right, innerWidth) };
      return { area: Math.max(0, rect.bottom - rect.top) * Math.max(0, rect.right - rect.left), whole: own.width * own.height };
    };
    const cells = [...grid.querySelectorAll('tbody tr:not(.omni-data-grid__spacer) td')].filter(cell => shown(cell).area > 0).length;
    const buttons = [...bar.querySelectorAll('button')];
    const style = getComputedStyle(box);
    const inner = box.getBoundingClientRect().bottom - parseFloat(style.borderBottomWidth) - parseFloat(style.paddingBottom);
    return {
      floor: parseFloat(getComputedStyle(viewport).minBlockSize),
      viewport: viewport.getBoundingClientRect().height,
      frame: frame.getBoundingClientRect().height,
      bar: bar.getBoundingClientRect().height,
      gridBottom: grid.getBoundingClientRect().bottom,
      inner,
      scrolls: box.scrollHeight > box.clientHeight + 1,
      cells,
      buttons: buttons.length,
      buttonsCut: buttons.filter(button => { const { area, whole } = shown(button); return Math.abs(area - whole) > 1; }).length
    };
  })()`;
  const hostRule = on => evaluate(`(() => { const grid = document.getElementById('demo-grid-fill-box-grid'); for (const [name, value] of [['display', 'flex'], ['flex', '1 1 0'], ['flex-direction', 'column'], ['min-block-size', '0']]) { if (${on}) grid.style.setProperty(name, value); else grid.style.removeProperty(name); } })()`);
  for (const [option, mode] of [['0', 'virtuel'], ['1', 'paginé']]) {
    await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
    await evaluate(`(() => { const box = ${fillBox}; box.firstElementChild.style.removeProperty('min-block-size'); box.scrollTop = 0; box.scrollIntoView({ block: 'start' }); const select = document.getElementById('grid-fill-box-pager'); select.value = '${option}'; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await waitFor(`la grille de l'aperçu en mode ${mode}`, `document.getElementById('demo-grid-fill-box-grid').classList.contains('omni-data-grid--virtual') === ${option === '0'} && document.querySelectorAll('#demo-grid-fill-box-grid tbody td').length > 3`);
    await waitFor(`la grille ${mode} au bas de l'aperçu, l'aperçu sans défilement`, `(() => { const state = ${fillBoxState}; return Math.abs(state.gridBottom - state.inner) <= 2 && !state.scrolls && state.viewport >= state.floor - 1; })()`);
    await evaluate(`${fillBox}.firstElementChild.style.setProperty('min-block-size', '40rem')`);
    let state;
    for (const width of [390, 1280]) {
      for (const rule of [false, true]) {
        await send('Emulation.setDeviceMetricsOverride', { width, height: width === 390 ? 844 : 900, deviceScaleFactor: 1, mobile: false });
        await hostRule(rule);
        await evaluate(`(() => { const box = ${fillBox}; box.scrollIntoView({ block: 'start' }); box.scrollTop = 0; })()`);
        const label = `${mode}, ${width} px${rule ? ', règle hôte flex 1 1 0' : ''}`;
        await waitFor(`la grille sous les réglages à son plancher (${label})`, `(() => { const state = ${fillBoxState}; return state.scrolls && Math.abs(state.viewport - state.floor) <= 1 && state.frame >= state.viewport + state.bar; })()`);
        await evaluate(`(() => { const box = ${fillBox}; box.scrollTop = box.scrollHeight; })()`);
        await waitFor(`les cellules visibles une fois l'aperçu défilé (${label})`, `(() => { const state = ${fillBoxState}; return state.cells >= 6 && state.buttons > 0 && state.buttonsCut === 0; })()`);
        state = await evaluate(fillBoxState);
      }
    }
    await hostRule(false);
    step(`grille remplissant un aperçu qui défile, en mode ${mode} : plancher de ${state.floor} px tenu sous des réglages trop hauts, ${state.cells} cellules et ${state.buttons} boutons de barre visibles à 390 et 1280 px, avec ou sans règle hôte flex 1 1 0`);
  }
  await evaluate(`(() => { const box = ${fillBox}; box.firstElementChild.style.removeProperty('min-block-size'); box.scrollTop = 0; })()`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });

  // 10. Requests of a client application of 2026-10-13 (PLAN-001, lot 8). R3-7: a model typed outside the
  // suggestions is the value once the field is left. R3-54: the export buttons of a grid given
  // BarButtonSize are as tall as the page's buttons of that size. R1-75: a variable button writes its token
  // at the caret of the subject, and the preview draws the rendered mail under the strict CSP, its look
  // given through the CSSOM (any refused style would be a console error, failing the step).
  await session.visit('/composants/saisie-avancee', '#inputs-model');
  await evaluate("(() => { const field = document.getElementById('inputs-model'); field.scrollIntoView({ block: 'center' }); field.focus(); })()");
  await waitFor('le champ du modèle focalisé', "document.activeElement?.id === 'inputs-model'");
  await type('modele-maison-7b');
  await tab();
  await waitFor('le texte libre pris pour valeur à la sortie du champ', "document.getElementById('inputs-model-value').textContent.includes('modele-maison-7b')");
  step('saisie libre : un modèle absent des suggestions devient la valeur quand on quitte le champ');

  await session.visit('/composants/grilles-maquette', '#runs-striped .omni-data-grid__export-button');
  const barState = await evaluate(`(() => {
    const heights = [...document.querySelectorAll('#runs-striped .omni-data-grid__export-button')].map(button => Math.round(button.getBoundingClientRect().height));
    const ruler = document.createElement('button');
    ruler.className = 'omni-button omni-button--secondary omni-button--medium';
    ruler.textContent = 'x';
    document.querySelector('main').append(ruler);
    const expected = Math.round(ruler.getBoundingClientRect().height);
    ruler.remove();
    return JSON.stringify({ heights, expected });
  })()`);
  const bar = JSON.parse(barState);
  check(bar.heights.length > 0 && bar.heights.every(height => height === bar.expected), `Les boutons d'export ne suivent pas BarButtonSize : ${barState}`);
  step(`boutons de barre de grille : ${bar.heights.length} boutons d'export à ${bar.expected} px, la hauteur d'un bouton Medium`);

  await session.visit('/composants/editeur', '#editor-mail');
  await evaluate("(() => { const field = document.querySelector('#editor-mail .omni-mail-template-editor__subject input'); field.scrollIntoView({ block: 'center' }); field.focus(); field.setSelectionRange(field.value.length, field.value.length); })()");
  await waitFor("l'objet focalisé", "document.activeElement?.closest('.omni-mail-template-editor__subject') !== null");
  await clickOn('#editor-mail [data-omni-variable="client"]');
  await waitFor("le jeton écrit au curseur de l'objet", "document.querySelector('#editor-mail .omni-mail-template-editor__subject input').value.endsWith('{{client}}')");
  await evaluate("document.querySelectorAll('#editor-mail [role=tab]')[1].setAttribute('data-probe-tab', 'preview')");
  await clickOn('#editor-mail [data-probe-tab=preview]');
  const previewState = `(() => {
    const host = document.querySelector('#editor-mail [data-omni-mail-preview]');
    const shadow = host?.shadowRoot;
    const heading = shadow?.querySelector('h2');
    const link = shadow?.querySelector('a');
    return JSON.stringify({
      drawn: !!heading,
      headingSize: heading ? getComputedStyle(heading).fontSize : null,
      linkUnderlined: link ? getComputedStyle(link).textDecorationLine.includes('underline') : false,
      sample: shadow?.textContent.includes('D-2026-0142') ?? false,
      subject: document.querySelector('#editor-mail .omni-mail-template-editor__preview-subject')?.textContent ?? ''
    });
  })()`;
  await waitFor("l'aperçu du mail dessiné", `JSON.parse(${previewState}).drawn`);
  const preview = JSON.parse(await evaluate(previewState));
  check(preview.headingSize === '22px' && preview.linkUnderlined && preview.sample, `L'aperçu du mail n'a pas ses styles ou ses valeurs d'exemple : ${JSON.stringify(preview)}`);
  check(preview.subject.includes('Anne Martin'), `L'objet rendu ne porte pas la variable insérée : ${JSON.stringify(preview)}`);
  // A link of the preview leads nowhere: clicking it leaves the page being edited where it is.
  const before = await evaluate('location.href');
  const followed = await evaluate("(() => { const link = document.querySelector('#editor-mail [data-omni-mail-preview]').shadowRoot.querySelector('a'); const click = new MouseEvent('click', { bubbles: true, cancelable: true, composed: true }); link.dispatchEvent(click); return click.defaultPrevented; })()");
  check(followed === true && await evaluate('location.href') === before, `Un lien de l'aperçu a été suivi : ${followed}`);
  step("modèle de mail : variable insérée au curseur de l'objet, aperçu dessiné avec les styles du mail et les valeurs d'exemple");

  // The tokens of the body are marked by the CSS Custom Highlight API, and « Send a test » hands the mail to the host.
  const marked = await evaluate("CSS.highlights.get('omni-mail-token')?.size ?? 0");
  const bodyTokens = await evaluate("(document.querySelector('#editor-mail .omni-html-editor__surface').textContent.match(/\{\{[^}]+\}\}/g) ?? []).length");
  check(bodyTokens > 0 && marked === bodyTokens, `Jetons du corps non marqués : ${marked} marqués pour ${bodyTokens}`);
  await clickOn('#editor-mail .omni-mail-template-editor__send-test');
  await waitFor("l'essai remis à l'application", "document.getElementById('editor-mail-test').textContent.includes('Anne Martin')");
  step(`modèle de mail : ${marked} jetons du corps marqués, essai remis à l'application`);

  // 11. A grid veiled by LoadingContent waits for the pictures of its first rows (waitForReady of
  // omni-grid.js, at most 2 s): the demo-grid-pictures image is held at the request stage by CDP. The
  // grid stays veiled while the picture is held, shows once it arrives (hold of 400 ms), and still
  // shows, the picture unarrived, once the 2 s timeout has passed. The Fetch interception and the cache
  // bypass are lifted at the end whatever happens, so later steps are not touched.
  {
    const held = [];
    const stop = session.on('Fetch.requestPaused', params => held.push(params.requestId));
    const grid = "document.getElementById('demo-grid-pictures')";
    const veiled = `(() => { const grid = ${grid}; return !!grid && grid.classList.contains('omni-data-grid--preparing') && !!grid.querySelector('.omni-data-grid__preparation'); })()`;
    const shown = `(() => { const grid = ${grid}; return !!grid && !grid.classList.contains('omni-data-grid--preparing') && !grid.querySelector('.omni-data-grid__preparation') && grid.querySelectorAll('tbody img').length > 0; })()`;
    const arrived = `[...${grid}.querySelectorAll('tbody img')].every(image => image.complete && image.naturalWidth > 0)`;
    const release = async () => {
      for (const requestId of held.splice(0)) await session.send('Fetch.continueRequest', { requestId });
    };
    // A picture the document already drew comes again from its list of available images, without a
    // request, whatever the cache says: each hold starts on a new document, its CSP record checked first.
    const { siteUrl } = probeOptions('Test-ShowcaseScriptsProbe.mjs');
    const holdPicture = async () => {
      held.length = 0;
      const csp = await evaluate('window.__omniCsp');
      check(csp.length === 0, `Violations CSP avant le rechargement de la grille à images : ${csp.join(' | ')}`);
      await send('Page.navigate', { url: siteUrl });
      await waitFor('le runtime Blazor rechargé', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null");
      await session.visit('/composants/grille', '#demo-grid-pictures');
      const until = Date.now() + 15_000;
      while (held.length === 0 && Date.now() < until) await pause(50);
      check(held.length > 0, "Aucune requête de l'image de la grille interceptée.");
    };
    await session.send('Network.enable');
    await session.send('Network.setCacheDisabled', { cacheDisabled: true });
    await session.send('Fetch.enable', { patterns: [{ urlPattern: '*grid-preparation*', requestStage: 'Request' }] });
    try {
      // The picture arrives after 400 ms: veiled meanwhile, shown with its picture drawn afterwards.
      await holdPicture();
      await pause(400);
      check(await evaluate(veiled), "La grille à images est affichée alors que l'image est retenue.");
      await release();
      await waitFor('la grille à images affichée avec ses images', `${shown} && ${arrived}`);
      step("grille à images : voilée tant que l'image est retenue (400 ms), affichée à son arrivée");

      // The picture never arrives within the timeout: the grid shows as it is, picture still unarrived.
      await holdPicture();
      await waitFor('la grille à images affichée malgré une image qui tarde', shown, 6_000);
      check(!(await evaluate(arrived)), "L'image retenue est déjà arrivée : le délai n'est pas éprouvé.");
      await release();
      await waitFor("l'image tardive arrivée", arrived);
      step("grille à images : affichée telle quelle après le délai de 2 s, l'image retenue");
    }
    finally {
      stop();
      await session.send('Fetch.disable');
      await session.send('Network.setCacheDisabled', { cacheDisabled: false });
    }
  }
}
