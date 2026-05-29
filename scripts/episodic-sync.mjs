#!/usr/bin/env node
/**
 * Sync Cursor/Claude conversations into episodic-memory index.
 * Usage:
 *   node scripts/episodic-sync.mjs           # foreground sync
 *   node scripts/episodic-sync.mjs --background
 *   node scripts/episodic-sync.mjs --stats
 */
import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';
import { EPISODIC_CLI, SYNC_STATUS_PATH, SYNC_LOG_DIR } from './episodic-config.mjs';

const args = process.argv.slice(2);
const background = args.includes('--background');
const statsOnly = args.includes('--stats');
const cliArgs = statsOnly ? ['stats'] : ['sync', ...(background ? ['--background'] : [])];

function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
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
  if (background) {
    writeStatus({ phase: 'background-started', cliArgs });
  }

  const { stdout, stderr } = await runCli();

  const copied = stdout.match(/Copied:\s*(\d+)/)?.[1];
  const indexed = stdout.match(/Indexed:\s*(\d+)/)?.[1];

  writeStatus({
    phase: statsOnly ? 'stats' : background ? 'background-requested' : 'sync-complete',
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
