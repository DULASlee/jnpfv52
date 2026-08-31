# GATE-COMPLETION — Final Gate ONLY (Phase 1 @1.0)

> **Boundary:** This Gate is Final Gate only, not pre-enforcement aggregator. Pre-action policies (P001/P003/P004) BLOCK immediately at PreMutation/PreBuild hooks. This Gate only verifies structured evidence convergence at Stop.

## Pre-conditions (Immediate Enforcement — already BLOCKed before reaching here)

- **P001 No Fake Green** → PreMutationHook: weaken assert/skip/mock → BLOCK (evidence: p001-fake-green.json)
- **P003 Mutation Evidence** → PreMutationHook: no 5-tuple → BLOCK
- **P004 Contract Preservation** → PreMutationHook frozen: no cr-approved → BLOCK
- **P002 Real Build** → PreBuildHook + Stop: no REAL_BUILD evidence <30min → BLOCK

## Final Gate Check (PreCompletionHook — Stop)

**GATE Requires (structured, versioned):**

```
evidence.file = build-evidence.json
  AND evidenceType = REAL_BUILD
  AND policy_id = P002
  AND policy_version = 1.0
  AND exitCode = 0
  AND result = ALLOW
  AND age < 30min
```

Missing/invalid → BLOCK + `completion-gate.json` (11 fields, policy P005@1.0)

## State Boundary

```
Gate ALLOW → AgentOS State Authority → Transition (e.g., BUILDING→BUILT, PENDING→COMPLETED)
```

Not: `state = COMPLETED` in gate. Phase 1 owns decision, AgentOS owns state (Task/Stage/Operation).

## Evidence

- `09-evidence/build-evidence.json` (P002@1.0)
- `09-evidence/completion-gate.json` (P005@1.0) — records missing[] or ALLOW

## Version

- Policy version @1.0 tracked in every evidence for replay
- Determinism: same(Task,Phase,Context,Evidence,PolicyVersion) → same decision

## Failure Action

- BLOCK exit 2 with audit log; Bypass (direct File API, fake evidence wrong type) → BLOCK
