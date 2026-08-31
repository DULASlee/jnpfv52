# POLICY-005 — Completion Requires Evidence (HARD @1.0) — Final Gate ONLY

**id:** `P005@1.0`
**type:** Hard
**scope:** all completions (Task/Phase/Stage completion claims)
**applicability:** Stop (pre-completion) — Final Gate only, not pre-enforcement aggregator
**enforcementPoint:** PreCompletionHook (Stop) — **Final Gate ONLY**
**requires:** Build + Test + Review + Evidence — 4 structured evidences each with correct type/version:
- `build-evidence.json` : evidenceType=REAL_BUILD & P002@1.0 & exitCode=0
- `test-evidence.json` or `p001` pass : P001/P002 related
- `review-evidence` : reviewer check (if applicable)
- `completion-gate.json` : P005 itself

All must be `policy_version=1.0` and Result=ALLOW

**onViolation:** BLOCK exit 2 + `completion-gate.json` with missing list

## Important Boundary

```
State Mutation (BUILDING→BUILT etc) is owned by AgentOS State Authority.
P005 Gate only decides ALLOW → State Authority transitions.
Phase 1 does NOT reinvent state.js — calls existing Task/Stage/Operation authority.
```

```
Gate
 ↓
ALLOW
 ↓
AgentOS State Authority
 ↓
Transition
```

Not: `state = BUILT` in policy engine.

## Evidence

`EvidenceType=COMPLETION, Policy=P005@1.0, Result=BLOCK/ALLOW, missing:[...], Timestamp, Integrity`

## Determinism & Versioning

same(Build,Test,Review,Evidence, PolicyVersion=1.0) → same decision
Evidence records `policy_id=P005, policy_version=1.0, decision`
