// The scroll chevrons of a tab strip and of the sidebar menu (focus/tabs.js, configureOverflow), recette
// R1-22: they lie over the edge of the scrolling box and take no room, so the content never moves when one
// comes or goes, and the end of the list is told with a margin of a few pixels, lit later than it goes out,
// so a position at the stop cannot make a chevron flicker. Driven in a real Chromium, a position at a time,
// with the geometry read back after each: the size of the box and the place of the content must not
// depend on a chevron, whatever the offset.

export async function chevronSteps(session, results) {
  const { evaluate, waitFor, check, send, clickOn, pause } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  // Drives one scrolling box. `selectors` names the strip (that holds the chevrons), the viewport that
  // scrolls and the two chevrons; `upright` is a vertical box. Returns what it measured.
  const drive = async (label, selectors, upright) => {
    const probe = `(async () => {
      const { strip, viewport, start, end } = ${JSON.stringify(selectors)};
      const box = document.querySelector(strip);
      const view = box.querySelector(viewport);
      const first = box.querySelector(start);
      const last = box.querySelector(end);
      const frames = async count => { for (let index = 0; index < count; index++) await new Promise(resolve => requestAnimationFrame(resolve)); };
      const size = () => ${upright ? 'view.clientHeight' : 'view.clientWidth'};
      const maximum = () => ${upright ? 'view.scrollHeight - view.clientHeight' : 'view.scrollWidth - view.clientWidth'};
      const place = () => { const content = [...view.querySelectorAll('*')].find(element => element.getClientRects().length > 0).getBoundingClientRect(); const frame = view.getBoundingClientRect(); return Math.round((${upright ? 'content.top - frame.top' : 'content.left - frame.left'}) + (${upright ? 'view.scrollTop' : 'view.scrollLeft'})); };
      const scrollTo = async offset => { view.scrollTo({ ${upright ? 'top' : 'left'}: offset, behavior: 'instant' }); await frames(3); };
      const lit = () => ({ start: !first.hidden, end: !last.hidden });
      const length = maximum();
      // The chevrons have no say in the geometry: the size of the viewport and the place of the content
      // at rest are the same at every offset, chevron or not. Recorded while walking the whole box.
      const sizes = new Set();
      const places = new Set();
      const flips = [];
      let previous = null;
      for (let offset = 0; offset <= length; offset += 1) {
        await scrollTo(offset);
        sizes.add(size());
        places.add(place());
        const now = JSON.stringify(lit());
        if (now !== previous) { flips.push({ offset, lit: now }); previous = now; }
      }
      // Settled at the stop, the chevrons stop changing: no toggle during 20 frames.
      await scrollTo(length);
      let toggles = 0;
      const watcher = new MutationObserver(() => { toggles++; });
      watcher.observe(first, { attributes: true, attributeFilter: ['hidden'] });
      watcher.observe(last, { attributes: true, attributeFilter: ['hidden'] });
      await frames(20);
      const settled = toggles;
      // Hysteresis: the end chevron is lit when more than 4 px remain hidden after it and goes out when
      // at most 2 do; from the stop it stays out 3 px short of the end, comes at 8, and stays at 3.
      await scrollTo(length);
      const atStop = lit().end;
      await scrollTo(length - 3);
      const nearStop = lit().end;
      await scrollTo(length - 8);
      const wellShort = lit().end;
      await scrollTo(length - 3);
      const backNear = lit().end;
      await scrollTo(length - 1);
      const backAtStop = lit().end;
      watcher.disconnect();
      const over = (chevron, edge) => { const a = chevron.getBoundingClientRect(); const b = view.getBoundingClientRect(); return ${upright} ? (edge === 'start' ? a.top >= b.top - 1 && a.bottom <= b.bottom + 1 : a.top >= b.top - 1 && a.bottom <= b.bottom + 1) : a.left >= b.left - 6 && a.right <= b.right + 6; };
      await scrollTo(Math.floor(length / 2));
      const position = [getComputedStyle(first).position, getComputedStyle(last).position];
      const inside = [over(first, 'start'), over(last, 'end')];
      return { length, sizes: [...sizes], places: [...places], flips, settled, atStop, nearStop, wellShort, backNear, backAtStop, position, inside, middle: lit() };
    })()`;
    const seen = await evaluate(probe);
    check(seen.length > 20, `${label} : la boîte ne défile que de ${seen.length} px, la sonde ne prouverait rien.`);
    check(seen.sizes.length === 1, `${label} : la taille de la fenêtre de défilement change avec un chevron (${seen.sizes.join(', ')} px).`);
    check(seen.places.length === 1, `${label} : le contenu bouge quand un chevron vient ou part (${seen.places.join(', ')}).`);
    check(seen.position.every(value => value === 'absolute'), `${label} : un chevron est dans le flux (${seen.position.join(', ')}).`);
    check(seen.inside.every(Boolean), `${label} : un chevron sort de la boîte qu'il recouvre.`);
    check(seen.flips.length === 3 && JSON.parse(seen.flips[0].lit).start === false && JSON.parse(seen.flips[0].lit).end === true
      && JSON.parse(seen.flips[1].lit).start === true && JSON.parse(seen.flips[1].lit).end === true
      && JSON.parse(seen.flips[2].lit).start === true && JSON.parse(seen.flips[2].lit).end === false,
      `${label} : les chevrons ne suivent pas début seul, deux, fin seule : ${JSON.stringify(seen.flips)}.`);
    check(seen.settled === 0, `${label} : un chevron change encore après l'arrêt (${seen.settled} bascule(s)).`);
    check(seen.atStop === false && seen.nearStop === false && seen.wellShort === true && seen.backNear === true && seen.backAtStop === false,
      `${label} : l'hystérésis de la butée n'est pas respectée : ${JSON.stringify({ atStop: seen.atStop, nearStop: seen.nearStop, wellShort: seen.wellShort, backNear: seen.backNear, backAtStop: seen.backAtStop })}.`);
    return seen;
  };

  // 1. A tab strip too narrow for its tabs, on the navigation page.
  await send('Emulation.setDeviceMetricsOverride', { width: 250, height: 800, deviceScaleFactor: 1, mobile: false });
  await session.visit('/composants/navigation', '.omni-tabs__strip [role="tab"]');
  await waitFor('une bande d\'onglets qui déborde', "(() => { const view = document.querySelector('.omni-tabs__viewport'); return view.scrollWidth - view.clientWidth > 20; })()");
  await drive('onglets', { strip: '.omni-tabs__strip', viewport: '.omni-tabs__viewport', start: '.omni-tabs__scroll--start', end: '.omni-tabs__scroll--end' }, false);
  step('onglets : chevrons posés sur la bande, ni taille ni contenu qui bougent, butée à marge');

  // 2. The keyboard brings a focused tab clear of the chevron that now covers the edge.
  await evaluate("document.querySelector('.omni-tabs__viewport').scrollTo({ left: 0, behavior: 'instant' })");
  await pause(150);
  const lastTab = "[...document.querySelectorAll('.omni-tabs__strip [role=\"tab\"]')].at(-1)";
  await evaluate(`${lastTab}.focus()`);
  await send('Input.dispatchKeyEvent', { type: 'rawKeyDown', key: 'Shift', code: 'ShiftLeft', windowsVirtualKeyCode: 16 });
  await send('Input.dispatchKeyEvent', { type: 'keyUp', key: 'Shift', code: 'ShiftLeft', windowsVirtualKeyCode: 16 });
  await send('Input.dispatchKeyEvent', { type: 'rawKeyDown', key: 'Tab', code: 'Tab', windowsVirtualKeyCode: 9 });
  await send('Input.dispatchKeyEvent', { type: 'keyUp', key: 'Tab', code: 'Tab', windowsVirtualKeyCode: 9 });
  await evaluate(`${lastTab}.focus()`);
  await pause(300);
  const covered = await evaluate(`(() => {
    const tab = ${lastTab}.getBoundingClientRect();
    const hit = [...document.querySelectorAll('.omni-tabs__strip .omni-tabs__scroll')].filter(chevron => !chevron.hidden).map(chevron => chevron.getBoundingClientRect()).filter(box => box.left < tab.right && box.right > tab.left && box.top < tab.bottom && box.bottom > tab.top);
    return hit.length;
  })()`);
  check(covered === 0, 'Le dernier onglet, focalisé, reste sous un chevron.');
  step('onglet focalisé amené hors de la zone d\'un chevron');

  // 3. The sidebar menu, shortened until it scrolls, on the shell page.
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 800, deviceScaleFactor: 1, mobile: false });
  await session.visit('/composants/coquille', '#shell-demo-sidebar');
  await evaluate(`(() => { const toggle = document.querySelector('[aria-controls="shell-demo-sidebar"]'); if (!document.querySelector('#shell-demo-sidebar').classList.contains('omni-sidebar--open')) toggle.click(); })()`);
  await waitFor('le menu de la coquille ouvert', "document.querySelector('#shell-demo-sidebar').classList.contains('omni-sidebar--open')");
  await evaluate("(() => { const aside = document.querySelector('#shell-demo-sidebar'); aside.style.height = '110px'; aside.style.maxHeight = '110px'; })()");
  await waitFor('un menu qui déborde', "(() => { const view = document.querySelector('#shell-demo-sidebar .omni-sidebar__viewport'); return view.scrollHeight - view.clientHeight > 20; })()");
  await drive('menu latéral', { strip: '#shell-demo-sidebar .omni-sidebar__scroller', viewport: '.omni-sidebar__viewport', start: '.omni-sidebar__scroll--start', end: '.omni-sidebar__scroll--end' }, true);
  await evaluate("(() => { const aside = document.querySelector('#shell-demo-sidebar'); aside.style.height = ''; aside.style.maxHeight = ''; })()");
  step('menu latéral : chevrons posés sur le menu, ni taille ni contenu qui bougent, butée à marge');

  // 4. A click on a chevron still scrolls by about a page, and the strip stays whole.
  await send('Emulation.setDeviceMetricsOverride', { width: 250, height: 800, deviceScaleFactor: 1, mobile: false });
  await session.visit('/composants/navigation', '.omni-tabs__strip [role="tab"]');
  await waitFor('une bande d\'onglets qui déborde', "(() => { const view = document.querySelector('.omni-tabs__viewport'); return view.scrollWidth - view.clientWidth > 20; })()");
  await waitFor('le chevron de fin', "document.querySelector('.omni-tabs__scroll--end') && !document.querySelector('.omni-tabs__scroll--end').hidden");
  await clickOn('.omni-tabs__scroll--end');
  await waitFor('la bande défilée par son chevron', "document.querySelector('.omni-tabs__viewport').scrollLeft > 10");
  step('chevron des onglets cliqué : la bande défile');
  await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
  await pause(200);
}
