// The one call from the page to .NET that tolerates a lost circuit (the scripts of the focus engine,
// the HTML editor, the preferences watcher, ...).

// The element Blazor Server shows while its connection is lost (the reconnection options' default id):
// either the host's own element, whose state classes ASP.NET Core documents, or, when the host has
// none, the default display Blazor adds itself, an open shadow root holding its overlay.
const ReconnectId = 'components-reconnect-modal';
const DownClasses = [
    'components-reconnect-show',
    'components-reconnect-retrying',
    'components-reconnect-failed',
    'components-reconnect-paused',
    'components-reconnect-resume-failed',
    'components-reconnect-rejected',
    // The overlay of the default display while it is shown.
    'components-reconnect-visible'
];

function reconnectIndicator() {
    const host = globalThis.document?.getElementById?.(ReconnectId);
    if (!host) {
        return null;
    }

    return host.shadowRoot?.querySelector('.components-reconnect-overlay') ?? host;
}

// True while Blazor Server shows its connection as lost. A call made then never reaches .NET, and the
// send Blazor attempts is rejected inside its own script, out of reach of any catch here: the browser
// reports it as an uncaught rejection. WebAssembly and Hybrid hosts have no such element: never down.
export function circuitDown() {
    const indicator = reconnectIndicator();
    return indicator !== null && DownClasses.some(name => indicator.classList.contains(name));
}

// Runs the callback once the connection is back: at once when it is not down, otherwise when the
// reconnection display leaves its lost state. A connection never restored (the page reloads) never runs
// it. Returns a function that stops waiting.
export function whenCircuitBack(callback) {
    const indicator = reconnectIndicator();
    if (indicator === null || !circuitDown()) {
        callback();
        return () => { };
    }

    const observer = new MutationObserver(() => {
        if (!circuitDown()) {
            observer.disconnect();
            callback();
        }
    });
    observer.observe(indicator, { attributes: true, attributeFilter: ['class'] });
    return () => observer.disconnect();
}

// Tells .NET about an event of the page. While the connection is lost the call is not made at all (it
// could not arrive, and Blazor would report its failed send as uncaught); otherwise it is rejected when the
// circuit is gone or the component already released, which leaves nobody to tell: nothing to report,
// nothing unhandled. Returns true when the call was made.
export function notify(dotnet, method, ...args) {
    if (circuitDown()) {
        return false;
    }

    try {
        dotnet.invokeMethodAsync(method, ...args).catch(() => { });
    } catch {
        // The reference itself is already disposed.
    }

    return true;
}
