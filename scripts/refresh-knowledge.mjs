#!/usr/bin/env node
/**
 * Refresh knowledge indexes + run freshness check.
 * Usage: node scripts/refresh-knowledge.mjs [--check-only]
 */
import { spawnSync } from 'child_process';
import { getRepoRoot } from './toolchain-lib.mjs';

const root = getRepoRoot();
const checkOnly = process.argv.includes('--check-only');

function run(label, cmd, args) {
  console.log(`\n[Auto-Knowledge] ${label}`);
  const r = spawnSync(cmd, args, { cwd: root, stdio: 'inherit', shell: process.platform === 'win32' });
  if (r.status !== 0) console.warn(`  [WARN] ${label} exited ${r.status}`);
}

if (!checkOnly) {
  run('Update OpenSpec index', 'node', ['scripts/update-openspec-index.mjs']);
}
run('Freshness check', 'node', ['scripts/check-knowledge-freshness.mjs']);
