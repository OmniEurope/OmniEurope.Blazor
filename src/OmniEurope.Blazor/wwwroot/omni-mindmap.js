// Pointer gestures of OmniMindMap. The document belongs to .NET, which renders the whole drawing;
// this module only moves what is under the pointer while a gesture lasts, then reports its outcome
// (where the nodes were dropped, where the pan stopped, what the lasso enclosed) through the bridge.
// It never writes a style: geometry goes through SVG attributes, visual state through the classes
// .NET renders, so the canvas runs under `style-src 'self'`.

const MIN_ZOOM = 0.1;
const MAX_ZOOM = 5;
const WHEEL_IN = 1.08;
const WHEEL_OUT = 0.92;
const DRAG_THRESHOLD = 3;
const LONG_PRESS_MS = 600;
const VIEW_REPORT_MS = 120;
const KEYBOARD_MENU_MS = 800;

const states = new WeakMap();

const clampZoom = zoom => Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom));

// Same rounding as MindMapGeometry.Format, so an attribute written here reads the way the next
// render from .NET writes it.
const format = value => String(Math.round(value * 1000) / 1000);

// Mirror of MindMapGeometry.EdgePath: a cubic curve leaving and reaching each node horizontally.
const edgePath = (fromX, fromY, toX, toY) => {
    const dx = toX - fromX;
    return `M ${format(fromX)} ${format(fromY)} C ${format(fromX + (dx * 0.4))} ${format(fromY)}, ${format(fromX + (dx * 0.6))} ${format(toY)}, ${format(toX)} ${format(toY)}`;
};

const notify = (state, method, ...args) => {
    try {
        const pending = state.bridge.invokeMethodAsync(method, ...args);
        if (pending && typeof pending.catch === 'function') {
            pending.catch(() => { });
        }
    } catch {
        // The component is gone and its reference with it; there is nobody left to tell.
    }
};

const viewport = canvas => canvas.querySelector('.omni-mindmap__viewport');

const readView = canvas => {
    const group = viewport(canvas);
    const number = (name, fallback) => {
        const value = Number.parseFloat(group?.getAttribute(name) ?? '');
        return Number.isFinite(value) ? value : fallback;
    };
    return { panX: number('data-pan-x', 0), panY: number('data-pan-y', 0), zoom: number('data-zoom', 1) };
};

const writeView = (canvas, view) => {
    const group = viewport(canvas);
    if (!group) {
        return;
    }

    group.setAttribute('transform', `translate(${format(view.panX)} ${format(view.panY)}) scale(${format(view.zoom)})`);
    group.setAttribute('data-pan-x', format(view.panX));
    group.setAttribute('data-pan-y', format(view.panY));
    group.setAttribute('data-zoom', format(view.zoom));
};

const canvasPoint = (canvas, event) => {
    const box = canvas.getBoundingClientRect();
    return { x: event.clientX - box.left, y: event.clientY - box.top };
};

const mapPoint = (canvas, event) => {
    const point = canvasPoint(canvas, event);
    const view = readView(canvas);
    return { x: (point.x - view.panX) / view.zoom, y: (point.y - view.panY) / view.zoom };
};

const closestIn = (canvas, target, selector) => {
    const element = target instanceof Element ? target.closest(selector) : null;
    return element && canvas.contains(element) ? element : null;
};

const menuOf = canvas => canvas.querySelector('.omni-mindmap__menu');

const isReadOnly = canvas => canvas.getAttribute('data-omni-readonly') === 'true';

const isLinking = canvas => canvas.getAttribute('data-omni-linking') === 'true';

const nodeElement = (canvas, id) => {
    for (const element of canvas.querySelectorAll('[data-omni-node]')) {
        if (element.getAttribute('data-omni-node') === id) {
            return element;
        }
    }

    return null;
};

const nodePosition = element => ({
    x: Number.parseFloat(element.getAttribute('data-x') ?? '0') || 0,
    y: Number.parseFloat(element.getAttribute('data-y') ?? '0') || 0
});

const placeNode = (element, x, y) => {
    element.setAttribute('transform', `translate(${format(x)} ${format(y)})`);
    element.setAttribute('data-x', format(x));
    element.setAttribute('data-y', format(y));
};

// Redraws the links that touch a moving node, reading the ends from the node attributes so a link
// between two moving nodes follows both.
const redrawEdges = (canvas, moving) => {
    for (const edge of canvas.querySelectorAll('[data-omni-edge]')) {
        const from = edge.getAttribute('data-from');
        const to = edge.getAttribute('data-to');
        if (!moving.has(from) && !moving.has(to)) {
            continue;
        }

        const fromElement = nodeElement(canvas, from);
        const toElement = nodeElement(canvas, to);
        if (!fromElement || !toElement) {
            continue;
        }

        const start = nodePosition(fromElement);
        const end = nodePosition(toElement);
        const path = edgePath(start.x, start.y, end.x, end.y);
        for (const line of edge.querySelectorAll('path')) {
            line.setAttribute('d', path);
        }
    }
};

const lassoOf = canvas => canvas.querySelector('.omni-mindmap__lasso');

const drawLasso = (canvas, from, to) => {
    const lasso = lassoOf(canvas);
    if (!lasso) {
        return;
    }

    lasso.setAttribute('x', format(Math.min(from.x, to.x)));
    lasso.setAttribute('y', format(Math.min(from.y, to.y)));
    lasso.setAttribute('width', format(Math.abs(to.x - from.x)));
    lasso.setAttribute('height', format(Math.abs(to.y - from.y)));
    lasso.setAttribute('visibility', 'visible');
};

const hideLasso = canvas => {
    const lasso = lassoOf(canvas);
    if (lasso) {
        lasso.setAttribute('visibility', 'hidden');
        lasso.setAttribute('width', '0');
        lasso.setAttribute('height', '0');
    }
};

const setGestureClass = (canvas, name) => {
    for (const gesture of ['panning', 'dragging', 'selecting']) {
        canvas.classList.toggle(`omni-mindmap__canvas--${gesture}`, gesture === name);
    }
};

// A view changed by the wheel or a pinch is reported once it settles rather than on every tick, so
// a fast scroll is one round trip instead of dozens.
const reportViewSoon = state => {
    window.clearTimeout(state.viewTimer);
    state.viewTimer = window.setTimeout(() => reportView(state), VIEW_REPORT_MS);
};

const reportView = state => {
    window.clearTimeout(state.viewTimer);
    state.viewTimer = 0;
    const view = readView(state.canvas);
    notify(state, 'ViewChanged', view.panX, view.panY, view.zoom);
};

const zoomAt = (canvas, point, factor) => {
    const view = readView(canvas);
    const zoom = clampZoom(view.zoom * factor);
    const ratio = zoom / view.zoom;
    writeView(canvas, {
        panX: point.x - ((point.x - view.panX) * ratio),
        panY: point.y - ((point.y - view.panY) * ratio),
        zoom
    });
};

// The pointer is captured only once a gesture really moves: a capture taken on the press would
// retarget the click and double click that follow it to the canvas, and a double click on a node
// would then read as one on the background.
const capture = (canvas, pointerId) => {
    try {
        canvas.setPointerCapture(pointerId);
    } catch {
        // A pointer that is already gone cannot be captured; the gesture still ends on its own.
    }
};

const cancelLongPress = state => {
    if (state.longPress) {
        window.clearTimeout(state.longPress);
        state.longPress = 0;
    }
};

const endGesture = state => {
    const { canvas } = state;
    state.gesture = null;
    hideLasso(canvas);
    setGestureClass(canvas, null);
};

const requestMenu = (state, target, event) => {
    const { canvas } = state;
    const node = closestIn(canvas, target, '[data-omni-node]');
    const point = canvasPoint(canvas, event);
    const map = mapPoint(canvas, event);
    notify(state, 'ContextMenuRequested', node ? node.getAttribute('data-omni-node') : null, point.x, point.y, map.x, map.y);
};

const startPinch = state => {
    const [first, second] = [...state.pointers.values()];
    state.gesture = {
        kind: 'pinch',
        distance: Math.hypot(first.x - second.x, first.y - second.y),
        middle: { x: (first.x + second.x) / 2, y: (first.y + second.y) / 2 }
    };
    hideLasso(state.canvas);
    setGestureClass(state.canvas, 'panning');
};

const onPointerDown = (state, event) => {
    const { canvas } = state;
    if (menuOf(canvas)?.contains(event.target)) {
        return;
    }

    // The forward and back buttons would otherwise navigate away from the page being edited.
    if (event.button === 3 || event.button === 4) {
        event.preventDefault();
        return;
    }

    if (event.pointerType === 'touch') {
        state.pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        if (state.pointers.size === 2) {
            cancelLongPress(state);
            startPinch(state);
            return;
        }

        if (state.pointers.size > 2) {
            return;
        }
    }

    if (event.button !== 0 && event.button !== 1) {
        return;
    }

    // A mouse press focuses the canvas natively, which keeps the keyboard ring for keyboard users
    // only. A touch or pen press does not always move the focus, so it is asked for, ring hidden.
    if (event.button === 1) {
        event.preventDefault();
    }

    if (event.pointerType !== 'mouse' && document.activeElement !== canvas) {
        canvas.focus({ preventScroll: true, focusVisible: false });
    }
    const start = canvasPoint(canvas, event);

    // The middle button pans wherever it is pressed, over a node or not.
    if (event.button === 1) {
        state.gesture = { kind: 'pan', pointerId: event.pointerId, start, last: start, moved: false };
        setGestureClass(canvas, 'panning');
        return;
    }

    if (event.pointerType === 'touch') {
        state.longPress = window.setTimeout(() => {
            state.longPress = 0;
            endGesture(state);
            requestMenu(state, event.target, event);
        }, LONG_PRESS_MS);
    }

    const node = closestIn(canvas, event.target, '[data-omni-node]');
    if (node) {
        const id = node.getAttribute('data-omni-node');
        if (isLinking(canvas) || isReadOnly(canvas)) {
            notify(state, 'NodePressed', id);
            return;
        }

        const selected = [...canvas.querySelectorAll('[data-omni-node][data-omni-selected="true"]')];
        const group = selected.length > 1 && selected.includes(node);
        const moving = group ? selected : [node];
        if (!group) {
            notify(state, 'NodePressed', id);
        }

        state.gesture = {
            kind: 'drag',
            pointerId: event.pointerId,
            pressed: id,
            group,
            start,
            zoom: readView(canvas).zoom,
            origins: new Map(moving.map(element => [element.getAttribute('data-omni-node'), { element, ...nodePosition(element) }])),
            moved: false
        };
        return;
    }

    const edge = closestIn(canvas, event.target, '[data-omni-edge]');
    if (edge) {
        notify(state, 'EdgePressed', Number.parseInt(edge.getAttribute('data-omni-edge') ?? '-1', 10));
        return;
    }

    notify(state, 'BackgroundPressed');
    if (event.pointerType === 'touch') {
        state.gesture = { kind: 'pan', pointerId: event.pointerId, start, last: start, moved: false };
        setGestureClass(canvas, 'panning');
    } else {
        state.gesture = { kind: 'lasso', pointerId: event.pointerId, start, moved: false };
        setGestureClass(canvas, 'selecting');
    }
};

const onPointerMove = (state, event) => {
    const { canvas } = state;
    if (state.pointers.has(event.pointerId)) {
        state.pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
    }

    const gesture = state.gesture;
    if (!gesture) {
        return;
    }

    if (gesture.kind === 'pinch') {
        if (state.pointers.size < 2) {
            return;
        }

        const [first, second] = [...state.pointers.values()];
        const distance = Math.hypot(first.x - second.x, first.y - second.y);
        const middle = { x: (first.x + second.x) / 2, y: (first.y + second.y) / 2 };
        const box = canvas.getBoundingClientRect();
        if (gesture.distance > 0) {
            zoomAt(canvas, { x: middle.x - box.left, y: middle.y - box.top }, distance / gesture.distance);
        }

        const view = readView(canvas);
        writeView(canvas, { panX: view.panX + (middle.x - gesture.middle.x), panY: view.panY + (middle.y - gesture.middle.y), zoom: view.zoom });
        gesture.distance = distance;
        gesture.middle = middle;
        reportViewSoon(state);
        return;
    }

    if (event.pointerId !== gesture.pointerId) {
        return;
    }

    const point = canvasPoint(canvas, event);
    if (!gesture.moved) {
        if (Math.hypot(point.x - gesture.start.x, point.y - gesture.start.y) < DRAG_THRESHOLD) {
            return;
        }

        gesture.moved = true;
        cancelLongPress(state);
        capture(canvas, event.pointerId);
    }

    if (gesture.kind === 'pan') {
        const view = readView(canvas);
        writeView(canvas, { panX: view.panX + (point.x - gesture.last.x), panY: view.panY + (point.y - gesture.last.y), zoom: view.zoom });
        gesture.last = point;
    } else if (gesture.kind === 'drag') {
        setGestureClass(canvas, 'dragging');
        const dx = (point.x - gesture.start.x) / gesture.zoom;
        const dy = (point.y - gesture.start.y) / gesture.zoom;
        for (const origin of gesture.origins.values()) {
            placeNode(origin.element, origin.x + dx, origin.y + dy);
        }

        redrawEdges(canvas, gesture.origins);
    } else if (gesture.kind === 'lasso') {
        drawLasso(canvas, gesture.start, point);
    }
};

const onPointerUp = (state, event) => {
    const { canvas } = state;
    state.pointers.delete(event.pointerId);
    cancelLongPress(state);
    const gesture = state.gesture;
    if (!gesture) {
        return;
    }

    if (gesture.kind === 'pinch') {
        if (state.pointers.size < 2) {
            endGesture(state);
            reportView(state);
        }

        return;
    }

    if (event.pointerId !== gesture.pointerId) {
        return;
    }

    endGesture(state);
    if (event.type === 'pointercancel') {
        return;
    }

    if (gesture.kind === 'pan') {
        if (gesture.moved) {
            reportView(state);
        }
    } else if (gesture.kind === 'drag') {
        if (gesture.moved) {
            const moves = [];
            for (const [id, origin] of gesture.origins) {
                const position = nodePosition(origin.element);
                const x = Math.round(position.x);
                const y = Math.round(position.y);
                placeNode(origin.element, x, y);
                moves.push({ id, x, y });
            }

            redrawEdges(canvas, gesture.origins);
            notify(state, 'NodesMoved', moves);
        } else if (gesture.group) {
            // A plain press on one node of a multiple selection selects that node alone.
            notify(state, 'NodePressed', gesture.pressed);
        }
    } else if (gesture.kind === 'lasso' && gesture.moved) {
        const end = canvasPoint(canvas, event);
        const view = readView(canvas);
        const toMap = point => ({ x: (point.x - view.panX) / view.zoom, y: (point.y - view.panY) / view.zoom });
        const a = toMap(gesture.start);
        const b = toMap(end);
        notify(state, 'LassoSelected', Math.min(a.x, b.x), Math.min(a.y, b.y), Math.max(a.x, b.x), Math.max(a.y, b.y));
    }
};

const onWheel = (state, event) => {
    event.preventDefault();
    zoomAt(state.canvas, canvasPoint(state.canvas, event), event.deltaY > 0 ? WHEEL_OUT : WHEEL_IN);
    reportViewSoon(state);
};

const onDoubleClick = (state, event) => {
    const { canvas } = state;
    const target = document.elementFromPoint(event.clientX, event.clientY) ?? event.target;
    if (menuOf(canvas)?.contains(target)) {
        return;
    }

    const node = closestIn(canvas, target, '[data-omni-node]');
    if (node) {
        notify(state, 'NodeDoubleClicked', node.getAttribute('data-omni-node'));
        return;
    }

    if (closestIn(canvas, target, '[data-omni-edge]')) {
        return;
    }

    const map = mapPoint(canvas, event);
    notify(state, 'CanvasDoubleClicked', map.x, map.y);
};

const onContextMenu = (state, event) => {
    event.preventDefault();
    const { canvas } = state;
    if (menuOf(canvas)?.contains(event.target)) {
        return;
    }

    // The Menu key and Shift+F10 are handled by the component itself, which opens the menu beside
    // the selected node; the contextmenu event the browser raises for them would open it twice.
    if (performance.now() - state.keyboardMenuAt < KEYBOARD_MENU_MS) {
        return;
    }

    endGesture(state);
    requestMenu(state, event.target, event);
};

const MENU_ITEMS = '[role="menuitem"], [role="menuitemradio"]';

const onKeyDown = (state, event) => {
    const { canvas } = state;
    const menu = menuOf(canvas);
    if (menu && menu.contains(event.target)) {
        const items = [...menu.querySelectorAll(MENU_ITEMS)];
        const index = items.indexOf(event.target);
        let next = -1;
        if (event.key === 'ArrowDown' || event.key === 'ArrowRight') {
            next = index < 0 ? 0 : (index + 1) % items.length;
        } else if (event.key === 'ArrowUp' || event.key === 'ArrowLeft') {
            next = index <= 0 ? items.length - 1 : index - 1;
        } else if (event.key === 'Home') {
            next = 0;
        } else if (event.key === 'End') {
            next = items.length - 1;
        }

        if (next >= 0 && items[next]) {
            event.preventDefault();
            items[next].focus({ preventScroll: true });
        }

        return;
    }

    if (event.target !== canvas) {
        return;
    }

    if (event.key === 'ContextMenu' || (event.key === 'F10' && event.shiftKey)) {
        state.keyboardMenuAt = performance.now();
        event.preventDefault();
        return;
    }

    // Keys the component acts on must not also scroll the page or trigger the browser's own
    // command; the component receives the event either way.
    const command = event.ctrlKey || event.metaKey;
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'Backspace'].includes(event.key)
        || (command && ['z', 'Z', 'y', 'Y'].includes(event.key))) {
        event.preventDefault();
    }
};

// A press anywhere outside the canvas closes an open context menu, as a click beside a native menu
// does. Presses inside the canvas close it through the gesture they start.
const onDocumentPointerDown = (state, event) => {
    if (menuOf(state.canvas) && !state.canvas.contains(event.target)) {
        notify(state, 'MenuDismissed');
    }
};

const sizeOf = canvas => {
    const box = canvas.getBoundingClientRect();
    return { width: box.width, height: box.height };
};

export function attach(canvas, bridge) {
    if (!canvas || !bridge) {
        return null;
    }

    if (states.has(canvas)) {
        states.get(canvas).bridge = bridge;
        return sizeOf(canvas);
    }

    const state = {
        canvas,
        bridge,
        gesture: null,
        pointers: new Map(),
        longPress: 0,
        viewTimer: 0,
        resizeFrame: 0,
        keyboardMenuAt: Number.NEGATIVE_INFINITY,
        observer: null,
        listeners: []
    };

    const listen = (target, type, handler, options) => {
        const wrapped = event => handler(state, event);
        target.addEventListener(type, wrapped, options);
        state.listeners.push(() => target.removeEventListener(type, wrapped, options));
    };

    listen(canvas, 'pointerdown', onPointerDown);
    listen(canvas, 'pointermove', onPointerMove);
    listen(canvas, 'pointerup', onPointerUp);
    listen(canvas, 'pointercancel', onPointerUp);
    listen(canvas, 'wheel', onWheel, { passive: false });
    listen(canvas, 'dblclick', onDoubleClick);
    listen(canvas, 'contextmenu', onContextMenu);
    listen(canvas, 'keydown', onKeyDown);
    listen(document, 'pointerdown', onDocumentPointerDown, true);

    if (typeof ResizeObserver === 'function') {
        state.observer = new ResizeObserver(() => {
            window.cancelAnimationFrame(state.resizeFrame);
            state.resizeFrame = window.requestAnimationFrame(() => {
                const size = sizeOf(canvas);
                if (size.width > 0 && size.height > 0) {
                    notify(state, 'Resized', size.width, size.height);
                }
            });
        });
        state.observer.observe(canvas);
    }

    states.set(canvas, state);
    return sizeOf(canvas);
}

export function detach(canvas) {
    const state = canvas ? states.get(canvas) : null;
    if (!state) {
        return;
    }

    for (const remove of state.listeners) {
        remove();
    }

    state.observer?.disconnect();
    cancelLongPress(state);
    window.clearTimeout(state.viewTimer);
    window.cancelAnimationFrame(state.resizeFrame);
    states.delete(canvas);
}

// The rendered size of every label, so .NET can size the boxes on the text the browser actually
// drew rather than on an estimate. A label that is not laid out yet reports nothing.
export function measure(canvas) {
    if (!canvas) {
        return [];
    }

    const results = [];
    for (const text of canvas.querySelectorAll('text[data-omni-measure]')) {
        try {
            const box = text.getBBox();
            if (box.width > 0 || box.height > 0) {
                results.push({ key: text.getAttribute('data-omni-measure'), width: box.width, height: box.height });
            }
        } catch {
            // getBBox throws on an element that is not rendered; it is measured on a later pass.
        }
    }

    return results;
}

export function focusMenu(canvas) {
    const menu = canvas ? menuOf(canvas) : null;
    if (!menu) {
        return;
    }

    const first = menu.querySelector(MENU_ITEMS);
    (first ?? menu).focus({ preventScroll: true });
}

export function focusCanvas(canvas) {
    canvas?.focus({ preventScroll: true });
}

export function focusById(id) {
    document.getElementById(id)?.focus({ preventScroll: true });
}
