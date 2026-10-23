import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
let loads = 0;

// A fresh page and a fresh module (it keeps the tracked tooltip in its own state), with the
// markup of an OmniTooltip around a control that carries a native title.
const fixture = async (control = { title: 'Delete the row' }) => {
    const dom = installDom();
    const tooltip = await import(`${pathToFileURL(path.join(wwwroot, 'omni-tooltip.js')).href}?tooltip${loads++}`);
    tooltip.install();
    const button = dom.el('button', control);
    const content = dom.el('span', { class: 'omni-tooltip__content', role: 'tooltip', text: 'Removes it for good' });
    content.rect = { left: 0, top: 0, right: 120, bottom: 24, width: 120, height: 24 };
    const root = dom.el('span', { class: 'omni-tooltip' }, dom.el('span', { class: 'omni-tooltip__trigger' }, button), content);
    const elsewhere = dom.el('p');
    dom.body.append(root);
    dom.body.append(elsewhere);
    const pointerOn = target => dom.dispatch('pointermove', { target, clientX: 200, clientY: 300 });
    return { dom, button, root, elsewhere, pointerOn };
};

test('a title inside the anchor is set aside while the package tooltip is shown, so the browser draws no second one', async () => {
    const { button, pointerOn } = await fixture();

    pointerOn(button);

    assert.equal(button.getAttribute('title'), null);
    assert.equal(button.getAttribute('data-omni-title'), 'Delete the row');
});

test('the title is given back when the pointer leaves the tooltip', async () => {
    const { button, elsewhere, pointerOn } = await fixture();
    pointerOn(button);

    pointerOn(elsewhere);

    assert.equal(button.getAttribute('title'), 'Delete the row');
    assert.equal(button.getAttribute('data-omni-title'), null);
});

test('a control named by its title alone keeps that name for assistive technology meanwhile, and not afterwards', async () => {
    const { button, elsewhere, pointerOn } = await fixture();

    pointerOn(button);
    assert.equal(button.getAttribute('aria-label'), 'Delete the row');

    pointerOn(elsewhere);
    assert.equal(button.getAttribute('aria-label'), null);
});

test('a control that has a name of its own gets no aria-label from its title', async () => {
    const { button, pointerOn } = await fixture({ title: 'Delete the row', 'aria-label': 'Delete' });

    pointerOn(button);

    assert.equal(button.getAttribute('aria-label'), 'Delete');
    assert.equal(button.getAttribute('title'), null);
});

test('the keyboard focus sets the title aside too, and leaving gives it back', async () => {
    const { button, dom } = await fixture();
    button.rect = { left: 10, top: 100, right: 40, bottom: 120, width: 30, height: 20 };

    dom.dispatch('focusin', { target: button });
    assert.equal(button.getAttribute('title'), null);

    dom.dispatch('focusout', { target: button, relatedTarget: null });
    assert.equal(button.getAttribute('title'), 'Delete the row');
});

test('a scroll of the page gives the title back', async () => {
    const { button, dom, pointerOn } = await fixture();
    pointerOn(button);

    dom.dispatch('scroll', { target: dom.body });

    assert.equal(button.getAttribute('title'), 'Delete the row');
});
