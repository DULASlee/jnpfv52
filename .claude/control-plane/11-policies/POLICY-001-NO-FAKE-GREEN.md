# POLICY-001 — No Fake Green (HARD @1.0)

**id:** `P001@1.0`
**type:** Hard — always BLOCK, never AuditOnly
**scope:** [refactoring, feature, bugfix] — README-only / docs-only changes exempt (Minimum Sufficient Thought)
**applicability:** taskType=Execute|Verify, phase=P1+, mode != audit
**severity:** Hard
**enforcementPoint:** PreMutationHook (PreToolUse Write/Edit/MultiEdit)
**files:** `*.cs`, `*.ts`, `*.vue`, `*.test.*`, `*Tests.cs`, `*Test.cs`, `__tests__/**`
**requires:**
- evidence.type = `P001_ASSERT_INTEGRITY`
- evidence.policy_id = `P001`
- evidence.policy_version = `1.0`
- evidence.result = BLOCK or ALLOW with Before/After counts
**onViolation:** BLOCK exit 2 + structured evidence `09-evidence/p001-fake-green.json` (11 fields + policy_id/version/decision)

## Prohibits

- 修改 Assertion 使通过率虚高 (weaken: old assert count > new assert count)
- Skip/Delete Test (`[Fact(Skip=...)]`, `.skip(`, `it.skip`, `test.skip`, `// skip`)
- Mock 替代真实验证 (replace real call with mock without real call)
- 删除测试文件以消灭失败

## Condition (Evaluator)

```
oldAsserts = countAsserts(oldContent)
newAsserts = countAsserts(newContent)
if newAsserts < oldAsserts - 1 → BLOCK (allow diff -1 for refactor noise)
if hasSkip(newContent) && !hasSkip(oldContent) → BLOCK
if mockReplacesReal(newContent, oldContent) → BLOCK
```

## Enforcement

PreMutationHook — immediate BLOCK before Write, not delayed to Final Gate.

## Evidence (11-field structured)

`EvidenceType=P001_ASSERT_INTEGRITY, Actor=agent, Task=P1, Stage=mutation, Policy=P001, Action=edit, Before=oldAsserts, After=newAsserts, Tool=hook, Result=BLOCK, Timestamp, Integrity=sha256, policy_id=P001, policy_version=1.0, decision=BLOCK`

## Migration

Superpowers `verification-before-completion` → Principle: Evidence before claim → Policy → Hook → Test

## Determinism

same(Task,Phase,Context,Evidence,PolicyVersion=1.0) → same decision (no time dependence)

## Test

Adversarial: weaken 5→2 asserts → BLOCK; add Skip → BLOCK; delete test file → BLOCK; Bypass: direct File API without hook → BLOCK
