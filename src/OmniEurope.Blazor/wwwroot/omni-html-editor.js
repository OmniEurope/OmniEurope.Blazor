// Visual (WYSIWYG) surface of OmniHtmlEditor.
//
// Blazor renders the surface empty and never diffs into it: this module owns its children. The
// browser's editing commands are used where they produce plain elements (b, i, u, lists, headings,
// links); alignment and text size are applied here as classes, because the commands that do them
// natively write a style attribute. HTML is inserted as nodes (insertHtmlAt), and a list asked again
// inside itself is undone by hand (unlist): insertHTML and that list command stage styled elements a
// strict style-src refuses. normalise() removes any style attribute the editing engine still leaves
// behind (a merge of two paragraphs can create one), so the strict CSP holds.
//
// Pasted and dropped content never reaches the document raw: it goes through .NET, where the same
// HtmlSanitizer allow-list as the value applies, and only the sanitised result is inserted. The
// history also lives in .NET, so Ctrl+Z undoes a command and a burst of typing alike.
//
// This entry module, the one the component imports (OmniModules.HtmlEditor), keeps the lifecycle,
// the events, the selection reports and every export .NET calls. The document model and its
// normalisation live in ./html-editor/model.js, the formatting commands in commands.js, the table
// editing in tables.js, the text an extension proposes after the caret in suggestions.js and the
// passages a proofreader flags in proofreading.js.
import {
    editors, allowedClasses, normalise, elementOf, caretFromPoint, caretOffset, placeCaret, joinInstead, insertHtmlAt
} from './html-editor/model.js';
import { describe, apply, readClipboard } from './html-editor/commands.js';
import { moveBetweenCells } from './html-editor/tables.js';
import { requestSuggestion, clearSuggestion, acceptSuggestion } from './html-editor/suggestions.js';
import { requestProofreading, proofreadAgain, clearProofreading, issueAt, replaceIssue, ignoreIssue } from './html-editor/proofreading.js';
// The toolbar held to a number of rows, and the package tooltips of its controls.
export { fitToolbar, unfit as unfitToolbar } from './html-editor/toolbar.js';
export { installPackageTooltips, uninstallPackageTooltips } from './omni-tooltip.js';

const inputDelay = 250;
const selectionDelay = 120;

export function mount(surface, dotnet, html, options) {
    if (!surface || editors.has(surface)) {
        return;
    }

    const state = {
        surface, dotnet, timer: 0, range: null, key: '', sent: null, listeners: [], classes: allowedClasses, policy: false,
        selection: false, selectionTimer: 0, selectionKey: '', shortcuts: new Map(), inline: [], menu: false, activated: null,
        suggest: false, suggestion: null, suggestTimer: 0, suggestTicket: 0,
        proofread: false, proofreadTimer: 0, proofreadTicket: 0, proofreadCache: new Map(), proofreadIgnored: new Set(), proofreadIssues: [], menuIssue: null
    };
    editors.set(surface, state);
    configure(surface, options);
    surface.innerHTML = html ?? '';
    normalise(surface);
    state.sent = surface.innerHTML;
    listen(state, surface, 'focusin', () => prepareDocument());
    listen(state, surface, 'input', () => {
        schedule(state);
        requestSuggestion(state);
        requestProofreading(state);
    });
    listen(state, surface, 'mousedown', () => clearSuggestion(state));
    listen(state, surface, 'paste', event => paste(state, event));
    listen(state, surface, 'drop', event => drop(state, event));
    listen(state, surface, 'keydown', event => keydown(state, event));
    listen(state, surface, 'beforeinput', event => beforeInput(state, event));
    listen(state, surface, 'focusout', () => flush(state));
    listen(state, document, 'selectionchange', () => selectionChanged(state));
    listen(state, surface, 'click', event => activate(state, event));
    listen(state, surface, 'contextmenu', event => contextMenu(state, event));
    requestProofreading(state, true);
}

export function configure(surface, options) {
    const rows = Number(options?.rows);
    if (surface && Number.isFinite(rows) && rows > 0) {
        surface.style.setProperty('--omni-html-editor-rows', String(Math.min(Math.round(rows), 80)));
    }

    // The sanitiser policy of the host, mirrored so that tidying keeps what .NET would keep: the
    // classes it allows (null for any class), and spans that carry an allowed attribute.
    const state = editors.get(surface);
    if (state) {
        state.selection = options?.selection === true;
        state.policy = options?.policy === true;
        state.classes = !state.policy
            ? allowedClasses
            : Array.isArray(options?.classes) ? new Set(options.classes) : null;
        // What the extensions of the host added: their shortcuts (combination to position), the
        // selectors of their inline elements, and whether they have a context menu.
        state.shortcuts = new Map((Array.isArray(options?.shortcuts) ? options.shortcuts : []).map((keys, index) => [keys, index]));
        state.inline = Array.isArray(options?.inline) ? options.inline : [];
        state.menu = options?.menu === true;
        state.suggest = options?.suggest === true;
        // Whether an extension brings a proofreader: its passages are underlined, or the underlining goes.
        const proofread = options?.proofread === true;
        if (state.proofread !== proofread) {
            state.proofread = proofread;
            if (proofread) {
                requestProofreading(state, true);
            }
            else {
                clearProofreading(state);
            }
        }
    }
}

export function read(surface) {
    const state = editors.get(surface);
    if (!state) {
        return null;
    }

    window.clearTimeout(state.timer);
    state.timer = 0;
    tidy(surface);
    // A surface untouched since it was drawn or last reported is not a change: the browser's own
    // serialisation of the value must not come back as an edit the user never made.
    const html = surface.innerHTML;
    if (html === state.sent) {
        return null;
    }

    state.sent = html;
    return html;
}

export function setHtml(surface, html) {
    const state = editors.get(surface);
    if (!state) {
        return;
    }

    window.clearTimeout(state.timer);
    state.timer = 0;
    const focused = surface.contains(document.activeElement);
    const offset = focused ? caretOffset(surface) : -1;
    surface.innerHTML = html ?? '';
    normalise(surface);
    state.sent = surface.innerHTML;
    state.range = null;
    if (offset >= 0) {
        placeCaret(surface, offset);
    }

    report(state, true);
    requestProofreading(state, true);
}

export async function exec(surface, action, argument) {
    const state = editors.get(surface);
    if (!state) {
        return null;
    }

    // Paste reads the clipboard (the browser may ask the user first), then goes through the same
    // sanitiser in .NET as a paste typed with Ctrl+V. A refusal or an empty clipboard changes nothing.
    let clean = null;
    if (action === 'paste') {
        remember(state);
        const clipboard = await readClipboard();
        if (!clipboard || !editors.has(surface)) {
            return null;
        }

        clean = await state.dotnet.invokeMethodAsync('SanitizePaste', clipboard.html, clipboard.text);
        if (!clean || !editors.has(surface)) {
            return null;
        }
    }

    prepareDocument();
    const range = restore(state);
    if (clean !== null) {
        insertHtmlAt(surface, range, clean);
    }
    else {
        // Only cut and copy wait: for the clipboard, where the browser refuses its own command.
        await apply(surface, range, action, argument ?? '');
        if (!editors.has(surface)) {
            return null;
        }
    }

    return settle(state);
}

export function insertHtml(surface, html) {
    return exec(surface, 'inserthtml', html);
}

// Replaces the innermost element around the selection (or the one kept while a dialog was open)
// that matches the selector, with HTML .NET has already sanitised. Null when there is none.
export function replaceClosest(surface, selector, html) {
    const state = editors.get(surface);
    if (!state) {
        return null;
    }

    prepareDocument();
    const range = restore(state);
    let target = null;
    try {
        target = startElement(range)?.closest(selector);
    }
    catch {
        return null;
    }

    if (!target || target === surface || !surface.contains(target)) {
        return null;
    }

    replaceNode(target, html);
    return settle(state);
}

// Replaces, or removes with an empty HTML, the inline element the last click activated.
export function replaceActivated(surface, html) {
    const state = editors.get(surface);
    const target = state?.activated;
    if (!state || !target || !surface.contains(target)) {
        return null;
    }

    state.activated = null;
    surface.focus({ preventScroll: true });
    replaceNode(target, html);
    return settle(state);
}

// A correction of a proofreader replaces the passage the menu was opened on, reported as typing is.
export function proofreadReplace(surface, text) {
    const state = editors.get(surface);
    if (state && replaceIssue(state, text ?? '')) {
        surface.focus({ preventScroll: true });
        schedule(state);
        requestProofreading(state, true);
    }
}

// "Ignore": the passage the menu was opened on is no longer underlined while its block keeps its text.
export function proofreadIgnore(surface) {
    const state = editors.get(surface);
    if (state) {
        ignoreIssue(state);
    }
}

// The proofreaders' word lists changed ("Ignore all", "Add to dictionary"): every block is checked again.
export function proofreadRecheck(surface) {
    const state = editors.get(surface);
    if (state) {
        state.menuIssue = null;
        proofreadAgain(state);
    }
}

// Puts back the selection the context menu opened on: closing the menu returns the focus to the
// surface, which may move the caret, and its command must act where the menu was asked for.
export function restoreMenuSelection(surface) {
    const state = editors.get(surface);
    const range = state?.menuRange;
    if (!state || !range || !surface.contains(range.commonAncestorContainer)) {
        return;
    }

    state.menuRange = null;
    surface.focus({ preventScroll: true });
    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
    state.range = range.cloneRange();
}

// Replaces the text of the inline element the last click activated, keeping the element itself.
export function setActivatedText(surface, text) {
    const state = editors.get(surface);
    const target = state?.activated;
    if (!state || !target || !surface.contains(target)) {
        return null;
    }

    target.textContent = text ?? '';
    return settle(state);
}

// The content before and after the caret (the start of the kept selection), each as balanced HTML:
// the ranges are cloned from the document, so an element the caret sits in is closed on one side and
// reopened on the other. Nothing changes in the surface.
export function aroundCaret(surface) {
    const state = editors.get(surface);
    if (!state) {
        return null;
    }

    remember(state);
    const caret = state.range && surface.contains(state.range.commonAncestorContainer) ? state.range : null;
    const serialize = fragment => {
        const holder = document.createElement('div');
        holder.appendChild(fragment);
        return holder.innerHTML;
    };
    const before = document.createRange();
    before.selectNodeContents(surface);
    const after = before.cloneRange();
    if (caret) {
        before.setEnd(caret.startContainer, caret.startOffset);
        after.setStart(caret.startContainer, caret.startOffset);
    } else {
        before.collapse(false);
        after.collapse(false);
    }

    return JSON.stringify({ before: serialize(before.cloneContents()), after: serialize(after.cloneContents()) });
}

// The text of the selection, or of the one kept while the focus was elsewhere (a dialog).
export function selectedText(surface) {
    const state = editors.get(surface);
    if (!state) {
        return '';
    }

    remember(state);
    return state.range && surface.contains(state.range.commonAncestorContainer) ? state.range.toString() : '';
}

// The new nodes take the place of the old one and the caret goes after them, so typing continues
// where the replaced element was.
function replaceNode(target, html) {
    const template = document.createElement('template');
    template.innerHTML = html ?? '';
    const last = template.content.lastChild;
    target.replaceWith(template.content);
    const selection = document.getSelection();
    if (last && last.isConnected) {
        const after = document.createRange();
        after.setStartAfter(last);
        after.collapse(true);
        selection.removeAllRanges();
        selection.addRange(after);
    }
}

// The tail of every change made here rather than typed: tidied, reported as sent, selection kept.
// A command changed the document, often without an input event (the classes, the tables, the inserted
// nodes): the underlined passages are read again, or they would keep ranges the command detached.
function settle(state) {
    tidy(state.surface);
    window.clearTimeout(state.timer);
    state.timer = 0;
    state.sent = state.surface.innerHTML;
    remember(state);
    report(state, true);
    requestProofreading(state, true);
    return state.sent;
}

// A click on an inline element of an extension: the innermost element matching one of their
// selectors is kept as the activated one and described to .NET, with its text.
function activate(state, event) {
    if (state.inline.length === 0 || !(event.target instanceof Element)) {
        return;
    }

    for (let node = event.target; node && node !== state.surface && state.surface.contains(node); node = node.parentElement) {
        const index = state.inline.findIndex(selector => matchesSafely(node, selector));
        if (index >= 0) {
            event.preventDefault();
            flush(state);
            state.activated = node;
            state.dotnet.invokeMethodAsync('OnElementActivated', index, JSON.stringify(describeNode(node)), node.textContent ?? '');
            return;
        }
    }
}

// The extensions' menu replaces the browser's. From the keyboard (the context-menu key, Shift+F10)
// the event has no pointer position, so the menu opens at the caret.
function contextMenu(state, event) {
    // A flagged passage under the pointer opens the menu of its corrections, even without extension commands.
    const issue = issueAt(state, event);
    if (!state.menu && !issue) {
        return;
    }

    event.preventDefault();
    flush(state);
    // A right-click on an inline element selects it, so the menu's commands act on it: the caret
    // never enters an element that is not editable.
    const element = event.target instanceof Element ? inlineElementAt(state, event.target) : null;
    if (element) {
        const range = document.createRange();
        range.selectNode(element);
        const selection = document.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
    }

    remember(state);
    state.menuRange = state.range?.cloneRange() ?? null;
    let x = event.clientX;
    let y = event.clientY;
    if (!x && !y && state.range) {
        const box = state.range.getBoundingClientRect();
        x = box.left;
        y = box.bottom;
    }

    // The selection goes with the request, so the entries are enabled for where the menu opens
    // rather than for where the caret was a moment ago.
    state.dotnet.invokeMethodAsync('OnContextMenu', x, y, describeSelection(state.surface), issue ? JSON.stringify(issue) : null);
}

// The innermost element from the target up that matches the selector of an inline element.
function inlineElementAt(state, target) {
    for (let node = target; node && node !== state.surface && state.surface.contains(node); node = node.parentElement) {
        if (state.inline.some(selector => matchesSafely(node, selector))) {
            return node;
        }
    }

    return null;
}

function matchesSafely(node, selector) {
    try {
        return node.matches(selector);
    }
    catch {
        return false;
    }
}

// The element a range starts in; a range that selects exactly one element (a right-clicked note)
// starts in that element rather than in its parent.
function startElement(range) {
    if (range.startContainer === range.endContainer && range.endOffset - range.startOffset === 1) {
        const selected = range.startContainer.childNodes[range.startOffset];
        if (selected?.nodeType === Node.ELEMENT_NODE) {
            return selected;
        }
    }

    return elementOf(range.startContainer);
}

// "ctrl+alt+shift+key", as OmniHtmlEditorShortcut normalises it. Letters and digits are read from the
// physical key, so a layout or Alt that changes the character still matches.
function combination(event) {
    let key = event.key.toLowerCase();
    if (/^Key[A-Z]$/.test(event.code)) {
        key = event.code.slice(3).toLowerCase();
    }
    else if (/^Digit[0-9]$/.test(event.code)) {
        key = event.code.slice(5);
    }

    const parts = [];
    if (event.ctrlKey || event.metaKey) {
        parts.push('ctrl');
    }

    if (event.altKey) {
        parts.push('alt');
    }

    if (event.shiftKey) {
        parts.push('shift');
    }

    parts.push(key);
    return parts.join('+');
}

export function dispose(surface) {
    const state = editors.get(surface);
    if (!state) {
        return;
    }

    window.clearTimeout(state.timer);
    window.clearTimeout(state.selectionTimer);
    window.clearTimeout(state.suggestTimer);
    clearSuggestion(state);
    clearProofreading(state);
    for (const [target, type, handler] of state.listeners) {
        target.removeEventListener(type, handler);
    }

    editors.delete(surface);
}

function listen(state, target, type, handler) {
    target.addEventListener(type, handler);
    state.listeners.push([target, type, handler]);
}

// Both settings are document-wide. They are set again on every focus because another editor on
// the page may have changed them: paragraphs rather than div elements on Enter, and elements
// rather than style attributes for the formatting commands.
function prepareDocument() {
    document.execCommand('defaultParagraphSeparator', false, 'p');
    document.execCommand('styleWithCSS', false, false);
}

function schedule(state) {
    window.clearTimeout(state.timer);
    state.timer = window.setTimeout(() => send(state), inputDelay);
}

function send(state) {
    state.timer = 0;
    tidy(state.surface);
    const html = state.surface.innerHTML;
    if (html === state.sent) {
        return;
    }

    state.sent = html;
    state.dotnet.invokeMethodAsync('OnVisualInput', html);
}

function flush(state) {
    clearSuggestion(state);
    if (state.timer) {
        window.clearTimeout(state.timer);
        send(state);
    }
}

async function paste(state, event) {
    const data = event.clipboardData;
    if (!data) {
        return;
    }

    event.preventDefault();
    await insertTransfer(state, data.getData('text/html'), data.getData('text/plain'));
}

async function drop(state, event) {
    const data = event.dataTransfer;
    if (!data) {
        return;
    }

    event.preventDefault();
    const html = data.getData('text/html');
    const text = data.getData('text/plain');
    if (!html && !text) {
        return;
    }

    const point = caretFromPoint(event.clientX, event.clientY);
    if (point && state.surface.contains(point.startContainer)) {
        state.range = point;
    }

    await insertTransfer(state, html, text);
}

async function insertTransfer(state, html, text) {
    remember(state);
    const clean = await state.dotnet.invokeMethodAsync('SanitizePaste', html ?? '', text ?? '');
    if (!clean || !editors.has(state.surface)) {
        return;
    }

    prepareDocument();
    const range = restore(state);
    insertHtmlAt(state.surface, range, clean);

    tidy(state.surface);
    schedule(state);
    requestProofreading(state);
}

function keydown(state, event) {
    if (state.suggestion) {
        if (event.key === 'Tab' && !event.ctrlKey && !event.metaKey && !event.altKey && !event.shiftKey) {
            event.preventDefault();
            acceptSuggestion(state);
            schedule(state);
            return;
        }

        const dismissed = clearSuggestion(state);
        if (dismissed && event.key === 'Escape') {
            event.preventDefault();
            return;
        }
    }

    const command = event.ctrlKey || event.metaKey;
    const key = event.key.toLowerCase();
    if (command && !event.altKey && (key === 'z' || key === 'y')) {
        event.preventDefault();
        flush(state);
        state.dotnet.invokeMethodAsync('OnHistoryShortcut', key === 'y' || event.shiftKey);
        return;
    }

    if (command && !event.altKey && !event.shiftKey && key === 'k') {
        event.preventDefault();
        remember(state);
        state.dotnet.invokeMethodAsync('OnLinkShortcut');
        return;
    }

    const shortcut = state.shortcuts.get(combination(event));
    if (shortcut !== undefined) {
        event.preventDefault();
        flush(state);
        remember(state);
        state.dotnet.invokeMethodAsync('OnShortcut', shortcut);
        return;
    }

    if (event.key === 'Tab' && !command && !event.altKey && moveBetweenCells(state.surface, event.shiftKey)) {
        event.preventDefault();
    }
}

// The browser's own history knows nothing of the changes made by the history in .NET, so its entry
// points (the context menu, a touch gesture) are routed there too.
function beforeInput(state, event) {
    if (event.inputType === 'historyUndo' || event.inputType === 'historyRedo') {
        event.preventDefault();
        flush(state);
        state.dotnet.invokeMethodAsync('OnHistoryShortcut', event.inputType === 'historyRedo');
        return;
    }

    if (joinInstead(state.surface, event)) {
        event.preventDefault();
        tidy(state.surface);
        schedule(state);
    }
}

function selectionChanged(state) {
    if (remember(state)) {
        report(state, false);
    }
}

function remember(state) {
    const selection = document.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return false;
    }

    const range = selection.getRangeAt(0);
    if (!state.surface.contains(range.commonAncestorContainer)) {
        return false;
    }

    state.range = range.cloneRange();
    return true;
}

// The live selection wins when it is inside the surface; the remembered one covers the moments the
// focus is elsewhere (the link field, a list of the toolbar), since selectionchange arrives late.
function restore(state) {
    const { surface } = state;
    remember(state);
    surface.focus({ preventScroll: true });
    const selection = document.getSelection();
    let range = state.range;
    if (!range || !surface.contains(range.commonAncestorContainer)) {
        range = document.createRange();
        range.selectNodeContents(surface);
        range.collapse(false);
    }

    selection.removeAllRanges();
    selection.addRange(range);
    return range;
}

function report(state, force) {
    scheduleSelection(state);
    const key = describe(state.surface);
    if (!force && key === state.key) {
        return;
    }

    state.key = key;
    state.dotnet.invokeMethodAsync('OnVisualState', key);
}

// Where the selection is, for the host: sent only when the host listens, once the caret has been
// still for a moment, and only when the answer differs from the last one sent.
function scheduleSelection(state) {
    if (!state.selection) {
        return;
    }

    window.clearTimeout(state.selectionTimer);
    state.selectionTimer = window.setTimeout(() => sendSelection(state), selectionDelay);
}

function sendSelection(state) {
    state.selectionTimer = 0;
    if (!editors.has(state.surface)) {
        return;
    }

    const payload = describeSelection(state.surface);
    if (payload === null || payload === state.selectionKey) {
        return;
    }

    state.selectionKey = payload;
    state.dotnet.invokeMethodAsync('OnSelectionChanged', payload);
}

// {"collapsed":bool,"ancestors":[{"tag","classes","data"}]}: the elements holding the start of the
// selection, innermost first, up to the surface, which is left out; null outside the surface.
function describeSelection(surface) {
    const selection = document.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return null;
    }

    const range = selection.getRangeAt(0);
    if (!surface.contains(range.commonAncestorContainer)) {
        return null;
    }

    const ancestors = [];
    for (let node = startElement(range); node && node !== surface && surface.contains(node); node = node.parentElement) {
        ancestors.push(describeNode(node));
    }

    return JSON.stringify({ collapsed: range.collapsed, ancestors });
}

// One element as .NET reads a selection node: tag, classes, data-* attributes, and a cell's spans.
function describeNode(node) {
    const data = {};
    for (const attribute of node.attributes) {
        if (attribute.name.startsWith('data-')) {
            data[attribute.name] = attribute.value;
        }
    }

    const described = { tag: node.tagName.toLowerCase(), classes: [...node.classList], data };
    if (node.matches('td,th')) {
        described.colspan = node.colSpan || 1;
        described.rowspan = node.rowSpan || 1;
    }

    return described;
}

// normalise() keeping the caret where it was, counted in characters, when it had to move a block.
function tidy(surface) {
    const owner = editors.get(surface);
    if (owner) {
        clearSuggestion(owner);
    }

    const offset = surface.contains(document.activeElement) ? caretOffset(surface) : -1;
    if (normalise(surface) && offset >= 0) {
        placeCaret(surface, offset);
    }
}
