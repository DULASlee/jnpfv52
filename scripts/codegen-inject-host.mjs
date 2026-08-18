#!/usr/bin/env node
/**
 * P4-B06 — 将 workspace/generated/{tenant}/{project}/backend 注入宿主并全工程 build
 *
 *   node scripts/codegen-inject-host.mjs --tenant _d3-gate --project leave-simple
 *   node scripts/codegen-inject-host.mjs --ensure-generated   # 缺产物时先跑 D3 sandbox-gate
 */
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const phaseBDir = path.join(repoRoot, 'backend', 'tests', 'JNPF.Tests.PhaseB');
const hostDir = path.join(repoRoot, 'workspace', 'codegen-host-demo');
const hostSln = path.join(hostDir, 'JNPF.Codegen.HostDemo.sln');
const hostCsproj = path.join(hostDir, 'JNPF.Codegen.HostDemo.csproj');
const generatedTarget = path.join(hostDir, 'Modules', 'Generated');
const nugetPackages = path.join(repoRoot, 'workspace', 'codegen-sandbox', '.nuget', 'packages');
const evidenceDir = path.join(repoRoot, '.claude', 'evidence');

function parseArgs(argv) {
  let tenant = '_d3-gate';
  let project = 'leave-simple';
  let ensureGenerated = false;
  let skipBuild = false;

  for (let i = 2; i < argv.length; i++) {
    if (argv[i] === '--tenant' && argv[i + 1]) tenant = argv[++i];
    else if (argv[i] === '--project' && argv[i + 1]) project = argv[++i];
    else if (argv[i] === '--ensure-generated') ensureGenerated = true;
    else if (argv[i] === '--skip-build') skipBuild = true;
  }

  return { tenant, project, ensureGenerated, skipBuild };
}

function copyGeneratedTree(sourceRoot, targetRoot) {
  if (fs.existsSync(targetRoot)) {
    fs.rmSync(targetRoot, { recursive: true, force: true });
  }
  fs.mkdirSync(targetRoot, { recursive: true });

  const stack = [sourceRoot];
  while (stack.length) {
    const current = stack.pop();
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const src = path.join(current, entry.name);
      const rel = path.relative(sourceRoot, src);
      const dest = path.join(targetRoot, rel);

      if (entry.isDirectory()) {
        if (entry.name === 'obj' || entry.name === 'bin') continue;
        fs.mkdirSync(dest, { recursive: true });
        stack.push(src);
        continue;
      }

      if (entry.name.endsWith('.csproj')) continue;
      fs.mkdirSync(path.dirname(dest), { recursive: true });
      fs.copyFileSync(src, dest);
    }
  }
}

function ensureLeaveSimpleGenerated() {
  console.log('[host-inject] running D3 sandbox-gate to materialize leave-simple...');
  const build = spawnSync('dotnet', ['build', '-v', 'q'], {
    cwd: phaseBDir,
    stdio: 'inherit',
    shell: true,
  });
  if (build.status !== 0) process.exit(build.status ?? 1);

  const gate = spawnSync('dotnet', ['run', '--no-build', '--', 'sandbox-gate'], {
    cwd: phaseBDir,
    stdio: 'inherit',
    shell: true,
  });
  if (gate.status !== 0) process.exit(gate.status ?? 1);
}

function countCsFiles(dir) {
  if (!fs.existsSync(dir)) return 0;
  let count = 0;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) count += countCsFiles(full);
    else if (entry.name.endsWith('.cs')) count += 1;
  }
  return count;
}

const opts = parseArgs(process.argv);
const sourceRoot = path.join(repoRoot, 'workspace', 'generated', opts.tenant, opts.project, 'backend');

if (!fs.existsSync(sourceRoot) || countCsFiles(sourceRoot) === 0) {
  if (opts.ensureGenerated) ensureLeaveSimpleGenerated();
  else {
    console.error(`[host-inject] source missing: ${sourceRoot}`);
    console.error('  hint: node scripts/codegen-inject-host.mjs --ensure-generated');
    process.exit(1);
  }
}

if (!fs.existsSync(sourceRoot) || countCsFiles(sourceRoot) === 0) {
  console.error(`[host-inject] still no generated cs files under ${sourceRoot}`);
  process.exit(1);
}

console.log(`[host-inject] copy ${sourceRoot} → ${generatedTarget}`);
copyGeneratedTree(sourceRoot, generatedTarget);
console.log(`[host-inject] injected ${countCsFiles(generatedTarget)} .cs file(s)`);

if (opts.skipBuild) {
  console.log('[host-inject] --skip-build set, done.');
  process.exit(0);
}

if (!fs.existsSync(nugetPackages)) {
  console.log('[host-inject] NuGet cache missing — run codegen-init-workspace.ps1 first');
}

const env = { ...process.env, NUGET_PACKAGES: nugetPackages };

const hostMarker = path.join(hostDir, '.restore-complete');

console.log('[host-inject] dotnet restore host solution...');
if (!fs.existsSync(hostMarker)) {
  const restore = spawnSync(
    'dotnet',
    ['restore', hostSln, '--packages', nugetPackages],
    { cwd: repoRoot, stdio: 'inherit', shell: true, env },
  );
  if (restore.status !== 0) process.exit(restore.status ?? 1);
  fs.writeFileSync(hostMarker, new Date().toISOString(), 'utf8');
} else {
  console.log('[host-inject] restore marker present — skip restore');
}

console.log('[host-inject] dotnet build host solution (full project, not sandbox csproj)...');
const build = spawnSync(
  'dotnet',
  [
    'build',
    hostSln,
    '--no-restore',
    '-v',
    'q',
    `/p:RestorePackagesPath=${nugetPackages}`,
  ],
  { cwd: repoRoot, stdio: 'inherit', shell: true, env },
);

const pass = build.status === 0;
const evidence = {
  tenant: opts.tenant,
  project: opts.project,
  sourceRoot,
  generatedTarget,
  csFileCount: countCsFiles(generatedTarget),
  pass,
  exitCode: build.status ?? 1,
  timestamp: new Date().toISOString(),
};

fs.mkdirSync(evidenceDir, { recursive: true });
const evidencePath = path.join(
  evidenceDir,
  `phase4-d11-host-build-${opts.project}.json`,
);
fs.writeFileSync(evidencePath, JSON.stringify(evidence, null, 2), 'utf8');

if (pass) {
  console.log(`[host-inject] PASS — Build succeeded → ${evidencePath}`);
  process.exit(0);
}

console.error(`[host-inject] FAIL — host build exit ${build.status}`);
process.exit(build.status ?? 1);
