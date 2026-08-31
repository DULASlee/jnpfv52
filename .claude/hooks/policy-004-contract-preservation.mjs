#!/usr/bin/env node
/**
 * P004 — Contract Preservation (HARD @1.0)
 * EnforcementPoint: PreMutationHook (frozen path)
 * Scope: frozen contracts: 08-phase-contracts/*, 00-governance/L0-LAWS.md, GOVERNANCE-INDEX, MASTER, HUMAN-GATE-RULES
 * Requires: cr-approved in workflow-state.json
 */

import fs from 'node:fs';
import path from 'node:path';

let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

const filePath = (process.env.CLAUDE_FILE_PATH || input.tool_input?.file_path || '').replace(/\\/g, '/');
if (!filePath) process.exit(0);

// Frozen contract patterns
const frozenPatterns = [
  /08-phase-contracts\//,
  /00-governance\/L0-LAWS\.md/,
  /00-governance\/GOVERNANCE-INDEX\.md/,
  /00-governance\/MASTER-GOVERNANCE\.md/,
  /00-governance\/HUMAN-GATE-RULES\.yaml/,
  /00-governance\/HARNESS-INVENTORY\.md/,
  /00-governance\/HARNESS-AUTHORITY-MAP\.md/,
];

const isFrozen = frozenPatterns.some(p => p.test(filePath));
if (!isFrozen) process.exit(0);

// Check cr-approved (allow crApproved camelCase too)
let approved = null;
try {
  const wfPath = path.join(process.cwd(), '.claude/workflow-state.json');
  if (fs.existsSync(wfPath)) {
    const wf = JSON.parse(fs.readFileSync(wfPath, 'utf-8'));
    approved = wf['cr-approved'] || wf['crApproved'] || wf.crApproved || null;
  }
} catch { approved = null; }

// Also check // cr-safe: <reason> in content for trivial edits (like formatting)
let content = input.tool_input?.content || input.tool_input?.new_string || input.tool_input?.newText || '';
if (/cr-safe\s*:/i.test(content)) process.exit(0);

if (!approved) {
  const { collectContractEvidence } = await import('./evidence-collector.mjs');
  collectContractEvidence(filePath, null, 'BLOCK');
  console.error(`BLOCKED P004@1.0 Contract Preservation — frozen contract ${filePath} without cr-approved`);
  console.error('  Hard Policy: frozen contract mutation requires Change Request approval (workflow-state.json: cr-approved)');
  console.error('  For trivial formatting, add // cr-safe: <reason> to content');
  process.exit(2);
}

process.exit(0);
