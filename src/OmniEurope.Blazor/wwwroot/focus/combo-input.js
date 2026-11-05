// Part of omni-focus.js, which re-exports attachComboInput and detachComboInput: the text field of an
// autocomplete. Two behaviours live here because only the browser can do them:
//
// - Escape never empties the field. A search field (type="search") empties itself on Escape in Chromium
//   browsers; the autocomplete takes Escape to close its list and otherwise leaves the text alone. With
//   the list open the key is consumed (the default action is cancelled, so nothing else reacts to it as
//   a clearing); with the list closed the key still reaches the page (a dialog or a sidebar closes on
//   it), and the clearing the browser performs is undone before .NET hears of it.
// - With replace set (ReplaceTextOnType), the text is replaced rather than extended, as in a word
//   processor's combo box: entering the field selects its text, and after a suggestion is picked the
//   first text inserted (typed, pasted, dictated, composed) replaces the picked text. Moving the caret,
//   a press in the field or leaving it ends that state: the text typed afterwards is joined to it.
const attached = new Map();

const isInsert = inputType => typeof inputType === 'string' && inputType.startsWith('insert');

export function attachComboInput(root, input, replace) {
    detachComboInput(input);
    if (!(root instanceof HTMLElement) || !(input instanceof HTMLInputElement)) {
        return;
    }

    // A pick leaves the text to replace on the next insertion; a press that focuses the field must not
    // let its mouseup put the caret back and drop the selection made on focus.
    let picked = false;
    let focusedByMouse = false;
    let justFocused = false;
    // The text the field held when Escape was pressed with the list closed, until the browser's own
    // clearing (a synchronous input event after the keydown) has been undone or has not come.
    let escapedText = null;
    const listeners = [];
    const listen = (target, type, handler, capture = false) => {
        target.addEventListener(type, handler, capture);
        listeners.push(() => target.removeEventListener(type, handler, capture));
    };

    listen(input, 'keydown', event => {
        if (event.key === 'Escape') {
            if (input.getAttribute('aria-expanded') === 'true') {
                event.preventDefault();
            } else {
                escapedText = input.value;
                setTimeout(() => { escapedText = null; }, 0);
            }

            return;
        }

        if (!replace) {
            return;
        }

        // Enter on the highlighted suggestion picks it; moving the caret says the text is to be edited.
        if (event.key === 'Enter' && input.getAttribute('aria-activedescendant')) {
            picked = true;
        } else if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
            picked = false;
        }
    });

    // Runs before .NET's own listener, which sits higher in the tree: the clearing is undone first and
    // never reaches it.
    listen(input, 'input', event => {
        if (escapedText !== null && escapedText !== '' && input.value === '') {
            event.stopImmediatePropagation();
            input.value = escapedText;
        }

        escapedText = null;
    }, true);

    if (!replace) {
        attached.set(input, () => listeners.forEach(remove => remove()));
        return;
    }

    listen(input, 'mousedown', () => {
        picked = false;
        justFocused = false;
        focusedByMouse = document.activeElement !== input;
    });
    listen(input, 'focus', () => {
        input.select();
        justFocused = focusedByMouse;
        focusedByMouse = false;
    });
    listen(input, 'mouseup', event => {
        if (justFocused) {
            event.preventDefault();
        }

        justFocused = false;
    });
    listen(input, 'blur', () => {
        picked = false;
        justFocused = false;
        focusedByMouse = false;
    });
    // The suggestions are plain options inside the root: a click on one picks it and keeps the focus in the field.
    listen(root, 'click', event => {
        if (event.target instanceof Element && event.target.closest('[role="option"]')) {
            picked = true;
        }
    }, true);
    listen(input, 'beforeinput', event => {
        if (picked && isInsert(event.inputType) && input.selectionStart === input.selectionEnd) {
            input.select();
        }

        picked = false;
    });
    attached.set(input, () => listeners.forEach(remove => remove()));
}

export function detachComboInput(input) {
    attached.get(input)?.();
    attached.delete(input);
}
