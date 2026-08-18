#!/usr/bin/env node
/**
 * guard-skill-load.mjs — PreToolUse(Skill) 限速，防止 Skill 风暴
 */

import { readStdin, checkSkillLoadRate } from './hook-lib.mjs';

try {
  let input = {};
  try {
    const raw = await readStdin(2000);
    if (raw.trim()) input = JSON.parse(raw);
  } catch {
    process.exit(0);
  }

  if ((input.tool_name || '') !== 'Skill') {
    process.exit(0);
  }

  const skillName = (
    input.tool_input?.skill
    || input.tool_input?.name
    || input.tool_input?.skill_name
    || ''
  ).toString();

  const { allow, reason } = checkSkillLoadRate(skillName);

  if (!allow && reason === 'storm-limit') {
    console.error('⛔ Skill 加载风暴：15s 内调用过多。等待 10s 后重试，勿循环 Skill。');
    process.exit(2);
  }

  process.exit(0);
} catch {
  process.exit(0);
}
