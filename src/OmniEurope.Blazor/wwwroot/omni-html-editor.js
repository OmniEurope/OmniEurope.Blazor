// Visual (WYSIWYG) surface of OmniHtmlEditor.
//
// Blazor renders the surface empty and never diffs into it: this module owns its children. The
// browser's editing commands are used where they produce plain elements (b, i, u, lists, headings,
// links); alignment and text size are applied here as classes, because the commands that do them
// natively write a style attribute. normalise() removes any style attribute the editing engine
// still leaves behind (a merge of two paragraphs can create one), so the strict CSP holds.
//
// Pasted and dropped content never reaches the document raw: it goes through .NET, where the same
// HtmlSanitizer allow-list as the value applies, and only the sanitised result is inserted. The
// history also lives in .NET, so Ctrl+Z undoes a command and a burst of typing alike.
const editors = new WeakMap();
const inputDelay = 250;
const selectionDelay = 120;
const suggestionDelay = 1200;
const blockSelector = 'p,h1,h2,h3,h4,h5,h6,li,blockquote,pre,td,th,div';
const alignClasses = ['omni-align-left', 'omni-align-center', 'omni-align-right', 'omni-align-justify'];
const sizeClasses = {
    small: 'omni-font-size-small',
    normal: 'omni-font-size-normal',
    large: 'omni-font-size-large',
    xlarge: 'omni-font-size-xlarge'
};
const allowedClasses = new Set([...alignClasses, ...Object.values(sizeClasses)]);
// Underline and strikethrough are read from the elements rather than from queryCommandState, which
// reports the underline every link is drawn with.
const marks = [
    ['bold', 'bold'],
    ['italic', 'italic'],
    ['subscript', 'subscript'],
    ['superscript', 'superscript']
];

// The elements a paragraph cannot hold: text next to them is wrapped in its own paragraph, and one
// found inside a paragraph is lifted out of it. The sectioning elements only ever reach the surface
// when a host policy allows them (an aside holding a note, a figure).
const hostBlocks = 'ul,ol,table,blockquote,pre,h1,h2,h3,h4,h5,h6,div,p,aside,section,article,figure,header,footer,nav,address,dl';

export function mount(surface, dotnet, html, options) {
    if (!surface || editors.has(surface)) {
        return;
    }

    const state = {
        surface, dotnet, timer: 0, range: null, key: '', sent: null, listeners: [], classes: allowedClasses, policy: false,
        selection: false, selectionTimer: 0, selectionKey: '', shortcuts: new Map(), inline: [], menu: false, activated: null,
        suggest: false, suggestion: null, suggestTimer: 0, suggestTicket: 0
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
        if (!insertBlocks(surface, range, clean)) {
            document.execCommand('insertHTML', false, clean);
        }
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

// ── Text proposed by an extension after the caret: shown dimmed, never part of the value ──

// Once typing pauses with a caret at the end of at least five characters of a text, the extensions
// are asked for the rest; the answer only shows if the caret has not moved meanwhile.
function requestSuggestion(state) {
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

function clearSuggestion(state) {
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

// Tab types the proposal where it stood, as text, and the caret goes after it.
function acceptSuggestion(state) {
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
    schedule(state);
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
function settle(state) {
    tidy(state.surface);
    window.clearTimeout(state.timer);
    state.timer = 0;
    state.sent = state.surface.innerHTML;
    remember(state);
    report(state, true);
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
    if (!state.menu) {
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
    state.dotnet.invokeMethodAsync('OnContextMenu', x, y, describeSelection(state.surface));
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
    if (!insertBlocks(state.surface, range, clean)) {
        document.execCommand('insertHTML', false, clean);
    }

    tidy(state.surface);
    schedule(state);
}

function keydown(state, event) {
    if (state.suggestion) {
        if (event.key === 'Tab' && !event.ctrlKey && !event.metaKey && !event.altKey && !event.shiftKey) {
            event.preventDefault();
            acceptSuggestion(state);
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

// When two blocks are joined, by a deletion at their boundary or across them, the browser keeps the
// look of the moved text with a styled span; under a strict style-src that span is refused and
// reported. The common joins are therefore done here, as plain moves of nodes; a join inside a
// list or a table is left to the browser.
const joinable = 'p,h1,h2,h3,h4,h5,h6,blockquote,pre,div';

function joinInstead(surface, event) {
    const selection = document.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return false;
    }

    const range = selection.getRangeAt(0);
    const type = event.inputType;
    const deleting = type.startsWith('delete');
    const typing = type === 'insertText' || type === 'insertReplacementText';
    if (!deleting && !typing) {
        return false;
    }

    const startBlock = topBlock(surface, range.startContainer);
    const endBlock = topBlock(surface, range.endContainer);
    if (!range.collapsed) {
        if (!startBlock || !endBlock || startBlock === endBlock) {
            return false;
        }

        range.deleteContents();
        join(startBlock, endBlock);
        if (typing && event.data) {
            const text = document.createTextNode(event.data);
            const caret = selection.getRangeAt(0);
            caret.insertNode(text);
            caret.setStartAfter(text);
            caret.collapse(true);
            selection.removeAllRanges();
            selection.addRange(caret);
        }

        return true;
    }

    if (!startBlock || (type !== 'deleteContentBackward' && type !== 'deleteContentForward')) {
        return false;
    }

    const backwards = type === 'deleteContentBackward';
    const edge = document.createRange();
    edge.selectNodeContents(startBlock);
    if (backwards) {
        edge.setEnd(range.startContainer, range.startOffset);
    }
    else {
        edge.setStart(range.startContainer, range.startOffset);
    }

    if (edge.toString() !== '' || edge.cloneContents().querySelector('br,img,hr')) {
        return false;
    }

    const other = backwards ? startBlock.previousElementSibling : startBlock.nextElementSibling;
    if (!other || !other.matches(joinable)) {
        return false;
    }

    return backwards ? join(other, startBlock) : join(startBlock, other);
}

function topBlock(surface, node) {
    const block = elementOf(node)?.closest(joinable);
    return block && block.parentElement === surface ? block : null;
}

// Appends the content of second to first, removes second and puts the caret at the seam.
function join(first, second) {
    const selection = document.getSelection();
    if (!first.textContent && !first.querySelector('img,hr')) {
        first.remove();
        const start = document.createRange();
        start.selectNodeContents(second);
        start.collapse(true);
        selection.removeAllRanges();
        selection.addRange(start);
        return true;
    }

    for (const trailing of [...first.childNodes].reverse()) {
        if (trailing.nodeName === 'BR' || (trailing.nodeType === Node.TEXT_NODE && trailing.data === '')) {
            trailing.remove();
            continue;
        }

        break;
    }

    const seam = document.createRange();
    seam.selectNodeContents(first);
    seam.collapse(false);
    const moved = [...second.childNodes].filter(node => node.nodeName !== 'BR' || second.childNodes.length > 1);
    first.append(...moved);
    second.remove();
    selection.removeAllRanges();
    selection.addRange(seam);
    return true;
}

// Pasted blocks placed between the two halves of the paragraph holding the caret, rather than
// merged into it by insertHTML, which would keep their look with styled spans. Inline content, or
// a caret inside a list or a table, is left to insertHTML.
function insertBlocks(surface, range, html) {
    const template = document.createElement('template');
    template.innerHTML = html;
    const nodes = [...template.content.childNodes];
    if (!nodes.some(node => node.nodeType === Node.ELEMENT_NODE && node.matches('p,h1,h2,h3,h4,h5,h6,blockquote,pre,ul,ol,table,div,hr'))) {
        return false;
    }

    const startBlock = topBlock(surface, range.startContainer);
    const endBlock = topBlock(surface, range.endContainer);
    if (!startBlock || !endBlock) {
        return false;
    }

    range.deleteContents();
    if (startBlock !== endBlock) {
        join(startBlock, endBlock);
    }

    const caret = document.getSelection().getRangeAt(0);
    const tail = document.createRange();
    tail.setStart(caret.startContainer, caret.startOffset);
    tail.setEnd(startBlock, startBlock.childNodes.length);
    const rest = startBlock.cloneNode(false);
    rest.appendChild(tail.extractContents());
    const last = nodes[nodes.length - 1];
    startBlock.after(...nodes, rest);
    if (!startBlock.textContent && !startBlock.querySelector('img,hr')) {
        startBlock.remove();
    }

    if (!rest.textContent && !rest.querySelector('img,hr')) {
        rest.remove();
    }

    const after = document.createRange();
    if (last.nodeType === Node.ELEMENT_NODE) {
        after.selectNodeContents(last);
        after.collapse(false);
    }
    else {
        after.setStartAfter(last);
        after.collapse(true);
    }
    document.getSelection().removeAllRanges();
    document.getSelection().addRange(after);
    return true;
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

// "marks|block|align|size": the pressed toggles, the block tag, the alignment and the text size at
// the caret, compared as one string so .NET is only called when one of them changes.
function describe(surface) {
    const selection = document.getSelection();
    if (!selection || selection.rangeCount === 0 || !surface.contains(selection.getRangeAt(0).commonAncestorContainer)) {
        return '|p|left|normal';
    }

    const node = elementOf(selection.getRangeAt(0).startContainer);
    const within = selector => {
        const found = node?.closest(selector);
        return found && found !== surface && surface.contains(found) ? found : null;
    };
    const pressed = marks.filter(([command]) => document.queryCommandState(command)).map(([, name]) => name);
    if (within('u')) {
        pressed.push('underline');
    }

    if (within('s,strike,del')) {
        pressed.push('strikethrough');
    }

    if (within('mark')) {
        pressed.push('highlight');
    }

    const pre = within('pre');
    if (within('code') && !pre) {
        pressed.push('inlinecode');
    }

    const list = within('ul,ol');
    if (list) {
        pressed.push(list.tagName === 'UL' ? 'bulletlist' : 'numberedlist');
    }

    if (within('blockquote')) {
        pressed.push('quote');
    }

    if (pre) {
        pressed.push('codeblock');
    }

    if (within('a[href]')) {
        pressed.push('link');
    }

    // Not a toggle: tells .NET the caret is in a table cell, which enables the table commands.
    if (within('td,th')) {
        pressed.push('intable');
    }

    const block = within('h1,h2,h3,h4,p,pre,blockquote,li,td,th,div');
    const tag = block && /^h[1-4]$/i.test(block.tagName) ? block.tagName.toLowerCase() : 'p';
    const aligned = within(alignClasses.map(name => `.${name}`).join(','));
    const align = aligned ? alignClasses.find(name => aligned.classList.contains(name)).slice('omni-align-'.length) : 'left';
    const sized = within(Object.values(sizeClasses).map(name => `.${name}`).join(','));
    const size = sized ? Object.keys(sizeClasses).find(name => sized.classList.contains(sizeClasses[name])) : 'normal';
    return `${pressed.join(' ')}|${tag}|${align}|${size}`;
}

function apply(surface, range, action, argument) {
    const within = selector => {
        const found = elementOf(range.startContainer)?.closest(selector);
        return found && found !== surface && surface.contains(found) ? found : null;
    };

    switch (action) {
        case 'bold':
        case 'italic':
        case 'underline':
        case 'subscript':
        case 'superscript':
            document.execCommand(action);
            break;
        case 'strikethrough':
            document.execCommand('strikeThrough');
            break;
        case 'highlight':
            toggleHighlight(range, within);
            break;
        case 'inlinecode':
            toggleInlineCode(range, within);
            break;
        case 'blockformat':
            document.execCommand('formatBlock', false, `<${/^h[1-4]$/.test(argument) ? argument : 'p'}>`);
            break;
        case 'fontsize':
            applySize(surface, argument in sizeClasses ? argument : 'normal');
            break;
        case 'bulletlist':
            document.execCommand('insertUnorderedList');
            break;
        case 'numberedlist':
            document.execCommand('insertOrderedList');
            break;
        case 'indent':
            if (within('li')) {
                document.execCommand('indent');
            }
            else {
                document.execCommand('formatBlock', false, '<blockquote>');
            }
            break;
        case 'outdent':
            if (within('li')) {
                document.execCommand('outdent');
            }
            else {
                unwrapQuote(within('blockquote'));
            }
            break;
        case 'quote':
            if (within('blockquote')) {
                unwrapQuote(within('blockquote'));
            }
            else {
                document.execCommand('formatBlock', false, '<blockquote>');
            }
            break;
        case 'codeblock':
            document.execCommand('formatBlock', false, within('pre') ? '<p>' : '<pre>');
            break;
        case 'link':
            applyLink(range, within, argument);
            break;
        case 'unlink':
            if (within('a')) {
                unwrap(within('a'));
            }
            else {
                document.execCommand('unlink');
            }
            break;
        case 'alignleft':
        case 'aligncenter':
        case 'alignright':
        case 'alignjustify':
            applyAlignment(surface, action.slice('align'.length));
            break;
        case 'inserttable':
            document.execCommand('insertHTML', false, tableHtml(argument));
            break;
        case 'clearformatting':
            clearFormatting(surface);
            break;
        case 'inserthtml':
            if (argument) {
                document.execCommand('insertHTML', false, argument);
            }
            break;
        case 'inserttext':
            if (argument) {
                document.execCommand('insertText', false, argument);
            }
            break;
        case 'changecase':
            changeCase(surface, range, argument);
            break;
        case 'cut':
        case 'copy':
            return copySelection(range, action === 'cut');
        case 'insertparagraph':
            document.execCommand('insertParagraph');
            break;
        case 'addrowabove':
        case 'addrowbelow':
        case 'deleterow':
        case 'addcolumnbefore':
        case 'addcolumnafter':
        case 'deletecolumn':
        case 'mergecellright':
        case 'mergecelldown':
        case 'splitcell':
        case 'setcellspan': {
            const cell = within('td,th');
            if (cell) {
                editTable(cell, action, argument);
            }
            break;
        }
        default:
            break;
    }
}

// Rewrites the case of the selected text in place, text node by text node, so every bold, link or
// note around it stays. Title case capitalises a letter that follows a space, including across nodes.
// A mark around the selection, or none: the caret in a mark unwraps it, keeping its text.
function toggleHighlight(range, within) {
    const existing = within('mark');
    if (existing) {
        existing.replaceWith(...existing.childNodes);
        return;
    }

    if (range.collapsed) {
        return;
    }

    const mark = document.createElement('mark');
    mark.appendChild(range.extractContents());
    range.insertNode(mark);
    range.selectNodeContents(mark);
    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
}

function changeCase(surface, range, mode) {
    if (range.collapsed || !['upper', 'lower', 'title'].includes(mode)) {
        return;
    }

    const root = range.commonAncestorContainer;
    const nodes = [];
    if (root.nodeType === Node.TEXT_NODE) {
        nodes.push(root);
    }
    else {
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
        while (walker.nextNode()) {
            if (range.intersectsNode(walker.currentNode) && surface.contains(walker.currentNode)) {
                nodes.push(walker.currentNode);
            }
        }
    }

    if (nodes.length === 0) {
        return;
    }

    const first = nodes[0];
    const last = nodes[nodes.length - 1];
    const firstStart = first === range.startContainer ? range.startOffset : 0;
    let wordStart = firstStart === 0 || /\s/.test(first.data[firstStart - 1]);
    let lastEnd = 0;
    for (const node of nodes) {
        const start = node === range.startContainer ? range.startOffset : 0;
        const end = node === range.endContainer ? range.endOffset : node.data.length;
        let text = node.data.slice(start, end);
        if (mode === 'upper') {
            text = text.toUpperCase();
        }
        else if (mode === 'lower') {
            text = text.toLowerCase();
        }
        else {
            text = [...text].map(character => {
                const changed = wordStart ? character.toUpperCase() : character.toLowerCase();
                wordStart = /\s/.test(character);
                return changed;
            }).join('');
        }

        node.data = node.data.slice(0, start) + text + node.data.slice(end);
        if (node === last) {
            lastEnd = start + text.length;
        }
    }

    // The selection keeps covering the rewritten text, whose length a case change may alter (ß).
    const selected = document.createRange();
    selected.setStart(first, Math.min(firstStart, first.data.length));
    selected.setEnd(last, Math.min(lastEnd, last.data.length));
    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(selected);
}

function toggleInlineCode(range, within) {
    const code = within('code');
    if (code && !within('pre')) {
        unwrap(code);
        return;
    }

    if (range.collapsed) {
        return;
    }

    // Built by hand: insertHTML turns a code element that ends a paragraph into a bare span.
    const element = document.createElement('code');
    element.textContent = range.toString();
    range.deleteContents();
    range.insertNode(element);
    const selection = document.getSelection();
    const inside = document.createRange();
    inside.selectNodeContents(element);
    selection.removeAllRanges();
    selection.addRange(inside);
}

function applyLink(range, within, url) {
    if (!url) {
        return;
    }

    const anchor = within('a');
    if (anchor && range.collapsed) {
        anchor.setAttribute('href', url);
    }
    else if (range.collapsed) {
        document.execCommand('insertHTML', false, `<a href="${escapeAttribute(url)}">${escapeHtml(url)}</a>`);
    }
    else {
        document.execCommand('createLink', false, url);
    }
}

// A quote made by formatBlock holds its text directly; unwrapping it would leave bare text in the
// surface, so such a quote becomes a paragraph instead.
function unwrapQuote(quote) {
    if (!quote) {
        return;
    }

    if ([...quote.children].some(child => child.matches(blockSelector))) {
        unwrap(quote);
        return;
    }

    const paragraph = document.createElement('p');
    while (quote.firstChild) {
        paragraph.appendChild(quote.firstChild);
    }

    quote.replaceWith(paragraph);
}

// fontSize with styleWithCSS off writes font elements, which are then turned into classed spans:
// the one route to sized text that neither leaves a font element nor writes a style attribute.
function applySize(surface, size) {
    document.execCommand('fontSize', false, '7');
    for (const font of surface.querySelectorAll('font[size="7"]')) {
        for (const inner of font.querySelectorAll('span')) {
            inner.classList.remove(...Object.values(sizeClasses));
        }

        // Resizing exactly what an earlier size covered changes that size rather than nesting a
        // second one inside it.
        const outer = font.parentElement;
        if (outer?.tagName === 'SPAN' && outer.childNodes.length === 1 && surface.contains(outer)) {
            unwrap(font);
            if (size === 'normal') {
                outer.classList.remove(...Object.values(sizeClasses));
            }
            else {
                outer.classList.remove(...Object.values(sizeClasses));
                outer.classList.add(sizeClasses[size]);
            }

            continue;
        }

        const inherited = font.parentElement?.closest(Object.values(sizeClasses).map(name => `.${name}`).join(','));
        if (size === 'normal' && !(inherited && surface.contains(inherited))) {
            unwrap(font);
            continue;
        }

        const span = document.createElement('span');
        span.className = sizeClasses[size];
        while (font.firstChild) {
            span.appendChild(font.firstChild);
        }

        font.replaceWith(span);
    }
}

function applyAlignment(surface, direction) {
    let blocks = blocksOfSelection(surface);
    if (blocks.length === 0) {
        document.execCommand('formatBlock', false, '<p>');
        blocks = blocksOfSelection(surface);
    }

    for (const block of blocks) {
        block.classList.remove(...alignClasses);
        if (direction !== 'left') {
            block.classList.add(`omni-align-${direction}`);
        }
    }
}

function clearFormatting(surface) {
    document.execCommand('removeFormat');
    const selection = document.getSelection();
    const range = selection && selection.rangeCount > 0 ? selection.getRangeAt(0) : null;
    if (!range) {
        return;
    }

    for (const span of surface.querySelectorAll('span')) {
        if (range.intersectsNode(span)) {
            unwrap(span);
        }
    }

    for (const block of blocksOfSelection(surface)) {
        block.classList.remove(...alignClasses);
    }
}

// The innermost blocks the selection touches, so a list item is aligned rather than its list.
function blocksOfSelection(surface) {
    const selection = document.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return [];
    }

    const range = selection.getRangeAt(0);
    const found = new Set();
    const add = node => {
        const block = elementOf(node)?.closest(blockSelector);
        if (block && block !== surface && surface.contains(block)) {
            found.add(block);
        }
    };
    add(range.startContainer);
    add(range.endContainer);
    for (const block of surface.querySelectorAll(blockSelector)) {
        if (range.intersectsNode(block)) {
            found.add(block);
        }
    }

    return [...found].filter(block => ![...found].some(other => other !== block && block.contains(other)));
}

// The browser's own cut and copy, which need the click that ran the command; where the browser
// refuses them, the text of the selection goes through the asynchronous clipboard instead. A cut
// removes the selection only once the clipboard holds it: without a clipboard, or on a refused
// permission, the text stays where it was.
async function copySelection(range, cut) {
    if (range.collapsed) {
        return;
    }

    if (document.execCommand(cut ? 'cut' : 'copy')) {
        return;
    }

    if (!navigator.clipboard) {
        return;
    }

    try {
        await navigator.clipboard.writeText(range.toString());
    }
    catch {
        return;
    }

    if (cut) {
        range.deleteContents();
    }
}

async function readClipboard() {
    const clipboard = navigator.clipboard;
    if (!clipboard) {
        return null;
    }

    try {
        if (!clipboard.read) {
            return { html: '', text: await clipboard.readText() };
        }

        let html = '';
        let text = '';
        for (const item of await clipboard.read()) {
            if (!html && item.types.includes('text/html')) {
                html = await (await item.getType('text/html')).text();
            }

            if (!text && item.types.includes('text/plain')) {
                text = await (await item.getType('text/plain')).text();
            }
        }

        return { html, text };
    }
    catch {
        // Permission refused or clipboard unavailable: nothing is pasted.
        return null;
    }
}

// The table as a grid of slots, each holding the cell that covers it, so row and column spans are
// honoured: places maps a cell to its anchor row and column and to its spans.
function tableGrid(table) {
    const rows = [...table.rows];
    const grid = rows.map(() => []);
    const places = new Map();
    rows.forEach((row, r) => {
        let c = 0;
        for (const cell of row.cells) {
            while (grid[r][c]) {
                c++;
            }

            const spanRows = Math.max(1, cell.rowSpan || 1);
            const spanCols = Math.max(1, cell.colSpan || 1);
            places.set(cell, { row: r, col: c, rows: spanRows, cols: spanCols });
            for (let i = 0; i < spanRows && r + i < rows.length; i++) {
                for (let j = 0; j < spanCols; j++) {
                    grid[r + i][c + j] = cell;
                }
            }

            c += spanCols;
        }
    });
    return { rows, grid, places, width: Math.max(0, ...grid.map(line => line.length)) };
}

function newCell(like) {
    const cell = document.createElement(like?.tagName === 'TH' ? 'th' : 'td');
    cell.appendChild(document.createElement('br'));
    return cell;
}

function setSpan(cell, name, value) {
    if (value > 1) {
        cell.setAttribute(name, String(value));
    }
    else {
        cell.removeAttribute(name);
    }
}

// Puts a cell in row r at column col: before the first cell of that row anchored at or after it.
function placeCell(info, r, cell, col) {
    const row = info.rows[r];
    const next = [...row.cells].find(other => other !== cell && (info.places.get(other)?.col ?? Infinity) >= col);
    row.insertBefore(cell, next ?? null);
}

function caretInto(cell) {
    const range = document.createRange();
    range.selectNodeContents(cell);
    range.collapse(true);
    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
}

function hasContent(cell) {
    return cell.textContent.trim() !== '' || cell.querySelector('img,hr,table,aside') !== null;
}

function editTable(cell, action, argument) {
    const table = cell.closest('table');
    if (!table) {
        return;
    }

    switch (action) {
        case 'addrowabove':
        case 'addrowbelow':
            addRow(table, cell, action === 'addrowbelow');
            break;
        case 'deleterow':
            deleteRow(table, cell);
            break;
        case 'addcolumnbefore':
        case 'addcolumnafter':
            addColumn(table, cell, action === 'addcolumnafter');
            break;
        case 'deletecolumn':
            deleteColumn(table, cell);
            break;
        case 'mergecellright': {
            const info = tableGrid(table);
            const place = info.places.get(cell);
            const right = info.grid[place.row][place.col + place.cols];
            if (right) {
                setCellSpan(table, cell, place.rows, place.cols + info.places.get(right).cols);
            }
            break;
        }
        case 'mergecelldown': {
            const info = tableGrid(table);
            const place = info.places.get(cell);
            const below = info.grid[place.row + place.rows]?.[place.col];
            if (below) {
                setCellSpan(table, cell, place.rows + info.places.get(below).rows, place.cols);
            }
            break;
        }
        case 'splitcell':
            splitCell(table, cell);
            break;
        case 'setcellspan': {
            // "rowsxcolumns", as for inserttable; without an argument the cell is split back to 1x1.
            const match = /^(\d{1,2})x(\d{1,2})$/.exec(argument ?? '');
            setCellSpan(table, cell, match ? Number(match[1]) : 1, match ? Number(match[2]) : 1);
            break;
        }
        default:
            return;
    }

    if (cell.isConnected) {
        caretInto(cell);
    }
}

function addRow(table, cell, below) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    const r = below ? place.row + place.rows - 1 : place.row;
    const row = document.createElement('tr');
    for (let c = 0; c < info.width; c++) {
        const over = info.grid[r][c];
        if (!over) {
            row.appendChild(newCell(null));
            continue;
        }

        const at = info.places.get(over);
        c = at.col + at.cols - 1;
        // A cell crossing the new row's edge grows over it instead of getting a neighbour.
        if (below ? at.row + at.rows - 1 > r : at.row < r) {
            setSpan(over, 'rowspan', at.rows + 1);
            continue;
        }

        const added = newCell(over);
        setSpan(added, 'colspan', at.cols);
        row.appendChild(added);
    }

    if (below) {
        info.rows[r].after(row);
    }
    else {
        info.rows[r].before(row);
    }
}

function deleteRow(table, cell) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    const r = place.row;
    if (info.rows.length === 1) {
        removeTable(table);
        return;
    }

    const seen = new Set();
    for (let c = 0; c < info.width; c++) {
        const over = info.grid[r][c];
        if (!over || seen.has(over)) {
            continue;
        }

        seen.add(over);
        const at = info.places.get(over);
        if (at.rows > 1) {
            setSpan(over, 'rowspan', at.rows - 1);
            if (at.row === r) {
                // Anchored in the row that goes: it moves down, keeping its column.
                placeCell(info, r + 1, over, at.col);
            }
        }
    }

    info.rows[r].remove();
    const landing = info.grid[r + 1]?.[place.col] ?? info.grid[r - 1]?.[place.col];
    if (landing?.isConnected) {
        caretInto(landing);
    }
}

function addColumn(table, cell, after) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    const c = after ? place.col + place.cols - 1 : place.col;
    const grown = new Set();
    info.rows.forEach((row, r) => {
        const over = info.grid[r][c];
        if (!over) {
            row.appendChild(newCell(null));
            return;
        }

        const at = info.places.get(over);
        if (after ? at.col + at.cols - 1 > c : at.col < c) {
            if (!grown.has(over)) {
                grown.add(over);
                setSpan(over, 'colspan', at.cols + 1);
            }

            return;
        }

        // A cell spanning several rows gets one neighbour spanning the same rows, added once.
        if (at.row !== r) {
            return;
        }

        const added = newCell(over);
        setSpan(added, 'rowspan', at.rows);
        if (after) {
            over.after(added);
        }
        else {
            over.before(added);
        }
    });
}

function deleteColumn(table, cell) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    const seen = new Set();
    let landing = null;
    for (let r = 0; r < info.rows.length; r++) {
        const over = info.grid[r][place.col];
        if (!over || seen.has(over)) {
            continue;
        }

        seen.add(over);
        const at = info.places.get(over);
        if (at.cols > 1) {
            setSpan(over, 'colspan', at.cols - 1);
        }
        else {
            over.remove();
        }
    }

    if (!table.querySelector('td,th')) {
        removeTable(table);
        return;
    }

    landing = info.grid[place.row][place.col + place.cols] ?? info.grid[place.row][place.col - 1];
    if (landing?.isConnected) {
        caretInto(landing);
    }
}

function splitCell(table, cell) {
    const info = tableGrid(table);
    const place = info.places.get(cell);
    if (place.rows === 1 && place.cols === 1) {
        return;
    }

    setSpan(cell, 'rowspan', 1);
    setSpan(cell, 'colspan', 1);
    for (let j = 1; j < place.cols; j++) {
        cell.after(newCell(cell));
    }

    for (let i = 1; i < place.rows && place.row + i < info.rows.length; i++) {
        for (let j = 0; j < place.cols; j++) {
            placeCell(info, place.row + i, newCell(cell), place.col);
        }
    }
}

// Makes the cell span rows x columns from where it is anchored, absorbing the content of the cells
// it now covers. Refused (nothing changes) when a covered cell reaches outside that area, or the
// area leaves the table: the table stays a rectangle.
function setCellSpan(table, cell, rows, cols) {
    let info = tableGrid(table);
    let place = info.places.get(cell);
    const spanRows = Math.max(1, Math.min(rows, info.rows.length - place.row));
    const spanCols = Math.max(1, Math.min(cols, info.width - place.col));
    for (let r = place.row; r < place.row + spanRows; r++) {
        for (let c = place.col; c < place.col + spanCols; c++) {
            const over = info.grid[r][c];
            if (!over) {
                return;
            }

            const at = info.places.get(over);
            if (over !== cell && (at.row < place.row || at.col < place.col
                || at.row + at.rows > place.row + spanRows || at.col + at.cols > place.col + spanCols)) {
                return;
            }
        }
    }

    splitCell(table, cell);
    info = tableGrid(table);
    place = info.places.get(cell);
    const absorbed = new Set();
    for (let r = place.row; r < place.row + spanRows; r++) {
        for (let c = place.col; c < place.col + spanCols; c++) {
            const over = info.grid[r][c];
            if (over && over !== cell) {
                absorbed.add(over);
            }
        }
    }

    for (const other of absorbed) {
        if (hasContent(other)) {
            if (!hasContent(cell)) {
                cell.replaceChildren();
            }
            else {
                cell.appendChild(document.createElement('br'));
            }

            cell.append(...other.childNodes);
        }

        other.remove();
    }

    setSpan(cell, 'rowspan', spanRows);
    setSpan(cell, 'colspan', spanCols);
}

function removeTable(table) {
    const paragraph = document.createElement('p');
    paragraph.appendChild(document.createElement('br'));
    table.replaceWith(paragraph);
    caretInto(paragraph);
}

function tableHtml(argument) {
    const match = /^(\d{1,2})x(\d{1,2})$/.exec(argument);
    const rows = match ? Math.max(1, Number(match[1])) : 3;
    const columns = match ? Math.max(1, Number(match[2])) : 3;
    const row = `<tr>${'<td><br></td>'.repeat(columns)}</tr>`;
    return `<table><tbody>${row.repeat(rows)}</tbody></table><p><br></p>`;
}

function moveBetweenCells(surface, backwards) {
    const selection = document.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return false;
    }

    const cell = elementOf(selection.getRangeAt(0).startContainer)?.closest('td,th');
    const table = cell?.closest('table');
    if (!cell || !table || !surface.contains(table)) {
        return false;
    }

    const cells = [...table.querySelectorAll('td,th')];
    const next = cells[cells.indexOf(cell) + (backwards ? -1 : 1)];
    if (!next) {
        return false;
    }

    const range = document.createRange();
    range.selectNodeContents(next);
    range.collapse(true);
    selection.removeAllRanges();
    selection.addRange(range);
    return true;
}

// Keeps the document equal to what the sanitiser in .NET would produce from it, so the value
// round-trips: no style attribute, no font element, no class outside the allowed ones, no empty
// span, the rel protection on every link, and no block inside a paragraph. Returns whether a block
// had to be moved, since that loses the caret.
function normalise(surface) {
    const lifted = liftBlocks(surface);
    const wrapped = wrapLooseContent(surface);
    for (const element of surface.querySelectorAll('[style]')) {
        element.removeAttribute('style');
    }

    for (const font of surface.querySelectorAll('font')) {
        unwrap(font);
    }

    const state = editors.get(surface);
    const classes = state ? state.classes : allowedClasses;
    for (const element of surface.querySelectorAll('[class]')) {
        for (const name of [...element.classList]) {
            if (classes && !classes.has(name)) {
                element.classList.remove(name);
            }
        }

        if (element.classList.length === 0) {
            element.removeAttribute('class');
        }
    }

    // Under a host policy a span may carry its meaning in another attribute (data-*, a title), so
    // only a bare span is unwrapped; the sanitiser in .NET still has the last word on the value.
    for (const span of surface.querySelectorAll('span:not([class])')) {
        if (!state?.policy || span.attributes.length === 0) {
            unwrap(span);
        }
    }

    for (const anchor of surface.querySelectorAll('a[href]')) {
        if (anchor.getAttribute('rel') !== 'noopener noreferrer') {
            anchor.setAttribute('rel', 'noopener noreferrer');
        }
    }

    return lifted || wrapped;
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

// Text left directly in the surface (the first characters typed in an empty editor, a list turned
// back into text) becomes paragraphs, a line break ending one.
function wrapLooseContent(surface) {
    const blocks = `${hostBlocks},hr`;
    let moved = false;
    let run = null;
    for (const child of [...surface.childNodes]) {
        const isBlock = child.nodeType === Node.ELEMENT_NODE && child.matches(blocks);
        if (isBlock || (child.nodeType === Node.TEXT_NODE && child.data.trim() === '' && !run)) {
            run = null;
            continue;
        }

        if (child.nodeName === 'BR') {
            if (run) {
                child.remove();
            }
            else {
                const empty = document.createElement('p');
                child.replaceWith(empty);
                empty.appendChild(child);
            }

            run = null;
            moved = true;
            continue;
        }

        if (!run) {
            run = document.createElement('p');
            child.before(run);
        }

        run.appendChild(child);
        moved = true;
    }

    return moved;
}

// A list command run inside a paragraph nests the list in it. HTML cannot parse that back (the
// paragraph closes before the list), so the value would change on its next load: the paragraph is
// split around its blocks instead, what surrounds them becoming paragraphs of their own.
function liftBlocks(surface) {
    const blocks = hostBlocks;
    let moved = false;
    for (const paragraph of surface.querySelectorAll('p')) {
        if (!paragraph.isConnected || ![...paragraph.children].some(child => child.matches(blocks))) {
            continue;
        }

        const parts = [];
        const created = new Set();
        let run = null;
        for (const child of [...paragraph.childNodes]) {
            if (child.nodeType === Node.ELEMENT_NODE && child.matches(blocks)) {
                run = null;
                parts.push(child);
                continue;
            }

            if (!run) {
                run = document.createElement('p');
                created.add(run);
                parts.push(run);
            }

            run.appendChild(child);
        }

        paragraph.replaceWith(...parts.filter(part => !created.has(part) || part.textContent.trim() !== '' || part.querySelector('br')));
        moved = true;
    }

    return moved;
}

function unwrap(element) {
    if (!element?.parentNode) {
        return;
    }

    while (element.firstChild) {
        element.parentNode.insertBefore(element.firstChild, element);
    }

    element.remove();
}

function elementOf(node) {
    return node?.nodeType === Node.ELEMENT_NODE ? node : node?.parentElement ?? null;
}

function caretFromPoint(x, y) {
    if (document.caretRangeFromPoint) {
        return document.caretRangeFromPoint(x, y);
    }

    const position = document.caretPositionFromPoint?.(x, y);
    if (!position) {
        return null;
    }

    const range = document.createRange();
    range.setStart(position.offsetNode, position.offset);
    range.collapse(true);
    return range;
}

// The caret as a count of characters from the start, which survives the document being rebuilt
// from a new value (undo, redo, a value changed by the application).
function caretOffset(surface) {
    const selection = document.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return -1;
    }

    const range = selection.getRangeAt(0);
    if (!surface.contains(range.startContainer)) {
        return -1;
    }

    const before = document.createRange();
    before.selectNodeContents(surface);
    before.setEnd(range.startContainer, range.startOffset);
    return before.toString().length;
}

function placeCaret(surface, offset) {
    const walker = document.createTreeWalker(surface, NodeFilter.SHOW_TEXT);
    let remaining = offset;
    let node = walker.nextNode();
    while (node) {
        if (remaining <= node.data.length) {
            const range = document.createRange();
            range.setStart(node, remaining);
            range.collapse(true);
            const selection = document.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
            return;
        }

        remaining -= node.data.length;
        node = walker.nextNode();
    }

    const end = document.createRange();
    end.selectNodeContents(surface);
    end.collapse(false);
    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(end);
}

function escapeHtml(text) {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function escapeAttribute(text) {
    return escapeHtml(text).replace(/"/g, '&quot;');
}
