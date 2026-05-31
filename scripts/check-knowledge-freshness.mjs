#!/usr/bin/env node
/**
 * Knowledge freshness checker. Exit 1 if any FAIL.
 * Usage: node scripts/check-knowledge-freshness.mjs
 */
import fs from 'fs';
import path from 'path';
import { spawnSync } from 'child_process';
import { getRepoRoot } from './toolchain-lib.mjs';

const root = getRepoRoot();
let alerts = 0;

function gitLastCommitDays(fileRel) {
  const r = spawnSync('git', ['log', '-1', '--format=%ct', '--', fileRel], {
    cwd: root,
    encoding: 'utf8',
  });
  if (r.status !== 0 || !r.stdout.trim()) return null;
  const ts = parseInt(r.stdout.trim(), 10);
  return Math.floor((Date.now() / 1000 - ts) / 86400);
}

function check(label, fileRel, maxDays, required = true) {
  const abs = path.join(root, fileRel);
  if (!fs.existsSync(abs)) {
    if (required) {
      console.log(`  [FAIL] ${label}: 文件不存在 (${fileRel})`);
      alerts++;
    } else {
      console.log(`  [SKIP] ${label}: 不存在`);
    }
    return;
  }
  const days = gitLastCommitDays(fileRel);
  if (days === null) {
    console.log(`  [WARN] ${label}: 无 git 历史 (${fileRel})`);
    return;
  }
  if (days > maxDays) {
    console.log(`  [FAIL] ${label}: ${days} 天前更新（阈值 ${maxDays} 天）`);
    alerts++;
  } else {
    console.log(`  [OK] ${label}: ${days} 天前更新`);
  }
}

console.log('\n=== Knowledge Freshness Check ===\n');

console.log('--- Rules (.cursor/rules/) ---');
const rulesDir = path.join(root, '.cursor', 'rules');
if (fs.existsSync(rulesDir)) {
  for (const f of fs.readdirSync(rulesDir).filter((x) => x.endsWith('.mdc'))) {
    check(f, `.cursor/rules/${f}`, 60, false);
  }
}

console.log('\n--- OpenSpec specs ---');
function walkSpecs(dir, base = 'openspec/specs') {
  if (!fs.existsSync(dir)) return;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const rel = `${base}/${ent.name}`;
    const abs = path.join(dir, ent.name);
    if (ent.isDirectory()) walkSpecs(abs, rel);
    else if (ent.name === 'spec.md') check(rel, rel, 90);
  }
}
walkSpecs(path.join(root, 'openspec', 'specs'));

console.log('\n--- ADR ---');
const adrDir = path.join(root, 'openspec', 'adr');
if (fs.existsSync(adrDir)) {
  for (const f of fs.readdirSync(adrDir).filter((x) => x.startsWith('ADR-') && x.endsWith('.md'))) {
    check(f, `openspec/adr/${f}`, 180, false);
  }
} else {
  console.log('  [FAIL] openspec/adr/ 目录不存在');
  alerts++;
}

console.log('\n--- Toolchain ---');
check('toolchain.manifest', '.cursor/toolchain.manifest.json', 365);
check('episodic search-templates', '.cursor/episodic/search-templates.yaml', 90);
check('knowledge-base rule', '.cursor/rules/knowledge-base.mdc', 90, false);

console.log('\n--- Progress registry ---');
const registry = path.join(root, 'docs', 'progress-registry.yaml');
if (fs.existsSync(registry)) {
  const text = fs.readFileSync(registry, 'utf8');
  if (/entries:\s*\[\s*\]/.test(text) || !text.includes('knowledge-base-infra')) {
    console.log('  [WARN] progress-registry: 尚无 knowledge-base 完成条目');
  } else {
    check('progress-registry', 'docs/progress-registry.yaml', 14);
  }
} else {
  console.log('  [FAIL] progress-registry 不存在');
  alerts++;
}

console.log(`\n=== Result: ${alerts === 0 ? 'PASS' : `${alerts} FAIL(S)`} ===\n`);
process.exit(alerts > 0 ? 1 : 0);
