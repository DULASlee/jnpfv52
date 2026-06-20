#!/usr/bin/env node
/**
 * SessionStart Hook — Superpowers 强制激活验证 (JNPF v5.2)
 *
 * 职责：
 *   1. 验证 superpowers 插件已加载（技能目录存在）
 *   2. 输出强制性使用提醒（AI MUST 遵循）
 *   3. 验证核心 MCP 可用（serena / playwright）
 *
 * NEVER 阻断会话启动。仅输出强制性指令。
 * 预算：≤ 3s
 */

import { existsSync } from 'fs';
import { homedir } from 'os';
import { join } from 'path';

const HOME = homedir();
const CHECKS = [];

// ─── 1. Superpowers 插件验证 ─────────────────────────────────────
const spDir = join(HOME, '.claude', 'plugins', 'cache', 'superpowers-marketplace', 'superpowers');
if (existsSync(spDir)) {
  CHECKS.push('✅ superpowers 插件已激活');
} else {
  CHECKS.push('❌ superpowers 插件未安装！请运行: claude plugins install superpowers@superpowers-marketplace');
}

// ─── 2. 核心技能可用性 ──────────────────────────────────────────
const skillsDir = join(HOME, '.claude', 'skills');
const requiredSkills = ['brainstorming', 'verification-before-completion', 'systematic-debugging'];
if (existsSync(skillsDir)) {
  for (const sk of requiredSkills) {
    if (existsSync(join(skillsDir, sk))) {
      CHECKS.push(`✅ 技能可用: ${sk}`);
    } else {
      CHECKS.push(`⚠️ 技能缺失: ${sk}`);
    }
  }
}

// ─── 3. 核心 MCP 验证 ────────────────────────────────────────────
// serena (C# 符号级重构)
const serenaDir = join(HOME, '.local', 'bin');
if (existsSync(join(serenaDir, 'serena.exe'))) {
  CHECKS.push('✅ MCP: serena');
} else {
  CHECKS.push('⚠️ MCP: serena 未找到');
}

// playwright
try {
  require.resolve('playwright');
  CHECKS.push('✅ MCP: playwright');
} catch {
  CHECKS.push('⚠️ MCP: playwright 未安装');
}

// ─── 输出强制性激活横幅 ──────────────────────────────────────────
const passed = CHECKS.filter(c => c.includes('✅')).length;
const total = CHECKS.length;

console.error('');
console.error('╔══════════════════════════════════════════════════════════════╗');
console.error('║           JNPF v5.2 — Superpowers 强制激活                   ║');
console.error('╠══════════════════════════════════════════════════════════════╣');
console.error(`║  插件/技能: ${passed}/${total} OK`);
CHECKS.forEach(c => console.error(`║  ${c}`));
console.error('╠══════════════════════════════════════════════════════════════╣');
console.error('║  ⬛ AI 强制性指令（违反 = 流程违规）：                       ║');
console.error('║  1. 任何编码任务前 MUST 调用 brainstorming 技能              ║');
console.error('║  2. 任何声称"完成"前 MUST 调 verification-before-completion  ║');
console.error('║  3. 任何 bug/异常 MUST 调 systematic-debugging 技能          ║');
console.error('║  4. Skill 工具 MUST 在任何响应之前检查 applicable skills     ║');
console.error('║  5. 违反以上 → 未通过 Supreme Iron Law 验收                  ║');
console.error('╚══════════════════════════════════════════════════════════════╝');
console.error('');

process.exit(0);
