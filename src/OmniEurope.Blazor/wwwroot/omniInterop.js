export function focusFirstInvalid(root) {
    const invalid = root?.querySelector?.('[aria-invalid="true"], .invalid');
    if (invalid instanceof HTMLElement) {
        invalid.focus({ preventScroll: true });
        const reducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches === true;
        invalid.scrollIntoView({ block: 'center', behavior: reducedMotion ? 'auto' : 'smooth' });
    }
}

export function wrapTextSelection(element, prefix, suffix) {
    if (!(element instanceof HTMLTextAreaElement)) {
        throw new TypeError('A textarea is required.');
    }

    const start = element.selectionStart ?? 0;
    const end = element.selectionEnd ?? start;
    const selected = element.value.slice(start, end);
    return {
        value: `${element.value.slice(0, start)}${prefix}${selected}${suffix}${element.value.slice(end)}`,
        selectionStart: start + prefix.length,
        selectionEnd: start + prefix.length + selected.length
    };
}

export function restoreTextSelection(element, start, end) {
    if (!(element instanceof HTMLTextAreaElement)) {
        throw new TypeError('A textarea is required.');
    }

    element.focus({ preventScroll: true });
    element.setSelectionRange(start, end);
}

export function setDocumentMetadata(language, title) {
    document.documentElement.lang = language;
    document.title = title;
}

// The back action of a page header or a not-found state that names no destination: one step back in
// the history, exactly what the browser's own back button does.
export function historyBack() {
    window.history.back();
}

// Puts a text on the clipboard. The asynchronous clipboard API needs a secure context and the
// clipboard-write permission; when it is missing or refuses, a hidden text area and the copy command
// take over, the way a user would select and copy. True only when one of the two succeeded.
export async function copyText(text) {
    const value = typeof text === 'string' ? text : '';
    if (navigator.clipboard?.writeText && window.isSecureContext) {
        try {
            await navigator.clipboard.writeText(value);
            return true;
        } catch {
            // Refused (no focus, no permission): fall back to the selection below.
        }
    }

    const area = document.createElement('textarea');
    area.value = value;
    area.setAttribute('readonly', '');
    area.className = 'omni-visually-hidden';
    const previous = document.activeElement;
    document.body.appendChild(area);
    try {
        area.select();
        return document.execCommand('copy');
    } catch {
        return false;
    } finally {
        area.remove();
        if (previous instanceof HTMLElement) {
            previous.focus({ preventScroll: true });
        }
    }
}
