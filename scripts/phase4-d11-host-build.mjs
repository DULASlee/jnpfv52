#!/usr/bin/env node
/**
 * D11-D12 GATE — 宿主注入 + 全工程 dotnet build
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { buildPhaseB, PHASEB_DIR, runPhaseBCli } from './lib/dotnet-build.mjs';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const phaseBDir = PHASEB_DIR;
const injectScript = path.join(repoRoot, 'scripts', 'codegen-inject-host.mjs');
const NO_BUILD = process.argv.includes('--no-build');

console.log('[D11-GATE] inject leave-simple...');
const inject = spawnSync(
  process.execPath,
  [injectScript, '--ensure-generated', '--tenant', '_d3-gate', '--project', 'leave-simple', '--skip-build'],
  { cwd: repoRoot, stdio: 'inherit' },
);
if (inject.status !== 0) process.exit(inject.status ?? 1);

if (!NO_BUILD) {
  console.log('[D11-GATE] building PhaseB host...');
  const built = buildPhaseB({ inherit: true, retries: 1 });
  if (!built.pass) process.exit(built.exitCode ?? 1);
} else {
  console.log('[D11-GATE] skip PhaseB build (--no-build)');
}

console.log('[D11-GATE] host-demo full build...');
const fullBuild = runPhaseBCli(['host-demo-build'], { inherit: true });

if (fullBuild.status === 0) {
  console.log('[D11-GATE] PASS — codegen-host-demo full build exit 0');
}
process.exit(fullBuild.status ?? 1);
