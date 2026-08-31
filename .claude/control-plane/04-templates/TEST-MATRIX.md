# Test Matrix Template

> **目的：** 定义测试矩阵的标准模板

---

```markdown
# Test Matrix: [Feature Name]

## Metadata

```yaml
testMatrix:
  id: "TEST-[Phase]-[N]"
  phase: "Phase X-Y"
  testingProfile: STRICT-TDD  # 或 CONTRACT-FIRST-TDD
  createdAt: "YYYY-MM-DD"
```

---

## Testing Profile

```yaml
testingProfile: STRICT-TDD
# 或
testingProfile: CONTRACT-FIRST-TDD
```

## Test Categories

### A. Unit Tests

| Test ID | Test Case | Input | Expected Output | Status |
|---------|-----------|-------|-----------------|--------|
| UT-001 | Test case 1 | input1 | output1 | TODO |
| UT-002 | Test case 2 | input2 | output2 | TODO |

### B. Contract Tests

| Test ID | Test Case | Contract | Expected | Status |
|---------|-----------|----------|----------|--------|
| CT-001 | Test case 1 | Contract1 | Expected1 | TODO |

### C. State / Lifecycle Tests

| Test ID | Test Case | Current State | Event | Expected State | Status |
|---------|-----------|--------------|-------|----------------|--------|
| SL-001 | Test case 1 | State1 | Event1 | State2 | TODO |

### D. Integration Tests

| Test ID | Test Case | Components | Expected | Status |
|---------|-----------|------------|----------|--------|
| IT-001 | Test case 1 | Comp1+Comp2 | Expected1 | TODO |

### E. Concurrency Tests

| Test ID | Test Case | Scenario | Expected | Status |
|---------|-----------|----------|----------|--------|
| CON-001 | Test case 1 | Scenario1 | Expected1 | TODO |

### F. Failure Tests

| Test ID | Test Case | Failure Scenario | Expected Handling | Status |
|---------|-----------|-----------------|------------------|--------|
| FT-001 | Test case 1 | Failure1 | Handling1 | TODO |

### G. Regression Tests

| Test ID | Test Case | Regression Of | Expected | Status |
|---------|-----------|---------------|----------|--------|
| RT-001 | Test case 1 | Feature1 | Expected1 | TODO |

### H. Boundary / Isolation Tests

| Test ID | Test Case | Boundary | Expected | Status |
|---------|-----------|----------|----------|--------|
| BT-001 | Test case 1 | Boundary1 | Expected1 | TODO |

### I. Negative Tests

| Test ID | Test Case | Invalid Input | Expected | Status |
|---------|-----------|--------------|----------|--------|
| NT-001 | Test case 1 | Invalid1 | Expected1 | TODO |

### J. API Surface Tests

| Test ID | Test Case | Endpoint | Method | Expected | Status |
|---------|-----------|----------|--------|----------|--------|
| AT-001 | Test case 1 | /api/xxx | GET | 200 | TODO |

---

## Test Coverage Summary

| Category | Count | Pass Rate |
|----------|-------|-----------|
| Unit Tests | 0 | 0% |
| Contract Tests | 0 | 0% |
| State / Lifecycle Tests | 0 | 0% |
| Integration Tests | 0 | 0% |
| Concurrency Tests | 0 | 0% |
| Failure Tests | 0 | 0% |
| Regression Tests | 0 | 0% |
| Boundary / Isolation Tests | 0 | 0% |
| Negative Tests | 0 | 0% |
| API Surface Tests | 0 | 0% |
| **Total** | **0** | **0%** |

---

## Signatures

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Engineer | | | |
| Reviewer | | | |
