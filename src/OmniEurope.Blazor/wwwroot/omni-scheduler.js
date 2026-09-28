// Drag support of OmniScheduler.
//
// The scheduler belongs to .NET, which renders the appointments and decides every move. This module
// only gives each drag of an appointment the data some browsers require before they start one. It
// never writes a style.
const schedulers = new WeakMap();

export function attach(root) {
    if (!root || schedulers.has(root)) {
        return;
    }

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
    };

    root.addEventListener('dragstart', onDragStart);
    schedulers.set(root, { onDragStart });
}

export function detach(root) {
    const state = root ? schedulers.get(root) : undefined;
    if (!state) {
        return;
    }

    root.removeEventListener('dragstart', state.onDragStart);
    schedulers.delete(root);
}
