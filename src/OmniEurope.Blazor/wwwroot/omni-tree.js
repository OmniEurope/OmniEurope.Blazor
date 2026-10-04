// Drag and drop of OmniTree items. .NET keeps the state (the item carried, the target, the drop); this
// module only fills in the drag data some browsers require before they start a drag, and asks for a move
// rather than a copy. It writes no style.

const attachments = new Map();

export function attachTreeDrag(root) {
    if (!(root instanceof HTMLElement) || attachments.has(root)) {
        return;
    }

    const onDragStart = event => {
        const row = event.target instanceof Element ? event.target.closest('.omni-tree__row[draggable="true"]') : null;
        if (!row || !root.contains(row) || !event.dataTransfer) {
            return;
        }

        event.dataTransfer.effectAllowed = 'move';
        try {
            event.dataTransfer.setData('text/plain', row.textContent?.trim() ?? '');
        } catch {
            // A browser that refuses the data still drags without it.
        }
    };
    root.addEventListener('dragstart', onDragStart);
    attachments.set(root, onDragStart);
}

export function detachTreeDrag(root) {
    const onDragStart = attachments.get(root);
    if (onDragStart) {
        root.removeEventListener('dragstart', onDragStart);
        attachments.delete(root);
    }
}
