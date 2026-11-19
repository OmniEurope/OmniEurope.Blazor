// Part of omni-focus.js, which re-exports it: keys the page answers the same way wherever they are
// pressed. They are on as soon as the focus module is loaded (by a select bar, a popover, a dialog);
// a component that loads it for nothing else calls enablePageKeys(). One listener on the document then
// serves every instance, so nothing is attached or released per element.
//
// - A radio group marked data-omni-roving (OmniSelectBar, the modes of OmniAppMenu and
//   OmniAppearanceWindow, the views of OmniScheduler): the arrows
//   move to the previous or next option that is not disabled, Home and End to the first or the last, and
//   pick it, as a native radio group does. The component keeps the single tab stop (tabindex 0 on the
//   chosen option).
// - An accordion marked data-omni-accordion (OmniAccordion): on one of its toggles (data-omni-accordion-toggle),
//   Up and Down move the focus to the previous or next toggle of that accordion, wrapping, Home and End to
//   the first or the last, as the WAI-ARIA accordion pattern has it. Nothing is opened by the move.
// - A row of a data grid marked data-omni-row-nav (OmniDataGrid rows the host made interactive), when the key
//   is pressed on the row itself: Up and Down move the focus to the previous or next such row of the same
//   body, Home and End to the first or the last, without wrapping and without scrolling the page. A key
//   pressed in a control inside the row is left to that control. The grid keeps the single tab stop (tabindex
//   0 on the row that last took the focus) from the focus the move leaves on the row.
// - An element marked data-omni-mirror-arrows (the day grid of the date pickers, the radio cards of an
//   OmniSelectableCardGroup), whose arrows .NET handles in reading order: in a right-to-left page the two
//   horizontal keys are swapped before anything else sees them (see mirrorArrows).
// - An element marked data-omni-key-button that is not a native button (an entry of a chart legend drawn
//   in the SVG): Enter and Space activate it as a click would, Space without scrolling the page.

const steps = new Map([['ArrowLeft', -1], ['ArrowUp', -1], ['ArrowRight', 1], ['ArrowDown', 1]]);
let enabled = false;

export function enablePageKeys() {
    if (enabled) {
        return;
    }

    enabled = true;
    // Capture phase: the swap has to come before the listener of Blazor and the ones of the picker.
    document.addEventListener('keydown', mirrorArrows, true);
    document.addEventListener('keydown', onKeyDown);
}

enablePageKeys();

// The copies made by mirrorArrows, so that they are not swapped a second time.
const copies = new WeakSet();

// .NET counts the horizontal arrows in reading order (ArrowLeft is the previous day or card) and does not
// know the direction of the page. In a right-to-left page the previous one is drawn on the right, so the
// original event stops here and a copy with the other horizontal key goes to the same target: that copy is
// what the handler of Blazor and the other listeners receive.
function mirrorArrows(event) {
    if (copies.has(event) || event.defaultPrevented || !(event.target instanceof Element)
        || (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight')) {
        return;
    }

    const marked = event.target.closest('[data-omni-mirror-arrows]');
    if (!marked || getComputedStyle(marked).direction !== 'rtl') {
        return;
    }

    const key = event.key === 'ArrowLeft' ? 'ArrowRight' : 'ArrowLeft';
    const copy = new KeyboardEvent('keydown', {
        key,
        code: key,
        location: event.location,
        repeat: event.repeat,
        shiftKey: event.shiftKey,
        ctrlKey: event.ctrlKey,
        altKey: event.altKey,
        metaKey: event.metaKey,
        bubbles: true,
        cancelable: true,
        composed: true
    });
    copies.add(copy);
    event.preventDefault();
    event.stopImmediatePropagation();
    event.target.dispatchEvent(copy);
}

function onKeyDown(event) {
    if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || !(event.target instanceof Element)) {
        return;
    }

    if (!moveInRadioGroup(event) && !moveInAccordion(event) && !moveInGridRows(event)) {
        pressKeyButton(event);
    }
}

function moveInRadioGroup(event) {
    if (!steps.has(event.key) && event.key !== 'Home' && event.key !== 'End') {
        return false;
    }

    const current = event.target.closest('[role="radio"]');
    const group = current?.closest('[data-omni-roving]');
    if (!group) {
        return false;
    }

    const radios = Array.from(group.querySelectorAll('[role="radio"]'))
        .filter(radio => !radio.disabled && radio.getAttribute('aria-disabled') !== 'true');
    const index = radios.indexOf(current);
    if (index < 0) {
        return false;
    }

    event.preventDefault();
    const next = event.key === 'Home' ? 0
        : event.key === 'End' ? radios.length - 1
        : (index + stepOf(event.key, group) + radios.length) % radios.length;
    radios[next].focus();
    if (next !== index) {
        radios[next].click();
    }

    return true;
}

// The step of an arrow key in a group: left and right follow what the reader sees, so they swap in a
// right-to-left group; up and down never do.
function stepOf(key, group) {
    const step = steps.get(key);
    const sideways = key === 'ArrowLeft' || key === 'ArrowRight';
    return sideways && getComputedStyle(group).direction === 'rtl' ? -step : step;
}

function moveInAccordion(event) {
    if (event.key !== 'ArrowUp' && event.key !== 'ArrowDown' && event.key !== 'Home' && event.key !== 'End') {
        return false;
    }

    const current = event.target.closest('[data-omni-accordion-toggle]');
    const group = current?.closest('[data-omni-accordion]');
    if (!group) {
        return false;
    }

    // A nested accordion keeps to its own toggles.
    const toggles = Array.from(group.querySelectorAll('[data-omni-accordion-toggle]'))
        .filter(toggle => toggle.closest('[data-omni-accordion]') === group);
    const index = toggles.indexOf(current);
    if (index < 0) {
        return false;
    }

    event.preventDefault();
    const next = event.key === 'Home' ? 0
        : event.key === 'End' ? toggles.length - 1
        : (index + (event.key === 'ArrowDown' ? 1 : -1) + toggles.length) % toggles.length;
    toggles[next].focus();
    return true;
}

// The rows are the siblings of the pressed one: a body holds the rows it renders, a nested grid has its own body.
function moveInGridRows(event) {
    const keys = ['ArrowUp', 'ArrowDown', 'Home', 'End'];
    const row = event.target;
    if (event.shiftKey || !keys.includes(event.key) || !row.hasAttribute('data-omni-row-nav')) {
        return false;
    }

    const rows = Array.from(row.parentElement.children).filter(sibling => sibling.hasAttribute('data-omni-row-nav'));
    const index = rows.indexOf(row);
    event.preventDefault();
    const next = event.key === 'Home' ? 0
        : event.key === 'End' ? rows.length - 1
        : Math.max(0, Math.min(rows.length - 1, index + (event.key === 'ArrowDown' ? 1 : -1)));
    rows[next].focus();
    return true;
}

function pressKeyButton(event) {
    if (event.key !== 'Enter' && event.key !== ' ') {
        return;
    }

    const target = event.target;
    if (!target.hasAttribute('data-omni-key-button')) {
        return;
    }

    event.preventDefault();
    target.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
}
