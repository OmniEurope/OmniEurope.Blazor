// Part of omni-html-editor.js: the toolbar state read at the caret (describe) and the formatting
// commands (apply), with the clipboard fallbacks of cut, copy and paste.
import {
    blockSelector, alignClassByDirection, alignClasses, sizeClasses, unwrap, elementOf, escapeHtml, escapeAttribute
} from './model.js';
import { editTable, tableHtml } from './tables.js';

// Underline and strikethrough are read from the elements rather than from queryCommandState, which
// reports the underline every link is drawn with.
const marks = [
    ['bold', 'bold'],
    ['italic', 'italic'],
    ['subscript', 'subscript'],
    ['superscript', 'superscript']
];

// "marks|block|align|size": the pressed toggles, the block tag, the alignment and the text size at
// the caret, compared as one string so .NET is only called when one of them changes.
export function describe(surface) {
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
    const align = aligned ? Object.keys(alignClassByDirection).find(direction => aligned.classList.contains(alignClassByDirection[direction])) : 'left';
    const sized = within(Object.values(sizeClasses).map(name => `.${name}`).join(','));
    const size = sized ? Object.keys(sizeClasses).find(name => sized.classList.contains(sizeClasses[name])) : 'normal';
    return `${pressed.join(' ')}|${tag}|${align}|${size}`;
}

export function apply(surface, range, action, argument) {
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
            block.classList.add(alignClassByDirection[direction]);
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

export async function readClipboard() {
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
