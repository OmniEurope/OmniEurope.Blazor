// The document viewer page (/components/document-viewer, OmniDocumentViewer): the two listeners omni-document-viewer.js
// puts on its viewport, driven in a real Chromium. A reader scrolling with the mouse wheel moves the page indicator of the
// toolbar to the page in view (the scroll listener reports it to .NET), and a picture finishing its load after the script
// attached sizes its page again (the load listener, caught on the way down because a load does not bubble).

export async function documentViewerSteps(session, results) {
  const { evaluate, waitFor, check, send, centerOf, pause } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };

  const viewport = '#document-viewer';
  const indicator = () => evaluate("document.querySelector('.omni-document-viewer__indicator')?.textContent ?? ''");
  // The numbers of the indicator, whatever the language of its wording: the page in view, then the count of pages.
  const pageInView = async () => Number.parseInt(/\d+/.exec(await indicator())?.[0] ?? '0', 10);
  const scrollState = () => evaluate(`(() => { const box = document.querySelector(${JSON.stringify(viewport)}); return JSON.stringify({ top: box.scrollTop, max: box.scrollHeight - box.clientHeight }); })()`).then(JSON.parse);

  // The wheel turned over the viewport, the way a reader scrolls: until the viewport reaches the end it was sent towards.
  const wheelTo = async direction => {
    const point = await centerOf(viewport);
    for (let turns = 0; turns < 40; turns++) {
      const { top, max } = await scrollState();
      if (direction > 0 ? top >= max - 1 : top <= 0) return;
      await send('Input.dispatchMouseEvent', { type: 'mouseWheel', x: point.x, y: point.y, deltaX: 0, deltaY: direction * 400 });
      await pause(60);
    }
    check(false, `La molette n'a pas conduit le visualiseur à sa ${direction > 0 ? 'fin' : 'tête'} : ${JSON.stringify(await scrollState())}.`);
  };

  await session.visit('/components/document-viewer', `${viewport} .omni-document-viewer__page`);
  await waitFor('les deux pages dessinées et la page 1 indiquée', `document.querySelectorAll(${JSON.stringify(viewport + ' .omni-document-viewer__page')}).length === 2 && document.querySelector('.omni-document-viewer__indicator') !== null`);
  check((await scrollState()).max > 0, 'Le visualiseur ne défile pas : rien à lire au-delà de la première page.');
  check((await pageInView()) === 1, `Le visualiseur ne commence pas à la page 1 : « ${await indicator()} ».`);

  // 1. Scroll: the wheel down to the last page moves the indicator to page 2, the wheel back up brings page 1 back, and
  // the zoom the toolbar shows is the one the width fit gave, untouched by the scroll.
  const zoomBefore = await evaluate("document.querySelector('.omni-document-viewer__zoom').textContent");
  await wheelTo(1);
  await waitFor('la page 2 indiquée après la molette vers le bas', "/\\d+/.exec(document.querySelector('.omni-document-viewer__indicator').textContent)?.[0] === '2'");
  const last = await indicator();
  await wheelTo(-1);
  await waitFor('la page 1 indiquée après la molette vers le haut', "/\\d+/.exec(document.querySelector('.omni-document-viewer__indicator').textContent)?.[0] === '1'");
  check((await evaluate("document.querySelector('.omni-document-viewer__zoom').textContent")) === zoomBefore, `Le zoom a changé avec le défilement : « ${zoomBefore} » puis « ${await evaluate("document.querySelector('.omni-document-viewer__zoom').textContent")} ».`);
  step(`défilement à la molette : page 2 indiquée au bas du document (« ${last} »), page 1 au retour en haut, zoom « ${zoomBefore} » inchangé`);

  // 2. Load: a picture that finishes loading after the script attached (the cover is a data address, which CDP cannot
  // hold at the request stage, so the picture is loaded again, which raises a real load event on the same image). The
  // sizes the script wrote on the page are taken away first: the load puts them back.
  const cover = `${viewport} .omni-document-viewer__page--image`;
  await evaluate(`(() => { const page = document.querySelector(${JSON.stringify(cover)}); page.style.removeProperty('--omni-document-page-width'); page.style.removeProperty('--omni-document-page-height'); })()`);
  check(await evaluate(`(() => { const style = document.querySelector(${JSON.stringify(cover)}).style; return style.getPropertyValue('--omni-document-page-width') === '' && style.getPropertyValue('--omni-document-page-height') === ''; })()`), 'Les dimensions de la page de couverture restent posées.');
  await evaluate(`(() => { const image = document.querySelector(${JSON.stringify(cover + ' img')}); const address = image.getAttribute('src'); image.removeAttribute('src'); image.setAttribute('src', address); })()`);
  await waitFor('la page de couverture dimensionnée après le chargement de son image', `(() => { const page = document.querySelector(${JSON.stringify(cover)}); const style = page.style; return style.getPropertyValue('--omni-document-page-width') !== '' && style.getPropertyValue('--omni-document-page-height') !== '' && page.querySelector('img').complete; })()`);
  const sized = await evaluate(`(() => { const style = document.querySelector(${JSON.stringify(cover)}).style; return style.getPropertyValue('--omni-document-page-width') + ' x ' + style.getPropertyValue('--omni-document-page-height'); })()`);
  step(`image chargée après l'attache : page de couverture redimensionnée (${sized})`);
}
