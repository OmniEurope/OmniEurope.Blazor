// The caret of a bound text field (reported by a client application that kept a global script to patch it):
// the characters typed in the middle of the text land where the caret was and the caret stays right after
// them, with the value bound back to the page on every keystroke, for the text box, the text box with a
// debounce, the text area and a text box in a grid row in edit mode. Driven with real key presses, so the native caret placement runs before the
// re-render the binding causes.

export async function caretSteps(session, results) {
  const { evaluate, waitFor, key, check, clickOn, pause } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  // A real key press that inserts its character (keydown with text: beforeinput, input, keyup).
  const press = character => key(character, `Key${character.toUpperCase()}`, character.toUpperCase().charCodeAt(0), 0, character);
  const field = id => `document.getElementById(${JSON.stringify(id)})`;
  const state = id => evaluate(`({ value: ${field(id)}.value, start: ${field(id)}.selectionStart, end: ${field(id)}.selectionEnd })`);

  // Types a character at a caret placed by the script in the middle of the text, then reads where it went.
  const typeInMiddle = async (id, caret, characters) => {
    await clickOn(`#${id}`);
    await evaluate(`${field(id)}.setSelectionRange(${caret}, ${caret})`);
    for (const character of characters) {
      await press(character);
      // No pause on purpose: the next key arrives as soon as the binding re-render of the previous one is done.
      await pause(0);
    }
    return state(id);
  };
  const expectCaret = async (id, label, expected, expectedCaret, characters) => {
    const text = await state(id);
    check(text.value === expected, `${label} : « ${expected} » attendu, « ${text.value} » lu.`);
    check(text.start === expectedCaret && text.end === expectedCaret,
      `${label} : le caret devrait rester à ${expectedCaret} après « ${characters} », il est à ${text.start}-${text.end}.`);
  };

  await session.visit('/components/text-fields', '#bare-text-box');
  await evaluate(`${field('bare-text-box')}.scrollIntoView({ block: 'center' })`);

  // 1. Text box bound on every keystroke (no debounce): typed in the middle, the caret stays after the characters.
  await clickOn('#bare-text-box');
  for (const character of 'hello') await press(character);
  await waitFor('le texte lié', `${field('bare-text-box')}.value === 'hello'`);
  await typeInMiddle('bare-text-box', 2, 'XYZWVUTSRQ');
  await expectCaret('bare-text-box', 'Champ texte lié', 'heXYZWVUTSRQllo', 12, 'XYZWVUTSRQ');
  step('champ texte lié à chaque frappe : le caret reste après les caractères tapés au milieu du texte');

  // 2. The same with the debounce of the example switched on: the delay ends while the user types on.
  await clickOn('[aria-label="Debounced typing"]');
  await pause(300);
  await evaluate(`${field('text-box')}.scrollIntoView({ block: 'center' })`);
  await clickOn('#text-box');
  await evaluate(`${field('text-box')}.select()`);
  for (const character of 'world') await press(character);
  await pause(1500);
  await typeInMiddle('text-box', 2, 'XY');
  await pause(1500);
  await expectCaret('text-box', 'Champ texte avec délai', 'woXYrld', 4, 'XY');
  step('champ texte avec délai : le caret reste après les caractères tapés au milieu du texte');

  // 3. The text area, bound the same way.
  await evaluate(`${field('text-area')}.scrollIntoView({ block: 'center' })`);
  await clickOn('#text-area');
  for (const character of 'abcd') await press(character);
  await waitFor('le texte de la zone lié', `${field('text-area')}.value === 'abcd'`);
  await typeInMiddle('text-area', 2, 'XY');
  await expectCaret('text-area', 'Zone de texte liée', 'abXYcd', 4, 'XY');
  step('zone de texte liée : le caret reste après les caractères tapés au milieu du texte');

  // 4. The field of a grid row in edit mode (the setting of a client application: a bound text box in a cell).
  await session.visit('/components/data-grid-variants', '#demo-grid-edit');
  await evaluate("document.getElementById('demo-grid-edit').scrollIntoView({ block: 'center' })");
  const cell = '#demo-grid-edit tbody tr:first-child td:nth-child(2)';
  await clickOn(cell);
  await clickOn(cell, { clickCount: 2 });
  const applicant = '#demo-grid-edit input[aria-label="Applicant"]';
  await waitFor('le champ de la ligne en édition', `document.querySelector('${applicant}') !== null`);
  await evaluate(`document.querySelector('${applicant}').id = 'grid-applicant'`);
  await evaluate(`${field('grid-applicant')}.select()`);
  for (const character of 'abcd') await press(character);
  await waitFor('le texte de la cellule lié', `${field('grid-applicant')}.value === 'abcd'`);
  await typeInMiddle('grid-applicant', 2, 'XYZWV');
  await expectCaret('grid-applicant', 'Champ de cellule', 'abXYZWVcd', 7, 'XYZWV');
  step('champ d\'une ligne de grille en édition : le caret reste après les caractères tapés au milieu du texte');
}
