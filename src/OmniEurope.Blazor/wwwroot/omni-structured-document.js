// The script of OmniDocumentEditor: the plain-text editing of the runs of a paragraph.
//
// A run is a span the stylesheet makes editable in place (contenteditable="plaintext-only", else "true" in a browser
// that does not know that value, see attach). .NET renders the text of a run once and never again until the document
// changes, so the text the reader types is never overwritten by a render. This script only reads it back: on every
// input and when the run loses the focus, it tells .NET the text of the run, by its identifier (data-omni-run), if it
// differs from what .NET was last told.
//
// A run holds plain text on one line: Enter and any line break are refused, and a paste or a drop is inserted as the
// plain text of the clipboard with its line breaks turned into spaces, whatever formatting it carried. Everything is
// delegated from the root of the editor, so the runs of a document need no listener of their own. Nothing here writes
// a style, an attribute or a node.
import { notify } from './shared/notify.js';

const RUN = '[data-omni-run]';
const LINE_BREAKS = /\s*[\r\n]+\s*/g;
const BREAK_INPUTS = new Set(['insertParagraph', 'insertLineBreak']);
const TEXT_INPUTS = new Set(['insertFromPaste', 'insertFromDrop']);
const states = new WeakMap();

const runOf = target => target?.closest?.(RUN) ?? null;
const textOf = run => run.textContent ?? '';

// Whether the browser edits a span as plain text; the value is dropped by a browser that does not know it.
function supportsPlainTextOnly() {
    const probe = document.createElement('span');
    probe.contentEditable = 'plaintext-only';
    return probe.contentEditable === 'plaintext-only';
}

// Wires the editor and says whether runs can use contenteditable="plaintext-only".
export function attach(root, dotNet) {
    const told = new WeakMap();
    const tell = run => {
        const text = textOf(run);
        if (told.get(run) !== text) {
            told.set(run, text);
            notify(dotNet, 'OnRunText', run.getAttribute('data-omni-run'), text);
        }
    };

    const onFocusIn = event => {
        const run = runOf(event.target);
        if (run) {
            told.set(run, textOf(run));
        }
    };
    const onInput = event => {
        const run = runOf(event.target);
        if (run) {
            tell(run);
        }
    };
    const onFocusOut = event => {
        const run = runOf(event.target);
        if (run) {
            tell(run);
        }
    };
    const onKeyDown = event => {
        if (event.key === 'Enter' && runOf(event.target)) {
            event.preventDefault();
        }
    };
    const onBeforeInput = event => {
        if (!runOf(event.target)) {
            return;
        }

        if (BREAK_INPUTS.has(event.inputType)) {
            event.preventDefault();
        } else if (TEXT_INPUTS.has(event.inputType)) {
            event.preventDefault();
            const text = (event.dataTransfer?.getData('text/plain') ?? '').replace(LINE_BREAKS, ' ');
            if (text) {
                document.execCommand('insertText', false, text);
            }
        }
    };

    const listeners = [['focusin', onFocusIn], ['input', onInput], ['focusout', onFocusOut], ['keydown', onKeyDown], ['beforeinput', onBeforeInput]];
    for (const [type, listener] of listeners) {
        root.addEventListener(type, listener);
    }

    states.set(root, listeners);
    return supportsPlainTextOnly();
}

export function detach(root) {
    for (const [type, listener] of states.get(root) ?? []) {
        root.removeEventListener(type, listener);
    }

    states.delete(root);
}
