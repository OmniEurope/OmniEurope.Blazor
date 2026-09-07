// Self-test run from inside the WebView2. The CI runner never exposes the remote debugging port,
// so nothing outside the host can drive it; the host proves its own rendering, interaction and
// clean console instead, and the .NET side reports the result over standard output.
//
// This module is loaded by index.html before Blazor starts so the error hooks are in place for
// the whole page life, and imported again from .NET later: ES modules are singletons per URL,
// so both see the same error list.
const errors = [];

function describe(value) {
  if (value instanceof Error) return value.message;
  return String(value);
}

const originalConsoleError = console.error;
console.error = (...args) => {
  errors.push(args.map(describe).join(' '));
  originalConsoleError.apply(console, args);
};
window.addEventListener('error', event => errors.push(describe(event.error ?? event.message)));
window.addEventListener('unhandledrejection', event => errors.push(describe(event.reason)));

function wait(milliseconds) {
  return new Promise(resolve => setTimeout(resolve, milliseconds));
}

export async function runSelfTest(actionSelector, outputSelector, expected, timeoutMilliseconds) {
  const action = document.querySelector(actionSelector);
  if (!action) throw new Error(`Missing action element: ${actionSelector}`);
  // A real DOM click, so the Blazor event pipeline is exercised the same way a user would.
  action.click();

  const deadline = Date.now() + timeoutMilliseconds;
  let observed = '';
  while (Date.now() < deadline) {
    observed = document.querySelector(outputSelector)?.textContent?.trim() ?? '';
    if (observed === expected) break;
    await wait(50);
  }

  return {
    language: document.documentElement.lang,
    title: document.title,
    observed,
    errors: [...errors]
  };
}
