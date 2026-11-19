import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { dotnetRecorder, installDom } from './dom-stub.mjs';

// The keys of OmniKanban that the script handles: the focus moved from card to card, and the keys of a
// carried card sent to .NET. The columns follow the direction of the page, so the horizontal arrows too.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');

// A board of three columns in drawing order, one card in each of the outer two and two in the middle one.
const fixture = async direction => {
    const dom = installDom();
    globalThis.getComputedStyle = () => ({ direction });
    const card = id => dom.el('div', { 'data-omni-kanban-card': id });
    const cards = { a: card('a'), b1: card('b1'), b2: card('b2'), c: card('c') };
    const column = (key, ...children) => dom.el('div', { 'data-omni-kanban-column': key }, ...children);
    const root = dom.el('div', {}, column('first', cards.a), column('middle', cards.b1, cards.b2), column('last', cards.c));
    dom.body.append(root);
    const handlers = new Map();
    root.addEventListener = (type, handler) => handlers.set(type, handler);
    const { attach } = await import(pathToFileURL(path.join(wwwroot, 'omni-kanban.js')).href);
    const dotnet = dotnetRecorder();
    attach(root, dotnet);
    const press = (target, key) => {
        const event = { key, target, prevented: false, preventDefault() { this.prevented = true; } };
        handlers.get('keydown')(event);
        return event;
    };
    return { root, cards, dotnet, press };
};

test('left to right, ArrowRight moves the focus to the next column and ArrowLeft to the previous one', async () => {
    const { cards, press, dotnet } = await fixture('ltr');

    assert.ok(press(cards.b1, 'ArrowRight').prevented);
    assert.ok(cards.c.focused && !cards.a.focused);
    cards.c.focused = false;
    press(cards.b1, 'ArrowLeft');
    assert.ok(cards.a.focused && !cards.c.focused);
    assert.deepEqual(dotnet.calls, [], 'no card is carried: the focus moves in the page, .NET is not told');
});

test('right to left, the next column is on the left: ArrowLeft goes to it and ArrowRight to the previous one', async () => {
    const { cards, press } = await fixture('rtl');

    assert.ok(press(cards.b1, 'ArrowLeft').prevented);
    assert.ok(cards.c.focused && !cards.a.focused, 'ArrowLeft reaches the column drawn on the left, the next one');
    cards.c.focused = false;
    press(cards.b1, 'ArrowRight');
    assert.ok(cards.a.focused && !cards.c.focused, 'ArrowRight reaches the column drawn on the right, the previous one');
});

test('right to left, the vertical arrows are unchanged', async () => {
    const { cards, press } = await fixture('rtl');

    press(cards.b1, 'ArrowDown');
    assert.ok(cards.b2.focused);
    press(cards.b2, 'ArrowUp');
    assert.ok(cards.b1.focused);
});

test('a carried card is moved toward the column the arrow points at: .NET receives the arrow in drawing order', async () => {
    const leftToRight = await fixture('ltr');
    leftToRight.root.setAttribute('data-omni-kanban-grabbed', '');
    leftToRight.press(leftToRight.cards.b1, 'ArrowLeft');
    assert.deepEqual(leftToRight.dotnet.calls, [['OnCardKey', 'b1', 'ArrowLeft']]);

    const rightToLeft = await fixture('rtl');
    rightToLeft.root.setAttribute('data-omni-kanban-grabbed', '');
    rightToLeft.press(rightToLeft.cards.b1, 'ArrowLeft');
    rightToLeft.press(rightToLeft.cards.b1, 'ArrowDown');
    assert.deepEqual(rightToLeft.dotnet.calls, [['OnCardKey', 'b1', 'ArrowRight'], ['OnCardKey', 'b1', 'ArrowDown']],
        'the left arrow carries the card to the next column, which .NET names ArrowRight');
});
