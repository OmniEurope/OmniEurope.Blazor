// The text field of an autocomplete, driven by real mouse and keyboard input (the owner's decision of
// 2026-11-05, requested by a client application that patched this in its own script): with
// ReplaceTextOnType the first text typed after a suggestion is picked replaces it, an arrow ends that
// state and the text is then extended, entering the field selects its text; and Escape never empties the
// field, whatever the autocomplete (the native search field empties itself on Escape in Chromium).

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
}
