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

// Frozen contract patterns — must match CONTRACT-BASELINE.json keys
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

// --- BLOCK-003: Authoritative Contract Baseline (outside agent mutation authority) ---
// Baseline is integrity-bound and traceable, stored in 00-governance/CONTRACT-BASELINE.json
// Agent-writable workflow-state.json cr-approved and // cr-safe textual marker MUST NOT be accepted
import crypto from 'node:crypto';
let baseline = null;
try {
  const baselinePath = path.join(process.cwd(), '.claude/control-plane/00-governance/CONTRACT-BASELINE.json');
  baseline = JSON.parse(fs.readFileSync(baselinePath, 'utf-8'));
} catch { baseline = null; }

let content = input.tool_input?.content || input.tool_input?.new_string || input.tool_input?.newText || '';
// Also read from env file path if content empty (for Write via file)
if (!content && filePath) {
  try { content = fs.readFileSync(filePath, 'utf-8'); } catch {}
}

// Compute hash of new content vs baseline
let isContractMutation = false;
if (baseline && baseline.hashes) {
  // Determine baseline key for this file
  let baselineKey = null;
  for (const k of Object.keys(baseline.hashes)) {
    if (filePath.endsWith(k) || filePath.includes(k)) { baselineKey = k; break; }
  }
  // If file is in 08-phase-contracts and not in baseline (new file), treat as mutation
  if (!baselineKey && /08-phase-contracts\//.test(filePath)) {
    isContractMutation = true;
  } else if (baselineKey) {
    const baselineHash = baseline.hashes[baselineKey];
    const newHash = crypto.createHash('sha256').update(content || '').digest('hex').slice(0, 16);
    if (newHash !== baselineHash) isContractMutation = true;
  }
} else {
  // No baseline → treat any frozen write as mutation (fail closed)
  isContractMutation = true;
}

if (isContractMutation) {
  // BLOCK: Contract Preservation — baseline mismatch, regardless of agent self-attested cr-approved or cr-safe
  // Do NOT check workflow-state cr-approved (agent-writable) and do NOT accept // cr-safe textual marker
  // Both MUST fail per BLOCK-003 directive
  const { collectContractEvidence } = await import('./evidence-collector.mjs');
  // Record that agent self-attested markers were present but ignored
  const hasCrSafe = /cr-safe\s*:/i.test(content);
  const wfCr = (()=>{ try{ const wf=JSON.parse(fs.readFileSync(path.join(process.cwd(), '.claude/workflow-state.json'),'utf-8')); return wf['cr-approved']||''; }catch{return ''}})();
  collectContractEvidence(filePath, wfCr || (hasCrSafe ? 'cr-safe' : null), 'BLOCK');
  console.error(`BLOCKED P004@1.0 Contract Preservation — frozen contract ${filePath} integrity mismatch vs authoritative baseline`);
  console.error(`  Baseline: CONTRACT-BASELINE.json hashes (authoritative, outside agent authority)`);
  console.error(`  Agent self-attested: workflow-state cr-approved="${wfCr}" ${hasCrSafe ? 'cr-safe marker present' : ''} → IGNORED (not authoritative)`);
  console.error(`  Required: Baseline update via legitimate CR process, not workflow-state mutation`);
  process.exit(2);
}

// ALLOW: hash matches baseline (no mutation) or baseline updated authoritatively — write traceable evidence
try {
  const { collectContractEvidence } = await import('./evidence-collector.mjs');
  collectContractEvidence(filePath, 'baseline-match', 'ALLOW');
} catch {}
process.exit(0);
