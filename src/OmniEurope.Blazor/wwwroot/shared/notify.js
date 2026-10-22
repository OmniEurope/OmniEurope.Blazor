// The one call from the page to .NET that tolerates a lost circuit (the scripts of the focus engine,
// the HTML editor, ...).

// Tells .NET about an event of the page. The call is rejected when the circuit is gone or the
// component already released, which leaves nobody to tell: nothing to report, nothing unhandled.
export function notify(dotnet, method, ...args) {
    try {
        dotnet.invokeMethodAsync(method, ...args).catch(() => { });
    } catch {
        // The reference itself is already disposed.
    }
}
