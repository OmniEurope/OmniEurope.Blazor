// Keyboard and drag support of OmniKanban.
//
// The board belongs to .NET, which renders the columns and decides every move. This module only does
// what Razor cannot: it keeps the keys pressed on a control inside a card away from the card (a Razor
// handler on the card would receive them through bubbling, with no way to tell them apart), cancels
// the scroll and the activation those keys would otherwise cause, forwards the card's own keys to .NET,
// moves the focus between cards while none is carried, and gives each drag the data some browsers
// require before they start one. It never writes a style.
const boards = new WeakMap();

const cardsOf = container => Array.from(container.querySelectorAll('[data-omni-kanban-card]'));

const moveFocus = (root, card, key) => {
    const column = card.closest('[data-omni-kanban-column]');
    if (!column) {
        return false;
    }

    if (key === 'ArrowUp' || key === 'ArrowDown') {
        const cards = cardsOf(column);
        const next = cards[cards.indexOf(card) + (key === 'ArrowUp' ? -1 : 1)];
        next?.focus();
        return Boolean(next);
    }

    const columns = Array.from(root.querySelectorAll('[data-omni-kanban-column]'));
    let at = columns.indexOf(column);
    const place = cardsOf(column).indexOf(card);
    // Empty columns are skipped: the focus lands on the nearest column that holds a card.
    for (at += key === 'ArrowLeft' ? -1 : 1; at >= 0 && at < columns.length; at += key === 'ArrowLeft' ? -1 : 1) {
        const cards = cardsOf(columns[at]);
        if (cards.length > 0) {
            cards[Math.min(place, cards.length - 1)].focus();
            return true;
        }
    }

    return false;
};

export function attach(root, dotnet) {
    if (!root || boards.has(root)) {
        return;
    }

    const onKeyDown = event => {
        const card = event.target instanceof Element && event.target.hasAttribute('data-omni-kanban-card') ? event.target : null;
        if (!card || !root.contains(card) || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        const key = event.key;
        const grabbed = root.hasAttribute('data-omni-kanban-grabbed');
        const arrow = key === 'ArrowUp' || key === 'ArrowDown' || key === 'ArrowLeft' || key === 'ArrowRight';
        if (arrow && !grabbed) {
            if (moveFocus(root, card, key)) {
                event.preventDefault();
            }

            return;
        }

        if (key !== ' ' && key !== 'Enter' && !(grabbed && (arrow || key === 'Escape'))) {
            return;
        }

        event.preventDefault();
        try {
            const pending = dotnet.invokeMethodAsync('OnCardKey', card.getAttribute('data-omni-kanban-card') ?? '', key);
            pending?.catch?.(() => { });
        } catch {
            // The component is gone; there is nobody left to tell.
        }
    };

    const onDragStart = event => {
        const card = event.target instanceof Element ? event.target.closest('[data-omni-kanban-card]') : null;
        if (!card || !root.contains(card) || !event.dataTransfer) {
            return;
        }

        event.dataTransfer.effectAllowed = 'move';
        try {
            event.dataTransfer.setData('text/plain', card.getAttribute('data-omni-kanban-card') ?? '');
        } catch {
            // Some browsers refuse data on a drag they did not start; the drag still works without it.
        }
    };

    root.addEventListener('keydown', onKeyDown);
    root.addEventListener('dragstart', onDragStart);
    boards.set(root, { onKeyDown, onDragStart });
}

export function focusCard(root, card) {
    if (!root) {
        return;
    }

    for (const element of root.querySelectorAll('[data-omni-kanban-card]')) {
        if (element.getAttribute('data-omni-kanban-card') === card) {
            element.focus();
            element.scrollIntoView({ block: 'nearest', inline: 'nearest' });
            return;
        }
    }
}

export function detach(root) {
    const state = root ? boards.get(root) : undefined;
    if (!state) {
        return;
    }

    root.removeEventListener('keydown', state.onKeyDown);
    root.removeEventListener('dragstart', state.onDragStart);
    boards.delete(root);
}
