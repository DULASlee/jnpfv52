#!/usr/bin/env node
/**
 * Cursor sessionStart: episodic sync + 宪法级最高约束注入（对抗衰减）
 */
import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import { loadManifest } from '../../scripts/toolchain-lib.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');
const syncScript = path.join(repoRoot, 'scripts', 'episodic-sync.mjs');
const { episodic_project_id: projectId, project_slug: slug, docs } = loadManifest(repoRoot);

spawn(process.execPath, [syncScript, '--background'], {
  detached: true,
  stdio: 'ignore',
  windowsHide: true,
}).unref();

const context = [
  '<CONSTITUTION-PRIORITY>',
  '最高约束（先于一切实现）：',
  '1. Q1–Q3 业务锚定；答不出禁止编码。',
  '2. S/A：workflow-state adfPhase=P0→P3 禁止写业务源码（L12）；用户「继续」后升到 P4 才实现。B级 adfPhase=exempt。',
  '3. 四支柱①硬门：可审批前写 pillar-claim-current.json + pillar-claim-check.mjs --force。',
  '4. 零占位符硬拦；唯一 alwaysApply=.cursor/rules/00-constitution.mdc。',
  '详规入口：.cursor/rules/00-constitution.mdc',
  '</CONSTITUTION-PRIORITY>',
  '',
  '<EPISODIC-MEMORY-AUTOMATION>',
  `本项目 episodic-memory 已启用（project=${projectId}，slug=${slug}）。sessionStart/stop 触发 sync。`,
  '',
  '**会话开始必做（第一轮流式回复前）**：',
  `1. MCP episodic-memory \`search\`：project=\`${projectId}\`，query 见 \`.cursor/episodic/search-templates.yaml\``,
  '2. 对 top 2-3 命中用 `read` 只读相关行段',
  '3. 读推进清单待审项 + 相关 `openspec/specs/`',
  '4. 非 trivial 任务走 Superpowers（brainstorming → writing-plans → executing-plans）',
  '',
  '**阶段完成**：verification → progress-registry + 推进清单 LOG → 定稿写入 openspec/specs/',
  '',
  `Playbook: ${docs?.playbook || 'docs/toolchain/SETUP.md'}`,
  'Manifest: .cursor/toolchain.manifest.json',
  '</EPISODIC-MEMORY-AUTOMATION>',
].join('\n');

process.stdout.write(JSON.stringify({ additional_context: context }));
process.exit(0);
