// Part of omni-html-editor.js: the passages the proofreaders of the host flag, underlined with named
// highlights (CSS Custom Highlight API), so the document itself never changes. The text goes to .NET
// block by block (a paragraph, a heading, a list item, a cell) with its language; a block whose text
// has not changed keeps the answer it had, and only the others are asked again once typing pauses.
import { editors, caretFromPoint } from './model.js';

const proofreadDelay = 700;
const cacheLimit = 2000;
const blockSelector = 'p,h1,h2,h3,h4,h5,h6,li,td,th,blockquote,pre,dd,dt,figcaption,caption';
const kinds = ['spelling', 'grammar'];
// The ranges of every editor of the page, by surface: the highlight registry is the document's.
const painted = new Map();

export function requestProofreading(state, now = false) {
    if (!state.proofread) {
        return;
    }

    window.clearTimeout(state.proofreadTimer);
    state.proofreadTimer = window.setTimeout(() => proofread(state), now ? 0 : proofreadDelay);
}

// Forgets every answer (a word was ignored everywhere or added to the dictionary) and asks again now.
export function proofreadAgain(state) {
    state.proofreadCache?.clear();
    requestProofreading(state, true);
}

export function clearProofreading(state) {
    window.clearTimeout(state.proofreadTimer);
    state.proofreadTicket = (state.proofreadTicket ?? 0) + 1;
    state.proofreadIssues = [];
    painted.delete(state.surface);
    repaint();
}

// The flagged passage under the pointer, or at the caret for the context-menu key, described for .NET.
export function issueAt(state, event) {
    if (!state.proofread || !state.proofreadIssues?.length) {
        return null;
    }

    const point = event.clientX || event.clientY ? caretFromPoint(event.clientX, event.clientY) : state.range;
    if (!point) {
        return null;
    }

    const issue = state.proofreadIssues.find(candidate => {
        try {
            return candidate.range.comparePoint(point.startContainer, point.startOffset) === 0;
        }
        catch {
            return false;
        }
    });
    state.menuIssue = issue ?? null;
    return issue ? {
        p: issue.p, text: issue.text, lang: issue.language, s: issue.s, l: issue.l, k: issue.k, m: issue.m ?? null
    } : null;
}

// The chosen correction replaces the passage the menu was opened on, as typed text.
export function replaceIssue(state, text) {
    const issue = state.menuIssue;
    state.menuIssue = null;
    if (!issue || !state.surface.contains(issue.range.commonAncestorContainer)) {
        return false;
    }

    const typed = document.createTextNode(text);
    issue.range.deleteContents();
    issue.range.insertNode(typed);
    const caret = document.createRange();
    caret.setStartAfter(typed);
    caret.collapse(true);
    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(caret);
    typed.parentNode?.normalize();
    return true;
}

// "Ignore": this passage of this block is no longer underlined while the block keeps its text.
export function ignoreIssue(state) {
    const issue = state.menuIssue;
    state.menuIssue = null;
    if (!issue) {
        return;
    }

    state.proofreadIgnored.add(`${issue.key}|${issue.s}|${issue.l}`);
    paint(state, blocks(state.surface));
}

async function proofread(state) {
    const surface = state.surface;
    if (!editors.has(surface) || !state.proofread) {
        return;
    }

    const found = blocks(surface);
    const missing = found.filter(block => !state.proofreadCache.has(block.key));
    if (missing.length > 0) {
        const ticket = ++state.proofreadTicket;
        let answer = null;
        try {
            answer = await state.dotnet.invokeMethodAsync('OnProofreadRequested', missing.map(block => block.text), missing.map(block => block.language));
        }
        catch {
            return;
        }

        // Null: nothing was checked (the editor locked, every proofreader failing), so nothing is kept as clean.
        if (ticket !== state.proofreadTicket || !editors.has(surface) || answer == null) {
            return;
        }

        if (state.proofreadCache.size > cacheLimit) {
            state.proofreadCache.clear();
        }

        for (const block of missing) {
            state.proofreadCache.set(block.key, []);
        }

        for (const [p, t, s, l, k, m] of JSON.parse(answer)) {
            const block = missing[t];
            if (block && s >= 0 && l > 0 && s + l <= block.text.length) {
                state.proofreadCache.get(block.key).push({ p, s, l, k, m });
            }
        }

        // The document may have changed while the proofreaders worked: read it again.
        paint(state, blocks(surface));
        return;
    }

    paint(state, found);
}

// The ranges of the cached issues in the document as it stands, then the highlights of the page.
function paint(state, found) {
    const issues = [];
    for (const block of found) {
        for (const issue of state.proofreadCache.get(block.key) ?? []) {
            if (state.proofreadIgnored.has(`${block.key}|${issue.s}|${issue.l}`)) {
                continue;
            }

            const range = rangeOf(block, issue.s, issue.s + issue.l);
            if (range) {
                issues.push({ ...issue, range, key: block.key, text: block.text, language: block.language });
            }
        }
    }

    state.proofreadIssues = issues;
    painted.set(state.surface, issues);
    repaint();
}

function repaint() {
    if (typeof CSS === 'undefined' || !CSS.highlights || typeof Highlight === 'undefined') {
        return;
    }

    kinds.forEach((kind, index) => {
        const ranges = [...painted.values()].flat().filter(issue => issue.k === index).map(issue => issue.range);
        const name = `omni-proofreading-${kind}`;
        if (ranges.length === 0) {
            CSS.highlights.delete(name);
        }
        else {
            CSS.highlights.set(name, new Highlight(...ranges));
        }
    });
}

// The text of the surface grouped by block, inline markup read through; the elements that are not
// editable (a note, an image caption kept by the host) and the text proposed after the caret left out.
function blocks(surface) {
    const result = [];
    const byElement = new Map();
    const walker = document.createTreeWalker(surface, NodeFilter.SHOW_TEXT, {
        acceptNode: node => skipped(surface, node) ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT
    });
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        const closest = node.parentElement?.closest(blockSelector);
        const element = closest && surface.contains(closest) ? closest : surface;
        let block = byElement.get(element);
        if (!block) {
            block = { element, text: '', parts: [], language: node.parentElement?.closest('[lang]')?.getAttribute('lang') || null };
            byElement.set(element, block);
            result.push(block);
        }

        block.parts.push({ node, start: block.text.length });
        block.text += node.data;
    }

    return result.filter(block => /\S/.test(block.text)).map(block => ({ ...block, key: `${block.language ?? ''}\n${block.text}` }));
}

function skipped(surface, node) {
    const parent = node.parentElement;
    if (!parent) {
        return true;
    }

    const locked = parent.closest('[contenteditable="false"],.omni-html-editor__suggestion');
    return locked !== null && surface.contains(locked);
}

function rangeOf(block, start, end) {
    const from = positionOf(block, start, false);
    const to = positionOf(block, end, true);
    if (!from || !to) {
        return null;
    }

    const range = document.createRange();
    range.setStart(from.node, from.offset);
    range.setEnd(to.node, to.offset);
    return range;
}

// The text node and offset of a character position of the block; an end falls at the end of a node
// rather than at the start of the next one.
function positionOf(block, at, end) {
    for (const part of block.parts) {
        const length = part.node.data.length;
        if (at < part.start + length || (end && at === part.start + length)) {
            return at >= part.start ? { node: part.node, offset: at - part.start } : null;
        }
    }

    return null;
}
