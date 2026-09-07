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
