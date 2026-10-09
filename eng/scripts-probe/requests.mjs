// The requests of a client application of 2026-10-08 (PLAN-018) that only a browser shows: a title the
// package tooltip takes away still names an icon-only button while it is focused or hovered, and the
// skip link moves the focus to the main content by script, without leaving the page.

export async function requestSteps(session, results) {
  const { evaluate, waitFor, key, check, hover, mouse, pointOf, clickOn } = session;
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

  // 4. The initial focus of a dialog (recette R1-10, InitialFocus Panel on the intent dialogs): the panel
  // itself holds the focus as it opens, no control of the dialog is focused and nothing draws a focus
  // ring; the first Tab reaches the first focusable element (the close button), Escape gives the focus
  // back to the trigger.
  await session.visit('/composants/superpositions', '#demo-dialog-intent-accent');
  await clickOn('#demo-dialog-intent-accent');
  await waitFor("le dialogue d'intention ouvert, le panneau focalisé", "document.activeElement?.matches('.omni-overlay section.omni-dialog') === true");
  const ring = await evaluate("(() => { const panel = document.activeElement; const style = getComputedStyle(panel); return JSON.stringify({ outline: style.outlineStyle, shadowRing: !!panel.querySelector(':focus-visible') }); })()");
  check(ring === JSON.stringify({ outline: 'none', shadowRing: false }), `Le panneau focalisé montre un anneau : ${ring}`);
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
}
