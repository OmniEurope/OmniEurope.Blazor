// The drawing pad (OmniInkCanvas, PLAN-010 lot 1), driven in a real Chromium on its showcase page with real input:
// a mouse stroke, a pen stroke with its pressure, a touch stroke (the page does not scroll), Ctrl+Z and Delete from
// the keyboard, the sample strokes redrawn after a resize and after a change of theme (a stroke in the theme colour
// is dark in the light theme and light in the dark one), a read-only and a disabled pad that draw nothing, the
// signature shape (aspect ratio), the PNG export, whose decoded image is transparent where nothing is drawn, and the
// shared-drawing parts: the eraser (a path removes each whole stroke it touches, from the pointer and the toolbar,
// blocked when read only), the StrokeAdded and StrokesRemoved counters of the page (drawn and removed strokes counted,
// the strokes the host gives not), and a frame whose background a parent can make transparent.
// The pixels are read back from the canvas, because the strokes exist only there.

export async function inkSteps(session, results) {
  const { evaluate, waitFor, check, clickOn, key, pause, mouse, send } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  const surface = '#ink-demo canvas.omni-ink-canvas__surface';
  const box = () => evaluate(`(() => { const b = document.querySelector(${JSON.stringify(surface)}).getBoundingClientRect(); return JSON.stringify({ left: b.left, top: b.top, width: b.width, height: b.height }); })()`).then(JSON.parse);
  const at = (frame, x, y) => ({ x: frame.left + frame.width * x, y: frame.top + frame.height * y });
  // The opaque pixels of the canvas: how many, and the mean of their red channel (0 is black, 255 is white).
  const pixels = () => evaluate(`(() => {
    const canvas = document.querySelector(${JSON.stringify(surface)});
    const data = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data;
    let count = 0; let red = 0;
    for (let index = 3; index < data.length; index += 4) if (data[index] > 0) { count++; red += data[index - 3]; }
    return JSON.stringify({ count, red: count ? red / count : null, width: canvas.width, height: canvas.height });
  })()`).then(JSON.parse);
  const opaqueCount = `(() => { const canvas = document.querySelector(${JSON.stringify(surface)}); const data = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data; let count = 0; for (let index = 3; index < data.length; index += 4) if (data[index] > 0) count++; return count; })()`;
  const strokes = () => evaluate("Number.parseInt(document.getElementById('ink-count').textContent, 10)");
  const pickMode = async position => {
    await clickOn('.omni-header .omni-app-menu__trigger');
    await waitFor("le menu d'application", "document.querySelector('.omni-app-menu__mode') !== null");
    await clickOn(`.omni-app-menu__mode .omni-appearance-window__mode:nth-child(${position})`);
    await key('Escape', 'Escape', 27);
    await waitFor("le menu d'application refermé", "document.querySelector('.omni-app-menu__mode') === null");
  };
  const drag = async (points, extra = {}) => {
    await mouse('mouseMoved', points[0], { button: 'none', ...extra });
    await mouse('mousePressed', points[0], extra);
    for (const point of points.slice(1)) await mouse('mouseMoved', point, { button: 'left', buttons: 1, ...extra });
    await mouse('mouseReleased', points.at(-1), extra);
  };
  const waitForStrokes = (description, expected) => waitFor(description, `Number.parseInt(document.getElementById('ink-count')?.textContent, 10) === ${expected}`);
  const focusSurface = () => evaluate(`document.querySelector(${JSON.stringify(surface)}).focus()`);
  const ctrlZ = () => key('z', 'KeyZ', 90, 2);

  await session.visit('/components/ink-canvas', surface);
  await waitFor('la zone dimensionnée', `document.querySelector(${JSON.stringify(surface)}).width > 0`);
  check((await pixels()).count === 0, 'La zone de dessin commence non vide.');
  const touchAction = await evaluate(`getComputedStyle(document.querySelector(${JSON.stringify(surface)})).touchAction`);
  check(touchAction === 'none', `touch-action vaut ${touchAction}, pas none.`);

  // 1. A mouse stroke: drawn as it goes, kept as one stroke, the canvas takes the focus.
  let frame = await box();
  await drag([at(frame, 0.1, 0.3), at(frame, 0.3, 0.6), at(frame, 0.5, 0.3), at(frame, 0.7, 0.6)]);
  await waitForStrokes('un trait à la souris', 1);
  const mouseDrawn = await pixels();
  check(mouseDrawn.count > 50, `Trait à la souris absent du canevas : ${JSON.stringify(mouseDrawn)}.`);
  check(await evaluate(`document.activeElement === document.querySelector(${JSON.stringify(surface)})`), 'La zone de dessin ne prend pas le focus au tracé.');
  step(`trait à la souris : ${mouseDrawn.count} pixels dessinés, un trait compté, focus sur la zone`);

  // 2. A pen stroke whose pressure rises: a second stroke, with a pressure the value keeps.
  await drag([at(frame, 0.1, 0.8), at(frame, 0.4, 0.8), at(frame, 0.7, 0.8)], { pointerType: 'pen', force: 0.9 });
  await waitForStrokes('un trait au stylet', 2);
  check((await pixels()).count > mouseDrawn.count, 'Le trait au stylet ne dessine rien de plus.');
  step('trait au stylet avec sa pression : deux traits comptés');

  // 3. A touch stroke: a third stroke, and the page does not scroll with it.
  const scrolled = await evaluate('document.querySelector(\'.omni-main-scroll\')?.scrollTop ?? window.scrollY');
  const finger = (x, y) => ({ x: Math.round(frame.left + frame.width * x), y: Math.round(frame.top + frame.height * y), id: 1 });
  await send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [finger(0.8, 0.2)] });
  for (let index = 1; index <= 6; index++) {
    await send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [finger(0.8 + index * 0.02, 0.2 + index * 0.1)] });
  }

  await send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await waitForStrokes('un trait au doigt', 3);
  check((await evaluate('document.querySelector(\'.omni-main-scroll\')?.scrollTop ?? window.scrollY')) === scrolled, 'La page défile pendant un trait au doigt.');
  step('trait au doigt : trois traits comptés, la page ne défile pas');

  // 4. The keyboard: Ctrl+Z undoes the last stroke, Delete clears, each announced.
  await focusSurface();
  await ctrlZ();
  await waitForStrokes('Ctrl+Z', 2);
  check((await evaluate("document.querySelector('#ink-demo [role=status]').textContent.trim().length")) > 0, "L'annulation n'est pas annoncée.");
  await key('Delete', 'Delete', 46);
  await waitForStrokes('Suppr', 0);
  check((await pixels()).count === 0, 'Le canevas garde des pixels après Suppr.');
  step('Ctrl+Z annule un trait, Suppr efface le dessin, annoncés');

  // 5. The toolbar buttons undo and clear too; a sample is a value given by the host.
  await clickOn('#ink-sample');
  await waitForStrokes('le modèle chargé', 3);
  await waitFor('le modèle dessiné', `${opaqueCount} > 200`);
  const sample = await pixels();
  check(sample.count > 200, `Modèle absent du canevas : ${JSON.stringify(sample)}.`);
  await clickOn('#ink-demo .omni-ink-canvas__toolbar button[title="Undo last stroke"]');
  await waitForStrokes('Annuler de la barre', 2);
  await clickOn('#ink-demo .omni-ink-canvas__toolbar button[title="Clear drawing"]');
  await waitForStrokes('Effacer de la barre', 0);
  check((await pixels()).count === 0, 'Le canevas garde des pixels après Effacer.');
  step('Annuler et Effacer de la barre, modèle donné par la valeur dessiné');

  // 6. A resize redraws the strokes at the new size, and the theme colour follows the theme.
  await clickOn('#ink-sample');
  await waitForStrokes('le modèle rechargé', 3);
  await focusSurface();
  await ctrlZ();
  await ctrlZ();
  await waitForStrokes('la vague seule', 1);
  const light = await pixels();
  check(light.red < 100, `Trait de la couleur du thème pas sombre en clair : ${JSON.stringify(light)}.`);
  // Narrow enough that the canvas shrinks whatever layout the earlier parts left the shell in (measured: at 760 px
  // a shell already in its narrow layout kept the canvas at the same 689 px).
  await send('Emulation.setDeviceMetricsOverride', { width: 480, height: 900, deviceScaleFactor: 1, mobile: false });
  await waitFor('le canevas redessiné à la nouvelle taille', `document.querySelector(${JSON.stringify(surface)}).width !== ${light.width}`);
  const narrow = await pixels();
  check(narrow.count > 100 && narrow.red < 100, `Trait perdu après le redimensionnement : ${JSON.stringify(narrow)}.`);
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  // The shell may keep its narrow layout (a collapsed menu) once back to the wide size, so the canvas need not
  // find its first width again (measured: the same 689 px as the narrow layout): what counts is that its pixels
  // match its box again.
  await waitFor('le canevas revenu à la taille de sa boîte', `(() => { const canvas = document.querySelector(${JSON.stringify(surface)}); return canvas.width === Math.round(canvas.clientWidth * devicePixelRatio); })()`);
  await pickMode(2);
  try {
    await waitFor('le mode sombre', "document.documentElement.getAttribute('data-omni-theme') === 'dark'");
    await waitFor('le trait clair en mode sombre', `(() => { const canvas = document.querySelector(${JSON.stringify(surface)}); const data = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data; let count = 0; let red = 0; for (let index = 3; index < data.length; index += 4) if (data[index] > 0) { count++; red += data[index - 3]; } return count > 100 && red / count > 150; })()`);
    step(`trait de la couleur du thème : sombre en clair (rouge moyen ${light.red.toFixed(0)}), clair en sombre, redessiné au redimensionnement`);
  } finally {
    await pickMode(1);
  }

  // 7. A read-only pad draws nothing and disables its toolbar; a disabled one leaves the tab order.
  await waitForStrokes('la vague seule', 1);
  await clickOn('[role=switch][aria-label="Read only"]');
  await waitFor('la zone en lecture seule', `document.querySelector(${JSON.stringify(surface)}).dataset.omniInkLocked === 'true'`);
  frame = await box();
  await drag([at(frame, 0.2, 0.9), at(frame, 0.6, 0.9)]);
  await pause(300);
  check((await strokes()) === 1, 'Un trait est ajouté sur une zone en lecture seule.');
  check(await evaluate("[...document.querySelectorAll('#ink-demo .omni-ink-canvas__toolbar button')].every(button => button.disabled)"), 'La barre (crayon, gomme, annuler, effacer) reste active en lecture seule.');
  await clickOn('[role=switch][aria-label="Read only"]');
  await clickOn('[role=switch][aria-label="Disabled"]');
  await waitFor('la zone désactivée', `!document.querySelector(${JSON.stringify(surface)}).hasAttribute('tabindex')`);
  await clickOn('[role=switch][aria-label="Disabled"]');
  await waitFor('la zone réactivée', `document.querySelector(${JSON.stringify(surface)}).getAttribute('tabindex') === '0'`);
  step('zone en lecture seule : aucun trait, barre désactivée ; zone désactivée : hors de la tabulation');

  // 8. The signature shape: the height follows the width.
  await clickOn('[role=switch][aria-label="Signature shape"]');
  await waitFor('la forme de signature', `(() => { const b = document.querySelector(${JSON.stringify(surface)}).getBoundingClientRect(); return Math.abs(b.width / b.height - 3) < 0.05; })()`);
  await waitFor('le dessin gardé à la nouvelle forme', `(() => { const c = document.querySelector(${JSON.stringify(surface)}); return c.height === Math.max(1, Math.round(c.clientHeight * (devicePixelRatio || 1))); })()`);
  check((await pixels()).count > 100, 'Le dessin disparaît quand la forme change.');
  step('forme de signature : hauteur égale au tiers de la largeur, dessin gardé');

  // 9. The PNG export: the image has the asked size and is transparent where nothing is drawn.
  await clickOn('#ink-export');
  await waitFor("l'image exportée", "document.querySelector('#ink-export-image')?.src?.startsWith('data:image/png;base64,') === true");
  const exported = await evaluate(`(async () => {
    const image = document.getElementById('ink-export-image');
    await image.decode();
    const canvas = document.createElement('canvas');
    canvas.width = image.naturalWidth;
    canvas.height = image.naturalHeight;
    const context = canvas.getContext('2d');
    context.drawImage(image, 0, 0);
    const data = context.getImageData(0, 0, canvas.width, canvas.height).data;
    let opaque = 0; let red = 0;
    for (let index = 3; index < data.length; index += 4) if (data[index] > 0) { opaque++; red += data[index - 3]; }
    return JSON.stringify({ width: canvas.width, height: canvas.height, corner: data[3], opaque, red: opaque ? red / opaque : null });
  })()`).then(JSON.parse);
  check(exported.width === 600 && exported.height === 200, `Image exportée de ${exported.width} par ${exported.height} pixels, pas 600 par 200.`);
  check(exported.corner === 0, `Le coin de l'image exportée est opaque (${exported.corner}).`);
  check(exported.opaque > 100 && exported.red < 10, `Trait de couleur de thème pas noir dans l'image exportée : ${JSON.stringify(exported)}.`);
  await clickOn('[role=switch][aria-label="Signature shape"]');
  step(`export PNG : ${exported.width} par ${exported.height} pixels, fond transparent, trait de couleur de thème en noir`);

  // 10. The eraser. The page counts what the canvas reports: strokes added (drawn here) and removed (eraser, undo, clear).
  const counter = id => evaluate(`Number.parseInt(document.getElementById(${JSON.stringify(id)}).textContent, 10)`);
  const opaqueIn = (from, to) => evaluate(`(() => { const canvas = document.querySelector(${JSON.stringify(surface)}); const data = canvas.getContext('2d').getImageData(0, Math.floor(canvas.height * ${from}), canvas.width, Math.floor(canvas.height * (${to} - ${from}))).data; let count = 0; for (let index = 3; index < data.length; index += 4) if (data[index] > 0) count++; return count; })()`);
  const toolbar = '#ink-demo .omni-ink-canvas__toolbar';
  await focusSurface();
  await key('Delete', 'Delete', 46);
  await waitForStrokes('le dessin vidé', 0);
  const added = await counter('ink-added');
  const removed = await counter('ink-removed');
  frame = await box();
  await drag([at(frame, 0.1, 0.25), at(frame, 0.5, 0.25), at(frame, 0.9, 0.25)]);
  await drag([at(frame, 0.1, 0.75), at(frame, 0.5, 0.75), at(frame, 0.9, 0.75)]);
  await waitForStrokes('deux traits horizontaux', 2);
  check((await counter('ink-added')) === added + 2 && (await counter('ink-removed')) === removed, 'StrokeAdded ne compte pas exactement les deux traits dessinés.');
  const bothStrokes = (await pixels()).count;
  check(await opaqueIn(0, 0.5) > 50 && await opaqueIn(0.5, 1) > 50, 'Les deux traits horizontaux ne sont pas dans les moitiés haute et basse.');
  await clickOn(`${toolbar} button[aria-label="Eraser"]`);
  await waitFor('la gomme pressée', `document.querySelector('${toolbar} button[aria-label="Eraser"]').getAttribute('aria-pressed') === 'true' && document.querySelector('${toolbar} button[aria-label="Pen"]').getAttribute('aria-pressed') === 'false'`);
  const cursor = await evaluate(`getComputedStyle(document.querySelector(${JSON.stringify(surface)})).cursor`);
  check(cursor.startsWith('url('), `Le curseur de la gomme n'est pas dessiné : ${cursor}.`);
  // A vertical path from above the first stroke to between the two removes the first one only.
  await drag([at(frame, 0.5, 0.05), at(frame, 0.5, 0.2), at(frame, 0.5, 0.4)]);
  await waitForStrokes('le trait du haut effacé', 1);
  check(await opaqueIn(0, 0.5) === 0 && await opaqueIn(0.5, 1) > 50, 'Le trait du haut reste dessiné, ou celui du bas a disparu.');
  check((await pixels()).count < bothStrokes, 'La gomme ne retire aucun pixel.');
  check((await counter('ink-removed')) === removed + 1 && (await counter('ink-added')) === added + 2, 'StrokesRemoved ne compte pas le trait effacé, ou StrokeAdded a bougé.');
  // A path that touches nothing changes nothing; a click on the stroke removes it, with the eraser only.
  await drag([at(frame, 0.2, 0.4), at(frame, 0.8, 0.4)]);
  await pause(300);
  check((await strokes()) === 1, 'Un chemin loin de tout trait en a effacé un.');
  await drag([at(frame, 0.3, 0.75)]);
  await waitForStrokes('le trait du bas effacé d\'un clic', 0);
  check((await pixels()).count === 0 && (await counter('ink-removed')) === removed + 2, 'Le canevas garde des pixels après la gomme, ou StrokesRemoved ne compte pas deux traits.');
  step('gomme : chemin qui retire le seul trait touché (hautes, basses, pixels), clic qui retire un trait, curseur dessiné, StrokeAdded et StrokesRemoved comptés');

  // 11. What the host gives is not reported; the pen comes back; the eraser is blocked when read only; undo counts too.
  await clickOn('#ink-sample');
  await waitForStrokes('le modèle donné par l\'hôte', 3);
  await pause(300);
  check((await counter('ink-added')) === added + 2 && (await counter('ink-removed')) === removed + 2, 'Une valeur donnée par l\'hôte lève StrokeAdded ou StrokesRemoved.');
  await clickOn(`${toolbar} button[aria-label="Pen"]`);
  await waitFor('le crayon pressé', `document.querySelector('${toolbar} button[aria-label="Pen"]').getAttribute('aria-pressed') === 'true'`);
  await drag([at(frame, 0.05, 0.95), at(frame, 0.95, 0.95)]);
  await waitForStrokes('un trait au crayon après la gomme', 4);
  check((await counter('ink-added')) === added + 3, 'Le crayon ne dessine plus après la gomme.');
  await clickOn('[role=switch][aria-label="Eraser"]');
  await waitFor('la gomme choisie par la page', `document.querySelector(${JSON.stringify(surface)}).dataset.omniInkTool === 'eraser'`);
  await clickOn('[role=switch][aria-label="Read only"]');
  await waitFor('la zone en lecture seule', `document.querySelector(${JSON.stringify(surface)}).dataset.omniInkLocked === 'true'`);
  await drag([at(frame, 0.05, 0.95), at(frame, 0.95, 0.95)]);
  await pause(300);
  check((await strokes()) === 4 && (await counter('ink-removed')) === removed + 2, 'La gomme efface sur une zone en lecture seule.');
  await clickOn('[role=switch][aria-label="Read only"]');
  await waitFor('la zone modifiable', `document.querySelector(${JSON.stringify(surface)}).dataset.omniInkLocked !== 'true'`);
  await clickOn('[role=switch][aria-label="Eraser"]');
  await waitFor('le crayon rendu par la page', `document.querySelector(${JSON.stringify(surface)}).dataset.omniInkTool === 'pen'`);
  await focusSurface();
  await ctrlZ();
  await waitForStrokes('Ctrl+Z après le crayon', 3);
  check((await counter('ink-removed')) === removed + 3, 'Ctrl+Z ne lève pas StrokesRemoved.');
  step('valeur de l\'hôte sans événement, crayon rendu après la gomme, gomme sans effet en lecture seule, Ctrl+Z compté');

  // 12. The frame can be made transparent by a parent: the surface itself never paints a background.
  const backgrounds = () => evaluate(`JSON.stringify({ surface: getComputedStyle(document.querySelector(${JSON.stringify(surface)})).backgroundColor, frame: getComputedStyle(document.querySelector('#ink-demo .omni-ink-canvas__frame')).backgroundColor })`).then(JSON.parse);
  const before = await backgrounds();
  check(before.surface === 'rgba(0, 0, 0, 0)' && before.frame !== 'rgba(0, 0, 0, 0)', `Fonds avant : ${JSON.stringify(before)} (la surface doit être transparente, le cadre opaque).`);
  await evaluate("document.getElementById('ink-demo').style.setProperty('--omni-ink-background', 'transparent')");
  const after = await backgrounds();
  await evaluate("document.getElementById('ink-demo').style.removeProperty('--omni-ink-background')");
  check(after.surface === 'rgba(0, 0, 0, 0)' && after.frame === 'rgba(0, 0, 0, 0)', `Fonds après la surcharge du parent : ${JSON.stringify(after)}.`);
  step('fond : la surface est transparente, le cadre le devient par --omni-ink-background posée par un parent');
}
