// The label and the value of OmniStatTile are fitted to the tile. The label stays on one line (recette R1-15): when it does not fit its tile, the
// module marks it data-fit="shrink", which the stylesheet writes in a smaller size (0.6875rem, the
// readable floor); when it still does not fit, data-fit="clip": the stylesheet cuts it with an ellipsis
// and the module lends the whole text to its title, so the hover tooltip reads it. It is measured again
// when the tile is resized and when its text or its class changes. A tile with WrapTitle (the class
// omni-stat-tile--wrap) lets the label wrap and is left alone. The module toggles attributes only; it never
// writes a style. The title is an attribute Blazor never renders on the label, so Blazor leaves it.
const tiles = new WeakMap();

// The value of the tile is fitted the same way (recette R-25), but never cut: data-fit="shrink-1", then
// "shrink-2", then "shrink-3" (the stylesheet writes each in a smaller size, down to its floor), and when the
// text still overflows at the floor, data-fit="wrap" (the stylesheet lets it wrap). No tooltip: nothing is cut.
const valueSteps = ['shrink-1', 'shrink-2', 'shrink-3', 'wrap'];

// No tolerance, unlike the label: the value is clipped, not ellipsed, and a pixel of slack let a text that overflows
// its box by more than a pixel keep its size, its last glyph cut.
function fitValue(value) {
    value.removeAttribute('data-fit');
    for (const step of valueSteps) {
        if (value.scrollWidth <= value.clientWidth) {
            return;
        }

        value.setAttribute('data-fit', step);
    }
}

function fit(tile) {
    const value = tile.querySelector('.omni-stat-tile__value');
    if (value) {
        fitValue(value);
    }

    const label = tile.querySelector('.omni-stat-tile__label');
    if (!label) {
        return;
    }

    label.removeAttribute('data-fit');
    // A tile that lets its title wrap (WrapTitle) has nothing to fit: no mark, no tooltip.
    if (tile.classList.contains('omni-stat-tile--wrap')) {
        label.removeAttribute('title');
        return;
    }

    const overflows = () => label.scrollWidth > label.clientWidth + 1;
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
    // The class of the tile is watched too: WrapTitle can be switched while the tile is on the page.
    mutations.observe(tile, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['class'] });
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
