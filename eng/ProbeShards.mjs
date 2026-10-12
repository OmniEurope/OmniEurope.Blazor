// The shards of a showcase probe run in parallel by eng/Test-ShowcaseHost.ps1, and the reports a probe
// prints at its end, shared by the probe run whole and by eng/Merge-ProbeShards.mjs once its shards are
// merged, so a sharded pass and a whole one print the same lines from the same counts.
//
// A shard is `--shard <i>/<n>` (1 <= i <= n) with `--shard-file <path>`: the probe takes every n-th item
// of its list, starting at the i-th, checks that slice at full strength and writes its raw results to
// the shard file for the merge. It is not a filter: a shard is never reported as a partial run, and the
// merge refuses a set of shards that does not cover the whole list exactly once.
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname } from 'node:path';

export function parseShard(value, file) {
  if (value === undefined || value === null) return null;
  const match = /^(\d+)\/(\d+)$/.exec(String(value));
  const index = match ? Number(match[1]) : 0;
  const count = match ? Number(match[2]) : 0;
  if (!match || count < 1 || index < 1 || index > count) throw new Error(`--shard attend i/n avec 1 <= i <= n, reçu : ${value}`);
  if (!file) throw new Error('--shard exige --shard-file <chemin>.');
  return { index, count, file, label: `${index}/${count}` };
}

// The items of a shard: round robin over the list, so the slices stay balanced whatever its order.
export function shardSlice(items, shard) {
  return shard ? items.filter((_, position) => position % shard.count === shard.index - 1) : items;
}

export async function writeShardResult(shard, data) {
  await mkdir(dirname(shard.file), { recursive: true });
  await writeFile(shard.file, JSON.stringify({ shard: { index: shard.index, count: shard.count }, ...data }));
}

// The end of the contrast probe: the failures grouped, or the line that validates the pass. Returns the
// exit code. `scope` names a shard ("Fragment 3/12 (thèmes Halo, Néon)") whose own report this is.
export function reportContrast(registry, { registryPath, shotDirectory, scope = null }) {
  const { failures, measures, combinations, acceptedBusyVeil, acceptedContrastWaiver, screenshots, partial } = registry;
  const prefix = scope ? `${scope} : ` : '';
  if (failures.length > 0) {
    const grouped = new Map();
    for (const failure of failures) {
      const key = [failure.page, failure.target, failure.state, failure.check].filter(Boolean).join(' | ');
      const group = grouped.get(key) ?? { count: 0, sample: failure };
      group.count++;
      grouped.set(key, group);
    }
    const lines = [...grouped].slice(0, 60).map(([key, group]) => {
      const sample = group.sample;
      const where = [sample.theme, sample.palette, sample.mode].filter(Boolean).join(' + ');
      const value = sample.ratio !== undefined ? `${sample.foreground} sur ${sample.background} = ${sample.ratio} < ${sample.required}` : sample.detail;
      return `  ${key} x${group.count} (ex. ${where} : ${value})`;
    });
    console.error(`${prefix}Contrastes : ${failures.length} échec(s) sur ${measures} mesures, ${combinations} combinaison(s). Registre : ${registryPath}\n${lines.join('\n')}${grouped.size > 60 ? `\n  ... ${grouped.size - 60} groupe(s) de plus dans le registre` : ''}`);
    return 1;
  }
  const waived = Object.entries(Object.groupBy(acceptedContrastWaiver, entry => entry.theme)).map(([theme, entries]) => `${theme} ${entries.length}`).join(', ');
  console.log(`${prefix}Sonde de contraste validée : ${measures} mesures sur ${combinations} combinaison(s) thème x palette x mode${partial ? ' (passage partiel)' : ''}, repos, survol forcé et focus, voile d'occupation (${acceptedBusyVeil.length} mesure(s) sous le ratio acceptée(s) par décision du 2026-09-18, voir acceptedBusyVeil), contraste non garanti (${acceptedContrastWaiver.length} mesure(s) sous le ratio acceptée(s) par décision du 2026-09-28${waived ? ` : ${waived}` : ''}, voir acceptedContrastWaiver), géométrie, 375 px ; registre vide (${registryPath}), ${screenshots.length} capture(s) dans ${shotDirectory}, aucune violation CSP, console sans erreur.`);
  return 0;
}

// The end of the languages probe. Returns the exit code.
// `problems` are the merge's own findings (a missing shard, a language checked twice): each fails the pass.
export function reportLanguages({ measuredPages, languages, widths, failures, csp, consoleErrors }, { scope = null, problems = [] } = {}) {
  const prefix = scope ? `${scope} : ` : '';
  let code = 0;
  for (const problem of problems) {
    console.error(`${prefix}Langues : ${problem}`);
    code = 1;
  }
  if (measuredPages === 0) {
    console.error(`${prefix}Langues : aucune page mesurée ; la sonde ne prouve rien.`);
    code = 1;
  }
  if (failures.length > 0) {
    const grouped = new Map();
    for (const failure of failures) {
      const key = `${failure.element} ${failure.clipped ? 'rogné' : 'déborde'}`;
      const group = grouped.get(key) ?? { count: 0, languages: new Set(), widths: new Set(), paths: new Set(), beyond: 0, sample: failure.text };
      group.count++;
      group.languages.add(failure.language);
      group.widths.add(failure.width);
      group.paths.add(failure.path);
      group.beyond = Math.max(group.beyond, failure.beyond);
      grouped.set(key, group);
    }
    const lines = [...grouped]
      .sort((left, right) => right[1].count - left[1].count)
      .map(([key, group]) => `  ${key} x${group.count}, jusqu'à ${group.beyond} px, ${[...group.languages].join('/')} à ${[...group.widths].join('/')} px sur ${[...group.paths].slice(0, 4).join(', ')}${group.paths.size > 4 ? ` (+${group.paths.size - 4})` : ''} : « ${group.sample} »`);
    console.error(`${prefix}Langues : ${failures.length} texte(s) sortent de leur boîte (${grouped.size} forme(s), ${measuredPages} pages mesurées en ${languages.join(', ')} à ${widths.join(' et ')} px) :\n${lines.join('\n')}`);
    code = 1;
  }
  if (csp.length > 0) {
    console.error(`${prefix}Violations CSP : ${csp.join(' | ')}`);
    code = 1;
  }
  if (consoleErrors.length > 0) {
    console.error(`${prefix}Console navigateur en erreur : ${consoleErrors.join(' | ')}`);
    code = 1;
  }
  if (code === 0) {
    console.log(`${prefix}Sonde des langues validée : ${measuredPages} pages mesurées en ${languages.join(', ')} à ${widths.join(' et ')} px, aucun texte ne sort de sa boîte, aucune page ne défile en largeur ; aucune violation CSP, console sans erreur.`);
  }
  return code;
}
