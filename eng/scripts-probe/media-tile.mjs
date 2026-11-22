// The video tile (OmniMediaTile, PLAN-002 lot 2), driven in a real Chromium under the strict CSP on its showcase page, at
// 1440 and at 375 px. Two tiles show the same demonstration stream (an animated canvas of 640 x 480 captured by
// captureStream, framed in yellow so the edges of the picture can be seen), the first in a 16:9 tile, the second in a square one.
// Checked, each time with the real video and real pointer input:
// - the video plays (currentTime advances, videoWidth is 640) in both tiles;
// - the drawing overlay lies exactly on the picture the video shows, within 1 px: in Contain the picture without its bands, in
//   Cover the whole picture, overflowing the tile (the expected rectangle is computed here from the video element's own box and
//   the video size, independently of the script), and the pixels agree: on a screenshot the yellow frame of the picture is
//   just inside the edge of the overlay and the dark band just outside it;
// - a stroke drawn with the mouse on the first tile appears on the second (pixels on its canvas, at the same relative
//   place), in Contain and in Cover; an eraser path on the first removes it from the second; a stroke drawn on the second
//   appears on the first; the page is the transport, as the component transports nothing;
// - the PNG export has the native size of the video, contains video pixels and stroke pixels at the right place;
// - full screen (the tile follows the browser: button label, overlay replaced on the new box) when the browser grants it;
// - the no-stream state (initials on a disc, no overlay size) and its return.

export async function mediaTileSteps(session, results) {
  const { evaluate, waitFor, check, clickOn, pause, mouse, send } = session;
  const step = session.stepRecorder(results);

  const tiles = ['media-tile-a', 'media-tile-b'];
  const surface = tile => `#${tile} canvas.omni-ink-canvas__surface`;
  const viewport = width => send('Emulation.setDeviceMetricsOverride', { width, height: 900, deviceScaleFactor: 1, mobile: false });
  const toggle = label => clickOn(`[role=switch][aria-label="${label}"]`);
  const bringIntoView = tile => evaluate(`document.getElementById(${JSON.stringify(tile)}).scrollIntoView({ block: 'center' })`);

  const geometry = tile => evaluate(`(() => {
    const root = document.getElementById(${JSON.stringify(tile)});
    const video = root.querySelector('video');
    const picture = root.querySelector('[data-omni-media-picture]');
    const rect = element => { const box = element.getBoundingClientRect(); return { left: box.left, top: box.top, width: box.width, height: box.height }; };
    return JSON.stringify({ video: rect(video), picture: picture ? rect(picture) : null, mediaWidth: video.videoWidth, mediaHeight: video.videoHeight, time: video.currentTime, ready: video.readyState, paused: video.paused, fit: root.dataset.omniMediaFit, hasStream: video.srcObject !== null });
  })()`).then(JSON.parse);

  // What the picture is, from the video element's box and the size of the video: the object-fit arithmetic, written again here.
  const expected = geo => {
    const horizontal = geo.video.width / geo.mediaWidth;
    const vertical = geo.video.height / geo.mediaHeight;
    const scale = geo.fit === 'cover' ? Math.max(horizontal, vertical) : Math.min(horizontal, vertical);
    const width = geo.mediaWidth * scale;
    const height = geo.mediaHeight * scale;
    return { left: geo.video.left + (geo.video.width - width) / 2, top: geo.video.top + (geo.video.height - height) / 2, width, height };
  };
  const within = (actual, wanted, tolerance) => ['left', 'top', 'width', 'height'].every(name => Math.abs(actual[name] - wanted[name]) <= tolerance);
  const describe = rect => JSON.stringify(Object.fromEntries(Object.entries(rect).map(([name, value]) => [name, Math.round(value * 100) / 100])));

  // The overlay must be the picture: the same rectangle, to the pixel.
  const checkOverlay = async (tile, fit) => {
    await waitFor(`l'incrustation de ${tile} placée`, `(() => { const picture = document.querySelector('#${tile} [data-omni-media-picture]'); return picture !== null && picture.getBoundingClientRect().width > 0; })()`);
    await pause(200);
    const geo = await geometry(tile);
    check(geo.fit === fit, `${tile} est en ${geo.fit}, pas en ${fit}.`);
    const wanted = expected(geo);
    check(within(geo.picture, wanted, 1), `${tile} : l'incrustation ${describe(geo.picture)} n'est pas l'image ${describe(wanted)} (${fit}).`);
    return { geo, wanted };
  };

  // Pixels of the screen, from a real screenshot (nothing the page drew itself): [r, g, b] at each CSS pixel asked.
  const screenPixels = async points => {
    const shot = await send('Page.captureScreenshot', { format: 'png' });
    return evaluate(`(async () => {
      const image = new Image();
      image.src = 'data:image/png;base64,${shot.data}';
      await image.decode();
      const canvas = document.createElement('canvas');
      canvas.width = image.naturalWidth;
      canvas.height = image.naturalHeight;
      const context = canvas.getContext('2d');
      context.drawImage(image, 0, 0);
      return JSON.stringify(${JSON.stringify(points)}.map(([x, y]) => [...context.getImageData(Math.round(x), Math.round(y), 1, 1).data].slice(0, 3)));
    })()`).then(JSON.parse);
  };
  const isYellow = ([r, g, b]) => r > 200 && g > 170 && b < 90;
  const isBand = ([r, g, b]) => r < 40 && g < 40 && b < 50;

  // The frame of the picture is yellow and the bands beside it are dark: two pixels, 2 px inside and 4 px outside an edge.
  const checkEdge = async (tile, edge) => {
    await bringIntoView(tile);
    await pause(200);
    const geo = await geometry(tile);
    const box = geo.picture;
    const middleX = box.left + box.width / 2;
    const middleY = box.top + box.height / 2;
    const points = edge === 'left'
      ? [[box.left + 2, middleY], [box.left - 4, middleY]]
      : [[middleX, box.top + 2], [middleX, box.top - 4]];
    const [inside, outside] = await screenPixels(points);
    check(isYellow(inside), `${tile} : le cadre jaune de l'image n'est pas 2 px à l'intérieur du bord ${edge} de l'incrustation (pixel ${inside}).`);
    // In Cover the picture overflows the tile on one axis only; the edge on the other axis is the tile's own, so only the bands of Contain are tested outside.
    if (geo.fit === 'contain') check(isBand(outside), `${tile} : le bord ${edge} de l'incrustation n'est pas collé à la bande sombre (pixel ${outside} 4 px dehors ; image ${describe(box)}, vidéo ${describe(geo.video)}).`);
  };

  const opaque = tile => evaluate(`(() => {
    const canvas = document.querySelector(${JSON.stringify(surface(tile))});
    const data = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data;
    let count = 0; let left = canvas.width; let right = -1; let top = canvas.height; let bottom = -1;
    for (let index = 3; index < data.length; index += 4) {
      if (data[index] > 0) {
        const pixel = (index - 3) / 4;
        const x = pixel % canvas.width; const y = Math.floor(pixel / canvas.width);
        count++; left = Math.min(left, x); right = Math.max(right, x); top = Math.min(top, y); bottom = Math.max(bottom, y);
      }
    }
    return JSON.stringify({ count, width: canvas.width, height: canvas.height, left: left / canvas.width, right: (right + 1) / canvas.width, top: top / canvas.height, bottom: (bottom + 1) / canvas.height });
  })()`).then(JSON.parse);
  const counts = tile => evaluate(`Number.parseInt(document.getElementById('media-tile-count-${tile}').textContent.replace(/^\\D+/, ''), 10)`);

  // A path across the overlay of a tile, in fractions of the picture, with the real mouse.
  const drag = async (tile, fractions) => {
    await bringIntoView(tile);
    await pause(150);
    const { picture } = await geometry(tile);
    const points = fractions.map(([x, y]) => ({ x: picture.left + picture.width * x, y: picture.top + picture.height * y }));
    await mouse('mouseMoved', points[0], { button: 'none' });
    await mouse('mousePressed', points[0]);
    for (const point of points.slice(1)) {
      await mouse('mouseMoved', point, { button: 'left', buttons: 1 });
      await pause(25);
    }
    await mouse('mouseReleased', points.at(-1));
  };
  const near = (actual, wanted, tolerance) => Math.abs(actual - wanted) <= tolerance;
  const sameShape = (first, second) => ['left', 'right', 'top', 'bottom'].every(name => near(first[name], second[name], 0.03));
  const toolbarButton = (tile, label) => `#${tile} .omni-media-tile__toolbar button[aria-label="${label}"]`;

  for (const width of [1440, 375]) {
    const at = `${width} px`;
    await viewport(width);
    await session.visit('/components/media-stage', '#media-stage-demo');
    await session.visit('/components/media-tile', '#media-tile-a video');

    // 1. The demo stream plays in both tiles, through a real srcObject.
    for (const tile of tiles) {
      await waitFor(`la vidéo de ${tile}`, `(() => { const video = document.querySelector('#${tile} video'); return video && video.videoWidth === 640 && video.readyState >= 2 && video.currentTime > 0; })()`);
    }
    const before = await geometry(tiles[0]);
    await pause(700);
    const after = await geometry(tiles[0]);
    const other = await geometry(tiles[1]);
    check(after.time > before.time + 0.3 && !after.paused, `La vidéo de la première tuile ne joue pas : ${before.time} puis ${after.time}.`);
    check(after.mediaWidth === 640 && after.mediaHeight === 480 && other.mediaWidth === 640 && other.time > 0.3, `Tailles ou temps de la vidéo inattendus : ${JSON.stringify({ after, other })}.`);
    check(await evaluate("document.querySelector('#media-tile-a video').muted === true"), 'La vidéo n\'est pas muette par défaut.');
    step(`${at} : la vidéo joue dans les deux tuiles (currentTime ${before.time.toFixed(2)} puis ${after.time.toFixed(2)}, ${after.mediaWidth} par ${after.mediaHeight})`);

    // 2. Contain: the overlay is the picture without its bands, and the pixels agree.
    const contained = await checkOverlay('media-tile-a', 'contain');
    const containedSquare = await checkOverlay('media-tile-b', 'contain');
    check(contained.geo.picture.width < contained.geo.video.width - 10 && contained.geo.picture.height <= contained.geo.video.height + 1, `Tuile 16:9 : l'image 4:3 n'est pas plus étroite que la tuile (${describe(contained.geo.picture)} dans ${describe(contained.geo.video)}).`);
    check(containedSquare.geo.picture.height < containedSquare.geo.video.height - 10, 'Tuile carrée : l\'image 4:3 n\'a pas de bandes en haut et en bas.');
    await checkEdge('media-tile-a', 'left');
    await checkEdge('media-tile-b', 'top');
    step(`${at} : Contain, incrustation ${describe(contained.geo.picture)} pour l'image ${describe(contained.wanted)} (16:9), ${describe(containedSquare.geo.picture)} (carrée), bandes exclues, pixels du cadre jaune collés aux bords`);

    // 3. A stroke drawn on the first tile appears on the second, at the same relative place.
    check((await opaque('media-tile-a')).count === 0 && (await opaque('media-tile-b')).count === 0, 'Un canevas commence non vide.');
    await drag('media-tile-a', [[0.2, 0.3], [0.35, 0.45], [0.5, 0.6], [0.65, 0.45], [0.8, 0.3]]);
    await waitFor('le trait sur la seconde tuile', `(() => { const canvas = document.querySelector(${JSON.stringify(surface('media-tile-b'))}); const data = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data; let count = 0; for (let index = 3; index < data.length; index += 4) if (data[index] > 0) count++; return count > 50; })()`);
    const drawn = await opaque('media-tile-a');
    const copied = await opaque('media-tile-b');
    check(drawn.count > 50 && sameShape(drawn, copied), `Le trait de la première tuile n'est pas au même endroit relatif sur la seconde : ${JSON.stringify({ drawn, copied })}.`);
    check((await counts('a')) === 1 && (await counts('b')) === 1, 'Les compteurs de traits ne valent pas un partout.');
    step(`${at} : un trait à la souris sur la première tuile apparaît sur la seconde (${copied.count} pixels), même boîte relative à 3 % près`);

    // 4. Cover: the overlay is the whole picture, overflowing; the stroke stays where it was; the pixels agree.
    await toggle('Fill the tile');
    await waitFor('les tuiles en Cover', "document.getElementById('media-tile-a').dataset.omniMediaFit === 'cover'");
    const covered = await checkOverlay('media-tile-a', 'cover');
    const coveredSquare = await checkOverlay('media-tile-b', 'cover');
    check(covered.geo.picture.height > covered.geo.video.height + 10 && Math.abs(covered.geo.picture.width - covered.geo.video.width) <= 1, `Cover 16:9 : l'image ne remplit pas la largeur en débordant en hauteur (${describe(covered.geo.picture)} dans ${describe(covered.geo.video)}).`);
    check(coveredSquare.geo.picture.width > coveredSquare.geo.video.width + 10 && Math.abs(coveredSquare.geo.picture.height - coveredSquare.geo.video.height) <= 1, 'Cover carré : l\'image ne remplit pas la hauteur en débordant en largeur.');
    await checkEdge('media-tile-a', 'left');
    check(sameShape(await opaque('media-tile-a'), drawn) && sameShape(await opaque('media-tile-b'), copied), 'Le trait a bougé quand l\'ajustement est passé en Cover.');
    // A stroke in the visible middle of both tiles in Cover, propagated as well.
    await drag('media-tile-a', [[0.3, 0.45], [0.5, 0.5], [0.7, 0.55]]);
    await waitFor('le second trait en Cover', "Number.parseInt(document.getElementById('media-tile-count-b').textContent.replace(/^\\D+/, ''), 10) === 2");
    check((await counts('a')) === 2, 'Le second trait en Cover n\'est pas compté sur la première tuile.');
    step(`${at} : Cover, incrustation ${describe(covered.geo.picture)} pour l'image entière débordant de la tuile ${describe(covered.geo.video)}, trait gardé et nouveau trait propagé`);

    // 5. The eraser on the first tile removes the strokes from the second.
    await toggle('Fill the tile');
    await waitFor('les tuiles en Contain', "document.getElementById('media-tile-a').dataset.omniMediaFit === 'contain'");
    await checkOverlay('media-tile-a', 'contain');
    await bringIntoView('media-tile-a');
    await clickOn(toolbarButton('media-tile-a', 'Eraser'));
    await waitFor('la gomme pressée', `document.querySelector('${toolbarButton('media-tile-a', 'Eraser')}').getAttribute('aria-pressed') === 'true'`);
    // The V of the first stroke is crossed at its lowest point; the second stroke (a nearly flat line at mid-height) at 0.5.
    await drag('media-tile-a', [[0.5, 0.2], [0.5, 0.45], [0.5, 0.75]]);
    await waitFor('les traits effacés des deux tuiles', "document.getElementById('media-tile-count-a').textContent.includes(' 0 ') && document.getElementById('media-tile-count-b').textContent.includes(' 0 ')");
    check((await opaque('media-tile-a')).count === 0 && (await opaque('media-tile-b')).count === 0, 'Un canevas garde des pixels après la gomme.');
    await clickOn(toolbarButton('media-tile-a', 'Pen'));
    await waitFor('le crayon rendu', `document.querySelector('${toolbarButton('media-tile-a', 'Pen')}').getAttribute('aria-pressed') === 'true'`);
    step(`${at} : la gomme passée sur la première tuile retire les traits de la seconde (aucun pixel, compteurs à zéro)`);

    // 6. And the other way: a stroke drawn on the second appears on the first.
    await drag('media-tile-b', [[0.15, 0.7], [0.5, 0.8], [0.85, 0.7]]);
    await waitFor('le trait de la seconde sur la première', "document.getElementById('media-tile-count-a').textContent.includes(' 1 ')");
    const fromSecond = await opaque('media-tile-b');
    const onFirst = await opaque('media-tile-a');
    check(onFirst.count > 50 && sameShape(fromSecond, onFirst), `Le trait de la seconde tuile n'est pas au même endroit relatif sur la première : ${JSON.stringify({ fromSecond, onFirst })}.`);
    await clickOn(toolbarButton('media-tile-b', 'Clear drawing'));
    await waitFor('le dessin effacé par la barre', "document.getElementById('media-tile-count-a').textContent.includes(' 0 ')");
    step(`${at} : un trait de la seconde tuile apparaît sur la première, Effacer de la barre le retire des deux`);

    // 7. The export: the frame at its native size with the strokes over it, at the right place.
    await drag('media-tile-a', [[0.1, 0.5], [0.5, 0.5], [0.9, 0.5]]);
    await waitFor('le trait à exporter', "document.getElementById('media-tile-count-a').textContent.includes(' 1 ')");
    await clickOn('#media-tile-export');
    await waitFor("l'image exportée", "document.querySelector('#media-tile-export-image')?.src?.startsWith('data:image/png;base64,') === true");
    const exported = await evaluate(`(async () => {
      const image = document.getElementById('media-tile-export-image');
      await image.decode();
      const canvas = document.createElement('canvas');
      canvas.width = image.naturalWidth;
      canvas.height = image.naturalHeight;
      const context = canvas.getContext('2d');
      context.drawImage(image, 0, 0);
      const data = context.getImageData(0, 0, canvas.width, canvas.height).data;
      let stroke = 0; let video = 0; let transparent = 0; let top = canvas.height; let bottom = -1; let left = canvas.width; let right = -1;
      for (let index = 0; index < data.length; index += 4) {
        const pixel = index / 4; const x = pixel % canvas.width; const y = Math.floor(pixel / canvas.width);
        if (data[index + 3] < 255) transparent++;
        if (data[index] > 240 && data[index + 1] > 40 && data[index + 1] < 80 && data[index + 2] > 30 && data[index + 2] < 70) { stroke++; top = Math.min(top, y); bottom = Math.max(bottom, y); left = Math.min(left, x); right = Math.max(right, x); } else video++;
      }
      const frame = [...context.getImageData(2, canvas.height / 2, 1, 1).data];
      return JSON.stringify({ width: canvas.width, height: canvas.height, stroke, video, transparent, top, bottom, left, right, frame });
    })()`).then(JSON.parse);
    check(exported.width === 640 && exported.height === 480, `Image exportée de ${exported.width} par ${exported.height}, pas la taille native 640 par 480.`);
    check(exported.transparent === 0 && exported.video > exported.width * exported.height * 0.9, `L'image exportée n'est pas l'image vidéo : ${JSON.stringify(exported)}.`);
    check(isYellow(exported.frame), `Le cadre jaune de la vidéo manque dans l'image exportée : ${exported.frame}.`);
    check(exported.stroke > 300 && near(exported.top, 240, 4) && near(exported.bottom, 240, 4) && near(exported.left, 64, 5) && near(exported.right, 576, 5), `Le trait n'est pas à sa place dans l'image exportée : ${JSON.stringify(exported)}.`);
    step(`${at} : export PNG de ${exported.width} par ${exported.height} (taille native), ${exported.video} pixels de vidéo et ${exported.stroke} de trait, le trait à mi-hauteur entre x ${exported.left} et ${exported.right}`);
    await bringIntoView('media-tile-a');
    await pause(300);
    await clickOn(toolbarButton('media-tile-a', 'Clear drawing'));
    await waitFor('le dessin vidé', "document.getElementById('media-tile-count-a').textContent.includes(' 0 ')");

    // 8. Full screen, when the browser grants it: the tile follows it, the overlay is placed on the new box.
    await clickOn('#media-tile-a .omni-media-tile__fullscreen');
    await waitFor('la tuile en plein écran', "document.fullscreenElement?.id === 'media-tile-a'", 5_000);
    await waitFor('le bouton qui propose de quitter', "document.querySelector('#media-tile-a .omni-media-tile__fullscreen').getAttribute('aria-label') === 'Exit full screen'");
    await pause(300);
    const full = await checkOverlay('media-tile-a', 'contain');
    check(full.geo.video.width >= width - 1 && full.geo.video.height > full.geo.video.width * 0.4, `La vidéo n'occupe pas l'écran en plein écran : ${describe(full.geo.video)}.`);
    await evaluate('document.exitFullscreen()');
    await waitFor('la sortie du plein écran suivie', "document.fullscreenElement === null && document.querySelector('#media-tile-a .omni-media-tile__fullscreen').getAttribute('aria-label') === 'Full screen'");
    await checkOverlay('media-tile-a', 'contain');
    step(`${at} : plein écran suivi (bouton « Exit full screen », vidéo ${describe(full.geo.video)}, incrustation replacée sur l'image), sortie suivie`);

    // 9. No stream: initials on a disc, no overlay, then the stream comes back.
    await toggle('Stream');
    await waitFor('les tuiles sans flux', "document.querySelectorAll('.omni-media-tile__avatar').length === 2");
    const initials = await evaluate("JSON.stringify([...document.querySelectorAll('.omni-media-tile__avatar')].map(element => element.textContent.trim()))").then(JSON.parse);
    check(initials[0] === 'AL' && initials[1] === 'GH', `Initiales ${initials} au lieu de AL (déduites du nom) et GH (données).`);
    check(await evaluate("(() => { const picture = document.querySelector('#media-tile-a [data-omni-media-picture]'); return picture.getBoundingClientRect().width === 0 && document.querySelector('#media-tile-a video').srcObject === null; })()"), 'Sans flux, l\'incrustation garde une taille ou la vidéo son flux.');
    await clickOn('#media-tile-export');
    await waitFor('le refus de l\'export', "document.getElementById('media-tile-message').textContent.includes('no Stream')");
    await toggle('Stream');
    await waitFor('la vidéo revenue', "(() => { const video = document.querySelector('#media-tile-a video'); return video.srcObject !== null && video.videoWidth === 640 && video.currentTime > 0; })()");
    await checkOverlay('media-tile-a', 'contain');
    step(`${at} : sans flux, les initiales AL et GH sur un disque, incrustation sans taille, export refusé, puis le flux revient et l'incrustation avec`);
  }
}
