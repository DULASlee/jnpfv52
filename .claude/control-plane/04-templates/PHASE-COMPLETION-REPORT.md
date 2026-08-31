# Phase Completion Report Template

> **目的：** 定义 Phase 完成报告的标准模板

---

```markdown
# Phase N Completion Report: [Phase Name]

## Metadata

```yaml
completion:
  phaseId: "X-Y"
  phaseName: "Phase Name"
  status: "COMPLETED"  # IN_PROGRESS / COMPLETED / BLOCKED
  completedAt: "YYYY-MM-DD"
  testingProfile: "STRICT-TDD"  # 或 CONTRACT-FIRST-TDD
```

---

## 1. Objective

本 Phase 解决什么问题。

## 2. Discovery

发现了什么：
- 发现 1
- 发现 2

## 3. Architecture Decision

采用什么设计，为什么。

## 4. Scope

### 包含

- Item 1
- Item 2

### 不包含

- Item 1
- Item 2

## 5. Specification

设计契约摘要。

## 6. Implementation

实际修改摘要。

## 7. TDD

### Testing Profile

```yaml
testingProfile: STRICT-TDD
```

### Test Coverage

| Type | Count | Pass Rate |
|------|-------|-----------|
| Unit | 0 | 0% |
| Contract | 0 | 0% |
| Integration | 0 | 0% |
| Negative | 0 | 0% |

## 8. Verification

### Build

- Project Build: PASS/FAIL
- Solution Build: PASS/FAIL

### Tests

- Target Tests: PASS/FAIL
- Regression Tests: PASS/FAIL

### Architecture

- Dependency Direction: PASS/FAIL
- Layer Boundary: PASS/FAIL

### API

- API Surface: PASS/FAIL
- Breaking Change: PASS/FAIL

## 9. Self-Review

### Findings

- Finding 1
- Finding 2

## 10. Adversarial Review

### Destruction Points

- Point 1
- Point 2

### Negative Tests Written

- Test 1
- Test 2

## 11. Self-Repair

### Repairs Made

- Repair 1
- Repair 2

## 12. Evidence

### Build Result

- `build.log`

### Test Result

- `test-report.html`

### API Diff

- `api-diff.json`

### Files Changed

- `src/...`
- `tests/...`

## 13. Deferred Items

有意延期的问题：

| Item | Reason | Target Phase |
|------|--------|--------------|
| Item 1 | Reason 1 | Phase X |

## 14. Contract Impact

```
No Contract Change
Additive Contract Change
Breaking Change → Human Gate Required
```

## 15. Final Gate

```
PASS
PASS WITH DEFERRED ITEMS
BLOCKED
```

## 16. Next Phase Recommendation

### Next Phase Objective

下一 Phase 的目标。

### Scope

下一 Phase 的范围。

### Dependencies

下一 Phase 的依赖。

### Risks

下一 Phase 的风险。

### Expected Artifacts

下一 Phase 的预期产出。

### Potential Human Gates

下一 Phase 可能触发的 Human Gate。

---

## Signatures

| Role | Name | Date | Signature |
|------|------|------|-----------|
| AI Engineer | | | |
| Chief Architect | | | |
