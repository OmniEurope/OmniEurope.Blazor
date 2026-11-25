// The CDP session a showcase probe drives: connection to the page target, commands, evaluation, waits,
// real mouse and keyboard input, and the closing checks every probe makes (no console error, no
// Content Security Policy violation). The V8 coverage of eng/JsCoverage.mjs is wired in, inactive
// unless OMNI_JS_COVERAGE_DIR is set.
import { createJsCoverage } from './JsCoverage.mjs';

// The `--name value` pairs of the command line, as every probe and engineering tool reads them.
export function readOptions() {
  const options = new Map();
  for (let index = 2; index < process.argv.length; index += 2) {
    options.set(process.argv[index], process.argv[index + 1]);
  }
  return options;
}

export const pause = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

export const check = (condition, message) => {
  if (!condition) throw new Error(message);
};

const isPageTarget = candidate => candidate.type === 'page' && candidate.webSocketDebuggerUrl;

// The page targets the browser lists, polled until one is accepted (the browser or WebView2 host
// may still be starting); throws when none came within the timeout.
export async function findPageTargets(endpoint, { timeout = 20_000, accept = isPageTarget } = {}) {
  const deadline = Date.now() + timeout;
  let found = [];
  while (Date.now() < deadline) {
    try {
      const targets = await fetch(`${endpoint}/json/list`).then(response => response.json());
      found = targets.filter(accept);
      if (found.length > 0) break;
    } catch {
      // The browser may still be starting.
    }
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  if (found.length === 0) throw new Error(`Aucune cible CDP disponible sur ${endpoint}.`);
  return found;
}

// Opens the debugging WebSocket of a target and resolves once it is connected.
export async function openSocket(url) {
  const socket = new WebSocket(url);
  await new Promise((resolve, reject) => {
    socket.addEventListener('open', resolve, { once: true });
    socket.addEventListener('error', () => reject(new Error('Connexion CDP impossible.')), { once: true });
  });
  return socket;
}

// Hands every message of a debugging socket to `onMessage`, parsed, once its origin is checked: a
// message must come from the endpoint the socket was opened on (the loopback debugging port). A message
// of any other origin is not read; `onForeign` receives that origin, so the caller breaks its link.
export function listenToSocket(socket, onMessage, onForeign) {
  const expectedOrigin = new URL(socket.url).origin;
  socket.addEventListener('message', event => {
    if (event.origin !== expectedOrigin) {
      onForeign(event.origin);
      return;
    }
    onMessage(JSON.parse(event.data));
  });
}

// Settles the pending command a CDP response answers; true when the message was such a response.
export function settleResponse(message, pending) {
  if (!(message.id && pending.has(message.id))) return false;
  const { resolve, reject } = pending.get(message.id);
  pending.delete(message.id);
  if (message.error) reject(new Error(message.error.message));
  else resolve(message.result);
  return true;
}

// Records a page error a CDP event reports: an uncaught exception (its description when
// `describeExceptions`, its short text otherwise), an error log entry or a console.error call.
export function recordConsoleError(message, consoleErrors, describeExceptions = true) {
  if (message.method === 'Runtime.exceptionThrown') {
    const details = message.params.exceptionDetails;
    consoleErrors.push(describeExceptions ? details.exception?.description ?? details.text : details.text);
  } else if (message.method === 'Log.entryAdded' && message.params.entry.level === 'error') {
    const source = message.params.entry.url ? ` (${message.params.entry.url})` : '';
    consoleErrors.push(`${message.params.entry.text}${source}`);
  } else if (message.method === 'Runtime.consoleAPICalled' && message.params.type === 'error') {
    consoleErrors.push(message.params.args.map(argument => argument.value ?? argument.description ?? '').join(' '));
  }
}

// The longest a CDP command may stay unanswered before it fails (a whole-page screenshot or a long
// evaluation stays well under it); a probe never waits forever on a renderer that stopped answering.
export const defaultCommandTimeout = 120_000;

// The command channel over a connected socket: numbered commands answered by their response, and the
// page errors collected from the events. `onEvent` sees every message first. A command fails, instead
// of waiting forever, when it is not answered within `commandTimeout`, when the socket closes or
// errors, and when the target's renderer crashes (`Inspector.targetCrashed`) or is detached
// (`Inspector.detached`); once the link is broken every later command fails at once with the cause.
export function createCdpLink(socket, { describeExceptions = true, onEvent, commandTimeout = defaultCommandTimeout } = {}) {
  let commandId = 0;
  let broken = null;
  const pending = new Map();
  const consoleErrors = [];

  const breakLink = reason => {
    broken ??= new Error(reason);
    for (const [id, entry] of pending) {
      clearTimeout(entry.timer);
      entry.reject(new Error(`${reason} (commande CDP #${id} ${entry.method} sans réponse).`));
    }
    pending.clear();
  };

  listenToSocket(socket, message => {
    onEvent?.(message);
    if (message.method === 'Inspector.targetCrashed') {
      breakLink('Le moteur de rendu de la page a planté (Inspector.targetCrashed)');
      return;
    }
    if (message.method === 'Inspector.detached') {
      breakLink(`La liaison CDP a été détachée (Inspector.detached : ${message.params?.reason ?? 'raison inconnue'})`);
      return;
    }
    if (message.id && pending.has(message.id)) clearTimeout(pending.get(message.id).timer);
    if (settleResponse(message, pending)) return;
    recordConsoleError(message, consoleErrors, describeExceptions);
  }, origin => breakLink(`Message CDP d'une origine inattendue (${origin})`));
  socket.addEventListener('close', event => breakLink(`La liaison CDP s'est fermée (code ${event.code})`));
  socket.addEventListener('error', () => breakLink('La liaison CDP est en erreur'));

  const sendCdp = (method, params = {}) => {
    if (broken) return Promise.reject(new Error(`${broken.message} : ${method} non envoyée.`));
    const id = ++commandId;
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        if (!pending.delete(id)) return;
        // The evaluated expression says which step hung; without it the timeout names only a number.
        const what = params.expression ? ` (${String(params.expression).replace(/\s+/g, ' ').slice(0, 240)})` : '';
        reject(new Error(`Commande CDP #${id} ${method}${what} sans réponse après ${commandTimeout} ms.`));
      }, commandTimeout);
      pending.set(id, { resolve, reject, method, timer });
      socket.send(JSON.stringify({ id, method, params }));
    });
  };

  return { sendCdp, pending, consoleErrors };
}

// Finds the page target, connects to it and opens the command channel, with the Inspector domain on
// so a renderer crash or a detach fails the pending commands.
export async function connectCdp(endpoint, { timeout, describeExceptions, onEvent, commandTimeout } = {}) {
  const [target] = await findPageTargets(endpoint, { timeout });
  const socket = await openSocket(target.webSocketDebuggerUrl);
  const link = createCdpLink(socket, { describeExceptions, onEvent, commandTimeout });
  await link.sendCdp('Inspector.enable');
  return { target, socket, ...link };
}

// Evaluates an expression in the page and returns its value; a page exception becomes an error (its
// description when `describeExceptions`, its short text otherwise).
export function createEvaluate(send, describeExceptions = true) {
  return async expression => {
    const response = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
    if (response.exceptionDetails) throw new Error(describeExceptions ? response.exceptionDetails.exception?.description ?? response.exceptionDetails.text : response.exceptionDetails.text);
    return response.result.value;
  };
}

// Polls an expression until it is truthy; false when the timeout ran out. A page being replaced by a
// navigation is waited through.
export async function pollUntil(evaluate, expression, timeout = 15_000, interval = 100) {
  const until = Date.now() + timeout;
  while (Date.now() < until) {
    try {
      if (await evaluate(expression)) return true;
    } catch (error) {
      if (!String(error.message).includes('context was destroyed')) throw error;
    }
    await pause(interval);
  }
  return false;
}

// The script that records every Content Security Policy violation of the page in window.__omniCsp.
export const cspRecorderSource = "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));";

// The opening every showcase probe makes: runtime, log and page domains, coverage, the desktop
// viewport (none when `viewport` is null) and the CSP violation recorder.
export async function enableShowcasePage(send, coverage, viewport = { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false }) {
  await send('Runtime.enable');
  await send('Log.enable');
  await send('Page.enable');
  await coverage.start();
  if (viewport) await send('Emulation.setDeviceMetricsOverride', viewport);
  await send('Page.addScriptToEvaluateOnNewDocument', { source: cspRecorderSource });
}

export function probeOptions(probeFile) {
  const options = readOptions();

  const endpoint = options.get('--endpoint');
  const siteUrl = options.get('--url');
  if (!endpoint || !siteUrl) {
    throw new Error(`Usage: node ${probeFile} --endpoint <cdp url> --url <showcase root url>`);
  }

  return { endpoint, siteUrl };
}

export async function openShowcaseSession(endpoint, probeName) {
  const listeners = new Set();
  const { socket, sendCdp, consoleErrors } = await connectCdp(endpoint, {
    onEvent: message => {
      for (const listener of listeners) {
        if (listener.method === message.method) listener.handler(message.params);
      }
    }
  });
  const coverage = createJsCoverage(sendCdp, probeName);
  const send = coverage.send;

  const evaluate = createEvaluate(send);

  // Listens to a CDP event; returns the function that stops listening.
  const on = (method, handler) => {
    const listener = { method, handler };
    listeners.add(listener);
    return () => listeners.delete(listener);
  };

  const waitFor = async (description, expression, timeout = 15_000) => {
    if (await pollUntil(evaluate, expression, timeout)) return;
    // Where the page stands when the wait runs out: without it a timeout says nothing of its cause.
    const state = await evaluate("JSON.stringify({ path: location.pathname, blazor: typeof Blazor, title: document.title, text: (document.querySelector('main') ?? document.body).innerText.slice(0, 160) })").catch(error => String(error.message));
    throw new Error(`Attente dépassée : ${description}. Page : ${state}. Console : ${consoleErrors.join(' | ') || 'aucune erreur'}.`);
  };

  const mouse = (type, point, extra = {}) => send('Input.dispatchMouseEvent', { type, x: point.x, y: point.y, button: 'left', clickCount: 1, ...extra });
  const click = async (point, extra = {}) => {
    await mouse('mouseMoved', point, { button: 'none' });
    await mouse('mousePressed', point, extra);
    await mouse('mouseReleased', point, extra);
  };
  const key = async (name, code, virtualKey, modifiers = 0, text) => {
    await send('Input.dispatchKeyEvent', { type: text ? 'keyDown' : 'rawKeyDown', key: name, code, windowsVirtualKeyCode: virtualKey, modifiers, ...(text ? { text } : {}) });
    await send('Input.dispatchKeyEvent', { type: 'keyUp', key: name, code, windowsVirtualKeyCode: virtualKey, modifiers });
  };
  const type = text => send('Input.insertText', { text });
  const centerOf = selector => evaluate(`(() => { const element = document.querySelector(${JSON.stringify(selector)}); if (!element) throw new Error('Introuvable : ' + ${JSON.stringify(selector)}); const box = element.getBoundingClientRect(); return { x: box.left + box.width / 2, y: box.top + box.height / 2 }; })()`);

  // The centre of the first visible element a selector finds, scrolled into view and read once it
  // stops moving (a menu or a popover is placed by its script after it renders).
  const pointOf = async selector => {
    await waitFor(`l'élément ${selector}`, `[...document.querySelectorAll(${JSON.stringify(selector)})].some(element => element.getClientRects().length > 0)`);
    const read = () => evaluate(`(() => {
      const element = [...document.querySelectorAll(${JSON.stringify(selector)})].find(candidate => candidate.getClientRects().length > 0);
      let box = element.getBoundingClientRect();
      if (box.top < 0 || box.bottom > innerHeight || box.left < 0 || box.right > innerWidth) {
        element.scrollIntoView({ block: 'center', inline: 'center' });
        box = element.getBoundingClientRect();
      }
      return { x: Math.round(box.left + box.width / 2), y: Math.round(box.top + box.height / 2) };
    })()`);
    let previous = await read();
    for (let tries = 0; tries < 20; tries++) {
      await pause(100);
      const next = await read();
      if (next.x === previous.x && next.y === previous.y) break;
      previous = next;
    }
    return previous;
  };
  const clickOn = async (selector, extra = {}) => click(await pointOf(selector), extra);
  const hover = async selector => mouse('mouseMoved', await pointOf(selector), { button: 'none' });

  // The listeners of a page object (document, window) by event type, read by CDP: what a module left
  // behind once its component was removed shows here.
  const listenersOf = async expression => {
    const { result } = await send('Runtime.evaluate', { expression });
    const { listeners } = await send('DOMDebugger.getEventListeners', { objectId: result.objectId });
    await send('Runtime.releaseObject', { objectId: result.objectId });
    const counts = {};
    for (const listener of listeners) counts[listener.type] = (counts[listener.type] ?? 0) + 1;
    return counts;
  };

  // A real HTML5 drag: the browser starts it from a pressed and moved mouse (dragstart fires in the
  // page), CDP intercepts its data, and the drop is dispatched on the target. `during` runs while the
  // drag is held, before the drop.
  const html5Drag = async (fromSelector, toSelector, during = async () => {}) => {
    const from = await pointOf(fromSelector);
    const to = await pointOf(toSelector);
    let intercepted = null;
    const stop = on('Input.dragIntercepted', params => { intercepted = params.data; });
    await send('Input.setInterceptDrags', { enabled: true });
    try {
      await mouse('mouseMoved', from, { button: 'none' });
      await mouse('mousePressed', from);
      for (let index = 1; index <= 6 && !intercepted; index++) {
        await mouse('mouseMoved', { x: from.x + (to.x - from.x) * index / 12, y: from.y + (to.y - from.y) * index / 12 }, { buttons: 1 });
        await pause(30);
      }
      const until = Date.now() + 3_000;
      while (!intercepted && Date.now() < until) await pause(50);
      if (!intercepted) throw new Error(`Aucun glisser HTML5 démarré depuis ${fromSelector}.`);
      await during();
      for (const type of ['dragEnter', 'dragOver', 'drop']) {
        await send('Input.dispatchDragEvent', { type, x: to.x, y: to.y, data: intercepted });
      }
      await mouse('mouseReleased', to);
      return intercepted;
    }
    finally {
      stop();
      await send('Input.setInterceptDrags', { enabled: false });
    }
  };

  // Opens the showcase at its root, with the shipped look and a CSP violation recorder, and waits
  // for the rendered application: a navigation asked before the router listens is lost.
  const start = async siteUrl => {
    await enableShowcasePage(send, coverage);
    await send('Page.navigate', { url: siteUrl });
    await waitFor('la vitrine', "document.readyState === 'complete'");
    await evaluate("(() => { try { localStorage.removeItem('omnieurope.showcase.theme'); } catch { } })()");
    await send('Page.navigate', { url: siteUrl });
    await waitFor('le runtime Blazor', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null");
  };

  // Navigates inside the application and waits for the demonstration to render.
  const visit = async (path, readySelector) => {
    await evaluate(`Blazor.navigateTo(${JSON.stringify(path)})`);
    await waitFor(`la page ${path}`, `location.pathname === ${JSON.stringify(path)} && document.querySelector(${JSON.stringify(readySelector)}) !== null`);
    await pause(300);
  };

  // The Tab key, Shift+Tab when `shift`.
  const tab = (shift = false) => key('Tab', 'Tab', 9, shift ? 8 : 0);

  // The step recorder of a scripts probe: a step is recorded only while the console stayed clean.
  const stepRecorder = results => label => {
    if (consoleErrors.length > 0) throw new Error(`Console en erreur après « ${label} » : ${consoleErrors.join(' | ')}`);
    results.push(label);
  };

  // The page recorded no CSP violation so far (a reload starts a new record).
  const cspClean = async when => {
    const csp = await evaluate('window.__omniCsp');
    check(csp.length === 0, `Violations CSP ${when} : ${csp.join(' | ')}`);
  };

  // Reloads the application at a root address (the static host has no fallback for deep links) once the
  // CSP record of the page left is checked, and waits for the runtime.
  const reloadShowcase = async url => {
    await cspClean('avant le rechargement');
    await send('Page.navigate', { url });
    await waitFor('le runtime Blazor rechargé', "typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function' && document.getElementById('showcase-theme') !== null");
  };

  // Chooses a mode in the application menu of the header (its position among the modes), then closes the menu.
  const pickMode = async position => {
    await clickOn('.omni-header .omni-app-menu__trigger');
    await waitFor("le menu d'application", "document.querySelector('.omni-app-menu__mode') !== null");
    await clickOn(`.omni-app-menu__mode .omni-appearance-window__mode:nth-child(${position})`);
    await key('Escape', 'Escape', 27);
    await waitFor("le menu d'application refermé", "document.querySelector('.omni-app-menu__mode') === null");
  };

  // The closing checks: the page recorded no CSP violation and the console no error.
  const finish = async () => {
    await pause(300);
    const csp = await evaluate('window.__omniCsp');
    await coverage.finish();
    socket.close();
    check(csp.length === 0, `Violations CSP : ${csp.join(' | ')}`);
    check(consoleErrors.length === 0, `Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
  };

  return { send, on, evaluate, pause, waitFor, check, mouse, click, key, type, centerOf, pointOf, clickOn, hover, html5Drag, listenersOf, start, visit, finish, consoleErrors, tab, stepRecorder, cspClean, reloadShowcase, pickMode };
}
