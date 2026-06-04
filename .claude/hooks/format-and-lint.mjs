#!/usr/bin/env node
/**
 * PostToolUse Hook — 格式化 + Lint
 * Write → Prettier + ESLint | Edit → 仅 ESLint（不破坏后续 Edit 上下文）
 * 性能预算：≤ 5s | 无状态
 */
import { execSync } from 'child_process';

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

// Detect project root with node_modules
function findProjectRoot(filePath) {
  const parts = filePath.replace(/\\/g, '/').split('/');
  for (let i = parts.length - 1; i >= 1; i--) {
    const candidate = parts.slice(0, i).join('/');
    try {
      execSync(`test -d "${candidate}/node_modules/.bin"`, { stdio: 'pipe', timeout: 1000 });
      return candidate;
    } catch {}
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

// 所有操作 → ESLint
try {
  execSync(`${BIN('eslint')} --fix --max-warnings 0 "${filePath}"`, {
    stdio: 'pipe', timeout: 5000, killSignal: 'SIGKILL',
  });
} catch (e) {
  const output = e.stdout?.toString() || e.stderr?.toString() || '';
  console.error(`ESLint errors in ${filePath}:\n${output}`);
  console.log(JSON.stringify({
    decision: 'block',
    reason: `ESLint violations found in ${filePath}. Review the errors above and fix them.`,
  }));
  process.exit(0);
}

process.exit(0);
