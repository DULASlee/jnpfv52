#!/usr/bin/env node
/**
 * P002 — Real Build Required (CONDITIONAL @1.0)
 * EnforcementPoint: PreBuildHook + Stop (pre-completion)
 * Scope: refactoring/feature/bugfix; audit→AuditOnly
 * Requires: evidenceType=REAL_BUILD & policy_id=P002 & policy_version=1.0 & exitCode=0 & timestamp<30min
 */

import { hasBuildEvidence } from './policy-lib.mjs';

const mode = (process.env.TASK_MODE || process.env.TASK_TYPE || 'execute').toLowerCase();
if (mode === 'audit' || mode === 'auditonly') {
  console.log('P002@1.0 AuditOnly — no build required (scope exempt per Minimum Sufficient Thought)');
  process.exit(0);
}

if (!hasBuildEvidence(30 * 60 * 1000)) {
  console.error('BLOCKED P002@1.0 Real Build Required — no fresh structured build evidence (REAL_BUILD & exit0 & <30min & version 1.0)');
  console.error('  Conditional Policy: refactoring/feature/bugfix requires real build. Run: dotnet build (backend/) or pnpm build (jnpf-web-vue3/)');
  console.error('  Evidence: .claude/control-plane/09-evidence/build-evidence.json');
  console.error('  Bypass with fake evidence (wrong type/version) → BLOCK (Gateway checks structured relation)');
  process.exit(2);
}

process.exit(0);
