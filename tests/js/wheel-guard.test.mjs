import assert from 'node:assert/strict';
import { test } from 'node:test';
import { packageScriptUrl } from './dom-stub.mjs';

const { isPlainVerticalWheel } = await import(packageScriptUrl('shared/scroll.js'));

const turn = overrides => ({ defaultPrevented: false, ctrlKey: false, shiftKey: false, deltaX: 0, deltaY: 40, ...overrides });

test('a plain vertical turn may be taken by an area, up or down', () => {
    assert.equal(isPlainVerticalWheel(turn()), true);
    assert.equal(isPlainVerticalWheel(turn({ deltaY: -40, deltaX: 10 })), true);
});

test('a handled turn, a zoom (Ctrl), a horizontal scroll (Shift) or a mostly horizontal turn is left alone', () => {
    assert.equal(isPlainVerticalWheel(turn({ defaultPrevented: true })), false);
    assert.equal(isPlainVerticalWheel(turn({ ctrlKey: true })), false);
    assert.equal(isPlainVerticalWheel(turn({ shiftKey: true })), false);
    assert.equal(isPlainVerticalWheel(turn({ deltaX: 40 })), false, 'as much across as down');
    assert.equal(isPlainVerticalWheel(turn({ deltaX: -60, deltaY: 20 })), false);
});
