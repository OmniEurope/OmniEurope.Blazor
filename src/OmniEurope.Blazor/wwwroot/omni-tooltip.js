// One delegated set of listeners for every tooltip on the page. A tooltip is a leaf that a grid can
// repeat hundreds of times, so attaching per instance would cost hundreds of interop calls and
// hundreds of listeners for a behaviour that only ever concerns the one under the pointer.

const GAP = 12;
const EDGE = 8;

let installed = false;
let tracked = null;

const clear = () => {
    if (!tracked) {
        return;
    }

    tracked.classList.remove('omni-tooltip--tracked', 'omni-tooltip--below');
    tracked.style.removeProperty('--omni-tooltip-x');
    tracked.style.removeProperty('--omni-tooltip-y');
    tracked.style.removeProperty('--omni-tooltip-arrow');
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

    const box = content.getBoundingClientRect();
    const half = box.width / 2;
    const left = Math.min(Math.max(x, half + EDGE), window.innerWidth - half - EDGE);
    // Above the pointer by default; flipped below it when there is no room left overhead.
    const below = y - GAP - box.height < EDGE;
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
    document.addEventListener('focusout', clear, { capture: true, passive: true });
    // A page that scrolls under a held pointer would leave the tooltip at coordinates that no longer
    // describe anything; dropping it is truer than dragging a stale box along.
    document.addEventListener('scroll', clear, { capture: true, passive: true });
}

// ---- title tooltips -----------------------------------------------------------------------------
// A host that opts in (OmniTitleTooltips) gets the package tooltip for every element carrying a title
// attribute, instead of the browser's own box, which cannot be styled. One floating element serves the
// whole page: it shows after a short delay at the pointer, never wider than 12rem, with a pointer
// (chevron) toward the cursor. The title moves to data-omni-title only while the element is hovered or
// focused, so the native tooltip never shows, and comes back as soon as it is left: the accessible name
// and every title selector stay intact.

const TITLE_DELAY = 450;
let titleInstalled = false;
let titleBox = null;
let titleTarget = null;
let titleTimer = 0;
let titlePointer = { x: 0, y: 0 };

const titleOf = element => element.getAttribute('title') || element.getAttribute('data-omni-title') || '';

const adoptTitle = element => {
    const title = element.getAttribute('title');
    if (title) {
        element.setAttribute('data-omni-title', title);
        element.removeAttribute('title');
    }
};

const restoreTitle = element => {
    const title = element?.getAttribute('data-omni-title');
    if (title && !element.hasAttribute('title')) {
        element.setAttribute('title', title);
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

const showTitle = (text, x, y) => {
    if (!titleBox) {
        titleBox = document.createElement('div');
        titleBox.className = 'omni-title-tooltip';
        titleBox.setAttribute('role', 'tooltip');
        document.body.appendChild(titleBox);
    }

    titleBox.textContent = text;
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
    const element = event.target instanceof Element ? event.target.closest('[title], [data-omni-title]') : null;
    if (element === titleTarget) {
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
            showTitle(text, titlePointer.x, titlePointer.y);
        }
    }, TITLE_DELAY);
};

const onTitleMove = event => {
    titlePointer = { x: event.clientX, y: event.clientY };
};

const onTitleFocus = event => {
    const element = event.target instanceof Element ? event.target.closest('[title], [data-omni-title]') : null;
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
    showTitle(text, box.left + box.width / 2, box.top);
};

export function installTitleTooltips() {
    if (titleInstalled) {
        return;
    }

    titleInstalled = true;
    document.addEventListener('pointerover', onTitleOver, { capture: true, passive: true });
    document.addEventListener('pointermove', onTitleMove, { capture: true, passive: true });
    document.addEventListener('pointerdown', hideTitle, { capture: true, passive: true });
    document.addEventListener('focusin', onTitleFocus, { capture: true, passive: true });
    document.addEventListener('focusout', hideTitle, { capture: true, passive: true });
    document.addEventListener('scroll', hideTitle, { capture: true, passive: true });
    document.addEventListener('keydown', event => { if (event.key === 'Escape') hideTitle(); }, { capture: true });
}
