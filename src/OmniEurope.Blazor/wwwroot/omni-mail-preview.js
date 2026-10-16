// The script of OmniMailTemplateEditor: its preview, the caret insertion of a variable in the subject and
// the marking of the tokens in the body. The preview: A mail carries its look in style attributes, as mail clients
// need, and the strict CSP of the page refuses them, even in a document parsed apart: for the preview the
// server writes each look in data-omni-mail-look instead. The document is parsed apart (DOMParser runs no
// script), each look is given to its element through the CSSOM (element.style.cssText), which the CSP does
// not govern, and the elements are moved into a shadow root, where the page's stylesheet does not reach
// them. Anything that could run or load something is dropped first, though the server already sanitised it.
const previews = new WeakMap();
const look = 'data-omni-mail-look';
const dropped = 'script, style, link, meta, base, iframe, frame, object, embed, form, input, button, textarea, select, svg, math, template';

function rootOf(host) {
    let preview = previews.get(host);
    if (preview) {
        return preview;
    }

    const shadow = host.attachShadow({ mode: 'open' });
    // A link of the preview leads nowhere: following it would leave the page being edited.
    shadow.addEventListener('click', event => {
        if (event.target instanceof Element && event.target.closest('a')) {
            event.preventDefault();
        }
    });
    preview = { shadow };
    previews.set(host, preview);
    return preview;
}

function clean(element) {
    for (const attribute of [...element.attributes]) {
        const name = attribute.name.toLowerCase();
        if (name.startsWith('on') || name === 'srcdoc' || ((name === 'href' || name === 'src') && /^\s*javascript:/i.test(attribute.value))) {
            element.removeAttribute(attribute.name);
        }
    }
}

export function render(host, html) {
    if (!host) {
        return;
    }

    const { shadow } = rootOf(host);
    const parsed = new DOMParser().parseFromString(html, 'text/html');
    parsed.querySelectorAll(dropped).forEach(node => node.remove());

    const page = document.createElement('div');
    // The page of the mail stands alone: nothing inherited from the editor's page leaks in.
    page.style.cssText = 'all: initial; display: block; ' + (parsed.body.getAttribute(look) ?? '');
    const looks = [];
    for (const element of parsed.body.querySelectorAll('*')) {
        clean(element);
        const declarations = element.getAttribute(look);
        if (declarations !== null) {
            element.removeAttribute(look);
            looks.push([element, declarations]);
        }
    }

    for (const node of [...parsed.body.childNodes]) {
        page.appendChild(document.adoptNode(node));
    }

    for (const [element, declarations] of looks) {
        element.style.cssText = declarations;
    }

    shadow.replaceChildren(page);
}

// Writes a variable's token at the caret of the subject field, replacing its selection, then tells
// Blazor the value changed through the field's own input event.
export function insertAtCaret(id, text) {
    const field = document.getElementById(id);
    if (!(field instanceof HTMLInputElement) || field.disabled || field.readOnly) {
        return false;
    }

    const start = field.selectionStart ?? field.value.length;
    const end = field.selectionEnd ?? start;
    field.focus({ preventScroll: true });
    field.setRangeText(text, start, end, 'end');
    field.dispatchEvent(new Event('input', { bubbles: true }));
    field.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
}

// The {{name}} tokens of a template, and its block markers ({{#button name}}, {{#banner tone}}), marked in the body's surface by the CSS Custom Highlight API
// (::highlight(omni-mail-token) in the stylesheet), as the proofreading marks its passages: the document is
// never touched and the value stays the template. Every attached surface shares the one named highlight;
// each is read again after any change of its text, once per frame. A browser without the API marks nothing.
const tokenPattern = /\{\{\s*(?:#(?:button|banner)(?:\s+[A-Za-z][A-Za-z0-9_.-]*)?|[A-Za-z][A-Za-z0-9_.-]*)\s*\}\}/gi;
const tokenSurfaces = new Map();

function highlightsSupported() {
    return typeof CSS !== 'undefined' && !!CSS.highlights && typeof Highlight !== 'undefined';
}

function paintTokens() {
    // A surface the editor replaced (its source face, then the visual one again) leaves the document: forgotten.
    for (const [surface, entry] of tokenSurfaces) {
        if (!surface.isConnected) {
            entry.observer.disconnect();
            tokenSurfaces.delete(surface);
        }
    }

    const ranges = [...tokenSurfaces.values()].flatMap(entry => entry.ranges);
    if (ranges.length === 0) {
        CSS.highlights.delete('omni-mail-token');
    }
    else {
        CSS.highlights.set('omni-mail-token', new Highlight(...ranges));
    }
}

function readTokens(surface) {
    const entry = tokenSurfaces.get(surface);
    if (!entry) {
        return 0;
    }

    entry.queued = false;
    entry.ranges = [];
    const walker = document.createTreeWalker(surface, NodeFilter.SHOW_TEXT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        for (const match of node.data.matchAll(tokenPattern)) {
            const range = document.createRange();
            range.setStart(node, match.index);
            range.setEnd(node, match.index + match[0].length);
            entry.ranges.push(range);
        }
    }

    paintTokens();
    return entry.ranges.length;
}

// Returns the number of tokens marked, or -1 when the browser has no Custom Highlight API.
export function attachTokens(surface) {
    if (!surface || !highlightsSupported()) {
        return -1;
    }

    if (!tokenSurfaces.has(surface)) {
        const entry = { ranges: [], queued: false };
        entry.observer = new MutationObserver(() => {
            if (!entry.queued) {
                entry.queued = true;
                requestAnimationFrame(() => readTokens(surface));
            }
        });
        entry.observer.observe(surface, { characterData: true, childList: true, subtree: true });
        tokenSurfaces.set(surface, entry);
    }

    return readTokens(surface);
}

export function detachTokens(surface) {
    const entry = tokenSurfaces.get(surface);
    if (!entry) {
        return;
    }

    entry.observer.disconnect();
    tokenSurfaces.delete(surface);
    paintTokens();
}
