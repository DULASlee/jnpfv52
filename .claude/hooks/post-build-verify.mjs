#!/usr/bin/env node
/**
 * PostToolUse Hook — Phase 5 Verify 强制阻断 (JNPF v5.2)
 *
 * 状态机：idle → build_done → test_done → idle
 * - Bash 检测到 Build succeeded / Compilation succeeded → 设 buildFlag
 * - Bash/Playwright/Agent(test-runner) 检测到测试 → 清除 buildFlag
 * - PreToolUse(Bash) 时 buildFlag 存在且超 30min → BLOCK
 *
 * 预算：≤ 2s。失败静默放行，不阻断。
 */

import { execSync } from 'child_process';
import { existsSync, readFileSync, writeFileSync, unlinkSync } from 'fs';
import { join } from 'path';

// ─── 项目根目录解析 ─────────────────────────────────────────────
function getProjectRoot() {
  try {
    return execSync('git rev-parse --show-toplevel', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
    }).trim().replace(/\\/g, '/');
  } catch { /* fall through */ }
  let dir = process.cwd();
  for (let i = 0; i < 5; i++) {
    if (existsSync(`${dir}/CLAUDE.md`)) return dir.replace(/\\/g, '/');
    const parent = dir.replace(/[/\\][^/\\]+$/, '');
    if (parent === dir) break;
    dir = parent;
  }
  return process.cwd().replace(/\\/g, '/');
}
const ROOT = getProjectRoot();

// ─── 配置 ──────────────────────────────────────────────────────────
const FLAG_FILE = join(ROOT, '.claude', '.build-verify-flag.json');
const MAX_BUILD_AGE_MS = 30 * 60 * 1000; // 30 分钟

try {
  const input = JSON.parse(
    Buffer.concat(await (async () => {
      const chunks = [];
      for await (const chunk of process.stdin) chunks.push(chunk);
      return chunks;
    })()).toString('utf-8') || '{}',
  );

  const { tool_name, tool_input } = input;

  // ─── 1. 检查阶段：PreToolUse 拦截 ─────────────────────────────────
  if (input.hook_event === 'PreToolUse') {
    if (!existsSync(FLAG_FILE)) {
      console.log(JSON.stringify({ decision: 'approve' }));
      process.exit(0);
    }

    try {
      const flag = JSON.parse(readFileSync(FLAG_FILE, 'utf-8'));
      const age = Date.now() - flag.timestamp;

      if (age > MAX_BUILD_AGE_MS) {
        // 超时 → BLOCK
        console.log(JSON.stringify({
          decision: 'block',
          reason: `❌ 构建完成已 ${Math.round(age / 60000)} 分钟，但未运行验证。\n`
            + `请执行 Phase 5: Verify（SP: verification-before-completion）。\n`
            + `  - 后端: dotnet build + 确认 0 errors\n`
            + `  - 前端: vue-tsc --noEmit\n`
            + `  - E2E: 调用 playwright 技能产出截图至 .claude/evidence/\n`
            + `禁止继续后续任务。验证通过后此阻断自动解除。`,
        }));
        process.exit(0);
      }

      // 未超时 → 检查是否是测试命令
      if (tool_name === 'Bash') {
        const cmd = tool_input?.command || '';
        const isTest = /\b(vue-tsc|dotnet\s+test|playwright|npm\s+test|pnpm\s+test|npx\s+playwright|jest|vitest)\b/i.test(cmd);
        if (isTest) {
          // 检测到测试命令 → 清除标志
          try { unlinkSync(FLAG_FILE); } catch { /* ok */ }
          console.error('[post-build-verify] ✅ 检测到测试命令，清除 build 标志');
        }
      }
    } catch (e) {
      // flag 文件损坏 → 清除
      try { unlinkSync(FLAG_FILE); } catch { /* ok */ }
    }

    console.log(JSON.stringify({ decision: 'approve' }));
    process.exit(0);
  }

  // ─── 2. 设置阶段：PostToolUse 检测 build 成功 ────────────────────
  if (input.hook_event === 'PostToolUse') {
    if (tool_name !== 'Bash') {
      console.log(JSON.stringify({ decision: 'approve' }));
      process.exit(0);
    }

    const stdout = (tool_input?.result?.stdout || '').toString();
    const exitCode = tool_input?.result?.exit_code;

    // 检测 Build succeeded / Compilation succeeded（退出码 0 且输出含 success 标记）
    const buildSucceeded = exitCode === 0 && /\b(Build succeeded|Compilation succeeded)\b/i.test(stdout);

    if (buildSucceeded) {
      const flag = {
        timestamp: Date.now(),
        command: (tool_input?.command || '').slice(0, 200),
      };
      try {
        writeFileSync(FLAG_FILE, JSON.stringify(flag), 'utf-8');
        console.error('[post-build-verify] ⚡ Build 成功，设置验证标志。30 分钟内必须执行测试。');
      } catch { /* disk full 等 → 静默失败 */ }
    }

    console.log(JSON.stringify({ decision: 'approve' }));
    process.exit(0);
  }

  // 其他 hook 事件 → 放行
  console.log(JSON.stringify({ decision: 'approve' }));
  process.exit(0);

} catch (e) {
  // 任何未预期错误 → 静默放行，不阻断
  console.error(`[post-build-verify] ⚠️ 异常: ${e.message}`);
  console.log(JSON.stringify({ decision: 'approve' }));
  process.exit(0);
}
