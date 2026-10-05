// PLAN-014, lot JavaScript: the zero blind spot gate of the package's scripts. Merges the V8 precise
// coverage the showcase probes wrote (eng/JsCoverage.mjs, eng/Test-ShowcaseHost.ps1 -JsCoverage) and checks
// every function of src/OmniEurope.Blazor/wwwroot/**/*.js: a function never called, or a module never
// loaded, is a gap. A file absent from eng/js-coverage-baseline.json must have none; a listed file may only
// shrink its count; eng/js-coverage-exceptions.json admits the definitive ones with their reason, as
// eng/coverage-exceptions.json does for the C#. A file may have both: the exception admits its count,
// the baseline holds the rest.
//
// A function V8 never compiled because its enclosing function never ran is not reported on its own: the
// enclosing function is, which is the gap to close first.
//
// Usage: node eng/Test-JsCoverage.mjs --coverage <folder of probe JSON> [--update-baseline]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const engDirectory = path.dirname(fileURLToPath(import.meta.url));
const wwwroot = path.resolve(engDirectory, '..', 'src', 'OmniEurope.Blazor', 'wwwroot');
const baselinePath = path.join(engDirectory, 'js-coverage-baseline.json');
const exceptionsPath = path.join(engDirectory, 'js-coverage-exceptions.json');
const PackagePath = '/_content/OmniEurope.Blazor/';

const args = process.argv.slice(2);
const coverageDirectory = args[args.indexOf('--coverage') + 1];
const update = args.includes('--update-baseline');
if (!args.includes('--coverage') || !coverageDirectory || !fs.existsSync(coverageDirectory)) {
  console.error('Usage: node eng/Test-JsCoverage.mjs --coverage <dossier des relevés> [--update-baseline]');
  process.exit(2);
}

const files = [];
(function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(full);
    else if (entry.name.endsWith('.js')) files.push(path.relative(wwwroot, full).replaceAll('\\', '/'));
  }
})(wwwroot);
files.sort();

// A served path, fingerprinted or not, back to its source file.
function sourceOf(url) {
  const served = decodeURIComponent(new URL(url).pathname.split(PackagePath)[1] ?? '');
  if (files.includes(served)) return served;
  const unprinted = served.replace(/\.[a-z0-9]{8,}(\.js)$/i, '$1');
  return files.includes(unprinted) ? unprinted : null;
}

const lineStarts = new Map();
function lineOf(file, offset) {
  if (!lineStarts.has(file)) {
    const text = fs.readFileSync(path.join(wwwroot, file), 'utf8');
    const starts = [0];
    for (let index = 0; index < text.length; index++) if (text[index] === '\n') starts.push(index + 1);
    lineStarts.set(file, { starts, length: text.length });
  }
  const { starts } = lineStarts.get(file);
  let low = 0;
  let high = starts.length - 1;
  while (low < high) {
    const middle = (low + high + 1) >> 1;
    if (starts[middle] <= offset) low = middle; else high = middle - 1;
  }
  return low + 1;
}

// Every function V8 reported, by file and start offset: called once in any take of any probe is enough.
const functions = new Map();
const loaded = new Set();
const unmatched = new Set();
let takes = 0;
for (const name of fs.readdirSync(coverageDirectory).filter(entry => entry.endsWith('.json'))) {
  const report = JSON.parse(fs.readFileSync(path.join(coverageDirectory, name), 'utf8'));
  takes++;
  for (const script of report.scripts) {
    const file = sourceOf(script.url);
    if (!file) {
      unmatched.add(script.url);
      continue;
    }
    loaded.add(file);
    const perFile = functions.get(file) ?? new Map();
    for (const fn of script.functions) {
      const range = fn.ranges[0];
      const key = range.startOffset;
      const previous = perFile.get(key);
      perFile.set(key, {
        name: fn.functionName || (range.startOffset === 0 ? '(module)' : '(anonyme)'),
        start: range.startOffset,
        end: range.endOffset,
        called: (previous?.called ?? false) || range.count > 0
      });
    }
    functions.set(file, perFile);
  }
}

if (takes === 0) {
  console.error(`Aucun relevé de couverture dans ${coverageDirectory} : la porte ne prouve rien.`);
  process.exit(1);
}

// The gaps of each file: the module when no probe loaded it, else its functions never called.
const gaps = new Map();
for (const file of files) {
  if (!loaded.has(file)) {
    gaps.set(file, ['(module jamais chargé)']);
    continue;
  }
  const { length } = (lineOf(file, 0), lineStarts.get(file));
  const list = [...functions.get(file).values()]
    .filter(fn => !fn.called)
    .sort((a, b) => a.start - b.start)
    .map(fn => {
      if (fn.end > length) throw new Error(`${file} : le script servi diffère de la source (décalage ${fn.end} > ${length}).`);
      return `${fn.name} (ligne ${lineOf(file, fn.start)})`;
    });
  if (list.length > 0) gaps.set(file, list);
}

const read = (file, fallback) => fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : fallback;
const exceptions = new Map(read(exceptionsPath, { exceptions: [] }).exceptions.map(entry => [entry.file, entry]));
const baselineDocument = read(baselinePath, null);
const baseline = new Map((baselineDocument?.files ?? []).map(entry => [entry.file, entry.functions]));

// A file may hold definitive gaps (its exception) and gaps still to close (its baseline entry): the
// exception admits its count first, the baseline holds what remains.
const residuals = new Map();
const failures = [];
let total = 0;
for (const [file, list] of gaps) {
  total += list.length;
  const admitted = exceptions.get(file)?.functions ?? 0;
  const residual = list.length - admitted;
  if (residual <= 0) continue;
  residuals.set(file, residual);
  const allowed = baseline.get(file);
  const what = admitted > 0 ? `${residual} fonction(s) jamais exécutée(s) hors des ${admitted} admises` : `${residual} fonction(s) jamais exécutée(s)`;
  if (allowed === undefined && baselineDocument !== null) failures.push(`${file} : ${what}, fichier hors de la liste.`);
  else if (allowed !== undefined && residual > allowed) failures.push(`${file} : ${what}, plus que les ${allowed} de la liste.`);
  else if (!update && allowed !== undefined && residual < allowed) failures.push(`${file} : ${what} au lieu de ${allowed} : réduire la liste (--update-baseline).`);
}
for (const [file] of baseline) {
  if (!residuals.has(file) && !update) failures.push(`${file} : plus d'écart hors exception, à retirer de la liste (--update-baseline).`);
}
for (const [file, admitted] of exceptions) {
  const found = gaps.get(file)?.length ?? 0;
  if (found === 0) failures.push(`${file} : exception admise sans écart restant, à retirer.`);
  else if (found < admitted.functions) failures.push(`${file} : ${found} écart(s) pour ${admitted.functions} admis, ajuster l'exception.`);
  if (!admitted.reason) failures.push(`${file} : exception sans raison.`);
}

for (const [file, list] of gaps) console.log(`== ${file}${exceptions.has(file) ? ' (admis)' : ''}\n   ${list.join('\n   ')}`);
for (const url of unmatched) console.log(`?? script du paquet sans source : ${url}`);

if (update) {
  const grown = failures.filter(failure => failure.includes('plus que') || failure.includes('hors de la liste'));
  if (grown.length > 0 && baselineDocument !== null) {
    console.error(`La liste ne peut pas absorber un écart qui grandit :\n  ${grown.join('\n  ')}`);
    process.exit(1);
  }
  const entries = [...residuals].map(([file, functions]) => ({ file, functions }));
  fs.writeFileSync(baselinePath, JSON.stringify({
    $comment: "Scripts du paquet pas encore entièrement exécutés par les sondes de la vitrine (PLAN-014) : nombre de fonctions jamais appelées. La liste ne peut que rétrécir ; un fichier absent doit être entièrement couvert. Réécrite par eng/Test-JsCoverage.mjs --update-baseline.",
    files: entries
  }, null, 2) + '\n');
  console.log(`Liste réécrite : ${entries.length} fichier(s) à écarts.`);
  process.exit(0);
}

if (failures.length > 0) {
  console.error(`Couverture JavaScript refusée (${failures.length}) :\n  ${failures.join('\n  ')}`);
  process.exit(1);
}

console.log(`Couverture JavaScript validée : ${takes} relevé(s), ${files.length} scripts, ${total} fonction(s) non exécutée(s) toutes listées ou admises.`);
