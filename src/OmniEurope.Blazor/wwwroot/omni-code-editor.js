// Monaco side of OmniCodeEditor.
//
// Monaco is not shipped in the package: its editor alone weighs several times the package budget,
// and it cannot run under the strict policy the rest of the package keeps, since it writes style
// elements at run time and starts web workers. The host serves the "min/vs" folder of the
// monaco-editor package itself and passes its address; this module loads it once, with Monaco's own
// AMD loader, from that address and from nowhere else. When it cannot be loaded, the component keeps
// its plain textarea, which works everywhere.
const editors = new WeakMap();
const statesByModel = new WeakMap();
const linkTargets = new Map();
const languagesWithLinks = new Set();
const monacoLocales = new Set(['de', 'es', 'fr', 'it', 'ja', 'ko', 'ru', 'zh-cn', 'zh-tw']);
const changeDelay = 150;
let loading = null;
let linkCounter = 0;
let openerRegistered = false;

export function setHeight(root, height) {
    if (!root) {
        return;
    }

    if (height) {
        root.style.setProperty('--omni-code-editor-height', height);
    }
    else {
        root.style.removeProperty('--omni-code-editor-height');
    }
}

export function load(basePath, culture, timeout) {
    if (window.monaco?.editor) {
        return Promise.resolve(true);
    }

    if (loading) {
        return loading;
    }

    const base = new URL(String(basePath).replace(/\/+$/, ''), document.baseURI).href;
    const locale = monacoLocale(culture);
    loading = new Promise(resolve => {
        const timer = window.setTimeout(() => resolve(false), timeout > 0 ? timeout : 30000);
        const finish = succeeded => {
            window.clearTimeout(timer);
            resolve(succeeded);
        };
        // Workers are started straight from the served file rather than through the blob: wrapper
        // Monaco uses by default, so worker-src 'self' is enough. A host's own setting wins.
        window.MonacoEnvironment ??= { getWorkerUrl: () => `${base}/base/worker/workerMain.js` };
        const start = () => {
            const configuration = { paths: { vs: base } };
            if (locale) {
                configuration['vs/nls'] = { availableLanguages: { '*': locale } };
            }

            window.require.config(configuration);
            window.require(['vs/editor/editor.main'], () => finish(Boolean(window.monaco?.editor)), () => finish(false));
        };

        if (typeof window.require?.config === 'function') {
            start();
            return;
        }

        const script = document.createElement('script');
        script.src = `${base}/loader.js`;
        script.addEventListener('load', start);
        script.addEventListener('error', () => finish(false));
        document.head.appendChild(script);
    });

    // A failed attempt is not remembered: the files may be deployed, or the network back, by the
    // time the next editor asks.
    loading.then(succeeded => {
        if (!succeeded) {
            loading = null;
        }
    });
    return loading;
}

export function mount(host, dotnet, options) {
    const monaco = window.monaco;
    if (!monaco?.editor || !host || editors.has(host)) {
        return false;
    }

    const state = {
        host,
        dotnet,
        editor: null,
        timer: 0,
        cursorTimer: 0,
        applying: false,
        links: [],
        tokens: [],
        disposables: [],
        observer: null,
        media: null
    };
    editors.set(host, state);
    applyTheme(state);
    state.editor = monaco.editor.create(host, {
        value: options?.value ?? '',
        language: options?.language || 'plaintext',
        readOnly: Boolean(options?.readOnly),
        domReadOnly: Boolean(options?.readOnly),
        lineNumbers: options?.lineNumbers === false ? 'off' : 'on',
        wordWrap: options?.wordWrap ? 'on' : 'off',
        tabSize: options?.tabSize > 0 ? options.tabSize : 4,
        ariaLabel: options?.label ?? '',
        automaticLayout: true,
        fixedOverflowWidgets: true,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        fontSize: 14,
        theme: themeName(state)
    });
    statesByModel.set(state.editor.getModel(), state);
    state.disposables.push(state.editor.onDidChangeModelContent(() => {
        if (!state.applying) {
            schedule(state);
        }
    }));
    state.disposables.push(state.editor.onDidBlurEditorText(() => flush(state)));
    state.disposables.push(state.editor.onDidChangeCursorPosition(() => scheduleCursor(state)));
    setLinks(state, options?.links);
    watchTheme(state);
    scheduleCursor(state);
    return true;
}

export function setValue(host, value) {
    const state = editors.get(host);
    if (!state) {
        return;
    }

    const model = state.editor.getModel();
    const text = value ?? '';
    if (model.getValue() === text) {
        return;
    }

    // pushEditOperations rather than setValue: the change becomes one step of the undo stack
    // instead of wiping it, and it goes through a read-only editor too.
    state.applying = true;
    try {
        state.editor.pushUndoStop();
        model.pushEditOperations([], [{ range: model.getFullModelRange(), text }], () => null);
        state.editor.pushUndoStop();
    }
    finally {
        state.applying = false;
    }
}

export function configure(host, options) {
    const state = editors.get(host);
    if (!state) {
        return;
    }

    const monaco = window.monaco;
    const model = state.editor.getModel();
    const language = options?.language || 'plaintext';
    if (model.getLanguageId() !== language) {
        monaco.editor.setModelLanguage(model, language);
    }

    state.editor.updateOptions({
        readOnly: Boolean(options?.readOnly),
        domReadOnly: Boolean(options?.readOnly),
        lineNumbers: options?.lineNumbers === false ? 'off' : 'on',
        wordWrap: options?.wordWrap ? 'on' : 'off',
        tabSize: options?.tabSize > 0 ? options.tabSize : 4,
        ariaLabel: options?.label ?? ''
    });
    setLinks(state, options?.links);
}

export function read(host) {
    const state = editors.get(host);
    if (!state) {
        return null;
    }

    window.clearTimeout(state.timer);
    state.timer = 0;
    return state.editor.getValue();
}

export function focus(host) {
    editors.get(host)?.editor.focus();
}

export function dispose(host) {
    const state = editors.get(host);
    if (!state) {
        return;
    }

    window.clearTimeout(state.timer);
    window.clearTimeout(state.cursorTimer);
    state.observer?.disconnect();
    state.media?.removeEventListener('change', state.repaint);
    for (const disposable of state.disposables) {
        disposable.dispose();
    }

    forgetTokens(state);
    const model = state.editor.getModel();
    state.editor.dispose();
    model?.dispose();
    editors.delete(host);
}

function schedule(state) {
    window.clearTimeout(state.timer);
    state.timer = window.setTimeout(() => send(state), changeDelay);
}

function send(state) {
    state.timer = 0;
    state.dotnet.invokeMethodAsync('OnCodeChanged', state.editor.getValue());
}

function flush(state) {
    if (state.timer) {
        window.clearTimeout(state.timer);
        send(state);
    }
}

function scheduleCursor(state) {
    window.clearTimeout(state.cursorTimer);
    state.cursorTimer = window.setTimeout(() => {
        const position = state.editor.getPosition();
        if (position) {
            state.dotnet.invokeMethodAsync('OnCursorChanged', position.lineNumber, position.column);
        }
    }, changeDelay);
}

function monacoLocale(culture) {
    const name = String(culture ?? '').toLowerCase();
    if (monacoLocales.has(name)) {
        return name;
    }

    const language = name.split('-')[0];
    if (language === 'zh') {
        return name.includes('tw') || name.includes('hant') ? 'zh-tw' : 'zh-cn';
    }

    return monacoLocales.has(language) ? language : null;
}

// The theme follows the closest OmniThemeScope: its data-omni-theme picks the light or dark base,
// and the surface colour of the editor's own frame becomes Monaco's background, so a preset repaints
// the editor with the rest of the scope. Monaco has one theme per page: with several editors under
// scopes of different appearances, the last one painted wins.
function themeName(state) {
    return isDark(state) ? 'omni-dark' : 'omni-light';
}

function isDark(state) {
    const scope = state.host.closest('[data-omni-theme]');
    const appearance = scope?.getAttribute('data-omni-theme');
    if (appearance === 'dark') {
        return true;
    }

    return appearance === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function applyTheme(state) {
    const monaco = window.monaco;
    const frame = state.host.closest('.omni-code-editor') ?? state.host;
    const background = hexColor(getComputedStyle(frame).backgroundColor);
    const colors = background ? { 'editor.background': background, 'editorGutter.background': background } : {};
    monaco.editor.defineTheme('omni-light', { base: 'vs', inherit: true, rules: [], colors: isDark(state) ? {} : colors });
    monaco.editor.defineTheme('omni-dark', { base: 'vs-dark', inherit: true, rules: [], colors: isDark(state) ? colors : {} });
    monaco.editor.setTheme(themeName(state));
}

function watchTheme(state) {
    state.repaint = () => applyTheme(state);
    const scope = state.host.closest('[data-omni-theme]');
    if (scope) {
        state.observer = new MutationObserver(state.repaint);
        state.observer.observe(scope, { attributes: true, attributeFilter: ['data-omni-theme', 'style', 'class'] });
    }

    state.media = window.matchMedia('(prefers-color-scheme: dark)');
    state.media.addEventListener('change', state.repaint);
}

function hexColor(value) {
    const match = /^rgba?\((\d+)[,\s]+(\d+)[,\s]+(\d+)(?:[,\s/]+([\d.]+%?))?\)$/.exec(String(value).trim());
    if (!match) {
        return null;
    }

    const alpha = match[4] === undefined ? 1 : match[4].endsWith('%') ? parseFloat(match[4]) / 100 : parseFloat(match[4]);
    if (alpha < 1) {
        return null;
    }

    return `#${[match[1], match[2], match[3]].map(part => Number(part).toString(16).padStart(2, '0')).join('')}`;
}

// Links: each pattern is a regular expression applied line by line; its first group, or the whole
// match when it has none, becomes a Ctrl+click link that is reported to .NET rather than opened.
function setLinks(state, links) {
    const monaco = window.monaco;
    state.links = (links ?? []).map(link => {
        try {
            return { name: link.name, pattern: new RegExp(link.pattern, 'g'), tooltip: link.tooltip ?? '' };
        }
        catch {
            return null;
        }
    }).filter(Boolean);

    const language = state.editor.getModel().getLanguageId();
    if (state.links.length > 0 && !languagesWithLinks.has(language)) {
        languagesWithLinks.add(language);
        monaco.languages.registerLinkProvider(language, { provideLinks: model => ({ links: linksOf(model) }) });
    }

    if (state.links.length > 0 && !openerRegistered && monaco.editor.registerLinkOpener) {
        openerRegistered = true;
        monaco.editor.registerLinkOpener({ open: resource => openLink(resource) });
    }
}

function linksOf(model) {
    const monaco = window.monaco;
    const state = statesByModel.get(model);
    if (!state || state.links.length === 0) {
        return [];
    }

    forgetTokens(state);
    const found = [];
    for (let line = 1; line <= model.getLineCount(); line++) {
        const text = model.getLineContent(line);
        for (const link of state.links) {
            link.pattern.lastIndex = 0;
            let match = link.pattern.exec(text);
            while (match) {
                const target = match[1] ?? match[0];
                const start = match.index + (match[1] === undefined ? 0 : match[0].indexOf(match[1]));
                if (target.length > 0) {
                    const token = String(++linkCounter);
                    linkTargets.set(token, { state, name: link.name, target, line });
                    state.tokens.push(token);
                    found.push({
                        range: new monaco.Range(line, start + 1, line, start + 1 + target.length),
                        url: `omni-code-link:${token}`,
                        tooltip: link.tooltip
                    });
                }

                if (match[0].length === 0) {
                    link.pattern.lastIndex++;
                }

                match = link.pattern.exec(text);
            }
        }
    }

    return found;
}

function openLink(resource) {
    if (resource?.scheme !== 'omni-code-link') {
        return false;
    }

    const entry = linkTargets.get(resource.path);
    if (!entry || !editors.has(entry.state.host)) {
        return false;
    }

    entry.state.dotnet.invokeMethodAsync('OnLinkActivated', entry.name, entry.target, entry.line);
    return true;
}

function forgetTokens(state) {
    for (const token of state.tokens) {
        linkTargets.delete(token);
    }

    state.tokens = [];
}
