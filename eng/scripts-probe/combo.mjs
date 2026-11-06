// The text field of an autocomplete, driven by real mouse and keyboard input (the owner's decision of
// 2026-11-05, requested by a client application that patched this in its own script): with
// ReplaceTextOnType the first text typed after a suggestion is picked replaces it, an arrow ends that
// state and the text is then extended, entering the field selects its text; and Escape never empties the
// field, whatever the autocomplete (the native search field empties itself on Escape in Chromium). The same
// Escape in a dialog (step 6): the list of an autocomplete or of a filterable drop-down, the panel of a multiple
// selection and the character panel of the HTML editor close without closing the dialog, a second Escape closes
// it, and the search text typed in the last two is never emptied by the browser.

export async function comboSteps(session, results) {
  const { evaluate, waitFor, key, check, clickOn, pause } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const tab = () => key('Tab', 'Tab', 9);
  const escape = () => key('Escape', 'Escape', 27);
  // A real key press that inserts its character (keydown with text: beforeinput, input, keyup).
  const press = character => key(character, `Key${character.toUpperCase()}`, character.toUpperCase().charCodeAt(0), 0, character);
  const typeText = async text => {
    for (const character of text) {
      await (character === '-' ? key('-', 'Minus', 189, 0, '-') : press(character));
    }
  };
  const field = id => `document.getElementById(${JSON.stringify(id)})`;
  const valueOf = id => evaluate(`${field(id)}.value`);
  const expanded = id => evaluate(`${field(id)}.getAttribute('aria-expanded') === 'true'`);
  const boundText = id => evaluate(`document.getElementById(${JSON.stringify(id + '-value')}).textContent`);

  // Picks the suggestion of that text by a click on it, the field being focused by a click first.
  const pickByClick = async (id, text) => {
    await clickOn(`#${id}`);
    await waitFor('la liste des suggestions ouverte', `${field(id)}.getAttribute('aria-expanded') === 'true' && document.querySelector('#${id}-results [role=option]') !== null`);
    const option = await evaluate(`[...document.querySelectorAll('#${id}-results [role=option]')].findIndex(candidate => candidate.textContent.trim() === ${JSON.stringify(text)})`);
    check(option >= 0, `La suggestion ${text} n'est pas listée.`);
    await clickOn(`#${id}-results [role=option]:nth-of-type(${option + 1})`);
    await waitFor(`${text} pris pour valeur`, `${field(id)}.value === ${JSON.stringify(text)} && document.getElementById('${id}-value').textContent.includes(${JSON.stringify(text)})`);
    check(await evaluate(`document.activeElement === ${field(id)}`), 'Le focus a quitté le champ après le choix de la suggestion.');
  };

  // 0. Without ReplaceTextOnType (the free text demonstration), a pick then a character extends the text, and
  // Escape still leaves the value alone.
  await session.visit('/components/autocomplete', '#autocomplete-model');
  await evaluate(`${field('autocomplete-model')}.scrollIntoView({ block: 'center' })`);
  await pickByClick('autocomplete-model', 'model-large');
  await press('x');
  check(await valueOf('autocomplete-model') === 'model-largex', `Sans ReplaceTextOnType la frappe s'ajoute : « model-largex » attendu, « ${await valueOf('autocomplete-model')} » lu.`);
  await escape();
  await escape();
  await pause(300);
  check(await valueOf('autocomplete-model') === 'model-largex', `Échap a vidé le champ d'une saisie libre : « ${await valueOf('autocomplete-model')} ».`);
  await tab();
  step('sans ReplaceTextOnType : la frappe suivant un choix s\'ajoute au texte, et Échap laisse le champ intact');

  await session.visit('/components/autocomplete', '#autocomplete-replace');
  await evaluate(`${field('autocomplete-replace')}.scrollIntoView({ block: 'center' })`);

  // 1. Pick by a click, then type: the first character replaces the picked text, the next ones join it.
  await pickByClick('autocomplete-replace', 'model-large');
  await press('x');
  check(await valueOf('autocomplete-replace') === 'x', `Après un choix puis une frappe, le champ devrait valoir « x », il vaut « ${await valueOf('autocomplete-replace')} ».`);
  await press('y');
  check(await valueOf('autocomplete-replace') === 'xy', `Seule la première frappe remplace : « xy » attendu, « ${await valueOf('autocomplete-replace')} » lu.`);
  step('ReplaceTextOnType : après un clic sur une suggestion, la première frappe remplace le texte choisi et les suivantes s\'y ajoutent');

  // 2. Pick by Enter on the highlighted suggestion, then an arrow: what is typed is inserted at the caret, not substituted.
  await evaluate(`${field('autocomplete-replace')}.select()`);
  await typeText('model-s');
  await waitFor('la suggestion model-small listée', "document.querySelector('#autocomplete-replace-results [role=option]') !== null");
  await key('ArrowDown', 'ArrowDown', 40);
  await waitFor('une suggestion en surbrillance', `${field('autocomplete-replace')}.getAttribute('aria-activedescendant') !== null`);
  await key('Enter', 'Enter', 13);
  await waitFor('model-small pris par Entrée', `${field('autocomplete-replace')}.value === 'model-small'`);
  await key('ArrowLeft', 'ArrowLeft', 37);
  await press('z');
  check(await valueOf('autocomplete-replace') === 'model-smalzl', `Après une flèche, la frappe s'ajoute au texte : « model-smalzl » attendu, « ${await valueOf('autocomplete-replace')} » lu.`);
  step('ReplaceTextOnType : après Entrée sur une suggestion puis une flèche, la frappe s\'ajoute au texte au lieu de le remplacer');

  // 3. Pick by Enter, then type at once: replaced.
  await evaluate(`${field('autocomplete-replace')}.select()`);
  await typeText('model-m');
  await waitFor('la suggestion model-medium listée', "document.querySelector('#autocomplete-replace-results [role=option]') !== null");
  await key('ArrowDown', 'ArrowDown', 40);
  await key('Enter', 'Enter', 13);
  await waitFor('model-medium pris par Entrée', `${field('autocomplete-replace')}.value === 'model-medium'`);
  await press('q');
  check(await valueOf('autocomplete-replace') === 'q', `Après Entrée sur une suggestion, la première frappe remplace : « q » attendu, « ${await valueOf('autocomplete-replace')} » lu.`);
  step('ReplaceTextOnType : après Entrée sur une suggestion, la première frappe remplace le texte choisi');

  // 4. Escape: with the list open it closes it, with the list closed it does nothing; the value stays either way,
  // and .NET never hears of an emptied field (leaving the field takes the text as it is).
  await evaluate(`${field('autocomplete-replace')}.select()`);
  await typeText('model-m');
  await waitFor('la liste ouverte', "document.getElementById('autocomplete-replace').getAttribute('aria-expanded') === 'true'");
  await escape();
  await waitFor('la liste refermée par Échap', `${field('autocomplete-replace')}.getAttribute('aria-expanded') === 'false'`);
  check(await valueOf('autocomplete-replace') === 'model-m', `Échap a vidé ou changé le champ, liste ouverte : « ${await valueOf('autocomplete-replace')} ».`);
  await escape();
  await pause(300);
  check(await valueOf('autocomplete-replace') === 'model-m', `Échap a vidé ou changé le champ, liste fermée : « ${await valueOf('autocomplete-replace')} ».`);
  check(!await expanded('autocomplete-replace'), 'Échap a rouvert la liste.');
  await tab();
  await waitFor('le texte pris pour valeur à la sortie du champ', "document.getElementById('autocomplete-replace-value').textContent.includes('model-m')");
  check((await boundText('autocomplete-replace')).includes('model-m'), 'La valeur liée ne reprend pas le texte resté dans le champ.');
  step('Échap : la liste ouverte se ferme, fermée rien ne change, le texte reste et .NET le prend à la sortie du champ');

  // 5. Entering the field by a mouse press selects its text, and the release does not drop that selection.
  await clickOn('#autocomplete-replace');
  await waitFor('le texte sélectionné à l\'entrée dans le champ', `(() => { const input = ${field('autocomplete-replace')}; return document.activeElement === input && input.value.length > 0 && input.selectionStart === 0 && input.selectionEnd === input.value.length; })()`);
  await escape();
  await tab();
  step('ReplaceTextOnType : entrer dans le champ à la souris en sélectionne le texte, que le relâchement ne défait pas');

  // 6. Escape in a dialog (the owner's decision of 2026-11-05): the Escape that closes the open list of a field
  // inside an OmniDialog must not close the dialog, the next one does. The dialog closes on a Blazor handler, so a
  // list that consumes Escape has to keep it from reaching it. Each field is driven by real keys and real clicks.
  const dialogOpen = id => evaluate(`document.getElementById(${JSON.stringify(id)}) !== null`);
  const openDialog = async (buttonId, dialogId) => {
    await clickOn(`#${buttonId}`);
    await waitFor(`le dialogue ${dialogId} ouvert`, `document.getElementById('${dialogId}') !== null`);
    await pause(300);
  };
  const closeDialogByEscape = async dialogId => {
    await escape();
    await waitFor(`le dialogue ${dialogId} refermé par un second Échap`, `document.getElementById('${dialogId}') === null`);
  };
  const inDialog = dialogId => evaluate(`document.getElementById(${JSON.stringify(dialogId)}).contains(document.activeElement)`);
  // Records, in the page, what happens to a search field around the next Escape: the input events it receives
  // (the browser's own emptying fires one) and whether a script cancelled the key (read on an ancestor of the field,
  // after the listeners of the package that sit lower).
  const watchEscape = (selector, name, holder = 'document') => evaluate(`(() => {
    const search = document.querySelector(${JSON.stringify(selector)});
    const record = window[${JSON.stringify(name)}] = { prevented: null, inputs: [] };
    search.addEventListener('input', () => record.inputs.push(search.value));
    (${holder === 'document' ? 'document' : `document.querySelector(${JSON.stringify(holder)})`}).addEventListener('keydown', event => { if (event.key === 'Escape') record.prevented = event.defaultPrevented; }, { once: true });
  })()`);

  await session.visit('/components/multi-select', '#fields-dialog-open');

  // 6a. The autocomplete: its list open, a first Escape closes the list and the dialog stays, a second closes the dialog.
  await openDialog('fields-dialog-open', 'fields-dialog');
  await clickOn('#fields-dialog-model');
  await typeText('model-m');
  await waitFor('la liste de l\'autocomplétion ouverte', `${field('fields-dialog-model')}.getAttribute('aria-expanded') === 'true'`);
  await escape();
  await waitFor('la liste refermée par Échap', `(${field('fields-dialog-model')}?.getAttribute('aria-expanded') ?? 'false') === 'false'`);
  await pause(300);
  check(await dialogOpen('fields-dialog'), 'Le premier Échap, qui ferme la liste de l\'autocomplétion, a aussi fermé le dialogue.');
  check(await valueOf('fields-dialog-model') === 'model-m', `Échap a changé le champ : « ${await valueOf('fields-dialog-model')} ».`);
  await closeDialogByEscape('fields-dialog');
  step('Échap dans un dialogue : la liste ouverte d\'un OmniAutocomplete se ferme, le dialogue reste ouvert et un second Échap le ferme');

  // 6b. The Tab trap of the dialog still works with a list open (its native listener is not reached by the Blazor
  // stop of the key): Tab and Shift+Tab leave the field for its neighbours, inside the dialog.
  const shiftTab = () => key('Tab', 'Tab', 9, 8);
  await openDialog('fields-dialog-open', 'fields-dialog');
  await clickOn('#fields-dialog-model');
  await waitFor('la liste de l\'autocomplétion ouverte', `${field('fields-dialog-model')}.getAttribute('aria-expanded') === 'true'`);
  await tab();
  await waitFor('le focus passé au choix filtrable', `document.activeElement === ${field('fields-dialog-size')}`);
  await shiftTab();
  await waitFor('le focus revenu à l\'autocomplétion, sa liste rouverte', `document.activeElement === ${field('fields-dialog-model')} && ${field('fields-dialog-model')}.getAttribute('aria-expanded') === 'true'`);
  check(await inDialog('fields-dialog'), 'Le focus a quitté le dialogue : le piège de Tab est cassé.');
  await evaluate("document.getElementById('fields-dialog').querySelector('.omni-dialog__close').focus()");
  await closeDialogByEscape('fields-dialog');
  step('Échap dans un dialogue : le piège de Tab du dialogue fonctionne avec une liste ouverte (Tab et Maj+Tab passent aux champs voisins du dialogue)');

  // 6c. The multiple selection: the panel open with a search text typed, a first Escape closes the panel and the
  // dialog stays, the text typed stays in the search field and .NET never hears of an emptied one, a second Escape
  // closes the dialog.
  await openDialog('fields-dialog-open', 'fields-dialog');
  await clickOn('#fields-dialog-countries > summary');
  await waitFor('le panneau de la sélection multiple ouvert', `${field('fields-dialog-countries')}.open === true`);
  await clickOn('#fields-dialog-countries input[type=search]');
  await typeText('ger');
  await waitFor('le texte de recherche pris par .NET', "document.getElementById('fields-dialog-state').textContent.includes('search: ger')");
  await watchEscape('#fields-dialog-countries input[type=search]', '__multiSelectEscape', '#fields-dialog-countries');
  await escape();
  await waitFor('le panneau refermé par Échap', `${field('fields-dialog-countries')}?.open !== true`);
  await pause(400);
  check(await dialogOpen('fields-dialog'), 'Le premier Échap, qui ferme le panneau de la sélection multiple, a aussi fermé le dialogue.');
  const kept = await evaluate("document.querySelector('#fields-dialog-countries input[type=search]').value");
  check(kept === 'ger', `Échap a vidé le champ de recherche de la sélection multiple : « ${kept} » lu.`);
  const multiSelectEscape = await evaluate('window.__multiSelectEscape');
  check(multiSelectEscape.prevented === true, 'Échap n\'a pas été annulé dans le champ de recherche : le navigateur le vide.');
  check(multiSelectEscape.inputs.length === 0, `Le champ de recherche a reçu un événement input à l'Échap : ${JSON.stringify(multiSelectEscape.inputs)}.`);
  check(await evaluate("document.getElementById('fields-dialog-state').textContent.includes('search: ger')"), 'Le texte de recherche lié n\'est plus « ger » : .NET a reçu un champ vidé.');
  await closeDialogByEscape('fields-dialog');
  step('Échap dans un dialogue : le panneau de l\'OmniMultiSelect se ferme, le dialogue reste ouvert, le texte du filtre reste (jamais vidé, .NET ne reçoit rien) et un second Échap ferme le dialogue');

  // 6d. The filterable drop-down is an autocomplete: same behaviour.
  await openDialog('fields-dialog-open', 'fields-dialog');
  await clickOn('#fields-dialog-size');
  await waitFor('la liste du choix filtrable ouverte', `${field('fields-dialog-size')}.getAttribute('aria-expanded') === 'true'`);
  await escape();
  await waitFor('la liste refermée par Échap', `(${field('fields-dialog-size')}?.getAttribute('aria-expanded') ?? 'false') === 'false'`);
  await pause(300);
  check(await dialogOpen('fields-dialog'), 'Le premier Échap, qui ferme la liste du choix filtrable, a aussi fermé le dialogue.');
  await closeDialogByEscape('fields-dialog');
  step('Échap dans un dialogue : la liste d\'un OmniDropDown filtrable se ferme, le dialogue reste ouvert et un second Échap le ferme');

  // 6e. The character search of the HTML editor: Escape closes the panel, the key is cancelled so the browser does
  // not empty the search field, the focus goes back to the surface and the dialog stays open; a second Escape closes it.
  await session.visit('/components/html-editor', '#editor-dialog-open');
  await openDialog('editor-dialog-open', 'editor-dialog');
  await waitFor('la surface de l\'éditeur prête', "document.getElementById('editor-in-dialog')?.getAttribute('data-omni-ready') === 'true'");
  await clickOn('#editor-dialog [data-command="insert-special-character"]');
  await waitFor('le panneau des caractères ouvert', "document.querySelector('#editor-dialog .omni-html-editor__char-search') !== null");
  await clickOn('#editor-dialog .omni-html-editor__char-search');
  await typeText('eur');
  await waitFor('le texte cherché dans le champ', "document.querySelector('#editor-dialog .omni-html-editor__char-search').value === 'eur'");
  await watchEscape('#editor-dialog .omni-html-editor__char-search', '__charSearchEscape');
  await escape();
  await waitFor('le panneau des caractères refermé par Échap', "document.querySelector('#editor-dialog .omni-html-editor__char-search') === null");
  await pause(300);
  check(await dialogOpen('editor-dialog'), 'Échap dans la recherche de caractères a fermé le dialogue.');
  const charSearchEscape = await evaluate('window.__charSearchEscape');
  check(charSearchEscape.prevented === true, 'Échap n\'a pas été annulé dans la recherche de caractères : le navigateur vide le champ.');
  check(charSearchEscape.inputs.length === 0, `La recherche de caractères a reçu un événement input à l'Échap : ${JSON.stringify(charSearchEscape.inputs)}.`);
  check(await evaluate("document.activeElement === document.getElementById('editor-in-dialog')"), 'Le focus n\'est pas revenu à la surface de l\'éditeur.');
  await closeDialogByEscape('editor-dialog');
  step('Échap dans la recherche de caractères de l\'éditeur HTML : le panneau se ferme, le navigateur ne vide pas le champ, le focus revient à la surface, le dialogue reste ouvert et un second Échap le ferme');

  // 6f. The date, date and time and time pickers: their panel is fixed to the window, and the Escape that closes it
  // must not close the dialog, the next one does, with the focus in the panel or in the text field of the picker.
  // The picker closes on Escape through a keydown listener that omni-focus.js attaches a few milliseconds after the
  // panel shows (3 to 20 ms measured): an Escape sent before that is held back from the dialog but closes nothing,
  // which a person cannot press fast enough to meet and a probe can (it failed 2 runs in 10 sent at once). The probe
  // therefore waits for the listener, which the debugger reports, instead of a fixed time.
  const pickerListens = async id => {
    const { result } = await session.send('Runtime.evaluate', { expression: `document.getElementById(${JSON.stringify(id)}).closest('.omni-date')` });
    const { listeners } = await session.send('DOMDebugger.getEventListeners', { objectId: result.objectId });
    await session.send('Runtime.releaseObject', { objectId: result.objectId });
    return listeners.some(listener => listener.type === 'keydown');
  };
  await session.visit('/components/multi-select', '#fields-dialog-open');
  for (const [id, label] of [['fields-dialog-date', 'date'], ['fields-dialog-moment', 'date et heure'], ['fields-dialog-time', 'heure']]) {
    const panel = '#fields-dialog .omni-calendar';
    for (const fromField of [false, true]) {
      await openDialog('fields-dialog-open', 'fields-dialog');
      await clickOn(`#${id} ~ .omni-date__toggle`);
      await waitFor(`le panneau du sélecteur de ${label} ouvert`, `document.querySelector('${panel}') !== null`);
      if (fromField) {
        await clickOn(`#${id}`);
        check(await evaluate(`document.activeElement === ${field(id)}`), `Le focus n'est pas dans le champ du sélecteur de ${label}.`);
      }
      for (const until = Date.now() + 5000; !await pickerListens(id);) {
        check(Date.now() < until, `Le sélecteur de ${label} n'a pas branché l'écoute d'Échap.`);
        await pause(5);
      }
      await escape();
      await waitFor(`le panneau du sélecteur de ${label} refermé par Échap`, `document.querySelector('${panel}') === null`);
      await pause(400);
      check(await dialogOpen('fields-dialog'), `Le premier Échap, qui ferme le panneau du sélecteur de ${label}${fromField ? ' (focus dans le champ)' : ''}, a aussi fermé le dialogue.`);
      await closeDialogByEscape('fields-dialog');
    }
  }
  step('Échap dans un dialogue : le panneau d\'un sélecteur de date, de date et heure et d\'heure se ferme (focus dans le panneau ou dans le champ), le dialogue reste ouvert et un second Échap le ferme');

  // 6g. The Tab trap of the dialog cycles over what takes the focus on screen: a closed multiple selection is its
  // summary alone (the filter field hidden in its closed panel cannot take the focus, and counted in the cycle it
  // swallowed the Tab). Forward from the summary, backward from the next field, and a whole turn of the dialog.
  await session.visit('/components/multi-select', '#fields-dialog-open');
  await openDialog('fields-dialog-open', 'fields-dialog');
  check(await evaluate(`${field('fields-dialog-countries')}.open === false`), 'La sélection multiple devrait être fermée.');
  await evaluate("document.querySelector('#fields-dialog-countries > summary').focus()");
  await tab();
  await waitFor('le focus passé du résumé de la sélection multiple au champ de date', `document.activeElement === ${field('fields-dialog-date')}`);
  await shiftTab();
  await waitFor('le focus revenu au résumé de la sélection multiple', "document.activeElement === document.querySelector('#fields-dialog-countries > summary')");
  const visited = [];
  await evaluate("document.querySelector('#fields-dialog .omni-dialog__close').focus()");
  for (let turn = 0; turn < 16; turn++) {
    const before = await evaluate('document.activeElement' + ".outerHTML.slice(0, 80)");
    await tab();
    await pause(60);
    const after = await evaluate(`(() => { const active = document.activeElement; const hidden = active.closest('details:not([open])') !== null && active.tagName !== 'SUMMARY'; return { html: active.outerHTML.slice(0, 80), inDialog: document.getElementById('fields-dialog').contains(active), hidden }; })()`);
    check(after.inDialog && !after.hidden, `Un Tab a mené le focus hors du cycle visible du dialogue : ${JSON.stringify(after)}.`);
    check(after.html !== before, `Un Tab n'a pas déplacé le focus (piège bloqué sur un élément caché ?) : ${after.html}.`);
    visited.push(after.html);
  }
  check(new Set(visited).size >= 8, `Le cycle de Tab du dialogue ne parcourt pas ses champs : ${JSON.stringify(visited)}.`);
  await evaluate("document.querySelector('#fields-dialog .omni-dialog__close').focus()");
  await closeDialogByEscape('fields-dialog');
  step('Tab dans un dialogue : le cycle ne compte que les éléments visibles (le champ de filtre d\'une sélection multiple fermée est ignoré, son résumé garde un arrêt)');
}
