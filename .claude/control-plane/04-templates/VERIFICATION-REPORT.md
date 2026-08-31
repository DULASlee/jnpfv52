# Verification Report Template

> **目的：** 定义验证报告的标准模板

---

```markdown
# Verification Report: [Feature Name]

## Metadata

```yaml
verification:
  id: "VR-[Phase]-[N]"
  phase: "Phase X-Y"
  status: "PENDING"  # PENDING / PASS / FAIL
  createdAt: "YYYY-MM-DD"
```

---

## 1. Build Verification

### Project Build

| Item | Result | Evidence |
|------|--------|----------|
| Compilation | PASS/FAIL | build.log |
| Warnings | 0/1/2+ | build.log |
| Dependencies | OK/FAIL | packages.lock |

### Solution Build

| Item | Result | Evidence |
|------|--------|----------|
| Solution Build | PASS/FAIL | solution-build.log |

---

## 2. Test Verification

### Target Tests

| Test Type | Count | Pass | Fail | Pass Rate |
|-----------|-------|------|------|-----------|
| Unit Tests | 0 | 0 | 0 | 0% |
| Contract Tests | 0 | 0 | 0 | 0% |
| Integration Tests | 0 | 0 | 0 | 0% |

### Regression Tests

| Test Type | Count | Pass | Fail | Pass Rate |
|-----------|-------|------|------|-----------|
| Full Suite | 0 | 0 | 0 | 0% |

---

## 3. Architecture Verification

### Dependency Direction

| Dependency | Expected | Actual | Result |
|------------|----------|--------|--------|
| LayerA → LayerB | OK | OK | PASS |

### Forbidden Dependency

| Dependency | Result |
|------------|--------|
| None found | PASS |

### Layer Boundary

| Boundary | Result |
|----------|--------|
| LayerA → LayerB | PASS |

---

## 4. API Verification

### API Surface

| Endpoint | Method | Expected | Actual | Result |
|----------|--------|----------|--------|--------|
| /api/xxx | GET | 200 | 200 | PASS |

### Breaking Change

| Check | Result |
|-------|--------|
| No Breaking Change | PASS |

---

## 5. Summary

### Overall Result

```
BUILD: PASS/FAIL
TESTS: PASS/FAIL
ARCHITECTURE: PASS/FAIL
API: PASS/FAIL
```

### OVERALL: PASS/FAIL

---

## 6. Issues Found

### Critical Issues

| # | Issue | Impact | Resolution |
|---|-------|--------|------------|
| 1 | | | |

### Warnings

| # | Warning | Impact | Resolution |
|---|---------|--------|------------|
| 1 | | | |

---

## Signatures

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Engineer | | | |
| Reviewer | | | |
