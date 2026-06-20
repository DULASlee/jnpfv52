#!/usr/bin/env node
/**
 * PostToolUse Hook — Superpowers 技能触发提醒 (JNPF v5.2)
 *
 * 代码变更后，判断变更规模和类型，向 AI 注入强制性技能调用提醒。
 * 仅触发于重度变更（3+ 文件 或 50+ 行），不影响轻量编辑。
 *
 * 预算：≤ 3s。失败静默跳过，不阻断。
 */

import { execSync } from 'child_process';

// ─── 收集变更 ────────────────────────────────────────────────────
let changedFiles = [];
let lineCount = 0;

try {
  const unstaged = execSync('git diff --name-only', {
    encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
  }).trim();
  changedFiles = unstaged.split('\n').filter(Boolean);
} catch { /* git 不可用，跳过 */ }

if (changedFiles.length === 0) {
  console.log(JSON.stringify({ decision: 'approve' }));
  process.exit(0);
}

// 行数统计
try {
  const diff = execSync('git diff --unified=0', {
    encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
  });
  const added = (diff.match(/^\+[^+]/gm) || []).length;
  const removed = (diff.match(/^\-[^\-]/gm) || []).length;
  lineCount = added + removed;
} catch { lineCount = changedFiles.length * 10; /* 估算 */ }

// ─── 阈值判断：仅重度变更提醒 ────────────────────────────────────
const isHeavy = changedFiles.length >= 3 || lineCount >= 50;
if (!isHeavy) {
  console.log(JSON.stringify({ decision: 'approve' }));
  process.exit(0);
}

// ─── 变更类型分析 ────────────────────────────────────────────────
const hasBackend = changedFiles.some(f => /\.(cs|csproj)$/.test(f));
const hasFrontend = changedFiles.some(f => /\.(vue|ts|tsx|less|css)$/.test(f) && /^jnpf-web/.test(f));
const hasInfra = changedFiles.some(f => /\.claude[\\/]/.test(f));

// ─── 构建提醒 ────────────────────────────────────────────────────
const reminders = [];

// 强制性流程技能（所有重度变更）
reminders.push('🔴 强制性流程（违反 = Supreme Iron Law 未通过）：');
if (hasBackend || hasFrontend) {
  reminders.push('  1. 调用 Skill 工具 → superpowers:brainstorming（确认意图和设计方向）');
  reminders.push('  2. 编码完成后 → superpowers:verification-before-completion（Gate Function 验证）');
}
reminders.push('  3. 任何 bug/异常 → superpowers:systematic-debugging（Phase 1-4 根因调查）');
reminders.push('  4. 声称"完成/已修复/已验证"前 → verification-before-completion（跑命令+读输出+确认证据）');

// 领域专属技能
if (hasBackend) {
  reminders.push('');
  reminders.push('🟡 后端变更专属：');
  reminders.push('  - 写 C# 前 → 读 .claude/rules/architecture-redlines.md + jnpf-expert-traps.md');
  reminders.push('  - 修改 Service 方法签名 → 用 Serena MCP find_referencing_symbols 检查调用方');
  reminders.push('  - 新 API 端点 → 确认已声明 [AllowAnonymous]/[SecurityDefine]（R8 红线）');
}

if (hasFrontend) {
  reminders.push('');
  reminders.push('🟡 前端变更专属：');
  reminders.push('  - 写 Vue 前 → 读 .claude/rules/jnpf-frontend-rules.md');
  reminders.push('  - 自定义页面 → 调用 jnpf-ui-enhance 技能');
  reminders.push('  - 有 SSE/EventSource → 读 .claude/rules/frontend-memory-leak.md（R6 红线）');
  reminders.push('  - 视觉变更后 → 调用 playwright 技能产出 E1 截图至 .claude/evidence/');
}

if (hasInfra) {
  reminders.push('');
  reminders.push('🟡 基础设施变更专属：');
  reminders.push('  - Hook 修改后 → 运行 node scripts/test-hooks.mjs');
  reminders.push('  - 规则文件修改 → 检查 CLAUDE.md On-Demand Rules 引用链');
}

// 代码审查
if (changedFiles.length >= 3 || lineCount >= 50) {
  reminders.push('');
  reminders.push('🟡 触发审查门禁：');
  reminders.push('  - spawn test-runner 子代理（Agent tool, subagent_type="test-runner"）');
  reminders.push('  - test-runner PASS 后 → spawn code-reviewer 子代理');
  reminders.push('  - 或调用 full-review skill 一键执行三阶段审查');
}

reminders.push('');
reminders.push('⚠️ 以上所有步骤 MUST 在向用户报告"完成"前执行。跳过 = 违反 R9/R10 红线。');

const systemMessage = `[SKILL MANDATE] 刚修改了 ${changedFiles.length} 个文件（~${lineCount} 行）。backend=${hasBackend}, frontend=${hasFrontend}。\n${reminders.join('\n')}`;

console.log(JSON.stringify({ decision: 'approve', systemMessage }));
process.exit(0);
