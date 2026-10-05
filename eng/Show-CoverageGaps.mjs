// PLAN-014: reads a Cobertura report of the RCL and prints, for each source file that is not fully
// covered, every line never run ("M") and every line with a branch outcome never taken ("B taken/total"),
// next to its source text. With --conditions, each partial line also lists its conditions as
// "ilOffset:coverage%": a condition at 50% took one of its two outcomes only, which tells which part of
// a compound condition (a && b || c) a test still has to reach.
//
// A .razor line often reports a branch that is not its own: the Razor compiler maps a conditional
// attribute (class="@(x ? a : null)", disabled="@(a || b)") to a neighbouring line such as an @if or a
// @ChildContent. Look at the attributes around the reported line before calling it unreachable.
//
// Usage:
//   node eng/Show-CoverageGaps.mjs <report.xml | directory> [file filter] [--conditions]
// A directory is searched for its most recent coverage.cobertura*.xml (recursively). The filter keeps
// the files whose path (relative to src/OmniEurope.Blazor, forward slashes) contains it.
// Exit code: 0 always; the gate itself is eng/Test-Coverage.ps1.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const args = process.argv.slice(2);
const showConditions = args.includes('--conditions');
const [target, filter = ''] = args.filter(arg => arg !== '--conditions');
if (!target) {
  console.error('Usage: node eng/Show-CoverageGaps.mjs <report.xml | directory> [file filter] [--conditions]');
  process.exit(2);
}

const sourceRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', 'src', 'OmniEurope.Blazor');

function findReport(start) {
  if (fs.statSync(start).isFile()) {
    return start;
  }

  const found = [];
  const walk = directory => {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      const full = path.join(directory, entry.name);
      if (entry.isDirectory()) {
        walk(full);
      } else if (/^coverage\.cobertura.*\.xml$/.test(entry.name)) {
        found.push(full);
      }
    }
  };
  walk(start);
  if (found.length === 0) {
    console.error(`No coverage.cobertura*.xml under ${start}.`);
    process.exit(2);
  }

  return found.sort((a, b) => fs.statSync(b).mtimeMs - fs.statSync(a).mtimeMs)[0];
}

const report = findReport(target);
const xml = fs.readFileSync(report, 'utf8');

// One entry per source line, merged over the classes the compiler made of the file (lambdas, async
// state machines): the most hits, the fewest missed outcomes, and every condition reading.
const files = new Map();
for (const cls of xml.matchAll(/<class [^>]*name="([^"]*)" filename="([^"]*)"[^>]*>([\s\S]*?)<\/class>/g)) {
  const file = cls[2].replaceAll('\\', '/');
  if (!file.includes(filter)) {
    continue;
  }

  const body = cls[3].replace(/<methods>[\s\S]*<\/methods>/, '');
  const lines = files.get(file) ?? new Map();
  for (const line of body.matchAll(/<line number="(\d+)" hits="(\d+)"([^>]*?)(?:\/>|>([\s\S]*?)<\/line>)/g)) {
    const number = Number(line[1]);
    const hits = Number(line[2]);
    const coverage = /condition-coverage="[^"]*\((\d+)\/(\d+)\)"/.exec(line[3]);
    const missed = coverage ? Number(coverage[2]) - Number(coverage[1]) : 0;
    const conditions = [...(line[4] ?? '').matchAll(/condition number="(\d+)"[^>]*coverage="(\d+)%"/g)].map(c => `${c[1]}:${c[2]}`);
    const previous = lines.get(number);
    lines.set(number, {
      hits: Math.max(hits, previous?.hits ?? 0),
      missed: previous ? Math.min(previous.missed, missed) : missed,
      ratio: coverage ? `${coverage[1]}/${coverage[2]}` : previous?.ratio ?? '',
      conditions: [...(previous?.conditions ?? []), ...conditions]
    });
  }

  files.set(file, lines);
}

let fileCount = 0;
let lineCount = 0;
let branchCount = 0;
for (const [file, lines] of [...files].sort(([a], [b]) => a.localeCompare(b))) {
  const gaps = [...lines].filter(([, value]) => value.hits === 0 || value.missed > 0).sort((a, b) => a[0] - b[0]);
  if (gaps.length === 0) {
    continue;
  }

  fileCount++;
  const sourcePath = path.join(sourceRoot, file);
  const source = fs.existsSync(sourcePath) ? fs.readFileSync(sourcePath, 'utf8').split(/\r?\n/) : [];
  console.log(`== ${file}`);
  for (const [number, value] of gaps) {
    const never = value.hits === 0;
    lineCount += never ? 1 : 0;
    branchCount += never ? 0 : value.missed;
    const kind = never ? 'M    ' : `B ${value.ratio.padEnd(5)}`;
    const conditions = showConditions && !never && value.conditions.length > 0 ? `   [${value.conditions.join(' ')}]` : '';
    console.log(`${String(number).padStart(5)} ${kind} ${(source[number - 1] ?? '').trim()}${conditions}`);
  }
}

console.log(`-- ${report}`);
console.log(`-- ${fileCount} file(s), ${lineCount} line(s) never run, ${branchCount} branch outcome(s) never taken on partly run lines.`);
