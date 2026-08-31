#!/usr/bin/env node
/**
 * evidence-collector.mjs — Structured Evidence Producer (NOT log collector)
 * Phase 1 Vertical Slice — Task 2, Calibration 3
 * Produces 11-field structured evidence with Gate Requires relation
 */

import { writeEvidence } from './policy-lib.mjs';

export function collectBuildEvidence(exitCode, logTail, actor = 'agent', task = 'P1') {
  return writeEvidence('.claude/control-plane/09-evidence', 'build-evidence.json', {
    evidenceType: 'REAL_BUILD',
    policy: 'P002',
    policy_id: 'P002',
    policy_version: '1.0',
    actor,
    task,
    stage: 'build',
    action: 'dotnet build',
    tool: 'dotnet',
    result: exitCode === 0 ? 'ALLOW' : 'BLOCK',
    decision: exitCode === 0 ? 'ALLOW' : 'BLOCK',
    exitCode,
    logTail: (logTail || '').slice(-800),
  });
}

export function collectMutationEvidence(policy_id, before, after, actor = 'agent', task = 'P1', file = '') {
  return writeEvidence('.claude/control-plane/09-evidence', `mutation-${Date.now()}.json`, {
    evidenceType: 'MUTATION',
    policy: policy_id,
    policy_id,
    policy_version: '1.0',
    actor,
    task,
    stage: 'mutation',
    action: 'write',
    before: (before || '').slice(0, 500),
    after: (after || '').slice(0, 500),
    tool: 'hook',
    result: 'ALLOW',
    decision: 'ALLOW',
    file,
    diffStat: `before:${(before||'').length} after:${(after||'').length}`,
  });
}

export function collectFakeGreenEvidence(reason, file, beforeCount, afterCount, actor = 'agent', task = 'P1') {
  return writeEvidence('.claude/control-plane/09-evidence', 'p001-fake-green.json', {
    evidenceType: 'P001_ASSERT_INTEGRITY',
    policy: 'P001',
    policy_id: 'P001',
    policy_version: '1.0',
    actor,
    task,
    stage: 'mutation',
    action: 'edit',
    before: String(beforeCount),
    after: String(afterCount),
    tool: 'hook',
    result: 'BLOCK',
    decision: 'BLOCK',
    reason,
    file,
  });
}

export function collectContractEvidence(file, crApproved, result = 'BLOCK') {
  return writeEvidence('.claude/control-plane/09-evidence', 'contract-guard.json', {
    evidenceType: 'CONTRACT_GUARD',
    policy: 'P004',
    policy_id: 'P004',
    policy_version: '1.0',
    actor: 'agent',
    task: 'P1',
    stage: 'mutation',
    action: 'write-frozen',
    tool: 'hook',
    result,
    decision: result,
    file,
    crApproved: crApproved || null,
  });
}

export function collectCompletionEvidence(missing, result = 'BLOCK') {
  return writeEvidence('.claude/control-plane/09-evidence', 'completion-gate.json', {
    evidenceType: 'COMPLETION',
    policy: 'P005',
    policy_id: 'P005',
    policy_version: '1.0',
    actor: 'agent',
    task: 'P1',
    stage: 'completion',
    action: 'gate-evaluate',
    tool: 'gate',
    result,
    decision: result,
    missing,
  });
}
