import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

// The structured document script (omni-structured-document.js): what it tells .NET about the text of a run, when, and
// how it keeps a run on one line and plain. It runs against a root made of the few members the script reads, and the
// events are fired by hand; what a real browser does with contenteditable and the clipboard stays with the showcase
// probe (eng/scripts-probe/structured-document.mjs).
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');

let supportsPlainText = true;
const commands = [];
globalThis.document = {
    createElement: () => {
        const probe = { value: 'inherit' };
        return Object.defineProperty(probe, 'contentEditable', {
            get: () => probe.value,
            set: value => { if (supportsPlainText || value !== 'plaintext-only') probe.value = value; }
        });
    },
    execCommand: (...command) => { commands.push(command); return true; }
};
const editor = await import(pathToFileURL(path.join(wwwroot, 'omni-structured-document.js')).href);

// A run answers closest('[data-omni-run]') with itself; anything else answers null.
const element = (id, text) => {
    const made = {
        text,
        closest: selector => (selector === '[data-omni-run]' ? made : null),
        getAttribute: name => (name === 'data-omni-run' ? id : null),
        get textContent() { return this.text; }
    };
    return made;
};
const outside = { closest: () => null };

const setup = ({ failing = false } = {}) => {
    const listeners = new Map();
    const root = {
        addEventListener: (type, listener) => listeners.set(type, listener),
        removeEventListener: (type, listener) => { if (listeners.get(type) === listener) listeners.delete(type); }
    };
    const calls = [];
    const dotNet = {
        invokeMethodAsync: (method, ...args) => {
            calls.push([method, ...args]);
            if (failing) throw new Error('disposed');
            return Promise.resolve();
        }
    };
    commands.length = 0;
    const supported = editor.attach(root, dotNet);
    const fire = (type, event) => {
        const prevented = { value: false };
        listeners.get(type)({ ...event, preventDefault: () => { prevented.value = true; } });
        return prevented.value;
    };
    return { root, listeners, calls, supported, fire };
};

test('attach says whether the browser edits a span as plain text', () => {
    supportsPlainText = true;
    assert.equal(setup().supported, true);
    supportsPlainText = false;
    assert.equal(setup().supported, false);
    supportsPlainText = true;
});

test('attach listens to focusin, input, focusout, keydown and beforeinput on the root only', () => {
    const { listeners } = setup();

    assert.deepEqual([...listeners.keys()].sort(), ['beforeinput', 'focusin', 'focusout', 'input', 'keydown']);
});

test('an input tells .NET the text of the run by its identifier, once per distinct text', () => {
    const { fire, calls } = setup();
    const first = element('r1', 'Hello');

    fire('input', { target: first });
    fire('input', { target: first });
    first.text = 'Hello!';
    fire('input', { target: first });

    assert.deepEqual(calls, [['OnRunText', 'r1', 'Hello'], ['OnRunText', 'r1', 'Hello!']]);
});

test('each run is followed on its own', () => {
    const { fire, calls } = setup();
    const first = element('r1', 'one');
    const second = element('r2', 'two');

    fire('input', { target: first });
    fire('input', { target: second });
    fire('input', { target: first });

    assert.deepEqual(calls, [['OnRunText', 'r1', 'one'], ['OnRunText', 'r2', 'two']]);
});

test('leaving a run tells .NET what it holds when that differs from what .NET knows', () => {
    const { fire, calls } = setup();
    const first = element('r1', 'before');

    fire('focusin', { target: first });
    fire('focusout', { target: first });
    assert.deepEqual(calls, [], 'a run left as it was entered says nothing');

    fire('focusin', { target: first });
    first.text = 'after';
    fire('focusout', { target: first });
    fire('focusout', { target: first });
    assert.deepEqual(calls, [['OnRunText', 'r1', 'after']]);
});

test('an input already told is not told again when the run is left', () => {
    const { fire, calls } = setup();
    const first = element('r1', 'a');

    fire('focusin', { target: first });
    first.text = 'ab';
    fire('input', { target: first });
    fire('focusout', { target: first });

    assert.equal(calls.length, 1);
});

test('events outside a run are ignored', () => {
    const { fire, calls } = setup();

    fire('focusin', { target: outside });
    fire('input', { target: outside });
    fire('focusout', { target: outside });
    fire('input', { target: null });
    assert.equal(fire('keydown', { target: outside, key: 'Enter' }), false);
    assert.equal(fire('beforeinput', { target: outside, inputType: 'insertParagraph' }), false);
    assert.equal(fire('beforeinput', { target: outside, inputType: 'insertFromPaste' }), false);

    assert.deepEqual(calls, []);
    assert.deepEqual(commands, []);
});

test('Enter in a run makes no paragraph; other keys are left alone', () => {
    const { fire } = setup();
    const target = element('r1', 'x');

    assert.equal(fire('keydown', { target, key: 'Enter' }), true);
    assert.equal(fire('keydown', { target, key: 'a' }), false);
    assert.equal(fire('keydown', { target, key: 'Tab' }), false);
});

test('a paragraph or a line break typed into a run is refused', () => {
    const { fire } = setup();
    const target = element('r1', 'x');

    assert.equal(fire('beforeinput', { target, inputType: 'insertParagraph' }), true);
    assert.equal(fire('beforeinput', { target, inputType: 'insertLineBreak' }), true);
    assert.equal(fire('beforeinput', { target, inputType: 'insertText' }), false);
    assert.equal(fire('beforeinput', { target, inputType: 'deleteContentBackward' }), false);
    assert.deepEqual(commands, []);
});

test('a paste or a drop is inserted as plain text with its line breaks turned into spaces', () => {
    const { fire } = setup();
    const target = element('r1', 'x');
    const data = text => ({ getData: type => (type === 'text/plain' ? text : '<b>formatted</b>') });

    assert.equal(fire('beforeinput', { target, inputType: 'insertFromPaste', dataTransfer: data('line one\r\n line two\n\nline three') }), true);
    assert.equal(fire('beforeinput', { target, inputType: 'insertFromDrop', dataTransfer: data('dropped') }), true);

    assert.deepEqual(commands, [['insertText', false, 'line one line two line three'], ['insertText', false, 'dropped']]);
});

test('a paste with no text (an image, nothing) is refused and inserts nothing', () => {
    const { fire } = setup();
    const target = element('r1', 'x');

    assert.equal(fire('beforeinput', { target, inputType: 'insertFromPaste', dataTransfer: { getData: () => '' } }), true);
    assert.equal(fire('beforeinput', { target, inputType: 'insertFromPaste' }), true);

    assert.deepEqual(commands, []);
});

test('a .NET reference that is already released is not an error', () => {
    const { fire, calls } = setup({ failing: true });

    fire('input', { target: element('r1', 'text') });

    assert.equal(calls.length, 1);
});

test('detach removes every listener, and detaching twice is harmless', () => {
    const { root, listeners } = setup();

    editor.detach(root);
    assert.equal(listeners.size, 0);
    editor.detach(root);
    editor.detach({});
    assert.equal(listeners.size, 0);
});
