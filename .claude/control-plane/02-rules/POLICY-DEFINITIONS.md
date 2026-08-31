# POLICY-DEFINITIONS (Machine-readable — 5 Policies @1.0)

> Authority: `.claude/control-plane/11-policies/` is authoritative source. This file is machine index.

## P001@1.0 — No Fake Green

```yaml
id: P001@1.0
type: Hard
scope: [refactoring, feature, bugfix] # README-only exempt
applicability: { taskType: [Execute, Verify], phase: P1+, mode: "!audit" }
trigger: PreToolUse Write|Edit|MultiEdit
enforcementPoint: PreMutationHook
files: ["*.cs", "*.ts", "*.vue", "*.test.*", "*Tests.cs"]
detect: [assert-weaken, test-delete, skip-add, mock-replace]
requires:
  evidenceType: P001_ASSERT_INTEGRITY
  policy_id: P001
  policy_version: "1.0"
onViolation: BLOCK exit2
evidence:
  fields: [EvidenceType, Actor, Task, Stage, Policy, Action, Before, After, Tool, Result, Timestamp, Integrity]
  producer: evidence-collector.mjs
gate:
  requires: evidence.type=P001_ASSERT_INTEGRITY & result!=BLOCK
determinism: same(Task,Phase,Context,Evidence,PolicyVersion) -> same decision
```

## P002@1.0 — Real Build Required

```yaml
id: P002@1.0
type: Conditional # Hard for Execute, AuditOnly for audit
scope: [refactoring, feature, bugfix] # audit->AuditOnly
applicability: { taskType: Execute, mode: "!audit" }
trigger: Stop + PreBuildHook
requires:
  evidenceType: REAL_BUILD
  policy_id: P002
  policy_version: "1.0"
  exitCode: 0
  maxAge: 1800000 # 30min
onViolation: BLOCK exit2
```

## P003@1.0 — Mutation Must Be Evidenced

```yaml
id: P003@1.0
type: Hard
scope: [all-writes] # except .gitignore, 09-evidence, .claude/memory transient
trigger: PreToolUse Write|Edit|MultiEdit
enforcementPoint: PreMutationHook
requires: [Before, After, Diff, Actor, Task] # 5-tuple via git diff + workflow-state
onViolation: BLOCK exit2
```

## P004@1.0 — Contract Preservation

```yaml
id: P004@1.0
type: Hard
scope: [frozen-contracts] # 08-phase-contracts/*, L0-LAWS, GOVERNANCE-INDEX, MASTER
trigger: PreToolUse Write on frozen path
requires: cr-approved in workflow-state.json
onViolation: BLOCK exit2
```

## P005@1.0 — Completion Requires Evidence

```yaml
id: P005@1.0
type: Hard
scope: [all-completions]
trigger: Stop # PreCompletionHook — Final Gate ONLY
enforcementPoint: PreCompletionHook
requires: [Build(REAL_BUILD+P002@1.0), Test, Review, Evidence] # 4 structured, versioned
onViolation: BLOCK exit2
stateBoundary: Gate ALLOW -> AgentOS State Authority Transition (not state=BUILT in policy engine)
```

---

## Global

- **Hook is Enforcement Point, not Policy Container** — Phase1 Hook-per-Policy is implementation strategy; future stable hooks PreMutation/PreBuild/PreTest/PreCompletion
- **Evidence is structured Producer, not log** — 11 fields + policy_id@version + Integrity
- **Determinism** — all policies: same(Task,Phase,Context,Evidence,PolicyVersion) -> same decision
- **Versioning** — @1.0, evidence records policy_id, policy_version, decision for replay
