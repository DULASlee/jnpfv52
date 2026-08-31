# POLICY-002 — Real Build Required (CONDITIONAL @1.0)

**id:** `P002@1.0`
**type:** Conditional — Hard for Execute, AuditOnly for audit mode
**scope:** [refactoring, feature, bugfix] — audit / docs-only → AuditOnly (no build required)
**applicability:** taskType=Execute, mode != audit, phase=P1+
**severity:** Conditional Hard
**enforcementPoint:** PreBuildHook + Stop (pre-completion)
**requires:**
- evidence.evidenceType = `REAL_BUILD`
- evidence.policy_id = `P002`
- evidence.policy_version = `1.0`
- evidence.exitCode = 0
- evidence.timestamp < 30min
- evidence.result = ALLOW
**onViolation:** BLOCK exit 2 + hint `dotnet build` or `pnpm build`; writes `build-evidence.json` on success

## Rule

Without fresh real build evidence (exit 0, <30min, structured), `Completion = BLOCK`.

## Enforcement

PreBuildHook immediate check; Stop hook re-check before completion.

## Evidence

Producer: `evidence-collector.mjs::collectBuildEvidence` — 11-field structured: `REAL_BUILD, Actor, Task, Stage=build, Policy=P002, Action=dotnet build, Tool=dotnet, Result=ALLOW/BLOCK, Timestamp, Integrity, exitCode, logTail`

Gate Requires: `evidence.evidenceType=REAL_BUILD & exitCode=0 & policy_version=1.0`

## Determinism

same(BuildEvidence, PolicyVersion) → same decision; time-dependent via TTL 30min (explicit)

## Bypass

Direct `dotnet build` skipping hook → still requires evidence file; fake evidence with wrong type/version → BLOCK
