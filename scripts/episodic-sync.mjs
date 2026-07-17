#!/usr/bin/env node
/**
 * Sync Cursor/Claude conversations into episodic-memory index.
 *
 * Cursor IDE 对话在 ~/.cursor/projects/<slug>/agent-transcripts/，
 * episodic CLI 只读 ~/.claude/projects — 本脚本先把 Cursor jsonl 转成 Claude 兼容格式再 sync。
 *
 * Usage:
 *   node scripts/episodic-sync.mjs           # foreground sync
 *   node scripts/episodic-sync.mjs --background
 *   node scripts/episodic-sync.mjs --stats
 */
import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';
import { EPISODIC_CLI, SYNC_STATUS_PATH, SYNC_LOG_DIR } from './episodic-config.mjs';
import { getRepoRoot, loadManifest } from './toolchain-lib.mjs';

const args = process.argv.slice(2);
const background = args.includes('--background');
const statsOnly = args.includes('--stats');

/** 默认走快路径（只索引、不调 LLM 摘要）；摘要 API 配好后可设 EPISODIC_SYNC_WITH_SUMMARIES=1 */
function buildCliArgs() {
  if (statsOnly) return ['stats'];
  if (process.env.EPISODIC_SYNC_WITH_SUMMARIES === '1') {
    return ['sync', ...(background ? ['--background'] : [])];
  }
  return ['index', 'index-cleanup', '--no-summaries'];
}

const cliArgs = buildCliArgs();
const usesFastIndex = cliArgs[0] === 'index';

function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

/** episodic id `D--JNPF-v52` → Cursor 项目目录 `d-JNPF-v52` */
function episodicIdToCursorProjectId(episodicProjectId) {
  const m = /^([A-Za-z])--(.+)$/.exec(episodicProjectId || '');
  if (!m) return null;
  return `${m[1].toLowerCase()}-${m[2]}`;
}

/** Cursor jsonl 行 → episodic parser 可读的 Claude 兼容行（type + message.role） */
function convertCursorLine(rawLine, sessionId) {
  const parsed = JSON.parse(rawLine);
  const role = parsed.role;
  if (role !== 'user' && role !== 'assistant') return null;
  if (!parsed.message?.content) return null;

  return JSON.stringify({
    type: role,
    message: {
      role,
      content: parsed.message.content,
    },
    sessionId,
    timestamp: parsed.timestamp || new Date().toISOString(),
    cwd: parsed.cwd,
  });
}

/**
 * 将 Cursor agent-transcripts 桥接到 ~/.claude/projects/<episodicProjectId>/cursor-*.jsonl
 * @returns {{ bridged: number, skipped: number, cursorRoot?: string, reason?: string }}
 */
function bridgeCursorTranscripts(episodicProjectId) {
  const cursorId = episodicIdToCursorProjectId(episodicProjectId);
  if (!cursorId) {
    return { bridged: 0, skipped: 0, reason: 'invalid episodic project id' };
  }

  const home = process.env.USERPROFILE || process.env.HOME || '';
  const cursorRoot = path.join(home, '.cursor', 'projects', cursorId, 'agent-transcripts');
  const claudeDest = path.join(home, '.claude', 'projects', episodicProjectId);

  if (!fs.existsSync(cursorRoot)) {
    return { bridged: 0, skipped: 0, cursorRoot, reason: 'no cursor agent-transcripts dir' };
  }

  ensureDir(claudeDest);

  let bridged = 0;
  let skipped = 0;

  for (const entry of fs.readdirSync(cursorRoot, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;

    const sessionId = entry.name;
    const srcFile = path.join(cursorRoot, sessionId, `${sessionId}.jsonl`);
    if (!fs.existsSync(srcFile)) continue;

    const destFile = path.join(claudeDest, `cursor-${sessionId}.jsonl`);
    const srcStat = fs.statSync(srcFile);

    if (fs.existsSync(destFile)) {
      const destStat = fs.statSync(destFile);
      if (destStat.mtimeMs >= srcStat.mtimeMs && destStat.size > 0) {
        skipped++;
        continue;
      }
    }

    const out = [];
    for (const line of fs.readFileSync(srcFile, 'utf8').split('\n')) {
      if (!line.trim()) continue;
      try {
        const converted = convertCursorLine(line, sessionId);
        if (converted) out.push(converted);
      } catch {
        // 跳过损坏行
      }
    }

    if (out.length === 0) continue;

    fs.writeFileSync(destFile, `${out.join('\n')}\n`, 'utf8');
    fs.utimesSync(destFile, srcStat.atime, srcStat.mtime);
    bridged++;
  }

  return { bridged, skipped, cursorRoot };
}

function writeStatus(payload) {
  ensureDir(path.dirname(SYNC_STATUS_PATH));
  fs.writeFileSync(
    SYNC_STATUS_PATH,
    JSON.stringify({ ...payload, updatedAt: new Date().toISOString() }, null, 2),
    'utf8',
  );
}

function runCli() {
  return new Promise((resolve, reject) => {
    if (!fs.existsSync(EPISODIC_CLI)) {
      reject(new Error(`episodic-memory CLI not found: ${EPISODIC_CLI}`));
      return;
    }

    ensureDir(SYNC_LOG_DIR);
    const logPath = path.join(SYNC_LOG_DIR, 'episodic-sync.log');
    const logStream = fs.createWriteStream(logPath, { flags: 'a' });
    logStream.write(`\n--- ${new Date().toISOString()} ${cliArgs.join(' ')} ---\n`);

    // index 无 --background；hook 快路径自行 detach
    if (background && usesFastIndex) {
      const logFd = fs.openSync(logPath, 'a');
      fs.writeSync(logFd, 'Fast index started in background (no LLM summaries).\n');
      const child = spawn(process.execPath, [EPISODIC_CLI, ...cliArgs], {
        detached: true,
        stdio: ['ignore', logFd, logFd],
        windowsHide: true,
      });
      child.unref();
      logStream.end();
      resolve({ stdout: 'Fast index started in background.', stderr: '' });
      return;
    }

    const child = spawn(process.execPath, [EPISODIC_CLI, ...cliArgs], {
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true,
    });

    let stdout = '';
    let stderr = '';
    child.stdout.on('data', (d) => {
      const s = d.toString();
      stdout += s;
      logStream.write(s);
    });
    child.stderr.on('data', (d) => {
      const s = d.toString();
      stderr += s;
      logStream.write(s);
    });
    child.on('error', reject);
    child.on('close', (code) => {
      logStream.end();
      if (code === 0) resolve({ stdout, stderr });
      else reject(new Error(stderr || stdout || `exit ${code}`));
    });
  });
}

try {
  let bridge = null;
  if (!statsOnly) {
    const manifest = loadManifest(getRepoRoot());
    bridge = bridgeCursorTranscripts(manifest.episodic_project_id);
  }

  if (background) {
    writeStatus({ phase: 'background-started', cliArgs, bridge });
  }

  const { stdout, stderr } = await runCli();

  const copied = stdout.match(/Copied:\s*(\d+)/)?.[1];
  const indexed = stdout.match(/Indexed:\s*(\d+)/)?.[1]
    ?? stdout.match(/Conversations:\s*(\d+)/)?.[1];

  writeStatus({
    phase: statsOnly ? 'stats' : background ? 'background-requested' : 'sync-complete',
    mode: usesFastIndex ? 'index-no-summaries' : 'sync',
    bridge,
    copied: copied ? Number(copied) : undefined,
    indexed: indexed ? Number(indexed) : undefined,
    ok: true,
    tail: stdout.split('\n').slice(-8).join('\n').trim(),
  });

  if (!background) {
    process.stdout.write(stdout);
    if (stderr) process.stderr.write(stderr);
  } else {
    console.log(JSON.stringify({ ok: true, background: true, statusFile: SYNC_STATUS_PATH }));
  }
} catch (err) {
  writeStatus({ phase: 'error', ok: false, error: String(err.message || err) });
  console.error(err.message || err);
  process.exit(1);
}
