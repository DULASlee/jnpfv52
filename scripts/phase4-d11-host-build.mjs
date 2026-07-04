#!/usr/bin/env node
/**
 * D11-D12 GATE — 宿主注入 + 全工程 dotnet build
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const phaseBDir = path.join(repoRoot, 'backend', 'tests', 'JNPF.Tests.PhaseB');
const injectScript = path.join(repoRoot, 'scripts', 'codegen-inject-host.mjs');

console.log('[D11-GATE] inject leave-simple...');
const inject = spawnSync(
  process.execPath,
  [injectScript, '--ensure-generated', '--tenant', '_d3-gate', '--project', 'leave-simple', '--skip-build'],
  { cwd: repoRoot, stdio: 'inherit' },
);
if (inject.status !== 0) process.exit(inject.status ?? 1);

console.log('[D11-GATE] building PhaseB host...');
const buildPhaseB = spawnSync('dotnet', ['build', '-v', 'q'], {
  cwd: phaseBDir,
  stdio: 'inherit',
  shell: true,
});
if (buildPhaseB.status !== 0) process.exit(buildPhaseB.status ?? 1);

console.log('[D11-GATE] host-demo full build...');
const fullBuild = spawnSync('dotnet', ['run', '--no-build', '--', 'host-demo-build'], {
  cwd: phaseBDir,
  stdio: 'inherit',
  shell: true,
});

if (fullBuild.status === 0) {
  console.log('[D11-GATE] PASS — codegen-host-demo full build exit 0');
}
process.exit(fullBuild.status ?? 1);
