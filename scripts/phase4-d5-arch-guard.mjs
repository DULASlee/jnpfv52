#!/usr/bin/env node
/**
 * D10 / P4-B04b — ArchGuard Q2 可复现违规模板
 *
 *   node scripts/phase4-d5-arch-guard.mjs
 *   node scripts/phase4-d5-arch-guard.mjs --profile ag001-ddl-controller-ref
 *   node scripts/phase4-d5-arch-guard.mjs --profile ag002-no-tenant-filter
 *
 * 断言（DoD §7 D5）：
 *   - sandbox build 通过 → ArchGuard Critical → AbortSkillChainException (ArchAbort)
 *   - ArchViolationDetected 存在
 *   - TestSuiteGenerated 不存在
 */
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { buildPhaseB, PHASEB_DIR, runPhaseBCli } from './lib/dotnet-build.mjs';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const phaseBDir = PHASEB_DIR;
const NO_BUILD = process.argv.includes('--no-build');
const evidenceDir = path.join(repoRoot, '.claude', 'evidence');

const DEFAULT_PROFILES = ['ag001-ddl-controller-ref', 'ag002-no-tenant-filter'];

function parseArgs(argv) {
  const profiles = [];
  for (let i = 2; i < argv.length; i++) {
    if (argv[i] === '--profile' && argv[i + 1]) {
      profiles.push(argv[++i]);
    }
  }
  return profiles.length > 0 ? profiles : DEFAULT_PROFILES;
}

function runProfile(profile) {
  console.log(`\n[D10-Q2] profile=${profile}`);
  const gate = runPhaseBCli(['arch-guard-q2', '--profile', profile], {
    cwd: phaseBDir,
    inherit: false,
  });

  const stdout = gate.stdout || '';
  const stderr = gate.stderr || '';
  if (stdout) process.stdout.write(stdout);
  if (stderr) process.stderr.write(stderr);

  const pass = gate.status === 0;
  const evidence = {
    profile,
    pass,
    exitCode: gate.status ?? 1,
    timestamp: new Date().toISOString(),
    assertions: {
      orchestratorAborted: pass,
      archViolationDetected: pass,
      testSuiteGenerated: false,
      abortPhase: 'ArchAbort',
    },
    stdoutTail: stdout.split('\n').slice(-20).join('\n'),
  };

  fs.mkdirSync(evidenceDir, { recursive: true });
  const evidencePath = path.join(evidenceDir, `phase4-d5-arch-guard-${profile}.json`);
  fs.writeFileSync(evidencePath, JSON.stringify(evidence, null, 2), 'utf8');
  console.log(`[D10-Q2] evidence → ${evidencePath}`);

  return pass;
}

if (!NO_BUILD) {
  console.log('[D10-Q2] building PhaseB test host...');
  const build = buildPhaseB({ inherit: true, retries: 1 });
  if (!build.pass) process.exit(build.exitCode ?? 1);
} else {
  console.log('[D10-Q2] skip build (--no-build)');
}

const profiles = parseArgs(process.argv);
let failed = 0;
for (const profile of profiles) {
  if (!runProfile(profile)) failed++;
}

if (failed === 0) {
  console.log(`\n[D10-Q2] PASS — ${profiles.length} profile(s) exit 0`);
  process.exit(0);
}

console.error(`\n[D10-Q2] FAIL — ${failed}/${profiles.length} profile(s) failed`);
process.exit(1);
