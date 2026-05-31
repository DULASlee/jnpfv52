#!/usr/bin/env node
/**
 * Verify Superpowers + OpenSpec + episodic-memory toolchain in current repo.
 * Usage: node scripts/verify-toolchain.mjs
 */
import fs from 'fs';
import path from 'path';
import { spawnSync } from 'child_process';
import { loadManifest, getRepoRoot } from './toolchain-lib.mjs';
import { EPISODIC_CLI } from './episodic-config.mjs';

const repoRoot = getRepoRoot();
const results = [];

function ok(name, detail = '') {
  results.push({ name, pass: true, detail });
  console.log(`  [OK] ${name}${detail ? ` — ${detail}` : ''}`);
}

function fail(name, detail = '') {
  results.push({ name, pass: false, detail });
  console.log(`  [FAIL] ${name}${detail ? ` — ${detail}` : ''}`);
}

function warn(name, detail = '') {
  results.push({ name, pass: true, warn: true, detail });
  console.log(`  [WARN] ${name}${detail ? ` — ${detail}` : ''}`);
}

function parseSkillFrontmatter(content) {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  if (!match) return {};
  const fm = {};
  for (const line of match[1].split('\n')) {
    const m = line.match(/^([\w-]+):\s*(.+)$/);
    if (m) fm[m[1]] = m[2].trim();
  }
  return fm;
}

console.log('\n=== Toolchain Verification ===\n');
console.log(`Repo: ${repoRoot}\n`);

let manifest;
try {
  manifest = loadManifest(repoRoot);
  ok('toolchain.manifest.json', `project=${manifest.episodic_project_id}`);
} catch (e) {
  fail('toolchain.manifest.json', e.message);
  process.exit(1);
}

const skillDir = path.join(repoRoot, '.cursor', 'skills');
const skills = fs.existsSync(skillDir)
  ? fs.readdirSync(skillDir).filter((d) => fs.existsSync(path.join(skillDir, d, 'SKILL.md')))
  : [];
if (skills.length >= 10) ok('Superpowers skills', `${skills.length} skills`);
else fail('Superpowers skills', `found ${skills.length}, expected >= 10`);

const requiredSkills = ['brainstorming', 'writing-plans', 'executing-plans', 'verification-before-completion'];
const recommendedSkills = [
  'using-superpowers',
  'using-git-worktrees',
  'dispatching-parallel-agents',
  'receiving-code-review',
  'finishing-a-development-branch',
  'writing-skills',
];
for (const s of requiredSkills) {
  if (skills.includes(s)) ok(`skill:${s}`);
  else fail(`skill:${s}`, 'missing');
}
for (const s of recommendedSkills) {
  if (skills.includes(s)) ok(`skill:${s}`);
  else fail(`skill:${s}`, 'missing (sync from superpowers plugin)');
}

const missingScope = [];
for (const s of skills) {
  const content = fs.readFileSync(path.join(skillDir, s, 'SKILL.md'), 'utf8');
  const fm = parseSkillFrontmatter(content);
  if (!fm.scope) missingScope.push(s);
}
if (missingScope.length === 0) ok('skill frontmatter:scope', 'all skills tagged');
else warn('skill frontmatter:scope', `missing in: ${missingScope.join(', ')}`);

if (fs.existsSync(path.join(repoRoot, '.cursor', 'hooks.json'))) ok('Cursor hooks.json');
else fail('Cursor hooks.json');

if (fs.existsSync(path.join(repoRoot, 'scripts', 'episodic-sync.mjs'))) ok('episodic-sync.mjs');
else fail('episodic-sync.mjs');

if (EPISODIC_CLI && fs.existsSync(EPISODIC_CLI)) ok('episodic CLI', path.basename(path.dirname(path.dirname(EPISODIC_CLI))));
else fail('episodic CLI', 'install plugin episodic-memory@superpowers-marketplace in Cursor');

const openspecConfig = path.join(repoRoot, 'openspec', 'config.yaml');
const openspecSpecs = path.join(repoRoot, 'openspec', 'specs');
if (fs.existsSync(openspecConfig)) ok('openspec/config.yaml');
else fail('openspec/config.yaml', 'run: openspec init --tools cursor');

if (fs.existsSync(openspecSpecs)) {
  const specCount = fs.readdirSync(openspecSpecs).filter((d) =>
    fs.existsSync(path.join(openspecSpecs, d, 'spec.md')),
  ).length;
  ok('openspec/specs', `${specCount} capability spec(s)`);
} else fail('openspec/specs');

const openspecList = spawnSync('openspec', ['list'], { cwd: repoRoot, encoding: 'utf8', shell: true });
if (openspecList.status === 0) ok('openspec CLI', 'openspec list');
else fail('openspec CLI', openspecList.stderr?.trim() || 'not in PATH');

const opsxCmds = ['opsx-propose', 'opsx-archive', 'opsx-explore'];
for (const c of opsxCmds) {
  if (fs.existsSync(path.join(repoRoot, '.cursor', 'commands', `${c}.md`))) ok(`command:${c}`);
  else fail(`command:${c}`);
}

if (fs.existsSync(path.join(repoRoot, '.cursor', 'rules', 'toolchain-division.mdc'))) ok('rule:toolchain-division');
else fail('rule:toolchain-division');

if (fs.existsSync(path.join(repoRoot, '.cursor', 'rules', 'knowledge-base.mdc'))) ok('rule:knowledge-base');
else fail('rule:knowledge-base');

if (fs.existsSync(path.join(repoRoot, 'scripts', 'check-knowledge-freshness.mjs'))) ok('check-knowledge-freshness.mjs');
else fail('check-knowledge-freshness.mjs');

const adr001 = path.join(repoRoot, 'openspec', 'adr', 'ADR-001-reject-local-vector-rag.md');
if (fs.existsSync(adr001)) ok('ADR-001');
else fail('ADR-001', 'missing openspec/adr/ADR-001-reject-local-vector-rag.md');

const passed = results.filter((r) => r.pass && !r.warn).length;
const warned = results.filter((r) => r.warn).length;
const failed = results.filter((r) => !r.pass).length;
console.log(`\n=== Summary: ${passed} passed, ${warned} warned, ${failed} failed ===\n`);
process.exit(failed > 0 ? 1 : 0);
