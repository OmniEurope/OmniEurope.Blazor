import assert from 'node:assert/strict';
import { test } from 'node:test';
import { installDom, packageScriptUrl } from './dom-stub.mjs';

// The guard of the moving CSS fields (omni-theme.js): a renderer too slow to move the scope's field (three
// frames in a row more than 100 ms apart) marks the scope data-omni-backdrop-slow, which the stylesheet holds
// still. It runs against the minimal DOM of dom-stub.mjs, a scope whose animations a test sets, and animation
// frames driven by hand with their time. What the browser paints stays with the showcase probes.
const { apply, clear } = await import(packageScriptUrl('omni-theme.js'));

let dom;
let frames;
let nextFrame;

const installPage = () => {
    dom = installDom();
    frames = new Map();
    nextFrame = 0;
    globalThis.requestAnimationFrame = callback => {
        frames.set(++nextFrame, callback);
        return nextFrame;
    };
    globalThis.cancelAnimationFrame = id => frames.delete(id);
    globalThis.window.matchMedia = () => ({ matches: false, addEventListener: () => { }, removeEventListener: () => { } });
    globalThis.document.hidden = false;
};

// A scope in the page whose field moves (the scope's own omni-scope-turn animation runs) or not.
const scope = moving => {
    const element = dom.el('div', { class: 'omni-theme-scope' });
    element.isConnected = true;
    element.moving = moving;
    element.getAnimations = () => element.moving ? [{ animationName: 'omni-scope-turn', playState: 'running' }] : [];
    return element;
};

// Runs the frames requested so far at the time given; returns how many ran.
const frameAt = now => {
    const due = [...frames.values()];
    frames.clear();
    due.forEach(callback => callback(now));
    return due.length;
};

const paint = element => apply(element, { '--omni-color-text': '#000' }, { '--omni-color-text': '#fff' }, 'light', false, false);

test('three frames in a row more than 100 ms apart hold the moving field still', () => {
    installPage();
    const element = scope(true);
    paint(element);
    frameAt(1000);
    frameAt(1150);
    frameAt(1300);
    assert.equal(element.hasAttribute('data-omni-backdrop-slow'), false, 'two slow frames are not enough');
    frameAt(1450);
    assert.equal(element.hasAttribute('data-omni-backdrop-slow'), true);
    assert.equal(frames.size, 0, 'the watch stops once the field is held');
    assert.equal(element.listeners.filter(item => item.type === 'animationstart').length, 0);
    assert.equal(dom.listenerCount('visibilitychange'), 0);
});

test('a quick frame between slow ones starts the count again', () => {
    installPage();
    const element = scope(true);
    paint(element);
    let now = 1000;
    frameAt(now);
    for (const gap of [150, 150, 16, 150, 150, 16, 150, 150]) {
        now += gap;
        frameAt(now);
    }
    assert.equal(element.hasAttribute('data-omni-backdrop-slow'), false);
    assert.equal(frames.size, 1, 'the watch goes on while the field moves');
});

test('a still field is not watched, and its start brings the watch back', () => {
    installPage();
    const element = scope(false);
    paint(element);
    frameAt(1000);
    assert.equal(frames.size, 0, 'no loop while nothing moves');
    element.moving = true;
    element.fire('animationstart', {});
    for (const now of [2000, 2200, 2400, 2600]) {
        frameAt(now);
    }
    assert.equal(element.hasAttribute('data-omni-backdrop-slow'), true);
});

test('the time spent with the page hidden is no slow frame', () => {
    installPage();
    const element = scope(true);
    paint(element);
    frameAt(1000);
    frameAt(1150);
    frameAt(1300);
    globalThis.document.hidden = false;
    dom.dispatch('visibilitychange', {});
    frameAt(9000);
    frameAt(9016);
    assert.equal(element.hasAttribute('data-omni-backdrop-slow'), false);
});

test('clear stops the watch and leaves no listener', () => {
    installPage();
    const element = scope(true);
    paint(element);
    clear(element, false, false);
    assert.equal(frames.size, 0);
    assert.equal(element.listeners.filter(item => item.type === 'animationstart').length, 0);
    assert.equal(dom.listenerCount('visibilitychange'), 0);
});

test('a scope already held still is not watched again', () => {
    installPage();
    const element = scope(true);
    element.setAttribute('data-omni-backdrop-slow', '');
    paint(element);
    assert.equal(frames.size, 0);
    assert.equal(element.listeners.length, 0);
});
