/**
 * 跨会话结构化归档 — sessionStart/stop hook 共享库
 * 写入路径：.claude/memory/session-digest/（不依赖 episodic MCP write）
 */
import { execSync } from 'child_process';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const HOOKS_DIR = path.dirname(fileURLToPath(import.meta.url));
const REPO_FROM_SCRIPT = path.resolve(HOOKS_DIR, '..', '..');

export function getProjectRoot() {
  try {
    return execSync('git rev-parse --show-toplevel', {
      cwd: REPO_FROM_SCRIPT,
      encoding: 'utf8',
      stdio: 'pipe',
      timeout: 5000,
    }).trim();
  } catch {
    return REPO_FROM_SCRIPT;
  }
}

export function getTodayStr(d = new Date()) {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function getChangedFiles(root) {
  const files = new Set();
  for (const cmd of [
    'git diff --name-only HEAD',
    'git diff --name-only --cached',
    'git ls-files --others --exclude-standard',
  ]) {
    try {
      const out = execSync(cmd, { cwd: root, encoding: 'utf8', stdio: 'pipe', timeout: 8000 }).trim();
      for (const line of out.split('\n')) {
        const t = line.trim().replace(/\\/g, '/');
        if (t) files.add(t);
      }
    } catch { /* ignore */ }
  }
  return [...files];
}

export function isCodeFile(f) {
  const n = f.replace(/\\/g, '/');
  if (/\.claude\/evidence\//.test(n)) return false;
  if (/^docs\//.test(n) && !/progress-registry\.yaml$/.test(n)) return false;
  return /\.(cs|vue|ts|tsx|js|mjs|sql|json)$/i.test(n)
    && !/package-lock\.json$/i.test(n)
    && !/pnpm-lock\.yaml$/i.test(n);
}

export function isArchiveMetaFile(f) {
  const n = f.replace(/\\/g, '/');
  return n === '.cursor/CURRENT-FOCUS.md'
    || n === 'docs/progress-registry.yaml'
    || n === '.claude/memory/mistake-log.md'
    || n.startsWith('.claude/memory/session-summaries/')
    || n.startsWith('.claude/memory/session-digest/');
}

export function archivalComplete(root, today) {
  const mistake = path.join(root, '.claude/memory/mistake-log.md');
  const focus = path.join(root, '.cursor/CURRENT-FOCUS.md');
  const registry = path.join(root, 'docs/progress-registry.yaml');

  let mistakeOk = false;
  let focusOk = false;
  let registryOk = false;

  if (fs.existsSync(mistake)) {
    const c = fs.readFileSync(mistake, 'utf8');
    mistakeOk = c.includes(`## ${today}`);
  }
  if (fs.existsSync(focus)) {
    const c = fs.readFileSync(focus, 'utf8');
    focusOk = c.includes(today) && /会话结论|当前节点|待你验|hook 自动/.test(c);
  }
  if (fs.existsSync(registry)) {
    registryOk = fs.readFileSync(registry, 'utf8').includes(`date: "${today}"`);
  }

  return {
    mistakeOk,
    focusOk,
    registryOk,
    complete: mistakeOk && focusOk && registryOk,
  };
}

export function writeSessionDigest(root, payload) {
  const dir = path.join(root, '.claude/memory/session-digest');
  fs.mkdirSync(dir, { recursive: true });
  const latest = path.join(dir, 'latest.json');
  fs.writeFileSync(latest, JSON.stringify(payload, null, 2), 'utf8');
  const stamp = payload.endedAt.replace(/[-:T.Z]/g, '').slice(0, 14);
  fs.writeFileSync(path.join(dir, `${payload.date}-${stamp}.json`), JSON.stringify(payload, null, 2), 'utf8');
  return latest;
}

export function readSessionDigest(root) {
  const latest = path.join(root, '.claude/memory/session-digest/latest.json');
  if (!fs.existsSync(latest)) return null;
  try {
    return JSON.parse(fs.readFileSync(latest, 'utf8'));
  } catch {
    return null;
  }
}

export function ensureMistakeLogStub(root, today, codeFiles) {
  const mistakePath = path.join(root, '.claude/memory/mistake-log.md');
  if (!fs.existsSync(mistakePath)) return false;
  let content = fs.readFileSync(mistakePath, 'utf8');
  if (content.includes(`## ${today}`)) return false;

  const keywords = codeFiles.slice(0, 4).map((f) => path.basename(f)).join(', ');
  const stub = [
    '',
    `## ${today}`,
    '',
    '| 日期 | 类别 | 症状 | 根因 | 修复 | 关键词 |',
    '|------|------|------|------|------|--------|',
    `| ${today} | AUTO-DRAFT | stop hook：本日有代码变更但未归档 | 待 Agent 补全 | 待 Agent 补全 | ${keywords || 'code-change'} |`,
    '',
    `> **自动草稿**（session-archive-stop）：变更 ${codeFiles.length} 个代码文件。Agent MUST 补全 \`### M0xx\` 条目并更新 CURRENT-FOCUS / progress-registry。`,
    '',
  ].join('\n');

  const anchor = '## 一、方法论';
  content = content.includes(anchor) ? content.replace(anchor, `${stub}\n${anchor}`) : `${content}\n${stub}`;
  fs.writeFileSync(mistakePath, content, 'utf8');
  return true;
}

export function inferTopic(codeFiles) {
  const joined = codeFiles.join(' ').toLowerCase();
  if (/designskillorchestrator|architectskill|dbdesign|uidesign|designskills/.test(joined)) return '设计 Skill 编排';
  if (/requirementanalysis|aichatpanel|clarification/.test(joined)) return 'PM 需求分析澄清续跑';
  if (/irprojection|ireventstore/.test(joined)) return 'IR 投影/事件';
  if (/hook|episodic|session-archive/.test(joined)) return '工具链/跨会话归档';
  return '代码变更';
}

export function inferCategory(codeFiles) {
  const joined = codeFiles.join(' ').toLowerCase();
  if (/\.cursor\/hooks|episodic|toolchain|scripts\//.test(joined)) return '工具链';
  if (/inteassistant|backend\//.test(joined)) return '后端';
  if (/jnpf-web-vue3|\.vue/.test(joined)) return '前端';
  return '代码变更';
}

export function inferVerifyHint(codeFiles) {
  const joined = codeFiles.join(' ').toLowerCase();
  if (/backend\//.test(joined) || codeFiles.some((f) => /\.cs$/i.test(f))) {
    return 'cd backend && dotnet build';
  }
  if (/jnpf-web-vue3/.test(joined) || codeFiles.some((f) => /\.(vue|ts|tsx)$/i.test(f))) {
    return 'cd jnpf-web-vue3 && pnpm type-check';
  }
  if (/hook|episodic|toolchain/.test(joined)) return 'node scripts/verify-toolchain.mjs';
  return 'node scripts/verify-toolchain.mjs';
}

export function buildArchiveSessionId(digest) {
  if (digest.conversationId) return String(digest.conversationId);
  return digest.endedAt.replace(/[-:T.Z]/g, '').slice(0, 14);
}

export function alreadyMachineArchived(content, sessionId) {
  return content.includes(`hook-auto-archive:${sessionId}`)
    || content.includes(`hook_auto_archive: "${sessionId}"`);
}

export function getNextMistakeNumber(content) {
  let max = 0;
  for (const m of content.matchAll(/### M(\d{3})/g)) {
    max = Math.max(max, parseInt(m[1], 10));
  }
  return max + 1;
}

function formatTimeLabel(iso) {
  try {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  } catch {
    return 'auto';
  }
}

function summarizeChangedFiles(codeFiles, limit = 6) {
  return codeFiles.slice(0, limit).map((f) => path.basename(f)).join(', ')
    + (codeFiles.length > limit ? ` +${codeFiles.length - limit}` : '');
}

/** 机器归档：错题本 + CURRENT-FOCUS + progress-registry（deterministic，无 LLM） */
export function applyMachineArchival(root, digest, codeFiles, autoSummaryPath = null) {
  if (codeFiles.length === 0) {
    return { applied: false, reason: 'no-code-changes', sessionId: null };
  }

  const sessionId = buildArchiveSessionId(digest);
  const focusPath = path.join(root, '.cursor/CURRENT-FOCUS.md');
  const registryPath = path.join(root, 'docs/progress-registry.yaml');

  if (fs.existsSync(focusPath)) {
    const focusContent = fs.readFileSync(focusPath, 'utf8');
    if (alreadyMachineArchived(focusContent, sessionId)) {
      return { applied: false, reason: 'already-archived', sessionId };
    }
  }

  const mistakeId = writeMachineMistakeLog(root, digest, codeFiles, sessionId);
  writeMachineCurrentFocus(root, digest, codeFiles, sessionId, autoSummaryPath, mistakeId);
  writeMachineProgressRegistry(root, digest, codeFiles, sessionId, autoSummaryPath, mistakeId);

  return {
    applied: true,
    sessionId,
    mistakeId,
    focusPath: path.relative(root, focusPath).replace(/\\/g, '/'),
    registryPath: path.relative(root, registryPath).replace(/\\/g, '/'),
  };
}

function writeMachineMistakeLog(root, digest, codeFiles, sessionId) {
  const mistakePath = path.join(root, '.claude/memory/mistake-log.md');
  if (!fs.existsSync(mistakePath)) return null;

  let content = fs.readFileSync(mistakePath, 'utf8');
  if (alreadyMachineArchived(content, sessionId)) return null;

  const today = digest.date;
  const num = getNextMistakeNumber(content);
  const mistakeId = `M${String(num).padStart(3, '0')}`;
  const category = inferCategory(codeFiles);
  const keywords = summarizeChangedFiles(codeFiles, 4);
  const fileList = codeFiles.slice(0, 12).map((f) => `\`${f}\``).join(', ');

  if (!content.includes(`## ${today}`)) {
    const dayHeader = [
      '',
      `## ${today}`,
      '',
      '| 日期 | 类别 | 症状 | 根因 | 修复 | 关键词 |',
      '|------|------|------|------|------|--------|',
    ].join('\n');
    const anchor = '## 一、方法论';
    content = content.includes(anchor)
      ? content.replace(anchor, `${dayHeader}\n${anchor}`)
      : `${content}\n${dayHeader}`;
  }

  const tableRow = `| ${today} | ${category} | hook 自动：${digest.topic}（${codeFiles.length} 文件） | 见 session-digest | 见 AUTO summary / digest | ${keywords} |`;
  const sectionAnchor = `## ${today}`;
  const sectionIdx = content.indexOf(sectionAnchor);
  if (sectionIdx >= 0) {
    const afterSection = content.slice(sectionIdx);
    const tableHeaderEnd = afterSection.indexOf('|------|');
    if (tableHeaderEnd >= 0) {
      const insertAt = sectionIdx + tableHeaderEnd + afterSection.slice(tableHeaderEnd).indexOf('\n') + 1;
      content = `${content.slice(0, insertAt)}${tableRow}\n${content.slice(insertAt)}`;
    }
  }

  const entry = [
    '',
    `### ${mistakeId} | ${digest.topic}（hook 自动归档）`,
    '',
    `- **症状**：stop hook 检测到 ${codeFiles.length} 个代码文件变更`,
    `- **根因**：机器归档快照（语义根因待人工可选补全）`,
    `- **修复**：见 \`.claude/memory/session-digest/latest.json\` 与 AUTO summary`,
    `- **变更**：${fileList}${codeFiles.length > 12 ? ' …' : ''}`,
    `- **hook-auto-archive**: ${sessionId}`,
    `- **日期**：${today} | **关键词**：\`${keywords}\``,
    '',
  ].join('\n');

  const insertBefore = '## 一、方法论';
  content = content.includes(insertBefore)
    ? content.replace(insertBefore, `${entry}${insertBefore}`)
    : `${content}${entry}`;

  fs.writeFileSync(mistakePath, content, 'utf8');
  return mistakeId;
}

function writeMachineCurrentFocus(root, digest, codeFiles, sessionId, autoSummaryPath, mistakeId) {
  const focusPath = path.join(root, '.cursor/CURRENT-FOCUS.md');
  if (!fs.existsSync(focusPath)) return false;

  let content = fs.readFileSync(focusPath, 'utf8');
  if (alreadyMachineArchived(content, sessionId)) return false;

  const timeLabel = formatTimeLabel(digest.endedAt);
  const verify = inferVerifyHint(codeFiles);
  const summaryRel = autoSummaryPath
    ? path.relative(root, autoSummaryPath).replace(/\\/g, '/')
    : '.claude/memory/session-digest/latest.json';
  const filesSummary = summarizeChangedFiles(codeFiles, 5);

  content = content.replace(
    /\| \*\*本 Chat 成果\*\* \|[^\n]*\|/,
    `| **本 Chat 成果** | ${digest.topic}（hook 自动 · ${codeFiles.length} 文件） |`,
  );
  content = content.replace(
    /\| \*\*待你验\*\* \|[^\n]*\|/,
    `| **待你验** | ${verify} |`,
  );
  content = content.replace(
    /\| \*\*跨会话归档\*\* \|[^\n]*\|/,
    `| **跨会话归档** | ${summaryRel} |`,
  );

  const sessionBlock = [
    '',
    `## ${digest.date} 会话结论（hook 自动 · ${timeLabel}）`,
    '',
    '| 问题 | 结论 |',
    '|---|---|',
    `| **主题** | ${digest.topic} |`,
    `| **变更** | ${filesSummary} |`,
    `| **待你验** | ${verify} |`,
    `| **摘要** | \`${summaryRel}\` |`,
    mistakeId ? `| **错题本** | ${mistakeId} |` : null,
    `| **hook-auto-archive** | \`${sessionId}\` |`,
    '',
  ].filter(Boolean).join('\n');

  const anchor = '\n## 2026-07-18 会话结论';
  const genericAnchor = /\n## \d{4}-\d{2}-\d{2} 会话结论/;
  if (genericAnchor.test(content)) {
    content = content.replace(genericAnchor, `${sessionBlock}$&`);
  } else {
    const nodeAnchor = '## 当前节点';
    const idx = content.indexOf(nodeAnchor);
    if (idx >= 0) {
      const afterNode = content.indexOf('\n## ', idx + nodeAnchor.length);
      const insertAt = afterNode >= 0 ? afterNode : content.length;
      content = `${content.slice(0, insertAt)}${sessionBlock}${content.slice(insertAt)}`;
    } else {
      content = `${content}\n${sessionBlock}`;
    }
  }

  fs.writeFileSync(focusPath, content, 'utf8');
  return true;
}

function writeMachineProgressRegistry(root, digest, codeFiles, sessionId, autoSummaryPath, mistakeId) {
  const registryPath = path.join(root, 'docs/progress-registry.yaml');
  if (!fs.existsSync(registryPath)) return false;

  let content = fs.readFileSync(registryPath, 'utf8');
  if (alreadyMachineArchived(content, sessionId)) return false;

  const verify = inferVerifyHint(codeFiles);
  const summaryRel = autoSummaryPath
    ? path.relative(root, autoSummaryPath).replace(/\\/g, '/')
    : '.claude/memory/session-digest/latest.json';
  const topic = digest.topic.replace(/"/g, '\\"');

  const entryLines = [
    `  - date: "${digest.date}"`,
    `    topic: "${topic}"`,
    `    hook_auto_archive: "${sessionId}"`,
    '    outcomes:',
    `      - "hook 自动归档：${codeFiles.length} 个代码文件 · ${digest.topic}"`,
    ...codeFiles.slice(0, 10).map((f) => `      - "${f.replace(/"/g, '\\"')}"`),
    `    verify: "${verify.replace(/"/g, '\\"')}"`,
    `    session_summary: "${summaryRel.replace(/"/g, '\\"')}"`,
    mistakeId ? `    mistake_log: "${mistakeId}"` : null,
    '    episodic_project_id: "D--JNPF-v52"',
  ].filter(Boolean);

  const marker = 'session_log:';
  const markerIdx = content.indexOf(marker);
  if (markerIdx < 0) return false;

  const insertAt = markerIdx + marker.length + 1;
  content = `${content.slice(0, insertAt)}${entryLines.join('\n')}\n${content.slice(insertAt)}`;
  content = content.replace(/^last_updated: ".*"/m, `last_updated: "${digest.date}"`);

  fs.writeFileSync(registryPath, content, 'utf8');
  return true;
}

export function writeHookRunLog(root, entry) {
  const dir = path.join(root, '.cursor', 'episodic');
  fs.mkdirSync(dir, { recursive: true });
  const logPath = path.join(dir, 'hook-run-log.json');
  let history = [];
  if (fs.existsSync(logPath)) {
    try {
      history = JSON.parse(fs.readFileSync(logPath, 'utf8')).runs || [];
    } catch { /* ignore */ }
  }
  history.unshift(entry);
  fs.writeFileSync(logPath, JSON.stringify({
    updatedAt: new Date().toISOString(),
    runs: history.slice(0, 40),
  }, null, 2), 'utf8');
  return logPath;
}

/** 无人工 summary 时写 AUTO 草稿，便于跨 Chat 续接 */
export function writeAutoSessionSummary(root, digest, codeFiles) {
  if (codeFiles.length === 0) return null;
  const dir = path.join(root, '.claude/memory/session-summaries');
  fs.mkdirSync(dir, { recursive: true });
  const slug = (digest.topic || 'code-change')
    .replace(/[^\w\u4e00-\u9fff-]+/g, '-')
    .replace(/-+/g, '-')
    .slice(0, 36)
    .replace(/^-|-$/g, '') || 'session';
  const manualExists = fs.readdirSync(dir).some(
    (f) => f.startsWith(`${digest.date}-`) && !f.includes('-AUTO'),
  );
  if (manualExists) return null;

  const fname = `${digest.date}-${slug}-AUTO.md`;
  const fpath = path.join(dir, fname);
  if (fs.existsSync(fpath)) return fpath;

  const lines = [
    `# ${digest.topic || '会话'}（自动草稿）`,
    '',
    `> Cursor \`${digest.hookEvent || 'stop'}\` hook 于 ${digest.endedAt} 自动生成。`,
    '> Agent 或用户 SHOULD 补全：问题链、根因、下 Chat 开场词。',
    '',
    `## 变更文件（${codeFiles.length}）`,
    ...codeFiles.slice(0, 40).map((f) => `- \`${f}\``),
    codeFiles.length > 40 ? `- … 另有 ${codeFiles.length - 40} 个` : '',
    '',
    '## 归档状态',
    `- archiveStatus: **${digest.archiveStatus}**`,
    `- mistakeOk: ${digest.archiveChecks?.mistakeOk ?? '?'}`,
    `- focusOk: ${digest.archiveChecks?.focusOk ?? '?'}`,
    `- registryOk: ${digest.archiveChecks?.registryOk ?? '?'}`,
    '',
    '## 机器归档',
    '- [x] `.cursor/CURRENT-FOCUS.md`（hook 自动）',
    '- [x] `docs/progress-registry.yaml` session_log（hook 自动）',
    '- [x] `.claude/memory/mistake-log.md` M0xx 占位（hook 自动）',
    '- [ ] 可选：人工润色根因/修复语义',
    '',
  ].filter(Boolean);

  fs.writeFileSync(fpath, lines.join('\n'), 'utf8');
  return fpath;
}

export function readEpisodicSyncStatus(root) {
  const p = path.join(root, '.cursor', 'episodic', 'sync-status.json');
  if (!fs.existsSync(p)) return null;
  try {
    return JSON.parse(fs.readFileSync(p, 'utf8'));
  } catch {
    return null;
  }
}

export const ARCHIVE_BANNER_PATH_REL = '.cursor/episodic/last-archive-banner.txt';

/** 聊天抬头：三项归档状态（deterministic，写入文件供 Agent 原样输出） */
export function buildArchiveStatusBanner(digest, syncStatus) {
  const checks = digest.archiveChecks || {};
  const ma = digest.machineArchival || {};
  const icon = (ok) => (ok ? '✅' : '❌');

  let episodicDetail = '已触发（后台 index）';
  if (syncStatus?.ok && syncStatus.phase === 'sync-complete') episodicDetail = '已完成';
  else if (syncStatus?.ok && /^background-/.test(syncStatus.phase || '')) episodicDetail = '后台进行中';
  else if (syncStatus?.ok && syncStatus.phase === 'stats') episodicDetail = '已触发（后台 index）';
  else if (syncStatus?.ok) episodicDetail = '已触发';

  const structOk = checks.focusOk && checks.registryOk;
  const lines = [
    '【跨会话归档完成】',
    '',
    `${icon(true)} ① episodic 对话全文索引 — ${episodicDetail}`,
    `${icon(structOk)} ② CURRENT-FOCUS + progress-registry — ${structOk ? '已更新' : '未完成'}`,
    `${icon(checks.mistakeOk)} ③ mistake-log — ${ma.mistakeId || (checks.mistakeOk ? '已更新' : '未完成')}`,
    '',
    `主题：${digest.topic || '代码变更'} · ${digest.codeFilesChanged ?? 0} 个代码文件`,
    `归档：${digest.archiveStatus || 'unknown'} · ${digest.endedAt || ''}`,
    `digest：.claude/memory/session-digest/latest.json`,
    '',
    '_（系统归档播报 · 无需 Agent 执行操作 · 可忽略或回复「已阅」）_',
  ];
  return lines.join('\n');
}

export function writeArchiveBannerFile(root, banner, digest) {
  const dir = path.join(root, '.cursor', 'episodic');
  fs.mkdirSync(dir, { recursive: true });
  const bannerPath = path.join(dir, 'last-archive-banner.txt');
  fs.writeFileSync(bannerPath, banner, 'utf8');
  fs.writeFileSync(
    path.join(dir, 'last-archive-banner.json'),
    JSON.stringify({
      updatedAt: new Date().toISOString(),
      endedAt: digest?.endedAt || null,
      sessionId: digest?.machineArchival?.sessionId || buildArchiveSessionId(digest || {}),
      codeFilesChanged: digest?.codeFilesChanged ?? 0,
      bannerPreview: banner.split('\n').slice(0, 6).join('\n'),
    }, null, 2),
    'utf8',
  );
  return bannerPath;
}

export function readLatestArchiveBanner(root, maxAgeMs = 7 * 24 * 3600 * 1000) {
  const bannerPath = path.join(root, '.cursor', 'episodic', 'last-archive-banner.txt');
  if (!fs.existsSync(bannerPath)) return null;
  try {
    const stat = fs.statSync(bannerPath);
    if (Date.now() - stat.mtimeMs > maxAgeMs) return null;
    return fs.readFileSync(bannerPath, 'utf8').trim();
  } catch {
    return null;
  }
}

export function buildArchiveFollowup(digest, checks) {
  const missing = [];
  if (!checks.mistakeOk) missing.push('`.claude/memory/mistake-log.md` → 追加 `## 今日`（机器归档失败，需手补）');
  if (!checks.focusOk) missing.push('`.cursor/CURRENT-FOCUS.md` → 更新当前节点/待验/会话结论（机器归档失败）');
  if (!checks.registryOk) missing.push('`docs/progress-registry.yaml` → session_log 顶插一条（机器归档失败）');

  if (missing.length === 0) {
    return [
      '【会话归档】机器归档已完成（CURRENT-FOCUS / progress-registry / mistake-log）。',
      `主题：${digest.topic || '代码变更'}；代码文件 ${digest.codeFilesChanged} 个。`,
      '可选：人工润色 mistake-log 根因/修复语义后回复「归档已补全」。',
    ].join('\n');
  }

  return [
    '【会话归档硬门】检测到有代码变更，但机器归档未写全。',
    `主题推断：${digest.topic || '代码变更'}；代码文件 ${digest.codeFilesChanged} 个。`,
    '',
    '**必须 NOW 完成（禁止结束）：**',
    ...missing.map((m, i) => `${i + 1}. ${m}`),
    '',
    '完成后回复用户「归档已补全」+ 本会话结论 ≤6 行。',
    '规则：`.cursor/rules/toolchain/episodic-memory-automation.mdc`',
  ].join('\n');
}
