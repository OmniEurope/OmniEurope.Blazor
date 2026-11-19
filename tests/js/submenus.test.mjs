import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
// The scripts read the page when they load (omni-focus.js hooks the page keys): a first page exists by then.
const load = file => {
    if (!globalThis.document) {
        installDom();
    }

    return import(pathToFileURL(path.join(wwwroot, file)).href);
};

const box = (left, top, width, height) => ({ left, top, width, height, right: left + width, bottom: top + height });

// A fixed box the corner of which is read from the custom properties, in a containing block at (dx, dy).
const follow = (element, width, height, dx = 0, dy = 0) => {
    const read = name => parseFloat(element.style.getPropertyValue(name)) || 0;
    element.getBoundingClientRect = () => box(read('--omni-menu-x') + dx, read('--omni-menu-y') + dy, width, height);
};

// A menu of three entries, the middle one opening a submenu of three: the shape OmniSubMenu draws.
//   menu > [ first, wrapper > [ entry, submenu > [ one, two (disabled), three ] ], last ]
const fixture = (expanded = true) => {
    const dom = installDom();
    const menu = dom.el('div', { id: 'menu', role: 'menu' });
    const first = dom.el('button', { id: 'first', role: 'menuitem' });
    const entry = dom.el('button', { id: 'entry', role: 'menuitem', 'aria-haspopup': 'menu', 'aria-expanded': String(expanded) });
    const one = dom.el('button', { id: 'one', role: 'menuitem' });
    const two = dom.el('button', { id: 'two', role: 'menuitem', disabled: '' });
    const three = dom.el('button', { id: 'three', role: 'menuitem' });
    const last = dom.el('button', { id: 'last', role: 'menuitem' });
    const submenu = dom.el('div', { id: 'entry-menu', role: 'menu', class: 'omni-menu omni-menu__submenu', 'aria-labelledby': 'entry' }, one, two, three);
    if (expanded) {
        entry.setAttribute('aria-controls', 'entry-menu');
    }

    menu.append(first);
    menu.append(dom.el('div', { class: 'omni-menu__sub' }, entry, ...(expanded ? [submenu] : [])));
    menu.append(last);
    dom.body.append(menu);
    const clicks = [];
    entry.click = () => clicks.push('entry');
    const key = (target, name) => {
        const event = { key: name, target, defaultPrevented: false, stopped: false, preventDefault() { this.defaultPrevented = true; }, stopPropagation() { this.stopped = true; } };
        return event;
    };
    return { dom, menu, first, entry, one, two, three, last, submenu, clicks, key };
};

test('placeBeside puts the box at the inline end of the anchor, level with the row plus the offset', async () => {
    const { placeBeside } = await load('shared/placement.js');

    const placed = placeBeside({ anchor: box(100, 200, 180, 30), size: { width: 160, height: 120 }, offset: -5, viewport: { width: 1024, height: 768 } });

    assert.deepEqual(placed, { left: 280, top: 195, side: 'end' });
});

test('placeBeside mirrors in a right-to-left page: the end side is the left one', async () => {
    const { placeBeside } = await load('shared/placement.js');

    const placed = placeBeside({ anchor: box(500, 200, 180, 30), size: { width: 160, height: 120 }, rtl: true, viewport: { width: 1024, height: 768 } });

    assert.deepEqual(placed, { left: 340, top: 200, side: 'end' });
});

test('placeBeside flips to the start side when the end side has no room', async () => {
    const { placeBeside } = await load('shared/placement.js');

    const placed = placeBeside({ anchor: box(800, 200, 180, 30), size: { width: 160, height: 120 }, viewport: { width: 1024, height: 768 } });

    assert.deepEqual(placed, { left: 640, top: 200, side: 'start' });
});

test('placeBeside moves the box up to stay inside the window at the bottom', async () => {
    const { placeBeside } = await load('shared/placement.js');

    const placed = placeBeside({ anchor: box(100, 700, 180, 30), size: { width: 160, height: 200 }, viewport: { width: 1024, height: 768 } });

    assert.equal(placed.top, 768 - 8 - 200);
    assert.equal(placed.side, 'end');
});

test('placeBeside on a narrow window, with room on neither side, takes the side with the most room and stays inside', async () => {
    const { placeBeside } = await load('shared/placement.js');

    // 375 px wide: a menu from 8 to 200, a submenu of 176 px: 200 + 176 = 376 leaves the window, and so does the start side.
    const placed = placeBeside({ anchor: box(8, 100, 192, 30), size: { width: 176, height: 150 }, viewport: { width: 375, height: 667 } });

    assert.equal(placed.side, 'end');
    assert.equal(placed.left, 375 - 8 - 176);
    assert.ok(placed.left >= 8 && placed.left + 176 <= 375 - 8);
});

test('moveMenuFocus moves through the top level only, the items of a submenu are not in it', async () => {
    const { moveMenuFocus } = await load('omni-focus.js');
    const { dom, first, entry, last } = fixture();
    globalThis.document.activeElement = first;

    moveMenuFocus('menu', 'ArrowDown');
    assert.ok(entry.focused);
    entry.focused = false;
    globalThis.document.activeElement = entry;
    moveMenuFocus('menu', 'ArrowDown');
    assert.ok(last.focused, 'the entry is followed by the next entry of its own level');
    globalThis.document.activeElement = last;
    last.focused = false;
    first.focused = false;
    moveMenuFocus('menu', 'ArrowDown');
    assert.ok(first.focused, 'the top level wraps without entering the submenu');
    assert.equal(dom.body.querySelectorAll('[id="one"]')[0].focused, false);
});

test('moveMenuFocus acts on the submenu the focus is in, skipping its disabled item and wrapping inside it', async () => {
    const { moveMenuFocus } = await load('omni-focus.js');
    const { one, two, three, first } = fixture();
    globalThis.document.activeElement = one;

    moveMenuFocus('menu', 'ArrowDown');
    assert.ok(three.focused && !two.focused, 'the disabled item is skipped');
    globalThis.document.activeElement = three;
    three.focused = false;
    moveMenuFocus('menu', 'ArrowDown');
    assert.ok(one.focused, 'the submenu wraps to its own first item');
    assert.equal(first.focused, false);
    globalThis.document.activeElement = one;
    one.focused = false;
    moveMenuFocus('menu', 'ArrowUp');
    assert.ok(three.focused, 'ArrowUp wraps to the last item of the submenu');
    globalThis.document.activeElement = one;
    three.focused = false;
    moveMenuFocus('menu', 'End');
    assert.ok(three.focused);
    moveMenuFocus('menu', 'Home');
    assert.ok(one.focused);
});

test('moveMenuFocus from the menu itself goes to the first item on ArrowDown and to the last on ArrowUp', async () => {
    const { moveMenuFocus } = await load('omni-focus.js');
    const { menu, first, entry, last } = fixture();
    // A press on the padding of the menu, or on a disabled row, leaves the focus on the menu itself.
    globalThis.document.activeElement = menu;

    moveMenuFocus('menu', 'ArrowDown');
    assert.ok(first.focused && !entry.focused, 'ArrowDown starts at the first item, it does not skip it');
    first.focused = false;
    globalThis.document.activeElement = menu;
    moveMenuFocus('menu', 'ArrowUp');
    assert.ok(last.focused && !entry.focused, 'ArrowUp starts at the last item');
});

test('openSubMenu places the submenu beside its entry and focuses its first item for the keyboard', async () => {
    const { openSubMenu } = await load('omni-focus.js');
    const { menu, entry, submenu, one } = fixture();
    menu.rect = box(100, 100, 192, 200);
    entry.rect = box(104, 140, 184, 30);
    follow(submenu, 176, 120);

    openSubMenu('entry', 'entry-menu', true);

    assert.ok(submenu.hasAttribute('data-omni-placed'));
    assert.equal(submenu.style.getPropertyValue('--omni-menu-x'), '292px');
    assert.equal(submenu.style.getPropertyValue('--omni-menu-y'), '140px');
    assert.ok(one.focused);
});

test('openSubMenu does not take the focus when the pointer opened the submenu', async () => {
    const { openSubMenu } = await load('omni-focus.js');
    const { menu, entry, submenu, one } = fixture();
    menu.rect = box(100, 100, 192, 200);
    entry.rect = box(104, 140, 184, 30);
    follow(submenu, 176, 120);

    openSubMenu('entry', 'entry-menu', false);

    assert.ok(submenu.hasAttribute('data-omni-placed'));
    assert.equal(one.focused, false);
});

test('openSubMenu corrects the corner by what a containing block takes off, so the box lands where it was placed', async () => {
    const { openSubMenu } = await load('omni-focus.js');
    const { menu, entry, submenu } = fixture();
    menu.rect = box(100, 100, 192, 200);
    entry.rect = box(104, 140, 184, 30);
    // The submenu sits in a box that is the containing block of its fixed position: the corner it
    // is given is read from that box at (100, 100), not from the window.
    follow(submenu, 176, 120, 100, 100);

    openSubMenu('entry', 'entry-menu', false);

    assert.equal(submenu.style.getPropertyValue('--omni-menu-x'), '192px');
    assert.equal(submenu.style.getPropertyValue('--omni-menu-y'), '40px');
});

test('leaveSubMenu gives the entry the focus the submenu holds, and leaves any other focus alone', async () => {
    const { leaveSubMenu } = await load('omni-focus.js');
    const { entry, one, last } = fixture();

    globalThis.document.activeElement = last;
    leaveSubMenu('entry-menu', 'entry');
    assert.equal(entry.focused, false);

    globalThis.document.activeElement = one;
    leaveSubMenu('entry-menu', 'entry');
    assert.ok(entry.focused);
});

test('the arrow toward the inline end on a closed entry presses it, which opens it from the keyboard', async () => {
    const { handleSubMenuKey } = await load('focus/menus.js');
    const { menu, entry, clicks, key } = fixture(false);
    const event = key(entry, 'ArrowRight');

    assert.equal(handleSubMenuKey(event, menu), true);

    assert.deepEqual(clicks, ['entry']);
    assert.ok(event.defaultPrevented && event.stopped);
});

test('the arrow toward the inline end on an open entry focuses the first item of its submenu without pressing it', async () => {
    const { handleSubMenuKey } = await load('focus/menus.js');
    const { menu, entry, one, clicks, key } = fixture(true);

    assert.equal(handleSubMenuKey(key(entry, 'ArrowRight'), menu), true);

    assert.ok(one.focused);
    assert.deepEqual(clicks, []);
});

test('the arrow back and Escape in a submenu focus its entry and close it, and keep the key from closing the menu', async () => {
    const { handleSubMenuKey } = await load('focus/menus.js');
    for (const name of ['ArrowLeft', 'Escape']) {
        const { menu, entry, one, clicks, key } = fixture(true);
        const event = key(one, name);

        assert.equal(handleSubMenuKey(event, menu), true, name);

        assert.ok(entry.focused, name);
        assert.deepEqual(clicks, ['entry'], name);
        assert.ok(event.defaultPrevented && event.stopped, name);
    }
});

test('Escape and the arrows on the top level, and the arrow toward the inline end on a plain item, are left to the menu', async () => {
    const { handleSubMenuKey } = await load('focus/menus.js');
    const { menu, first, one, key } = fixture(true);

    for (const [target, name] of [[first, 'Escape'], [first, 'ArrowLeft'], [first, 'ArrowRight'], [one, 'ArrowRight'], [one, 'ArrowDown']]) {
        const event = key(target, name);
        assert.equal(handleSubMenuKey(event, menu), false, `${name} on ${target.getAttribute('id')}`);
        assert.equal(event.stopped, false);
    }
});

test('in a right-to-left page the arrows swap: the left one opens, the right one closes', async () => {
    const { handleSubMenuKey } = await load('focus/menus.js');
    const closed = fixture(false);
    globalThis.getComputedStyle = () => ({ visibility: 'visible', direction: 'rtl', fontFamily: '' });

    assert.equal(handleSubMenuKey(closed.key(closed.entry, 'ArrowRight'), closed.menu), false);
    assert.equal(handleSubMenuKey(closed.key(closed.entry, 'ArrowLeft'), closed.menu), true);
    assert.deepEqual(closed.clicks, ['entry']);

    const open = fixture(true);
    globalThis.getComputedStyle = () => ({ visibility: 'visible', direction: 'rtl', fontFamily: '' });
    assert.equal(handleSubMenuKey(open.key(open.one, 'ArrowLeft'), open.menu), false);
    assert.equal(handleSubMenuKey(open.key(open.one, 'ArrowRight'), open.menu), true);
    assert.ok(open.entry.focused);
});
