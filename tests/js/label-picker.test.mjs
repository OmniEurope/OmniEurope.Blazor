import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
let loads = 0;

// A fresh page and a fresh module (it remembers that its listener is installed), with a label for a
// control placed beside it, as an OmniFormField draws them.
const fixture = async (control = { localName: 'select' }, showPicker = () => { }) => {
    const dom = installDom();
    const labels = await import(`${pathToFileURL(path.join(wwwroot, 'focus/labels.js')).href}?labels${loads++}`);
    const { localName, ...attributes } = control;
    const field = dom.el(localName, { id: 'field', ...attributes });
    const calls = [];
    field.showPicker = () => {
        calls.push('showPicker');
        showPicker();
    };
    const text = dom.el('span', { text: 'Country' });
    const label = dom.el('label', { for: 'field' }, text);
    dom.body.append(label);
    dom.body.append(field);
    labels.enableLabelPicker();
    const click = (target, extra = {}) => dom.dispatch('click', { target, defaultPrevented: false, ...extra });
    return { dom, labels, field, label, text, calls, click };
};

test('a click on the label of an enabled select opens its picker, and focuses it', async () => {
    const { calls, click, field, text } = await fixture();

    click(text);

    assert.deepEqual(calls, ['showPicker']);
    assert.equal(field.focused, true);
});

test('a click on the label itself, not on its text, opens the picker too', async () => {
    const { calls, click, label } = await fixture();

    click(label);

    assert.deepEqual(calls, ['showPicker']);
});

test('the picker is not opened for a disabled select', async () => {
    const { calls, click, text } = await fixture({ localName: 'select', disabled: '' });

    click(text);

    assert.deepEqual(calls, []);
});

test('the picker is not opened for a select marked read-only', async () => {
    for (const marker of [{ readonly: '' }, { 'aria-readonly': 'true' }]) {
        const { calls, click, text } = await fixture({ localName: 'select', ...marker });

        click(text);

        assert.deepEqual(calls, []);
    }
});

test('the picker is not opened for a label whose control is an input', async () => {
    const { calls, click, text } = await fixture({ localName: 'input', type: 'text' });

    click(text);

    assert.deepEqual(calls, []);
});

test('an error thrown by showPicker is swallowed, so the native focus remains', async () => {
    const { calls, click, text } = await fixture({ localName: 'select' }, () => {
        throw new DOMException('Not allowed', 'NotAllowedError');
    });

    assert.doesNotThrow(() => click(text));
    assert.deepEqual(calls, ['showPicker']);
});

test('a missing showPicker (an old browser) is swallowed too', async () => {
    const { click, field, text } = await fixture();
    field.showPicker = undefined;

    assert.doesNotThrow(() => click(text));
});

test('a click that is not on a label, or already handled, opens nothing', async () => {
    const { calls, click, dom, field, text } = await fixture();
    const elsewhere = dom.el('p');
    dom.body.append(elsewhere);

    click(elsewhere);
    click(field);
    click(text, { defaultPrevented: true });

    assert.deepEqual(calls, []);
});

test('a control placed inside the label is that control\'s own click', async () => {
    const dom = installDom();
    const labels = await import(`${pathToFileURL(path.join(wwwroot, 'focus/labels.js')).href}?labels${loads++}`);
    const calls = [];
    const select = dom.el('select');
    select.showPicker = () => calls.push('showPicker');
    const link = dom.el('a', { href: '#' });
    const label = dom.el('label', {}, dom.el('span', { text: 'Country' }), select, link);
    label.control = select;
    dom.body.append(label);
    labels.enableLabelPicker();

    dom.dispatch('click', { target: select, defaultPrevented: false });
    dom.dispatch('click', { target: link, defaultPrevented: false });
    assert.deepEqual(calls, []);

    dom.dispatch('click', { target: label.children[0], defaultPrevented: false });
    assert.deepEqual(calls, ['showPicker']);
});

test('enabling twice installs one listener on the document, and nothing per element', async () => {
    const { dom, labels } = await fixture();

    labels.enableLabelPicker();
    labels.enableLabelPicker();

    assert.equal(dom.listenerCount('click'), 1);
});
