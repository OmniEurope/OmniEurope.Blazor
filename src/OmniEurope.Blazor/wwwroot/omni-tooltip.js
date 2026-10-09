// One delegated set of listeners for every tooltip on the page. A tooltip is a leaf that a grid can
// repeat hundreds of times, so attaching per instance would cost hundreds of interop calls and
// hundreds of listeners for a behaviour that only ever concerns the one under the pointer.

const GAP = 12;
const EDGE = 8;

let installed = false;
let tracked = null;

// The top a tooltip may reach: the window's, or the bottom of a sticky application header, which is
// drawn above the page and would cover the part of a tooltip placed under it. A tooltip inside that
// header is not covered by it and keeps the window's edge.
const ceilingFor = tooltip => {
    let ceiling = EDGE;
    for (const header of document.querySelectorAll('.omni-header--sticky')) {
        if (!header.contains(tooltip)) {
            const bottom = header.getBoundingClientRect().bottom;
            if (bottom > 0) {
                ceiling = Math.max(ceiling, bottom + EDGE);
            }
        }
    }

    return ceiling;
};

// An unfolded long tooltip grows after it was placed: it is pushed back inside the window instead
// of spilling over its top or bottom edge.
const keepInside = content => {
    const tooltip = content.closest('.omni-tooltip');
    if (!tooltip || tooltip !== tracked) {
        return;
    }

    const box = content.getBoundingClientRect();
    const y = Number.parseFloat(tooltip.style.getPropertyValue('--omni-tooltip-y'));
    if (!Number.isFinite(y)) {
        return;
    }

    const ceiling = ceilingFor(tooltip);
    const shift = box.top < ceiling ? ceiling - box.top : box.bottom > window.innerHeight - EDGE ? window.innerHeight - EDGE - box.bottom : 0;
    if (shift !== 0) {
        tooltip.style.setProperty('--omni-tooltip-y', `${y + shift}px`);
    }
};
const resizeObserver = typeof ResizeObserver === 'function'
    ? new ResizeObserver(entries => entries.forEach(entry => keepInside(entry.target)))
    : null;

const clear = () => {
    if (!tracked) {
        return;
    }

    tracked.classList.remove('omni-tooltip--tracked', 'omni-tooltip--below');
    tracked.style.removeProperty('--omni-tooltip-x');
    tracked.style.removeProperty('--omni-tooltip-y');
    tracked.style.removeProperty('--omni-tooltip-arrow');
    const content = tracked.querySelector('.omni-tooltip__content');
    if (content) {
        resizeObserver?.unobserve(content);
    }
    tracked = null;
};

// The tooltip is measured before it is placed, so a long one can be kept inside the viewport rather
// than sliding off the right edge or under the top of the window. Reading the content while it is
// still hidden is fine: visibility, unlike display, leaves the box laid out.
const place = (tooltip, x, y) => {
    const content = tooltip.querySelector('.omni-tooltip__content');
    if (!content) {
        return;
    }

    // Figée, une infobulle déjà affichée ne bouge plus : la replacer à chaque mouvement est exactement
    // ce que ce mode refuse. Pendant son délai d'apparition, elle suit encore le pointeur, pour
    // paraître là où il s'est posé et non là où il est entré.
    if (tracked === tooltip && tooltip.dataset.omniTooltipTrack === 'pinned'
        && getComputedStyle(content).visibility === 'visible') {
        return;
    }

    if (tracked && tracked !== tooltip) {
        clear();
    }

    tooltip.classList.add('omni-tooltip--tracked');
    tracked = tooltip;
    resizeObserver?.observe(content);

    const box = content.getBoundingClientRect();
    const half = box.width / 2;
    const left = Math.min(Math.max(x, half + EDGE), window.innerWidth - half - EDGE);
    // Above the pointer by default; flipped below it when there is no room left overhead.
    // Under a sticky header, overhead ends at its bottom edge.
    const below = y - GAP - box.height < ceilingFor(tooltip);
    const top = below ? y + GAP + box.height : y - GAP;

    tooltip.style.setProperty('--omni-tooltip-x', `${left}px`);
    tooltip.style.setProperty('--omni-tooltip-y', `${top}px`);
    // The arrow keeps pointing at the pointer when the box is held inside the viewport, and turns
    // upward when the box is flipped below it.
    tooltip.style.setProperty('--omni-tooltip-arrow', `${x - left + half}px`);
    tooltip.classList.toggle('omni-tooltip--below', below);
};

const onPointerMove = event => {
    const tooltip = event.target instanceof Element ? event.target.closest('.omni-tooltip') : null;
    if (!tooltip) {
        clear();
        return;
    }

    // Over the box of a long tooltip (only such a box takes the pointer): it holds still so its
    // "Show more" action can be reached.
    if (tooltip === tracked && event.target.closest('.omni-tooltip__content')) {
        return;
    }

    place(tooltip, event.clientX, event.clientY);
};

// Keyboard users get the same fixed placement, anchored on the trigger they just reached rather than
// on a pointer they are not using. Without this the tooltip would keep the stylesheet's anchored
// position while its neighbours moved to the pointer, which is the inconsistency this replaces.
const onFocusIn = event => {
    const tooltip = event.target instanceof Element ? event.target.closest('.omni-tooltip') : null;
    if (!tooltip) {
        clear();
        return;
    }

    // The "Show more" action of a long tooltip is inside the box already placed: focusing it keeps
    // the box where it is.
    if (tooltip === tracked && event.target.closest('.omni-tooltip__content')) {
        return;
    }

    const trigger = tooltip.querySelector('.omni-tooltip__trigger') ?? tooltip;
    const box = trigger.getBoundingClientRect();
    place(tooltip, box.left + (box.width / 2), box.top);
};

export function install() {
    if (installed) {
        return;
    }

    installed = true;
    // Capture: a trigger that stops the event on its own element must not strand a tooltip in the
    // tracked state, since nothing else would ever clear it.
    document.addEventListener('pointermove', onPointerMove, { capture: true, passive: true });
    document.addEventListener('focusin', onFocusIn, { capture: true, passive: true });
    document.addEventListener('focusout', event => {
        const next = event.relatedTarget instanceof Element ? event.relatedTarget.closest('.omni-tooltip') : null;
        if (!next || next !== tracked) {
            clear();
        }
    }, { capture: true, passive: true });
    // A page that scrolls under a held pointer would leave the tooltip at coordinates that no longer
    // describe anything; dropping it is truer than dragging a stale box along.
    // A pointer press on "Show more" must not move the focus into the tooltip: the focus would keep
    // it open (focus-within) after the pointer has left. The click still fires; the keyboard still
    // reaches the action with Tab.
    document.addEventListener('mousedown', event => {
        if (event.target instanceof Element && event.target.closest('.omni-tooltip__more')) {
            event.preventDefault();
        }
    }, { capture: true });
    // Scrolling the unfolded text of a long tooltip is not a page scroll: that one stays open.
    document.addEventListener('scroll', event => {
        if (!(event.target instanceof Element && event.target.closest('.omni-tooltip__content'))) {
            clear();
        }
    }, { capture: true, passive: true });
}

// ---- title tooltips -----------------------------------------------------------------------------
// A host that opts in (OmniTitleTooltips) gets the package tooltip for every element carrying a title
// attribute, instead of the browser's own box, which cannot be styled. One floating element serves the
// whole page: it shows after a short delay at the pointer, never wider than 12rem, with a pointer
// (chevron) toward the cursor. The title moves to data-omni-title only while the element is hovered or
// focused, so the native tooltip never shows, and comes back as soon as it is left: every title
// selector stays intact, and an element the title alone named keeps that name as aria-label meanwhile.
//
// The same box serves the package's own tips, whether or not the host replaced its title tooltips: the
// whole text of a data grid cell cut by its ellipsis, only while it is cut (the cell is narrower than
// its text), and the text of an element carrying data-omni-tip (the toolbar of the HTML editor, with
// a command's description on its second line). A grid or an editor turns them on
// (installPackageTooltips). The cell keeps its full text in the page for screen readers, and an
// element with a tip keeps its accessible name.

const TITLE_DELAY = 450;
// How many OmniTitleTooltips are placed: with the grids below, they share one set of listeners.
let titleInstalls = 0;
let titleBox = null;
let titleTarget = null;
let titleTimer = 0;
let titlePointer = { x: 0, y: 0 };
// How many grids and editors are on the page: cut cells and data-omni-tip get the tooltip while one is.
let packageInstalls = 0;
const CUT_CELL = '.omni-data-grid__cell--text';

const isCut = element => element.scrollWidth > element.clientWidth + 1;

// A chart point names itself with an SVG <title> child, which the browser shows as its own box. It is
// replaced like a title attribute: the node is set aside while the point is hovered or focused, and the
// same node goes back afterwards, so the renderer that owns it still finds it. The titles of the SVG
// ancestors (the chart's own name) are set aside with it, or the browser would show the nearest of them.
const svgTitleOf = element => element instanceof SVGElement
    ? [...element.children].find(child => child.localName === 'title') ?? null
    : null;

const svgTitled = target => {
    for (let element = target; element instanceof SVGElement; element = element.parentElement) {
        if (svgTitleOf(element) || element.omniTitleNode) {
            return element;
        }
    }
    return null;
};

// What a pointer or the focus reaches: an element with a title once a host replaced title tooltips,
// else an element with a package tip, else a grid cell whose text is cut.
const tipTarget = target => {
    if (!(target instanceof Element)) {
        return null;
    }

    const titled = titleInstalls > 0 ? svgTitled(target) ?? target.closest('[title], [data-omni-title]') : null;
    if (titled) {
        return titled;
    }

    if (packageInstalls === 0) {
        return null;
    }

    const tipped = target.closest('[data-omni-tip]');
    if (tipped) {
        return tipped;
    }

    const cell = target.closest(CUT_CELL);
    return cell && isCut(cell) ? cell : null;
};

const titleOf = element => element.getAttribute('title') || element.getAttribute('data-omni-title')
    || element.getAttribute('data-omni-tip') || (element.matches(CUT_CELL) ? element.textContent.trim() : '');

// Whether the title is the only name of the element: no aria-label, no aria-labelledby, no label of a
// form field and no text of its own (an icon-only button named by its title alone).
const namedByTitleOnly = element => !element.hasAttribute('aria-label') && !element.hasAttribute('aria-labelledby')
    && !(element.labels?.length > 0) && element.textContent.trim() === '';

// While the title is away, a title that was the element's only name lends it to aria-label
// (data-omni-title-named says so), so a focused icon-only button keeps its accessible name.
const adoptTitle = element => {
    const node = svgTitleOf(element);
    if (node) {
        const text = node.textContent.trim();
        if (!element.hasAttribute('aria-label') && !element.hasAttribute('aria-labelledby')) {
            element.setAttribute('aria-label', text);
            element.setAttribute('data-omni-title-named', '');
        }
        element.setAttribute('data-omni-title', text);
        const aside = [];
        for (let owner = element; owner instanceof SVGElement; owner = owner.parentElement) {
            const title = svgTitleOf(owner);
            if (title) {
                aside.push({ owner, title });
                title.remove();
            }
        }
        element.omniTitleNode = aside;
        return;
    }
    const title = element.getAttribute('title');
    if (title) {
        if (namedByTitleOnly(element)) {
            element.setAttribute('aria-label', title);
            element.setAttribute('data-omni-title-named', '');
        }
        element.setAttribute('data-omni-title', title);
        element.removeAttribute('title');
    }
};

const restoreTitle = element => {
    const title = element?.getAttribute('data-omni-title');
    if (element?.omniTitleNode) {
        for (const { owner, title } of element.omniTitleNode) {
            owner.prepend(title);
        }
        delete element.omniTitleNode;
    } else if (title && !element.hasAttribute('title')) {
        element.setAttribute('title', title);
    }
    if (element?.hasAttribute('data-omni-title-named')) {
        element.removeAttribute('aria-label');
        element.removeAttribute('data-omni-title-named');
    }
    element?.removeAttribute('data-omni-title');
};

const hideTitle = () => {
    window.clearTimeout(titleTimer);
    titleTimer = 0;
    restoreTitle(titleTarget);
    titleTarget = null;
    titleBox?.classList.remove('omni-title-tooltip--visible');
};

// The box lives at the end of the body, outside any theme scope: it takes the font of the element it
// describes, so a themed page does not get the browser's default serif in its tooltips.
const showTitle = (text, x, y, source) => {
    if (!titleBox) {
        titleBox = document.createElement('div');
        titleBox.className = 'omni-title-tooltip';
        titleBox.setAttribute('role', 'tooltip');
        document.body.appendChild(titleBox);
    }

    titleBox.textContent = text;
    titleBox.style.fontFamily = source ? getComputedStyle(source).fontFamily : '';
    titleBox.classList.remove('omni-title-tooltip--below');
    const box = titleBox.getBoundingClientRect();
    const half = box.width / 2;
    const left = Math.min(Math.max(x, half + EDGE), window.innerWidth - half - EDGE);
    const below = y - GAP - box.height < EDGE;
    const top = below ? y + GAP : y - GAP - box.height;
    titleBox.classList.toggle('omni-title-tooltip--below', below);
    titleBox.style.setProperty('--omni-title-tooltip-x', `${left}px`);
    titleBox.style.setProperty('--omni-title-tooltip-y', `${top}px`);
    // The chevron points at the pointer even when the box was pushed back inside the window.
    titleBox.style.setProperty('--omni-title-tooltip-arrow', `${x - left + half}px`);
    titleBox.classList.add('omni-title-tooltip--visible');
};

const onTitleOver = event => {
    const element = tipTarget(event.target);
    if (element === titleTarget) {
        return;
    }

    // A tooltip the focus opened stays while its element keeps the focus: the pointer reaching a place
    // without a title (often the page moving under a still pointer) gave the focused element its title
    // back and closed the tooltip of a keyboard user who never touched the mouse.
    if (!element && titleTarget && titleTarget === document.activeElement) {
        return;
    }

    hideTitle();
    if (!element || element.closest('.omni-tooltip')) {
        return;
    }

    adoptTitle(element);
    const text = titleOf(element);
    if (!text) {
        return;
    }

    titleTarget = element;
    titleTimer = window.setTimeout(() => {
        if (titleTarget === element && element.isConnected) {
            showTitle(text, titlePointer.x, titlePointer.y, element);
        }
    }, TITLE_DELAY);
};

const onTitleMove = event => {
    titlePointer = { x: event.clientX, y: event.clientY };
};

const onTitleFocus = event => {
    const element = tipTarget(event.target);
    hideTitle();
    if (!element || !event.target.matches(':focus-visible')) {
        return;
    }

    adoptTitle(element);
    const text = titleOf(element);
    if (!text) {
        return;
    }

    titleTarget = element;
    const box = element.getBoundingClientRect();
    showTitle(text, box.left + box.width / 2, box.top, element);
};

// A scroll closes a tooltip the pointer opened, which no longer sits under the pointer. One the focus
// opened follows its element instead: Tab scrolls the page to show the element it reaches, and that
// scroll closed the tooltip it had just opened and gave the element its title back.
const onTitleScroll = () => {
    if (titleTarget && titleTarget === document.activeElement && titleTarget.isConnected) {
        const box = titleTarget.getBoundingClientRect();
        showTitle(titleOf(titleTarget), box.left + box.width / 2, box.top, titleTarget);
        return;
    }

    hideTitle();
};

const onTitleKey = event => {
    if (event.key === 'Escape') {
        hideTitle();
    }
};

const listen = add => {
    const method = add ? 'addEventListener' : 'removeEventListener';
    document[method]('pointerover', onTitleOver, { capture: true, passive: true });
    document[method]('pointermove', onTitleMove, { capture: true, passive: true });
    document[method]('pointerdown', hideTitle, { capture: true, passive: true });
    document[method]('focusin', onTitleFocus, { capture: true, passive: true });
    document[method]('focusout', hideTitle, { capture: true, passive: true });
    document[method]('scroll', onTitleScroll, { capture: true, passive: true });
    document[method]('keydown', onTitleKey, { capture: true });
};

// The first user (a title host or a grid) installs the listeners.
const acquire = () => {
    if (titleInstalls + packageInstalls === 1) {
        listen(true);
    }
};

// The last one removes them, gives the hovered element its title back and removes the floating box, so
// the browser's tooltips return; while another remains, only the tooltip shown now is closed.
const release = () => {
    hideTitle();
    if (titleInstalls + packageInstalls === 0) {
        listen(false);
        titleBox?.remove();
        titleBox = null;
    }
};

export function installTitleTooltips() {
    titleInstalls++;
    acquire();
}

// Called by each OmniTitleTooltips that goes away.
export function uninstallTitleTooltips() {
    if (titleInstalls === 0) {
        return;
    }

    titleInstalls--;
    release();
}

// Called by each data grid and HTML editor placed on the page, then when it goes away.
export function installPackageTooltips() {
    packageInstalls++;
    acquire();
}

export function uninstallPackageTooltips() {
    if (packageInstalls === 0) {
        return;
    }

    packageInstalls--;
    release();
}
