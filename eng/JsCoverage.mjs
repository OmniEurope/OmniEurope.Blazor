// PLAN-014, lot JavaScript: V8 precise coverage of the package's scripts, taken during a showcase probe.
// Inactive unless OMNI_JS_COVERAGE_DIR names a folder (eng/Test-ShowcaseHost.ps1 -JsCoverage sets it):
// the probe then runs exactly as before. Coverage is taken before each Page.navigate, since a new document
// drops the scripts of the previous one, and once more when the probe finishes; every take is written to
// <dir>/<probe>-<time>.json for eng/Test-JsCoverage.mjs to merge.
import fs from 'node:fs';
import path from 'node:path';

const PackagePath = '/_content/OmniEurope.Blazor/';

export function createJsCoverage(sendCdp, probeName) {
  const directory = process.env.OMNI_JS_COVERAGE_DIR;
  const takes = [];
  let started = false;

  async function take() {
    if (!started) {
      return;
    }

    const { result } = await sendCdp('Profiler.takePreciseCoverage');
    takes.push(...result.filter(script => script.url.includes(PackagePath)));
  }

  return {
    // The probe's own send, which takes the coverage of the page about to be left before a navigation.
    async send(method, params = {}) {
      if (method === 'Page.navigate') {
        await take();
      }

      return sendCdp(method, params);
    },

    async start() {
      if (!directory) {
        return;
      }

      await sendCdp('Profiler.enable');
      await sendCdp('Profiler.startPreciseCoverage', { callCount: true, detailed: true });
      started = true;
    },

    async finish() {
      if (!directory) {
        return;
      }

      await take();
      fs.mkdirSync(directory, { recursive: true });
      const file = path.join(directory, `${probeName}-${Date.now()}.json`);
      fs.writeFileSync(file, JSON.stringify({ probe: probeName, scripts: takes }));
    }
  };
}
