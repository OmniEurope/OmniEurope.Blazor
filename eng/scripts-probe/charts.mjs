// The charts under the pointer: the nearest point of a line and its package tooltip, the bar that stands
// out, and a label inside a bar that reads on that bar in the light and dark themes.

export async function chartSteps(session, results) {
  const { evaluate, waitFor, pause, check, hover, mouse } = session;
  const step = label => {
    if (session.consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${session.consoleErrors.join(' | ')}`);
    results.push(label);
  };
  const away = () => mouse('mouseMoved', { x: 5, y: 5 }, { button: 'none' });

  // 1. A line: at a third and two thirds of a segment, the pointer is on the line, far from any dot, and the
  // nearest point lights up; on the dot itself, over the marker, too.
  await session.visit('/components/line-charts', '#demo-chart-point-labels .omni-chart__point-zone');
  const place = (first, second, ratio) => evaluate(`(() => {
    const figure = document.getElementById('demo-chart-point-labels');
    figure.scrollIntoView({ block: 'center' });
    const line = figure.querySelector('polyline.omni-chart__line');
    const a = line.points[${first}];
    const b = line.points[${second}];
    const point = new DOMPoint(a.x + (b.x - a.x) * ${ratio}, a.y + (b.y - a.y) * ${ratio}).matrixTransform(line.getScreenCTM());
    return JSON.stringify({ x: point.x, y: point.y });
  })()`).then(JSON.parse);
  const lit = "[...document.querySelectorAll('#demo-chart-point-labels .omni-chart__point-mark')].filter(mark => getComputedStyle(mark).opacity === '1').length";
  const shown = index => `${lit} === 1 && getComputedStyle(document.querySelectorAll('#demo-chart-point-labels .omni-chart__point-mark')[${index}]).opacity === '1'`;
  const tip = "document.querySelector('.omni-title-tooltip.omni-title-tooltip--visible')?.textContent.trim()";
  const cases = [[0, 1, 0.3, 0, 'Alpha · 12 €'], [0, 1, 0.7, 1, 'Borealis · 30 €'], [1, 2, 0.3, 1, 'Borealis · 30 €'], [1, 2, 0.7, 2, 'Cygnus · 18 €'], [2, 3, 1, 3, 'Draco · 26 €']];
  for (const [first, second, ratio, index, text] of cases) {
    await mouse('mouseMoved', await place(first, second, ratio), { button: 'none' });
    await waitFor(`le point ${index} surligné`, shown(index));
    await waitFor(`l'infobulle du point ${index}`, `${tip} === ${JSON.stringify(text)}`);
    // The browser's own box would show the title of the zone or of the chart: both are set aside.
    check(await evaluate(`(() => {
      const svg = document.querySelector('#demo-chart-point-labels svg');
      const zone = document.querySelectorAll('#demo-chart-point-labels .omni-chart__point-zone')[${index}];
      return svg.querySelector(':scope > title') === null && zone.querySelector(':scope > title') === null;
    })()`), `le titre natif est resté en place au point ${index}`);
    await away();
    await waitFor('la bulle fermée hors du graphique', `(${tip}) === undefined && ${lit} === 0`);
  }
  step('point le plus proche surligné, avec le texte de l\'hôte en bulle');

  // 2. A bar under the pointer, over the label written in it: its edge turns, the others step back; with
  // the highlight off, nothing moves.
  await session.visit('/components/column-charts', '#demo-chart-inside-labels .omni-chart__label-on-bar');
  await evaluate("document.getElementById('demo-chart-inside-labels').scrollIntoView({ block: 'center' })");
  const bars = "#demo-chart-inside-labels .omni-chart__bars rect";
  const state = () => evaluate(`JSON.stringify([...document.querySelectorAll('${bars}')].map(bar => ({ hover: bar.matches(':hover'), width: getComputedStyle(bar).strokeWidth, opacity: getComputedStyle(bar).opacity })))`).then(JSON.parse);
  const rest = await state();
  await hover(`${bars}:nth-of-type(1)`);
  await waitFor('la barre survolée', `document.querySelector('${bars}').matches(':hover')`);
  const over = await state();
  check(over[0].width === '2px' && over[0].opacity === '1' && over.slice(1).every(bar => bar.opacity === '0.6'), `barre non ressortie : ${JSON.stringify(over)}`);
  check(rest.every(bar => bar.opacity === '1'), `barres atténuées au repos : ${JSON.stringify(rest)}`);
  await away();
  await evaluate("document.getElementById('demo-chart-inside-labels').classList.add('omni-chart--no-highlight')");
  await hover(`${bars}:nth-of-type(1)`);
  await waitFor('la barre survolée sans mise en avant', `document.querySelector('${bars}').matches(':hover')`);
  const still = await state();
  check(still.every(bar => bar.opacity === '1') && still[0].width !== '2px', `barre mise en avant malgré la désactivation : ${JSON.stringify(still)}`);
  await evaluate("document.getElementById('demo-chart-inside-labels').classList.remove('omni-chart--no-highlight')");
  await away();
  step('barre ressortie au survol, immobile une fois désactivée');

  // 3. The labels, measured on what the browser paints: a canvas turns any colour it computes into sRGB.
  const measure = () => evaluate(`(() => {
    const context = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
    const rgb = color => { context.clearRect(0, 0, 1, 1); context.fillStyle = color; context.fillRect(0, 0, 1, 1); return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3); };
    const luminance = values => { const [r, g, b] = values.map(value => { const unit = value / 255; return unit <= 0.04045 ? unit / 12.92 : ((unit + 0.055) / 1.055) ** 2.4; }); return 0.2126 * r + 0.7152 * g + 0.0722 * b; };
    const ratio = (a, b) => { const [high, low] = [luminance(a), luminance(b)].sort((x, y) => y - x); return (high + 0.05) / (low + 0.05); };
    const figure = document.getElementById('demo-chart-inside-labels');
    const rects = [...figure.querySelectorAll('.omni-chart__bars rect')];
    const texts = [...figure.querySelectorAll('.omni-chart__labels text')];
    return JSON.stringify(texts.map((text, index) => {
      const box = text.getBoundingClientRect();
      const bar = rects[index].getBoundingClientRect();
      const onBar = text.classList.contains('omni-chart__label-on-bar');
      return { text: text.textContent, onBar, inside: box.left >= bar.left && box.right <= bar.right, past: box.left >= bar.right, stroke: getComputedStyle(text).stroke, contrast: Number(ratio(rgb(getComputedStyle(text).fill), rgb(getComputedStyle(rects[index]).fill)).toFixed(2)) };
    }));
  })()`).then(JSON.parse);
  const verify = (labels, theme) => {
    check(labels.length === 4 && labels.slice(0, 3).every(label => label.onBar && label.inside && label.stroke === 'none' && label.contrast >= 4.5), `${theme} : étiquette illisible ou hors de sa barre : ${JSON.stringify(labels)}`);
    check(!labels[3].onBar && labels[3].past, `${theme} : l'étiquette trop longue n'est pas après sa barre : ${JSON.stringify(labels[3])}`);
  };
  const light = await measure();
  verify(light, 'clair');
  await evaluate("document.getElementById('demo-chart-inside-labels').setAttribute('data-omni-theme', 'dark')");
  await pause(100);
  const dark = await measure();
  verify(dark, 'sombre');
  await evaluate("document.getElementById('demo-chart-inside-labels').removeAttribute('data-omni-theme')");
  step(`étiquettes lisibles sur leur barre (contraste mini ${Math.min(...[...light, ...dark].filter(label => label.onBar).map(label => label.contrast))})`);
}
