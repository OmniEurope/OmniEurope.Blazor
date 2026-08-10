export function focusFirstInvalid(root) {
    const invalid = root?.querySelector?.('[aria-invalid="true"], .invalid');
    if (invalid instanceof HTMLElement) {
        invalid.focus({ preventScroll: true });
        invalid.scrollIntoView({ block: 'center', behavior: 'smooth' });
    }
}
