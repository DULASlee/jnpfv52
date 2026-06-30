// guard-workflow.mjs — L0 HARD GATE (exit 2 = BLOCK)
// PreToolUse Write|Edit|MultiEdit → 强制 SP 技能调用

import { execSync } from 'child_process';
import { existsSync, readFileSync } from 'fs';
import { join } from 'path';

const CODE_EXTS = ['.cs', '.vue', '.ts', '.tsx', '.js', '.jsx', '.css', '.less', '.scss'];
const STDIN_MS = 3000;

const MANDATORY_SP = [
  { key: 'brainstorming', minEdits: 0, msg: 'Skill("superpowers:brainstorming")' },
  { key: 'writing-plans', minEdits: 2, level: ['S', 'A'], msg: 'Skill("superpowers:writing-plans")' },
  { key: 'executing-plans', minEdits: 2, level: ['S', 'A'], any: ['executing-plans', 'subagent-driven-development', 'dispatching-parallel-agents', 'using-git-worktrees'], msg: 'Phase4: executing-plans|subagent-driven-development|dispatching-parallel-agents|using-git-worktrees 四选一' },
  { key: 'verification-before-completion', minEdits: 4, msg: 'Skill("superpowers:verification-before-completion")' },
  { key: 'requesting-code-review', minEdits: 5, any: ['requesting-code-review', 'full-review', 'security-review', 'health-check'], msg: 'Phase6: requesting-code-review|full-review|security-review|health-check 四选一' },
  { key: 'receiving-code-review', minEdits: 6, msg: 'Skill("superpowers:receiving-code-review")' },
  { key: 'pre-commit', minEdits: 99, msg: 'Skill("pre-commit")' },
];

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

function readState() {
  try {
    const p = join(getProjectRoot(), '.claude', 'workflow-state.json');
    return existsSync(p) ? JSON.parse(readFileSync(p, 'utf8')) : null;
  } catch {
    return null;
  }
}

async function readStdin(ms = STDIN_MS) {
  return Promise.race([
    (async () => {
      const chunks = [];
      for await (const c of process.stdin) chunks.push(c);
      return Buffer.concat(chunks).toString('utf-8');
    })(),
    new Promise((_, reject) => setTimeout(() => reject(new Error('stdin timeout')), ms)),
  ]);
}

try {
  let input = {};
  try {
    const raw = await readStdin();
    if (raw.trim()) input = JSON.parse(raw);
  } catch {
    process.exit(0);
  }

  const fp = (
    input?.tool_input?.file_path
    || input?.file_path
    || input?.files?.[0]?.file_path
    || ''
  ).replace(/\\/g, '/');

  // 自举免疫：hooks/rules/state 目录不触发 workflow 门禁
  if (!fp || /\.claude\//.test(fp)) {
    process.exit(0);
  }

  if (!CODE_EXTS.some((e) => fp.toLowerCase().endsWith(e))) {
    process.exit(0);
  }

  const s = readState();
  if (!s?.sp) {
    console.error('⛔ 工作流未启动 → Phase1 + Skill("superpowers:brainstorming") + 更新 .claude/workflow-state.json');
    process.exit(2);
  }

  const sp = s.sp || {};
  const lvl = s.level || 'B';
  const ec = s.editCount || 0;

  for (const g of MANDATORY_SP) {
    if (g.level && !g.level.includes(lvl)) continue;
    if (ec < g.minEdits) continue;
    const keys = g.any || [g.key];
    if (!keys.some((k) => sp[k])) {
      console.error(`⛔ ${g.key} 未调用 (${ec}次编辑/${lvl}级)`);
      console.error(`   MUST: ${g.msg} → 更新 state.sp.{${keys.join('|')}}=true`);
      process.exit(2);
    }
  }

  if (sp['systematic-debugging'] && !sp['data-driven-debug']) {
    console.error('⛔ Debug: systematic-debugging 已调用但 data-driven-debug 未调用');
    process.exit(2);
  }

  process.exit(0);
} catch (e) {
  console.error(e.message);
  process.exit(1);
}
