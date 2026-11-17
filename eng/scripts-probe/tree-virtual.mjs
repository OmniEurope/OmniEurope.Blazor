// A virtualized tree grid of 5 000 nodes (Data/DataGridTreeVirtualExample.razor on the grid variants page), in a
// real Chromium under the strict CSP: only the rows of the scroll window are in the page, the scroll height
// stands for the whole flattened tree, the bottom of the list is reachable and shows the last node, and a
// folder closed by a trusted click (then opened again) moves the scroll height by exactly the rows it hides.
// "Collapse all" and "Expand all" move the list from 5 000 rows to the 50 folders and back.

const nodes = 5000;
const folders = 50;
const filesPerFolder = 99;
const grid = '#demo-grid-tree-virtual';
const viewport = `${grid} .omni-data-grid__viewport`;
const rows = `${grid} tbody tr[data-omni-row-index]`;
const rowCount = `Number(document.querySelector('${grid} table').getAttribute('aria-rowcount'))`;
const renderedCount = `document.querySelectorAll(${JSON.stringify(rows)}).length`;
const scrollHeight = `document.querySelector(${JSON.stringify(viewport)}).scrollHeight`;
const rowHeights = `[...new Set([...document.querySelectorAll(${JSON.stringify(rows)})].map(row => row.getBoundingClientRect().height))]`;
const lastRowText = `(() => { const all = document.querySelectorAll(${JSON.stringify(rows)}); return all[all.length - 1].querySelector('td[data-omni-col="Name"]').textContent.trim(); })()`;

export async function treeVirtualSteps(session, results) {
  const { evaluate, waitFor, check, clickOn, pause } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const scrollTo = offset => evaluate(`(() => { document.querySelector(${JSON.stringify(viewport)}).scrollTop = ${offset}; })()`);
  const buttonByText = text => evaluate(`(() => {
    const button = [...document.querySelectorAll(${JSON.stringify(`${grid} .omni-data-grid__bar--header button`)})].find(candidate => candidate.textContent.trim() === ${JSON.stringify(text)});
    if (!button) throw new Error('Bouton introuvable : ${text}');
    button.setAttribute('data-probe-button', ${JSON.stringify(text)});
    return true;
  })()`);

  await session.visit('/components/data-grid-variants', `${grid} tbody tr[data-omni-row-index]`);

  // The window only: a handful of rows for 5 000 nodes, the scroll height of all of them.
  const total = await evaluate(rowCount);
  const rendered = await evaluate(renderedCount);
  // Every row is one height, the folders with their chevron as the files: the window counts them alike.
  const heights = await evaluate(rowHeights);
  check(heights.length === 1, `Arbre virtuel : des lignes de hauteurs différentes en hauteur fixe : ${heights.join(', ')} px.`);
  const height = heights[0];
  check(total === nodes, `Arbre virtuel : aria-rowcount ${total}, ${nodes} attendus.`);
  check(rendered > 0 && rendered < 100, `Arbre virtuel : ${rendered} lignes dans le DOM pour ${nodes} noeuds (moins de 100 attendues).`);
  const fullHeight = await evaluate(scrollHeight);
  check(fullHeight > nodes * height * 0.95, `Arbre virtuel : hauteur de défilement ${fullHeight} px pour ${nodes} lignes de ${height} px.`);
  step(`arbre virtuel : ${rendered} lignes dans le DOM pour ${nodes} noeuds`);

  // The bottom of the list: the last node is rendered, still a small window.
  await scrollTo(fullHeight);
  await waitFor('la dernière ligne de l\'arbre', `${lastRowText} === 'File ${folders}.${filesPerFolder}'`);
  const renderedAtEnd = await evaluate(renderedCount);
  check(renderedAtEnd < 100, `Arbre virtuel en bas : ${renderedAtEnd} lignes dans le DOM.`);
  const lastIndex = await evaluate(`document.querySelectorAll(${JSON.stringify(rows)})[${renderedAtEnd - 1}].getAttribute('aria-rowindex')`);
  check(lastIndex === String(nodes), `Arbre virtuel : la dernière ligne porte aria-rowindex ${lastIndex}, ${nodes} attendu.`);
  step('arbre virtuel : le bas de la liste montre le dernier noeud');

  // Back to the top, a folder closed by a trusted click on its chevron: the list loses its 99 files.
  await scrollTo(0);
  await waitFor('le premier dossier', `document.querySelector(${JSON.stringify(rows)}).querySelector('td[data-omni-col="Name"]').textContent.trim() === 'Folder 1'`);
  const before = await evaluate(scrollHeight);
  await clickOn(`${rows}[data-omni-row-index="0"] .omni-data-grid__tree-toggle`);
  await waitFor('le dossier fermé', `${rowCount} === ${nodes - filesPerFolder}`);
  await waitFor('la hauteur réduite', `${scrollHeight} < ${before - (filesPerFolder - 2) * height}`);
  const after = await evaluate(scrollHeight);
  check(Math.abs((before - after) - filesPerFolder * height) <= 2, `Arbre virtuel : fermer un dossier a retiré ${before - after} px, ${filesPerFolder * height} attendus.`);
  const levels = await evaluate(`[...document.querySelectorAll(${JSON.stringify(rows)})].slice(0, 2).map(row => row.getAttribute('aria-level') + ':' + (row.getAttribute('aria-expanded') ?? '-'))`);
  check(JSON.stringify(levels) === '["1:false","1:true"]', `Arbre virtuel : niveaux et état après fermeture ${JSON.stringify(levels)}.`);
  step(`arbre virtuel : un dossier fermé retire ${Math.round(before - after)} px`);

  await clickOn(`${rows}[data-omni-row-index="0"] .omni-data-grid__tree-toggle`);
  await waitFor('le dossier rouvert', `${rowCount} === ${nodes}`);
  await waitFor('la hauteur rétablie', `Math.abs(${scrollHeight} - ${before}) <= 2`);
  step('arbre virtuel : le dossier rouvert rend sa hauteur à la liste');

  // Every folder closed, then every folder open.
  await buttonByText('Collapse all');
  await clickOn('[data-probe-button="Collapse all"]');
  await waitFor('tous les dossiers fermés', `${rowCount} === ${folders}`);
  const collapsed = await evaluate(scrollHeight);
  check(collapsed < before / 10, `Arbre virtuel : hauteur ${collapsed} px une fois tout fermé (moins de ${before / 10} attendus).`);
  const renderedCollapsed = await evaluate(renderedCount);
  check(renderedCollapsed > 0 && renderedCollapsed <= folders, `Arbre virtuel : ${renderedCollapsed} lignes une fois tout fermé.`);
  await buttonByText('Expand all');
  await clickOn('[data-probe-button="Expand all"]');
  await waitFor('tous les dossiers ouverts', `${rowCount} === ${nodes} && ${renderedCount} < 100`);
  step('arbre virtuel : tout fermer puis tout ouvrir');

  // Every density: a folder with its chevron is exactly as tall as a file, at the row height of the density
  // (the tokens --omni-grid-row-estimate: 24, 36 and 44 px), and the chevron itself still follows the density.
  await scrollTo(0);
  await waitFor('le haut de la liste', `document.querySelector(${JSON.stringify(rows)}).querySelector('td[data-omni-col="Name"]').textContent.trim() === 'Folder 1'`);
  const chevrons = {};
  for (const [density, expected] of [['compact', 24], ['comfortable', 36], ['spacious', 44]]) {
    await evaluate(`document.getElementById('showcase-theme').setAttribute('data-omni-density', '${density}')`);
    await pause(300);
    const seen = await evaluate(`(() => {
      const all = [...document.querySelectorAll(${JSON.stringify(rows)})];
      const folders = all.filter(row => row.hasAttribute('aria-expanded'));
      return { heights: [...new Set(all.map(row => row.getBoundingClientRect().height))], folders: folders.length, chevron: document.querySelector(${JSON.stringify(`${rows} .omni-data-grid__tree-toggle`)}).getBoundingClientRect().height };
    })()`);
    check(seen.folders > 0, `Arbre virtuel en ${density} : aucun dossier dans la fenêtre, la sonde ne prouverait rien.`);
    check(seen.heights.length === 1 && seen.heights[0] === expected, `Arbre virtuel en ${density} : hauteurs de ligne ${seen.heights.join(', ')} px, ${expected} attendus pour les dossiers comme pour les fichiers.`);
    chevrons[density] = seen.chevron;
    step(`arbre virtuel en ${density} : lignes de ${expected} px (${seen.folders} dossier(s)), chevron de ${seen.chevron} px`);
  }
  check(chevrons.compact !== chevrons.spacious, `Arbre virtuel : le chevron garde ${chevrons.compact} px de la densité compacte à la spacieuse.`);
  await evaluate("document.getElementById('showcase-theme').setAttribute('data-omni-density', 'comfortable')");

  await pause(200);
  const csp = await evaluate('window.__omniCsp');
  check(csp.length === 0, `Violations CSP après l'arbre virtuel : ${csp.join(' | ')}`);
  step('arbre virtuel : aucune violation CSP');
}
