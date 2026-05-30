#!/usr/bin/env node
/**
 * JNPF V5.2 开发环境检查脚本
 * 用法：node scripts/check-dev-env.mjs
 */
import { spawnSync } from 'child_process';
import fs from 'fs';
import path from 'path';
import net from 'net';

const repoRoot = path.resolve(path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Z]:)/, '$1')));
const root = path.resolve(repoRoot, '..');
const results = [];

function ok(name, detail = '') {
  results.push({ pass: true });
  console.log(`  [OK]   ${name}${detail ? ` — ${detail}` : ''}`);
}
function fail(name, detail = '') {
  results.push({ pass: false });
  console.log(`  [FAIL] ${name}${detail ? ` — ${detail}` : ''}`);
}
function warn(name, detail = '') {
  results.push({ pass: true, warn: true });
  console.log(`  [WARN] ${name}${detail ? ` — ${detail}` : ''}`);
}

console.log('\n=== JNPF V5.2 Development Environment Check ===\n');

// 1. .NET SDK
const dotnetV = spawnSync('dotnet', ['--version'], { encoding: 'utf8', shell: true });
if (dotnetV.status === 0) {
  const ver = dotnetV.stdout.trim();
  if (ver.startsWith('6.')) ok('.NET SDK', ver);
  else warn('.NET SDK', `found ${ver}, expected 6.x (global.json locks latestPatch)`);
} else {
  fail('.NET SDK', 'dotnet not found in PATH');
}

// 2. Node.js
const nodeV = spawnSync('node', ['--version'], { encoding: 'utf8', shell: true });
if (nodeV.status === 0) {
  const ver = nodeV.stdout.trim();
  const major = parseInt(ver.replace('v', ''));
  if (major >= 16) ok('Node.js', ver);
  else fail('Node.js', `found ${ver}, requires >= 16`);
} else {
  fail('Node.js', 'node not found');
}

// 3. pnpm
const pnpmV = spawnSync('pnpm', ['--version'], { encoding: 'utf8', shell: true });
if (pnpmV.status === 0) ok('pnpm', pnpmV.stdout.trim());
else fail('pnpm', 'not installed (npm i -g pnpm)');

// 4. global.json
const gj = path.join(root, 'backend', 'global.json');
if (fs.existsSync(gj)) {
  const gjContent = JSON.parse(fs.readFileSync(gj, 'utf8'));
  if (gjContent.sdk?.rollForward === 'latestFeature' && !gjContent.sdk?.allowPrerelease) {
    ok('global.json', `SDK ${gjContent.sdk.version}, rollForward=${gjContent.sdk.rollForward}`);
  } else {
    warn('global.json', 'rollForward should be latestFeature, allowPrerelease should be false');
  }
} else {
  fail('global.json', 'not found');
}

// 5. ConnectionStrings.json
const csJson = path.join(root, 'backend', 'application', 'JNPF.API.Entry', 'Configurations', 'ConnectionStrings.json');
const csExample = path.join(root, 'backend', 'application', 'JNPF.API.Entry', 'Configurations', 'ConnectionStrings.example.json');
if (fs.existsSync(csJson)) ok('ConnectionStrings.json', 'exists');
else if (fs.existsSync(csExample)) fail('ConnectionStrings.json', 'missing — copy from ConnectionStrings.example.json and fill in values');
else fail('ConnectionStrings.json', 'missing, no example template found either');

// 6. Frontend node_modules
const nm = path.join(root, 'jnpf-web-vue3', 'node_modules');
if (fs.existsSync(nm)) ok('Frontend node_modules', 'installed');
else warn('Frontend node_modules', 'not found — run: cd jnpf-web-vue3 && pnpm install');

// 7. .editorconfig root=true
const ec = path.join(root, 'backend', '.editorconfig');
if (fs.existsSync(ec)) {
  const content = fs.readFileSync(ec, 'utf8');
  if (content.includes('root = true')) ok('.editorconfig', 'root = true');
  else warn('.editorconfig', 'missing root = true — IDE may inherit parent configs');
} else {
  fail('.editorconfig', 'not found');
}

// 8. Convention docs
const conventions = ['naming.md', 'git-workflow.md', 'error-response.md', 'logging.md'];
for (const c of conventions) {
  const p = path.join(root, 'docs', 'conventions', c);
  if (fs.existsSync(p)) ok(`docs/conventions/${c}`);
  else fail(`docs/conventions/${c}`, 'missing');
}

// 9. .gitignore secrets
const gi = fs.readFileSync(path.join(root, '.gitignore'), 'utf8');
const mustIgnore = ['ConnectionStrings.json', '.env.local', '*.key', '*.pfx'];
for (const pattern of mustIgnore) {
  if (gi.includes(pattern)) ok(`.gitignore: ${pattern}`);
  else warn(`.gitignore: ${pattern}`, 'not found — sensitive files may leak');
}

// Summary
const passed = results.filter(r => r.pass && !r.warn).length;
const warned = results.filter(r => r.warn).length;
const failed = results.filter(r => !r.pass).length;
console.log(`\n=== Summary: ${passed} passed, ${warned} warned, ${failed} failed ===\n`);
process.exit(failed > 0 ? 1 : 0);
