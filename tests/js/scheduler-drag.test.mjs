import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// The drag of OmniScheduler (omni-scheduler.js): the root is marked carrying only after the dragstart, on
// the next task, so the stylesheet never turns the dragged block transparent to the pointer while Chromium
// starts its drag, and the mark leaves with the drag.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const carrying = 'data-omni-scheduler-carrying';

async function load() {
    const dom = installDom();
    const script = await import(pathToFileURL(path.join(wwwroot, 'omni-scheduler.js')).href);
    const block = dom.el('div', { 'data-omni-scheduler-appointment': 'reports' });
    const root = dom.el('div', { class: 'omni-scheduler' }, block);
    const handlers = new Map();
    root.addEventListener = (type, handler) => handlers.set(type, handler);
    root.removeEventListener = type => handlers.delete(type);
    dom.body.append(root);
    script.attach(root);
    const data = [];
    const dragStart = (target = block) => handlers.get('dragstart')?.({ target, dataTransfer: { setData: (type, value) => data.push([type, value]) } });
    return { dom, script, root, block, handlers, data, dragStart };
}

test('the root is marked carrying on the task after the dragstart, never during it', async () => {
    const { dom, root, data, dragStart } = await load();

    dragStart();
    assert.equal(root.hasAttribute(carrying), false);
    assert.deepEqual(data, [['text/plain', 'reports']]);
    dom.runTimers();
    assert.equal(root.hasAttribute(carrying), true);
});

test('the mark leaves on the dragend or the drop heard by the document, and its listeners with it', async () => {
    for (const end of ['dragend', 'drop']) {
        const { dom, root, dragStart } = await load();

        dragStart();
        dom.runTimers();
        assert.equal(dom.listenerCount(end), 1);
        dom.dispatch(end, {});
        assert.equal(root.hasAttribute(carrying), false, end);
        assert.equal(dom.listenerCount('dragend') + dom.listenerCount('drop'), 0, end);
    }
});

test('a drag over before its next task never marks the root', async () => {
    const { dom, root, dragStart } = await load();

    dragStart();
    dom.dispatch('dragend', {});
    dom.runTimers();
    assert.equal(root.hasAttribute(carrying), false);
});

test('a drag that is not of an appointment is left alone', async () => {
    const { dom, root, dragStart } = await load();

    dragStart(dom.el('span'));
    dom.runTimers();
    assert.equal(root.hasAttribute(carrying), false);
    assert.equal(dom.listenerCount('dragend'), 0);
});

test('detach ends a drag in progress and stops listening', async () => {
    const { dom, script, root, handlers, dragStart } = await load();

    dragStart();
    script.detach(root);
    dom.runTimers();
    assert.equal(root.hasAttribute(carrying), false);
    assert.equal(dom.listenerCount('dragend'), 0);
    assert.equal(handlers.has('dragstart'), false);
});
