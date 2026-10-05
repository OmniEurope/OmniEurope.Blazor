// Part of omni-html-editor.js: the document model of the surface. The editors on the page, the
// classes the surface writes (the ones the stylesheet and the .NET sanitizer know), the joins of two
// blocks done without the browser's styled spans, the normalisation that keeps the document equal to
// what the sanitizer produces, and the caret arithmetic that survives a rebuilt document.

export const editors = new WeakMap();
export const blockSelector = 'p,h1,h2,h3,h4,h5,h6,li,blockquote,pre,td,th,div';
// Alignment classes by direction, the suffix of the align commands and the name reported to .NET.
// Right alignment writes the logical end class, the one the stylesheet and the .NET sanitizer know.
export const alignClassByDirection = {
    left: 'omni-align-left',
    center: 'omni-align-center',
    right: 'omni-align-end',
    justify: 'omni-align-justify'
};
export const alignClasses = Object.values(alignClassByDirection);
export const sizeClasses = {
    small: 'omni-font-size-small',
    normal: 'omni-font-size-normal',
    large: 'omni-font-size-large',
    xlarge: 'omni-font-size-xlarge'
};
export const allowedClasses = new Set([...alignClasses, ...Object.values(sizeClasses)]);

// The elements a paragraph cannot hold: text next to them is wrapped in its own paragraph, and one
// found inside a paragraph is lifted out of it. The sectioning elements only ever reach the surface
// when a host policy allows them (an aside holding a note, a figure).
const hostBlocks = 'ul,ol,table,blockquote,pre,h1,h2,h3,h4,h5,h6,div,p,aside,section,article,figure,header,footer,nav,address,dl';

// When two blocks are joined, by a deletion at their boundary or across them, the browser keeps the
// look of the moved text with a styled span; under a strict style-src that span is refused and
// reported. The common joins are therefore done here, as plain moves of nodes; a join inside a
// list or a table is left to the browser.
const joinable = 'p,h1,h2,h3,h4,h5,h6,blockquote,pre,div';

export function joinInstead(surface, event) {
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
export function insertBlocks(surface, range, html) {
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

// Inserts HTML at the range, in place of insertHTML: Chrome stages the fragment of insertHTML in an
// element it gives an inline style, which a strict style-src refuses (two violations at each
// insertion, plain text included). Blocks go through insertBlocks; anything else is inserted as
// nodes at the range, the caret after the last one. The history lives in .NET, so nothing is lost
// by leaving the browser's own undo stack out.
export function insertHtmlAt(surface, range, html) {
    if (insertBlocks(surface, range, html)) {
        return;
    }

    const template = document.createElement('template');
    template.innerHTML = html;
    const last = template.content.lastChild;
    if (!last) {
        return;
    }

    range.deleteContents();
    range.insertNode(template.content);
    const after = document.createRange();
    after.setStartAfter(last);
    after.collapse(true);
    document.getSelection().removeAllRanges();
    document.getSelection().addRange(after);
}

// Turns the items of a list the range touches into paragraphs, in place of the list command run
// again inside its own list, which writes styled spans a strict style-src refuses. The items before
// stay in the list, those after go into a copy of it; the caret keeps its text position.
export function unlist(range, item) {
    const list = item.parentElement;
    const items = [...list.children].filter(child => child.tagName === 'LI' && range.intersectsNode(child));
    const caret = { node: range.startContainer, offset: range.startOffset };
    const tail = list.cloneNode(false);
    for (let next = items[items.length - 1].nextSibling; next; next = items[items.length - 1].nextSibling) {
        tail.appendChild(next);
    }

    const paragraphs = items.map(entry => {
        const paragraph = document.createElement('p');
        while (entry.firstChild) {
            paragraph.appendChild(entry.firstChild);
        }

        if (!paragraph.firstChild) {
            paragraph.appendChild(document.createElement('br'));
        }

        entry.remove();
        return paragraph;
    });
    list.after(...paragraphs, ...(tail.childNodes.length > 0 ? [tail] : []));
    if (list.children.length === 0) {
        list.remove();
    }

    const restored = document.createRange();
    if (caret.node.isConnected && caret.node !== item) {
        restored.setStart(caret.node, Math.min(caret.offset, caret.node.nodeType === Node.TEXT_NODE ? caret.node.length : caret.node.childNodes.length));
    }
    else {
        restored.selectNodeContents(paragraphs[0]);
        restored.collapse(false);
    }
    restored.collapse(true);
    document.getSelection().removeAllRanges();
    document.getSelection().addRange(restored);
}

// Keeps the document equal to what the sanitiser in .NET would produce from it, so the value
// round-trips: no style attribute, no font element, no class outside the allowed ones, no empty
// span, the rel protection on every link, and no block inside a paragraph. Returns whether a block
// had to be moved, since that loses the caret.
export function normalise(surface) {
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

export function unwrap(element) {
    if (!element?.parentNode) {
        return;
    }

    while (element.firstChild) {
        element.parentNode.insertBefore(element.firstChild, element);
    }

    element.remove();
}

export function elementOf(node) {
    return node?.nodeType === Node.ELEMENT_NODE ? node : node?.parentElement ?? null;
}

export function caretFromPoint(x, y) {
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
export function caretOffset(surface) {
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

export function placeCaret(surface, offset) {
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

export function escapeHtml(text) {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

export function escapeAttribute(text) {
    return escapeHtml(text).replace(/"/g, '&quot;');
}
