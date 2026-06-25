#!/usr/bin/env node
/**
 * Stop Hook 前置校验 — 错题本强制验证 (JNPF v5.2)
 *
 * 在 guard-finish.mjs 执行前调用。检查逻辑：
 *   1. git diff --stat HEAD 检测是否有代码变更（排除 .md/.json 等纯文档）
 *   2. 读取 mistake-log.md，检查是否有今天日期的条目
 *   3. 有代码变更 && 无今日条目 → BLOCK
 *
 * 预算：≤ 5s。git 不可用 → 保守放行。
 */

import { execSync } from 'child_process';
import { existsSync, readFileSync } from 'fs';

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

try {
  // ─── 1. 检测代码变更 ──────────────────────────────────────────────
  let hasCodeChanges = false;

  try {
    const diff = execSync('git diff --stat HEAD', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 5000,
    }).trim();

    if (diff) {
      // 过滤纯文档/配置文件变更（.md, .json, .png 等不触发错题本检查）
      const codeLines = diff.split('\n').filter(line => {
        const file = line.split('|')[0]?.trim() || '';
        // 仅跳过纯文档/证据/配置/摘要文件
        if (/\.claude[\\/]evidence[\\/]/.test(file)) return false;
        if (/\.claude[\\/]memory[\\/]session-summaries[\\/]/.test(file)) return false;
        if (/^tc-result\.txt$/.test(file)) return false;
        if (/\.(md|json|png|jpg|jpeg)$/i.test(file)) {
          // .md/.json 在非 .claude/rules/, non-CLAUDE.md 路径下视为文档
          if (!/CLAUDE\.md$/i.test(file) && !/\.claude[\\/]rules[\\/]/.test(file)) {
            return false;
          }
        }
        return true;
      });

      if (codeLines.length > 0) {
        hasCodeChanges = true;
        console.error(`[verify-mistake-log] 检测到 ${codeLines.length} 个代码文件变更`);
      }
    }
  } catch (e) {
    // git 不可用 → 保守放行
    console.error(`[verify-mistake-log] ⚠️ git diff 失败: ${e.message?.slice(0, 100)}`);
    console.log('PASS'); // 放行
    process.exit(0);
  }

  if (!hasCodeChanges) {
    console.error('[verify-mistake-log] ✅ 无代码变更，跳过错题本检查');
    console.log('PASS');
    process.exit(0);
  }

  // ─── 2. 检查错题本今日条目 ─────────────────────────────────────────
  const mistakeLogPath = `${ROOT}/.claude/memory/mistake-log.md`;

  if (!existsSync(mistakeLogPath)) {
    console.log(JSON.stringify({
      decision: 'block',
      reason: '⛔ 本会话有代码变更但 .claude/memory/mistake-log.md 不存在。\n'
        + '请在 todo_write 中补充 📝错题本次条目，格式：日期 | 类别 | 症状 | 根因 | 修复 | 关键词。',
    }));
    process.exit(0);
  }

  const content = readFileSync(mistakeLogPath, 'utf-8');
  const today = new Date();
  const dateStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

  // 检查是否有今日日期标记的条目
  const hasTodayEntry = content.includes(`## ${dateStr}`);

  if (!hasTodayEntry) {
    console.log(JSON.stringify({
      decision: 'block',
      reason: `⛔ 本会话有代码变更但错题本中无今日 (${dateStr}) 条目。\n`
        + '请在 todo_write 中将 📝错题本次 标记 completed，并确保 .claude/memory/mistake-log.md 中有今日日期的新条目。\n'
        + '格式：### Mxxx | 类别 | 症状\n- **症状**：...\n- **根因**：...\n- **修复**：...\n- **关键词**：...',
    }));
    process.exit(0);
  }

  console.error(`[verify-mistake-log] ✅ 错题本有今日 (${dateStr}) 条目`);
  console.log('PASS');
  process.exit(0);

} catch (e) {
  // 未预期错误 → 放行
  console.error(`[verify-mistake-log] ⚠️ 异常: ${e.message}`);
  console.log('PASS');
  process.exit(0);
}
