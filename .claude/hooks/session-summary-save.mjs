#!/usr/bin/env node
/**
 * SessionSummaryStop Hook — 自动保存本次会话关键产出到 ECC Memory Vault
 *
 * 触发时机:  Claude/OpenCode 会话结束时（Stop event）
 * 数据源:    .claude/memory/session-digest/latest.json (Cursor sessionEnd 已生成)
 *            .claude/memory/session-summaries/YYYY-MM-DD-*-AUTO.md (本次草稿)
 * 输出:      .ecc/memory/project/{contexts,facts,decisions,handoffs}/mem_*.md
 *
 * 设计原则:
 * 1. 无 LLM 调用 — 完全机械化，5 秒内完成，token 零开销
 * 2. 防重复 — 同 digest hash 已 save 则跳过
 * 3. 防误触发 — digest 文件缺失或 archiveStatus=pending 时只记录事件，不 save
 * 4. 单点失败 — 每条 memory 独立 try/catch，一条失败不影响其他
 */

import { execSync, execFileSync } from 'child_process';
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { createHash } from 'crypto';
import { homedir } from 'os';

// ─── 路径解析 ─────────────────────────────────────────────
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
const DIGEST = `${ROOT}/.claude/memory/session-digest/latest.json`;
const AUTO_GLOB = `${ROOT}/.claude/memory/session-summaries`;
const ECC_VAULT = `${ROOT}/.ecc/memory/project`;
const ECC_BIN = 'npx ecc memory save';

const STAMP = () => new Date().toISOString();
const LOG = (msg) => console.error(`[session-summary-save ${STAMP()}] ${msg}`);

// ─── 防抖标记 ─────────────────────────────────────────────
const DEBOUNCE = `${ROOT}/.claude/.session-summary-save.last`;
function alreadySaved(digestHash) {
  if (!existsSync(DEBOUNCE)) return false;
  try {
    const last = JSON.parse(readFileSync(DEBOUNCE, 'utf-8'));
    return last.hash === digestHash;
  } catch { return false; }
}
function markSaved(digestHash) {
  try {
    mkdirSync(dirname(DEBOUNCE), { recursive: true });
    writeFileSync(DEBOUNCE, JSON.stringify({ hash: digestHash, at: STAMP() }, null, 2));
  } catch (e) { LOG(`debounce 写入失败: ${e.message}`); }
}

// ─── 读 stdin（Stop hook 标准做法）───────────────────────
let input = {};
try {
  const raw = readFileSync(0, 'utf-8').trim();
  if (raw) input = JSON.parse(raw);
} catch { input = {}; }

// ─── 主流程 ─────────────────────────────────────────────
async function main() {
  LOG(`stop_reason=${input.stop_reason || 'unknown'} cwd=${ROOT}`);

  if (!existsSync(DIGEST)) {
    LOG(`digest 缺失 ${DIGEST} — 跳过（等待 Cursor sessionEnd 生成）`);
    process.exit(0);
  }

  let digest;
  try {
    digest = JSON.parse(readFileSync(DIGEST, 'utf-8'));
  } catch (e) {
    LOG(`digest 解析失败: ${e.message}`);
    process.exit(0);
  }

  const hash = createHash('sha256')
    .update(JSON.stringify(digest.changedFiles || []) + (digest.endedAt || ''))
    .digest('hex').slice(0, 16);

  // Sanitize topic to ASCII for safe shell argument passing
  const safeTopic = (digest.topic || '(no topic)').replace(/[^\x20-\x7e]/g, '?').slice(0, 80) || '(no topic)';

  if (alreadySaved(hash)) {
    LOG(`digest hash=${hash} 已 save，跳过（防抖）`);
    process.exit(0);
  }

  if (digest.archiveStatus === 'pending') {
    LOG(`archiveStatus=pending — 等待 Cursor hook 完成归档，本 hook 只标记事件不 save`);
    process.exit(0);
  }

  const changed = digest.changedFiles || [];
  if (changed.length === 0) {
    LOG(`changedFiles 为空，跳过`);
    process.exit(0);
  }

  const topic = safeTopic;
  const date = digest.date || new Date().toISOString().slice(0, 10);
  const codeFilesChanged = digest.codeFilesChanged || 0;
  LOG(`digest: date=${date} topic="${topic}" codeFiles=${codeFilesChanged} totalFiles=${changed.length}`);

  let savedCount = 0;

  // ─── Memory #1: context - 本次会话摘要 ────────────────
  try {
    const contextBody = buildContextBody(date, topic, codeFilesChanged, changed, digest);
    const title = `Session ${date} - ${topic} (${codeFilesChanged} code files, ${changed.length} total)`;
    saveMemory({
      title,
      kind: 'context',
      tags: ['session-summary', 'auto-saved', date],
      body: contextBody,
    });
    savedCount++;
  } catch (e) { LOG(`context save 失败: ${e.message}`); }

  // ─── Memory #2: fact - 文件变更事实（如果是 Phase 8 / p8-* 等关键路径）───
  const criticalPaths = changed.filter(f =>
    /docs\/universal\/Phase-8|p8-[abc]\/|track-b|track-a|\.ecc\/memory|Phase-8-JNPF-Table|Table-Refactoring-Expert|Foundry-Target-Profile|JNPF-Extension/i.test(f)
  );
  if (criticalPaths.length > 0) {
    try {
      const factBody = buildFactBody(date, criticalPaths);
      saveMemory({
        title: `Phase 8 File Changes ${date} - ${criticalPaths.length} critical files`,
        kind: 'fact',
        tags: ['session-summary', 'auto-saved', 'phase-8', date],
        body: factBody,
      });
      savedCount++;
    } catch (e) { LOG(`fact save 失败: ${e.message}`); }
  }

  // ─── Memory #3: handoff - 如果有未完成的工作（基于 heuristic）─────
  try {
    const handoffBody = detectHandoff(date, topic, changed);
    if (handoffBody) {
      saveMemory({
        title: `Session Handoff ${date} - pending work for next session`,
        kind: 'handoff',
        tags: ['session-summary', 'auto-saved', 'handoff', date],
        body: handoffBody,
      });
      savedCount++;
    }
  } catch (e) { LOG(`handoff save 失败: ${e.message}`); }

  markSaved(hash);
  LOG(`✅ 已 save ${savedCount} 条 memory 到 ECC Vault`);
  process.exit(0);
}

function buildContextBody(date, topic, codeFilesChanged, changed, digest) {
  const fileList = changed.slice(0, 30).map(f => `- \`${f}\``).join('\n');
  const more = changed.length > 30 ? `\n- ... +${changed.length - 30} more` : '';
  return `Session auto-saved at ${STAMP()} from digest hash.

**Date**: ${date}
**Topic**: ${topic}
**Code files changed**: ${codeFilesChanged}
**Total files touched**: ${changed.length}
**Archive status**: ${digest.archiveStatus}
**Source**: .claude/memory/session-digest/latest.json

## Changed Files (first 30)
${fileList}${more}

## Next Session Quick Recall
Search this vault with: \`ecc memory search "${date}" --kind context\`
or: \`ecc memory search "${topic}" --kind context\`
`;
}

function buildFactBody(date, criticalPaths) {
  const fileList = criticalPaths.map(f => `- \`${f}\``).join('\n');
  return `Critical-path file changes from session ${date} (auto-detected).

${fileList}

Total: ${criticalPaths.length} files in Phase 8 / ECC memory / shadow-mode paths.

For full session context, recall the companion context memory for ${date}.
`;
}

function detectHandoff(date, topic, changed) {
  // Heuristic: if any file has TODO/FIXME/pending in name, or topic mentions pending
  const pendingSignals = changed.filter(f => /pending|todo|fixme|review/i.test(f));
  if (pendingSignals.length === 0 && !/pending|review|handoff/i.test(topic)) return null;
  return `Auto-detected pending work from session ${date}.

**Topic**: ${topic}
**Pending files**: ${pendingSignals.length || 0}

Next session should verify whether these items are still pending or have been resolved.
`;
}

function saveMemory({ title, kind, tags, body }) {
  // 写入 body 到临时文件（避免 shell 转义陷阱）
  const tmpBody = `${ROOT}/.claude/.session-summary-body.tmp`;
  try {
    writeFileSync(tmpBody, body, 'utf-8');
  } catch (e) {
    LOG(`body 写入 tmp 失败: ${e.message}`);
    throw e;
  }

  const args = [
    'ecc', 'memory', 'save',
    '--title', title,
    '--kind', kind,
    '--source-harness', 'auto-session-summary',
    '--target', 'all',
    ...tags.flatMap(t => ['--tag', t]),
    '--body-file', tmpBody,
    '--json',
  ];

  // 在 Windows 上必须用 shell 模式，但每个参数要双引号包裹并转义内部引号
  const quoted = args.map(a => {
    if (/[\s"&|<>^()]/.test(a)) {
      return `"${a.replace(/"/g, '\\"')}"`;
    }
    return a;
  }).join(' ');

  try {
    const result = execSync(`npx ${quoted}`, {
      cwd: ROOT,
      encoding: 'utf-8',
      stdio: ['ignore', 'pipe', 'pipe'],
      timeout: 30000,
      shell: true,
      windowsHide: true,
    });
    const parsed = JSON.parse(result);
    LOG(`${kind} memory saved: ${parsed.memory?.id || 'unknown'}`);
    return parsed;
  } catch (e) {
    const stderr = (e.stderr || e.stdout || e.message || '').toString();
    LOG(`${kind} save 失败: ${stderr.slice(0, 400)}`);
    throw e;
  } finally {
    try { execSync(`del "${tmpBody}"`, { shell: true }); } catch {}
  }
}

main().catch(e => {
  LOG(`fatal: ${e.message}`);
  process.exit(0); // hook 失败不阻断 session 退出
});