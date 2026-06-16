#!/usr/bin/env node
/**
 * PostToolUse Hook — 格式化 + Lint
 * Write → Prettier + ESLint | Edit → 仅 ESLint（不破坏后续 Edit 上下文）
 * 性能预算：≤ 5s | 无状态
 */
import { execSync } from 'child_process';
import { existsSync } from 'fs';

let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

const filePath = process.env.CLAUDE_FILE_PATH
  || input.tool_input?.file_path || '';

const toolName = process.env.CLAUDE_TOOL_NAME
  || input.tool_name || '';

const CODE_RE = /\.(ts|tsx|js|jsx|mjs|cjs|vue|svelte)$/;
if (!filePath || !CODE_RE.test(filePath)) process.exit(0);

// Detect project root with node_modules（跨平台，用 Node.js API 替代 test -d）
function findProjectRoot(filePath) {
  const parts = filePath.replace(/\\/g, '/').split('/');
  for (let i = parts.length - 1; i >= 1; i--) {
    const candidate = parts.slice(0, i).join('/');
    if (existsSync(`${candidate}/node_modules/.bin`)) {
      return candidate;
    }
  }
  return null;
}

const projectRoot = findProjectRoot(filePath);
if (!projectRoot) process.exit(0);

const BIN = (n) => `${projectRoot}/node_modules/.bin/${n}`;

// Write → Prettier + ESLint
if (toolName === 'Write') {
  try {
    execSync(`${BIN('prettier')} --write "${filePath}"`, {
      stdio: 'pipe', timeout: 2000, killSignal: 'SIGKILL',
    });
  } catch {}
}

// 所有操作 → ESLint（放宽 warning 阈值，只阻断 error）
try {
  execSync(`${BIN('eslint')} --fix "${filePath}"`, {
    stdio: 'pipe', timeout: 5000, killSignal: 'SIGKILL',
  });
} catch (e) {
  const output = e.stdout?.toString() || e.stderr?.toString() || '';
  // 只在有 error 时阻断，warning 不阻断（开发中 warning 很常见）
  if (/error/i.test(output)) {
    console.error(`ESLint errors in ${filePath}:\n${output}`);
    console.log(JSON.stringify({
      decision: 'block',
      reason: `ESLint errors found in ${filePath}. Fix the errors above.`,
    }));
    process.exit(0);
  }
  // 仅有 warning，放行
}

process.exit(0);
