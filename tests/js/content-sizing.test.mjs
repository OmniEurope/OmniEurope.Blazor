import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const { planColumnWidths, takeContentTexts, widestTextOf } = await import(pathToFileURL(path.join(wwwroot, 'grid/content-sizing.js')).href);

const column = (key, base, role = 'plain') => ({ key, base, role });
const total = plan => [...plan.widths.values()].reduce((sum, width) => sum + width, 0);

test('a grid wider than its content shares the room out equally between every column', () => {
    const plan = planColumnWidths([column('a', 100), column('b', 200), column('c', 150)], 0, 600);

    assert.deepEqual([...plan.widths], [['a', 150], ['b', 250], ['c', 200]]);
    assert.equal(total(plan), 600);
    assert.equal(plan.minimum, 450);
});

test('the remainder of an uneven share goes to the last column, so the sum is the room', () => {
    const plan = planColumnWidths([column('a', 100), column('b', 100), column('c', 100)], 0, 400);

    assert.deepEqual([...plan.widths.values()], [133, 133, 134]);
    assert.equal(total(plan), 400);
});

test('the room goes to the columns that grow, the others keep their content width', () => {
    const plan = planColumnWidths(
        [column('name', 144, 'flexible'), column('status', 90), column('visitors', 130), column('project', 128, 'flexible'), column('since', 80)],
        0,
        900);

    assert.equal(plan.widths.get('status'), 90);
    assert.equal(plan.widths.get('visitors'), 130);
    assert.equal(plan.widths.get('since'), 80);
    // 900 - (144 + 90 + 130 + 128 + 80) = 328, shared by the two columns that grow.
    assert.equal(plan.widths.get('name'), 144 + 164);
    assert.equal(plan.widths.get('project'), 128 + 164);
    assert.equal(total(plan), 900);
});

test('the control columns are taken out of the room', () => {
    const plan = planColumnWidths([column('a', 100, 'flexible'), column('b', 100)], 88, 500);

    assert.equal(plan.widths.get('a'), 100 + 212);
    assert.equal(plan.widths.get('b'), 100);
    assert.equal(plan.minimum, 288);
});

test('a column the reader sized keeps its width and takes none of the room', () => {
    const plan = planColumnWidths([column('a', 100), column('b', 220, 'fixed'), column('c', 100)], 0, 700);

    assert.equal(plan.widths.get('b'), 220);
    assert.equal(plan.widths.get('a'), 100 + 140);
    assert.equal(plan.widths.get('c'), 100 + 140);
});

test('under a narrower window every column keeps its content width and the table keeps its minimum', () => {
    const columns = [column('a', 144, 'flexible'), column('b', 90), column('c', 130)];
    const plan = planColumnWidths(columns, 44, 300);

    assert.deepEqual([...plan.widths.values()], [144, 90, 130]);
    // Wider than the window: the viewport scrolls sideways instead of cutting a column.
    assert.equal(plan.minimum, 408);
    assert.ok(plan.minimum > 300);
});

test('a window exactly as wide as the content leaves nothing to share', () => {
    const plan = planColumnWidths([column('a', 100, 'flexible'), column('b', 100)], 0, 200);

    assert.deepEqual([...plan.widths.values()], [100, 100]);
});

test('with only reader sized columns there is nobody to give the room to', () => {
    const plan = planColumnWidths([column('a', 100, 'fixed'), column('b', 100, 'fixed')], 0, 900);

    assert.deepEqual([...plan.widths.values()], [100, 100]);
    assert.equal(plan.minimum, 200);
});

test('a grid without columns plans nothing', () => {
    const plan = planColumnWidths([], 44, 900);

    assert.equal(plan.widths.size, 0);
    assert.equal(plan.minimum, 44);
});

// The state a viewport keeps, minus what only a browser fills (the observer, the lengths, the widths laid out).
const emptyState = () => ({ texts: new Map(), textWidths: new Map(), fresh: new Map(), widths: new Map() });
// A stand-in for the layout of a text: its length, so the widest is the longest.
const layOut = laidOut => texts => {
    laidOut.push(...texts);
    return Math.max(...texts.map(text => text.length));
};

test('replacing the texts forgets the ones held, their widest and the content widths', () => {
    const state = emptyState();
    takeContentTexts(state, new Map([['a', ['one', 'three']], ['b', ['x']]]), false);
    widestTextOf(state, 'a', layOut([]));
    state.widths.set('a', 40);

    takeContentTexts(state, new Map([['a', ['four']]]), false);

    assert.deepEqual([...state.texts], [['a', ['four']]]);
    assert.equal(state.textWidths.size, 0);
    assert.equal(state.fresh.size, 0);
    assert.equal(state.widths.size, 0);
});

test('appending joins the new texts to the ones held and keeps the widths measured', () => {
    const state = emptyState();
    takeContentTexts(state, new Map([['a', ['one', 'three']]]), false);
    widestTextOf(state, 'a', layOut([]));
    state.widths.set('a', 40);

    takeContentTexts(state, new Map([['a', ['sixsix']], ['b', ['new']]]), true);

    assert.deepEqual([...state.texts], [['a', ['one', 'three', 'sixsix']], ['b', ['new']]]);
    assert.deepEqual([...state.fresh], [['a', ['sixsix']], ['b', ['new']]]);
    assert.equal(state.textWidths.get('a'), 5);
    assert.equal(state.widths.get('a'), 40);
});

test('texts appended twice before a layout are laid out together, once', () => {
    const state = emptyState();
    takeContentTexts(state, new Map([['a', ['one']]]), false);
    widestTextOf(state, 'a', layOut([]));

    takeContentTexts(state, new Map([['a', ['two']]]), true);
    takeContentTexts(state, new Map([['a', ['eleven']]]), true);
    const laidOut = [];

    assert.equal(widestTextOf(state, 'a', layOut(laidOut)), 6);
    assert.deepEqual(laidOut, ['two', 'eleven']);
    assert.equal(state.fresh.size, 0);
});

test('the first layout takes every text, the next ones only what was appended', () => {
    const state = emptyState();
    takeContentTexts(state, new Map([['a', ['one', 'three']]]), false);
    const laidOut = [];

    assert.equal(widestTextOf(state, 'a', layOut(laidOut)), 5);
    assert.deepEqual(laidOut, ['one', 'three']);

    // Nothing new: nothing laid out, the widest is the one kept.
    assert.equal(widestTextOf(state, 'a', layOut(laidOut)), 5);
    assert.equal(laidOut.length, 2);

    takeContentTexts(state, new Map([['a', ['ab']]]), true);
    assert.equal(widestTextOf(state, 'a', layOut(laidOut)), 5);
    assert.deepEqual(laidOut.slice(2), ['ab']);

    takeContentTexts(state, new Map([['a', ['sixsix']]]), true);
    assert.equal(widestTextOf(state, 'a', layOut(laidOut)), 6);
});

test('a font change that clears the widest has every text laid out again, the appended ones included', () => {
    const state = emptyState();
    takeContentTexts(state, new Map([['a', ['one']]]), false);
    widestTextOf(state, 'a', layOut([]));
    takeContentTexts(state, new Map([['a', ['two']]]), true);
    state.textWidths.clear();
    const laidOut = [];

    assert.equal(widestTextOf(state, 'a', layOut(laidOut)), 3);
    assert.deepEqual(laidOut, ['one', 'two']);
    assert.equal(state.fresh.size, 0);
});

test('a column without texts has no widest', () => {
    const state = emptyState();
    takeContentTexts(state, new Map([['a', []]]), false);

    assert.equal(widestTextOf(state, 'a', layOut([])), 0);
    assert.equal(widestTextOf(state, 'unknown', layOut([])), 0);
    state.texts.set('odd', 'not a list');
    assert.equal(widestTextOf(state, 'odd', layOut([])), 0);
});
