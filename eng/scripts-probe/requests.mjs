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
}
