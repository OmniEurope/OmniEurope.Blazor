// PLAN-014, lot JavaScript: drives, in the published showcase and in a real Chromium through CDP, the
// script paths the other probes leave unexecuted, one page at a time, each step acting as a reader
// would and asserting what the page shows after it. With eng/Test-ShowcaseHost.ps1 -JsCoverage, its
// V8 coverage joins the other probes' in eng/Test-JsCoverage.mjs. It fails on any console error or
// Content Security Policy violation, the showcase being served with `style-src 'self'`.
//
// Usage: node Test-ShowcaseScriptsProbe.mjs --endpoint http://127.0.0.1:<cdp port> --url http://127.0.0.1:<site port>/
// The browser (started with --remote-debugging-port) and the static server are the caller's.
// OMNI_SCRIPTS_ONLY=boards,editor runs only those parts, to work on one of them; the probe then says
// so, and a coverage pass run that way executes less, which the gate reports.

import { openShowcaseSession, probeOptions } from './ShowcaseCdp.mjs';
import { boardSteps } from './scripts-probe/boards.mjs';
import { editorSteps } from './scripts-probe/editor.mjs';
import { keySteps } from './scripts-probe/keys.mjs';
import { leakSteps } from './scripts-probe/leaks.mjs';
import { overlaySteps } from './scripts-probe/overlays.mjs';
import { requestSteps } from './scripts-probe/requests.mjs';

const parts = { overlays: overlaySteps, editor: editorSteps, boards: boardSteps, keys: keySteps, requests: requestSteps, leaks: leakSteps };
const only = process.env.OMNI_SCRIPTS_ONLY?.split(',').map(name => name.trim()).filter(Boolean);
// A name that is no part, or a choice that keeps none, would validate a run that drove nothing.
const unknown = only?.filter(name => !Object.hasOwn(parts, name)) ?? [];
if (unknown.length > 0 || (only && only.length === 0)) {
  console.error(`OMNI_SCRIPTS_ONLY refusé : ${unknown.length > 0 ? `partie inconnue ${unknown.join(', ')}` : 'aucune partie choisie'} ; parties : ${Object.keys(parts).join(', ')}.`);
  process.exit(1);
}
const chosen = only ? Object.entries(parts).filter(([name]) => only.includes(name)) : Object.entries(parts);

const { endpoint, siteUrl } = probeOptions('Test-ShowcaseScriptsProbe.mjs');
const session = await openShowcaseSession(endpoint, 'Scripts');
const results = [];

await session.start(siteUrl);
for (const [, run] of chosen) {
  await run(session, results);
}
await session.finish();

const partial = only ? ` (parties ${chosen.map(([name]) => name).join(', ')} seulement)` : '';
console.log(`Sonde des scripts validée${partial} : ${results.join(', ')} ; aucune violation CSP, console sans erreur.`);
