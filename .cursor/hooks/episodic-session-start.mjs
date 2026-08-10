#!/usr/bin/env node
/**
 * Cursor sessionStart: 宪法级最高约束注入（对抗衰减）
 * episodic 自动同步已停用（2026-08-08）— 不再 spawn sync / 不再注入 EPISODIC 块
 */
import path from 'path';
import { fileURLToPath } from 'url';
import { readSessionDigest, getProjectRoot, readLatestArchiveBanner } from './session-archive-lib.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = getProjectRoot();

const pendingDigest = readSessionDigest(repoRoot);
const digestBlock = pendingDigest?.archiveStatus === 'pending'
  ? [
    '',
    '<SESSION-ARCHIVE-PENDING>',
    `上会话 (${pendingDigest.date}) 有 ${pendingDigest.codeFilesChanged} 个代码文件变更，结构化归档未完成。`,
    `主题：${pendingDigest.topic || '待补全'}`,
    '**本会话第一轮回复前 SHOULD：**',
    '1. 读 `.cursor/CURRENT-FOCUS.md` + progress-registry session_log[0]（hook 已自动写）',
    '2. 可选：润色 mistake-log 根因/修复语义（机器已写 M0xx 占位）',
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
  digestBlock,
  bannerBlock,
].join('\n');

process.stdout.write(JSON.stringify({ additional_context: context }));
process.exit(0);
