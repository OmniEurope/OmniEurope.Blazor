// Part of omni-html-editor.js: the text an extension proposes after the caret, shown dimmed and
// never part of the value.
import { editors } from './model.js';

const suggestionDelay = 1200;

// Once typing pauses with a caret at the end of at least five characters of a text, the extensions
// are asked for the rest; the answer only shows if the caret has not moved meanwhile.
export function requestSuggestion(state) {
    clearSuggestion(state);
    window.clearTimeout(state.suggestTimer);
    if (!state.suggest) {
        return;
    }

    const ticket = ++state.suggestTicket;
    state.suggestTimer = window.setTimeout(async () => {
        const selection = document.getSelection();
        if (!selection || selection.rangeCount === 0 || !selection.isCollapsed) {
            return;
        }

        const node = selection.anchorNode;
        const offset = selection.anchorOffset;
        if (!node || node.nodeType !== Node.TEXT_NODE || !state.surface.contains(node)) {
            return;
        }

        const before = node.data.slice(0, offset);
        if (before.length < 5) {
            return;
        }

        let text = null;
        try {
            text = await state.dotnet.invokeMethodAsync('OnSuggestionRequested', before.slice(-200));
        }
        catch {
            return;
        }

        const now = document.getSelection();
        if (!text || ticket !== state.suggestTicket || !editors.has(state.surface)
            || !now || now.rangeCount === 0 || !now.isCollapsed || now.anchorNode !== node || now.anchorOffset !== offset) {
            return;
        }

        const ghost = document.createElement('span');
        ghost.className = 'omni-html-editor__suggestion';
        ghost.setAttribute('contenteditable', 'false');
        ghost.setAttribute('aria-hidden', 'true');
        ghost.textContent = text;
        const at = document.createRange();
        at.setStart(node, offset);
        at.collapse(true);
        at.insertNode(ghost);
        const caret = document.createRange();
        caret.setStartBefore(ghost);
        caret.collapse(true);
        now.removeAllRanges();
        now.addRange(caret);
        state.suggestion = { element: ghost, text };
    }, suggestionDelay);
}

export function clearSuggestion(state) {
    if (!state?.suggestion) {
        return false;
    }

    const { element } = state.suggestion;
    state.suggestion = null;
    const parent = element.parentNode;
    element.remove();
    parent?.normalize();
    return true;
}

// Tab types the proposal where it stood, as text, and the caret goes after it; the caller then
// schedules the report of the change.
export function acceptSuggestion(state) {
    const { element, text } = state.suggestion;
    state.suggestion = null;
    const typed = document.createTextNode(text);
    element.replaceWith(typed);
    const caret = document.createRange();
    caret.setStartAfter(typed);
    caret.collapse(true);
    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(caret);
}
