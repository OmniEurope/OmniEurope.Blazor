// SelectOnFocus of the text-entry fields (OmniTextBox, OmniNumeric, OmniTextArea, OmniPassword): entering the field
// selects its whole content, so the next keystroke replaces it. Imported only by a field that asks for it.
//
// Every way of entering the field fires a focus event (a mouse press, a tap, Tab and Shift+Tab, a call to focus()), so
// the selection is made there with select(), which also works on type="number" where setSelectionRange does not.
// One case needs more: with a mouse the browser places the caret when the button is released, after the focus event,
// and drops the selection just made. A press that is about to give the field its focus is recognised on pointerdown
// (the field is not the active element yet); the mouseup that ends that same press is cancelled once, which keeps the
// caret where select() put it. Any press in a field that already has the focus, and any later one, is left alone: the
// caret is placed and the text can be selected by dragging, so a part of the value can still be edited. A field that
// already has the focus when it is attached (the parameter turned on in place) is left as it is: the next entry selects.
const attached = new WeakMap();

export function attach(field) {
    if (typeof field?.select !== 'function' || attached.has(field)) {
        return;
    }

    // The press in progress will focus the field (set on pointerdown), and the focus it gave (read on mouseup).
    let pressWillFocus = false;
    let swallowMouseUp = false;

    const onPointerDown = () => {
        pressWillFocus = document.activeElement !== field;
        swallowMouseUp = false;
    };
    const onFocus = () => {
        field.select();
        swallowMouseUp = pressWillFocus;
        pressWillFocus = false;
    };
    const onMouseUp = event => {
        if (swallowMouseUp) {
            event.preventDefault();
        }

        swallowMouseUp = false;
        pressWillFocus = false;
    };
    const reset = () => {
        pressWillFocus = false;
        swallowMouseUp = false;
    };

    const listeners = [
        ['pointerdown', onPointerDown],
        ['focus', onFocus],
        ['mouseup', onMouseUp],
        ['pointercancel', reset],
        ['blur', reset]
    ];
    for (const [type, handler] of listeners) {
        field.addEventListener(type, handler);
    }

    attached.set(field, () => {
        for (const [type, handler] of listeners) {
            field.removeEventListener(type, handler);
        }
    });
}

export function detach(field) {
    attached.get(field)?.();
    attached.delete(field);
}
