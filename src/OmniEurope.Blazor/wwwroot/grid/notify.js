// Shared by the grid modules (omni-grid.js and ./grid/*).
import { circuitDown } from '../shared/notify.js';

/**
 * Calls .NET for an attachment that may be detached before the call lands: the grid disposes its
 * reference right after detaching, so a notification already in flight (an animation frame that fired,
 * a resize released just before navigation) rejects with "no tracked object". That rejection only
 * means the grid is gone and is dropped; while the attachment is live, a failure still surfaces.
 * While Blazor Server shows its connection as lost the call is not made at all: it could not arrive,
 * and Blazor would report its failed send as an uncaught rejection, out of reach of the catch below.
 */
export function notifyDotNet(isLive, reference, method, ...args) {
    if (circuitDown()) {
        return;
    }

    reference.invokeMethodAsync(method, ...args).catch(error => {
        if (isLive()) {
            throw error;
        }
    });
}
