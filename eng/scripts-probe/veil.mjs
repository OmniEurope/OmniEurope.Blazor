// The page veil (OmniPageVeil, recette R1-18), driven in a real Chromium on the Progress page of the showcase:
// it appears after about 100 ms of waiting and not before, a wait shorter than that shows nothing, once
// shown it is opaque and of one constant colour (a different one in dark mode) with no opacity at any frame,
// and the logo is centred whether it is given as markup, as an image address, animated by the application,
// or absent. A recorder in the page samples every animation frame, because a timing read after the fact
// proves nothing.

export async function veilSteps(session, results) {
  const { evaluate, waitFor, check, clickOn, key, pause } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  // Samples every frame while the veil exists: when it is inserted (MutationObserver), its visibility,
  // opacity and colour at each frame, and its geometry and logo whenever it is visible. The showcase main area
  // scrolls (OmniMain Scrollable), so the veil is drawn in its non-scrolling frame and must cover that frame
  // exactly: under the header and beside the sidebar, which stay outside it (recette R1-29).
  const recorder = `(() => {
    const probe = window.__veilProbe = { insertedAt: null, removedAt: null, frames: [], geometry: null, done: false };
    new MutationObserver(records => {
      for (const record of records) {
        for (const node of record.addedNodes) if (node.nodeType === 1 && (node.matches('.omni-page-veil') || node.querySelector('.omni-page-veil'))) probe.insertedAt ??= performance.now();
      }
    }).observe(document.body, { childList: true, subtree: true });
    const started = performance.now();
    const tick = () => {
      const now = performance.now();
      const veil = document.querySelector('.omni-page-veil');
      if (veil) {
        const style = getComputedStyle(veil);
        probe.frames.push({ t: now, visibility: style.visibility, opacity: style.opacity, color: style.backgroundColor });
        if (style.visibility === 'visible') {
          const logo = veil.querySelector('.omni-page-veil__logo');
          const mark = logo?.firstElementChild ?? null;
          const box = veil.getBoundingClientRect();
          const logoBox = logo?.getBoundingClientRect() ?? null;
          probe.geometry = {
            position: style.position, zIndex: style.zIndex, role: veil.getAttribute('role'), label: veil.querySelector('.omni-visually-hidden')?.textContent ?? null,
            veil: [box.left, box.top, box.width, box.height], frame: (() => { const frame = veil.parentElement.getBoundingClientRect(); return [frame.left, frame.top, frame.width, frame.height]; })(), inFrame: veil.parentElement.matches('.omni-main-scroll'),
            header: document.querySelector('.omni-header')?.getBoundingClientRect().bottom ?? null, sidebar: (() => { const side = document.querySelector('.omni-sidebar--open'); return side ? side.getBoundingClientRect().right : null; })(),
            logo: logoBox ? [logoBox.left + logoBox.width / 2, logoBox.top + logoBox.height / 2, logoBox.width, logoBox.height] : null,
            mark: mark ? { tag: mark.tagName.toLowerCase(), loaded: mark.tagName === 'IMG' ? mark.complete && mark.naturalWidth > 0 : true, animated: mark.querySelector('animate') !== null } : null,
            pointerHit: document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2)?.closest('.omni-page-veil') !== null,
            headerHit: (() => { const header = document.querySelector('.omni-header')?.getBoundingClientRect(); return header ? document.elementFromPoint(header.left + header.width / 2, header.top + header.height / 2)?.closest('.omni-page-veil') === null : false; })(),
            surface: (() => { const probeElement = document.createElement('div'); probeElement.style.backgroundColor = 'var(--omni-color-surface)'; veil.parentElement.appendChild(probeElement); const colour = getComputedStyle(probeElement).backgroundColor; probeElement.remove(); return colour; })()
          };
        }
      } else if (probe.insertedAt !== null && probe.removedAt === null) {
        probe.removedAt = now;
      }
      if ((probe.removedAt !== null && now - probe.removedAt > 100) || now - started > 9000) { probe.done = true; return; }
      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  })()`;

  // Clicks the button of one wait and returns what the recorder saw of it.
  const wait = async button => {
    await evaluate(recorder);
    await clickOn(`#${button}`);
    await waitFor(`la fin de l'attente de ${button}`, 'window.__veilProbe.done === true', 12_000);
    const probe = await evaluate('JSON.parse(JSON.stringify(window.__veilProbe))');
    check(probe.insertedAt !== null, `Le voile de ${button} n'a jamais été inséré.`);
    return probe;
  };
  const distinct = (frames, property) => [...new Set(frames.map(frame => frame[property]))];

  // What a wait that outlasts the delay must show, whatever the logo and the mode.
  const checkShown = (label, probe, expectedSurface) => {
    const hiddenFirst = probe.frames[0]?.visibility === 'hidden';
    const visible = probe.frames.filter(frame => frame.visibility === 'visible');
    check(visible.length > 0, `${label} : le voile n'est jamais devenu visible (${JSON.stringify(distinct(probe.frames, 'visibility'))}).`);
    const delay = visible[0].t - probe.insertedAt;
    check(hiddenFirst, `${label} : le voile était déjà visible à sa première image.`);
    check(delay >= 95 && delay <= 400, `${label} : le voile est apparu après ${delay.toFixed(0)} ms, il doit apparaître après environ 100 ms.`);
    const before = probe.frames.filter(frame => frame.t - probe.insertedAt < 90);
    check(before.every(frame => frame.visibility === 'hidden'), `${label} : le voile était visible avant 90 ms.`);
    check(distinct(visible, 'opacity').join() === '1', `${label} : l'opacité varie ou n'est pas 1 : ${distinct(visible, 'opacity').join(' | ')}.`);
    check(distinct(probe.frames, 'opacity').join() === '1', `${label} : l'opacité varie pendant le délai : ${distinct(probe.frames, 'opacity').join(' | ')}.`);
    check(distinct(visible, 'color').length === 1, `${label} : la couleur varie : ${distinct(visible, 'color').join(' | ')}.`);
    const geometry = probe.geometry;
    const near = (a, b) => Math.abs(a - b) <= 1;
    check(geometry.inFrame && geometry.veil.every((value, index) => near(value, geometry.frame[index])), `${label} : le voile ne couvre pas exactement le cadre de la zone principale : ${JSON.stringify(geometry)}.`);
    check(geometry.header !== null && geometry.veil[1] >= geometry.header - 1 && geometry.headerHit, `${label} : le voile couvre la barre du haut : ${JSON.stringify(geometry)}.`);
    check(geometry.sidebar === null || geometry.veil[0] >= geometry.sidebar - 1, `${label} : le voile couvre le menu latéral : ${JSON.stringify(geometry)}.`);
    check(geometry.position === 'absolute' && geometry.role === 'status' && geometry.pointerHit, `${label} : voile non posé sur son cadre, sans rôle status ou qui laisse passer le pointeur : ${JSON.stringify(geometry)}.`);
    check(visible[0].color === geometry.surface && (!expectedSurface || visible[0].color === expectedSurface), `${label} : la couleur ${visible[0].color} n'est pas la surface du thème ${geometry.surface}.`);
    return { delay, color: visible[0].color, geometry, frames: probe.frames.length };
  };
  const checkCentred = (label, geometry, tag) => {
    check(geometry.logo !== null && geometry.mark?.tag === tag, `${label} : logo attendu (${tag}), obtenu ${JSON.stringify(geometry.mark)}.`);
    const [centreX, centreY] = geometry.logo;
    const [left, top, width, height] = geometry.veil;
    check(Math.abs(centreX - (left + width / 2)) <= 1 && Math.abs(centreY - (top + height / 2)) <= 1, `${label} : logo non centré (${centreX}, ${centreY}) dans le voile ${geometry.veil.join(' x ')}.`);
    check(geometry.mark.loaded, `${label} : l'image du logo n'est pas chargée.`);
  };

  await session.visit('/components/progress', '#page-veil-markup');

  // 1. Light mode, logo as markup.
  const markup = checkShown('logo en balisage', await wait('page-veil-markup'));
  checkCentred('logo en balisage', markup.geometry, 'svg');
  const light = markup.color;
  step(`voile de page : apparu après ${markup.delay.toFixed(0)} ms (jamais avant 90 ms), opaque, couleur constante ${light}, ${markup.frames} images relevées, logo en balisage centré`);

  // 2. The logo as an image address, centred and loaded.
  const address = checkShown('logo par adresse', await wait('page-veil-address'), light);
  checkCentred('logo par adresse', address.geometry, 'img');
  step(`voile de page : logo par adresse d'image chargé et centré, apparu après ${address.delay.toFixed(0)} ms`);

  // 3. A logo the application animates itself: the veil plays it as it is and adds nothing of its own.
  const animated = checkShown('logo animé', await wait('page-veil-animated'), light);
  checkCentred('logo animé', animated.geometry, 'svg');
  check(animated.geometry.mark.animated, 'Le logo animé de l\'application a perdu son animation.');
  step(`voile de page : logo animé par l'application centré, apparu après ${animated.delay.toFixed(0)} ms`);

  // 4. No logo: an opaque page and nothing else.
  const none = checkShown('sans logo', await wait('page-veil-none'), light);
  check(none.geometry.logo === null, 'Sans logo, le voile en dessine un.');
  step('voile de page : sans logo, une page opaque');

  // 5. A wait shorter than the delay shows nothing: the veil is inserted and removed without one visible frame.
  const short = await wait('page-veil-short');
  check(short.frames.length > 0, 'Attente courte : aucune image relevée.');
  check(short.frames.every(frame => frame.visibility === 'hidden'), `Attente courte : le voile a été visible (${JSON.stringify(distinct(short.frames, 'visibility'))}).`);
  check(short.geometry === null, 'Attente courte : une géométrie visible a été relevée.');
  step(`voile de page : une attente de 50 ms (${short.frames.length} images, ${(short.removedAt - short.insertedAt).toFixed(0)} ms d'existence) ne montre rien`);

  // 6. Dark mode: another colour, still one constant, still opaque. The mode is picked in the application menu.
  const pickMode = async position => {
    await clickOn('.omni-header .omni-app-menu__trigger');
    await waitFor('le menu d\'application', 'document.querySelector(\'.omni-app-menu__mode\') !== null');
    await clickOn(`.omni-app-menu__mode .omni-appearance-window__mode:nth-child(${position})`);
    await key('Escape', 'Escape', 27);
    await waitFor('le menu d\'application refermé', 'document.querySelector(\'.omni-app-menu__mode\') === null');
  };
  await pickMode(2);
  try {
    await waitFor('le mode sombre', 'document.documentElement.getAttribute(\'data-omni-theme\') === \'dark\'');
    await pause(300);
    const dark = checkShown('mode sombre', await wait('page-veil-markup'));
    checkCentred('mode sombre', dark.geometry, 'svg');
    check(dark.color !== light, `Le voile garde la même couleur en clair et en sombre (${light}).`);
    step(`voile de page : mode sombre ${dark.color} (clair ${light}), opaque et constant, apparu après ${dark.delay.toFixed(0)} ms`);
  } finally {
    await pickMode(1);
  }
}
