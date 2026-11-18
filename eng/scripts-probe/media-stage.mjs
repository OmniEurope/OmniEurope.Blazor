// The media stage (OmniMediaStage, PLAN-002 lot 3), driven in a real Chromium under the strict CSP on its showcase
// page, at 1440 and at 375 px: the layout follows the width of the stage (main item and thumbnail column side by side
// when wide, main item on top and one row of thumbnails scrolling sideways when narrow, 16:9 frames, the main item
// staying put while the row scrolls), a click on a thumbnail (real mouse, through the pointer-events pass-through of the
// content) and Enter on its button make it the main item, and, above all, the elements are MOVED, never recreated: a
// property set on every item element and on its content survives each switch, and a real <video> playing a canvas
// stream inside an item is neither paused, nor removed from the document, nor reloaded when its item becomes the main
// one and when it goes back to the strip. One item has no strip, no item has an empty stage, and nothing carries a
// style attribute.

export async function mediaStageSteps(session, results) {
  const { evaluate, waitFor, check, clickOn, hover, key, pause, send } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  const stage = '#media-stage-demo';
  const itemAt = index => `${stage} [data-omni-media-stage-item="${index}"]`;
  const viewport = (width, height) => send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
  const geometry = () => evaluate(`(() => {
    const root = document.querySelector(${JSON.stringify(stage)});
    const rect = element => { const box = element.getBoundingClientRect(); return { left: box.left, top: box.top, right: box.right, bottom: box.bottom, width: box.width, height: box.height }; };
    const all = [...root.querySelectorAll('.omni-media-stage__item')];
    const strip = root.querySelector('.omni-media-stage__items');
    const styles = getComputedStyle(strip);
    const main = all.find(item => item.classList.contains('omni-media-stage__item--main'));
    return JSON.stringify({
      root: { ...rect(root), scrollWidth: root.scrollWidth, clientWidth: root.clientWidth },
      strip: { ...rect(strip), scrollWidth: strip.scrollWidth, clientWidth: strip.clientWidth, scrollHeight: strip.scrollHeight, clientHeight: strip.clientHeight, overflowX: styles.overflowX, overflowY: styles.overflowY, scrollLeft: strip.scrollLeft },
      main: main ? { ...rect(main), index: all.indexOf(main), position: getComputedStyle(main).position } : null,
      thumbs: all.filter(item => item !== main).map(item => ({ ...rect(item), index: all.indexOf(item), position: getComputedStyle(item).position })),
      count: all.length
    });
  })()`).then(JSON.parse);
  const mainIndex = () => evaluate(`[...document.querySelectorAll(${JSON.stringify(`${stage} .omni-media-stage__item`)})].findIndex(item => item.classList.contains('omni-media-stage__item--main'))`);
  const close = (actual, expected, tolerance) => Math.abs(actual - expected) <= tolerance;
  // Compared against the stage itself: a click may scroll the page, which moves everything together.
  const placed = geo => ({ left: geo.main.left - geo.root.left, top: geo.main.top - geo.root.top, width: geo.main.width, height: geo.main.height });
  const sameRect = (a, b) => close(a.left, b.left, 1) && close(a.top, b.top, 1) && close(a.width, b.width, 1) && close(a.height, b.height, 1);
  const setCount = async count => {
    await evaluate(`(() => {
      const input = document.querySelector('input.omni-slider__input[aria-label="Participants"]');
      Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(input, ${JSON.stringify(String(count))});
      input.dispatchEvent(new Event('input', { bubbles: true }));
    })()`);
    await waitFor(`${count} participant(s)`, `document.querySelectorAll(${JSON.stringify(`${stage} .omni-media-stage__item`)}).length === ${count}`);
    await pause(200);
  };
  const assertFrames = (shape, geo, where) => {
    // A thumbnail is a 16:9 frame; the main item is 16:9 too (unless capped by its maximum height, where it is wider).
    for (const thumb of geo.thumbs) check(close(thumb.width / thumb.height, 16 / 9, 0.05), `Scène ${where} : une vignette de ${thumb.width} sur ${thumb.height} px n'est pas en 16:9.`);
    const ratio = geo.main.width / geo.main.height;
    check(ratio >= 16 / 9 - 0.05, `Scène ${where} : l'élément principal (${geo.main.width} sur ${geo.main.height} px) est plus étroit que 16:9.`);
    check(shape === 'any' || close(ratio, 16 / 9, 0.05) || geo.main.height >= 40 * 16 - 2, `Scène ${where} : l'élément principal de ${ratio.toFixed(2)} n'est ni en 16:9 ni au plafond de hauteur.`);
  };

  await session.visit('/components/media-stage', `${stage} .omni-media-stage__item`);
  await waitFor('les sept participants', `document.querySelectorAll(${JSON.stringify(`${stage} .omni-media-stage__item`)}).length === 7`);

  // ---- Wide: 1440 px ------------------------------------------------------------------------------------
  await viewport(1440, 900);
  await pause(400);
  let geo = await geometry();
  check(geo.count === 7 && geo.thumbs.length === 6, `Scène large : ${geo.count} éléments dont ${geo.thumbs.length} vignettes, 7 et 6 attendus.`);
  check(geo.main.index === 0, `Scène large : l'élément principal est le n° ${geo.main.index}, le premier attendu.`);
  check(geo.main.position === 'absolute' && geo.thumbs.every(thumb => thumb.position === 'relative'), 'Scène large : le principal n\'est pas hors flux ou une vignette y est.');
  const wide = geo.root.width >= 40 * 16;
  check(wide, `La scène ne fait que ${geo.root.width} px à 1440 px de fenêtre : la disposition large (40rem) ne serait pas éprouvée.`);
  // Beside: every thumbnail to the right of the main item, inside its height; the column scrolls when it is taller.
  check(geo.thumbs.every(thumb => thumb.left >= geo.main.right - 1), `Scène large : une vignette n'est pas à droite de l'élément principal (principal jusqu'à ${geo.main.right}, vignette dès ${Math.min(...geo.thumbs.map(thumb => thumb.left))}).`);
  check(geo.thumbs.every(thumb => close(thumb.left, geo.thumbs[0].left, 1) && close(thumb.width, geo.thumbs[0].width, 1)), 'Scène large : les vignettes ne forment pas une colonne.');
  check(close(geo.strip.height, geo.main.height, 8), `Scène large : la colonne (${geo.strip.height} px) n'a pas la hauteur de l'élément principal (${geo.main.height} px).`);
  check(geo.strip.overflowY === 'auto' && geo.strip.overflowX === 'hidden', `Scène large : débordement de la colonne ${geo.strip.overflowX}/${geo.strip.overflowY}.`);
  check(close(geo.root.height, geo.main.height, 8), `Scène large : la scène (${geo.root.height} px) est plus haute que son élément principal (${geo.main.height} px).`);
  check(geo.main.height <= 40 * 16 + 1, `Scène large : l'élément principal fait ${geo.main.height} px, plus que son plafond de 40rem.`);
  assertFrames('wide', geo, 'large');
  check(geo.root.scrollWidth <= geo.root.clientWidth + 6 && await evaluate("document.documentElement.scrollWidth <= innerWidth"), `Scène large : la scène ou la page déborde de côté (${geo.root.scrollWidth} px pour ${geo.root.clientWidth}).`);
  step(`large (${Math.round(geo.root.width)} px) : principal ${Math.round(geo.main.width)} x ${Math.round(geo.main.height)} px à gauche, ${geo.thumbs.length} vignettes de ${Math.round(geo.thumbs[0].width)} x ${Math.round(geo.thumbs[0].height)} px en colonne à droite${geo.strip.scrollHeight > geo.strip.clientHeight + 1 ? ' (la colonne défile)' : ''}`);

  const noStyle = await evaluate(`document.querySelectorAll(${JSON.stringify(`${stage} [style], ${stage}[style]`)}).length`);
  check(noStyle === 0, `Scène : ${noStyle} élément(s) portent un attribut style.`);
  const frames = await evaluate(`(() => { const items = [...document.querySelectorAll(${JSON.stringify(`${stage} .omni-media-stage__item`)})]; return JSON.stringify(items.map(item => { const style = getComputedStyle(item); const sides = ['Top', 'Right', 'Bottom', 'Left'].map(side => style['border' + side + 'Width']); return { sides, inset: style.boxShadow.includes('inset') }; })); })()`).then(JSON.parse);
  check(frames.every(frame => frame.sides.every(width => width === frames[0].sides[0]) && !frame.inset), 'Scène : un cadre a un bord d\'un seul côté ou une ombre intérieure (liseré).');
  step('aucun attribut style dans la scène, aucun cadre à un seul côté');

  // The elements and a real playing video, tagged so that a recreation shows.
  await evaluate(`(() => {
    const items = [...document.querySelectorAll(${JSON.stringify(`${stage} .omni-media-stage__item`)})];
    items.forEach((item, index) => { item.__probeItem = index; item.querySelector('.omni-media-stage__content').__probeContent = index; item.querySelector('.omni-media-stage__content > *').__probeInner = index; });
    const canvas = document.createElement('canvas');
    canvas.width = 160; canvas.height = 90;
    const context = canvas.getContext('2d');
    let tick = 0;
    window.__probeTimer = setInterval(() => { context.fillStyle = 'hsl(' + (tick++ * 12 % 360) + ' 70% 50%)'; context.fillRect(0, 0, 160, 90); }, 40);
    const video = document.createElement('video');
    video.muted = true; video.autoplay = true; video.playsInline = true; video.loop = true;
    video.srcObject = canvas.captureStream(25);
    window.__probeEvents = { pause: 0, emptied: 0, loadstart: 0 };
    for (const name of Object.keys(window.__probeEvents)) video.addEventListener(name, () => { window.__probeEvents[name]++; });
    items[3].querySelector('.omni-media-stage__content > *').appendChild(video);
    window.__probeVideo = video;
    return video.play();
  })()`);
  await waitFor('la vidéo de démonstration qui joue', 'window.__probeVideo.currentTime > 0.2 && !window.__probeVideo.paused');
  await evaluate('window.__probeEvents = { pause: 0, emptied: 0, loadstart: 0 }; ["pause", "emptied", "loadstart"].forEach(name => window.__probeVideo.addEventListener(name, () => { window.__probeEvents[name]++; }));');
  const tagged = () => evaluate(`(() => {
    const items = [...document.querySelectorAll(${JSON.stringify(`${stage} .omni-media-stage__item`)})];
    return items.every((item, index) => item.__probeItem === index && item.querySelector('.omni-media-stage__content').__probeContent === index && item.querySelector('.omni-media-stage__content > *').__probeInner === index);
  })()`);
  const videoState = () => evaluate(`(() => { const video = window.__probeVideo; return JSON.stringify({ time: video.currentTime, paused: video.paused, connected: video.isConnected, ended: video.ended, events: window.__probeEvents, inItem: video.closest('.omni-media-stage__item')?.getAttribute('data-omni-media-stage-item') ?? null, main: video.closest('.omni-media-stage__item')?.classList.contains('omni-media-stage__item--main') ?? null }); })()`).then(JSON.parse);
  const checkKept = async (description, expectedItem, expectedMain) => {
    check(await tagged(), `Scène ${description} : un élément ou son contenu a été recréé (marque perdue).`);
    const before = await videoState();
    await pause(400);
    const after = await videoState();
    check(after.connected && !after.paused && !after.ended, `Scène ${description} : la vidéo ne joue plus (${JSON.stringify(after)}).`);
    check(after.time > before.time + 0.1, `Scène ${description} : la vidéo n'avance plus (${before.time} puis ${after.time}).`);
    check(after.events.pause === 0 && after.events.emptied === 0 && after.events.loadstart === 0, `Scène ${description} : la vidéo a été interrompue ou rechargée (${JSON.stringify(after.events)}).`);
    check(after.inItem === String(expectedItem) && after.main === expectedMain, `Scène ${description} : la vidéo est dans l'élément ${after.inItem} (principal : ${after.main}), ${expectedItem} attendu.`);
  };
  await checkKept('avant le changement', 3, false);

  // A real mouse click in the middle of a thumbnail (the video of item 3): the content lets it through to the button.
  const mainBefore = placed(geo);
  await hover(itemAt(3));
  const badgeOpacity = await evaluate(`getComputedStyle(document.querySelector(${JSON.stringify(`${itemAt(3)} .omni-media-stage__badge`)})).opacity`);
  check(Number(badgeOpacity) === 1, `Scène : l'icône d'agrandissement d'une vignette survolée a une opacité de ${badgeOpacity}.`);
  await clickOn(itemAt(3));
  await waitFor('la vignette 3 en principal', `document.querySelector(${JSON.stringify(`${itemAt(3)}.omni-media-stage__item--main`)}) !== null`);
  check(await mainIndex() === 3, 'Scène : le clic sur la vignette 3 ne l\'a pas faite principale.');
  await waitFor('le badge de la page', `document.getElementById('media-stage-main').textContent.trim() === 'Main: Margaret'`);
  geo = await geometry();
  check(sameRect(placed(geo), mainBefore), `Scène large : l'élément principal a changé de place après le clic (${JSON.stringify(mainBefore)} puis ${JSON.stringify(placed(geo))}).`);
  check(geo.thumbs.length === 6 && geo.thumbs.some(thumb => thumb.index === 0), 'Scène large : l\'ancien principal n\'est pas revenu dans la colonne.');
  check(await evaluate(`document.activeElement === document.querySelector(${JSON.stringify(itemAt(3))})`), 'Scène : le focus n\'est pas passé à l\'élément devenu principal.');
  await checkKept('après le clic sur la vignette 3', 3, true);
  step('un vrai clic au milieu de la vignette 3 la fait principale à la place de l\'ancien : mêmes éléments (marques conservées), vidéo jouée sans pause ni rechargement, focus sur l\'élément principal');

  // Back to the strip: the video moves from the main item to a thumbnail without being touched.
  await clickOn(itemAt(0));
  await waitFor('la vignette 0 en principal', `document.querySelector(${JSON.stringify(`${itemAt(0)}.omni-media-stage__item--main`)}) !== null`);
  await checkKept('après le retour de l\'élément 3 dans la colonne', 3, false);
  step('l\'élément à la vidéo retourne dans la colonne : la vidéo joue toujours, jamais en pause');

  // Keyboard: the button of a thumbnail, focused and pressed with Enter.
  const buttonOf = index => `${itemAt(index)} button.omni-media-stage__select`;
  check(await evaluate(`document.querySelector(${JSON.stringify(buttonOf(5))}).getAttribute('aria-label')`) === 'Show Katherine large', 'Scène : le bouton de la vignette 5 n\'est pas nommé « Show Katherine large ».');
  // A key press first: a script focus after a mouse click is not a keyboard focus, and shows no ring.
  await key('Shift', 'ShiftLeft', 16);
  await evaluate(`document.querySelector(${JSON.stringify(buttonOf(5))}).focus()`);
  await pause(100);
  const ring = await evaluate(`getComputedStyle(document.querySelector(${JSON.stringify(itemAt(5))})).boxShadow`);
  await key('Enter', 'Enter', 13, 0, '\r');
  await waitFor('la vignette 5 en principal', `document.querySelector(${JSON.stringify(`${itemAt(5)}.omni-media-stage__item--main`)}) !== null`);
  check(await evaluate(`document.activeElement === document.querySelector(${JSON.stringify(itemAt(5))})`), 'Scène : le focus n\'est pas sur l\'élément principal après Entrée sur le bouton de sa vignette.');
  const announced = await evaluate(`document.querySelector(${JSON.stringify(`${stage} [role=status]`)}).textContent.replace(/\\u00a0/g, '').trim()`);
  check(announced === 'Katherine is now shown large', `Scène : annonce « ${announced} ».`);
  check(await tagged(), 'Scène : un élément a été recréé par le choix au clavier.');
  step(`Entrée sur le bouton « Show Katherine large » : vignette 5 principale, focus sur elle, annonce « ${announced} », ${ring === 'none' ? 'sans' : 'avec'} anneau au focus de la vignette`);
  check(ring !== 'none', 'Scène : le bouton focalisé d\'une vignette ne montre aucun anneau de focus.');

  await clickOn('#media-stage-reset');
  await waitFor('le retour au premier', `document.querySelector(${JSON.stringify(`${itemAt(0)}.omni-media-stage__item--main`)}) !== null`);
  step('le parent reprend la main : « Back to the first » remet le premier élément principal');

  // ---- Narrow: 375 px -------------------------------------------------------------------------------------
  await viewport(375, 800);
  await pause(500);
  geo = await geometry();
  check(geo.root.width <= 375, `Scène étroite : ${geo.root.width} px de large pour 375 px de fenêtre.`);
  check(geo.root.width < 40 * 16, 'Scène étroite : la scène a gardé 40rem et plus.');
  check(geo.main.bottom <= Math.min(...geo.thumbs.map(thumb => thumb.top)) + 1, `Scène étroite : l'élément principal (jusqu'à ${geo.main.bottom}) n'est pas au-dessus des vignettes (dès ${Math.min(...geo.thumbs.map(thumb => thumb.top))}).`);
  check(close(geo.main.width, geo.root.width, 1) && close(geo.main.left, geo.root.left, 1), 'Scène étroite : l\'élément principal ne prend pas toute la largeur.');
  check(geo.thumbs.every(thumb => close(thumb.top, geo.thumbs[0].top, 1) && close(thumb.height, geo.thumbs[0].height, 1)), 'Scène étroite : les vignettes ne sont pas sur une seule rangée.');
  check(geo.thumbs.every((thumb, index) => index === 0 || thumb.left > geo.thumbs[index - 1].left), 'Scène étroite : les vignettes ne se suivent pas de gauche à droite.');
  check(geo.strip.overflowX === 'auto' && geo.strip.scrollWidth > geo.strip.clientWidth + 50, `Scène étroite : la rangée ne défile pas de côté (${geo.strip.scrollWidth} px de contenu pour ${geo.strip.clientWidth}).`);
  check(geo.root.scrollWidth <= geo.root.clientWidth + 6 && await evaluate("document.documentElement.scrollWidth <= innerWidth"), `Scène étroite : la scène ou la page déborde (${geo.root.scrollWidth} px pour ${geo.root.clientWidth}) : seule la rangée doit défiler.`);
  assertFrames('wide', geo, 'étroite');
  step(`étroite (${Math.round(geo.root.width)} px) : principal ${Math.round(geo.main.width)} x ${Math.round(geo.main.height)} px en haut, ${geo.thumbs.length} vignettes de ${Math.round(geo.thumbs[0].width)} px en une rangée (${Math.round(geo.strip.scrollWidth)} px de contenu pour ${Math.round(geo.strip.clientWidth)}) qui défile de côté`);

  // The row scrolls, the main item stays put; the last thumbnail, scrolled into view by the click, becomes the main item.
  const pinned = placed(geo);
  await evaluate(`(() => { const strip = document.querySelector(${JSON.stringify(`${stage} .omni-media-stage__items`)}); strip.scrollLeft = strip.scrollWidth; })()`);
  await pause(300);
  const scrolled = await geometry();
  check(scrolled.strip.scrollLeft > 50, `Scène étroite : la rangée n'a pas défilé (${scrolled.strip.scrollLeft}).`);
  check(sameRect(placed(scrolled), pinned), `Scène étroite : l'élément principal a bougé avec la rangée (${JSON.stringify(pinned)} puis ${JSON.stringify(placed(scrolled))}).`);
  step(`la rangée défile de ${Math.round(scrolled.strip.scrollLeft)} px, l'élément principal ne bouge pas`);
  await clickOn(itemAt(6));
  await waitFor('la vignette 6 en principal', `document.querySelector(${JSON.stringify(`${itemAt(6)}.omni-media-stage__item--main`)}) !== null`);
  geo = await geometry();
  check(sameRect(placed(geo), pinned), 'Scène étroite : l\'élément principal a changé de place après le clic.');
  check(await tagged(), 'Scène étroite : un élément a été recréé par le clic.');
  await checkKept('étroite après le clic sur la dernière vignette', 3, false);
  step('étroite : un clic sur la dernière vignette, atteinte en faisant défiler, la fait principale sans rien recréer');

  // ---- Few items ---------------------------------------------------------------------------------------------
  await setCount(1);
  geo = await geometry();
  check(geo.count === 1 && geo.thumbs.length === 0 && geo.main.index === 0, 'Scène à un élément : il n\'est pas le seul principal.');
  check(await evaluate(`document.querySelectorAll(${JSON.stringify(`${stage} .omni-media-stage__select`)}).length`) === 0, 'Scène à un élément : un bouton de vignette est resté.');
  check(close(geo.root.height, geo.main.height, 2), `Scène à un élément : une bande vide reste sous le principal (${geo.root.height} px pour ${geo.main.height}).`);
  step('un seul élément : principal seul, sans bouton ni bande de vignettes');
  await setCount(0);
  geo = await geometry().catch(() => null);
  const emptyHeight = await evaluate(`document.querySelector(${JSON.stringify(stage)}).getBoundingClientRect().height`);
  check(emptyHeight <= 2, `Scène vide : ${emptyHeight} px de haut.`);
  check(await evaluate(`document.querySelector(${JSON.stringify(stage)}).getAttribute('aria-label')`) === 'Call', 'Scène vide : le nom accessible a disparu.');
  step('aucun élément : la scène est vide (hauteur nulle) et garde son nom accessible');

  await viewport(1440, 900);
  await setCount(1);
  geo = await geometry();
  check(close(geo.main.width, geo.root.width, 1) && close(geo.root.height, geo.main.height, 2), `Scène large à un élément : principal de ${geo.main.width} px dans ${geo.root.width}, scène de ${geo.root.height} px.`);
  await setCount(7);
  step('large à un élément : le principal prend toute la largeur ; sept participants rétablis');

  await evaluate('clearInterval(window.__probeTimer)');
  await viewport(1280, 900);
  await pause(200);
  const csp = await evaluate('window.__omniCsp');
  check(csp.length === 0, `Violations CSP après la scène média : ${csp.join(' | ')}`);
  step('scène média : aucune violation CSP');
}
