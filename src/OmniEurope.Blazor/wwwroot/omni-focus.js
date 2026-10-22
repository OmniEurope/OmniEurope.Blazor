// Focus management, the module the components import (OmniModules.Focus): the Escape listener of a
// floating sidebar and the phone width watch of a pushing one live here; everything else is split by
// responsibility under ./focus/ and re-exported, so every export keeps this address: the focus trap
// and the focus return of dialogs and windows (dialog.js), the non-modal popover (popover.js), the
// date and time pickers (picker.js), the native <details> disclosures and fieldsets (disclosure.js),
// the menus (menus.js), the page keys (keys.js) and the tab strips (tabs.js).
import { notify } from './shared/notify.js';

export { activateDialog, activateWindow, focusBoundary, restoreFocus } from './focus/dialog.js';
export { attachPopover, detachPopover } from './focus/popover.js';
export { attachPicker, detachPicker, focusPickerItem } from './focus/picker.js';
export { configureDisclosure, disposeDisclosure, observeFieldsetToggle, disposeFieldsetToggle } from './focus/disclosure.js';
export { openMenu, closeMenu, moveMenuFocus } from './focus/menus.js';
export { enablePageKeys } from './focus/keys.js';
export {
    configureTabs,
    disposeTabs,
    configureTabsOverflow,
    configureScrollOverflow,
    disposeScrollOverflow,
    configureSelectBarOverflow,
    configureSidebarOverflow,
    disposeSidebarOverflow,
    disposeTabsOverflow,
    attachTabsWheelScope,
    detachTabsWheelScope
} from './focus/tabs.js';

// A floating sidebar closes on Escape wherever the focus is: after the toggle opened it, the focus
// stays on the toggle in the header, outside the panel, where a key handler on the panel never hears.
const escapeListeners = new Map();

export function attachEscape(owner, dotnet) {
    detachEscape(owner);
    const listener = event => {
        if (event.key === 'Escape' && !event.defaultPrevented) {
            notify(dotnet, 'CloseFromEscapeAsync');
        }
    };
    document.addEventListener('keydown', listener);
    escapeListeners.set(owner, listener);
}

export function detachEscape(owner) {
    const listener = escapeListeners.get(owner);
    if (listener) {
        document.removeEventListener('keydown', listener);
        escapeListeners.delete(owner);
    }
}

// A pushing sidebar floats over the page on a phone, under the same 40rem threshold as the stylesheet:
// the sidebar is told the width class now and on every change.
const narrowWatchers = new Map();

export function watchNarrow(owner, dotnet) {
    unwatchNarrow(owner);
    const query = window.matchMedia('(max-width: 39.99rem)');
    const listener = () => notify(dotnet, 'SetNarrowAsync', query.matches);
    query.addEventListener('change', listener);
    narrowWatchers.set(owner, { query, listener });
    listener();
}

export function unwatchNarrow(owner) {
    const watcher = narrowWatchers.get(owner);
    if (watcher) {
        watcher.query.removeEventListener('change', watcher.listener);
        narrowWatchers.delete(owner);
    }
}
