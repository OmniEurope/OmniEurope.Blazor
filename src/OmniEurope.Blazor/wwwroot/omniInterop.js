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

// The boot splash of the page (see omni-boot.js), faded out then removed once the application has
// rendered. It lives outside the element Blazor renders into, so removing it never disturbs Blazor.
// Returns whether there was a splash to remove.
export function hideBootSplash(id) {
    const splash = document.getElementById(id);
    if (!splash || splash.classList.contains('omni-boot-splash--leaving')) {
        return false;
    }

    const remove = () => splash.remove();
    const reducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches === true;
    if (reducedMotion) {
        remove();
        return true;
    }

    splash.classList.add('omni-boot-splash--leaving');
    splash.addEventListener('transitionend', remove, { once: true });
    // A transition that never ends (a hidden tab, a stylesheet without it) must not keep the splash.
    window.setTimeout(remove, 600);
    return true;
}
