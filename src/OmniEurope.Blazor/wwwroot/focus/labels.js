// Part of omni-focus.js, which re-exports it: a click on the label of a native select opens its list.
// A label for a select only focuses it by default; a user who clicks the name of a drop-down expects
// the list, as a click on the field gives it. HTMLSelectElement.showPicker() needs the transient
// activation of a real user gesture, so it is called on the click itself, in this listener, never
// through a .NET round trip (the activation is gone by then).
//
// A component that draws a native select (OmniDropDown without Filterable) calls enableLabelPicker().
// One listener on the document then serves every label and every select, so nothing is attached or
// released per element, and calling it again changes nothing.

let enabled = false;

export function enableLabelPicker() {
    if (enabled) {
        return;
    }

    enabled = true;
    document.addEventListener('click', onClick);
}

// The control a label names: the browser resolves it (the for attribute, or the first labelable
// descendant); the for attribute is read when the property is missing.
function controlOf(label) {
    if (label.control) {
        return label.control;
    }

    return label.hasAttribute('for') ? document.getElementById(label.getAttribute('for')) : null;
}

// A select that cannot be changed: disabled, or marked read-only the way the package does it
// (a native select has no readonly of its own).
function isLocked(select) {
    return select.disabled === true
        || select.hasAttribute('disabled')
        || select.readOnly === true
        || select.hasAttribute('readonly')
        || select.getAttribute('aria-readonly') === 'true';
}

function onClick(event) {
    const target = event.target;
    if (event.defaultPrevented || !(target instanceof Element)) {
        return;
    }

    const label = target.closest('label');
    if (!label) {
        return;
    }

    // A click on a control placed inside the label is that control's own click, not the label's.
    const inner = target.closest('a, button, input, select, textarea');
    if (inner && label.contains(inner)) {
        return;
    }

    const control = controlOf(label);
    if (!control || control.localName !== 'select' || isLocked(control)) {
        return;
    }

    try {
        control.focus({ preventScroll: true });
        control.showPicker();
    } catch {
        // Not supported, a cross-origin frame, or no user activation: the native focus remains.
    }
}
