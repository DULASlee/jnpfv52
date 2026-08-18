#!/usr/bin/env node
/**
 * Cursor stop hook — 归档完成后触发 Agent 在助手回复正文中输出「三项抬头」
 * 须在 session-archive-stop 之后、guard-pillar-stop 之前执行：
 * - 不写 followup 原文（避免出现在聊天面板/输入区）
 * - 写 last-archive-banner.txt + followup 指令 → Agent 下一条助手消息原样输出
 * - loop_count > 0 时跳过，防无限循环
 */
import {
  buildArchiveBannerAgentFollowup,
  buildArchiveStatusBanner,
  getProjectRoot,
  readSessionDigest,
  writeArchiveBannerFile,
} from './session-archive-lib.mjs';

function readStdinWithTimeout(ms = 200) {
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
const loopCount = Number(hookInput.loop_count ?? hookInput.loopCount ?? 0);

if (hookEvent !== 'stop' || loopCount > 0) {
  process.stdout.write(JSON.stringify({ ok: true, skipped: 'not-stop-or-loop' }));
  process.exit(0);
}

const root = getProjectRoot();
const digest = readSessionDigest(root);

if (!digest || digest.codeFilesChanged === 0 || digest.archiveStatus !== 'complete') {
  process.stdout.write(JSON.stringify({
    ok: true,
    skipped: 'no-banner',
    reason: !digest ? 'no-digest' : digest.archiveStatus !== 'complete' ? 'incomplete' : 'no-code-changes',
  }));
  process.exit(0);
}

const banner = buildArchiveStatusBanner(digest, null);
writeArchiveBannerFile(root, banner, digest);

process.stdout.write(JSON.stringify({
  followup_message: buildArchiveBannerAgentFollowup(),
}));
process.exit(0);
