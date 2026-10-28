// A minimal DOM for the unit tests of the package scripts, run by node without any dependency. It
// implements only what the scripts under test read: elements with attributes, classes, style
// variables, simple selectors (tag, .class, [attribute], [attribute="value"], :not(...), commas),
// listeners dispatched by hand and a fake timer queue. It proves the logic of a script; the layout,
// the focus and the real events stay the business of the browser probes.

const splitTop = (text, separator) => {
    const parts = [];
    let depth = 0;
    let current = '';
    for (const character of text) {
        if (character === '(') depth++;
        if (character === ')') depth--;
        if (character === separator && depth === 0) {
            parts.push(current);
            current = '';
        } else {
            current += character;
        }
    }

    parts.push(current);
    return parts.map(part => part.trim()).filter(Boolean);
};

const matchesCompound = (element, compound) => {
    let rest = compound;
    for (const [whole, inner] of compound.matchAll(/:not\(([^)]*)\)/g)) {
        if (matchesCompound(element, inner)) {
            return false;
        }

        rest = rest.replace(whole, '');
    }

    const tag = /^[a-zA-Z][\w-]*/.exec(rest)?.[0];
    if (tag && element.localName !== tag.toLowerCase()) {
        return false;
    }

    for (const [, name] of rest.matchAll(/\.([\w-]+)/g)) {
        if (!element.classList.contains(name)) {
            return false;
        }
    }

    for (const [, name, value] of rest.matchAll(/\[([\w-]+)(?:="([^"]*)")?\]/g)) {
        if (!element.hasAttribute(name) || (value !== undefined && element.getAttribute(name) !== value)) {
            return false;
        }
    }

    return true;
};

const matchesSelector = (element, selector) => splitTop(selector, ',').some(compound => matchesCompound(element, compound));

class Node {}

class Element extends Node {
    isConnected = true;

    constructor(localName, attributes = {}, ...children) {
        super();
        this.localName = localName;
        this.tagName = localName.toUpperCase();
        this.parentElement = null;
        this.children = [];
        this.attributes = new Map();
        this.rect = { left: 0, top: 0, right: 0, bottom: 0, width: 0, height: 0 };
        this.focused = false;
        this.ownText = '';
        const classes = new Set();
        this.classList = {
            add: (...names) => names.forEach(name => classes.add(name)),
            remove: (...names) => names.forEach(name => classes.delete(name)),
            contains: name => classes.has(name),
            toggle: (name, force) => {
                const on = force ?? !classes.has(name);
                if (on) {
                    classes.add(name);
                } else {
                    classes.delete(name);
                }

                return on;
            }
        };
        const variables = new Map();
        this.style = {
            setProperty: (name, value) => variables.set(name, String(value)),
            getPropertyValue: name => variables.get(name) ?? '',
            removeProperty: name => variables.delete(name),
            fontFamily: ''
        };
        this.dataset = {};
        for (const [name, value] of Object.entries(attributes)) {
            if (name === 'class') {
                value.split(' ').filter(Boolean).forEach(item => classes.add(item));
            } else if (name === 'text') {
                this.ownText = value;
            } else {
                this.setAttribute(name, value);
            }
        }

        children.forEach(child => this.append(child));
    }

    get textContent() { return this.ownText + this.children.map(child => child.textContent).join(''); }
    set textContent(value) { this.ownText = String(value); this.children = []; }
    get scrollWidth() { return this.rect.width; }
    get clientWidth() { return this.rect.width; }
    getAttribute(name) { return this.attributes.get(name) ?? null; }
    setAttribute(name, value) { this.attributes.set(name, String(value)); }
    removeAttribute(name) { this.attributes.delete(name); }
    hasAttribute(name) { return this.attributes.has(name); }
    append(child) { child.parentElement = this; this.children.push(child); }
    appendChild(child) { this.append(child); return child; }
    prepend(child) { child.parentElement = this; this.children.unshift(child); }
    addEventListener() { }
    removeEventListener() { }
    getBoundingClientRect() { return this.rect; }
    focus() { this.focused = true; }
    matches(selector) { return matchesSelector(this, selector); }
    contains(other) {
        for (let node = other; node; node = node.parentElement) {
            if (node === this) {
                return true;
            }
        }

        return false;
    }

    closest(selector) {
        for (let node = this; node; node = node.parentElement) {
            if (node.matches(selector)) {
                return node;
            }
        }

        return null;
    }

    querySelectorAll(selector) {
        return this.children.flatMap(child => [...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)]);
    }

    querySelector(selector) { return this.querySelectorAll(selector)[0] ?? null; }
}

class HTMLElement extends Element {}
class SVGElement extends Element {}

// Installs a fresh document, window and timer queue as globals and returns what a test drives.
export function installDom() {
    const body = new HTMLElement('body');
    const listeners = new Map();
    const timers = [];
    const frames = new Map();
    let nextFrame = 0;
    const events = {
        addEventListener: (type, handler) => listeners.set(type, [...(listeners.get(type) ?? []), handler]),
        removeEventListener: (type, handler) => listeners.set(type, (listeners.get(type) ?? []).filter(item => item !== handler))
    };
    const documentStub = {
        ...events,
        body,
        activeElement: body,
        documentElement: { clientWidth: 1024, clientHeight: 768 },
        createElement: name => new HTMLElement(name),
        getElementById: id => body.querySelectorAll(`[id="${id}"]`)[0] ?? null,
        querySelectorAll: selector => body.querySelectorAll(selector)
    };
    const fakeSetTimeout = callback => {
        timers.push(callback);
        return timers.length;
    };
    Object.assign(globalThis, {
        Node,
        Element,
        HTMLElement,
        SVGElement,
        document: documentStub,
        window: {
            ...events,
            setTimeout: fakeSetTimeout,
            clearTimeout: () => { },
            requestAnimationFrame: callback => {
                frames.set(++nextFrame, callback);
                return nextFrame;
            },
            cancelAnimationFrame: id => frames.delete(id)
        },
        setTimeout: fakeSetTimeout,
        getComputedStyle: () => ({ visibility: 'visible', direction: 'ltr', fontFamily: '' })
    });

    return {
        body,
        listenerCount: type => (listeners.get(type) ?? []).length,
        dispatch: (type, event) => (listeners.get(type) ?? []).slice().forEach(handler => handler(event)),
        // Runs the timers queued so far, once each; the ones they queue wait for the next call.
        runTimers: () => timers.splice(0).forEach(callback => callback()),
        pendingTimers: () => timers.length,
        // Runs the animation frames requested so far, once each; the ones they request wait for the next call.
        runFrames: () => {
            const due = [...frames.values()];
            frames.clear();
            due.forEach(callback => callback());
        },
        el: (name, attributes, ...children) => new HTMLElement(name, attributes, ...children)
    };
}

// A reference to a .NET object, recording what the script reports to it.
export function dotnetRecorder() {
    const calls = [];
    return {
        calls,
        invokeMethodAsync: (method, ...args) => {
            calls.push([method, ...args]);
            return Promise.resolve();
        }
    };
}
