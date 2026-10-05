// The HTML editor page (/composants/editeur): every script path of omni-html-editor.js and its
// html-editor/ parts that a reader reaches with the toolbar, the keyboard, the mouse, the clipboard,
// the proofreader, the suggestions, the inline elements of an extension and the table commands.
// Each step acts as a reader would and asserts the document the editor leaves.

export async function editorSteps(session, results) {
  const { evaluate, waitFor, click, key, type, pause, check, send, centerOf } = session;

  // Records a step and stops at the first one after which the console holds an error, naming it.
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  const surface = id => `document.getElementById(${JSON.stringify(id)})`;
  const html = id => evaluate(`${surface(id)}.innerHTML`);

  // Selects the first occurrence of a text in the surface (collapsed at its start or end when asked).
  const select = (id, text, collapse = null) => evaluate(`(() => {
    const root = ${surface(id)};
    root.focus({ preventScroll: true });
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
      const at = node.data.indexOf(${JSON.stringify(text)});
      if (at < 0) continue;
      const range = document.createRange();
      range.setStart(node, at);
      range.setEnd(node, at + ${text.length});
      if (${JSON.stringify(collapse)} !== null) range.collapse(${JSON.stringify(collapse)} === 'start');
      const selection = getSelection();
      selection.removeAllRanges();
      selection.addRange(range);
      document.dispatchEvent(new Event('selectionchange'));
      return true;
    }
    throw new Error('Texte introuvable dans ' + ${JSON.stringify(id)} + ' : ' + ${JSON.stringify(text)});
  })()`);

  // Puts the caret at the start or the end of the element a selector finds inside the surface.
  const caretIn = (id, selector, atEnd = false) => evaluate(`(() => {
    const root = ${surface(id)};
    const element = root.querySelector(${JSON.stringify(selector)});
    if (!element) throw new Error('Introuvable dans ' + ${JSON.stringify(id)} + ' : ' + ${JSON.stringify(selector)});
    root.focus({ preventScroll: true });
    const range = document.createRange();
    range.selectNodeContents(element);
    range.collapse(!${atEnd});
    const selection = getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
    document.dispatchEvent(new Event('selectionchange'));
    return true;
  })()`);

  // Runs a toolbar command the way a reader does: a real click on the button when it is shown, through
  // the overflow menu when the one-row toolbar moved it there.
  const command = async (id, name) => {
    const target = await evaluate(`(() => {
      const editor = ${surface(id)}.closest('.omni-html-editor');
      const button = editor.querySelector('[data-command=${JSON.stringify(name)}]');
      if (!button) throw new Error('Commande absente de ' + ${JSON.stringify(id)} + ' : ' + ${JSON.stringify(name)});
      if (button.disabled) throw new Error('Commande désactivée dans ' + ${JSON.stringify(id)} + ' : ' + ${JSON.stringify(name)});
      const box = button.getBoundingClientRect();
      const shown = button.offsetParent !== null && box.width > 0 && box.top >= 0 && box.bottom <= innerHeight;
      if (!shown) return { overflow: true };
      return { x: box.left + box.width / 2, y: box.top + box.height / 2 };
    })()`);
    if (!target.overflow) {
      await click(target);
      return;
    }

    // A one-row toolbar keeps the trailing commands in its « More actions » menu.
    await click(await centerOf(`#${id}-toolbar .omni-html-editor__more .omni-overflow-menu__trigger, .omni-html-editor:has(#${id}) .omni-html-editor__more .omni-overflow-menu__trigger`));
    await menuItem(`[role="menu"] [data-command="${name}"]`);
  };
  const enabled = (id, name) => waitFor(`la commande ${name} active dans ${id}`, `!${surface(id)}.closest('.omni-html-editor').querySelector('[data-command=${JSON.stringify(name)}]').disabled`);
  const choose = (id, name, value) => evaluate(`(() => {
    const select = ${surface(id)}.closest('.omni-html-editor').querySelector('select[data-command=${JSON.stringify(name)}]');
    select.value = ${JSON.stringify(value)};
    select.dispatchEvent(new Event('change', { bubbles: true }));
  })()`);
  const scrollTo = id => evaluate(`${surface(id)}.scrollIntoView({ block: 'center' })`);
  const rightClickText = async (id, text) => {
    const point = await evaluate(`(() => {
      const root = ${surface(id)};
      const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
      for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        const at = node.data.indexOf(${JSON.stringify(text)});
        if (at < 0) continue;
        const range = document.createRange();
        range.setStart(node, at + 1);
        range.setEnd(node, at + 2);
        const box = range.getBoundingClientRect();
        return { x: box.left + box.width / 2, y: box.top + box.height / 2 };
      }
      throw new Error('Texte introuvable : ' + ${JSON.stringify(text)});
    })()`);
    await click(point, { button: 'right' });
  };
  const menuItem = async selector => {
    await waitFor(`l'entrée de menu ${selector}`, `[...document.querySelectorAll(${JSON.stringify(selector)})].some(item => item.offsetParent !== null)`);
    // The menu is placed by its script after it renders: the point is read once it stops moving.
    const point = () => evaluate(`(() => { const item = [...document.querySelectorAll(${JSON.stringify(selector)})].find(candidate => candidate.offsetParent !== null); const box = item.getBoundingClientRect(); return { x: Math.round(box.left + box.width / 2), y: Math.round(box.top + box.height / 2) }; })()`);
    let previous = await point();
    for (let tries = 0; tries < 20; tries++) {
      await pause(100);
      const next = await point();
      if (next.x === previous.x && next.y === previous.y) break;
      previous = next;
    }
    await click(previous);
  };

  await session.visit('/composants/editeur', '#editor-extended');
  await waitFor('les surfaces prêtes', `document.querySelectorAll('.omni-html-editor__surface[contenteditable="true"]').length >= 5`);

  // 1. #editor-body, inline formatting and blocks.
  await scrollTo('editor-body');
  await select('editor-body', 'dossier');
  await command('editor-body', 'inline-code');
  await waitFor('le code en ligne', `${surface('editor-body')}.querySelector('code')?.textContent === 'dossier'`);
  await caretIn('editor-body', 'code', true);
  await select('editor-body', 'dossier', 'start');
  await command('editor-body', 'inline-code');
  await waitFor('le code en ligne retiré', `${surface('editor-body')}.querySelector('code') === null`);
  step('code en ligne posé puis retiré');

  await caretIn('editor-body', 'p');
  await command('editor-body', 'align-center');
  await waitFor('le paragraphe centré', `${surface('editor-body')}.querySelector('p.omni-align-center') !== null`);
  await select('editor-body', 'dossier');
  await command('editor-body', 'bold');
  await waitFor('le gras', `${surface('editor-body')}.querySelector('b, strong') !== null`);
  await evaluate(`(() => { const root = ${surface('editor-body')}; root.focus(); const range = document.createRange(); range.selectNodeContents(root); const selection = getSelection(); selection.removeAllRanges(); selection.addRange(range); document.dispatchEvent(new Event('selectionchange')); })()`);
  await command('editor-body', 'clear-formatting');
  await waitFor('la mise en forme effacée', `${surface('editor-body')}.querySelector('b, strong, .omni-align-center') === null`);
  step('centrage, gras puis mise en forme effacée');

  // A quote holding an inline element (the bold word) is unwrapped back into a paragraph that keeps it.
  await select('editor-body', 'reçu');
  await command('editor-body', 'bold');
  await waitFor('le mot en gras', `${surface('editor-body')}.querySelector('p:last-of-type b, p:last-of-type strong') !== null`);
  await caretIn('editor-body', 'p:last-of-type');
  await command('editor-body', 'quote');
  await waitFor('la citation', `${surface('editor-body')}.querySelector('blockquote') !== null`);
  await caretIn('editor-body', 'blockquote');
  await command('editor-body', 'quote');
  await waitFor('la citation retirée', `${surface('editor-body')}.querySelector('blockquote') === null && ${surface('editor-body')}.querySelector('p b, p strong') !== null`);
  step('citation posée puis retirée');

  await caretIn('editor-body', 'p:last-of-type', true);
  await command('editor-body', 'link');
  await waitFor('le champ du lien', `document.getElementById('editor-body-link')?.offsetParent != null`);
  await evaluate("document.getElementById('editor-body-link').focus()");
  await type('https://example.eu/?a=1&b="2"');
  await key('Enter', 'Enter', 13);
  await waitFor('le lien inséré au curseur', `${surface('editor-body')}.querySelector('a[href^="https://example.eu/"][rel~="noopener"]') !== null`);
  const link = await evaluate(`${surface('editor-body')}.querySelector('a[href^="https://example.eu/"]').getAttribute('href')`);
  check(link === 'https://example.eu/?a=1&b="2"', `Lien mal échappé : ${link}.`);
  step('lien inséré au curseur, adresse échappée');

  // Backspace at the start of a paragraph joins it to the previous one.
  const before = await evaluate(`${surface('editor-body')}.querySelectorAll('p').length`);
  await caretIn('editor-body', 'p:nth-of-type(2)');
  await key('Backspace', 'Backspace', 8);
  await waitFor('les paragraphes joints', `${surface('editor-body')}.querySelectorAll('p').length === ${before - 1}`);
  check(!(await html('editor-body')).includes('style='), 'La jonction a laissé un attribut style.');
  step('Retour arrière joint deux paragraphes sans style');

  // A paste and a drop of HTML blocks, sanitized by .NET then inserted as blocks.
  await caretIn('editor-body', 'p', true);
  await evaluate(`(() => {
    const data = new DataTransfer();
    data.setData('text/html', '<p>Bloc collé A</p><p>Bloc collé B</p><script>window.__pasted = 1</script>');
    data.setData('text/plain', 'Bloc collé A');
    ${surface('editor-body')}.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
  })()`);
  await waitFor('les blocs collés', `${surface('editor-body')}.textContent.includes('Bloc collé B')`);
  check(!(await html('editor-body')).includes('script'), 'Le collage a gardé un script.');
  const dropPoint = await centerOf('#editor-body p');
  await evaluate(`(() => {
    const data = new DataTransfer();
    data.setData('text/html', '<p>Bloc déposé</p>');
    ${surface('editor-body')}.dispatchEvent(new DragEvent('drop', { dataTransfer: data, clientX: ${dropPoint.x}, clientY: ${dropPoint.y}, bubbles: true, cancelable: true }));
  })()`);
  await waitFor('le bloc déposé', `${surface('editor-body')}.textContent.includes('Bloc déposé')`);
  step('collage et dépôt nettoyés puis insérés en blocs');

  // A host command that sets the whole document, then Undo with the surface focused.
  const signed = await evaluate(`${surface('editor-body')}.querySelectorAll('p').length`);
  await caretIn('editor-body', 'p', true);
  await command('editor-body', 'signature');
  await waitFor('la signature ajoutée', `${surface('editor-body')}.querySelectorAll('p').length === ${signed + 1}`);
  await caretIn('editor-body', 'p', true);
  await command('editor-body', 'undo');
  await waitFor('la signature annulée', `${surface('editor-body')}.querySelectorAll('p').length === ${signed}`);
  step('commande de l\'hôte puis Annuler');

  // A list asked inside a paragraph is lifted out of it.
  await caretIn('editor-body', 'p:last-of-type');
  await command('editor-body', 'bullet-list');
  await waitFor('la liste', `${surface('editor-body')}.querySelector('ul') !== null`);
  check(await evaluate(`${surface('editor-body')}.querySelector('p ul, p ol') === null`), 'Une liste reste dans un paragraphe.');
  await caretIn('editor-body', 'li', true);
  await command('editor-body', 'bullet-list');
  await waitFor('la liste retirée', `${surface('editor-body')}.querySelector('ul, li') === null`);
  step('liste posée hors paragraphe puis retirée');

  // 2. #editor-source, the same value in source mode: Bold wraps the selection of the text area.
  await scrollTo('editor-source');
  const area = '#editor-source';
  await evaluate(`(() => { const area = document.querySelector(${JSON.stringify(area)}); area.focus(); const at = area.value.indexOf('Bonjour') >= 0 ? area.value.indexOf('Bonjour') : area.value.indexOf('<p>') + 3; area.setSelectionRange(at, at + 4); })()`);
  const sourceBefore = await evaluate(`document.querySelector(${JSON.stringify(area)}).value`);
  await click(await evaluate(`(() => { const button = document.querySelector(${JSON.stringify(area)}).closest('.omni-html-editor').querySelector('[data-command="bold"]'); const box = button.getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`));
  await waitFor('le gras dans la source', `document.querySelector(${JSON.stringify(area)}).value.length > ${sourceBefore.length}`);
  const wrapped = await evaluate(`(() => { const area = document.querySelector(${JSON.stringify(area)}); return { value: area.value, start: area.selectionStart, end: area.selectionEnd, focused: document.activeElement === area }; })()`);
  check(/<(strong|b)>/.test(wrapped.value) && wrapped.end - wrapped.start === 4 && wrapped.focused, `Sélection de la source mal enveloppée : ${JSON.stringify(wrapped)}.`);
  step('source : sélection enveloppée et rendue');

  // 3. #editor-extended: case, proofreading, suggestions, inline element and context menu.
  await scrollTo('editor-extended');
  await select('editor-extended', 'correcteur');
  await choose('editor-extended', 'change-case', 'upper');
  await waitFor('la casse changée', `${surface('editor-extended')}.textContent.includes('CORRECTEUR')`);
  await select('editor-extended', 'douteuse');
  await choose('editor-extended', 'change-case', 'title');
  await waitFor('les initiales en capitale', `${surface('editor-extended')}.textContent.includes('Douteuse')`);
  step('casse changée');

  await waitFor('la faute soulignée sur son texte', "[...(CSS.highlights?.get('omni-proofreading-spelling') ?? [])].some(range => range.toString() === 'ortografe')", 20_000);
  await rightClickText('editor-extended', 'ortografe');
  await menuItem('[data-proofreading="suggestion"]');
  await waitFor('la suggestion appliquée', `${surface('editor-extended')}.textContent.includes('orthographe')`);
  await waitFor('la répétition relevée', "[...(CSS.highlights.get('omni-proofreading-grammar') ?? CSS.highlights.get('omni-proofreading-spelling') ?? [])].length > 0 || [...CSS.highlights.keys()].some(name => name.startsWith('omni-proofreading') && CSS.highlights.get(name).size > 0)");
  const issuesBefore = await evaluate("[...CSS.highlights.keys()].filter(name => name.startsWith('omni-proofreading')).reduce((sum, name) => sum + CSS.highlights.get(name).size, 0)");
  await rightClickText('editor-extended', 'répété répété');
  await menuItem('[data-proofreading="ignore"]');
  await waitFor('la répétition ignorée', `[...CSS.highlights.keys()].filter(name => name.startsWith('omni-proofreading')).reduce((sum, name) => sum + CSS.highlights.get(name).size, 0) < ${issuesBefore}`);
  await select('editor-extended', 'Douteuse', 'end');
  await type(' ortografe');
  await waitFor('la nouvelle faute relevée', "[...(CSS.highlights.get('omni-proofreading-spelling') ?? [])].some(range => range.toString() === 'ortografe')", 20_000);
  await rightClickText('editor-extended', ' ortografe');
  await menuItem('[data-proofreading="ignore-all"]');
  // Ignore all forgets the answers and asks again: the word is no longer flagged anywhere.
  await waitFor('tout ignoré puis revérifié', "![...(CSS.highlights.get('omni-proofreading-spelling') ?? [])].some(range => range.toString().trim() === 'ortografe')", 20_000);
  step('correcteur : remplacer, ignorer, tout ignorer');

  await caretIn('editor-extended', 'p:last-of-type', true);
  await type(' sous réserve');
  await waitFor('la suggestion de texte', `${surface('editor-extended')}.closest('.omni-html-editor').querySelector('.omni-html-editor__suggestion')?.textContent.includes('vérification')`, 10_000);
  await key('Tab', 'Tab', 9);
  await waitFor('la suggestion acceptée', `${surface('editor-extended')}.querySelector('p:last-of-type').textContent.trimEnd().endsWith('sous réserve de vérification') && document.querySelector('.omni-html-editor__suggestion') === null`);
  step('suggestion proposée puis acceptée par Tab');

  await click(await centerOf('#editor-extended .demo-note'));
  await waitFor('la note marquée relue', `${surface('editor-extended')}.querySelector('.demo-note')?.getAttribute('data-state') === 'read'`);
  const note = await centerOf('#editor-extended .demo-note');
  await click(note, { button: 'right' });
  await menuItem('[data-command="demo-remove-note"]');
  await waitFor('la note retirée', `${surface('editor-extended')}.querySelector('.demo-note') === null`);
  await evaluate("document.activeElement?.blur()");
  step('note relue au clic, retirée par le menu contextuel');

  // 4. #editor-annotated: selection path, clipboard and table commands.
  await scrollTo('editor-annotated');
  await click(await centerOf('#editor-annotated td'));
  await waitFor('le chemin de la sélection', "document.getElementById('editor-annotated-path').textContent.includes('td')");
  await enabled('editor-annotated', 'add-row-below');
  const cell = async (selector, atEnd = false) => {
    await caretIn('editor-annotated', selector, atEnd);
    await enabled('editor-annotated', 'delete-row');
  };
  const table = () => evaluate(`(() => { const table = ${surface('editor-annotated')}.querySelector('table'); return table ? { rows: table.rows.length, cells: [...table.rows].map(row => row.cells.length), spans: [...table.querySelectorAll('[colspan],[rowspan]')].map(c => (c.getAttribute('colspan') ?? '1') + 'x' + (c.getAttribute('rowspan') ?? '1')) } : null; })()`);

  await cell('td');
  await command('editor-annotated', 'add-row-below');
  await waitFor('la ligne ajoutée', `${surface('editor-annotated')}.querySelector('table').rows.length === 3`);
  await cell('td');
  await command('editor-annotated', 'add-column-after');
  await waitFor('la colonne ajoutée', `${surface('editor-annotated')}.querySelector('table').rows[0].cells.length === 3`);
  await cell('tr:nth-child(2) td');
  await command('editor-annotated', 'merge-cell-right');
  await waitFor('la fusion à droite', `${surface('editor-annotated')}.querySelector('[colspan="2"]') !== null`);
  await cell('[colspan="2"]');
  await command('editor-annotated', 'split-cell');
  await waitFor('la cellule scindée', `${surface('editor-annotated')}.querySelector('[colspan]') === null`);
  await cell('tr:nth-child(2) td');
  await command('editor-annotated', 'merge-cell-down');
  await waitFor('la fusion vers le bas', `${surface('editor-annotated')}.querySelector('[rowspan="2"]') !== null`);
  await cell('tr:nth-child(3) td:last-child');
  await command('editor-annotated', 'delete-column');
  await waitFor('la colonne supprimée', `${surface('editor-annotated')}.querySelector('table').rows[0].cells.length === 2`);
  await cell('tr:nth-child(2) td');
  await command('editor-annotated', 'add-row-above');
  await waitFor('la ligne au-dessus', `${surface('editor-annotated')}.querySelector('table').rows.length === 4`);
  await cell('th');
  await command('editor-annotated', 'add-column-before');
  await waitFor('la colonne avant', `${surface('editor-annotated')}.querySelector('table').rows[0].cells.length === 3`);
  const shaped = await table();
  await cell('th');
  await key('Tab', 'Tab', 9);
  await waitFor('Tab vers la cellule suivante', "getSelection().anchorNode?.parentElement?.closest('th, td') === document.querySelector('#editor-annotated th:nth-child(2)') || getSelection().anchorNode?.closest?.('th, td') === document.querySelector('#editor-annotated th:nth-child(2)')");
  for (let guard = 0; guard < 8 && (await table()); guard++) {
    await cell('tr td, tr th');
    await command('editor-annotated', 'delete-row');
    await pause(150);
  }
  check((await table()) === null, 'Supprimer chaque ligne n\'a pas retiré le tableau.');
  await caretIn('editor-annotated', 'p', true);
  await command('editor-annotated', 'insert-table');
  await waitFor('le tableau inséré', `${surface('editor-annotated')}.querySelector('table')?.rows.length === 3`);
  step(`tableau : lignes, colonnes, fusion et scission (${JSON.stringify(shaped.cells)}), Tab, suppression puis insertion`);

  // The clipboard commands: copy a selection, then paste through the asynchronous clipboard.
  const origin = new URL(await evaluate('location.href')).origin;
  await send('Browser.grantPermissions', { origin, permissions: ['clipboardReadWrite', 'clipboardSanitizedWrite'] });
  await send('Emulation.setFocusEmulationEnabled', { enabled: true });
  await select('editor-annotated', await evaluate(`${surface('editor-annotated')}.querySelector('p').textContent.trim().split(/\\s+/)[0]`));
  await command('editor-annotated', 'copy');
  await evaluate("navigator.clipboard.write([new ClipboardItem({ 'text/html': new Blob(['<p>Presse A</p><p>Presse B</p>'], { type: 'text/html' }), 'text/plain': new Blob(['Presse A'], { type: 'text/plain' }) })])");
  await caretIn('editor-annotated', 'p', true);
  await command('editor-annotated', 'paste');
  await waitFor('le presse-papiers collé', `${surface('editor-annotated')}.textContent.includes('Presse B')`);
  await send('Emulation.setFocusEmulationEnabled', { enabled: false });
  step('copier puis coller par le presse-papiers');

  // 5. #editor-document: the size select and the HTML export of the status bar.
  await scrollTo('editor-document');
  await select('editor-document', 'important');
  await choose('editor-document', 'font-size', 'large');
  await waitFor('la taille appliquée', `${surface('editor-document')}.querySelector('span.omni-font-size-large') !== null && ${surface('editor-document')}.querySelector('font') === null`);
  // The caret inside the sized text: the toolbar reads the size back at the caret.
  await caretIn('editor-document', 'span.omni-font-size-large');
  await waitFor('la taille relue au curseur', `${surface('editor-document')}.closest('.omni-html-editor').querySelector('select[data-command="font-size"]').value === 'large'`);
  await caretIn('editor-document', 'p');
  await command('editor-document', 'align-right');
  await waitFor('l\'alignement à droite', `${surface('editor-document')}.querySelector('.omni-align-end') !== null`);
  step('taille et alignement du document');

  const downloads = [];
  const stop = session.on('Page.downloadWillBegin', params => downloads.push(params.suggestedFilename));
  const stopBrowser = session.on('Browser.downloadWillBegin', params => downloads.push(params.suggestedFilename));
  await send('Browser.setDownloadBehavior', { behavior: 'deny', eventsEnabled: true }).catch(() => send('Page.setDownloadBehavior', { behavior: 'deny' }));
  await click(await centerOf('#editor-document ~ * [data-export="html"], [data-export="html"]'));
  const until = Date.now() + 10_000;
  while (downloads.length === 0 && Date.now() < until) await pause(100);
  stop();
  stopBrowser();
  check(downloads.some(name => name.endsWith('.html')), `Export HTML sans téléchargement : ${JSON.stringify(downloads)}.`);
  await send('Browser.setDownloadBehavior', { behavior: 'default' }).catch(() => {});
  step(`export ${downloads[0]}`);
}
