/**
 * Phase4 / PhaseB 共享 dotnet build 配置
 * 与 start-dev.ps1 Invoke-BackendBuild 对齐：关 Analyzer、单线程、禁 nodeReuse，降低 DLL 锁与 MSB4166
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
export const REPO_ROOT = path.resolve(__dirname, '../..');
export const PHASEB_DIR = path.join(REPO_ROOT, 'backend', 'tests', 'JNPF.Tests.PhaseB');

/** 与 start-dev.ps1 Invoke-BackendBuild 一致 */
export const DOTNET_BUILD_ARGS = [
  'build',
  '-v',
  'q',
  '/nologo',
  '/nodeReuse:false',
  '-m:1',
  '-p:BuildInParallel=false',
  '-p:UseSharedCompilation=false',
  '-p:RunAnalyzers=false',
];

/**
 * @param {{ cwd?: string, inherit?: boolean, retries?: number }} [options]
 */
export function buildPhaseB(options = {}) {
  const cwd = options.cwd ?? PHASEB_DIR;
  const inherit = options.inherit ?? false;
  const retries = options.retries ?? 1;
  let last = null;

  for (let attempt = 0; attempt <= retries; attempt++) {
    if (attempt > 0) {
      spawnSync('powershell', ['-Command', 'Start-Sleep -Seconds 2'], { shell: true });
    }
    last = spawnSync('dotnet', DOTNET_BUILD_ARGS, {
      cwd,
      stdio: inherit ? 'inherit' : 'pipe',
      shell: true,
      encoding: 'utf8',
    });
    if (last.status === 0) {
      return { pass: true, attempt };
    }
  }

  return {
    pass: false,
    exitCode: last?.status ?? 1,
    stdoutTail: ((last?.stdout || '') + (last?.stderr || '')).split('\n').slice(-20).join('\n'),
  };
}

/**
 * @param {string[]} args  传给 PhaseB 测试宿主 CLI
 * @param {{ cwd?: string, inherit?: boolean, env?: Record<string,string> }} [options]
 */
export function runPhaseBCli(args, options = {}) {
  return spawnSync('dotnet', ['run', '--no-build', '--', ...args], {
    cwd: options.cwd ?? PHASEB_DIR,
    stdio: options.inherit ? 'inherit' : 'pipe',
    shell: true,
    encoding: 'utf8',
    env: { ...process.env, ...options.env },
  });
}

/** 释放端口/dotnet 锁（会停 :5000 后端，build-only 门禁前应调用） */
export function runDevCleanup() {
  const script = path.join(REPO_ROOT, 'start-dev.ps1');
  return spawnSync('powershell', ['-ExecutionPolicy', 'Bypass', '-File', script, '-CleanupOnly'], {
    cwd: REPO_ROOT,
    stdio: 'pipe',
    encoding: 'utf8',
  });
}
