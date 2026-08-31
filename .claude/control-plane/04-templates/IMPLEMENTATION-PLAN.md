# Implementation Plan Template

> **目的：** 定义实现计划的标准模板

---

```markdown
# Implementation Plan: [Feature Name]

## Metadata

```yaml
plan:
  id: "PLAN-[Phase]-[N]"
  phase: "Phase X-Y"
  status: "DRAFT"  # DRAFT / APPROVED
  version: "1.0"
  createdAt: "YYYY-MM-DD"
```

---

## Overview

简要描述实现计划。

## Task Groups

### Task Group A: [Group Name]

#### A1: [Task Name]

**文件：**
- Create: `path/to/file1.cs`
- Modify: `path/to/file2.cs:123-145`
- Test: `tests/path/to/test1.cs`

**步骤：**
- [ ] Step 1: Description
- [ ] Step 2: Description
- [ ] Step 3: Description

#### A2: [Task Name]

**文件：**
- Create: `path/to/file3.cs`
- Modify: `path/to/file4.cs`

**步骤：**
- [ ] Step 1: Description
- [ ] Step 2: Description

### Task Group B: [Group Name]

#### B1: [Task Name]

**文件：**
- Create: `path/to/file5.cs`
- Modify: `path/to/file6.cs`

**步骤：**
- [ ] Step 1: Description
- [ ] Step 2: Description

## File Structure

```
src/
├── Module/
│   ├── Component1.cs
│   └── Component2.cs
└── Tests/
    ├── Module.Tests/
    │   ├── Component1Tests.cs
    │   └── Component2Tests.cs
    └── Integration/
        └── ComponentIntegrationTests.cs
```

## Dependencies

| Dependency | Version | Purpose |
|------------|---------|---------|
| Package1 | 1.0.0 | Purpose1 |
| Package2 | 2.0.0 | Purpose2 |

## Implementation Order

```
1. A1 Contract
2. A1 Implementation
3. A1 Tests
4. A2 Contract
5. A2 Implementation
6. A2 Tests
7. B1 Integration
8. B1 Tests
9. Regression
```

## TDD Profile

```yaml
testingProfile: STRICT-TDD  # 或 CONTRACT-FIRST-TDD
```

### STRICT-TDD 流程

```
RED → GREEN → REFACTOR → REGRESSION
```

### CONTRACT-FIRST-TDD 流程

```
Contract → Test Matrix → Implementation → Verification → Regression
```

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Risk1 | Medium | Mitigation1 |

## Timeline

| Task | Estimated Time | Actual Time | Status |
|------|----------------|-------------|--------|
| Task A1 | 2h | | TODO |
| Task A2 | 1h | | TODO |
| Task B1 | 3h | | TODO |

## Signatures

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Engineer | | | |
| Reviewer | | | |
