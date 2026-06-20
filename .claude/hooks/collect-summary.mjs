#!/usr/bin/env node
/**
 * Stop Hook — 会话变更摘要收集器 (JNPF v5.2)
 *
 * 职责：AI 会话结束时，收集未提交的变更摘要，保存为 Markdown。
 * 输出：.claude/memory/session-summaries/{date}-{shortId}.md
 *
 * v2 增强：
 *   - 读取 .claude/memory/session-key-points.md（AI 在 Step 7 写入的决策/根因）
 *   - 合并到会话摘要中，记录"为什么改"而不只是"改了什么"
 *   - 读取后自动清空 key-points 文件
 *
 * 分类：后端 / 前端 / 配置 / 基础设施 / Hooks / 文档 / 其他
 * 预算：≤ 5 秒，失败静默跳过（不阻断会话退出）。
 */

import { execSync } from 'child_process';
import { mkdirSync, writeFileSync, existsSync, readFileSync, unlinkSync } from 'fs';
import { join } from 'path';

// ─── 读取 stdin ──────────────────────────────────────────────────
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

// 用户手动打断 → 跳过收集
if (input.stop_reason === 'user_interrupt') {
  console.log(JSON.stringify({ decision: 'approve', reason: 'User interrupted' }));
  process.exit(0);
}

try {
  // ─── 收集三种未提交变更 ────────────────────────────────────────
  let diffOutput = '', stagedOutput = '', untrackedOutput = '';
  try { diffOutput = execSync('git diff --name-only', { encoding: 'utf-8', stdio: 'pipe', timeout: 5000 }).trim(); } catch { /* skip */ }
  try { stagedOutput = execSync('git diff --name-only --cached', { encoding: 'utf-8', stdio: 'pipe', timeout: 5000 }).trim(); } catch { /* skip */ }
  try { untrackedOutput = execSync('git ls-files --others --exclude-standard', { encoding: 'utf-8', stdio: 'pipe', timeout: 5000 }).trim(); } catch { /* skip */ }

  const allFiles = [diffOutput, stagedOutput, untrackedOutput]
    .filter(Boolean)
    .join('\n');

  // ─── 读取 AI 写入的决策/根因 ──────────────────────────────────
  const keyPointsPath = join('.claude', 'memory', 'session-key-points.md');
  let keyPointsContent = '';
  if (existsSync(keyPointsPath)) {
    try {
      keyPointsContent = readFileSync(keyPointsPath, 'utf-8').trim();
      // 读取后清空（下个会话重新写入）
      unlinkSync(keyPointsPath);
    } catch { /* skip */ }
  }

  // 无文件变更且无关键决策 → 跳过
  if (!allFiles && !keyPointsContent) {
    console.log(JSON.stringify({ decision: 'approve', reason: 'No uncommitted changes' }));
    process.exit(0);
  }

  const files = allFiles ? [...new Set(allFiles.split('\n').filter(Boolean))] : [];

  // ─── 分类函数 ──────────────────────────────────────────────────
  const isBackend = f =>
    /\.(cs|csproj|sln)$/.test(f) && f.includes('backend');
  const isFrontend = f =>
    /\.(vue|ts|tsx|js|jsx|less|scss)$/.test(f);
  const isConfig = f =>
    /\.(json|yml|yaml|toml|env)/.test(f) && !f.includes('backend');
  const isInfra = f =>
    /(Dockerfile|Containerfile|docker-compose|\.github\/|Makefile|CMakeLists)/i.test(f);
  const isHooks = f =>
    f.includes('.claude/');
  const isDocs = f =>
    /\.(md|txt|rst|adoc)$/.test(f);

  const categories = {
    backend: files.filter(isBackend),
    frontend: files.filter(isFrontend),
    config: files.filter(isConfig),
    infra: files.filter(isInfra),
    hooks: files.filter(isHooks),
    docs: files.filter(isDocs),
    other: files.filter(f =>
      !isBackend(f) && !isFrontend(f) && !isConfig(f) &&
      !isInfra(f) && !isHooks(f) && !isDocs(f)
    ),
  };

  // ─── 读取最近 commit 信息 ──────────────────────────────────────
  let recentCommit = '';
  try {
    recentCommit = execSync('git log -1 --format="%s%n%b"', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
    }).trim();
  } catch { /* skip */ }

  // ─── 生成摘要 ──────────────────────────────────────────────────
  const now = new Date();
  const dateStr = now.toISOString().slice(0, 10);
  const timeStr = now.toTimeString().slice(0, 8);
  const shortId = (now.getTime().toString(36).slice(-6)) + '-' + (process.pid.toString(36).slice(-3));

  let summary = '# 会话变更摘要\n\n';
  summary += `**时间**: ${dateStr} ${timeStr}\n`;
  summary += `**变更文件数**: ${files.length}\n\n`;

  // ── 决策与根因（AI 在 Step 7 写入）──
  if (keyPointsContent) {
    summary += '## 🔑 关键决策与根因\n\n';
    summary += keyPointsContent + '\n\n';
  }

  // ── 最近提交 ──
  if (recentCommit) {
    summary += '## 📝 最近提交\n\n';
    summary += '```\n' + recentCommit + '\n```\n\n';
  }

  // ── 文件分类 ──
  if (files.length > 0) {
    const sectionMap = {
      backend: '后端代码',
      frontend: '前端代码',
      config: '配置文件',
      infra: '基础设施',
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
  }

  // ─── 写入文件 ──────────────────────────────────────────────────
  const summariesDir = join('.claude', 'memory', 'session-summaries');
  mkdirSync(summariesDir, { recursive: true });

  const filename = `${dateStr}-${shortId}.md`;
  writeFileSync(join(summariesDir, filename), summary, 'utf-8');

  const kbSize = (Buffer.byteLength(summary, 'utf-8') / 1024).toFixed(1);
  console.error(`📝 会话摘要已保存: ${summariesDir}/${filename} (${files.length} 个文件, ${kbSize}KB)`);
  if (keyPointsContent) console.error('   ✅ 已收录 AI 决策/根因');
} catch (e) {
  // 静默跳过，不阻断停止流程
  console.error('⚠️ 会话摘要收集跳过: ' + (e.message || ''));
}

console.log(JSON.stringify({
  decision: 'approve',
  reason: 'Session summary collected',
}));

process.exit(0);
