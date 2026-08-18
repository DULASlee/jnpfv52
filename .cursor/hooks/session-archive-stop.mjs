#!/usr/bin/env node
/**
 * Cursor stop / sessionEnd hook — 结构化跨会话归档
 * 1. 写 session-digest/latest.json（git 变更快照）
 * 2. 代码变更 → 自动 session-summaries/*-AUTO.md 草稿
 * 3. 写 .cursor/episodic/hook-run-log.json（可观测性）
 * 4. 机器归档：CURRENT-FOCUS + progress-registry + mistake-log（deterministic，无 LLM）
 * 5. 归档仍不全且 hook=stop → followup_message
 *
 * 与 episodic-sync 分工：episodic=对话全文索引；本 hook=可读的进度/错题本/焦点
 */
import {
  applyMachineArchival,
  archivalComplete,
  buildArchiveFollowup,
  getChangedFiles,
  getProjectRoot,
  getTodayStr,
  inferTopic,
  isArchiveMetaFile,
  isCodeFile,
  writeAutoSessionSummary,
  writeHookRunLog,
  writeSessionDigest,
} from './session-archive-lib.mjs';

function readStdinWithTimeout(ms = 250) {
  return new Promise((resolve) => {
    if (process.stdin.isTTY) {
      resolve('');
      return;
    }
    const chunks = [];
    let settled = false;
    const finish = () => {
      if (settled) return;
      settled = true;
      resolve(Buffer.concat(chunks).toString('utf8'));
    };
    process.stdin.on('data', (c) => chunks.push(c));
    process.stdin.on('end', finish);
    process.stdin.on('error', finish);
    process.stdin.resume();
    setTimeout(finish, ms);
  });
}

async function readHookInput() {
  if (process.env.CURSOR_HOOK_STDIN) {
    try {
      return JSON.parse(process.env.CURSOR_HOOK_STDIN);
    } catch { /* fall through */ }
  }
  try {
    const raw = (await readStdinWithTimeout()).trim();
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

const hookEvent = process.env.CURSOR_HOOK_EVENT || 'stop';
const hookInput = await readHookInput();
const root = getProjectRoot();
const today = getTodayStr();
const changed = getChangedFiles(root);
const codeChanged = changed.filter(isCodeFile);
const metaOnly = changed.length > 0 && changed.every(isArchiveMetaFile);

const checks = archivalComplete(root, today);
const digest = {
  date: today,
  endedAt: new Date().toISOString(),
  hookEvent,
  conversationId: hookInput.conversation_id || hookInput.conversationId || null,
  changedFiles: changed.slice(0, 80),
  codeFilesChanged: codeChanged.length,
  archiveStatus: checks.complete ? 'complete' : 'pending',
  archiveChecks: checks,
  topic: inferTopic(codeChanged),
  metaOnly,
};

writeSessionDigest(root, digest);

const autoSummaryPath = writeAutoSessionSummary(root, digest, codeChanged);
if (autoSummaryPath) digest.autoSummaryPath = autoSummaryPath;

let machineArchival = { applied: false, reason: 'skipped' };
if (codeChanged.length > 0) {
  machineArchival = applyMachineArchival(root, digest, codeChanged, autoSummaryPath);
  digest.machineArchival = machineArchival;
}

const finalChecks = archivalComplete(root, today);
digest.archiveChecks = finalChecks;
digest.archiveStatus = finalChecks.complete ? 'complete' : 'pending';
writeSessionDigest(root, digest);

writeHookRunLog(root, {
  at: digest.endedAt,
  hook: hookEvent,
  root,
  codeFilesChanged: codeChanged.length,
  archiveStatus: digest.archiveStatus,
  autoSummaryPath: autoSummaryPath || null,
  machineArchival,
  conversationId: digest.conversationId,
});

// sessionEnd：用户关 Chat，只落盘不 followup；stop：Agent 回合结束，仅机器归档失败时 followup
const needsFollowup = hookEvent === 'stop'
  && codeChanged.length > 0
  && !finalChecks.complete
  && !metaOnly;

if (needsFollowup) {
  process.stdout.write(JSON.stringify({
    followup_message: buildArchiveFollowup(digest, finalChecks),
  }));
} else {
  process.stdout.write(JSON.stringify({
    ok: true,
    hookEvent,
    archiveStatus: digest.archiveStatus,
    codeFilesChanged: codeChanged.length,
    autoSummaryPath: autoSummaryPath || null,
    machineArchival,
    hookRunLog: '.cursor/episodic/hook-run-log.json',
    episodicNote: '对话全文由 episodic-stop → scripts/episodic-sync.mjs 索引（MCP search 可读）',
  }));
}

process.exit(0);
