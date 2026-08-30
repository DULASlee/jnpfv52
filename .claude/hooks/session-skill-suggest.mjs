#!/usr/bin/env node
/**
 * session-skill-suggest.mjs — SessionStart 上下文感知 skill 推荐
 *
 * 触发: SessionStart（与 session-scheduler 并列）
 * 数据: 当前路径 + 最近 git 改动 + ECC Vault 最新 memory + 当前 Phase
 * 输出: additionalContext（注入会话开头），告诉 LLM 当前最可能需要的 skill
 *
 * 不自动加载 skill——只推荐。
 */

import { execSync } from 'child_process';
import { existsSync, readFileSync } from 'fs';
import { join, basename } from 'path';

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

function detectContext(root) {
  const ctx = {
    phase: null,
    hasOpenSpec: existsSync(`${root}/openspec`),
    hasPhase8: existsSync(`${root}/docs/universal/Phase-8`),
    hasEccVault: existsSync(`${root}/.ecc/memory/project`),
    hasMistakeLog: existsSync(`${root}/.claude/memory/mistake-log.md`),
    recentFiles: [],
  };

  // Detect Phase
  if (ctx.hasPhase8) ctx.phase = 'Phase-8';
  else if (existsSync(`${root}/docs/universal/Phase-7-Final-Report.md`)) ctx.phase = 'Phase-7';

  // Recent files via git (last 10 changed)
  try {
    const diff = execSync('git diff --name-only HEAD~3 HEAD 2>/dev/null || git diff --name-only --cached 2>/dev/null || echo ""', {
      encoding: 'utf-8', cwd: root, timeout: 3000,
    });
    ctx.recentFiles = diff.split('\n').filter(Boolean).slice(0, 10);
  } catch {}

  return ctx;
}

function suggestSkills(ctx) {
  const skills = [];

  // Phase 8 → table-refactor-expert + production-audit
  if (ctx.phase === 'Phase-8' || ctx.hasPhase8) {
    skills.push({
      name: 'table-refactor-expert',
      reason: 'Phase 8 表级重构：评估/设计/重构/验证/关闭单表',
      priority: 1,
    });
    skills.push({
      name: 'production-audit',
      reason: 'P8-B Controlled Production 准备就绪审计',
      priority: 2,
    });
  }

  // ECC Vault 上下文 → unified-memory
  if (ctx.hasEccVault) {
    skills.push({
      name: 'unified-memory',
      reason: '项目已配置 ECC Memory Vault（6+ memory 可 recall）',
      priority: 1,
    });
  }

  // OpenSpec → openspec-*
  if (ctx.hasOpenSpec) {
    skills.push({
      name: 'openspec-apply-change',
      reason: '检测到 openspec/ 目录，可能需要 apply proposed changes',
      priority: 3,
    });
  }

  // 最近改的文件含 JNPF .cs/.vue → dotnet-patterns
  const codeTouched = ctx.recentFiles.some(f => /\.(cs|vue|ts)$/.test(f));
  if (codeTouched) {
    skills.push({
      name: 'dotnet-patterns',
      reason: '最近有 .cs/.vue/.ts 文件改动，DI/async/conventions',
      priority: 2,
    });
  }

  // Mistake log 存在 → verification-loop
  if (ctx.hasMistakeLog) {
    skills.push({
      name: 'verification-loop',
      reason: '检测到 mistake-log.md，完成工作前先验证',
      priority: 2,
    });
  }

  // Agent / skill 健康 → skill-stocktake
  if (skills.length === 0) {
    skills.push({
      name: 'skill-stocktake',
      reason: '无明确任务上下文，audit 当前 skills 质量',
      priority: 3,
    });
  }

  // Sort by priority
  skills.sort((a, b) => a.priority - b.priority);
  return skills.slice(0, 4); // top 4
}

function getRecentEccMemories(root, limit = 3) {
  const indexFile = join(root, '.ecc', 'memory', 'project', '.index.json');
  // Use ecc CLI search instead — too heavy for hook
  return [];
}

// ─── 主流程 ─────────────────────────────────────────────
let input = {};
try {
  const raw = readFileSync(0, 'utf-8').trim();
  if (raw) input = JSON.parse(raw);
} catch {}

const ROOT = getProjectRoot();
const ctx = detectContext(ROOT);
const skills = suggestSkills(ctx);

const STAMP = new Date().toISOString();
console.error(`[session-skill-suggest ${STAMP}] phase=${ctx.phase} suggested=${skills.length} skills`);

const skillLines = skills.map((s, i) =>
  `${i + 1}. **${s.name}** (P${s.priority}) — ${s.reason}`
).join('\n');

const additionalContext = `<SKILL-SUGGEST>
Phase/context detected: ${ctx.phase || 'unknown'} (cwd=${basename(ROOT)})

Top skills to consider for THIS session (按优先级排序):
${skillLines}

Rules:
- Load via skill tool ONLY when task matches description
- Do NOT load all of them upfront (token cost)
- After loading, read the skill description carefully to know when to use it
</SKILL-SUGGEST>`;

console.log(JSON.stringify({
  hookSpecificOutput: {
    hookEventName: 'SessionStart',
    additionalContext,
  },
}));

process.exit(0);