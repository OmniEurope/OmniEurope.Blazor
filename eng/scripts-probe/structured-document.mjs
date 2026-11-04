// The structured document editor (OmniDocumentEditor, PLAN-010 lot 3), driven in a real Chromium on its showcase page
// with real input: a run edited in place by typing (the text the reader typed survives the render that reports it, Enter
// makes no paragraph), a date chosen in the calendar a field opens, a choice made in a list, a free text typed into a
// list that allows it, a number typed, a drawing made in the dialog and applied, the read-only mode, and the change set
// the page shows after each edit. The change set is read from the text the example writes below the editor.

export async function structuredDocumentSteps(session, results) {
  const { evaluate, waitFor, check, clickOn, key, type, mouse, pause } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  const editor = '#structured-demo';
  const run = id => `${editor} [data-omni-run="${id}"]`;
  const changes = () => evaluate("document.getElementById('structured-changes')?.textContent ?? ''");
  const waitForChange = async (description, fragment) => {
    try {
      await waitFor(description, `(document.getElementById('structured-changes')?.textContent ?? '').replace(/\\s+/g, ' ').includes(${JSON.stringify(fragment)})`);
    } catch (error) {
      throw new Error(`${error.message} Jeu de changements : ${(await changes()).replace(/\s+/g, ' ')}`);
    }
  };
  const runText = id => evaluate(`document.querySelector(${JSON.stringify(run(id))}).textContent`);
  const chip = label => evaluate(`(() => { const chip = [...document.querySelectorAll(${JSON.stringify(editor + ' .omni-document-editor__chip')})].find(element => element.querySelector('.omni-document-editor__chip-label')?.textContent.trim() === ${JSON.stringify(label)}); if (!chip) return null; chip.setAttribute('data-probe-chip', ${JSON.stringify(label)}); return true; })()`);
  const openChip = async label => {
    check(await chip(label), `Le champ « ${label} » est introuvable.`);
    await clickOn(`${editor} [data-probe-chip="${label}"]`);
    // Open means what a reader gets: the input of the field holds the focus (the popover script moves it in once
    // attached). Escape sent before that reaches the chip, which does not close the popover.
    await waitFor(`le champ « ${label} » ouvert, sa saisie au focus`, `document.querySelector(${JSON.stringify(editor + ' .omni-document-editor__editor')})?.contains(document.activeElement) === true`);
  };
  const closeChip = async label => {
    await key('Escape', 'Escape', 27);
    await waitFor(`le champ refermé après ${label ?? 'la saisie'}`, `document.querySelector(${JSON.stringify(editor + ' .omni-document-editor__editor')}) === null`);
  };
  const within = selector => `${editor} .omni-document-editor__editor ${selector}`;
  const chipValue = label => evaluate(`document.querySelector(${JSON.stringify(`${editor} [data-probe-chip="${label}"] .omni-document-editor__chip-value, ${editor} [data-probe-chip="${label}"] .omni-document-editor__chip-value--empty`)})?.textContent.trim() ?? null`);

  await session.visit('/components/structured-document', `${editor} [data-omni-run]`);
  await waitFor('le document dessiné', `document.querySelectorAll(${JSON.stringify(editor + ' [data-omni-run]')}).length === 9`);
  const editable = await evaluate(`document.querySelector(${JSON.stringify(run('order-title'))}).getAttribute('contenteditable')`);
  check(editable === 'plaintext-only', `Le texte est modifiable en « ${editable} », pas en plaintext-only.`);
  check((await changes()).replace(/\s+/g, ' ').includes('"runs": []'), `Le jeu de changements ne commence pas vide : ${await changes()}.`);

  // 1. Typing in a run: the change set carries the whole text of the run, the text typed survives the render that
  // reports it (the caret goes on where it was), and Enter makes no paragraph.
  await clickOn(run('order-title'));
  await key('End', 'End', 35);
  await type(' (urgent)');
  await waitForChange('le texte tapé dans le jeu de changements', '"runId": "order-title", "text": "Service order (urgent)"');
  await type('!');
  await waitForChange('la suite du texte', '"text": "Service order (urgent)!"');
  check((await runText('order-title')) === 'Service order (urgent)!', `Le texte tapé a été écrasé : « ${await runText('order-title')} ».`);
  const spans = await evaluate(`document.querySelectorAll(${JSON.stringify(editor + ' [data-omni-run]')}).length`);
  await key('Enter', 'Enter', 13);
  await pause(200);
  check((await runText('order-title')) === 'Service order (urgent)!', 'Entrée change le texte du passage.');
  check((await evaluate(`document.querySelectorAll(${JSON.stringify(editor + ' [data-omni-run]')}).length`)) === spans, 'Entrée crée un passage ou un paragraphe.');
  check((await evaluate(`document.querySelector(${JSON.stringify(run('order-title'))}).querySelector('br, div, p') === null`)), 'Entrée a inséré un saut de ligne dans le passage.');
  step('texte tapé dans un passage : rapporté en entier, jamais écrasé par le rendu, Entrée sans effet');

  // 2. A date: the field opens a date input whose calendar is not clipped by the popover; a day chosen is reported.
  await openChip('Delivery date');
  await clickOn(within('.omni-date__toggle'));
  await waitFor('le calendrier', "document.querySelector('.omni-calendar') !== null");
  const calendar = await evaluate("(() => { const b = document.querySelector('.omni-calendar').getBoundingClientRect(); return JSON.stringify({ left: b.left, top: b.top, right: b.right, bottom: b.bottom, width: innerWidth, height: innerHeight }); })()").then(JSON.parse);
  check(calendar.left >= 0 && calendar.top >= 0 && calendar.right <= calendar.width && calendar.bottom <= calendar.height, `Le calendrier sort de la fenêtre : ${JSON.stringify(calendar)}.`);
  await clickOn('.omni-calendar__day[data-date="2026-11-18"]');
  await waitForChange('la date choisie', '"fieldId": "order-date", "tag": "delivery-date", "value": "2026-11-18"');
  await closeChip('la date');
  const dateText = await chipValue('Delivery date');
  check(dateText?.includes('2026') && dateText.includes('18'), `Le champ n'affiche pas la date choisie : « ${dateText} ».`);
  step(`date choisie dans le calendrier du champ : rapportée (2026-11-18), affichée « ${dateText} »`);

  // 3. A list: a native select, the choice reported; a list that takes free text: a text typed and confirmed.
  await openChip('Priority');
  await evaluate(`(() => { const select = document.querySelector(${JSON.stringify(within('select'))}); select.value = [...select.options].find(option => option.textContent.trim() === 'High').value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await waitForChange('la priorité choisie', '"fieldId": "terms-priority-field", "tag": "priority", "value": "High"');
  await closeChip('la priorité');

  await openChip('Payment terms');
  await clickOn(within('input[role=combobox]'));
  await key('a', 'KeyA', 65, 2);
  await type('Net 45');
  await key('Enter', 'Enter', 13);
  await waitForChange('le texte libre de la liste', '"fieldId": "terms-payment-field", "tag": "payment-terms", "value": "Net 45"');
  await closeChip('les conditions');
  check((await chipValue('Payment terms')) === 'Net 45', `Le champ n'affiche pas « Net 45 » : « ${await chipValue('Payment terms')} ».`);
  step('liste : priorité « High » choisie, conditions « Net 45 » saisies librement, toutes deux rapportées et affichées');

  // 4. A number: typed, committed, reported.
  await openChip('Amount');
  await clickOn(within('input'));
  await key('a', 'KeyA', 65, 2);
  await type('2000');
  await key('Tab', 'Tab', 9);
  await waitForChange('le montant saisi', '"fieldId": "terms-total-field", "tag": "amount", "value": "2000"');
  // Tab left the panel, which Escape closes from inside only.
  await clickOn(within('input'));
  await closeChip('le montant');
  check(await evaluate("document.activeElement?.classList.contains('omni-document-editor__chip') === true"), 'Échap ne rend pas le focus au champ.');
  step('nombre saisi : rapporté (2000)');

  // 5. A drawing: the dialog around the pad, a stroke, Apply; the strokes are drawn under the paragraph and reported
  // with their PNG.
  const insert = `${editor} .omni-document-editor__block:nth-of-type(3) .omni-document-editor__draw`;
  await clickOn(insert);
  // A canvas is 300 pixels wide before any script: ready means omni-ink-canvas.js attached and sized it to its box,
  // and the dialog done moving in, so the stroke lands where it is aimed (measured: a stroke sent earlier was lost).
  await waitFor('la zone de dessin du dialogue, attachée', "(() => { const canvas = document.querySelector('.omni-dialog canvas.omni-ink-canvas__surface'); return canvas !== null && canvas.clientWidth > 0 && canvas.clientWidth !== 300 && canvas.width === Math.round(canvas.clientWidth * devicePixelRatio); })()");
  await waitFor('le dialogue immobile', "(() => { const box = document.querySelector('.omni-dialog canvas.omni-ink-canvas__surface').getBoundingClientRect(); const now = box.left + ',' + box.top + ',' + box.width; const same = window.__probeInkBox === now; window.__probeInkBox = now; return same; })()");
  check(await evaluate("(() => { const apply = [...document.querySelectorAll('.omni-dialog button')].find(button => button.classList.contains('omni-button--primary')); return apply?.disabled === true; })()"), 'Confirmer est actif sans aucun trait.');
  const frame = await evaluate("(() => { const b = document.querySelector('.omni-dialog canvas.omni-ink-canvas__surface').getBoundingClientRect(); return JSON.stringify({ left: b.left, top: b.top, width: b.width, height: b.height }); })()").then(JSON.parse);
  const at = (x, y) => ({ x: frame.left + frame.width * x, y: frame.top + frame.height * y });
  const stroke = [at(0.1, 0.5), at(0.3, 0.2), at(0.5, 0.7), at(0.7, 0.3), at(0.9, 0.6)];
  await mouse('mouseMoved', stroke[0], { button: 'none' });
  await mouse('mousePressed', stroke[0]);
  for (const point of stroke.slice(1)) await mouse('mouseMoved', point, { button: 'left', buttons: 1 });
  await mouse('mouseReleased', stroke.at(-1));
  await waitFor('Confirmer actif', "(() => { const apply = [...document.querySelectorAll('.omni-dialog button')].find(button => button.classList.contains('omni-button--primary')); return apply !== undefined && !apply.disabled; })()");
  await evaluate("[...document.querySelectorAll('.omni-dialog button')].find(button => button.classList.contains('omni-button--primary')).click()");
  await waitFor('le dialogue refermé', "document.querySelector('.omni-dialog') === null");
  await waitFor('le dessin sous le paragraphe', `document.querySelector(${JSON.stringify(editor + ' svg.omni-document-editor__sheet polyline')}) !== null`);
  await waitForChange('le dessin dans le jeu de changements', '"blockId": "signature", "strokes": 1');
  const png = Number.parseInt(/"pngBytes": (\d+)/.exec(await changes())?.[1] ?? '0', 10);
  check(png > 100, `Le PNG du dessin est absent ou vide (${png} octets).`);
  step(`dessin : tracé dans le dialogue, validé, affiché sous le paragraphe, rapporté avec son PNG (${png} octets)`);

  // 6. Read only: nothing can be typed, the fields open nothing, the drawing buttons are gone.
  await clickOn('[role=switch][aria-label="Read only"]');
  await waitFor('le mode lecture seule', `document.querySelector(${JSON.stringify(run('order-title'))}).getAttribute('contenteditable') !== 'plaintext-only'`);
  check(await evaluate(`document.querySelectorAll(${JSON.stringify(editor + ' .omni-document-editor__chip')}).length > 0 && [...document.querySelectorAll(${JSON.stringify(editor + ' .omni-document-editor__chip')})].every(chip => chip.tagName !== 'BUTTON')`), 'Un champ reste un bouton en lecture seule.');
  check(await evaluate(`document.querySelector(${JSON.stringify(editor + ' .omni-document-editor__draw')}) === null`), 'Le bouton de dessin reste en lecture seule.');
  const before = await changes();
  await clickOn(run('order-title'));
  await key('End', 'End', 35);
  await type('X');
  await pause(300);
  check((await runText('order-title')) === 'Service order (urgent)!', 'Un texte est saisi en lecture seule.');
  check((await changes()) === before, 'Le jeu de changements bouge en lecture seule.');
  await clickOn('[role=switch][aria-label="Read only"]');
  await waitFor('le mode édition', `document.querySelector(${JSON.stringify(run('order-title'))}).getAttribute('contenteditable') === 'plaintext-only'`);
  step('lecture seule : aucune saisie, champs sans bouton, dessin sans bouton, jeu de changements inchangé');

  // 7. Start over: a new document, the changes and the typed text gone.
  await clickOn('#structured-reset');
  await waitFor('le document de départ', `document.querySelector(${JSON.stringify(run('order-title'))})?.textContent === 'Service order'`);
  await waitForChange('le jeu de changements vide', '"runs": [], "fields": [], "drawings": []');
  check(await evaluate(`document.querySelector(${JSON.stringify(editor + ' svg.omni-document-editor__sheet')}) === null`), 'Le dessin reste après le retour au départ.');
  step('nouveau document : texte, champs et dessin remis à l\'état de départ, jeu de changements vide');

  // 8. Leaving the page releases the editor: its script is detached, and nothing is reported after it (the console
  // stays free of errors, which finish() checks).
  await session.visit('/components/ink-canvas', '#ink-demo canvas');
  check(await evaluate(`document.querySelector(${JSON.stringify(editor)}) === null`), "L'éditeur reste dans la page après son départ.");
  step('départ de la page : éditeur libéré, script détaché, console sans erreur');
}
