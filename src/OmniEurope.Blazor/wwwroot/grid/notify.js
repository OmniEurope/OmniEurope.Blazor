// Shared by the grid modules (omni-grid.js and ./grid/*).

/**
 * Calls .NET for an attachment that may be detached before the call lands: the grid disposes its
 * reference right after detaching, so a notification already in flight (an animation frame that fired,
 * a resize released just before navigation) rejects with "no tracked object". That rejection only
 * means the grid is gone and is dropped; while the attachment is live, a failure still surfaces.
 */
export function notifyDotNet(isLive, reference, method, ...args) {
    reference.invokeMethodAsync(method, ...args).catch(error => {
        if (isLive()) {
            throw error;
        }
    });
}
