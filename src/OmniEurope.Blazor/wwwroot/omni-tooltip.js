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

    tracked.classList.remove('omni-tooltip--tracked');
    tracked.style.removeProperty('--omni-tooltip-x');
    tracked.style.removeProperty('--omni-tooltip-y');
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

    // Figée, une infobulle déjà posée ne bouge plus : la replacer à chaque mouvement est exactement
    // ce que ce mode refuse.
    if (tracked === tooltip && tooltip.dataset.omniTooltipTrack === 'pinned') {
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
    const top = y - GAP - box.height < EDGE ? y + GAP + box.height : y - GAP;

    tooltip.style.setProperty('--omni-tooltip-x', `${left}px`);
    tooltip.style.setProperty('--omni-tooltip-y', `${top}px`);
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
