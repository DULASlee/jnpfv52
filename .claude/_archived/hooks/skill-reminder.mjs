#!/usr/bin/env node
/**
 * PostToolUse Hook — 技能提醒 (JNPF v5.2)
 *
 * 代码变更后，向 AI 注入流程提醒（纯提示，不强制）。
 * 仅触发于重度变更（3+ 文件 或 50+ 行）。
 *
 * 预算：≤ 2s。失败静默跳过，不阻断。
 */

import { execSync } from 'child_process';

const MAX_DIRTY_FILES = 20;

// ─── 收集变更 ────────────────────────────────────────────────────
let changedFiles = [];
let lineCount = 0;

try {
  const porcelain = execSync('git status --porcelain', {
    encoding: 'utf-8', stdio: 'pipe', timeout: 2000,
  }).trim();
  const dirtyCount = porcelain ? porcelain.split('\n').filter(Boolean).length : 0;
  if (dirtyCount > MAX_DIRTY_FILES) {
    console.log(JSON.stringify({ decision: 'approve', reason: `skip: ${dirtyCount} dirty files` }));
    process.exit(0);
  }
} catch { /* git 不可用，继续 */ }

try {
  const unstaged = execSync('git diff --name-only', {
    encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
  }).trim();
  changedFiles = unstaged.split('\n').filter(Boolean);
} catch { /* git 不可用，跳过 */ }

if (changedFiles.length === 0) {
  console.log(JSON.stringify({ decision: 'approve' }));
  process.exit(0);
}

// 行数统计
try {
  const diff = execSync('git diff --unified=0', {
    encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
  });
  const added = (diff.match(/^\+[^+]/gm) || []).length;
  const removed = (diff.match(/^\-[^\-]/gm) || []).length;
  lineCount = added + removed;
} catch { lineCount = changedFiles.length * 10; }

// ─── 阈值判断 ────────────────────────────────────────────────────
const isHeavy = changedFiles.length >= 3 || lineCount >= 50;
if (!isHeavy) {
  console.log(JSON.stringify({ decision: 'approve' }));
  process.exit(0);
}

// ─── 变更类型 ────────────────────────────────────────────────────
const hasBackend = changedFiles.some(f => /\.(cs|csproj)$/.test(f));
const hasFrontend = changedFiles.some(f => /\.(vue|ts|tsx|less|css)$/.test(f) && /^jnpf-web/.test(f));

// ─── 纯提示（非强制）───────────────────────────────────────────────
const systemMessage = `[INFO] 重度变更: ${changedFiles.length} 文件, ~${lineCount} 行 (backend=${hasBackend}, frontend=${hasFrontend})。
💡 SP 流水线提醒: Phase 2 Brainstorm → Phase 5 Verify (playwright) → Phase 6 Review (requesting-code-review)。
⛔ 强制阻断由 post-build-verify.mjs (build→test) 和 guard-finish.mjs (E2E+错题本) 实现。本消息仅为友情提示。`;

console.log(JSON.stringify({ decision: 'approve', systemMessage }));
process.exit(0);
