// PLAN-004 lot 10: the contrasts of the published showcase measured in a real Chromium, on every
// combination a visitor can build (every theme x every palette x light and dark; dark alone for a theme
// drawn dark only). The static matrix (ThemeContrastMatrixTests) proves the token pairs; this probe proves what the browser paints once the
// stylesheet has combined them: the colour a text really gets, on the background it really sits on, at
// rest, under a forced hover and under focus.
//
// Every combination is applied through the customizer's own controls (theme, palette and mode pickers),
// then the probe waits for every CSS transition to end before it measures (RET-002 n°52). It walks a
// closed list of elements (targets() below): each must be found, an absent one is a failure, not a skip.
// A colour is resolved the way the browser composes it: the computed colour (which the browser gives
// as `color(srgb r g b / a)` with 0..1 channels once a color-mix is involved), laid over every
// translucent background up the tree down to an opaque one, each element's opacity included, over the
// white canvas. A gradient background (Givre's colour field on the theme scope) counts as every colour
// it passes through, each stop and four points between each pair, and the lowest reading is the one
// judged. A backdrop blur is not modelled, for Givre as for the floating layers of every theme. The ratios are
// those of the static tests: 4.5 for text, 3.0 for a non-text mark, the
// border floor of 1.7 (ThemeContrastMatrixTests.BorderFloor) for a control's border, and the 5.0
// rendering margin where ThemeContrastMatrixTests applies it (theme and palette Essentiel in light, on the
// pairs of RenderingMarginPairs, recognised by their painted colours). The content of a busy control
// is measured under its veil at the veil's peak, read by stepping its animation, not taken from the
// stylesheet. Geometry is checked too: grid cells stay table cells, an icon-only button shows its icon
// whole (RET-002 n°48, n°53), and no page overflows horizontally at 375 px (RET-002 n°28).
//
// The busy veil is the one declared exception (owner decision, 2026-09-18): its black veil
// (#000000 at a peak of at most 0.55, the STD-BUSY rendering) may take the content of a busy control
// below its ratio. Those readings are measured, counted and written to the registry under
// acceptedBusyVeil, and they do not fail the probe. A veil of another colour or a higher peak is not
// covered: it fails as before, so changing the veil reopens the question.
//
// A theme may also declare a contrast waiver (owner decision, 2026-09-28): its style wins over the
// thresholds. It is measured like any other; its shortfalls go to acceptedContrastWaiver, focus state
// included. Only the focus ring stays mandatory, checked by ThemePaletteTests in every theme.
//
// Every failure goes into a JSON registry (artifacts/theme-contrast-registry.json), written even when
// empty, and the probe exits non-zero when it is not empty (RET-002 n°50). Screenshots go to
// artifacts/theme-probe/ for a human look: ratios do not see a glow that vanished or a card that lost
// its edge.
//
// Usage: node Test-ThemeContrastProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
//        [--themes "Essentiel,Néon"] [--palettes "Mono"] [--modes dark] [--no-screenshots]
// The filters exist to replay a failure quickly; a filtered run says so and never counts as the gate.
// The browser (started with --remote-debugging-port) and the static server are the caller's.

import { mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const options = new Map();
for (let index = 2; index < process.argv.length; index++) {
  const name = process.argv[index];
  if (name === '--no-screenshots') { options.set(name, true); continue; }
  options.set(name, process.argv[++index]);
}

const endpoint = options.get('--endpoint');
const siteUrl = options.get('--url');
if (!endpoint || !siteUrl) {
  throw new Error('Usage: node Test-ThemeContrastProbe.mjs --endpoint <cdp url> --url <showcase root url>');
}

const list = name => (options.get(name) ? String(options.get(name)).split(',').map(value => value.trim()).filter(Boolean) : null);
const themeFilter = list('--themes');
const paletteFilter = list('--palettes');
const modeFilter = list('--modes');
const partial = Boolean(themeFilter || paletteFilter || modeFilter);
const screenshots = !options.get('--no-screenshots');

const artifacts = resolve(dirname(fileURLToPath(import.meta.url)), '..', 'artifacts');
const shotDirectory = join(artifacts, 'theme-probe');
const registryPath = join(artifacts, 'theme-contrast-registry.json');

// The pages visited for each combination: the customizer, whose preview stacks most of the closed
// list, and the three gallery pages holding the rest (the Kanban board, the status strip, the list
// options).
const CUSTOMIZER = '/personnalisation';
const EXTRA_PAGES = ['/composants/listes', '/composants/retours', '/composants/selection'];
const NARROW_PAGES = ['/', CUSTOMIZER, ...EXTRA_PAGES];

// Screenshots besides every theme on its own palette in both modes: Halo and Néon, the two themes
// that draw with the accent (halos, glows, tinted borders), each on three palettes not their own;
// Relief on a white page, where its light shadow vanishes; Givre, whose colour field is drawn from the
// palette, on two others.
const FOREIGN_SHOTS = { 'Halo': ['Océan', 'Braise', 'Mono'], 'Néon': ['Essentiel', 'Forêt', 'Or ancien'], 'Relief': ['Essentiel'], 'Givre': ['Océan', 'Mono'] };

// ---------------------------------------------------------------------------------------------------
// The page side: colour arithmetic, the closed list of targets, the measures and the geometry checks.
// It is evaluated in the page once per document (Blazor navigation keeps the document).
// ---------------------------------------------------------------------------------------------------
const pageLibrary = String.raw`
(() => {
  if (window.__omniContrast) return;

  const WHITE = { r: 255, g: 255, b: 255, a: 1 };
  const TRANSPARENT = { r: 0, g: 0, b: 0, a: 0 };

  // Computed colours come as rgb()/rgba() for plain values and as color(srgb r g b / a), channels
  // from 0 to 1, when a color-mix is involved. Anything else is reported, never guessed.
  const parse = value => {
    const text = String(value ?? '').trim().toLowerCase();
    if (text === 'transparent') return { ...TRANSPARENT };
    let match = /^#([0-9a-f]{3}|[0-9a-f]{6})$/.exec(text);
    if (match) {
      const digits = match[1].length === 3 ? [...match[1]].map(digit => digit + digit).join('') : match[1];
      return { r: parseInt(digits.slice(0, 2), 16), g: parseInt(digits.slice(2, 4), 16), b: parseInt(digits.slice(4, 6), 16), a: 1 };
    }
    const alpha = raw => raw === undefined ? 1 : raw.endsWith('%') ? parseFloat(raw) / 100 : parseFloat(raw);
    match = /^rgba?\(\s*([\d.]+)[\s,]+([\d.]+)[\s,]+([\d.]+)(?:\s*[,/]\s*([\d.]+%?))?\s*\)$/.exec(text);
    if (match) return { r: +match[1], g: +match[2], b: +match[3], a: alpha(match[4]) };
    match = /^color\(srgb\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)(?:\s*\/\s*([\d.]+%?))?\s*\)$/.exec(text);
    if (match) return { r: +match[1] * 255, g: +match[2] * 255, b: +match[3] * 255, a: alpha(match[4]) };
    return null;
  };

  // Source-over compositing, straight (non premultiplied) channels.
  const over = (top, bottom) => {
    const a = top.a + bottom.a * (1 - top.a);
    if (a <= 0) return { ...TRANSPARENT };
    const channel = name => (top[name] * top.a + bottom[name] * bottom.a * (1 - top.a)) / a;
    return { r: channel('r'), g: channel('g'), b: channel('b'), a };
  };

  const luminance = color => {
    const channel = value => {
      const normalized = Math.min(255, Math.max(0, value)) / 255;
      return normalized <= 0.03928 ? normalized / 12.92 : Math.pow((normalized + 0.055) / 1.055, 2.4);
    };
    return 0.2126 * channel(color.r) + 0.7152 * channel(color.g) + 0.0722 * channel(color.b);
  };

  const contrast = (first, second) => {
    const a = luminance(first);
    const b = luminance(second);
    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  };

  const css = color => '#' + [color.r, color.g, color.b].map(value => Math.round(Math.min(255, Math.max(0, value))).toString(16).padStart(2, '0')).join('');

  class Unresolved extends Error {}

  // Splits a computed list on its top-level commas.
  const layers = value => {
    const parts = [];
    let depth = 0;
    let start = 0;
    for (let index = 0; index < value.length; index++) {
      const character = value[index];
      if (character === '(') depth++;
      else if (character === ')') depth--;
      else if (character === ',' && depth === 0) { parts.push(value.slice(start, index).trim()); start = index + 1; }
    }
    parts.push(value.slice(start).trim());
    return parts;
  };

  // The colour of one stop of a computed gradient: its leading colour, the positions after it ignored.
  // An argument without a colour (the direction, the shape and centre) gives null.
  const stopColor = argument => {
    const match = /^(rgba?\([^)]*\)|color\([^)]*\)|#[0-9a-f]{3,8}|transparent)/.exec(argument.trim().toLowerCase());
    return match ? parse(match[1]) : null;
  };

  // A point between two stops, interpolated the way gradients are, in premultiplied sRGB.
  const between = (first, second, t) => {
    const a = first.a + (second.a - first.a) * t;
    if (a <= 0) return { ...TRANSPARENT };
    const channel = name => (first[name] * first.a * (1 - t) + second[name] * second.a * t) / a;
    return { r: channel('r'), g: channel('g'), b: channel('b'), a };
  };

  const key = color => [color.r, color.g, color.b, color.a].map(value => value.toFixed(3)).join(',');
  const unique = colors => [...new Map(colors.map(color => [key(color), color])).values()];

  // Every colour a gradient layer can paint: each stop and four points between each pair of stops. A
  // flat gradient (the grid's row and column fills, linear-gradient(c, c)) reduces to its one colour. A
  // gradient whose stops cannot be read is reported, never guessed.
  const gradientColors = (node, layer) => {
    const gradient = /^(?:repeating-)?(?:linear|radial|conic)-gradient\((.*)\)$/.exec(layer);
    const stops = gradient ? layers(gradient[1]).map(stopColor).filter(Boolean) : [];
    if (stops.length < 2) throw new Unresolved('fond en dégradé illisible sur ' + describe(node) + ' : ' + layer.slice(0, 100));
    const colors = [];
    for (let index = 0; index < stops.length - 1; index++) {
      for (const t of [0, 0.25, 0.5, 0.75]) colors.push(between(stops[index], stops[index + 1], t));
    }
    colors.push(stops[stops.length - 1]);
    return unique(colors);
  };

  // An element's background as the list of colours it can paint under a point: background-color under
  // its image layers. A flat colour gives one; a gradient gives every colour it passes through, and
  // several gradient layers every combination of theirs, a superset of what is painted, so the worst
  // reading is never missed. An icon image (url) is not under text.
  const backgroundOf = node => {
    const style = getComputedStyle(node);
    const color = parse(style.backgroundColor);
    if (!color) throw new Unresolved('couleur de fond illisible sur ' + describe(node) + ' : ' + style.backgroundColor);
    const image = style.backgroundImage;
    if (!image || image === 'none') return [color];
    let colors = [color];
    for (const layer of layers(image).reverse()) {
      if (/^url\(/.test(layer)) continue;
      const painted = gradientColors(node, layer);
      colors = unique(colors.flatMap(below => painted.map(top => over(top, below))));
    }
    return colors;
  };

  // The colours one pixel of an element can take as painted: an optional ink (a glyph of its text, an
  // icon stroke, a border, a pseudo-element dot) over the element's own background, the whole group
  // taken at the element's opacity and laid over its parent's group, up to the canvas. An overlay (the
  // busy veil, an ::after positioned over the content) is laid above the content of its element. The
  // list has one entry per combination of the backgrounds met on the way, in a fixed order, so two
  // walks over the same ancestors (with and without the ink) line up entry by entry.
  const pixel = (element, { ink = null, overlay = null } = {}) => {
    let groups = [ink ? { ...ink } : { ...TRANSPARENT }];
    for (let node = element; node; node = node.parentElement) {
      const style = getComputedStyle(node);
      const backgrounds = backgroundOf(node);
      const opacity = parseFloat(style.opacity);
      groups = groups.flatMap(group => backgrounds.map(background => {
        let next = over(group, background);
        if (overlay && overlay.element === node) next = over(overlay.color, next);
        return opacity < 1 ? { ...next, a: next.a * opacity } : next;
      }));
    }
    return groups.map(group => over(group, WHITE));
  };

  // The lowest contrast between the colours an ink can take and those of the ground beside it: entry
  // by entry when both walks met the same backgrounds, every pairing otherwise.
  const worst = (foregrounds, backgrounds) => {
    const pairs = foregrounds.length === backgrounds.length
      ? foregrounds.map((foreground, index) => [foreground, backgrounds[index]])
      : foregrounds.flatMap(foreground => backgrounds.map(background => [foreground, background]));
    let lowest = null;
    for (const [foreground, background] of pairs) {
      const ratio = contrast(foreground, background);
      if (!lowest || ratio < lowest.ratio) lowest = { foreground, background, ratio };
    }
    return lowest;
  };

  const describe = element => {
    if (!element) return '(aucun)';
    const id = element.id ? '#' + element.id : '';
    const classes = typeof element.className === 'string' && element.className.trim() ? '.' + element.className.trim().split(/\s+/).slice(0, 3).join('.') : '';
    return element.tagName.toLowerCase() + id + classes;
  };

  const visible = element => {
    if (!element || !element.isConnected) return false;
    const style = getComputedStyle(element);
    const box = element.getBoundingClientRect();
    return style.display !== 'none' && style.visibility !== 'hidden' && box.width > 0 && box.height > 0;
  };

  // The element that holds the first visible text of a target: the one whose own text node is not
  // blank, skipping text kept for assistive technologies only.
  const textHolder = root => {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
      acceptNode: node => node.nodeValue.trim() && visible(node.parentElement) && !node.parentElement.closest('.omni-visually-hidden, [aria-hidden="true"]:not(svg)')
        ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT
    });
    const node = walker.nextNode();
    return node ? node.parentElement : null;
  };

  const block = key => document.querySelector('.showcase-stage section[aria-labelledby="stage-' + key + '-title"]');
  const within = (root, selector) => (root ? [...root.querySelectorAll(selector)] : []).filter(visible);
  const first = (root, selector) => within(root, selector)[0] ?? null;
  const ICON_ONLY = ':has(> .omni-button__content > .omni-icon:only-child)';

  // The closed list. kind: text (4.5), icon (3.0, the icon's stroke or fill against the pixel beside
  // it), dot (3.0, a ::before mark), border (1.7, the border against the pixel outside the element).
  // interactive: measured under forced hover and focus as well. Each entry must resolve.
  const targets = page => {
    const entries = [];
    const add = (id, element, kind = 'text', interactive = false, extra = {}) => entries.push({ id, element, kind, interactive, ...extra });
    if (page === '${CUSTOMIZER}') {
      const buttons = block('boutons');
      for (const variant of ['primary', 'secondary', 'ghost', 'success', 'warning', 'info', 'danger']) {
        add('bouton ' + variant, first(buttons, '.omni-button--' + variant + '.omni-button--medium:not(.omni-busy):not(:disabled):not(' + ICON_ONLY + ')'), 'text', true);
      }
      within(buttons, '.omni-button:not(.omni-busy)' + ICON_ONLY).forEach((button, index) => add('bouton icône seul ' + (index + 1), button, 'icon', true));
      within(block('coquille-application'), '.omni-button' + ICON_ONLY).forEach((button, index) => add('bouton icône seul de la coquille ' + (index + 1), button, 'icon', true));
      const surfaces = block('surfaces');
      add('lien', first(surfaces, '.omni-link'), 'text', true);
      add('onglet', first(surfaces, '.omni-tabs__tab[aria-selected="false"]'), 'text', true);
      add('onglet choisi', first(surfaces, '.omni-tabs__tab[aria-selected="true"]'), 'text', true);
      add('élément de menu', first(surfaces, '.omni-panel-menu__link:not(.omni-panel-menu__link--current):not([aria-current])'), 'text', true);
      add('élément de menu courant', first(surfaces, '.omni-panel-menu__link--current, .omni-panel-menu__link[aria-current]'), 'text', true);
      add('menu latéral de la vitrine', first(document.querySelector('#showcase-theme > .omni-layout .omni-sidebar'), '.omni-panel-menu__link:not(.omni-panel-menu__link--current):not([aria-current])'), 'text', true);
      add('menu latéral de la vitrine, page courante', first(document.querySelector('#showcase-theme > .omni-layout .omni-sidebar'), '.omni-panel-menu__link--current, .omni-panel-menu__link[aria-current]'), 'text', true);
      const card = first(surfaces, '.omni-card');
      add('titre de carte', card ? first(card, '.omni-heading') : null);
      add('texte de carte', card ? first(card, 'p') : null);
      add('bord de carte', card, 'card');
      const sorted = document.getElementById('runs-sorted');
      add('en-tête de grille', first(sorted, 'thead th.omni-data-grid__column--sortable:not(.omni-data-grid__column--active)'), 'text', true);
      add('en-tête de grille trié', first(sorted, 'thead th.omni-data-grid__column--active'), 'text', true);
      const row = first(sorted, 'tbody tr.omni-data-grid__row:not(.omni-data-grid__row--selected)');
      add('ligne de grille', row, 'text', true, { text: row ? first(row, 'td.omni-data-grid__column--align-start:not(.omni-data-grid__column--active)') : null });
      add('ligne de grille, colonne triée', row, 'text', true, { text: row ? first(row, 'td.omni-data-grid__column--active') : null });
      const selected = first(sorted, 'tbody tr.omni-data-grid__row--selected');
      add('ligne de grille choisie', selected, 'text', true, { text: selected ? first(selected, 'td.omni-data-grid__column--align-start:not(.omni-data-grid__column--active)') : null });
      const striped = first(document.getElementById('runs-striped'), 'tbody tr.omni-data-grid__row--alternate');
      add('ligne de grille alternée', striped, 'text', true, { text: striped ? first(striped, 'td.omni-data-grid__column--align-start') : null });
      add('statut dans la grille', first(sorted, '.omni-status'));
      const forms = block('formulaires');
      add('liste déroulante', first(forms, 'select.omni-drop-down'), 'text', true);
      const input = first(forms, 'input.omni-input:not([type="checkbox"]):not([type="radio"]):not([readonly]):not(:disabled)');
      add('champ de saisie', input, 'value', true);
      add('bord du champ de saisie', input, 'border', true);
      const badges = block('badges');
      for (const variant of ['neutral', 'accent', 'info', 'success', 'warning', 'danger']) {
        add('badge ' + variant, first(badges, '.omni-badge--' + variant + ':not(.omni-badge--outline):not(.omni-badge--solid)'));
        add('badge plein ' + variant, first(badges, '.omni-badge--solid.omni-badge--' + variant));
        if (variant !== 'info') add('badge contour ' + variant, first(badges, '.omni-badge--outline.omni-badge--' + variant));
      }
      const alerts = block('alertes');
      for (const severity of ['info', 'success', 'warning', 'danger']) {
        for (const [variant, label] of [['solid', 'pleine'], ['outline', 'contour'], ...(severity === 'info' ? [['tonal', 'tonale']] : [])]) {
          const alert = first(alerts, '.omni-alert--' + variant + '.omni-alert--' + severity);
          add('alerte ' + label + ' ' + severity, alert, 'text', false, { text: alert ? textHolder(first(alert, '.omni-alert__content') ?? alert) : null });
          add('glyphe d\'alerte ' + label + ' ' + severity, alert ? first(alert, '.omni-alert__icon') : null, 'icon');
        }
      }
      add('fermeture d\'alerte', first(alerts, '.omni-alert__dismiss'), 'icon', true);
      const settings = block('reglages-onglets');
      add('titre de tuile de réglage', first(settings, '.omni-settings-tile__title'));
      add('indice de tuile de réglage', first(settings, '.omni-settings-hint'));
      add('texte de la page', first(document.querySelector('main'), 'section > p'));
    } else if (page === '/composants/listes') {
      const board = first(document, '.omni-kanban');
      const column = board ? first(board, '.omni-kanban__column') : null;
      add('titre de colonne de Kanban', column ? first(column, '.omni-kanban__title') : null);
      add('compteur de colonne de Kanban', column ? first(column, '.omni-kanban__count') : null);
      add('carte de Kanban', column ? first(column, '.omni-kanban__card') : null, 'text', true);
      add('bord de colonne de Kanban', column, 'border');
    } else if (page === '/composants/retours') {
      const seen = new Set();
      for (const item of within(document, '.omni-status-strip__item')) {
        const strip = item.closest('.omni-status-strip');
        const key = (strip.classList.contains('omni-status-strip--segment') ? 'segment ' : 'pastille ') + (item.getAttribute('data-omni-status') ?? '?');
        if (seen.has(key)) continue;
        seen.add(key);
        add('bande d\'état, ' + key, item, 'dot', item.matches('a, button'));
      }
      if (seen.size === 0) add('bande d\'état', null, 'dot');
    } else if (page === '/composants/selection') {
      const box = document.getElementById('demo-city');
      add('option de liste', box ? [...box.options].find(option => !option.selected) ?? null : null);
      add('option de liste choisie', box ? [...box.options].find(option => option.selected) ?? null : null);
      // The drop-down panel of the compact multiple selection, opened: its options and its action sit
      // on the floating layer. The layer is positioned over the page; its backdrop is taken as the page
      // surface of its ancestors, which is what lies under it on this demonstration.
      const compact = document.getElementById('demo-tags-compact');
      if (compact && !compact.open) compact.open = true;
      add('option de liste déroulante', compact ? first(compact, '.omni-multi-select-compact__option') : null, 'text', true);
      add('action de liste déroulante', compact ? first(compact, '.omni-multi-select-compact__clear') : null, 'text', true);
    }
    return entries;
  };

  let current = [];

  // Tags the targets of the page and returns what the probe needs to force their states. One element
  // may serve several entries (a grid row, measured in two of its cells), so the tag is a list.
  const collect = page => {
    for (const element of document.querySelectorAll('[data-omni-contrast]')) element.removeAttribute('data-omni-contrast');
    current = targets(page);
    return current.map((entry, index) => {
      if (entry.element) entry.element.setAttribute('data-omni-contrast', ((entry.element.getAttribute('data-omni-contrast') ?? '') + ' ' + index).trim());
      return { index, id: entry.id, kind: entry.kind, interactive: entry.interactive, found: Boolean(entry.element) && ('text' in entry ? Boolean(entry.text) : true), node: describe(entry.element) };
    });
  };

  const iconOf = element => {
    const svg = element.querySelector('svg');
    if (!svg) return null;
    const style = getComputedStyle(svg);
    const paint = [style.stroke, style.fill].map(parse).find(color => color && color.a > 0);
    return paint ? { svg, paint } : { svg, paint: null };
  };

  // One measure: the painted ink against the painted pixel beside it, with the ratio it must reach.
  const measureEntry = entry => {
    const element = entry.element;
    if (!element || !element.isConnected) return { error: 'élément absent' };
    try {
      if (entry.kind === 'text' || entry.kind === 'value') {
        const holder = entry.text ?? (entry.kind === 'value' ? element : textHolder(element) ?? element);
        const ink = parse(getComputedStyle(holder).color);
        if (!ink) return { error: 'couleur de texte illisible : ' + getComputedStyle(holder).color };
        const reading = worst(pixel(holder, { ink }), pixel(holder));
        return { holder: describe(holder), foreground: css(reading.foreground), background: css(reading.background), ratio: reading.ratio, required: 4.5 };
      }
      if (entry.kind === 'icon') {
        const icon = iconOf(element);
        if (!icon || !icon.paint) return { error: 'icône sans trait ni remplissage visible' };
        const reading = worst(pixel(icon.svg, { ink: icon.paint }), pixel(icon.svg));
        return { holder: describe(icon.svg), foreground: css(reading.foreground), background: css(reading.background), ratio: reading.ratio, required: 3.0 };
      }
      if (entry.kind === 'dot') {
        const mark = getComputedStyle(element, '::before');
        const color = parse(mark.backgroundColor);
        if (!color) return { error: 'pastille illisible : ' + mark.backgroundColor };
        const ink = { ...color, a: color.a * parseFloat(mark.opacity) };
        const reading = worst(pixel(element, { ink }), pixel(element));
        return { holder: describe(element) + '::before', foreground: css(reading.foreground), background: css(reading.background), ratio: reading.ratio, required: 3.0 };
      }
      if (entry.kind === 'border') {
        const style = getComputedStyle(element);
        const color = parse(style.borderTopColor);
        if (!color) return { error: 'bordure illisible : ' + style.borderTopColor };
        if (parseFloat(style.borderTopWidth) <= 0) return { error: 'bordure absente' };
        const reading = worst(pixel(element, { ink: color }), pixel(element.parentElement));
        return { holder: describe(element), foreground: css(reading.foreground), background: css(reading.background), ratio: reading.ratio, required: 1.7 };
      }
      if (entry.kind === 'card') {
        // Recorded, not judged: a card may stand out by its border, its fill or a ring of its shadow,
        // and no static test sets a floor for that. The screenshots judge it.
        const style = getComputedStyle(element);
        const outside = pixel(element.parentElement);
        const fill = worst(pixel(element), outside);
        const border = parse(style.borderTopColor);
        const borderRatio = parseFloat(style.borderTopWidth) > 0 && border ? worst(pixel(element, { ink: border }), outside).ratio : 1;
        return { observation: true, holder: describe(element), background: css(fill.background), foreground: css(fill.foreground), fillRatio: fill.ratio, borderRatio, shadow: style.boxShadow.slice(0, 160) };
      }
      return { error: 'type de mesure inconnu : ' + entry.kind };
    } catch (error) {
      if (error instanceof Unresolved) return { error: error.message };
      throw error;
    }
  };

  const measure = () => current.map((entry, index) => ({ index, ...measureEntry(entry) }));

  // The busy veil: read the ::after colour, step its animation to find the peak opacity, then measure
  // the content of the control under it at that peak.
  const measureBusy = () => {
    const results = [];
    for (const control of [...document.querySelectorAll('.showcase-stage .omni-busy')].filter(visible)) {
      const veil = getComputedStyle(control, '::after');
      const color = parse(veil.backgroundColor);
      if (!color) { results.push({ node: describe(control), error: 'voile illisible : ' + veil.backgroundColor }); continue; }
      const animations = document.getAnimations().filter(animation => animation.effect?.target === control && animation.effect?.pseudoElement === '::after');
      let peak = parseFloat(veil.opacity);
      for (const animation of animations) {
        const duration = Number(animation.effect.getComputedTiming().duration) || 0;
        const time = animation.currentTime;
        animation.pause();
        for (let step = 0; step <= 40; step++) {
          animation.currentTime = duration * step / 40;
          peak = Math.max(peak, parseFloat(getComputedStyle(control, '::after').opacity));
        }
        animation.currentTime = time;
        animation.play();
      }
      const overlay = { element: control, color: { ...color, a: color.a * peak } };
      const holder = textHolder(control);
      const icon = holder ? null : iconOf(control);
      const target = holder ?? icon?.svg;
      if (!target) { results.push({ node: describe(control), error: 'contrôle occupé sans contenu mesurable' }); continue; }
      const ink = holder ? parse(getComputedStyle(holder).color) : icon.paint;
      const veiled = pixel(target, { overlay });
      const reading = worst(pixel(target, { ink, overlay }), veiled);
      results.push({
        node: describe(control), label: (control.textContent.trim() || control.getAttribute('aria-label') || '').slice(0, 40),
        veil: css(color), peak, foreground: css(reading.foreground), background: css(reading.background),
        ratio: reading.ratio, required: holder ? 4.5 : 3.0,
        restRatio: worst(pixel(target, { ink }), pixel(target)).ratio,
        // How far the veil moves the fill from its rest colour at the peak: recorded, not judged.
        veilShift: worst(pixel(target), veiled).ratio
      });
    }
    return results;
  };

  // Grid cells keep the table model, icon-only buttons show their icon whole.
  const geometry = () => {
    const failures = [];
    let checks = 0;
    for (const cell of document.querySelectorAll('.omni-data-grid__table th, .omni-data-grid__table td')) {
      if (!cell.isConnected || cell.closest('[hidden]')) continue;
      checks++;
      const display = getComputedStyle(cell).display;
      if (display !== 'table-cell') failures.push({ check: 'cellule de grille', node: describe(cell), detail: 'display: ' + display });
    }
    for (const button of [...document.querySelectorAll('.omni-button' + ICON_ONLY)].filter(visible)) {
      checks++;
      const icon = button.querySelector('.omni-button__content > .omni-icon');
      const svg = icon?.querySelector('svg') ?? icon;
      const box = svg?.getBoundingClientRect();
      const frame = button.getBoundingClientRect();
      const style = svg ? getComputedStyle(svg) : null;
      const label = describe(button) + ' ' + (button.getAttribute('aria-label') ?? '');
      if (!svg || !visible(svg) || parseFloat(style.opacity) === 0) { failures.push({ check: 'icône du bouton icône seul', node: label, detail: 'icône invisible' }); continue; }
      if (box.width < 8 || box.height < 8) failures.push({ check: 'icône du bouton icône seul', node: label, detail: 'icône de ' + box.width.toFixed(1) + ' x ' + box.height.toFixed(1) + ' px' });
      const tolerance = 0.5;
      if (box.left < frame.left - tolerance || box.right > frame.right + tolerance || box.top < frame.top - tolerance || box.bottom > frame.bottom + tolerance) {
        failures.push({ check: 'icône du bouton icône seul', node: label, detail: 'icône rognée : icône ' + [box.left, box.top, box.right, box.bottom].map(value => value.toFixed(1)).join(',') + ' hors du bouton ' + [frame.left, frame.top, frame.right, frame.bottom].map(value => value.toFixed(1)).join(',') });
      }
    }
    return { checks, failures };
  };

  // The rendering margin pairs of ThemeContrastMatrixTests, resolved on the theme scope.
  const marginPairs = () => {
    const scope = document.getElementById('showcase-theme');
    const style = getComputedStyle(scope);
    const token = name => {
      const probe = document.createElement('span');
      probe.style.setProperty('color', 'var(' + name + ')');
      scope.appendChild(probe);
      const color = parse(getComputedStyle(probe).color);
      probe.remove();
      return color ? css(color) : null;
    };
    return [
      ['--omni-color-on-accent', '--omni-color-accent'],
      ['--omni-color-on-accent', '--omni-color-accent-strong'],
      ['--omni-color-on-danger', '--omni-color-danger'],
      ['--omni-color-on-success', '--omni-color-success'],
      ['--omni-color-on-warning', '--omni-color-warning']
    ].map(([text, fill]) => ({ text, fill, foreground: token(text), background: token(fill) }));
  };

  // Transitions started by a change of combination or of forced state: wait until none runs. Endless
  // animations (the busy veil, loading bars) are not transitions and are left alone.
  const settle = async (timeout = 5000) => {
    const start = performance.now();
    let running = [];
    while (performance.now() - start < timeout) {
      running = document.getAnimations().filter(animation => animation instanceof CSSTransition && (animation.playState === 'running' || animation.playState === 'pending'));
      if (running.length === 0) break;
      await Promise.race([Promise.allSettled(running.map(animation => animation.finished)), new Promise(done => setTimeout(done, 250))]);
    }
    await new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)));
    return running.length === 0 ? 0 : running.length;
  };

  // The shipped look: the stylesheet alone, without the tokens the customizer writes on the scope.
  let stripped = null;
  const stripTheme = () => {
    const scope = document.getElementById('showcase-theme');
    stripped = [];
    for (const name of [...scope.style]) {
      if (name.startsWith('--')) { stripped.push([name, scope.style.getPropertyValue(name)]); }
    }
    for (const [name] of stripped) scope.style.removeProperty(name);
    return stripped.length;
  };
  const restoreTheme = () => {
    const scope = document.getElementById('showcase-theme');
    for (const [name, value] of stripped ?? []) scope.style.setProperty(name, value);
    const count = stripped?.length ?? 0;
    stripped = null;
    return count;
  };

  const overflow = () => ({ scrollWidth: document.body.scrollWidth, clientWidth: document.documentElement.clientWidth, innerWidth });

  const box = selector => {
    const element = document.querySelector(selector);
    if (!element) return null;
    const rect = element.getBoundingClientRect();
    return { x: rect.left + scrollX, y: rect.top + scrollY, width: rect.width, height: rect.height };
  };

  window.__omniContrast = { collect, measure, measureBusy, geometry, marginPairs, settle, stripTheme, restoreTheme, overflow, box };
})();`;

// ---------------------------------------------------------------------------------------------------
// CDP plumbing (as in the other showcase probes).
// ---------------------------------------------------------------------------------------------------
const deadline = Date.now() + 20_000;
let target;
while (Date.now() < deadline) {
  try {
    const targets = await fetch(`${endpoint}/json/list`).then(response => response.json());
    target = targets.find(candidate => candidate.type === 'page' && candidate.webSocketDebuggerUrl);
    if (target) break;
  } catch {
    // The browser may still be starting.
  }
  await new Promise(done => setTimeout(done, 100));
}
if (!target) throw new Error(`Aucune cible CDP disponible sur ${endpoint}.`);

const socket = new WebSocket(target.webSocketDebuggerUrl);
await new Promise((done, reject) => {
  socket.addEventListener('open', done, { once: true });
  socket.addEventListener('error', () => reject(new Error('Connexion CDP impossible.')), { once: true });
});

let commandId = 0;
const pending = new Map();
const consoleErrors = [];
socket.addEventListener('message', event => {
  const message = JSON.parse(event.data);
  if (message.id && pending.has(message.id)) {
    const { done, reject } = pending.get(message.id);
    pending.delete(message.id);
    if (message.error) reject(new Error(message.error.message));
    else done(message.result);
    return;
  }
  if (message.method === 'Runtime.exceptionThrown') {
    consoleErrors.push(message.params.exceptionDetails.exception?.description ?? message.params.exceptionDetails.text);
  } else if (message.method === 'Log.entryAdded' && message.params.entry.level === 'error') {
    const source = message.params.entry.url ? ` (${message.params.entry.url})` : '';
    consoleErrors.push(`${message.params.entry.text}${source}`);
  } else if (message.method === 'Runtime.consoleAPICalled' && message.params.type === 'error') {
    consoleErrors.push(message.params.args.map(argument => argument.value ?? argument.description ?? '').join(' '));
  }
});

const send = (method, params = {}) => {
  const id = ++commandId;
  socket.send(JSON.stringify({ id, method, params }));
  return new Promise((done, reject) => pending.set(id, { done, reject }));
};

const evaluate = async expression => {
  const response = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
  if (response.exceptionDetails) throw new Error(response.exceptionDetails.exception?.description ?? response.exceptionDetails.text);
  return response.result.value;
};

const pause = milliseconds => new Promise(done => setTimeout(done, milliseconds));

const waitFor = async (description, expression, timeout = 15_000) => {
  const until = Date.now() + timeout;
  while (Date.now() < until) {
    try {
      if (await evaluate(`Boolean(${expression})`)) return;
    } catch (error) {
      if (!String(error.message).includes('context was destroyed')) throw error;
    }
    await pause(50);
  }
  throw new Error(`Attente dépassée : ${description}.`);
};

const library = () => evaluate(pageLibrary);
const lib = (call) => evaluate(`window.__omniContrast.${call}`);

// ---------------------------------------------------------------------------------------------------
// The run.
// ---------------------------------------------------------------------------------------------------
const failures = [];
const observations = [];
const busyReadings = [];
const acceptedBusyVeil = [];
// The veil the owner accepted on 2026-09-18; anything else under the ratio is a failure.
const ACCEPTED_VEIL = { color: '#000000', peak: 0.55 };
let measures = 0;
let marginMeasures = 0;
let combinations = 0;
const shots = [];

// Themes that declare a contrast waiver (ThemeDefinition.ContrastWaiver, owner decision of 2026-09-28),
// read from the customizer's data-contrast-waiver note when the theme is picked: the catalogue stays the
// only source. Their contrast shortfalls (text, border, non-text mark, content under the busy veil, at
// rest, hovered or focused) are counted under acceptedContrastWaiver instead of failing. The focus ring
// itself is not measured here: ThemePaletteTests.The_style_themes_draw_a_solid_focus_ring keeps it at
// 3:1 in the four themes that draw a solid ring (Relief, Givre, Aplat, Épure) with every palette, waiver or not (owner decision of 2026-09-28: only the ring
// stays mandatory). Geometry, overflow, CSP, console and coverage are never waived.
const waivers = {};
// Themes drawn in dark mode only (OmniThemePreset.DarkOnly): the customizer fixes their mode picker on
// dark, so their light half is never drawn and is not measured; asking for it must still draw dark.
const darkOnly = {};
// A dark-only theme found drawn light is reported once and not measured further.
const unmeasurable = new Set();
const modesOf = theme => unmeasurable.has(theme) ? [] : darkOnly[theme] ? chosenModes.filter(mode => mode === 'dark') : chosenModes;
const acceptedContrastWaiver = [];
const WAIVABLE_CHECKS = new Set(['texte', 'bordure', 'marque non textuelle', 'voile d\'occupation']);

const fail = (combo, entry) => {
  const record = { theme: combo.theme, palette: combo.palette, mode: combo.mode, ...entry };
  if (waivers[combo.theme] && entry.ratio !== undefined && WAIVABLE_CHECKS.has(entry.check)) {
    acceptedContrastWaiver.push(record);
    return;
  }
  failures.push(record);
};

await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await send('DOM.enable');
await send('CSS.enable');
const DESKTOP = { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false };
await send('Emulation.setDeviceMetricsOverride', DESKTOP);
await send('Page.addScriptToEvaluateOnNewDocument', {
  source: "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
});
// Starts from a clean customizer: an earlier probe or visit may have left a theme, a mode or a density
// in this browser's storage, and the combination must come from the pickers alone.
await send('Page.navigate', { url: siteUrl });
await waitFor('la vitrine', "document.readyState === 'complete'", 30_000);
await evaluate("(() => { try { localStorage.removeItem('omnieurope.showcase.theme'); } catch { } })()");
await send('Page.navigate', { url: siteUrl });
await waitFor('le runtime Blazor', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null", 30_000);
await pause(1500);
await library();

const navigate = async (path, ready) => {
  await evaluate(`Blazor.navigateTo(${JSON.stringify(path)})`);
  await waitFor(`la page ${path}`, `location.pathname === ${JSON.stringify(path)} && document.querySelector('main') !== null && (${ready})`);
  await library();
};

const READY = {
  [CUSTOMIZER]: "document.getElementById('workshop-theme') && document.getElementById('runs-sorted') && document.querySelector('.showcase-stage .omni-card')",
  '/': "document.querySelector('main h1, main .omni-heading')",
  '/composants/listes': "document.querySelector('.omni-kanban__card')",
  '/composants/retours': "document.querySelector('.omni-status-strip__item')",
  '/composants/selection': "document.getElementById('demo-city')"
};

// The customizer's pickers. Selecting a theme gives it its own palette; the mode is kept.
const readState = () => evaluate(`({
  theme: document.getElementById('workshop-theme').selectedOptions[0]?.textContent.trim(),
  palette: document.getElementById('workshop-palette').selectedOptions[0]?.textContent.trim(),
  mode: document.getElementById('showcase-theme').getAttribute('data-omni-theme')
})`);

const pick = async (id, name) => {
  await evaluate(`(() => {
    const select = document.getElementById(${JSON.stringify(id)});
    const option = [...select.options].find(candidate => candidate.textContent.trim() === ${JSON.stringify(name)});
    if (!option) throw new Error('Option absente : ' + ${JSON.stringify(name)});
    select.value = option.value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  })()`);
};

const setMode = async mode => {
  const label = mode === 'dark' ? 'Sombre' : 'Clair';
  await evaluate(`[...document.querySelectorAll('#workshop-mode .omni-select-bar__item')].find(item => item.textContent.trim() === ${JSON.stringify(label)}).click()`);
  await waitFor(`le mode ${mode}`, `document.getElementById('showcase-theme').getAttribute('data-omni-theme') === ${JSON.stringify(mode)}`);
};

const apply = async (theme, palette, mode) => {
  const state = await readState();
  if (state.theme !== theme) {
    await pick('workshop-theme', theme);
    await waitFor(`le thème ${theme}`, `document.getElementById('workshop-theme').selectedOptions[0]?.textContent.trim() === ${JSON.stringify(theme)}`);
  }
  if ((await readState()).mode !== mode) await setMode(mode);
  if ((await readState()).palette !== palette) {
    await pick('workshop-palette', palette);
    await waitFor(`la palette ${palette}`, `document.getElementById('workshop-palette').selectedOptions[0]?.textContent.trim() === ${JSON.stringify(palette)}`);
  }
  const applied = await readState();
  if (applied.theme !== theme || applied.palette !== palette || applied.mode !== mode) {
    throw new Error(`Combinaison non appliquée : attendu ${theme} + ${palette} en ${mode}, obtenu ${JSON.stringify(applied)}.`);
  }
  return settleOrFail({ theme, palette, mode }, 'application de la combinaison');
};

const settleOrFail = async (combo, moment) => {
  const left = await lib('settle()');
  if (left > 0) fail(combo, { page: combo.page ?? CUSTOMIZER, check: 'transitions', detail: `${left} transition(s) encore en cours 5 s après ${moment}` });
};

// Forces a pseudo-state on every interactive target of the page, returns a function that lifts it.
// A target the DOM domain cannot find is a failure: measuring it unforced would pass for a hover.
const force = async (combo, page, entries, states) => {
  const { root } = await send('DOM.getDocument', { depth: 0 });
  const forced = new Set();
  for (const entry of entries.filter(candidate => candidate.interactive && candidate.found)) {
    const { nodeId } = await send('DOM.querySelector', { nodeId: root.nodeId, selector: `[data-omni-contrast~="${entry.index}"]` });
    if (!nodeId) {
      fail(combo, { page, target: entry.id, state: states.join('+'), check: 'forçage', detail: 'nœud introuvable par CDP' });
      entry.unforced = true;
      continue;
    }
    if (forced.has(nodeId)) continue;
    await send('CSS.forcePseudoState', { nodeId, forcedPseudoClasses: states });
    forced.add(nodeId);
  }
  return async () => {
    for (const nodeId of forced) {
      await send('CSS.forcePseudoState', { nodeId, forcedPseudoClasses: [] }).catch(() => {});
    }
  };
};

let margin = null;

// Measures every target of the current page in the three states.
const measurePage = async (combo, page) => {
  const context = { ...combo, page };
  const entries = await lib(`collect(${JSON.stringify(page)})`);
  await settleOrFail(context, 'la collecte');
  for (const entry of entries.filter(candidate => !candidate.found)) {
    fail(combo, { page, target: entry.id, check: 'liste fermée', detail: `élément introuvable (${entry.node})` });
  }
  const rendered = {};
  for (const [state, pseudo] of [['repos', null], ['survol', ['hover']], ['focus', ['focus', 'focus-visible']]]) {
    let lift = null;
    if (pseudo) {
      lift = await force(combo, page, entries, pseudo);
      await settleOrFail(context, `le forçage ${state}`);
    }
    const results = await lib('measure()');
    for (const result of results) {
      const entry = entries[result.index];
      if (!entry.found || (state !== 'repos' && (!entry.interactive || entry.unforced))) continue;
      if (result.error) {
        fail(combo, { page, target: entry.id, state, check: 'mesure', detail: result.error });
        continue;
      }
      if (result.observation) {
        if (state === 'repos') observations.push({ ...combo, page, target: entry.id, ...result });
        continue;
      }
      measures++;
      let required = result.required;
      // The rendering margin of RET-002 n°51, where the static matrix applies it.
      if (margin && margin.some(pair => pair.foreground === result.foreground && pair.background === result.background)) {
        required = Math.max(required, 5.0);
        marginMeasures++;
      }
      if (state === 'repos') rendered[entry.id] = `${result.foreground}/${result.background}`;
      if (result.ratio < required) {
        fail(combo, { page, target: entry.id, state, check: entry.kind === 'border' ? 'bordure' : entry.kind === 'text' || entry.kind === 'value' ? 'texte' : 'marque non textuelle', holder: result.holder, foreground: result.foreground, background: result.background, ratio: Math.round(result.ratio * 100) / 100, required });
      }
    }
    if (lift) {
      await lift();
      await settleOrFail(context, `la fin du forçage ${state}`);
    }
  }
  const geometry = await lib('geometry()');
  measures += geometry.checks;
  for (const failure of geometry.failures) fail(combo, { page, ...failure });
  return rendered;
};

const measureBusy = async combo => {
  const results = await lib('measureBusy()');
  if (results.length === 0) fail(combo, { page: CUSTOMIZER, check: 'voile d\'occupation', detail: 'aucun contrôle occupé trouvé' });
  for (const result of results) {
    if (result.error) { fail(combo, { page: CUSTOMIZER, check: 'voile d\'occupation', target: result.node, detail: result.error }); continue; }
    measures++;
    busyReadings.push({ ...combo, ...result });
    if (result.ratio < result.required) {
      const entry = { page: CUSTOMIZER, check: 'voile d\'occupation', target: `${result.node} « ${result.label} »`, foreground: result.foreground, background: result.background, veil: result.veil, peakOpacity: Math.round(result.peak * 100) / 100, ratio: Math.round(result.ratio * 100) / 100, restRatio: Math.round(result.restRatio * 100) / 100, required: result.required };
      if (result.veil === ACCEPTED_VEIL.color && result.peak <= ACCEPTED_VEIL.peak + 0.005) { acceptedBusyVeil.push({ ...combo, ...entry }); continue; }
      fail(combo, entry);
    }
  }
};

const slug = text => text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');

// A screenshot of the preview, from the button states to the end of the surfaces, plus the grids.
const shoot = async name => {
  if (!screenshots) return;
  await mkdir(shotDirectory, { recursive: true });
  const parts = [['etats', 'surfaces'], ['grilles-maquette', 'grilles-maquette']];
  let index = 0;
  for (const [from, to] of parts) {
    const top = await lib(`box('.showcase-stage section[aria-labelledby="stage-${from}-title"]')`);
    const bottom = await lib(`box('.showcase-stage section[aria-labelledby="stage-${to}-title"]')`);
    if (!top || !bottom) throw new Error(`Capture ${name} : section absente.`);
    const clip = { x: Math.max(0, top.x - 8), y: Math.max(0, top.y - 8), width: top.width + 16, height: bottom.y + bottom.height - top.y + 16, scale: 1 };
    const { data } = await send('Page.captureScreenshot', { format: 'png', clip, captureBeyondViewport: true });
    const file = join(shotDirectory, `${name}${index === 0 ? '' : '-grilles'}.png`);
    await writeFile(file, Buffer.from(data, 'base64'));
    shots.push(file);
    index++;
  }
};

const shootPage = async name => {
  if (!screenshots) return;
  await mkdir(shotDirectory, { recursive: true });
  const { data } = await send('Page.captureScreenshot', { format: 'png' });
  const file = join(shotDirectory, `${name}.png`);
  await writeFile(file, Buffer.from(data, 'base64'));
  shots.push(file);
};

await navigate(CUSTOMIZER, READY[CUSTOMIZER]);
const themes = await evaluate("[...document.getElementById('workshop-theme').options].map(option => option.textContent.trim())");
const palettes = await evaluate("[...document.getElementById('workshop-palette').options].map(option => option.textContent.trim())");
// The catalogue sizes are pinned once, by ThemePaletteTests; the probe only refuses empty pickers, and
// measures whatever the customizer offers.
if (themes.length === 0 || palettes.length === 0) {
  failures.push({ check: 'catalogue', detail: `${themes.length} thèmes et ${palettes.length} palettes proposés` });
}
const modes = ['light', 'dark'];
const chosenThemes = themes.filter(theme => !themeFilter || themeFilter.includes(theme));
const chosenPalettes = palettes.filter(palette => !paletteFilter || paletteFilter.includes(palette));
const chosenModes = modes.filter(mode => !modeFilter || modeFilter.includes(mode));

// 1. The shipped look (lot 7): the stylesheet alone must paint exactly what Essentiel + Essentiel paints.
for (const mode of chosenModes) {
  const combo = { theme: 'Essentiel', palette: 'Essentiel', mode };
  await apply('Essentiel', 'Essentiel', mode);
  margin = mode === 'light' ? await lib('marginPairs()') : null;
  const themed = await measurePage(combo, CUSTOMIZER);
  const count = await lib('stripTheme()');
  if (count === 0) fail(combo, { page: CUSTOMIZER, check: 'apparence livrée', detail: 'aucun jeton posé par le personnalisateur à retirer' });
  await settleOrFail(combo, 'le retrait des jetons');
  const shipped = await measurePage({ ...combo, theme: '(aucun thème)', palette: '(aucune palette)' }, CUSTOMIZER);
  if (mode === 'light' || mode === 'dark') await shoot(`vitrine-sans-theme-${mode}`);
  for (const [id, painted] of Object.entries(themed)) {
    measures++;
    if (shipped[id] !== painted) fail(combo, { page: CUSTOMIZER, target: id, check: 'apparence livrée', detail: `sans thème ${shipped[id] ?? '(non mesuré)'}, avec Essentiel + Essentiel ${painted}` });
  }
  await lib('restoreTheme()');
  await settleOrFail(combo, 'la remise des jetons');
}
if (chosenModes.length > 0) {
  // The shipped look as a visitor lands on it: the home page, stylesheet alone.
  for (const mode of chosenModes) {
    await apply('Essentiel', 'Essentiel', mode);
    await navigate('/', READY['/']);
    await lib('stripTheme()');
    await lib('settle()');
    await shootPage(`accueil-sans-theme-${mode}`);
    await lib('restoreTheme()');
    await navigate(CUSTOMIZER, READY[CUSTOMIZER]);
  }
}

// 2. Every combination (theme x palette x mode).
const defaultPalette = {};
const startedAt = Date.now();
for (const theme of chosenThemes) {
  // Light is asked before each theme while the mode picker allows it, so a dark-only theme proves it
  // draws dark whatever the mode asked.
  const lightAsked = await evaluate("document.getElementById('workshop-mode').getAttribute('aria-disabled') !== 'true'");
  if (lightAsked && (await readState()).mode !== 'light') await setMode('light');
  await pick('workshop-theme', theme);
  await waitFor(`le thème ${theme}`, `document.getElementById('workshop-theme').selectedOptions[0]?.textContent.trim() === ${JSON.stringify(theme)}`);
  // The picker's state is read once the theme is rendered, not as soon as the select holds it.
  await lib('settle()');
  defaultPalette[theme] = (await readState()).palette;
  waivers[theme] = await evaluate(`document.querySelector('[data-contrast-waiver]')?.dataset.contrastWaiver ?? null`);
  darkOnly[theme] = await evaluate("document.getElementById('workshop-mode').getAttribute('aria-disabled') === 'true'");
  if (darkOnly[theme] && lightAsked) {
    measures++;
    const drawn = await evaluate("document.getElementById('showcase-theme').getAttribute('data-omni-theme')");
    if (drawn !== 'dark') {
      // Its combinations cannot be applied either: the mode picker stays fixed while the page is light.
      fail({ theme, palette: defaultPalette[theme], mode: 'light' }, { page: CUSTOMIZER, check: 'thème sombre seulement', detail: `mode dessiné ${drawn}, attendu dark` });
      unmeasurable.add(theme);
      continue;
    }
  }
  for (const mode of modesOf(theme)) {
    for (const palette of chosenPalettes) {
      const combo = { theme, palette, mode };
      await apply(theme, palette, mode);
      combinations++;
      margin = theme === 'Essentiel' && palette === 'Essentiel' && mode === 'light' ? await lib('marginPairs()') : null;
      await measurePage(combo, CUSTOMIZER);
      await measureBusy(combo);
      margin = null;
      if (palette === defaultPalette[theme] || FOREIGN_SHOTS[theme]?.includes(palette)) {
        await shoot(`${slug(theme)}-${slug(palette)}-${mode}`);
      }
      for (const page of EXTRA_PAGES) {
        await navigate(page, READY[page]);
        await settleOrFail({ ...combo, page }, 'la navigation');
        await measurePage(combo, page);
      }
      await navigate(CUSTOMIZER, READY[CUSTOMIZER]);
      await settleOrFail(combo, 'le retour au personnalisateur');
    }
    const elapsed = Math.round((Date.now() - startedAt) / 1000);
    console.log(`  ${theme} en ${mode === 'dark' ? 'sombre' : 'clair'} : ${combinations} combinaison(s), ${measures} mesures, ${failures.length} échec(s), ${elapsed} s`);
  }
}

// 3. No horizontal overflow at 375 px (RET-002 n°28), each theme in both modes: palettes only paint.
await send('Emulation.setDeviceMetricsOverride', { width: 375, height: 812, deviceScaleFactor: 1, mobile: true });
for (const theme of chosenThemes) {
  for (const mode of modesOf(theme)) {
    await navigate(CUSTOMIZER, READY[CUSTOMIZER]);
    await apply(theme, defaultPalette[theme] ?? 'Essentiel', mode);
    for (const page of NARROW_PAGES) {
      await navigate(page, READY[page]);
      await lib('settle()');
      const width = await lib('overflow()');
      measures++;
      if (width.scrollWidth > width.clientWidth) {
        fail({ theme, palette: defaultPalette[theme], mode }, { page, check: 'débordement à 375 px', detail: `document.body.scrollWidth ${width.scrollWidth} > ${width.clientWidth}` });
      }
    }
  }
}
await send('Emulation.setDeviceMetricsOverride', DESKTOP);

const csp = await evaluate('window.__omniCsp');
socket.close();
for (const violation of csp) failures.push({ check: 'CSP', detail: violation });
for (const error of consoleErrors) failures.push({ check: 'console', detail: error });

const expected = chosenThemes.reduce((sum, theme) => sum + modesOf(theme).length, 0) * chosenPalettes.length;
if (!partial && combinations !== expected) failures.push({ check: 'couverture', detail: `${combinations} combinaisons mesurées au lieu de ${expected}` });

await mkdir(artifacts, { recursive: true });
await writeFile(registryPath, JSON.stringify({
  generatedAt: new Date().toISOString(),
  partial,
  combinations,
  expected,
  measures,
  failures,
  acceptedBusyVeil,
  contrastWaivers: Object.fromEntries(Object.entries(waivers).filter(([, reason]) => reason)),
  acceptedContrastWaiver,
  busyVeil: busyReadings.map(({ theme, palette, mode, label, veil, peak, foreground, background, ratio, restRatio, veilShift }) => ({ theme, palette, mode, label, veil, peak, foreground, background, ratio: Math.round(ratio * 100) / 100, restRatio: Math.round(restRatio * 100) / 100, veilShift: Math.round(veilShift * 100) / 100 })),
  cards: observations.map(({ theme, palette, mode, target, background, foreground, fillRatio, borderRatio, shadow }) => ({ theme, palette, mode, target, page: background, card: foreground, fillRatio: Math.round(fillRatio * 100) / 100, borderRatio: Math.round(borderRatio * 100) / 100, shadow })),
  screenshots: shots
}, null, 2));

if (partial) console.log(`Passage PARTIEL (filtres ${[themeFilter, paletteFilter, modeFilter].filter(Boolean).map(value => value.join('/')).join(', ')}) : il ne vaut pas preuve pour la porte.`);
if (failures.length > 0) {
  const grouped = new Map();
  for (const failure of failures) {
    const key = [failure.page, failure.target, failure.state, failure.check].filter(Boolean).join(' | ');
    const group = grouped.get(key) ?? { count: 0, sample: failure };
    group.count++;
    grouped.set(key, group);
  }
  const lines = [...grouped].slice(0, 60).map(([key, group]) => {
    const sample = group.sample;
    const where = [sample.theme, sample.palette, sample.mode].filter(Boolean).join(' + ');
    const value = sample.ratio !== undefined ? `${sample.foreground} sur ${sample.background} = ${sample.ratio} < ${sample.required}` : sample.detail;
    return `  ${key} x${group.count} (ex. ${where} : ${value})`;
  });
  console.error(`Contrastes : ${failures.length} échec(s) sur ${measures} mesures, ${combinations} combinaison(s). Registre : ${registryPath}\n${lines.join('\n')}${grouped.size > 60 ? `\n  ... ${grouped.size - 60} groupe(s) de plus dans le registre` : ''}`);
  process.exitCode = 1;
} else {
  const waived = Object.entries(Object.groupBy(acceptedContrastWaiver, entry => entry.theme)).map(([theme, entries]) => `${theme} ${entries.length}`).join(', ');
  console.log(`Sonde de contraste validée : ${measures} mesures sur ${combinations} combinaison(s) thème x palette x mode${partial ? ' (passage partiel)' : ''}, repos, survol forcé et focus, voile d'occupation (${acceptedBusyVeil.length} mesure(s) sous le ratio acceptée(s) par décision du 2026-09-18, voir acceptedBusyVeil), contraste non garanti (${acceptedContrastWaiver.length} mesure(s) sous le ratio acceptée(s) par décision du 2026-09-28${waived ? ` : ${waived}` : ''}, voir acceptedContrastWaiver), géométrie, 375 px ; registre vide (${registryPath}), ${shots.length} capture(s) dans ${shotDirectory}, aucune violation CSP, console sans erreur.`);
}
