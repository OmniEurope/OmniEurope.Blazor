import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { dotnetRecorder, installDom } from './dom-stub.mjs';

// The scripts that call .NET on their own (the grid, the board, the log viewer) make no call while Blazor
// Server shows its connection as lost. The send failure itself happens inside blazor.web.js, where no catch
// of the package reaches it, and the promise of the call is then never settled: what is proved here is that
// no call is made during the cut, and that nothing stays held once the connection is back.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const load = file => import(pathToFileURL(path.join(wwwroot, file)).href);

// A page with the reconnection display of Blazor Server (the host's own element), shown or hidden by the test.
const fixture = () => {
    const dom = installDom();
    const indicator = dom.el('div', { id: 'components-reconnect-modal' });
    dom.body.append(indicator);
    return {
        dom,
        cut: () => indicator.classList.add('components-reconnect-show'),
        back: () => indicator.classList.remove('components-reconnect-show')
    };
};

// Listeners kept by type, so a test can fire the one the script set.
const listening = element => {
    const handlers = new Map();
    element.addEventListener = (type, handler) => handlers.set(type, handler);
    return (type, event) => handlers.get(type)?.(event);
};

test('notifyDotNet of the grid makes no call while the connection is lost, and calls again once it is back', async () => {
    const { notifyDotNet } = await load('grid/notify.js');
    const { cut, back } = fixture();
    const dotnet = dotnetRecorder();

    cut();
    notifyDotNet(() => true, dotnet, 'OnColumnResizedAsync', 'name', 120);
    assert.deepEqual(dotnet.calls, []);

    back();
    notifyDotNet(() => true, dotnet, 'OnColumnResizedAsync', 'name', 120);
    assert.deepEqual(dotnet.calls, [['OnColumnResizedAsync', 'name', 120]]);
});

test('a column asked to fit while the connection is lost is not left measuring: it fits once the connection is back', async () => {
    const { attachResize, autoFitColumn } = await load('grid/resize.js');
    const { dom, cut, back } = fixture();
    const viewport = dom.el('div');
    dom.body.append(viewport);
    // As Blazor does when its send failed: the call is recorded, its promise never settles.
    const calls = [];
    const dotnet = { invokeMethodAsync: (method, ...args) => { calls.push([method, ...args]); return new Promise(() => { }); } };
    attachResize(viewport, dotnet, 48);

    cut();
    autoFitColumn(viewport, 'name');
    assert.deepEqual(calls, [], 'no question is asked that could never be answered');

    back();
    autoFitColumn(viewport, 'name');
    assert.deepEqual(calls, [['GetColumnAutoFitTexts', 'name']], 'the column was not left measuring by the request made during the cut');
});

test('a key on a kanban card is still kept from the page during the cut, and is not sent to .NET', async () => {
    const { attach } = await load('omni-kanban.js');
    const { dom, cut, back } = fixture();
    const card = dom.el('div', { 'data-omni-kanban-card': 'card-1' });
    const root = dom.el('div', {}, dom.el('div', { 'data-omni-kanban-column': 'todo' }, card));
    dom.body.append(root);
    const fire = listening(root);
    const dotnet = dotnetRecorder();
    attach(root, dotnet);
    const press = () => {
        const event = { key: 'Enter', target: card, prevented: false, preventDefault() { this.prevented = true; } };
        fire('keydown', event);
        return event;
    };

    cut();
    assert.ok(press().prevented, 'Enter on a card never activates or scrolls the page');
    assert.deepEqual(dotnet.calls, []);

    back();
    press();
    assert.deepEqual(dotnet.calls, [['OnCardKey', 'card-1', 'Enter']]);
});

test('the log viewer reports no scroll while the connection is lost, and reports the next one once it is back', async () => {
    const { attach } = await load('omni-log-viewer.js');
    const { dom, cut, back } = fixture();
    const viewport = dom.el('div');
    Object.assign(viewport, { scrollTop: 0, scrollHeight: 400, clientHeight: 100 });
    dom.body.append(viewport);
    const fire = listening(viewport);
    const dotnet = dotnetRecorder();
    attach(viewport, dotnet, false);
    const scrollTo = top => {
        viewport.scrollTop = top;
        fire('scroll', {});
        dom.runFrames();
    };

    cut();
    scrollTo(50);
    assert.deepEqual(dotnet.calls, []);

    back();
    scrollTo(80);
    assert.deepEqual(dotnet.calls, [['OnViewportChangedAsync', 80, 100, false, false]]);
});
