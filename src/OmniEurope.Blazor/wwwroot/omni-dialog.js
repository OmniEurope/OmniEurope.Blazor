const attached = new WeakMap();

export function attach(dialog) {
    if (!dialog || attached.has(dialog)) return;
    const handle = dialog.querySelector('[data-omni-dialog-drag-handle]');
    if (!handle) return;

    const onPointerDown = event => {
        if (event.button !== 0 || event.target.closest('button, a, input, select, textarea')) return;
        const startX = event.clientX;
        const startY = event.clientY;
        const matrix = new DOMMatrixReadOnly(getComputedStyle(dialog).transform);
        const originX = matrix.m41;
        const originY = matrix.m42;
        handle.setPointerCapture(event.pointerId);

        const onMove = move => {
            const bounds = dialog.getBoundingClientRect();
            const nextX = originX + move.clientX - startX;
            const nextY = originY + move.clientY - startY;
            const limitedX = Math.min(Math.max(nextX, originX - bounds.left + 8), originX + innerWidth - bounds.right - 8);
            const limitedY = Math.min(Math.max(nextY, originY - bounds.top + 8), originY + innerHeight - bounds.bottom - 8);
            dialog.style.transform = `translate(${limitedX}px, ${limitedY}px)`;
        };
        const onUp = () => {
            handle.removeEventListener('pointermove', onMove);
            handle.removeEventListener('pointerup', onUp);
            handle.removeEventListener('pointercancel', onUp);
        };
        handle.addEventListener('pointermove', onMove);
        handle.addEventListener('pointerup', onUp);
        handle.addEventListener('pointercancel', onUp);
    };

    handle.addEventListener('pointerdown', onPointerDown);
    attached.set(dialog, { handle, onPointerDown });
}

export function detach(dialog) {
    const state = attached.get(dialog);
    if (!state) return;
    state.handle.removeEventListener('pointerdown', state.onPointerDown);
    attached.delete(dialog);
}
