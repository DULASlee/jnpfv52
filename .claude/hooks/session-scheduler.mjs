#!/usr/bin/env node
/**
 * session-scheduler.mjs — SessionStart 唯一入口（智能调度）
 */

import { existsSync, readFileSync } from 'fs';
import { join } from 'path';
import { homedir } from 'os';
import {
  readStdin,
  shouldSkipSessionInit,
  markSessionInit,
  getProjectRoot,
} from './hook-lib.mjs';

const HOME = homedir();
const STDIN_MS = 3000;

function loadMistakeSummary(root) {
  const mistakePath = join(root, '.claude', 'memory', 'mistake-log.md');
  if (!existsSync(mistakePath)) return null;

  const content = readFileSync(mistakePath, 'utf-8');
  const now = Date.now();
  const MAX_AGE = 30 * 24 * 60 * 60 * 1000;
  const titles = [];
  let currentDate = '';

  for (const line of content.split('\n')) {
    const dm = line.match(/^## (\d{4}-\d{2}-\d{2})/);
    if (dm) {
      currentDate = dm[1];
      continue;
    }
    if (line.startsWith('### M') && currentDate) {
      const age = now - new Date(currentDate).getTime();
      if (age <= MAX_AGE) titles.push(line.replace(/^###\s*/, ''));
    }
  }

  if (titles.length === 0) return null;
  return titles.slice(0, 8).join('\n  ');
}

try {
  let eventSource = 'startup';
  try {
    const raw = await readStdin(STDIN_MS);
    if (raw.trim()) {
      const input = JSON.parse(raw);
      eventSource = input.source || input.session_source || input.hook_event_name || 'startup';
    }
  } catch { /* empty stdin */ }

  const skip = shouldSkipSessionInit(String(eventSource).toLowerCase());
  if (skip.skip) {
    console.error(`[session-scheduler] 跳过 (${skip.reason})`);
    process.exit(0);
  }

  markSessionInit(eventSource);

  const root = getProjectRoot();
  const spDir = join(HOME, '.claude', 'plugins', 'cache', 'superpowers-marketplace', 'superpowers');
  const skillsOk = existsSync(join(root, '.cursor', 'skills'))
    || existsSync(join(HOME, '.claude', 'skills'));

  console.error('[session-scheduler] JNPF SessionInit (轻量单次)');
  console.error(`  事件: ${eventSource}`);
  console.error(`  superpowers: ${existsSync(spDir) ? 'ok' : 'missing'}`);
  console.error(`  skills: ${skillsOk ? 'ok' : 'warn'}`);

  const mistakes = loadMistakeSummary(root);
  const parts = [
    '<SESSION-SCHEDULER>',
    'SessionStart 轻量完成。',
    '⛔ 禁止自动批量加载 MCP/Skill/Agent。禁止 ListMcpResourcesTool 全量探测。',
    '任务开始时按需单个调用 Skill；MCP 懒加载。',
  ];
  if (mistakes) {
    parts.push('', '📖 错题本摘要:', `  ${mistakes}`);
  }
  parts.push('</SESSION-SCHEDULER>');

  console.log(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: 'SessionStart',
      additionalContext: parts.join('\n'),
    },
  }));

  process.exit(0);
} catch (e) {
  console.error('[session-scheduler]', e.message);
  process.exit(0);
}
