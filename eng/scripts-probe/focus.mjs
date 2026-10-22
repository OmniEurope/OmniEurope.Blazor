// The focus module's defects of the 2026-10-22 audit (lot 4), each driven in the browser: the focus given
// back to the button that opened a dialog from a dialog (L4-05), the arrows of a tab strip that follow a
// right-to-left reading (L4-11), and the calls to .NET that a lost circuit rejects without leaving an
// unhandled promise rejection (L4-06), and the failure of a column auto-fit that is reported, not swallowed.

export async function focusSteps(session, results) {
  const { evaluate, waitFor, key, pause, check, clickOn } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const focusedId = () => evaluate('document.activeElement?.id ?? ""');

  // 1. A dialog opened from a dialog: when it closes, the focus goes back to the button of the lower dialog that
  // opened it, not to the page body; the lower dialog then gives it back to the button that opened that one.
  await session.visit('/composants/superpositions', '#demo-dialog-sticky');
  await clickOn('#demo-dialog-sticky');
  await waitFor('la boîte collante ouverte', "document.getElementById('demo-dialog-nested') !== null");
  await pause(300);
  await clickOn('#demo-dialog-nested');
  await waitFor('la boîte bloquante ouverte par-dessus', "document.getElementById('demo-dialog-blocking-done') !== null");
  await pause(300);
  await clickOn('#demo-dialog-blocking-done');
  await waitFor('la boîte bloquante fermée', "document.getElementById('demo-dialog-blocking-done') === null");
  await waitFor('le focus rendu au bouton qui a ouvert la seconde boîte', "document.activeElement?.id === 'demo-dialog-nested'");
  await key('Escape', 'Escape', 27);
  await waitFor('la boîte collante fermée', "document.getElementById('demo-dialog-nested') === null");
  await waitFor('le focus rendu au bouton qui a ouvert la première boîte', "document.activeElement?.id === 'demo-dialog-sticky'");
  step('dialogue ouvert depuis un dialogue : le focus revient au bouton qui l\'a ouvert, puis au déclencheur');

  // 2. The arrows of a tab strip follow what the reader sees: right to left, the right arrow goes to the previous tab.
  await session.visit('/composants/navigation', '.omni-tabs__viewport[role="tablist"] [role="tab"]');
  const tabs = "[...document.querySelector('.omni-tabs__viewport[role=\"tablist\"]').querySelectorAll('[role=\"tab\"]')]";
  const keys = await evaluate(`${tabs}.map(tab => tab.dataset.key)`);
  check(keys.length >= 3, `Il faut trois onglets pour distinguer les deux sens, la page en montre ${keys.length}.`);
  const focusedKey = () => evaluate('document.activeElement?.dataset?.key ?? ""');
  await evaluate(`${tabs}[1].focus()`);
  await key('ArrowRight', 'ArrowRight', 39);
  await waitFor('l\'onglet suivant en lecture de gauche à droite', `document.activeElement?.dataset?.key === ${JSON.stringify(keys[2])}`);
  await evaluate("document.documentElement.dir = 'rtl'");
  try {
    await evaluate(`${tabs}[1].focus()`);
    await key('ArrowRight', 'ArrowRight', 39);
    await waitFor('l\'onglet précédent en lecture de droite à gauche', `document.activeElement?.dataset?.key === ${JSON.stringify(keys[0])}`);
    await evaluate(`${tabs}[1].focus()`);
    await key('ArrowLeft', 'ArrowLeft', 37);
    await waitFor('l\'onglet suivant avec la flèche gauche, de droite à gauche', `document.activeElement?.dataset?.key === ${JSON.stringify(keys[2])}`);
    check(await focusedKey() === keys[2], 'La flèche gauche en lecture de droite à gauche n\'a pas atteint l\'onglet suivant.');
  } finally {
    await evaluate("document.documentElement.dir = 'ltr'");
  }
  step('onglets : les flèches suivent le sens de lecture, de gauche à droite et de droite à gauche');

  // 3. A call to .NET that a lost circuit rejects leaves no unhandled promise rejection: the keyboard of a tab
  // strip, the narrow-window watcher and the Escape listener of a sidebar, the viewport report of a log viewer, each given a reference that rejects.
  const rejections = await evaluate(`(async () => {
    const seen = [];
    const onRejection = event => { seen.push(String(event.reason?.message ?? event.reason)); event.preventDefault(); };
    window.addEventListener('unhandledrejection', onRejection);
    const focus = await import('/_content/OmniEurope.Blazor/omni-focus.js');
    const lost = { invokeMethodAsync: () => Promise.reject(new Error('circuit perdu')) };
    const strip = document.createElement('div');
    strip.setAttribute('role', 'tablist');
    strip.innerHTML = '<button role="tab" data-key="a">a</button><button role="tab" data-key="b">b</button>';
    document.body.append(strip);
    focus.configureTabs(strip, lost);
    strip.firstElementChild.focus();
    strip.firstElementChild.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    const owner = {};
    focus.watchNarrow(owner, lost);
    focus.attachEscape(owner, lost);
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    // The log viewer reports its viewport on the next frame after a scroll: the report is rejected too.
    const logViewer = await import('/_content/OmniEurope.Blazor/omni-log-viewer.js');
    const viewport = document.createElement('div');
    // Sized through the CSSOM: an inline style attribute is refused by the strict CSP.
    viewport.style.height = '40px';
    viewport.style.overflow = 'auto';
    const content = document.createElement('div');
    content.style.height = '400px';
    viewport.append(content);
    document.body.append(viewport);
    logViewer.attach(viewport, lost, true);
    viewport.scrollTop = 100;
    viewport.dispatchEvent(new Event('scroll'));
    await new Promise(resolve => setTimeout(resolve, 400));
    focus.unwatchNarrow(owner);
    logViewer.detach(viewport);
    viewport.remove();
    focus.detachEscape(owner);
    focus.disposeTabs(strip);
    strip.remove();
    window.removeEventListener('unhandledrejection', onRejection);
    return seen;
  })()`);
  check(rejections.length === 0, `Rejets de promesse non gérés après un circuit perdu : ${JSON.stringify(rejections)}.`);
  step('appels vers .NET d\'un circuit perdu : aucun rejet de promesse non géré');

  // 4. The auto-fit of a column asks .NET for its texts: while the attachment is live, a rejection is reported
  // by one console line and nothing more (no unhandled rejection); a grid detached while .NET answers is silent.
  const autoFit = await evaluate(`(async () => {
    const seen = [];
    const lines = [];
    const onRejection = event => { seen.push(String(event.reason?.message ?? event.reason)); event.preventDefault(); };
    window.addEventListener('unhandledrejection', onRejection);
    const original = console.error;
    console.error = (...parts) => lines.push(parts.map(String).join(' '));
    const grid = await import('/_content/OmniEurope.Blazor/omni-grid.js');
    const viewport = document.createElement('div');
    document.body.append(viewport);
    grid.attachResize(viewport, { invokeMethodAsync: () => Promise.reject(new Error('echec net')) }, 48);
    await grid.autoFitColumn(viewport, 'a');
    const whileLive = lines.length;
    grid.detachResize(viewport);
    grid.attachResize(viewport, { invokeMethodAsync: () => new Promise((_, reject) => setTimeout(() => reject(new Error('apres detachement')), 50)) }, 48);
    const pending = grid.autoFitColumn(viewport, 'b');
    grid.detachResize(viewport);
    await pending;
    await new Promise(resolve => setTimeout(resolve, 100));
    console.error = original;
    viewport.remove();
    window.removeEventListener('unhandledrejection', onRejection);
    return { seen, whileLive, total: lines.length, line: lines[0] ?? '' };
  })()`);
  check(autoFit.seen.length === 0, `Rejets de promesse non gérés après un ajustement de colonne en échec : ${JSON.stringify(autoFit.seen)}.`);
  check(autoFit.whileLive === 1 && autoFit.line.includes('auto-fit') && autoFit.line.includes('echec net'), `Un échec .NET de l'ajustement d'une colonne doit laisser une ligne de console : ${JSON.stringify(autoFit)}.`);
  check(autoFit.total === 1, `Une grille détachée pendant l'ajustement doit se taire : ${JSON.stringify(autoFit)}.`);
  step('ajustement de colonne : un échec .NET est signalé en console sans rejet, une grille détachée se tait');
}
