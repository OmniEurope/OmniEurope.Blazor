// The label of OmniStatTile stays on one line (recette R1-15): when it does not fit its tile, the
// module marks it data-fit="shrink", which the stylesheet writes in a smaller size (0.6875rem, the
// readable floor); when it still does not fit, data-fit="clip": the stylesheet cuts it with an ellipsis
// and the module lends the whole text to its title, so the hover tooltip reads it. It is measured again
// when the tile is resized and when its text changes. The module toggles attributes only; it never
// writes a style. The title is an attribute Blazor never renders on the label, so Blazor leaves it.
const tiles = new WeakMap();

function fit(tile) {
    const label = tile.querySelector('.omni-stat-tile__label');
    if (!label) {
        return;
    }

    const overflows = () => label.scrollWidth > label.clientWidth + 1;
    label.removeAttribute('data-fit');
    if (overflows()) {
        label.setAttribute('data-fit', 'shrink');
        if (overflows()) {
            label.setAttribute('data-fit', 'clip');
        }
    }

    const clipped = label.getAttribute('data-fit') === 'clip';
    if (clipped) {
        label.setAttribute('title', label.textContent.trim());
    } else {
        label.removeAttribute('title');
    }
}

export function attach(tile) {
    if (!tile || tiles.has(tile)) {
        return;
    }

    let queued = false;
    const queue = () => {
        if (queued) {
            return;
        }

        queued = true;
        requestAnimationFrame(() => {
            queued = false;
            if (tile.isConnected) {
                fit(tile);
            }
        });
    };

    const resize = typeof ResizeObserver === 'function' ? new ResizeObserver(queue) : null;
    const mutations = new MutationObserver(queue);
    resize?.observe(tile);
    mutations.observe(tile, { childList: true, subtree: true, characterData: true });
    tiles.set(tile, { resize, mutations });
    fit(tile);
}

export function detach(tile) {
    const state = tile ? tiles.get(tile) : undefined;
    if (!state) {
        return;
    }

    state.resize?.disconnect();
    state.mutations.disconnect();
    tiles.delete(tile);
}
