// State and helpers shared by the focus modules (omni-focus.js and its menus): the element each
// open surface gives the focus back to, keyed like the surface, and the call to .NET that
// tolerates a lost circuit. The placement and the press outside are in ../shared/.

export const returnTargets = new Map();

// The last element that really held the focus. A surface that opens over another one (a dialog opened
// from a dialog) can find the focus already pulled to the body, because the lower surface was made
// inert a moment earlier: the opener is then the element that held it just before.
let lastFocused = null;
if (typeof document !== 'undefined') {
    document.addEventListener('focusin', event => {
        if (event.target instanceof HTMLElement && event.target !== document.body) {
            lastFocused = event.target;
        }
    }, true);
}

export function rememberTarget(key) {
    if (returnTargets.has(key)) {
        return;
    }

    const active = document.activeElement;
    if (active instanceof HTMLElement && active !== document.body) {
        returnTargets.set(key, active);
    } else if (lastFocused?.isConnected) {
        returnTargets.set(key, lastFocused);
    } else if (active instanceof HTMLElement) {
        returnTargets.set(key, active);
    }
}

// Tells .NET about an event of the page. The call is rejected when the circuit is gone or the
// component already released, which leaves nobody to tell: nothing to report, nothing unhandled.
export function notify(dotnet, method, ...args) {
    try {
        dotnet.invokeMethodAsync(method, ...args).catch(() => { });
    } catch {
        // The reference itself is already disposed.
    }
}
