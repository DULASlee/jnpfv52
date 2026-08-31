#!/usr/bin/env node
/**
 * policy-lib.mjs — Shared pure functions for Phase 1 Policies
 * Phase 1 Vertical Slice — Task 2
 * Principles: Structured Evidence (11 fields), Determinism, Versioning, AgentOS State boundary
 */

import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const ROOT = process.cwd();

// --- P001: assert counting ---
export function countAsserts(content) {
  if (!content) return 0;
  const csAsserts = (content.match(/Assert\.(Equal|True|False|NotNull|NotEmpty|Throws|Contains|DoesNotContain|Single|Empty)/g) || []).length;
  const tsAsserts = (content.match(/\bassert\.(equal|true|false|ok|strictEqual|deepEqual|throws|doesNotThrow)/gi) || []).length;
  const expectAsserts = (content.match(/\bexpect\(/g) || []).length;
  return csAsserts + tsAsserts + expectAsserts;
}

export function hasSkip(content) {
  if (!content) return false;
  return /\bSkip\s*=/i.test(content) || /\bskip\s*\(/.test(content) || /it\.skip|test\.skip|describe\.skip/i.test(content) || /\[Fact\(Skip/.test(content);
}

export function isTestFile(filePath) {
  return /\.test\.(ts|js|cs)|__tests__|\.Tests\.cs$|\.Test\.cs$|Tests\.cs$/i.test(filePath);
}

export function mockReplacesReal(newContent, oldContent) {
  if (!newContent || !oldContent) return false;
  const hadReal = /new\s+\w+Service|_service\.\w+|await\s+\w+\.\w+\(/.test(oldContent);
  const hasMock = /Mock|jest\.fn|vi\.fn|Substitute\./.test(newContent);
  const lostReal = !/await\s+\w+\.\w+\(/.test(newContent) && hadReal;
  return hasMock && lostReal;
}

// --- P002: build evidence ---
export function hasBuildEvidence(maxAgeMs = 30 * 60 * 1000) {
  const p = path.join(ROOT, '.claude/control-plane/09-evidence/build-evidence.json');
  if (!fs.existsSync(p)) return false;
  try {
    const j = JSON.parse(fs.readFileSync(p, 'utf-8'));
    if (j.evidenceType !== 'REAL_BUILD') return false;
    if (j.policy_id !== 'P002' || j.policy_version !== '1.0') return false;
    if (j.exitCode !== 0) return false;
    if (j.result !== 'ALLOW') return false;
    const age = Date.now() - new Date(j.timestamp).getTime();
    return age < maxAgeMs;
  } catch { return false; }
}

// --- Structured Evidence (11 fields) ---
export function writeEvidence(dir, name, data) {
  const payload = {
    evidenceType: data.evidenceType || data.policy || 'UNKNOWN',
    actor: data.actor || 'agent',
    task: data.task || 'P1',
    stage: data.stage || 'verify',
    policy: data.policy || data.policy_id || 'UNKNOWN',
    policy_id: data.policy_id || data.policy || 'UNKNOWN',
    policy_version: data.policy_version || '1.0',
    action: data.action || 'check',
    before: data.before !== undefined ? String(data.before).slice(0, 500) : undefined,
    after: data.after !== undefined ? String(data.after).slice(0, 500) : undefined,
    tool: data.tool || 'hook',
    result: data.result || 'BLOCK',
    timestamp: new Date().toISOString(),
    integrity: '',
    decision: data.decision || data.result || 'BLOCK',
    exitCode: data.exitCode,
    reason: data.reason,
    file: data.file,
    // keep any extra fields
    ...data,
  };
  // integrity: sha256 of canonical decision fields
  const canonical = JSON.stringify({ policy: payload.policy_id, version: payload.policy_version, result: payload.result, evidenceType: payload.evidenceType, file: payload.file });
  payload.integrity = 'sha256:' + crypto.createHash('sha256').update(canonical).digest('hex').slice(0, 16);

  const d = path.join(ROOT, dir);
  fs.mkdirSync(d, { recursive: true });
  fs.writeFileSync(path.join(d, name), JSON.stringify(payload, null, 2), 'utf-8');
  return payload;
}

// --- Determinism ---
export function isDeterministicKey(task, phase, context, evidence, policyVersion) {
  return JSON.stringify({ task, phase, context, evidence, policyVersion });
}

// --- Version ---
export const POLICY_VERSION = '1.0';
export const POLICIES = ['P001@1.0','P002@1.0','P003@1.0','P004@1.0','P005@1.0'];
