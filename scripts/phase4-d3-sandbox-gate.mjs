#!/usr/bin/env node
/**
 * D3-GATE — leave-simple 渲染产物 sandbox dotnet build
 * 等价于: cd backend/tests/JNPF.Tests.PhaseB && dotnet run -- sandbox-gate
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const phaseBDir = path.join(repoRoot, 'backend', 'tests', 'JNPF.Tests.PhaseB');

console.log('[D3-GATE] building PhaseB test host...');
const build = spawnSync('dotnet', ['build', '-v', 'q'], {
  cwd: phaseBDir,
  stdio: 'inherit',
  shell: true,
});
if (build.status !== 0) process.exit(build.status ?? 1);

console.log('[D3-GATE] running sandbox-gate...');
const gate = spawnSync('dotnet', ['run', '--no-build', '--', 'sandbox-gate'], {
  cwd: phaseBDir,
  stdio: 'inherit',
  shell: true,
  env: { ...process.env },
});

if (gate.status === 0) {
  console.log('[D3-GATE] PASS — leave-simple sandbox dotnet build exit 0');
}
process.exit(gate.status ?? 1);
