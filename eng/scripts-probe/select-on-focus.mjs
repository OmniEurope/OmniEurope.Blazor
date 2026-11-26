// SelectOnFocus of the text-entry fields, driven in a real Chromium under the strict CSP on the Numbers and Text fields pages, at 1280
// and at 375 px, with real mouse and keyboard input (CDP Input events). The demos carry one field with SelectOnFocus
// (#numeric-select-on-focus, #text-box-select-on-focus) and the page's other fields do not. Checked:
// - a real mouse click that gives the focus selects the whole content and keeps it after the mouseup (the pitfall: the browser
//   places the caret on mouseup, after the focus event): typing "5" leaves the value 5, and the value the example shows after the
//   field is left is 5; for the text box the selection is read back (selectionStart 0, selectionEnd the length);
// - the same entering by Tab, by Shift+Tab and by a programmatic focus(), typing replaces the value;
// - a second click inside the now-focused field places the caret (the selection collapses, typing is inserted, not substituted);
// - a field without SelectOnFocus keeps today's behaviour: a click does not select all (typing is inserted);
// - the textarea and the password of the Text fields page, with the script attached by hand to them, select on a real click too.

export async function selectOnFocusSteps(session, results) {
  const { evaluate, waitFor, key, check, clickOn, pause } = session;
  const step = session.stepRecorder(results);
  const { tab } = session;
  const digit = character => key(character, `Digit${character}`, 48 + Number(character), 0, character);
  const press = character => key(character, `Key${character.toUpperCase()}`, character.toUpperCase().charCodeAt(0), 0, character);
  const field = id => `document.getElementById(${JSON.stringify(id)})`;
  const valueOf = id => evaluate(`${field(id)}.value`);
  const isActive = id => evaluate(`document.activeElement === ${field(id)}`);
  // What the page selects: the text of a text field (selectionStart and End), or the selection of the document for the number
  // field, whose selectionStart is null by specification.
  const selected = id => evaluate(`(() => { const element = ${field(id)}; const start = element.selectionStart; const end = element.selectionEnd; return JSON.stringify({ start, end, text: String(getSelection()) }); })()`).then(JSON.parse);
  const echoIs = (id, text) => waitFor(`${id} : « ${text} »`, `${field(id)}.textContent.trim() === ${JSON.stringify(text)}`);

  for (const width of [1280, 375]) {
    // ----- Numbers: OmniNumeric, type="number" ------------------------------------------------------------------------
    await session.visitAtWidth(width, '/components/numbers', '#numeric-select-on-focus');
    const focused = '#numeric-select-on-focus';
    const echo = 'numeric-select-on-focus-echo';
    await evaluate(`${field('numeric-select-on-focus')}.scrollIntoView({ block: 'center' })`);
    check(await valueOf('numeric-select-on-focus') === '42', `Valeur de départ du champ inattendue : ${await valueOf('numeric-select-on-focus')}.`);

    // A field without SelectOnFocus: a click does not select all, the typed digit is inserted after the caret.
    await clickOn('#numeric-int');
    check(await isActive('numeric-int'), 'Le clic n\'a pas donné le focus au champ numérique témoin.');
    let state = await selected('numeric-int');
    check(state.text === '', `Le champ numérique sans SelectOnFocus a une sélection après un clic : ${JSON.stringify(state)}.`);
    await digit('5');
    check(await valueOf('numeric-int') === '125', `Champ numérique sans SelectOnFocus : « ${await valueOf('numeric-int')} » au lieu de 125 (le chiffre doit s'ajouter).`);
    await tab();

    // A real mouse click gives the focus: the whole value is selected and the selection survives the mouseup.
    await clickOn(focused);
    check(await isActive('numeric-select-on-focus'), 'Le clic n\'a pas donné le focus au champ.');
    state = await selected('numeric-select-on-focus');
    check(state.text === '42', `Après un vrai clic, la sélection du champ numérique est ${JSON.stringify(state)} au lieu de « 42 ».`);
    await digit('5');
    check(await valueOf('numeric-select-on-focus') === '5', `Après un clic puis « 5 », le champ numérique vaut « ${await valueOf('numeric-select-on-focus')} » au lieu de 5.`);

    // A second click in the focused field places the caret instead of selecting all: the next digit is inserted.
    await clickOn(focused);
    state = await selected('numeric-select-on-focus');
    check(state.text === '', `Un second clic dans le champ focalisé sélectionne encore : ${JSON.stringify(state)}.`);
    await digit('7');
    check(await valueOf('numeric-select-on-focus') === '57', `Après un second clic puis « 7 », le champ numérique vaut « ${await valueOf('numeric-select-on-focus')} » au lieu de 57.`);

    // Leaving the field commits: the example shows the bound value.
    await tab();
    await echoIs(echo, 'Bound value: 57');
    step(`champ numérique à ${width} px : un vrai clic sélectionne tout (« 5 » remplace 42), un second clic pose le curseur (« 7 » s'ajoute : 57), le champ témoin sans SelectOnFocus garde son comportement, la valeur liée suit à la sortie`);

    // Entering by Tab and by Shift+Tab selects all, typing replaces the value, and the bound value follows.
    await evaluate(`${field('numeric-nullable')}.focus()`);
    await tab();
    check(await isActive('numeric-select-on-focus'), 'Tab n\'arrive pas dans le champ numérique à SelectOnFocus.');
    state = await selected('numeric-select-on-focus');
    check(state.text === '57', `Après Tab, la sélection du champ numérique est ${JSON.stringify(state)} au lieu de « 57 ».`);
    await digit('8');
    check(await valueOf('numeric-select-on-focus') === '8', `Après Tab puis « 8 », le champ numérique vaut « ${await valueOf('numeric-select-on-focus')} ».`);
    await tab();
    await echoIs(echo, 'Bound value: 8');
    await tab(true);
    check(await isActive('numeric-select-on-focus'), 'Maj+Tab n\'arrive pas dans le champ numérique à SelectOnFocus.');
    state = await selected('numeric-select-on-focus');
    check(state.text === '8', `Après Maj+Tab, la sélection du champ numérique est ${JSON.stringify(state)} au lieu de « 8 ».`);
    await digit('9');
    check(await valueOf('numeric-select-on-focus') === '9', `Après Maj+Tab puis « 9 », le champ numérique vaut « ${await valueOf('numeric-select-on-focus')} ».`);
    await tab();
    await echoIs(echo, 'Bound value: 9');

    // A programmatic focus selects all too.
    await evaluate(`${field('numeric-select-on-focus')}.focus()`);
    state = await selected('numeric-select-on-focus');
    check(state.text === '9', `Après focus(), la sélection du champ numérique est ${JSON.stringify(state)} au lieu de « 9 ».`);
    await digit('3');
    check(await valueOf('numeric-select-on-focus') === '3', `Après focus() puis « 3 », le champ numérique vaut « ${await valueOf('numeric-select-on-focus')} ».`);
    await tab();
    await echoIs(echo, 'Bound value: 3');
    step(`champ numérique à ${width} px : Tab, Maj+Tab et focus() sélectionnent tout, la frappe remplace la valeur et la valeur liée l'affiche à la sortie`);

    // ----- Text fields: OmniTextBox, selection read back --------------------------------------------------------------
    await session.visit('/components/theme', '#demo-appearance-window');
    await session.visit('/components/text-fields', '#text-box-select-on-focus');
    await evaluate(`${field('text-box-select-on-focus')}.scrollIntoView({ block: 'center' })`);
    const text = '#text-box-select-on-focus';
    check(await valueOf('text-box-select-on-focus') === 'Camille Durand', `Valeur de départ du champ texte inattendue : ${await valueOf('text-box-select-on-focus')}.`);

    await clickOn(text);
    state = await selected('text-box-select-on-focus');
    check(state.start === 0 && state.end === 14, `Après un vrai clic, la sélection du champ texte est ${JSON.stringify(state)} au lieu de 0 à 14.`);
    await press('z');
    check(await valueOf('text-box-select-on-focus') === 'z', `Après un clic puis « z », le champ texte vaut « ${await valueOf('text-box-select-on-focus')} ».`);
    await clickOn(text);
    state = await selected('text-box-select-on-focus');
    check(state.start === state.end, `Un second clic dans le champ texte focalisé sélectionne encore : ${JSON.stringify(state)}.`);
    await press('y');
    check(await valueOf('text-box-select-on-focus') === 'zy', `Après un second clic puis « y », le champ texte vaut « ${await valueOf('text-box-select-on-focus')} » au lieu de zy.`);
    await tab();
    await waitFor('la valeur liée du champ texte', "document.getElementById('text-box-select-on-focus-echo').textContent.includes('\"zy\"')");

    // Tab entry selects the text too; and the field without the parameter keeps the caret where a click puts it.
    await evaluate(`${field('bare-text-box')}.focus()`);
    await tab(true);
    check(await isActive('text-box-select-on-focus'), 'Maj+Tab n\'arrive pas dans le champ texte à SelectOnFocus.');
    state = await selected('text-box-select-on-focus');
    check(state.start === 0 && state.end === 2, `Après Maj+Tab, la sélection du champ texte est ${JSON.stringify(state)} au lieu de 0 à 2.`);
    await evaluate('document.activeElement.blur()');

    await clickOn('#bare-text-box');
    await press('a');
    await press('b');
    await tab();
    await clickOn('#bare-text-box');
    state = await selected('bare-text-box');
    check(state.start === state.end && await valueOf('bare-text-box') === 'ab', `Le champ texte sans SelectOnFocus sélectionne tout après un clic : ${JSON.stringify(state)}.`);
    step(`champ texte à ${width} px : un vrai clic sélectionne tout (0 à 14), un second clic pose le curseur, Maj+Tab sélectionne tout, le champ témoin sans SelectOnFocus ne sélectionne rien au clic`);

    // ----- The same script on the text area and the password (the demos give them no SelectOnFocus) ------------------------
    await evaluate(`import('./_content/OmniEurope.Blazor/omni-select-on-focus.js').then(module => { window.__selectOnFocus = module; module.attach(${field('text-area')}); module.attach(${field('password')}); })`);
    await clickOn('#text-area');
    await press('q');
    await press('r');
    await tab();
    await clickOn('#text-area');
    state = await selected('text-area');
    check(state.start === 0 && state.end === 2, `Après un vrai clic, la sélection de la zone de texte est ${JSON.stringify(state)} au lieu de 0 à 2.`);
    await clickOn('#text-area');
    state = await selected('text-area');
    check(state.start === state.end, `Un second clic dans la zone de texte focalisée sélectionne encore : ${JSON.stringify(state)}.`);
    await clickOn('#password');
    await press('s');
    await press('t');
    await tab();
    await clickOn('#password');
    state = await selected('password');
    check(state.start === 0 && state.end === 2, `Après un vrai clic, la sélection du mot de passe est ${JSON.stringify(state)} au lieu de 0 à 2.`);
    // Detached, the field is a plain one again.
    await evaluate(`window.__selectOnFocus.detach(${field('password')}); window.__selectOnFocus.detach(${field('text-area')})`);
    await tab();
    await clickOn('#password');
    state = await selected('password');
    check(state.start === state.end, `Détaché, le mot de passe sélectionne encore au clic : ${JSON.stringify(state)}.`);
    await pause(100);
    step(`zone de texte et mot de passe à ${width} px : le même script, posé à la main, sélectionne tout au vrai clic, un second clic pose le curseur, et détaché le champ redevient ordinaire`);
  }
}
