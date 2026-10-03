// The toolbar of the HTML editor held to a number of rows (OmniHtmlEditor.ToolbarRows, recette
// R-042). Every command is rendered in the bar; this script hides, from the end, the buttons that would
// open a row beyond the limit (data-omni-overflow, an attribute .NET never renders, so a render never
// undoes it) and reports their names to .NET, which lists them in the "more" menu at the end of the bar.
// The lists (block, size, case) always stay in the bar. The bar is fitted again when its width changes,
// and after a render that changed what decides its rows (the commands, their texts, the font, the
// control height); a render that changed none of them (a keystroke) measures nothing.

const fits = new Map();

// A row is a distinct top edge: offsetTop would be measured from each group (positioned), not the bar.
const rowsOf = items => new Set(items.map(item => Math.round(item.getBoundingClientRect().top))).size;

// What decides how many rows the bar takes, besides its width (followed by the observer).
function layoutOf(state) {
    const { toolbar } = state;
    const style = getComputedStyle(toolbar);
    const commands = [...toolbar.querySelectorAll('[data-command]')].map(control => control.getAttribute('data-command')).join(',');
    return [state.rows, toolbar.clientWidth, style.font, style.getPropertyValue('--omni-control-height'), toolbar.textContent.length, commands, state.more?.getAttribute('data-omni-fixed') ?? ''].join('|');
}

function fit(state) {
    const { toolbar, more } = state;
    if (!toolbar.isConnected) {
        unfit(toolbar);
        return;
    }

    state.layout = layoutOf(state);

    const buttons = [...toolbar.querySelectorAll('.omni-html-editor__group > button[data-command]')];
    for (const button of buttons) {
        button.removeAttribute('data-omni-overflow');
    }
    more?.removeAttribute('data-omni-overflow');

    const shown = () => [...toolbar.querySelectorAll('.omni-html-editor__group > [data-command]:not([data-omni-overflow])'), ...(more && !more.hasAttribute('data-omni-overflow') ? [more] : [])];
    const hidden = [];
    // Without commands placed in the menu by their author, the button of the menu shows only when
    // something has to go there: first try the bar without it.
    const fixed = Number.parseInt(more?.getAttribute('data-omni-fixed') ?? '0', 10) > 0;
    if (more && !fixed) {
        more.setAttribute('data-omni-overflow', '');
        if (rowsOf(shown()) <= state.rows) {
            report(state, hidden);
            return;
        }

        more.removeAttribute('data-omni-overflow');
    }

    for (let index = buttons.length - 1; index >= 0 && rowsOf(shown()) > state.rows; index--) {
        buttons[index].setAttribute('data-omni-overflow', '');
        hidden.unshift(buttons[index].getAttribute('data-command'));
    }

    report(state, hidden);
}

function report(state, hidden) {
    const key = hidden.join('\n');
    if (key !== state.reported) {
        state.reported = key;
        state.dotnet.invokeMethodAsync('OnToolbarOverflow', hidden).catch(() => { });
    }
}

/**
 * Holds the toolbar to `rows` rows and reports the commands moved to the menu; called after each render
 * of the editor. A first call starts following the width of the toolbar.
 */
export function fitToolbar(toolbar, rows, dotnet) {
    if (!(toolbar instanceof HTMLElement) || !dotnet) {
        return;
    }

    let state = fits.get(toolbar);
    if (!state) {
        state = { toolbar, dotnet, rows, reported: null, more: null, observer: null, frame: 0, layout: null };
        const schedule = () => {
            if (state.frame === 0) {
                state.frame = window.requestAnimationFrame(() => {
                    state.frame = 0;
                    fit(state);
                });
            }
        };
        state.observer = typeof ResizeObserver === 'function' ? new ResizeObserver(schedule) : null;
        state.observer?.observe(toolbar);
        fits.set(toolbar, state);
    }

    state.rows = Math.max(1, rows);
    state.dotnet = dotnet;
    state.more = toolbar.querySelector('.omni-html-editor__more');
    if (state.layout !== layoutOf(state)) {
        fit(state);
    }
}

/** Stops following the toolbar and shows every button again. */
export function unfit(toolbar) {
    const state = fits.get(toolbar);
    if (!state) {
        return;
    }

    state.observer?.disconnect();
    if (state.frame !== 0) {
        window.cancelAnimationFrame(state.frame);
    }
    for (const element of toolbar.querySelectorAll('[data-omni-overflow]')) {
        element.removeAttribute('data-omni-overflow');
    }
    fits.delete(toolbar);
}
