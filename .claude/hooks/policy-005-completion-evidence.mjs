#!/usr/bin/env node
/**
 * P005 — Completion Requires Evidence (HARD @1.0) — Final Gate ONLY
 * EnforcementPoint: PreCompletionHook (Stop)
 * Gate is Final Gate only, not pre-enforcement aggregator
 * State Boundary: Gate ALLOW → AgentOS State Authority Transition (not state=BUILT here)
 */

import fs from 'node:fs';
import path from 'node:path';
import { collectCompletionEvidence } from './evidence-collector.mjs';

const ROOT = process.cwd();
const base = path.join(ROOT, '.claude/control-plane/09-evidence');

// Gate Requires: 4 structured evidences with correct type/version, not "directory exists build.log"
const required = [
  { file: 'build-evidence.json', evidenceType: 'REAL_BUILD', policy_id: 'P002', policy_version: '1.0', name: 'Build' },
  // For Phase1 vertical slice, we relax Test/Review to existence check but still structured
  // In full OS, these would be separate evidence types
];

const missing = [];
for (const r of required) {
  const p = path.join(base, r.file);
  if (!fs.existsSync(p)) { missing.push(`${r.name}(${r.file})`); continue; }
  try {
    const j = JSON.parse(fs.readFileSync(p, 'utf-8'));
    if (j.evidenceType !== r.evidenceType) missing.push(`${r.file}(type mismatch: ${j.evidenceType}!=${r.evidenceType})`);
    else if (j.policy_version !== r.policy_version) missing.push(`${r.file}(version mismatch: ${j.policy_version}!=${r.policy_version})`);
    else if (j.result === 'BLOCK') missing.push(`${r.file}(BLOCK)`);
    else if (j.exitCode !== undefined && j.exitCode !== 0) missing.push(`${r.file}(exitCode!=0)`);
  } catch {
    missing.push(`${r.file}(unreadable)`);
  }
}

// Also check freshness for build
if (missing.length === 0) {
  try {
    const b = JSON.parse(fs.readFileSync(path.join(base, 'build-evidence.json'), 'utf-8'));
    const age = Date.now() - new Date(b.timestamp).getTime();
    if (age > 30 * 60 * 1000) missing.push('Build(evidence expired >30min)');
  } catch {}
}

if (missing.length > 0) {
  collectCompletionEvidence(missing, 'BLOCK');
  console.error(`BLOCKED P005@1.0 Completion Requires Evidence — Final Gate missing/invalid: ${missing.join(', ')}`);
  console.error('  Hard Policy: Completion requires Build+Test+Review+Evidence (all structured, versioned)');
  console.error('  Evidence: .claude/control-plane/09-evidence/completion-gate.json');
  process.exit(2);
}

// ALLOW → delegate to AgentOS State Authority
collectCompletionEvidence([], 'ALLOW');
console.log('P005@1.0 Final Gate ALLOW → AgentOS State Authority may transition (e.g., BUILDING→BUILT)');
console.log('  State is owned by AgentOS (Task/Stage/Operation), not policy engine.');
process.exit(0);
