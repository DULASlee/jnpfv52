#!/usr/bin/env node
/**
 * SessionStart Hook — 错题本自动加载 (JNPF v5.2)
 *
 * 读取 .claude/memory/mistake-log.md，提取最近 30 天的错误记录，
 * 注入到 systemMessage 中，AI 在编码前自动获取历史教训。
 *
 * 预算：≤ 2s，失败静默跳过。
 */

import { readFileSync, existsSync } from 'fs';
import { join } from 'path';
import { execSync } from 'child_process';

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
  const mistakePath = join(ROOT, '.claude', 'memory', 'mistake-log.md');
  if (!existsSync(mistakePath)) {
    console.log(JSON.stringify({ decision: 'approve' }));
    process.exit(0);
  }

  const content = readFileSync(mistakePath, 'utf-8');
  const lines = content.split('\n');

  // ─── 提取最近 30 天的错误 ────────────────────────────────────
  const now = Date.now();
  const MAX_AGE = 30 * 24 * 60 * 60 * 1000;
  const recentMistakes = [];
  let currentDate = '';
  let currentEntry = '';

  for (const line of lines) {
    // 日期标题：## 2026-06-20
    const dateMatch = line.match(/^## (\d{4}-\d{2}-\d{2})/);
    if (dateMatch) {
      // 保存上一条
      if (currentEntry.trim()) {
        const age = now - new Date(currentDate).getTime();
        if (age <= MAX_AGE) recentMistakes.push(currentEntry.trim());
      }
      currentDate = dateMatch[1];
      currentEntry = '';
      continue;
    }

    // M 编号条目：### M001 | ...
    if (line.startsWith('### M')) {
      if (currentEntry.trim()) {
        const age = now - new Date(currentDate).getTime();
        if (age <= MAX_AGE) recentMistakes.push(currentEntry.trim());
      }
      currentEntry = line + '\n';
      continue;
    }

    // 继续累积当前条目
    if (currentEntry && line.startsWith('- **')) {
      currentEntry += line + '\n';
    }
  }

  // 最后一条
  if (currentEntry.trim()) {
    const age = now - new Date(currentDate).getTime();
    if (age <= MAX_AGE) recentMistakes.push(currentEntry.trim());
  }

  if (recentMistakes.length === 0) {
    console.log(JSON.stringify({ decision: 'approve' }));
    process.exit(0);
  }

  // ─── 构建提醒消息 ──────────────────────────────────────────
  const summary = recentMistakes
    .map(m => {
      const lines = m.split('\n');
      return lines[0]; // 只取标题行
    })
    .join('\n  ');

  const systemMessage = `📖 **错题本提醒** — 最近 30 天记录了 ${recentMistakes.length} 条错误，AI 编码前应主动避免：

  ${summary}

⚠️ 编码前 MUST 用 Grep 搜索 \`.claude/memory/mistake-log.md\` 匹配当前任务关键词。
新错误发现后 MUST 立即追加到此文件。`;

  console.log(JSON.stringify({
    decision: 'approve',
    systemMessage,
  }));

} catch {
  // 静默跳过
  console.log(JSON.stringify({ decision: 'approve' }));
}

process.exit(0);
