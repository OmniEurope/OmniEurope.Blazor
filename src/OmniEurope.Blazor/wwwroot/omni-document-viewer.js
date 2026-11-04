// The script of OmniDocumentViewer: it sizes the pages, applies the zoom and tells .NET which page is in view.
//
// Sizes: a page is drawn at its natural width in CSS pixels, which is the width the host declared (points, at 96 dpi),
// else the width of its picture once loaded, else an A4 width. The script writes it, with the declared height, as
// custom properties on the page (--omni-document-page-width, --omni-document-page-height); the stylesheet reads them.
// The zoom is one custom property on the viewport (--omni-document-zoom), read by the CSS zoom of the pages. Only the
// CSSOM is written, never a style attribute, so the strict CSP holds.
//
// Fit width: when asked to fit, the script computes the zoom that makes the widest page fill the viewport (its width
// less its padding, rounded down to a whole percent), again at each resize and each picture that finishes loading, and
// reports it to .NET, which shows it and raises ZoomChanged. The zoom is kept between the limits .NET gives.
//
// Page in view: the page that fills most of the viewport, reported on scroll (once per frame).
//
// .NET is told only when the zoom or the page differs from what it already knows.
const PX_PER_PT = 96 / 72;
const FALLBACK_WIDTH = 794;
const TOLERANCE = 0.0001;
const states = new WeakMap();

const pagesOf = viewport => [...viewport.querySelectorAll('.omni-document-viewer__page')];

// The width of a page at zoom 1, in CSS pixels.
function pageWidth(page) {
    const declared = Number(page.getAttribute('data-width-pt'));
    if (declared > 0) {
        return declared * PX_PER_PT;
    }

    const natural = page.querySelector('img')?.naturalWidth ?? 0;
    return natural > 0 ? natural : FALLBACK_WIDTH;
}

// The position, from 1, of the page that shows most inside the viewport; 0 without pages.
function pageInView(viewport, pages) {
    const view = viewport.getBoundingClientRect();
    let best = 0;
    let most = -Infinity;
    pages.forEach((page, index) => {
        const box = page.getBoundingClientRect();
        const visible = Math.min(box.bottom, view.bottom) - Math.max(box.top, view.top);
        if (visible > most) {
            most = visible;
            best = index + 1;
        }
    });

    return best;
}

// The zoom that makes a page of the given width fill the viewport; the current one when the viewport has no width (hidden).
function fitZoom(viewport, widest, current) {
    const box = getComputedStyle(viewport);
    const available = viewport.clientWidth - (parseFloat(box.paddingLeft) || 0) - (parseFloat(box.paddingRight) || 0);
    return available > 0 ? Math.floor((available / widest) * 100) / 100 : current;
}

function report(viewport, state, pages) {
    const page = pageInView(viewport, pages);
    if (Math.abs(state.zoom - state.reportedZoom) > TOLERANCE || page !== state.reportedPage) {
        state.reportedZoom = state.zoom;
        state.reportedPage = page;
        state.dotNet.invokeMethodAsync('OnViewChangedAsync', state.zoom, page);
    }
}

function layout(viewport) {
    const state = states.get(viewport);
    const pages = pagesOf(viewport);
    let widest = 0;
    for (const page of pages) {
        const width = pageWidth(page);
        widest = Math.max(widest, width);
        page.style.setProperty('--omni-document-page-width', `${width}px`);
        const height = Number(page.getAttribute('data-height-pt'));
        if (height > 0) {
            page.style.setProperty('--omni-document-page-height', `${height * PX_PER_PT}px`);
        } else {
            page.style.removeProperty('--omni-document-page-height');
        }
    }

    if (state.fit && widest > 0) {
        state.zoom = fitZoom(viewport, widest, state.zoom);
    }

    state.zoom = Math.min(state.maximum, Math.max(state.minimum, state.zoom));
    viewport.style.setProperty('--omni-document-zoom', String(state.zoom));
    report(viewport, state, pages);
}

export function attach(viewport, dotNet, zoom, fit, minimum, maximum) {
    const state = { dotNet, zoom, fit, minimum, maximum, reportedZoom: zoom, reportedPage: 1, frame: 0 };
    state.observer = new ResizeObserver(() => layout(viewport));
    state.onLoad = () => layout(viewport);
    state.onScroll = () => {
        if (!state.frame) {
            state.frame = requestAnimationFrame(() => {
                state.frame = 0;
                report(viewport, state, pagesOf(viewport));
            });
        }
    };

    states.set(viewport, state);
    state.observer.observe(viewport);
    // A picture's load does not bubble: it is caught on the way down, and sizes the page that waited for it.
    viewport.addEventListener('load', state.onLoad, true);
    viewport.addEventListener('scroll', state.onScroll, { passive: true });
    layout(viewport);
}

// The zoom and fit .NET holds, and the pages it just drew.
export function update(viewport, zoom, fit) {
    const state = states.get(viewport);
    state.zoom = zoom;
    state.fit = fit;
    state.reportedZoom = zoom;
    layout(viewport);
}

export function detach(viewport) {
    const state = states.get(viewport);
    state.observer.disconnect();
    viewport.removeEventListener('load', state.onLoad, true);
    viewport.removeEventListener('scroll', state.onScroll);
    cancelAnimationFrame(state.frame);
    states.delete(viewport);
    viewport.style.removeProperty('--omni-document-zoom');
}
