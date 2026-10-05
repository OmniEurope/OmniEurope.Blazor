const attached = new WeakMap();
// A Map, not a WeakMap: a dialog that closes has already left the page when detach runs, and its
// reference then arrives as null, so the locks of dialogs gone from the page are found by a sweep.
const scaleLocks = new Map();
// What the freeze wrote on each element, with the inline value it replaced.
const frozen = new WeakMap();

const frozenProperties = ['font-size', 'line-height', 'letter-spacing', 'width', 'height',
    'min-width', 'min-height', 'padding-top', 'padding-right', 'padding-bottom', 'padding-left',
    'margin-top', 'margin-right', 'margin-bottom', 'margin-left', 'row-gap', 'column-gap',
    'border-top-width', 'border-right-width', 'border-bottom-width', 'border-left-width',
    // The tracks of a grid: a template written in rem (repeat(auto-fit, minmax(19rem, 1fr))) counts its
    // columns again when the root size changes, and every row of the window would move.
    'grid-template-columns'];

// Capture computed geometry before writing anything: rem lengths and density tokens are
// resolved once, so mixed pixel/rem rules cannot drift when the application scale changes.
function freezeElements(elements) {
    const snapshots = elements.map(element => {
        const computed = getComputedStyle(element);
        return [element, frozenProperties.map(name => [name, computed.getPropertyValue(name)])];
    });
    for (const [element, values] of snapshots) {
        const replaced = frozen.get(element) ?? new Map();
        for (const [name, value] of values) {
            if (!value.endsWith('px')) continue;
            if (!replaced.has(name)) replaced.set(name, element.style.getPropertyValue(name));
            element.style.setProperty(name, value);
        }
        frozen.set(element, replaced);
    }
}

function thawElements(elements) {
    for (const element of elements) {
        const replaced = frozen.get(element);
        if (!replaced) continue;
        for (const [name, value] of replaced) {
            if (value) element.style.setProperty(name, value);
            else element.style.removeProperty(name);
        }
        frozen.delete(element);
    }
}

export function freezeScale(dialog) {
    if (!dialog || scaleLocks.has(dialog)) return;
    freezeElements([dialog, ...dialog.querySelectorAll('*')]);
    // A row may appear or leave after the freeze (a setting only some themes offer). The boxes around
    // it then give their frozen block size back, so what follows moves instead of being overlapped or
    // leaving a hole; their content stays frozen, and a new row is frozen as it arrives.
    const observer = new MutationObserver(records => {
        const arrived = [];
        for (const record of records) {
            for (let box = record.target; box instanceof Element && dialog.contains(box); box = box.parentElement) {
                box.style.removeProperty('height');
            }
            for (const node of record.addedNodes) {
                if (node instanceof Element) arrived.push(node, ...node.querySelectorAll('*'));
            }
        }
        if (arrived.length > 0) freezeElements(arrived);
    });
    observer.observe(dialog, { childList: true, subtree: true });
    // The freeze holds against the scale and the density, which live on the document root and on an
    // attribute of the scope. A new look (theme, palette, font) rewrites the tokens on the style of a
    // theme scope: labels change face, case and tracking, and a width frozen for the old look would
    // clip them. The measures are then taken again under the new look, and once more when a web font
    // the look asked for has arrived.
    const looks = new Map();
    const retake = () => {
        if (!dialog.isConnected) return;
        const all = [dialog, ...dialog.querySelectorAll('*')];
        thawElements(all);
        freezeElements(all);
    };
    const look = new MutationObserver(() => {
        let changed = false;
        for (const [scope, seen] of looks) {
            const now = scope.style.cssText;
            changed ||= now !== seen;
            looks.set(scope, now);
        }
        if (!changed) return;
        retake();
        if (document.fonts?.status === 'loading') document.fonts.ready.then(retake);
    });
    for (let scope = dialog.parentElement?.closest('[data-omni-theme]'); scope; scope = scope.parentElement?.closest('[data-omni-theme]')) {
        looks.set(scope, scope.style.cssText);
        look.observe(scope, { attributes: true, attributeFilter: ['style'] });
    }
    scaleLocks.set(dialog, { disconnect() { observer.disconnect(); look.disconnect(); } });
}

export function attach(dialog) {
    if (!dialog || attached.has(dialog)) return;
    const handle = dialog.querySelector('[data-omni-dialog-drag-handle]');
    if (!handle) return;

    const onPointerDown = event => {
        if (event.button !== 0 || event.target.closest('button, a, input, select, textarea')) return;
        const startX = event.clientX;
        const startY = event.clientY;
        // The dialog is moved by its relative offsets, not by a transform: a transform would make it the
        // origin of every position: fixed surface it holds (a calendar, a filter panel, a tooltip), which
        // would then be misplaced and clipped by it.
        const originX = parseFloat(dialog.style.left) || 0;
        const originY = parseFloat(dialog.style.top) || 0;
        // The limits are the room around the dialog where the gesture starts, read once. Read again at
        // every move, the box has already travelled: the room left shrinks as the dialog advances and it
        // stops half way, against an invisible wall.
        const bounds = dialog.getBoundingClientRect();
        const minX = originX - bounds.left + 8;
        const maxX = originX + innerWidth - bounds.right - 8;
        const minY = originY - bounds.top + 8;
        const maxY = originY + innerHeight - bounds.bottom - 8;
        handle.setPointerCapture(event.pointerId);

        const onMove = move => {
            const nextX = originX + move.clientX - startX;
            const nextY = originY + move.clientY - startY;
            const limitedX = Math.min(Math.max(nextX, minX), Math.max(minX, maxX));
            const limitedY = Math.min(Math.max(nextY, minY), Math.max(minY, maxY));
            dialog.style.left = `${limitedX}px`;
            dialog.style.top = `${limitedY}px`;
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
    for (const [locked, lock] of scaleLocks) {
        if (locked === dialog || !locked.isConnected) {
            lock.disconnect();
            scaleLocks.delete(locked);
        }
    }
    const state = attached.get(dialog);
    if (!state) return;
    state.handle.removeEventListener('pointerdown', state.onPointerDown);
    attached.delete(dialog);
}

// OmniDialog.Width: the free width, checked on the server to be a number and a unit, written as a custom
// property through the CSSOM (never a style attribute). The ready mark ends the transparent wait of
// .omni-dialog--width, so the dialog appears at its own width instead of jumping from the default one.
export function setWidth(dialog, width) {
    if (!dialog) return;
    dialog.style.setProperty('--omni-dialog-width', width);
    dialog.setAttribute('data-omni-dialog-width-ready', '');
}

export function clearWidth(dialog) {
    if (!dialog) return;
    dialog.style.removeProperty('--omni-dialog-width');
    dialog.removeAttribute('data-omni-dialog-width-ready');
}
