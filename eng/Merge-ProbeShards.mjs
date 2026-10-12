// Merges the shards of a showcase probe run in parallel by eng/Test-ShowcaseHost.ps1 (eng/ProbeShards.mjs)
// into the result the probe run whole gives: for Contrast, the one registry
// artifacts/theme-contrast-registry.json and the validating line or the grouped failures; for Languages,
// the line or the failures over every language. The merge first proves the shards add up to the whole
// pass: every shard present, read from the same catalogue, every theme (or language x width pair) checked exactly
// once, the shipped look exactly once. A missing or inconsistent shard is a failure, never a smaller pass.
//
// Usage: node eng/Merge-ProbeShards.mjs --probe Contrast|Languages --directory <shard folder> --count <n>
// The shard files are <directory>/<probe>-<i>-of-<n>.json, as the host names them.
import { existsSync, readFileSync } from 'node:fs';
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { reportContrast, reportLanguages } from './ProbeShards.mjs';

const options = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  options.set(process.argv[index], process.argv[index + 1]);
}
const probe = options.get('--probe');
const directory = options.get('--directory');
const count = Number(options.get('--count'));
if (!['Contrast', 'Languages'].includes(probe) || !directory || !Number.isInteger(count) || count < 1) {
  throw new Error('Usage: node eng/Merge-ProbeShards.mjs --probe Contrast|Languages --directory <dossier> --count <n>');
}

const shards = [];
const missing = [];
for (let index = 1; index <= count; index++) {
  const file = join(directory, `${probe}-${index}-of-${count}.json`);
  if (existsSync(file)) shards.push(JSON.parse(readFileSync(file, 'utf8')));
  else missing.push(`${index}/${count}`);
}

// Every item of the whole list exactly once across the shards, all read from the same list.
const coverageProblems = (name, all, parts) => {
  const problems = [];
  const lists = new Set(shards.map(shard => JSON.stringify(shard[all])));
  if (lists.size > 1) problems.push(`les fragments n'ont pas lu la même liste de ${name} : ${[...lists].join(' / ')}`);
  const whole = shards[0]?.[all] ?? [];
  const seen = shards.flatMap(shard => shard[parts]);
  const twice = seen.filter((item, position) => seen.indexOf(item) !== position);
  const absent = missing.length === 0 ? whole.filter(item => !seen.includes(item)) : [];
  const foreign = seen.filter(item => !whole.includes(item));
  if (twice.length > 0) problems.push(`${name} vérifiés deux fois : ${[...new Set(twice)].join(', ')}`);
  if (absent.length > 0) problems.push(`${name} vérifiés par aucun fragment : ${absent.join(', ')}`);
  if (foreign.length > 0) problems.push(`${name} hors de la liste : ${foreign.join(', ')}`);
  return problems;
};

if (probe === 'Contrast') {
  const artifacts = resolve(dirname(fileURLToPath(import.meta.url)), '..', 'artifacts');
  const registryPath = join(artifacts, 'theme-contrast-registry.json');
  const shotDirectory = join(artifacts, 'theme-probe');
  const failures = [];
  for (const label of missing) failures.push({ check: 'fragment', detail: `fragment ${label} absent : la sonde s'est arrêtée avant d'écrire ses résultats` });
  for (const problem of coverageProblems('thèmes', 'allThemes', 'themes')) failures.push({ check: 'fragment', detail: problem });
  const shipped = shards.filter(shard => shard.shippedLook).length;
  if (missing.length === 0 && shipped !== 1) failures.push({ check: 'fragment', detail: `apparence livrée mesurée par ${shipped} fragment(s) au lieu d'un` });
  if (shards.some(shard => shard.partial)) failures.push({ check: 'fragment', detail: 'un fragment est un passage filtré : la fusion ne vaut pas preuve pour la porte' });

  // The order of the whole pass: the shipped look first, then the themes in catalogue order, then the
  // checks of no theme (CSP, console, coverage). The sort is stable, so each theme keeps its own order.
  const order = shards[0]?.allThemes ?? [];
  const rank = entry => entry.theme === undefined ? Number.MAX_SAFE_INTEGER : order.includes(entry.theme) ? order.indexOf(entry.theme) : -1;
  const merged = name => shards.flatMap(shard => shard[name]).sort((left, right) => rank(left) - rank(right));
  const sum = name => shards.reduce((total, shard) => total + shard[name], 0);
  const registry = {
    generatedAt: new Date().toISOString(),
    partial: shards.some(shard => shard.partial),
    shards: count,
    combinations: sum('combinations'),
    expected: sum('expected'),
    measures: sum('measures'),
    failures: [...merged('failures'), ...failures],
    acceptedBusyVeil: merged('acceptedBusyVeil'),
    contrastWaivers: Object.assign({}, ...shards.map(shard => shard.contrastWaivers)),
    acceptedContrastWaiver: merged('acceptedContrastWaiver'),
    busyVeil: merged('busyVeil'),
    cards: merged('cards'),
    screenshots: shards.flatMap(shard => shard.screenshots)
  };
  await mkdir(artifacts, { recursive: true });
  await writeFile(registryPath, JSON.stringify(registry, null, 2));
  process.exitCode = reportContrast(registry, { registryPath, shotDirectory, scope: `Fusion de ${count} fragment(s)` });
} else {
  const failures = shards.flatMap(shard => shard.failures);
  const problems = [...missing.map(label => `fragment ${label} absent : la sonde s'est arrêtée avant d'écrire ses résultats`), ...coverageProblems('couples langue x largeur', 'allPairs', 'pairs')];
  const pathLists = new Set(shards.map(shard => JSON.stringify(shard.paths)));
  if (pathLists.size > 1) problems.push('les fragments n\'ont pas parcouru les mêmes pages');
  const order = shards[0]?.allLanguages ?? [];
  const languages = order.filter(language => shards.some(shard => shard.languages.includes(language)));
  process.exitCode = reportLanguages({
    measuredPages: shards.reduce((total, shard) => total + shard.measuredPages, 0),
    languages,
    widths: (shards[0]?.allWidths ?? []).filter(width => shards.some(shard => shard.widths.includes(width))),
    failures,
    csp: shards.flatMap(shard => shard.csp),
    consoleErrors: shards.flatMap(shard => shard.consoleErrors)
  }, { scope: `Fusion de ${count} fragment(s)`, problems });
}
