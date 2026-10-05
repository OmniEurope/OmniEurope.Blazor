// Every showcase page whose components attach scripts, left twice with the same gestures: the
// document and the window must hold exactly as many listeners after the second round trip as after
// the first (Blazor registers its delegated events once, on their first use, which is why the first
// round trip is the reference). A module whose detach misses its element stays attached for good,
// one more set of listeners per visit; this is where it shows.

const neutral = ['/composants/badges', '.omni-badge'];

export async function leakSteps(session, results) {
  const { waitFor, key, pause, check, clickOn } = session;
  const listeners = async () => ({ document: await session.listenersOf('document'), window: await session.listenersOf('window') });

  const pages = [
    ['/composants/superpositions', '#demo-overflow-menu', async () => {
      await clickOn('#demo-overflow-menu');
      await waitFor('le menu ouvert', "document.activeElement?.getAttribute('role') === 'menuitem'");
      await key('Escape', 'Escape', 27);
      await clickOn('.omni-popover > button[aria-haspopup="dialog"]');
      await waitFor('la bulle ouverte', "document.querySelector('.omni-popover > button[aria-haspopup=\"dialog\"]').getAttribute('aria-expanded') === 'true'");
    }],
    ['/composants/editeur', '#editor-extended', async () => {
      await clickOn('#editor-body');
    }],
    ['/composants/grille', '#demo-grid-tree', async () => {}],
    ['/composants/listes', '.omni-log-viewer', async () => {}],
    ['/composants/tableur', '#demo-sheet [data-omni-sheet-grid]', async () => {}],
    ['/composants/arborescence', '#demo-tree-accounts', async () => {}],
    ['/composants/agenda', '.omni-scheduler', async () => {}],
    ['/composants/diagramme', '#demo-mindmap-canvas', async () => {}],
    ['/composants/pages', '#pages-demo-shell', async () => {}],
    ['/composants/coquille', '#shell-demo-sidebar', async () => {}],
    ['/composants/selection', '#demo-tags-compact', async () => {
      await clickOn('#demo-tags-compact > summary');
      await waitFor('le choix compact ouvert', "document.getElementById('demo-tags-compact').open");
    }]
  ];

  const roundTrip = async (path, ready, gesture) => {
    await session.visit(path, ready);
    await gesture();
    await session.visit(...neutral);
    await pause(400);
    return listeners();
  };

  const clean = [];
  for (const [path, ready, gesture] of pages) {
    const first = await roundTrip(path, ready, gesture);
    const second = await roundTrip(path, ready, gesture);
    check(JSON.stringify(second) === JSON.stringify(first),
      `${path} quittée a laissé des écouteurs : après un aller-retour ${JSON.stringify(first)}, après deux ${JSON.stringify(second)}.`);
    clean.push(path.split('/').pop());
  }
  if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après les allers-retours : ${session.consoleErrors.join(' | ')}`);
  results.push(`aucun écouteur laissé après deux allers-retours (${clean.join(', ')})`);
}
