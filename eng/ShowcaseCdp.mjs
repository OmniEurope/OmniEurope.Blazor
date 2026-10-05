// The CDP session a showcase probe drives: connection to the page target, commands, evaluation, waits,
// real mouse and keyboard input, and the closing checks every probe makes (no console error, no
// Content Security Policy violation). The V8 coverage of eng/JsCoverage.mjs is wired in, inactive
// unless OMNI_JS_COVERAGE_DIR is set.
import { createJsCoverage } from './JsCoverage.mjs';

export function probeOptions(probeFile) {
  const options = new Map();
  for (let index = 2; index < process.argv.length; index += 2) {
    options.set(process.argv[index], process.argv[index + 1]);
  }

  const endpoint = options.get('--endpoint');
  const siteUrl = options.get('--url');
  if (!endpoint || !siteUrl) {
    throw new Error(`Usage: node ${probeFile} --endpoint <cdp url> --url <showcase root url>`);
  }

  return { endpoint, siteUrl };
}

export async function openShowcaseSession(endpoint, probeName) {
  const deadline = Date.now() + 20_000;
  let target;
  while (Date.now() < deadline) {
    try {
      const targets = await fetch(`${endpoint}/json/list`).then(response => response.json());
      target = targets.find(candidate => candidate.type === 'page' && candidate.webSocketDebuggerUrl);
      if (target) break;
    } catch {
      // The browser may still be starting.
    }
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  if (!target) throw new Error(`Aucune cible CDP disponible sur ${endpoint}.`);

  const socket = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener('open', resolve, { once: true });
    socket.addEventListener('error', () => reject(new Error('Connexion CDP impossible.')), { once: true });
  });

  let commandId = 0;
  const pending = new Map();
  const consoleErrors = [];
  const listeners = new Set();
  socket.addEventListener('message', event => {
    const message = JSON.parse(event.data);
    for (const listener of listeners) {
      if (listener.method === message.method) listener.handler(message.params);
    }
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) reject(new Error(message.error.message));
      else resolve(message.result);
      return;
    }

    if (message.method === 'Runtime.exceptionThrown') {
      consoleErrors.push(message.params.exceptionDetails.exception?.description ?? message.params.exceptionDetails.text);
    } else if (message.method === 'Log.entryAdded' && message.params.entry.level === 'error') {
      const source = message.params.entry.url ? ` (${message.params.entry.url})` : '';
      consoleErrors.push(`${message.params.entry.text}${source}`);
    } else if (message.method === 'Runtime.consoleAPICalled' && message.params.type === 'error') {
      consoleErrors.push(message.params.args.map(argument => argument.value ?? argument.description ?? '').join(' '));
    }
  });

  const sendCdp = (method, params = {}) => {
    const id = ++commandId;
    socket.send(JSON.stringify({ id, method, params }));
    return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
  };
  const coverage = createJsCoverage(sendCdp, probeName);
  const send = coverage.send;

  const evaluate = async expression => {
    const response = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
    if (response.exceptionDetails) throw new Error(response.exceptionDetails.exception?.description ?? response.exceptionDetails.text);
    return response.result.value;
  };

  // Listens to a CDP event; returns the function that stops listening.
  const on = (method, handler) => {
    const listener = { method, handler };
    listeners.add(listener);
    return () => listeners.delete(listener);
  };

  const pause = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

  const waitFor = async (description, expression, timeout = 15_000) => {
    const until = Date.now() + timeout;
    while (Date.now() < until) {
      try {
        if (await evaluate(expression)) return;
      } catch (error) {
        if (!String(error.message).includes('context was destroyed')) throw error;
      }
      await pause(100);
    }
    // Where the page stands when the wait runs out: without it a timeout says nothing of its cause.
    const state = await evaluate("JSON.stringify({ path: location.pathname, blazor: typeof Blazor, title: document.title, text: (document.querySelector('main') ?? document.body).innerText.slice(0, 160) })").catch(error => String(error.message));
    throw new Error(`Attente dépassée : ${description}. Page : ${state}. Console : ${consoleErrors.join(' | ') || 'aucune erreur'}.`);
  };

  const check = (condition, message) => {
    if (!condition) throw new Error(message);
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
    await send('Runtime.enable');
    await send('Log.enable');
    await send('Page.enable');
    await coverage.start();
    await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false });
    await send('Page.addScriptToEvaluateOnNewDocument', {
      source: "window.__omniCsp = []; document.addEventListener('securitypolicyviolation', event => window.__omniCsp.push(`${event.violatedDirective} ${event.blockedURI}`));"
    });
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

  // The closing checks: the page recorded no CSP violation and the console no error.
  const finish = async () => {
    await pause(300);
    const csp = await evaluate('window.__omniCsp');
    await coverage.finish();
    socket.close();
    check(csp.length === 0, `Violations CSP : ${csp.join(' | ')}`);
    check(consoleErrors.length === 0, `Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
  };

  return { send, on, evaluate, pause, waitFor, check, mouse, click, key, type, centerOf, pointOf, clickOn, hover, html5Drag, listenersOf, start, visit, finish, consoleErrors };
}
