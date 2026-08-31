#!/usr/bin/env node
/**
 * P003 — Mutation Must Be Evidenced (HARD @1.0)
 * EnforcementPoint: PreMutationHook (PreToolUse Write/Edit/MultiEdit)
 * Scope: all writes except .gitignore, 09-evidence/**, .claude/memory/** transient, *.bak
 * Requires: Before/After/Diff/Actor/Task (5-tuple)
 */

import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';

let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

const filePath = (process.env.CLAUDE_FILE_PATH || input.tool_input?.file_path || '').replace(/\\/g, '/');
const toolName = process.env.CLAUDE_TOOL_NAME || input.tool_name || '';
let content = '';
if (toolName === 'Write') content = input.tool_input?.content || '';
else if (toolName === 'Edit') content = input.tool_input?.new_string || input.tool_input?.newText || '';
else if (toolName === 'MultiEdit') {
  const edits = input.tool_input?.edits || [];
  content = edits.map(e => e.new_string || e.newText || '').filter(Boolean).join('\n');
} else {
  content = input.tool_input?.content || input.tool_input?.new_string || input.tool_input?.newText || '';
}

if (!filePath) process.exit(0);

// Exempt: evidence dir itself, gitignore, memory transient, backups
if (/09-evidence\/|\.gitignore|\.claude\/memory\/|\.bak/.test(filePath)) process.exit(0);
if (!content) process.exit(0);

// --- BLOCK-002: Mutation Scope Binding — Target Artifact + Actual Diff ---
// Required binding: Task, Actor, Workspace/Mutation Boundary, Target Artifact, Actual Diff
// Global unrelated git diff MUST NOT satisfy P003
let targetArtifact = process.env.MUTATION_TARGET || '';
let workspace = process.env.MUTATION_WORKSPACE || '';
let task = process.env.MUTATION_TASK || '';
let actor = process.env.MUTATION_ACTOR || '';
// Also read from authoritative scope file (outside agent direct mutation, set by test harness)
try {
  const scopePath = path.join(process.cwd(), '.claude/control-plane/09-evidence/mutation-scope.json');
  if (fs.existsSync(scopePath)) {
    const scope = JSON.parse(fs.readFileSync(scopePath, 'utf-8'));
    targetArtifact = targetArtifact || scope.targetArtifact || scope.target || '';
    workspace = workspace || scope.workspace || scope.mutationBoundary || '';
    task = task || scope.task || '';
    actor = actor || scope.actor || '';
  }
} catch {}
try {
  const wf = JSON.parse(fs.readFileSync(path.join(process.cwd(), '.claude/workflow-state.json'), 'utf-8'));
  targetArtifact = targetArtifact || wf.targetArtifact || wf.target || '';
  task = task || wf.task || '';
  actor = actor || wf.actor || '';
} catch {}

// If targetArtifact is bound, enforce Actual Diff is for that target
if (targetArtifact) {
  const targetBase = path.basename(targetArtifact);
  const fileBase = path.basename(filePath);
  const fileNormalized = filePath.replace(/\\/g, '/');
  const targetNormalized = targetArtifact.replace(/\\/g, '/');
  // Check workspace boundary if set
  if (workspace && !fileNormalized.includes(workspace.replace(/\\/g, '/'))) {
    console.error(`BLOCKED P003@1.0 Mutation Scope — file ${filePath} outside workspace boundary ${workspace}`);
    console.error(`  Required: Target=${targetArtifact}, Workspace=${workspace}, Task=${task}, Actor=${actor}`);
    process.exit(2);
  }
  // Actual Diff must be for target artifact — unrelated file must fail
  if (fileBase !== targetBase && !fileNormalized.endsWith(targetNormalized) && fileNormalized !== targetNormalized) {
    console.error(`BLOCKED P003@1.0 Mutation Scope — unrelated mutation`);
    console.error(`  Target = ${targetArtifact}`);
    console.error(`  Changed = ${filePath}`);
    console.error(`  → BLOCK (global diff must not satisfy)`);
    process.exit(2);
  }
}

// Check Actual Diff for THIS file only — not global git diff --stat
let hasActualDiff = false;
let oldContent = '';
try { if (fs.existsSync(filePath)) oldContent = fs.readFileSync(filePath, 'utf-8'); } catch { oldContent = ''; }
if (oldContent !== content && content.trim()) hasActualDiff = true;
else {
  try {
    const fileDiff = execSync(`git diff --stat -- "${filePath}"`, { encoding: 'utf-8', timeout: 3000 });
    if (fileDiff.trim()) hasActualDiff = true;
    const stagedDiff = execSync(`git diff --cached --stat -- "${filePath}"`, { encoding: 'utf-8', timeout: 3000 });
    if (stagedDiff.trim()) hasActualDiff = true;
  } catch { hasActualDiff = false; }
}

if (!hasActualDiff) {
  console.error(`BLOCKED P003@1.0 Mutation Must Be Evidenced — no Actual Diff for target ${targetArtifact || filePath}`);
  console.error('  Required: Task/Actor/Workspace/Target/Actual Diff — unrelated file does not satisfy');
  process.exit(2);
}

// On ALLOW, produce structured MUTATION evidence (11 fields) — not just log
try {
  const { collectMutationEvidence } = await import('./evidence-collector.mjs');
  collectMutationEvidence('P003', oldContent, content, actor || 'agent', task || 'P1', filePath);
} catch {}
process.exit(0);
