#!/usr/bin/env node
/**
 * Cursor sessionStart: episodic sync + 宪法级最高约束注入（对抗衰减）
 */
import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import { loadManifest } from '../../scripts/toolchain-lib.mjs';
import { readSessionDigest, getProjectRoot, readLatestArchiveBanner } from './session-archive-lib.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = getProjectRoot();
const syncScript = path.join(repoRoot, 'scripts', 'episodic-sync.mjs');
const { episodic_project_id: projectId, project_slug: slug, docs } = loadManifest(repoRoot);

spawn(process.execPath, [syncScript, '--background'], {
  detached: true,
  stdio: 'ignore',
  windowsHide: true,
}).unref();

const pendingDigest = readSessionDigest(repoRoot);
const digestBlock = pendingDigest?.archiveStatus === 'pending'
  ? [
    '',
    '<SESSION-ARCHIVE-PENDING>',
    `上会话 (${pendingDigest.date}) 有 ${pendingDigest.codeFilesChanged} 个代码文件变更，结构化归档未完成。`,
    `主题：${pendingDigest.topic || '待补全'}`,
    '**本会话第一轮回复前 SHOULD：**',
    '1. MCP episodic-memory search/read 回忆上会话（只读）',
    '2. 读 `.cursor/CURRENT-FOCUS.md` + progress-registry session_log[0]（hook 已自动写）',
    '3. 可选：润色 mistake-log 根因/修复语义（机器已写 M0xx 占位）',
    `digest: .claude/memory/session-digest/latest.json`,
    '</SESSION-ARCHIVE-PENDING>',
  ].join('\n')
  : pendingDigest?.archiveStatus === 'complete'
    ? `\n<SESSION-ARCHIVE-OK>上会话 (${pendingDigest.date}) 归档已齐：${pendingDigest.topic || '—'}</SESSION-ARCHIVE-OK>`
    : '';

const latestBanner = readLatestArchiveBanner(repoRoot);
const bannerBlock = latestBanner
  ? [
    '',
    '<ARCHIVE-BANNER-PENDING>',
    '上会话归档已完成。若用户首条消息为任务指令（非「已阅」），可在该轮助手回复**末尾**原样追加：',
    '`.cursor/episodic/last-archive-banner.txt` 全部内容。',
    '</ARCHIVE-BANNER-PENDING>',
  ].join('\n')
  : '';

const context = [
  '<CONSTITUTION-PRIORITY>',
  '最高约束（先于一切实现）：',
  '1. Q1–Q3 业务锚定；答不出禁止编码。',
  '2. S/A：workflow-state adfPhase=P0→P3 禁止写业务源码（L12）；用户「继续」后升到 P4 才实现。B级 adfPhase=exempt。',
  '3. 四支柱①硬门：可审批前写 pillar-claim-current.json + pillar-claim-check.mjs --force。',
  '4. 零占位符硬拦；唯一 alwaysApply=.cursor/rules/00-constitution.mdc。',
  '详规入口：.cursor/rules/00-constitution.mdc',
  '</CONSTITUTION-PRIORITY>',
  '',
  '<EPISODIC-MEMORY-AUTOMATION>',
  `本项目 episodic-memory 已启用（project=${projectId}，slug=${slug}）。sessionStart/stop 触发 sync + 结构化归档。`,
  '',
  '**写入 vs 读取（勿混淆）：**',
  '- MCP `search`/`read` = 查已索引对话（只读 API）',
  '- **写入** = stop hook → `scripts/episodic-sync.mjs` 桥接 Cursor jsonl → episodic CLI `index`',
    '- **结构化进度** = stop hook → `session-archive-stop.mjs` → **机器自动写** CURRENT-FOCUS / progress-registry / mistake-log + session-digest',
  '',
  '**会话开始必做（第一轮流式回复前）**：',
  `1. MCP episodic-memory \`search\`：project=\`${projectId}\`，query 见 \`.cursor/episodic/search-templates.yaml\``,
  '2. 对 top 2-3 命中用 `read` 只读相关行段',
  '3. 读推进清单待审项 + 相关 `openspec/specs/`',
  '4. 非 trivial 任务走 Superpowers（brainstorming → writing-plans → executing-plans）',
  '',
  '**阶段完成**：verification → progress-registry + 推进清单 LOG → 定稿写入 openspec/specs/',
  '',
  `Playbook: ${docs?.playbook || 'docs/toolchain/SETUP.md'}`,
  'Manifest: .cursor/toolchain.manifest.json',
  '</EPISODIC-MEMORY-AUTOMATION>',
  digestBlock,
  bannerBlock,
].join('\n');

process.stdout.write(JSON.stringify({ additional_context: context }));
process.exit(0);
