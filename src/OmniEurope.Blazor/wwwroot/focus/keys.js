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
// - An element marked data-omni-key-button that is not a native button (an entry of a chart legend drawn
//   in the SVG): Enter and Space activate it as a click would, Space without scrolling the page.

const steps = new Map([['ArrowLeft', -1], ['ArrowUp', -1], ['ArrowRight', 1], ['ArrowDown', 1]]);
let enabled = false;

export function enablePageKeys() {
    if (enabled) {
        return;
    }

    enabled = true;
    document.addEventListener('keydown', onKeyDown);
}

enablePageKeys();

function onKeyDown(event) {
    if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || !(event.target instanceof Element)) {
        return;
    }

    if (!moveInRadioGroup(event) && !moveInAccordion(event)) {
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
