import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// OmniBackToTop: the frame is marked data-shown past the threshold of the content that scrolls (a TargetId, the
// scrollable OmniMain, or the document), the press brings that content to its top (instant under reduced motion) and
// moves the focus to the top of the content. The stub elements record their scrollTo and focus calls; the real
// scrolling, the fade and the focus ring are proved in a browser on the showcase page.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const dom = installDom();
const { attach, detach, scrollToTop } = await import(pathToFileURL(path.join(wwwroot, 'omni-back-to-top.js')).href);

// The stub document has querySelectorAll only.
document.querySelector = selector => document.querySelectorAll(selector)[0] ?? null;

let reduced = false;
window.matchMedia = query => ({ matches: query === '(prefers-reduced-motion: reduce)' && reduced });

// A stub element that scrolls: scrollTop is read by the module, scrollTo and focus are recorded.
const scrollable = (element, scrollTop = 0) => {
    element.scrollTop = scrollTop;
    element.scrolls = [];
    element.scrollTo = options => { element.scrolls.push(options); element.scrollTop = options.top; };
    element.focusCalls = [];
    element.focus = options => { element.focusCalls.push(options); document.activeElement = element; };
    return element;
};

// The page: a frame holding the button, and the document's own scroller.
const page = ({ main = null } = {}) => {
    dom.body.children = [];
    const button = dom.el('button', { type: 'button' });
    const frame = dom.el('div', { class: 'omni-back-to-top' }, button);
    if (main) {
        main.append(frame);
        dom.body.append(main);
    } else {
        dom.body.append(frame);
    }

    const root = scrollable(dom.el('html'));
    document.scrollingElement = root;
    document.documentElement = root;
    document.activeElement = dom.body;
    return { frame, button, root };
};

// The listeners the module puts on the document: the capturing scroll one is what hears every scroller.
const scrollListeners = new Set();
const addListener = document.addEventListener;
const removeListener = document.removeEventListener;
document.addEventListener = (type, handler, options) => {
    if (type === 'scroll') {
        assert.equal(options?.capture, true, 'the scroll listener must capture, a scroll event does not bubble');
        scrollListeners.add(handler);
    }

    addListener(type, handler, options);
};
document.removeEventListener = (type, handler, options) => {
    if (type === 'scroll') {
        scrollListeners.delete(handler);
    }

    removeListener(type, handler, options);
};
const scrolled = () => {
    dom.dispatch('scroll', {});
    dom.runFrames();
};

test('the button follows the scrollable main area of the layout and shows past the threshold only', () => {
    const main = scrollable(dom.el('main', { class: 'omni-main omni-main--scrollable', tabindex: '-1' }));
    const { frame } = page({ main });
    attach(frame, null, 400);
    assert.equal(frame.hasAttribute('data-shown'), false);

    main.scrollTop = 400;
    scrolled();
    assert.equal(frame.hasAttribute('data-shown'), false, 'exactly at the threshold the button stays hidden');

    main.scrollTop = 401;
    scrolled();
    assert.equal(frame.hasAttribute('data-shown'), true);

    main.scrollTop = 10;
    scrolled();
    assert.equal(frame.hasAttribute('data-shown'), false);
    detach(frame);
});

test('a frame outside the main area still follows the first scrollable main of the page', () => {
    const { frame } = page();
    const main = scrollable(dom.el('main', { class: 'omni-main omni-main--scrollable' }), 900);
    dom.body.append(main);

    attach(frame, null, 400);

    assert.equal(frame.hasAttribute('data-shown'), true);
    detach(frame);
});

test('without a scrollable main the document is followed, and the press focuses the main landmark', () => {
    const main = dom.el('main', { class: 'omni-main' });
    main.focusCalls = [];
    main.focus = options => { main.focusCalls.push(options); document.activeElement = main; };
    const { frame, button, root } = page({ main });
    attach(frame, null, 100);

    root.scrollTop = 150;
    scrolled();
    assert.equal(frame.hasAttribute('data-shown'), true);

    document.activeElement = button;
    reduced = false;
    assert.equal(scrollToTop(frame), true);

    assert.deepEqual(root.scrolls, [{ top: 0, behavior: 'smooth' }]);
    // The landmark could not take the focus: it is given tabindex -1, then focused without scrolling.
    assert.equal(main.getAttribute('tabindex'), '-1');
    assert.deepEqual(main.focusCalls, [{ preventScroll: true }]);
    assert.equal(document.activeElement, main);
    detach(frame);
});

test('under reduced motion the content goes back at once, and the focus moves to the scrolled area', () => {
    const main = scrollable(dom.el('main', { class: 'omni-main omni-main--scrollable', tabindex: '-1' }), 2000);
    const { frame, button } = page({ main });
    attach(frame, null, 400);
    document.activeElement = button;
    reduced = true;

    scrollToTop(frame);

    assert.deepEqual(main.scrolls, [{ top: 0, behavior: 'instant' }]);
    assert.deepEqual(main.focusCalls, [{ preventScroll: true }]);
    assert.equal(main.getAttribute('tabindex'), '-1', 'an OmniMain keeps its own tabindex');
    reduced = false;
    detach(frame);
});

test('a TargetId names the scroller; while it names nothing the button stays hidden and the press does nothing', () => {
    const { frame } = page();
    attach(frame, 'article', 0);
    scrolled();
    assert.equal(frame.hasAttribute('data-shown'), false);
    assert.equal(scrollToTop(frame), false);

    const article = scrollable(dom.el('section', { id: 'article', tabindex: '0' }), 5);
    dom.body.append(article);
    scrolled();
    assert.equal(frame.hasAttribute('data-shown'), true, 'a zero threshold shows it as soon as the content leaves its top');

    assert.equal(scrollToTop(frame), true);
    assert.deepEqual(article.scrolls, [{ top: 0, behavior: 'smooth' }]);
    assert.equal(article.getAttribute('tabindex'), '0', 'a target that takes the focus keeps its tabindex');
    detach(frame);
});

test('the document without a main landmark: the button lets go of the focus', () => {
    const { frame, button, root } = page();
    let blurred = 0;
    button.blur = () => { blurred++; document.activeElement = dom.body; };
    attach(frame, null, 0);
    document.activeElement = button;

    scrollToTop(frame);

    assert.equal(blurred, 1);
    assert.deepEqual(root.scrolls, [{ top: 0, behavior: 'smooth' }]);
    detach(frame);
});

test('a button that holds the focus stays shown below the threshold', () => {
    const main = scrollable(dom.el('main', { class: 'omni-main omni-main--scrollable' }), 0);
    const { frame, button } = page({ main });
    attach(frame, null, 400);
    document.activeElement = button;

    scrolled();

    assert.equal(frame.hasAttribute('data-shown'), true);
    document.activeElement = dom.body;
    detach(frame);
});

test('the focus coming back to the button while it fades out shows it again, without waiting for a scroll', () => {
    const main = scrollable(dom.el('main', { class: 'omni-main omni-main--scrollable' }), 0);
    const { frame, button } = page({ main });
    attach(frame, null, 400);
    assert.equal(frame.hasAttribute('data-shown'), false);

    document.activeElement = button;
    frame.fire('focusin', {});
    assert.equal(frame.hasAttribute('data-shown'), true);

    document.activeElement = dom.body;
    frame.fire('focusout', {});
    assert.equal(frame.hasAttribute('data-shown'), false);

    detach(frame);
    assert.equal(frame.listeners.length, 0, 'detach removes the focus listeners');
});

test('a second attach takes the new options without a second listener, and detach releases everything', () => {
    const main = scrollable(dom.el('main', { class: 'omni-main omni-main--scrollable' }), 250);
    const { frame } = page({ main });
    attach(frame, null, 400);
    assert.equal(frame.hasAttribute('data-shown'), false);
    assert.equal(scrollListeners.size, 1);

    attach(frame, null, 200);

    assert.equal(frame.hasAttribute('data-shown'), true);
    assert.equal(scrollListeners.size, 1);

    detach(frame);
    assert.equal(scrollListeners.size, 0);
    assert.equal(frame.hasAttribute('data-shown'), false);
    // Detaching again, or a frame never attached, is harmless.
    detach(frame);
    detach(dom.el('div'));
});
