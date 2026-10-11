// DEMONSTRATION preference "database" of the showcase. Not a production store, and never used unless
// the page is opened with ?store=demo.
//
// The showcase is a static site with no server and no database. To exercise the database path of
// OmniPreferences in a real browser (store wins when newer, synchronization, repaint), Program.cs
// registers Preferences/DemoPreferenceStore.cs when the address asks for it, and that store keeps its
// rows here, in sessionStorage under its own prefix: outside the localStorage cache of the package, so
// the two can disagree as a remote database and a device cache do. A probe plays "another device" by
// writing a row under that prefix directly. sessionStorage lives as long as the tab, reloads included,
// and is never shared with another tab or a later visit.
//
// ?store=demo turns the store on; &sync=<seconds> also sets the periodic synchronization, which is off
// otherwise (the tab coming back into view still synchronizes, and the first read of a key asks the store).
window.omniShowcaseDemoStore = {
    prefix: 'omni-showcase-demo-store:',

    // What Program.cs read at startup, kept for a probe once the address has changed: null before.
    active: null,

    // -1: no demo store; 0: demo store without periodic synchronization; n: every n seconds.
    request() {
        const query = new URLSearchParams(window.location.search);
        const seconds = Number.parseInt(query.get('sync') ?? '', 10);
        this.active = query.get('store') !== 'demo' ? -1 : Number.isFinite(seconds) && seconds > 0 ? seconds : 0;
        return this.active;
    },

    get(key) {
        return window.sessionStorage.getItem(this.prefix + key);
    },

    all() {
        const rows = [];
        for (let index = 0; index < window.sessionStorage.length; index++) {
            const name = window.sessionStorage.key(index);
            if (name !== null && name.startsWith(this.prefix)) {
                rows.push(window.sessionStorage.getItem(name));
            }
        }

        return rows;
    },

    save(key, row) {
        window.sessionStorage.setItem(this.prefix + key, row);
    }
};
