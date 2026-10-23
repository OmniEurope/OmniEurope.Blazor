import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { dotnetRecorder, installDom } from './dom-stub.mjs';

const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
let tests = 0;

// A fresh page, and a menu key of its own: the menus keep their state per key, in a module that is
// loaded once for the whole file.
const fixture = async () => {
    const dom = installDom();
    const focus = await import(pathToFileURL(path.join(wwwroot, 'omni-focus.js')).href);
    const dotnet = dotnetRecorder();
    const key = `menu-${tests++}`;
    return {
        dom,
        dotnet,
        open: () => {
            const anchor = dom.el('button', { 'aria-haspopup': 'menu' });
            dom.body.append(anchor);
            focus.openMenu('m-menu', key, anchor, 'start', null, null, false, true, dotnet);
        },
        arrive: () => {
            const menu = dom.el('div', { id: 'm-menu', role: 'menu' });
            dom.body.append(menu);
            return menu;
        },
        giveUp: () => {
            for (let attempt = 0; attempt < 12; attempt++) {
                dom.runTimers();
            }
        }
    };
};

test('a menu present at once is placed and nothing is reported', async () => {
    const { arrive, open, dotnet } = await fixture();
    const menu = arrive();

    open();

    assert.ok(menu.hasAttribute('data-omni-placed'));
    assert.deepEqual(dotnet.calls, []);
});

test('a menu that arrives within the attempts is placed and nothing is reported', async () => {
    const { arrive, dom, dotnet, open } = await fixture();
    open();
    for (let attempt = 0; attempt < 3; attempt++) {
        dom.runTimers();
    }

    const menu = arrive();
    dom.runTimers();

    assert.ok(menu.hasAttribute('data-omni-placed'));
    assert.deepEqual(dotnet.calls, []);
});

test('a menu that never arrives is reported to .NET as dismissed, so it does not stay open and invisible', async () => {
    const { dom, dotnet, giveUp, open } = await fixture();
    open();

    giveUp();

    assert.deepEqual(dotnet.calls, [['OmniMenu.Dismiss', false]]);
    assert.equal(dom.pendingTimers(), 0);
});

test('giving up detaches the press-outside listener of the menu', async () => {
    const { dom, giveUp, open } = await fixture();
    open();
    assert.equal(dom.listenerCount('pointerdown'), 1);

    giveUp();

    assert.equal(dom.listenerCount('pointerdown'), 0);
});

test('a menu asked again after giving up is looked for again and placed when it arrives', async () => {
    const { arrive, dom, dotnet, giveUp, open } = await fixture();
    open();
    giveUp();

    open();
    dom.runTimers();
    const menu = arrive();
    dom.runTimers();

    assert.ok(menu.hasAttribute('data-omni-placed'));
    assert.equal(dotnet.calls.length, 1);
});
