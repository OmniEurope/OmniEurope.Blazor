// The map (OmniMap), driven in a real Chromium under the strict CSP on its showcase page, at 1280 and at 375 px, in the
// light then the dark mode. The tile server is never reached: CDP's Fetch domain answers every request to
// https://tile.openstreetmap.org/ with a PNG generated here (a light tile with a darker frame), and with a body that is
// no image for one tile in three ((x + y) % 3 === 0), so those tiles fail to decode as a broken server's would, without a
// network error. Checked, each time with real input:
// - the tiles are asked from the server the CSP allows, with the page's origin as their referrer, and only the tiles in
//   view and one ring around them are in the page;
// - FitToContent frames the whole route: every marker and every line inside the map, away from its edges;
// - a failed tile is hidden and leaves the surface colour (pixels of a real screenshot), no broken image;
// - a real drag pans the map (a marker follows the pointer to the pixel) without raising a click;
// - the wheel zooms one level around the pointer (the marker under it stays under it), the + and - buttons zoom;
// - Tab reaches the map, its focus ring shows, the arrows pan and + zooms;
// - a real click on the map raises OnMapClick with the place under the pointer (computed here from the view the page shows,
//   with Web Mercator written again independently of the script);
// - a marker clicked, then Enter and Space on focused markers, raise OnMarkerClick;
// - the package tooltip shows the text of a line under the pointer;
// - the attribution is visible in a corner, a link to the OpenStreetMap copyright; the controls, the attribution and the
//   markers follow the theme in the dark mode (contrast measured) while the lines stay readable on their casing;
// - no CSP violation and a clean console (checked by the session at the end, and here after each step).
import { crc32, deflateSync } from 'node:zlib';

const TILE_HOST = 'https://tile.openstreetmap.org/';
const SIZE = 256;

// A 256 x 256 RGB PNG: a light paper colour with a 2-pixel darker frame, so the edges of the tiles can be seen.
function tilePng() {
  const row = SIZE * 3 + 1;
  const raw = Buffer.alloc(row * SIZE);
  for (let y = 0; y < SIZE; y++) {
    raw[y * row] = 0;
    for (let x = 0; x < SIZE; x++) {
      const edge = x < 2 || y < 2 || x >= SIZE - 2 || y >= SIZE - 2;
      const [r, g, b] = edge ? [201, 195, 180] : [233, 229, 220];
      raw.set([r, g, b], y * row + 1 + x * 3);
    }
  }

  const chunk = (type, data) => {
    const length = Buffer.alloc(4);
    length.writeUInt32BE(data.length);
    const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
    const crc = Buffer.alloc(4);
    crc.writeUInt32BE(crc32(body));
    return Buffer.concat([length, body, crc]);
  };
  const header = Buffer.alloc(13);
  header.writeUInt32BE(SIZE, 0);
  header.writeUInt32BE(SIZE, 4);
  header.set([8, 2, 0, 0, 0], 8);
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', header), chunk('IDAT', deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}

// Web Mercator, written again here so that the probe does not trust the script it checks.
const project = (latitude, longitude, zoom) => {
  const size = 256 * 2 ** zoom;
  const phi = latitude * Math.PI / 180;
  return { x: (longitude + 180) / 360 * size, y: (1 - Math.asinh(Math.tan(phi)) / Math.PI) / 2 * size };
};
const unproject = (x, y, zoom) => {
  const size = 256 * 2 ** zoom;
  return { latitude: Math.atan(Math.sinh(Math.PI * (1 - 2 * y / size))) * 180 / Math.PI, longitude: x / size * 360 - 180 };
};

const luminance = ([r, g, b]) => {
  const channel = value => {
    const c = value / 255;
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
};
const contrast = (first, second) => {
  const [light, dark] = [luminance(first), luminance(second)].sort((a, b) => b - a);
  return (light + 0.05) / (dark + 0.05);
};
// A computed colour, rgb(), rgba() or color(srgb r g b / a), laid over a backdrop when it is not opaque.
const rgb = (text, backdrop = [255, 255, 255]) => {
  const numbers = text.match(/[\d.]+/g).map(Number);
  const scale = text.startsWith('color(') ? 255 : 1;
  const alpha = numbers.length > 3 ? numbers[3] : 1;
  return numbers.slice(0, 3).map((value, index) => value * scale * alpha + backdrop[index] * (1 - alpha));
};
// The contrast of a text on a translucent background, over the worst of a black and a white tile under it.
const legible = (text, background) => Math.min(contrast(rgb(text), rgb(background, [0, 0, 0])), contrast(rgb(text), rgb(background, [255, 255, 255])));

export async function mapSteps(session, results) {
  const { evaluate, waitFor, check, clickOn, pause, mouse, send, key, on } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  const png = tilePng().toString('base64');
  const broken = Buffer.from('not an image').toString('base64');
  const asked = [];
  const stop = on('Fetch.requestPaused', async params => {
    const match = params.request.url.match(/\/(\d+)\/(\d+)\/(\d+)\.png$/);
    const [, zoom, x, y] = match ? match.map(Number) : [0, -1, -1, -1];
    asked.push({ url: params.request.url, zoom, x, y, referer: params.request.headers.Referer ?? params.request.headers.referer ?? null });
    const fails = match && (x + y) % 3 === 0;
    await send('Fetch.fulfillRequest', {
      requestId: params.requestId,
      responseCode: 200,
      responseHeaders: [{ name: 'Content-Type', value: 'image/png' }, { name: 'Cache-Control', value: 'no-store' }],
      body: fails ? broken : png
    }).catch(() => { });
  });
  await send('Fetch.enable', { patterns: [{ urlPattern: `${TILE_HOST}*`, requestStage: 'Request' }] });

  const viewport = width => send('Emulation.setDeviceMetricsOverride', { width, height: 900, deviceScaleFactor: 1, mobile: false });
  const pickMode = async position => {
    await clickOn('.omni-header .omni-app-menu__trigger');
    await waitFor("le menu d'application", "document.querySelector('.omni-app-menu__mode') !== null");
    await clickOn(`.omni-app-menu__mode .omni-appearance-window__mode:nth-child(${position})`);
    await key('Escape', 'Escape', 27);
    await waitFor("le menu d'application refermé", "document.querySelector('.omni-app-menu__mode') === null");
  };

  // What the page shows: the bound view, the last click, the last marker pressed.
  const shown = () => evaluate(`(() => {
    const view = document.getElementById('map-view').textContent.match(/(-?[\\d.]+), (-?[\\d.]+), zoom (\\d+)/);
    const click = document.getElementById('map-click').textContent.match(/(-?[\\d.]+), (-?[\\d.]+)/);
    return JSON.stringify({ latitude: Number(view[1]), longitude: Number(view[2]), zoom: Number(view[3]), click: click ? [Number(click[1]), Number(click[2])] : null, marker: document.getElementById('map-marker').textContent.replace(/^Marker: /, '').trim() });
  })()`).then(JSON.parse);

  // The geometry of the map in the page: its box, its markers, the boxes of its lines, its tiles.
  const geometry = () => evaluate(`(() => {
    const map = document.getElementById('map-demo');
    const box = map.getBoundingClientRect();
    const rect = element => { const r = element.getBoundingClientRect(); return { left: r.left, top: r.top, right: r.right, bottom: r.bottom, width: r.width, height: r.height, x: r.left + r.width / 2, y: r.top + r.height / 2 }; };
    return JSON.stringify({
      box: rect(map),
      markers: [...map.querySelectorAll('[data-omni-map-marker]')].map(marker => ({ label: marker.getAttribute('aria-label'), ...rect(marker) })),
      lines: [...map.querySelectorAll('g[data-omni-map-line] .omni-map__stroke')].map(rect),
      tiles: [...map.querySelectorAll('.omni-map__tile')].map(tile => ({ ...rect(tile), src: tile.src, loaded: tile.classList.contains('omni-map__tile--loaded'), failed: tile.classList.contains('omni-map__tile--failed'), visibility: getComputedStyle(tile).visibility })),
      attribution: (() => { const corner = map.querySelector('.omni-map__attribution'); const link = corner.querySelector('a'); return { ...rect(corner), text: corner.textContent.trim(), href: link?.href ?? null, visibility: getComputedStyle(corner).visibility, color: getComputedStyle(corner).color, background: getComputedStyle(corner).backgroundColor }; })()
    });
  })()`).then(JSON.parse);
  const inside = (inner, outer, margin = 0) => inner.left >= outer.left + margin - 0.5 && inner.top >= outer.top + margin - 0.5 && inner.right <= outer.right - margin + 0.5 && inner.bottom <= outer.bottom - margin + 0.5;

  // A point of the map where nothing but the map itself is under the pointer (no marker, line, control or attribution).
  const emptySpot = (preferX, preferY) => evaluate(`(() => {
    const map = document.getElementById('map-demo');
    const box = map.getBoundingClientRect();
    // Far from every control: a finger is snapped by the browser to a button near it.
    const controls = [...map.querySelectorAll('button, a')].map(element => element.getBoundingClientRect());
    const far = (x, y) => controls.every(r => x < r.left - 36 || x > r.right + 36 || y < r.top - 36 || y > r.bottom + 36);
    for (let ring = 0; ring < 12; ring++) {
      for (const [dx, dy] of [[0, 0], [1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [-1, -1], [1, -1], [-1, 1]]) {
        const x = box.left + box.width * ${preferX} + dx * ring * 14;
        const y = box.top + box.height * ${preferY} + dy * ring * 14;
        if (x > box.left + 8 && x < box.right - 8 && y > box.top + 8 && y < box.bottom - 8 && document.elementFromPoint(x, y) === map && far(x, y)) return JSON.stringify({ x: Math.round(x), y: Math.round(y) });
      }
    }
    return 'null';
  })()`).then(JSON.parse);

  const drag = async (from, to) => {
    await mouse('mouseMoved', from, { button: 'none' });
    await mouse('mousePressed', from);
    for (let index = 1; index <= 8; index++) {
      await mouse('mouseMoved', { x: from.x + (to.x - from.x) * index / 8, y: from.y + (to.y - from.y) * index / 8 }, { buttons: 1 });
      await pause(20);
    }
    await mouse('mouseReleased', to);
  };
  const marker = (geo, label) => geo.markers.find(item => item.label === label);

  try {
    let first = true;
    for (const width of [1280, 375]) {
      const at = `${width} px`;
      await viewport(width);
      await session.visit('/components/media-stage', '#media-stage-demo');
      asked.length = 0;
      await session.visit('/components/map', '#map-demo');
      await waitFor('les tuiles chargées', "document.querySelectorAll('#map-demo .omni-map__tile--loaded').length >= 4");
      await waitFor('les repères placés', "document.querySelectorAll('#map-demo [data-omni-map-placed]').length === 4");
      await evaluate("document.getElementById('map-demo').scrollIntoView({ block: 'center' })");
      await pause(400);

      // 1. Tiles: from the allowed server, with the origin as referrer, only the view and one ring.
      const geo = await geometry();
      const origin = new URL(await evaluate('location.href')).origin;
      check(asked.length > 0 && asked.every(request => request.url.startsWith(TILE_HOST)), `Tuiles demandées ailleurs : ${JSON.stringify(asked.slice(0, 3))}.`);
      check(asked.every(request => request.referer === `${origin}/`), `Référent des tuiles inattendu : ${JSON.stringify([...new Set(asked.map(request => request.referer))])}, attendu ${origin}/.`);
      const ring = { left: geo.box.left - SIZE, top: geo.box.top - SIZE, right: geo.box.right + SIZE, bottom: geo.box.bottom + SIZE };
      const outsideRing = geo.tiles.filter(tile => tile.right <= ring.left || tile.left >= ring.right || tile.bottom <= ring.top || tile.top >= ring.bottom);
      const most = (Math.ceil(geo.box.width / SIZE) + 3) * (Math.ceil(geo.box.height / SIZE) + 3);
      check(outsideRing.length === 0 && geo.tiles.length <= most, `Tuiles hors de la vue et de son anneau : ${outsideRing.length}, ${geo.tiles.length} tuiles pour ${most} au plus.`);
      step(`${at} : ${geo.tiles.length} tuiles (au plus ${most}) demandées à ${TILE_HOST} avec le référent ${origin}/, aucune hors de la vue et de son anneau`);

      // 2. FitToContent: every marker and every line inside the map, away from the edges.
      const view = await shown();
      const loose = geo.markers.filter(item => !inside(item, geo.box, 4));
      const looseLines = geo.lines.filter(line => !inside(line, geo.box, 4));
      check(geo.markers.length === 4 && loose.length === 0 && looseLines.length === 0, `Le cadrage laisse dehors : ${JSON.stringify({ loose, looseLines, box: geo.box })}.`);
      check(view.zoom >= 12 && view.zoom <= 15, `Zoom du cadrage inattendu pour le trajet : ${view.zoom}.`);
      step(`${at} : FitToContent cadre les 4 repères et les 3 tronçons dans la carte (zoom ${view.zoom}, centre ${view.latitude}, ${view.longitude})`);

      // 3. Failed tiles are hidden and leave the surface colour.
      const failed = geo.tiles.filter(tile => tile.failed);
      check(failed.length > 0 && failed.every(tile => tile.visibility === 'hidden'), `Tuiles en échec visibles ou absentes : ${JSON.stringify(failed.slice(0, 2))}.`);
      check(geo.tiles.filter(tile => tile.loaded).every(tile => tile.visibility === 'visible'), 'Une tuile chargée est cachée.');
      const surface = rgb(await evaluate("getComputedStyle(document.getElementById('map-demo')).backgroundColor"));
      // Points inside a failed tile and inside the map, where nothing but the map itself is under the pointer.
      const spots = [];
      for (const tile of failed) {
        const left = Math.max(tile.left, geo.box.left) + 8, right = Math.min(tile.right, geo.box.right) - 8;
        const top = Math.max(tile.top, geo.box.top) + 8, bottom = Math.min(tile.bottom, geo.box.bottom) - 8;
        if (right <= left || bottom <= top) continue;
        for (const [fx, fy] of [[0.5, 0.5], [0.1, 0.1], [0.9, 0.1], [0.1, 0.9], [0.9, 0.9]]) {
          const point = { x: Math.round(left + (right - left) * fx), y: Math.round(top + (bottom - top) * fy) };
          // Away from the shadows of the zoom buttons, the attribution and the markers too, which fall on the map.
          const clear = await evaluate(`document.elementFromPoint(${point.x}, ${point.y})?.id === 'map-demo' && [...document.querySelectorAll('#map-demo .omni-map__controls, #map-demo .omni-map__attribution, #map-demo [data-omni-map-marker]')].every(element => { const r = element.getBoundingClientRect(); return ${point.x} < r.left - 16 || ${point.x} > r.right + 16 || ${point.y} < r.top - 16 || ${point.y} > r.bottom + 16; })`);
          if (clear) spots.push(point);
        }
      }
      check(spots.length > 0, `Aucun point d'une tuile en échec dans la carte à mesurer : ${JSON.stringify(failed.map(tile => [tile.left, tile.top]))}, vue ${JSON.stringify(geo.box)}.`);
      const shot = await send('Page.captureScreenshot', { format: 'png' });
      const pixels = await evaluate(`(async () => { const image = new Image(); image.src = 'data:image/png;base64,${shot.data}'; await image.decode(); const canvas = document.createElement('canvas'); canvas.width = image.naturalWidth; canvas.height = image.naturalHeight; const context = canvas.getContext('2d'); context.drawImage(image, 0, 0); return JSON.stringify(${JSON.stringify(spots)}.map(point => [...context.getImageData(point.x, point.y, 1, 1).data].slice(0, 3))); })()`).then(JSON.parse);
      const wrong = pixels.map((pixel, index) => ({ pixel, at: spots[index] })).filter(({ pixel }) => !pixel.every((value, index) => Math.abs(value - surface[index]) <= 3));
      check(wrong.length === 0, `${at} : une tuile en échec ne laisse pas la surface ${surface} : ${JSON.stringify(wrong.slice(0, 3))} (bons : ${pixels.length - wrong.length} sur ${pixels.length}), carte ${JSON.stringify(geo.box)}, sous le point ${wrong.length ? await evaluate(`JSON.stringify(document.elementsFromPoint(${wrong[0].at.x}, ${wrong[0].at.y}).slice(0, 6).map(element => element.tagName + '.' + element.className))`) : ''}.`);
      step(`${at} : ${failed.length} tuiles en échec cachées, la surface de la carte visible à leur place (${spots.length} pixels d'une capture), aucune image cassée`);

      // 4. Attribution: visible, inside the map, a link to the copyright page.
      check(geo.attribution.visibility === 'visible' && inside(geo.attribution, geo.box) && geo.attribution.width > 40 && geo.attribution.text === '© OpenStreetMap contributors', `Attribution absente ou dehors : ${JSON.stringify(geo.attribution)}.`);
      check(geo.attribution.href === 'https://www.openstreetmap.org/copyright', `Lien d'attribution : ${geo.attribution.href}.`);
      check(legible(geo.attribution.color, geo.attribution.background) >= 4.5, `Attribution peu lisible : ${geo.attribution.color} sur ${geo.attribution.background}.`);
      step(`${at} : attribution « ${geo.attribution.text} » visible dans le coin, lien vers la page de copyright`);

      // 5. A real drag pans the map: a marker follows the pointer, and no click is raised.
      const start = marker(geo, 'Start: Place Flagey');
      const from = await emptySpot(0.5, 0.35);
      check(from !== null, 'Aucun point libre de la carte pour glisser.');
      const shift = { x: width > 400 ? -120 : -60, y: -50 };
      const viewBefore = await evaluate("document.getElementById('map-view').textContent");
      await drag(from, { x: from.x + shift.x, y: from.y + shift.y });
      await waitFor('la vue déplacée liée', `document.getElementById('map-view').textContent !== ${JSON.stringify(viewBefore)}`);
      const dragged = await geometry();
      const moved = marker(dragged, 'Start: Place Flagey');
      check(Math.abs(moved.x - start.x - shift.x) <= 2 && Math.abs(moved.y - start.y - shift.y) <= 2, `Le repère n'a pas suivi le glisser : ${JSON.stringify({ start, moved, shift })}.`);
      check((await shown()).click === null, 'Le glisser a levé un clic sur la carte.');
      step(`${at} : un vrai glisser de ${shift.x}, ${shift.y} déplace la carte (le repère de départ suit au pixel), sans clic`);

      // 6. The wheel zooms one level around the pointer; the + and - buttons zoom.
      const before = await shown();
      const anchor = marker(dragged, 'Stop: Gare Centrale');
      await mouse('mouseMoved', { x: anchor.x, y: anchor.y }, { button: 'none' });
      await send('Input.dispatchMouseEvent', { type: 'mouseWheel', x: anchor.x, y: anchor.y, deltaX: 0, deltaY: -120 });
      await waitFor('le zoom à la molette', `document.getElementById('map-view').textContent.endsWith('zoom ${before.zoom + 1}')`);
      await pause(150);
      const wheeled = marker(await geometry(), 'Stop: Gare Centrale');
      check(Math.abs(wheeled.x - anchor.x) <= 2 && Math.abs(wheeled.y - anchor.y) <= 2, `La molette n'a pas zoomé autour du pointeur : ${JSON.stringify({ anchor, wheeled })}.`);
      await clickOn('#map-demo .omni-map__controls button[aria-label="Zoom out"]');
      await waitFor('le zoom arrière au bouton', `document.getElementById('map-view').textContent.endsWith('zoom ${before.zoom}')`);
      await clickOn('#map-demo .omni-map__controls button[aria-label="Zoom out"]');
      await waitFor('le second zoom arrière', `document.getElementById('map-view').textContent.endsWith('zoom ${before.zoom - 1}')`);
      await clickOn('#map-demo .omni-map__controls button[aria-label="Zoom in"]');
      await waitFor('le zoom avant au bouton', `document.getElementById('map-view').textContent.endsWith('zoom ${before.zoom}')`);
      step(`${at} : la molette zoome de ${before.zoom} à ${before.zoom + 1} autour du pointeur (le repère sous lui ne bouge pas), les boutons - et + zooment`);

      // 7. Tab reaches the map, its ring shows; the arrows pan, + zooms.
      await evaluate("document.querySelector('[role=switch][aria-label=\"Fit to the route\"]').focus()");
      for (let tries = 0; tries < 8 && await evaluate("document.activeElement?.id") !== 'map-demo'; tries++) {
        await key('Tab', 'Tab', 9);
      }
      check(await evaluate("document.activeElement?.id === 'map-demo' && document.activeElement.matches(':focus-visible') && getComputedStyle(document.activeElement).boxShadow !== 'none'"), 'Tab n\'atteint pas la carte, ou son anneau de focus ne se voit pas.');
      const keyed = marker(await geometry(), 'Start: Place Flagey');
      await key('ArrowRight', 'ArrowRight', 39);
      await waitFor('la carte déplacée au clavier', `(() => { const box = document.querySelector('#map-demo [aria-label="Start: Place Flagey"]').getBoundingClientRect(); return Math.abs(box.left + box.width / 2 - ${keyed.x} + 80) <= 2; })()`);
      const zoomBefore = (await shown()).zoom;
      await key('+', 'Equal', 187, 0, '+');
      await waitFor('le zoom au clavier', `document.getElementById('map-view').textContent.endsWith('zoom ${zoomBefore + 1}')`);
      step(`${at} : Tab atteint la carte (anneau de focus visible), Droite la déplace de 80 px, + zoome à ${zoomBefore + 1}`);

      // 8. A real click on the map raises OnMapClick with the place under the pointer.
      const target = await emptySpot(0.3, 0.6);
      check(target !== null, 'Aucun point libre de la carte pour cliquer.');
      const shownView = await shown();
      await session.click(target);
      await waitFor('le clic sur la carte rapporté', "!document.getElementById('map-click').textContent.includes('nothing yet')");
      const clicked = (await shown()).click;
      const box = (await geometry()).box;
      const centre = project(shownView.latitude, shownView.longitude, shownView.zoom);
      // The view shown is rounded to 5 decimals: about 0.1 to 1 pixel at these zooms.
      const wanted = unproject(centre.x - box.width / 2 + (target.x - box.left), centre.y - box.height / 2 + (target.y - box.top), shownView.zoom);
      const tolerance = 360 / (256 * 2 ** shownView.zoom) * 3;
      check(Math.abs(clicked[0] - wanted.latitude) <= tolerance && Math.abs(clicked[1] - wanted.longitude) <= tolerance, `Lieu du clic ${clicked} au lieu de ${wanted.latitude}, ${wanted.longitude}.`);
      step(`${at} : un vrai clic sur la carte lève OnMapClick avec le lieu sous le pointeur (${clicked.join(', ')}, à 3 pixels près)`);

      // 9. Markers: a click, then Enter and Space on the focused button.
      // After the zooms the end may be out of the view: a focused marker outside it is brought to the centre.
      await evaluate("document.querySelector('#map-demo [aria-label=\"End: Gare du Nord\"]').focus()");
      await waitFor("l'arrivée ramenée dans la vue", `(() => { const map = document.getElementById('map-demo').getBoundingClientRect(); const box = document.querySelector('#map-demo [aria-label="End: Gare du Nord"]').getBoundingClientRect(); return box.left >= map.left && box.right <= map.right && box.top >= map.top && box.bottom <= map.bottom; })()`);
      await clickOn('#map-demo [aria-label="End: Gare du Nord"]');
      await waitFor('le repère cliqué', "document.getElementById('map-marker').textContent.includes('End: Gare du Nord')");
      check((await shown()).click.join() === clicked.join(), 'Le clic sur un repère a aussi levé un clic de carte.');
      await evaluate("document.querySelector('#map-demo [aria-label=\"Start: Place Flagey\"]').focus()");
      await key('Enter', 'Enter', 13, 0, '\r');
      await waitFor('Entrée sur le repère de départ', "document.getElementById('map-marker').textContent.includes('Start: Place Flagey')");
      await evaluate("document.querySelector('#map-demo [aria-label=\"Stop: Gare Centrale\"]').focus()");
      await key(' ', 'Space', 32, 0, ' ');
      await waitFor('Espace sur l\'arrêt', "document.getElementById('map-marker').textContent.includes('Stop: Gare Centrale')");
      step(`${at} : un repère cliqué, Entrée et Espace sur un repère focalisé lèvent OnMarkerClick (Arrivée, Départ, Arrêt), sans clic de carte`);

      // 9b. A finger drags the map like the mouse; a touch the browser cancels keeps the map where the finger took it,
      // without a click; a double click zooms in around the pointer, Shift and a double click zoom out.
      const viewText = () => evaluate("document.getElementById('map-view').textContent");
      const fingerFrom = await emptySpot(0.5, 0.5);
      check(fingerFrom !== null, 'Aucun point libre de la carte pour le doigt.');
      const touchStart = marker(await geometry(), 'Start: Place Flagey');
      const touchShift = { x: width > 400 ? 90 : 40, y: 40 };
      const fingerAt = index => ({ x: fingerFrom.x + touchShift.x * index / 6, y: fingerFrom.y + touchShift.y * index / 6, id: 1 });
      let previousView = await viewText();
      await send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [fingerAt(0)] });
      for (let index = 1; index <= 6; index++) {
        await send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [fingerAt(index)] });
        await pause(20);
      }
      await send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
      await waitFor('la vue déplacée au doigt', `document.getElementById('map-view').textContent !== ${JSON.stringify(previousView)}`);
      const touched = marker(await geometry(), 'Start: Place Flagey');
      check(Math.abs(touched.x - touchStart.x - touchShift.x) <= 2 && Math.abs(touched.y - touchStart.y - touchShift.y) <= 2, `Le repère n'a pas suivi le doigt : ${JSON.stringify({ touchStart, touched, touchShift })}.`);
      const clickBefore = (await shown()).click.join();
      previousView = await viewText();
      await send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [fingerAt(0)] });
      for (let index = 1; index <= 3; index++) {
        await send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [fingerAt(-index)] });
        await pause(20);
      }
      await send('Input.dispatchTouchEvent', { type: 'touchCancel', touchPoints: [] });
      await waitFor('la vue gardée après un toucher annulé', `document.getElementById('map-view').textContent !== ${JSON.stringify(previousView)}`);
      check((await shown()).click.join() === clickBefore, 'Un toucher annulé a levé un clic sur la carte.');
      check(!(await evaluate("document.getElementById('map-demo').classList.contains('omni-map--dragging')")), 'La carte reste en glisser après un toucher annulé.');
      const zoomed = (await shown()).zoom;
      const doubleAt = await emptySpot(0.6, 0.4);
      check(doubleAt !== null, 'Aucun point libre de la carte pour le double clic.');
      const under = await evaluate(`(() => { const box = document.getElementById('map-demo').getBoundingClientRect(); return JSON.stringify({ x: ${doubleAt.x} - box.left, y: ${doubleAt.y} - box.top }); })()`).then(JSON.parse);
      const viewNow = await shown();
      const box2 = (await geometry()).box;
      const centreNow = project(viewNow.latitude, viewNow.longitude, viewNow.zoom);
      const placeUnder = unproject(centreNow.x - box2.width / 2 + under.x, centreNow.y - box2.height / 2 + under.y, viewNow.zoom);
      await mouse('mouseMoved', doubleAt, { button: 'none' });
      await mouse('mousePressed', doubleAt, { clickCount: 1 });
      await mouse('mouseReleased', doubleAt, { clickCount: 1 });
      await mouse('mousePressed', doubleAt, { clickCount: 2 });
      await mouse('mouseReleased', doubleAt, { clickCount: 2 });
      await waitFor('le zoom au double clic', `document.getElementById('map-view').textContent.endsWith('zoom ${zoomed + 1}')`);
      const viewAfter = await shown();
      const centreAfter = project(viewAfter.latitude, viewAfter.longitude, viewAfter.zoom);
      const placeAfter = unproject(centreAfter.x - box2.width / 2 + under.x, centreAfter.y - box2.height / 2 + under.y, viewAfter.zoom);
      const toleranceDouble = 360 / (256 * 2 ** viewAfter.zoom) * 3;
      check(Math.abs(placeAfter.latitude - placeUnder.latitude) <= toleranceDouble && Math.abs(placeAfter.longitude - placeUnder.longitude) <= toleranceDouble, `Le double clic n'a pas zoomé autour du pointeur : ${JSON.stringify({ placeUnder, placeAfter })}.`);
      await mouse('mousePressed', doubleAt, { clickCount: 1, modifiers: 8 });
      await mouse('mouseReleased', doubleAt, { clickCount: 1, modifiers: 8 });
      await mouse('mousePressed', doubleAt, { clickCount: 2, modifiers: 8 });
      await mouse('mouseReleased', doubleAt, { clickCount: 2, modifiers: 8 });
      await waitFor('le dézoom à Maj et double clic', `document.getElementById('map-view').textContent.endsWith('zoom ${zoomed}')`);
      step(`${at} : un doigt glisse la carte (le repère suit à ${touchShift.x}, ${touchShift.y} px près de 2), un toucher annulé la laisse où le doigt l'a menée sans clic, le double clic zoome de ${zoomed} à ${zoomed + 1} autour du pointeur, Maj et double clic dézoome`);

      // 10. The package tooltip reads the text of a line under the pointer.
      if (first) {
        await evaluate("document.getElementById('map-demo').scrollIntoView({ block: 'center' })");
        await pause(200);
        const linePoint = await evaluate(`(() => {
          const path = document.querySelectorAll('#map-demo g[data-omni-map-line] .omni-map__stroke')[2];
          const svg = path.ownerSVGElement.getBoundingClientRect();
          const length = path.getTotalLength();
          for (let part = 0.2; part < 0.9; part += 0.05) {
            const point = path.getPointAtLength(length * part);
            const x = svg.left + point.x; const y = svg.top + point.y;
            const hit = document.elementFromPoint(x, y);
            if (hit && hit.closest('g[data-omni-map-line]') === path.parentElement) return JSON.stringify({ x, y });
          }
          return 'null';
        })()`).then(JSON.parse);
        check(linePoint !== null, 'Aucun point du tronçon de forte pluie sous le pointeur.');
        await mouse('mouseMoved', { x: linePoint.x + 1, y: linePoint.y }, { button: 'none' });
        await mouse('mouseMoved', linePoint, { button: 'none' });
        await waitFor("l'infobulle du tronçon", "document.querySelector('.omni-title-tooltip--visible')?.textContent.includes('08:10 to 08:15, heavy rain') === true", 5_000);
        await mouse('mouseMoved', { x: 2, y: 2 }, { button: 'none' });
        step(`${at} : l'infobulle du paquet lit le tronçon sous le pointeur (« 08:10 to 08:15, heavy rain »)`);
        first = false;
      }

      // 11. Dark mode: the controls, the attribution and the markers follow the theme; the lines stay readable.
      const look = () => evaluate(`(() => {
        const map = document.getElementById('map-demo');
        const style = element => getComputedStyle(element);
        const zoom = map.querySelector('.omni-map__controls button');
        const end = map.querySelector('.omni-map__marker--end');
        const lines = [...map.querySelectorAll('g[data-omni-map-line]')].map(group => ({ stroke: style(group.querySelector('.omni-map__stroke')).stroke, casing: style(group.querySelector('.omni-map__casing')).stroke }));
        return JSON.stringify({ theme: document.documentElement.getAttribute('data-omni-theme'), zoom: [style(zoom).color, style(zoom).backgroundColor], end: [style(end).color, style(end).backgroundColor], attribution: [style(map.querySelector('.omni-map__attribution')).color, style(map.querySelector('.omni-map__attribution')).backgroundColor], lines, tiles: map.querySelectorAll('.omni-map__tile--loaded').length });
      })()`).then(JSON.parse);
      const light = await look();
      await pickMode(2);
      try {
        await waitFor('le mode sombre', "document.documentElement.getAttribute('data-omni-theme') === 'dark'");
        await pause(300);
        const dark = await look();
        check(dark.zoom.join() !== light.zoom.join() && dark.attribution.join() !== light.attribution.join(), `Les commandes ou l'attribution ne suivent pas le thème : ${JSON.stringify({ light, dark })}.`);
        for (const mode of [light, dark]) {
          check(contrast(rgb(mode.zoom[0]), rgb(mode.zoom[1])) >= 4.5, `Bouton de zoom peu lisible en ${mode.theme} : ${mode.zoom}.`);
          check(legible(mode.attribution[0], mode.attribution[1]) >= 4.5, `Attribution peu lisible en ${mode.theme} : ${mode.attribution}.`);
          check(contrast(rgb(mode.end[0]), rgb(mode.end[1])) >= 3, `Repère d'arrivée peu lisible en ${mode.theme} : ${mode.end}.`);
          for (const line of mode.lines) check(contrast(rgb(line.stroke), rgb(line.casing)) >= 3, `Tronçon peu lisible sur sa bordure en ${mode.theme} : ${JSON.stringify(line)}.`);
        }
        check(dark.tiles > 0, 'Les tuiles ont disparu en mode sombre.');
        const lowest = Math.min(...[light, dark].flatMap(mode => mode.lines.map(line => contrast(rgb(line.stroke), rgb(line.casing)))));
        step(`${at} : en sombre, commandes, attribution et repères suivent le thème (contraste 4,5 et plus pour les textes), tronçons lisibles sur leur bordure dans les deux modes (contraste ${lowest.toFixed(2)} au moins), tuiles inchangées`);
      } finally {
        await pickMode(1);
        await waitFor('le mode clair', "document.documentElement.getAttribute('data-omni-theme') !== 'dark'");
      }

      // 12. At phone width, the page does not scroll sideways and the controls stay inside the map.
      const controls = await evaluate("JSON.stringify((() => { const box = document.querySelector('#map-demo .omni-map__controls').getBoundingClientRect(); return { left: box.left, top: box.top, right: box.right, bottom: box.bottom }; })())").then(JSON.parse);
      check(inside(controls, (await geometry()).box), `Les boutons de zoom sortent de la carte : ${JSON.stringify(controls)}.`);
      check(await evaluate('document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1'), 'La page défile en largeur.');
      check((await evaluate('window.__omniCsp')).length === 0, `Violations CSP : ${(await evaluate('window.__omniCsp')).join(' | ')}`);
      step(`${at} : boutons de zoom dans la carte, page sans défilement latéral, aucune violation CSP`);
    }
  } finally {
    stop();
    await send('Fetch.disable').catch(() => { });
  }
}
