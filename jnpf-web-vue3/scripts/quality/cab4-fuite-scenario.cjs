/**
 * Memory cabinet — fuite scenario for Studio / SSE paths.
 *
 * Prerequisites:
 *   - Dev front :3100 running (start-dev.ps1)
 *   - Logged-in session OR public route that mounts the panel
 *   - Chrome available for Puppeteer (fuite). If chrome download failed:
 *       set PUPPETEER_EXECUTABLE_PATH to system Chrome
 *
 * Usage:
 *   node scripts/quality/cab4-fuite-scenario.cjs
 *   node scripts/quality/cab4-fuite-scenario.cjs --url http://localhost:3100/
 *
 * This script prints the exact fuite CLI command and writes a runbook JSON;
 * it invokes fuite when --run is passed.
 */
const { spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '../..');
const evidence = path.resolve(root, '../.claude/evidence/frontend-ct');
fs.mkdirSync(evidence, { recursive: true });

const args = process.argv.slice(2);
const run = args.includes('--run');
const urlIdx = args.indexOf('--url');
// Prefer index.html — bare `/` can 404 under some Vite states; SPA still boots from index.html
const url = urlIdx >= 0 ? args[urlIdx + 1] : 'http://127.0.0.1:3100/index.html';
const scenarioFile = path.join(__dirname, 'cab4-fuite-scenario-def.cjs');
const outJson = path.join(evidence, 'cab4-fuite.json').replace(/\\/g, '/');

const scenario = {
  generatedAt: new Date().toISOString(),
  cabinet: 4,
  purpose: 'Detect retained heaps via reload iterations on PC Vite entry',
  url,
  scenarioFile,
  steps: [
    'Load Vite index.html (domcontentloaded, not networkidle).',
    'Reload N times; fuite compares heap growth.',
    'For Studio/SSE deep path: pass --url after login cookie setup later.',
  ],
  r6Checklist: [
    'Every setTimeout/setInterval stored and cleared in onUnmounted',
    'EventSource reconnect capped; onerror never sync-reconnects',
    'SSE URL via buildEventSourceUrl() with ?token=',
  ],
  env: {
    PUPPETEER_EXECUTABLE_PATH:
      process.env.PUPPETEER_EXECUTABLE_PATH ||
      '(unset — set to Chrome if puppeteer browser missing)',
  },
  command: [
    'pnpm',
    'exec',
    'fuite',
    url,
    '--scenario',
    scenarioFile.replace(/\\/g, '/'),
    '--iterations',
    '5',
    '--heapsnapshot',
    '--output',
    outJson,
  ],
};

const outMeta = path.join(evidence, 'cab4-fuite-runbook.json');
fs.writeFileSync(outMeta, JSON.stringify(scenario, null, 2), 'utf8');
console.log('Wrote', outMeta);
console.log('Command:', scenario.command.join(' '));

if (!run) {
  console.log('Dry-run only. Pass --run to invoke fuite (needs Chrome + :3100).');
  process.exit(0);
}

const env = { ...process.env };
const chromeCandidates = [
  process.env.PUPPETEER_EXECUTABLE_PATH,
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
].filter(Boolean);
for (const c of chromeCandidates) {
  if (fs.existsSync(c)) {
    env.PUPPETEER_EXECUTABLE_PATH = c;
    env.CHROME_PATH = c;
    break;
  }
}
scenario.env.PUPPETEER_EXECUTABLE_PATH = env.PUPPETEER_EXECUTABLE_PATH || scenario.env.PUPPETEER_EXECUTABLE_PATH;
fs.writeFileSync(outMeta, JSON.stringify(scenario, null, 2), 'utf8');

const timeoutMs = 600000;
const result = spawnSync(
  process.platform === 'win32' ? 'pnpm.cmd' : 'pnpm',
  scenario.command.slice(1),
  { cwd: root, env, encoding: 'utf8', shell: true, timeout: timeoutMs },
);
fs.writeFileSync(
  path.join(evidence, 'cab4-fuite-stdout.txt'),
  (result.stdout || '') + '\n' + (result.stderr || ''),
  'utf8',
);
console.log(result.stdout || '');
console.error(result.stderr || '');
process.exit(result.status == null ? 1 : result.status);
