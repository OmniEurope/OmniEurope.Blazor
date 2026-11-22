import { connectCdp, createEvaluate, readOptions } from './ShowcaseCdp.mjs';

const options = readOptions();

const endpoint = options.get('--endpoint');
const selector = options.get('--selector');
const outputSelector = options.get('--output') ?? selector;
const expected = options.get('--expected');
const assertSelector = options.get('--assert-selector');
const assertAttribute = options.get('--assert-attribute');
const assertExpected = options.get('--assert-expected');
const assertLanguage = options.get('--assert-language');
const assertTitle = options.get('--assert-title');
// Optional: selectors that must match at least one element once the interaction has run, separated
// by "||" (a CSS selector list cannot express "each of these must exist").
const assertPresent = (options.get('--assert-present') ?? '').split('||').map(value => value.trim()).filter(Boolean);
if (!endpoint || !selector || !expected) {
  throw new Error('Usage: node Test-CdpProbe.mjs --endpoint <url> --selector <css> --output <css> --expected <text>');
}

// The same deadline bounds the wait for a target and, below, for the hydrated probe element.
const deadline = Date.now() + 20_000;
const { socket, sendCdp: send, consoleErrors } = await connectCdp(endpoint, { timeout: deadline - Date.now(), describeExceptions: false });

const evaluate = createEvaluate(send, false);

await send('Runtime.enable');
await send('Log.enable');

let ready = false;
while (Date.now() < deadline) {
  try {
    ready = await evaluate(`Boolean(document.querySelector(${JSON.stringify(selector)}) && document.querySelector(${JSON.stringify(outputSelector)}))`);
    if (ready) break;
  } catch (error) {
    if (!String(error.message).includes('context was destroyed')) throw error;
  }
  await new Promise(resolve => setTimeout(resolve, 100));
}
if (!ready) throw new Error(`La sonde ${selector} n'est pas apparue après hydratation.`);

let observed = '';
const resultDeadline = Date.now() + 10_000;
while (Date.now() < resultDeadline) {
  await evaluate(`document.querySelector(${JSON.stringify(selector)}).click()`);
  await new Promise(resolve => setTimeout(resolve, 100));
  observed = await evaluate(`document.querySelector(${JSON.stringify(outputSelector)})?.textContent?.trim() ?? ''`);
  if (String(observed).includes(expected)) break;
}
await new Promise(resolve => setTimeout(resolve, 250));

let asserted;
if (assertSelector || assertAttribute || assertExpected) {
  if (!assertSelector || !assertAttribute || assertExpected == null) {
    throw new Error('The optional assertion requires --assert-selector, --assert-attribute and --assert-expected.');
  }
  asserted = await evaluate(`document.querySelector(${JSON.stringify(assertSelector)})?.getAttribute(${JSON.stringify(assertAttribute)}) ?? ''`);
}
const missing = [];
for (const presentSelector of assertPresent) {
  let found = false;
  const presentDeadline = Date.now() + 5_000;
  while (Date.now() < presentDeadline) {
    found = await evaluate(`Boolean(document.querySelector(${JSON.stringify(presentSelector)}))`);
    if (found) break;
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  if (!found) missing.push(presentSelector);
}
const observedLanguage = assertLanguage == null ? null : await evaluate('document.documentElement.lang');
const observedTitle = assertTitle == null ? null : await evaluate('document.title');

socket.close();
if (!String(observed).includes(expected)) {
  throw new Error(`Résultat interactif inattendu : ${JSON.stringify(observed)} (attendu : ${JSON.stringify(expected)}).`);
}
if (consoleErrors.length > 0) {
  throw new Error(`Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
}
if (missing.length > 0) {
  throw new Error(`Éléments attendus absents après l'interaction : ${missing.join(' | ')}`);
}
if (assertSelector && String(asserted) !== assertExpected) {
  throw new Error(`Attribut interactif inattendu : ${assertSelector}[${assertAttribute}]=${JSON.stringify(asserted)} (attendu : ${JSON.stringify(assertExpected)}).`);
}
if (assertLanguage != null && String(observedLanguage) !== assertLanguage) {
  throw new Error(`Langue du document inattendue : ${JSON.stringify(observedLanguage)} (attendu : ${JSON.stringify(assertLanguage)}).`);
}
if (assertTitle != null && String(observedTitle) !== assertTitle) {
  throw new Error(`Titre du document inattendu : ${JSON.stringify(observedTitle)} (attendu : ${JSON.stringify(assertTitle)}).`);
}

const assertionSummary = [
  assertSelector ? `${assertSelector}[${assertAttribute}]=${JSON.stringify(asserted)}` : '',
  assertLanguage != null ? `lang=${JSON.stringify(observedLanguage)}` : '',
  assertTitle != null ? `title=${JSON.stringify(observedTitle)}` : '',
  assertPresent.length > 0 ? `présents=${assertPresent.length}` : ''
].filter(Boolean).map(value => `, ${value}`).join('');
console.log(`Sonde CDP validée : ${selector}, résultat ${JSON.stringify(observed)}${assertionSummary}, console sans erreur.`);
