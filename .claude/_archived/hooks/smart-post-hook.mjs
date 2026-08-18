#!/usr/bin/env node
/**
 * smart-post-hook.mjs — PostToolUse 分级调度器
 *
 * L0: PreToolUse 已覆盖安全正则，此处不重复。
 * L1: 脏文件 ≤10 且为 jnpf-web-vue3 前端源码 → 单文件 eslint --fix
 * L2: git status --porcelain 计数提醒（禁止 git diff --unified=0 全量扫描）
 *
 * 失败/超时一律 exit(0)，不阻塞编辑。
 */

import { execSync } from 'child_process';
import { existsSync } from 'fs';
import { join } from 'path';

const GLOBAL_MS = 10000;
const STDIN_MS = 3000;
const L1_MAX_DIRTY = 10;
const L2_WARN_DIRTY = 20;

const globalTimer = setTimeout(() => {
  console.error('[smart-post-hook] 全局超时，强制退出');
  process.exit(0);
}, GLOBAL_MS);

function getProjectRoot() {
  try {
    return execSync('git rev-parse --show-toplevel', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
    }).trim().replace(/\\/g, '/');
  } catch {
    return process.cwd().replace(/\\/g, '/');
  }
}

function safeExec(cmd, opts = {}) {
  try {
    return execSync(cmd, {
      encoding: 'utf-8',
      stdio: 'pipe',
      timeout: 8000,
      killSignal: 'SIGKILL',
      ...opts,
    }).trim();
  } catch {
    return null;
  }
}

function findNodeProjectRoot(filePath) {
  const parts = filePath.replace(/\\/g, '/').split('/');
  for (let i = parts.length - 1; i >= 1; i--) {
    const candidate = parts.slice(0, i).join('/');
    if (existsSync(`${candidate}/node_modules/.bin`)) return candidate;
  }
  return null;
}

async function readStdin(ms = STDIN_MS) {
  return Promise.race([
    (async () => {
      const chunks = [];
      for await (const c of process.stdin) chunks.push(c);
      return Buffer.concat(chunks).toString('utf-8');
    })(),
    new Promise((_, reject) => setTimeout(() => reject(new Error('stdin timeout')), ms)),
  ]);
}

try {
  let input = {};
  try {
    const raw = await readStdin();
    if (raw.trim()) input = JSON.parse(raw);
  } catch {
    process.exit(0);
  }

  const filePath = (
    input?.tool_input?.file_path
    || input?.file_path
    || ''
  ).replace(/\\/g, '/');

  if (!filePath || /\.claude\//.test(filePath)) {
    process.exit(0);
  }

  const repoRoot = getProjectRoot();
  const porcelain = safeExec('git status --porcelain', { cwd: repoRoot, timeout: 2000 });
  const dirtyCount = porcelain ? porcelain.split('\n').filter(Boolean).length : 0;

  if (dirtyCount > L2_WARN_DIRTY) {
    console.error(`[smart-post-hook] L2: ${dirtyCount} 个未提交文件，已跳过 lint（建议 stash/commit）`);
    process.exit(0);
  }

  if (dirtyCount > 0) {
    console.error(`[smart-post-hook] ℹ️ 当前 ${dirtyCount} 个未提交文件`);
  }

  const CODE_RE = /\.(ts|tsx|js|jsx|vue|svelte)$/;
  const isFrontendSrc = /^jnpf-web-vue3\//.test(filePath) && CODE_RE.test(filePath);

  if (!isFrontendSrc || dirtyCount > L1_MAX_DIRTY) {
    if (isFrontendSrc && dirtyCount > L1_MAX_DIRTY) {
      console.error(`[smart-post-hook] L1 跳过: 未提交文件 ${dirtyCount} > ${L1_MAX_DIRTY}`);
    }
    process.exit(0);
  }

  const nodeRoot = findNodeProjectRoot(filePath);
  if (!nodeRoot) process.exit(0);

  const isWin = process.platform === 'win32';
  const eslintBin = join(nodeRoot, 'node_modules', '.bin', isWin ? 'eslint.cmd' : 'eslint');
  if (!existsSync(eslintBin)) process.exit(0);

  const absPath = /^([A-Za-z]:|\/)/.test(filePath)
    ? filePath
    : join(repoRoot, filePath).replace(/\\/g, '/');

  console.error(`[smart-post-hook] L1: eslint --fix ${filePath}`);
  safeExec(`"${eslintBin}" --fix "${absPath}"`, { cwd: nodeRoot, timeout: 8000 });

  process.exit(0);
} catch (e) {
  console.error('[smart-post-hook]', e.message);
  process.exit(0);
} finally {
  clearTimeout(globalTimer);
}
