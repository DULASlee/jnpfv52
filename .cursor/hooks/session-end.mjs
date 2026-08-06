#!/usr/bin/env node
/**
 * Cursor sessionEnd hook — 用户关闭 Chat / 结束会话时归档（不触发 followup 链）
 * 与 stop 共用同一套落盘逻辑，仅 CURSOR_HOOK_EVENT=sessionEnd
 */
import { spawnSync } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const HOOKS_DIR = path.dirname(fileURLToPath(import.meta.url));

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

async function readStdinJson() {
  try {
    const raw = (await readStdinWithTimeout()).trim();
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

const stdinPayload = await readStdinJson();

function run(script) {
  return spawnSync(process.execPath, [path.join(HOOKS_DIR, script)], {
    env: {
      ...process.env,
      CURSOR_HOOK_EVENT: 'sessionEnd',
      CURSOR_HOOK_STDIN: JSON.stringify(stdinPayload),
    },
    encoding: 'utf8',
    timeout: 25000,
    stdio: ['ignore', 'pipe', 'pipe'],
  });
}

// 1) episodic 全文索引（后台）
run('episodic-stop.mjs');
// 2) 结构化 digest + AUTO summary
const archive = run('session-archive-stop.mjs');
// 3) pillar 仅校验，sessionEnd 不 followup
run('guard-pillar-stop.mjs');

let payload = { ok: true, hookEvent: 'sessionEnd' };
try {
  payload = { ...payload, ...JSON.parse(archive.stdout?.trim() || '{}') };
} catch { /* keep ok */ }

process.stdout.write(JSON.stringify(payload));
process.exit(0);
