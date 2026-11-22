import assert from 'node:assert/strict';
import { test } from 'node:test';
import { installDom, packageScriptUrl } from './dom-stub.mjs';

// SelectOnFocus of the text-entry fields: the whole content selected when the field is entered, and a mouse press that
// gives the focus keeps that selection (its mouseup is cancelled once). The stub field records its listeners and its
// select() calls; the real caret behaviour of a click is proved by the showcase probe in a browser.
const dom = installDom();
const { attach, detach } = await import(packageScriptUrl('omni-select-on-focus.js'));

// A field that dispatches by hand to the listeners attached to it, like the browser would.
const newField = () => {
    const field = dom.el('input', { type: 'number' });
    const listeners = new Map();
    field.addEventListener = (type, handler) => listeners.set(type, [...(listeners.get(type) ?? []), handler]);
    field.removeEventListener = (type, handler) => listeners.set(type, (listeners.get(type) ?? []).filter(item => item !== handler));
    field.selections = 0;
    field.select = () => { field.selections++; };
    field.listenerCount = () => [...listeners.values()].reduce((sum, handlers) => sum + handlers.length, 0);
    field.fire = (type, event = {}) => {
        const sent = { defaultPrevented: false, preventDefault() { this.defaultPrevented = true; }, ...event };
        (listeners.get(type) ?? []).slice().forEach(handler => handler(sent));
        return sent;
    };
    // The browser gives the focus on a press after pointerdown, then fires focus; the active element is set first.
    field.focus = () => { document.activeElement = field; field.fire('focus'); };
    field.blur = () => { document.activeElement = document.body; field.fire('blur'); };
    return field;
};

test('focus from the keyboard or from code selects the whole content', () => {
    const field = newField();
    attach(field);

    field.focus();

    assert.equal(field.selections, 1);
    field.blur();
    detach(field);
});

test('a press that gives the focus keeps the selection: its mouseup is cancelled, once', () => {
    const field = newField();
    attach(field);

    field.fire('pointerdown');
    field.focus();
    const release = field.fire('mouseup');

    assert.equal(field.selections, 1);
    assert.equal(release.defaultPrevented, true);

    // The next press in the same, now focused, field is a normal one.
    field.fire('pointerdown');
    const second = field.fire('mouseup');
    assert.equal(second.defaultPrevented, false);
    assert.equal(field.selections, 1);
    field.blur();
    detach(field);
});

test('a click in a field that already has the focus does not select again and keeps its mouseup', () => {
    const field = newField();
    attach(field);
    field.focus();

    field.fire('pointerdown');
    const release = field.fire('mouseup');

    assert.equal(field.selections, 1);
    assert.equal(release.defaultPrevented, false);
    field.blur();
    detach(field);
});

test('a focus that did not come from a press never cancels a later mouseup', () => {
    const field = newField();
    attach(field);

    field.focus();
    const release = field.fire('mouseup');

    assert.equal(release.defaultPrevented, false);
    field.blur();
    detach(field);
});

test('a press that did not give the focus is forgotten when it ends, not carried to a later focus', () => {
    const field = newField();
    attach(field);

    field.fire('pointerdown');
    field.fire('mouseup');
    field.focus();
    const release = field.fire('mouseup');

    assert.equal(release.defaultPrevented, false);
    field.blur();
    detach(field);
});

test('leaving the field resets a pending press', () => {
    const field = newField();
    attach(field);
    field.fire('pointerdown');
    field.focus();
    field.blur();

    const release = field.fire('mouseup');

    assert.equal(release.defaultPrevented, false);
    detach(field);
});

test('attaching twice listens once, and detach removes every listener', () => {
    const field = newField();
    attach(field);
    const listening = field.listenerCount();
    attach(field);

    assert.ok(listening > 0);
    assert.equal(field.listenerCount(), listening);

    detach(field);
    assert.equal(field.listenerCount(), 0);
    field.focus();
    assert.equal(field.selections, 0);
    detach(field);
});

test('a missing element is ignored', () => {
    assert.doesNotThrow(() => { attach(null); detach(null); });
});
