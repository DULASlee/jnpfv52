#!/usr/bin/env node
/**
 * Stop Hook — 会话变更摘要收集器
 *
 * 职责：AI 会话结束时，收集未提交的变更摘要，保存为 Markdown。
 * 位置：.claude/memory/session-summaries/{date}-{short-id}.md
 *
 * 策略：只收集 git diff（未提交变更），不依赖 HEAD~1。
 * 预算：≤ 5 秒，失败静默跳过。
 */

import { execSync } from 'child_process';
import { mkdirSync, writeFileSync, existsSync } from 'fs';
import { join } from 'path';

// ─── 读取 stdin（Windows 兼容）────────────────────────────────
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch {
  input = {};
}

// 人类手动打断 → 跳过收集
if (input.stop_reason === 'user_interrupt') {
  console.log(JSON.stringify({ decision: 'approve', reason: 'User interrupted' }));
  process.exit(0);
}

try {
  // ─── 收集未提交变更 ─────────────────────────────────────────
  const diffOutput = execSync('git diff --name-only', {
    encoding: 'utf-8',
    stdio: 'pipe',
    timeout: 5000,
  }).trim();

  const stagedOutput = execSync('git diff --name-only --cached', {
    encoding: 'utf-8',
    stdio: 'pipe',
    timeout: 5000,
  }).trim();

  const untrackedOutput = execSync('git ls-files --others --exclude-standard', {
    encoding: 'utf-8',
    stdio: 'pipe',
    timeout: 5000,
  }).trim();

  const allFiles = [diffOutput, stagedOutput, untrackedOutput]
    .filter(Boolean)
    .join('\n');

  if (!allFiles) {
    console.log(JSON.stringify({ decision: 'approve', reason: 'No uncommitted changes' }));
    process.exit(0);
  }

  const files = [...new Set(allFiles.split('\n').filter(Boolean))];

  // ─── 分类文件（修复 TDZ bug：提取为独立函数，避免对象字面量内自引用）─────────
  const isBackend = f => /\.(cs|csproj|sln|json)$/.test(f) && f.includes('backend');
  const isFrontend = f => /\.(vue|ts|tsx|js|jsx|less|scss)$/.test(f);
  const isConfig = f => /\.(json|yml|yaml|toml|env)/.test(f) && !f.includes('backend');
  const isHooks = f => f.includes('.claude/');
  const isDocs = f => /\.(md|txt)$/.test(f);

  const categories = {
    backend: files.filter(isBackend),
    frontend: files.filter(isFrontend),
    config: files.filter(isConfig),
    hooks: files.filter(isHooks),
    docs: files.filter(isDocs),
    other: files.filter(f => !isBackend(f) && !isFrontend(f) && !isConfig(f) && !isHooks(f) && !isDocs(f)),
  };

  // ─── 生成摘要 ─────────────────────────────────────────────
  const now = new Date();
  const dateStr = now.toISOString().slice(0, 10);
  const timeStr = now.toTimeString().slice(0, 8);
  const shortId = now.getTime().toString(36).slice(-6);

  let summary = `# 会话变更摘要\n\n`;
  summary += `**时间**: ${dateStr} ${timeStr}\n`;
  summary += `**变更文件数**: ${files.length}\n\n`;

  const sectionMap = {
    backend: '后端代码',
    frontend: '前端代码',
    config: '配置文件',
    hooks: 'Hooks/规则',
    docs: '文档',
    other: '其他',
  };

  for (const [key, label] of Object.entries(sectionMap)) {
    if (categories[key].length > 0) {
      summary += `## ${label} (${categories[key].length})\n\n`;
      for (const f of categories[key]) {
        summary += `- \`${f}\`\n`;
      }
      summary += '\n';
    }
  }

  // ─── 写入文件 ─────────────────────────────────────────────
  const summariesDir = join('.claude', 'memory', 'session-summaries');
  mkdirSync(summariesDir, { recursive: true });

  const filename = `${dateStr}-${shortId}.md`;
  writeFileSync(join(summariesDir, filename), summary, 'utf-8');

  console.error(`📝 会话摘要已保存: ${summariesDir}/${filename} (${files.length} 个文件)`);
} catch {
  // 静默跳过，不阻断停止流程
  console.error('⚠️ 会话摘要收集跳过');
}

// stdout 输出 JSON（Claude Code 要求每个 command hook 都返回有效 JSON）
console.log(JSON.stringify({
  decision: 'approve',
  reason: 'Session summary collected',
}));

process.exit(0);
