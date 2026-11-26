// The help mark of OmniStatTile, driven in a real Chromium under the strict CSP on the Stat tiles page, at 1280 and at 375 px.
// Two tiles of the gallery example carry a Help: #demo-stat-tile-help (a plain tile) and #demo-stat-tile-help-click (a tile with
// OnClick, whose root is then a div holding the real button, the mark beside it). Checked with real keyboard and mouse input:
// - Tab reaches the mark of each tile (the button of the clickable tile first, in the document order) and the mark shows its
//   tooltip on focus; the same tooltip shows on hover;
// - the tile shows its focus ring when the inner button has the keyboard focus, and not when the mark has it;
// - Enter and Space on the focused mark never raise OnClick; Enter on the focused button does;
// - a real mouse click on the mark does not raise OnClick, a real click on the value and a real click on the bare corner of the
//   tile (the opposite one to the mark) do: the button is stretched over the whole tile, and the mark is the element under the
//   pointer at its own centre;
// - the mark never covers the value: the rectangles of the mark and of the text of the value do not intersect, also with a long
//   value that the tile script has shrunk to fit.

export async function statTileHelpSteps(session, results) {
  const { evaluate, waitFor, key, check, hover, mouse, pause, pointOf, click, clickOn } = session;
  const step = session.stepRecorder(results);
  const { tab } = session;
  const enter = () => key('Enter', 'Enter', 13, 0, '\r');
  const space = () => key(' ', 'Space', 32, 0, ' ');

  const plain = '#demo-stat-tile-help';
  const action = '#demo-stat-tile-help-click';
  const clicks = () => evaluate("document.getElementById('demo-stat-tile-help-clicks').textContent.trim()");
  const clicksAre = async count => waitFor(`${count} clic(s) levé(s) par la tuile`, `document.getElementById('demo-stat-tile-help-clicks').textContent.trim() === 'Help tile clicks: ${count}'`);
  const active = () => evaluate("(() => { const element = document.activeElement; return JSON.stringify({ tag: element.tagName, className: element.className, inPlain: !!element.closest('#demo-stat-tile-help'), inAction: !!element.closest('#demo-stat-tile-help-click'), inHelp: !!element.closest('.omni-stat-tile__help') }); })()").then(JSON.parse);
  // The tooltip fades in over a short transition: read it once that has run.
  const tooltip = async tile => { await pause(400); return evaluate(`(() => { const content = document.querySelector('${tile} .omni-tooltip__content'); const style = getComputedStyle(content); return JSON.stringify({ visible: style.visibility === 'visible' && Number(style.opacity) > 0.9, text: content.textContent.trim() }); })()`).then(JSON.parse); };
  const ring = tile => evaluate(`getComputedStyle(document.querySelector('${tile}')).boxShadow !== 'none'`);

  // The tile's own rectangles: the whole tile, the mark, and the lines of the text of the value.
  const geometry = tile => evaluate(`(() => {
    const root = document.querySelector('${tile}');
    const rect = box => ({ left: box.left, top: box.top, right: box.right, bottom: box.bottom, width: box.width, height: box.height });
    const value = root.querySelector('.omni-stat-tile__value');
    const range = document.createRange();
    range.selectNodeContents(value);
    return JSON.stringify({ tile: rect(root.getBoundingClientRect()), mark: rect(root.querySelector('.omni-stat-tile__help-mark').getBoundingClientRect()), text: [...range.getClientRects()].map(rect), value: rect(value.getBoundingClientRect()), fit: value.getAttribute('data-fit') });
  })()`).then(JSON.parse);
  const intersects = (a, b) => a.left < b.right && b.left < a.right && a.top < b.bottom && b.top < a.bottom;
  const noOverlap = async (tile, label) => {
    const geo = await geometry(tile);
    check(geo.mark.width > 8 && geo.mark.height > 8, `${label} : la marque d'aide est introuvable ou vide : ${JSON.stringify(geo.mark)}.`);
    check(geo.mark.left >= geo.tile.left && geo.mark.right <= geo.tile.right && geo.mark.top >= geo.tile.top && geo.mark.bottom <= geo.tile.bottom, `${label} : la marque sort de la tuile : ${JSON.stringify(geo)}.`);
    check(geo.mark.right > geo.tile.right - 24 && geo.mark.top < geo.tile.top + 24, `${label} : la marque n'est pas dans le coin haut de fin de la tuile : ${JSON.stringify(geo)}.`);
    check(geo.text.length > 0 && geo.text.every(line => !intersects(line, geo.mark)), `${label} : la marque couvre la valeur : ${JSON.stringify(geo)}.`);
    return geo;
  };

  for (const width of [1280, 375]) {
    // Another page first: the page is drawn anew, with its counters at zero and its values as the example writes them.
    await session.visitAtWidth(width, '/components/stat-tiles', action);
    await evaluate(`document.querySelector('${action}').scrollIntoView({ block: 'center' })`);
    await clicksAre(0);

    // The structure the browser really built: a div holding the button, the mark outside it.
    const structure = await evaluate(`(() => { const root = document.querySelector('${action}'); const button = root.querySelector(':scope > button.omni-stat-tile__action'); const mark = root.querySelector('.omni-stat-tile__help-mark'); return JSON.stringify({ tag: root.tagName, button: button !== null, markInButton: button !== null && button.contains(mark), name: button?.getAttribute('aria-label'), describedBy: root.querySelector('.omni-tooltip__trigger').getAttribute('aria-describedby'), plainTag: document.querySelector('${plain}').tagName }); })()`).then(JSON.parse);
    check(structure.tag === 'DIV' && structure.button && !structure.markInButton, `Structure de la tuile cliquable avec aide inattendue : ${JSON.stringify(structure)}.`);
    check(/^Diverted flights ?: 2$/.test(structure.name ?? '') && structure.plainTag === 'DIV', `Nom du bouton ou tuile simple inattendu : ${JSON.stringify(structure)}.`);
    check(structure.describedBy === 'demo-stat-tile-help-click-help-content', `Description de la marque inattendue : ${JSON.stringify(structure)}.`);

    // Real keyboard: from the button of the previous tile, Tab walks to the mark of the plain tile, then to the button of the
    // clickable one, then to its mark.
    await evaluate(`(() => { const buttons = [...document.querySelectorAll('button.omni-stat-tile')]; const before = buttons.find(button => button.getAttribute('aria-label')?.startsWith('Delayed flights')); before.focus(); })()`);
    await tab();
    let now = await active();
    check(now.inPlain && now.inHelp && now.className.includes('omni-tooltip__trigger'), `Tab n'arrive pas sur la marque de la tuile simple : ${JSON.stringify(now)}.`);
    let shown = await tooltip(plain);
    check(shown.visible && shown.text.startsWith('Flights cancelled by the airline'), `L'infobulle de la tuile simple ne s'affiche pas au focus : ${JSON.stringify(shown)}.`);
    check(!(await ring(plain)), `La tuile simple montre un anneau de focus.`);
    await noOverlap(plain, `tuile simple à ${width}`);

    await tab();
    now = await active();
    check(now.inAction && !now.inHelp && now.tag === 'BUTTON', `Tab n'arrive pas sur le bouton de la tuile cliquable : ${JSON.stringify(now)}.`);
    check(await ring(action), `La tuile cliquable ne montre pas l'anneau de focus quand son bouton est focalisé.`);
    shown = await tooltip(action);
    check(!shown.visible, `L'infobulle de la tuile cliquable est visible alors que le bouton est focalisé : ${JSON.stringify(shown)}.`);

    await tab();
    now = await active();
    check(now.inAction && now.inHelp && now.className.includes('omni-tooltip__trigger'), `Tab n'arrive pas sur la marque de la tuile cliquable : ${JSON.stringify(now)}.`);
    shown = await tooltip(action);
    check(shown.visible && shown.text.startsWith('Flights that landed at another airport'), `L'infobulle de la tuile cliquable ne s'affiche pas au focus : ${JSON.stringify(shown)}.`);
    check(!(await ring(action)), `La tuile cliquable montre l'anneau de focus alors que la marque est focalisée.`);

    // Enter and Space on the mark raise nothing.
    await enter();
    await space();
    await pause(300);
    check((await clicks()) === 'Help tile clicks: 0', `Entrée ou Espace sur la marque a levé OnClick : ${await clicks()}.`);
    // Enter on the button does.
    await evaluate(`document.querySelector('${action} > button').focus()`);
    await enter();
    await clicksAre(1);
    step(`tuile avec aide à ${width} px : Tab atteint la marque (infobulle au focus) et le bouton (anneau sur la tuile), Entrée et Espace sur la marque ne cliquent pas, Entrée sur le bouton clique`);

    // Hover shows the tooltip too.
    await evaluate('document.activeElement.blur()');
    await mouse('mouseMoved', { x: 2, y: 2 }, { button: 'none' });
    shown = await tooltip(plain);
    check(!shown.visible, `L'infobulle de la tuile simple reste visible hors survol : ${JSON.stringify(shown)}.`);
    await hover(`${plain} .omni-stat-tile__help-mark`);
    await pause(400);
    shown = await tooltip(plain);
    check(shown.visible, `L'infobulle de la tuile simple ne s'affiche pas au survol : ${JSON.stringify(shown)}.`);
    // The tooltip stays inside the window.
    const inside = await evaluate(`(() => { const box = document.querySelector('${plain} .omni-tooltip__content').getBoundingClientRect(); return box.left >= -1 && box.right <= innerWidth + 1; })()`);
    check(inside, `L'infobulle de la tuile simple sort de la fenêtre à ${width} px.`);

    // Real mouse: the mark is the element under the pointer at its centre, and a click there does nothing; the value and the bare
    // corner activate the button.
    await evaluate(`document.querySelector('${action}').scrollIntoView({ block: 'center' })`);
    const geo = await noOverlap(action, `tuile cliquable à ${width}`);
    const markPoint = await pointOf(`${action} .omni-stat-tile__help-mark`);
    const under = await evaluate(`(() => { const hit = document.elementFromPoint(${markPoint.x}, ${markPoint.y}); return !!hit && hit.closest('.omni-stat-tile__help') !== null; })()`);
    check(under, `Au centre de la marque, l'élément sous le pointeur n'est pas la marque (le bouton étiré la recouvre).`);
    await click(markPoint);
    await pause(400);
    check((await clicks()) === 'Help tile clicks: 1', `Un clic réel sur la marque a levé OnClick : ${await clicks()}.`);
    await clickOn(`${action} .omni-stat-tile__value`);
    await clicksAre(2);
    const corner = { x: Math.round(geo.tile.left + 4), y: Math.round(geo.tile.bottom - 4) };
    await click(corner);
    await clicksAre(3);
    step(`tuile avec aide à ${width} px : clic réel sur la marque sans effet, clic sur la valeur et dans le coin nu de la tuile levés par le bouton étiré, marque dans le coin haut de fin sans couvrir la valeur`);

    // A long value, shrunk to fit by the tile script, still clears the mark.
    await evaluate(`(() => { const value = document.querySelector('${action} .omni-stat-tile__value'); value.firstChild.textContent = '1 234 567 890 123,45 km'; })()`);
    await waitFor('la valeur longue ajustée à la tuile', `(() => { const value = document.querySelector('${action} .omni-stat-tile__value'); return value.scrollWidth <= value.clientWidth; })()`);
    await pause(300);
    const long = await noOverlap(action, `valeur longue à ${width}`);
    check(long.value.left >= long.tile.left && long.value.right <= long.tile.right, `La valeur longue sort de la tuile : ${JSON.stringify(long)}.`);
    // Wide, the tile grows with its content; narrow, it cannot, and the script must shrink the value.
    check(width > 400 || long.fit !== null, `La valeur longue n'a pas été réduite par le script, le cas n'est pas éprouvé : ${JSON.stringify(long)}.`);
    step(`tuile avec aide à ${width} px : une valeur longue ajustée (${long.fit ?? 'sans réduction'}) ne passe pas sous la marque`);
  }
}
