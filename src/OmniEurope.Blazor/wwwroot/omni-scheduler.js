// Drag support of OmniScheduler.
//
// The scheduler belongs to .NET, which renders the appointments and decides every move. This module
// gives each drag of an appointment the data some browsers require before they start one, and marks the
// root with data-omni-scheduler-carrying once the drag runs, never during its dragstart: the stylesheet
// lets the pointer through the blocks only under that mark. Chromium abandons a drag whose source stops
// being hit-testable while the drag starts, and .NET renders its dragging class during that dragstart.
// It never writes a style.
const schedulers = new WeakMap();
const carrying = 'data-omni-scheduler-carrying';

export function attach(root) {
    if (!root || schedulers.has(root)) {
        return;
    }

    const state = { drag: 0 };

    const stopCarrying = () => {
        // A later tick of a drag already over must not mark the root.
        state.drag++;
        root.removeAttribute(carrying);
        document.removeEventListener('dragend', stopCarrying, true);
        document.removeEventListener('drop', stopCarrying, true);
    };

    const onDragStart = event => {
        const appointment = event.target instanceof Element ? event.target.closest('[data-omni-scheduler-appointment]') : null;
        if (!appointment || !root.contains(appointment) || !event.dataTransfer) {
            return;
        }

        event.dataTransfer.effectAllowed = 'move';
        try {
            event.dataTransfer.setData('text/plain', appointment.getAttribute('data-omni-scheduler-appointment') ?? '');
        } catch {
            // Some browsers refuse data on a drag they did not start; the drag still works without it.
        }

        stopCarrying();
        // The drag ends on the document whatever became of the block, even one .NET rendered away.
        document.addEventListener('dragend', stopCarrying, true);
        document.addEventListener('drop', stopCarrying, true);
        const drag = state.drag;
        setTimeout(() => {
            if (state.drag === drag) {
                root.setAttribute(carrying, '');
            }
        }, 0);
    };

    state.onDragStart = onDragStart;
    state.stopCarrying = stopCarrying;
    root.addEventListener('dragstart', onDragStart);
    schedulers.set(root, state);
}

export function detach(root) {
    const state = root ? schedulers.get(root) : undefined;
    if (!state) {
        return;
    }

    state.stopCarrying();
    root.removeEventListener('dragstart', state.onDragStart);
    schedulers.delete(root);
}
