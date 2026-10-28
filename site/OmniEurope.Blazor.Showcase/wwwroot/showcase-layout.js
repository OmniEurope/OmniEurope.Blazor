// Width of the showcase shell: tells the layout when the window is a phone's, so the side menu pushes
// the page beside it on a wide screen and floats over it, with its veil, on a narrow one.
//
// The layout reads the state once (narrow) before it subscribes (watch), and the subscription reports
// only the crossings of the breakpoint afterwards: the stylesheet of the library switches at the same
// width. Reading the width once at startup left the menu in whichever state the first size chose.
window.omniShowcaseLayout = (() => {
    const query = window.matchMedia('(max-width: 47.99rem)');
    let owner = null;

    function crossed(event) {
        owner?.invokeMethodAsync('OnViewportChanged', event.matches);
    }

    return {
        narrow() {
            return query.matches;
        },

        watch(dotnet) {
            query.removeEventListener('change', crossed);
            owner = dotnet;
            query.addEventListener('change', crossed);
        },

        unwatch() {
            query.removeEventListener('change', crossed);
            owner = null;
        }
    };
})();
