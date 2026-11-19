// The horizontal arrows follow what the reader sees. A component that handles them in .NET (a spreadsheet
// column, a tree branch) counts in reading order, ArrowRight meaning "after"; in a right-to-left page the arrow
// pointing at the next column or the way into a branch is the left one. Blazor builds the arguments of the
// handler from the keydown event when it reaches the document, after a capturing listener on the component
// has run, so this swaps the two keys on the event itself, before .NET reads them. The vertical arrows and the
// default action of the key (a caret moving in a text box) are untouched.

const mirrored = new WeakSet();

export function mirrorSideKeys(event, element) {
    if (mirrored.has(event) || (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight')
        || getComputedStyle(element).direction !== 'rtl') {
        return;
    }

    mirrored.add(event);
    Object.defineProperty(event, 'key', { value: event.key === 'ArrowLeft' ? 'ArrowRight' : 'ArrowLeft', configurable: true });
}
