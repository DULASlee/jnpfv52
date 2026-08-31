# Phase Contract Template

> **目的：** 定义每个 Phase 的契约模板

---

```markdown
# Phase Contract

## Metadata

```yaml
phaseContract:
  phaseId: "X-Y"
  section: "Section N"
  status: "ACTIVE"
  createdAt: "YYYY-MM-DD"
  createdBy: "ai-engineer"
  approvedBy: "chief-architect"
```

---

## Objective

本 Phase 解决什么问题。

## Business / Engineering Problem

业务背景 / 工程问题

## Scope

### 包含

- [ ] Item 1
- [ ] Item 2

### 不包含

- Item 1
- Item 2

## Frozen Contracts

本 Phase 不得修改的契约：

| Contract | Location | Reason |
|----------|----------|--------|
| API Surface | `api/studio/ir` | Frozen |
| Data Model | `AiPipelineEntity` | Frozen |

## Dependencies

| Dependency | Location | Status |
|------------|----------|--------|
| Dependency 1 | path | Ready |

## Architecture Boundary

层级边界约束：

```
Allowed: Layer A → Layer B
Forbidden: Layer C → Layer A
```

## Testing Profile

```yaml
testingProfile: STRICT-TDD  # 或 CONTRACT-FIRST-TDD
```

### TDD Profile 说明

**STRICT-TDD 适用场景：**
- 核心算法实现
- 关键业务逻辑
- 状态机
- 生命周期

**CONTRACT-FIRST-TDD 适用场景：**
- 复杂系统集成
- 已有 Contract 扩展
- 大型 Phase

## Expected Artifacts

| Artifact | Location | Description |
|----------|----------|-------------|
| Design Spec | `docs/phase-x-y/design-spec.md` | 设计规格 |
| Implementation Plan | `docs/phase-x-y/impl-plan.md` | 实现计划 |
| Test Matrix | `docs/phase-x-y/test-matrix.md` | 测试矩阵 |
| Evidence | `docs/phase-x-y/evidence/` | 证据 |

## Test Requirements

### 必需测试类型

- [ ] Unit Tests
- [ ] Contract Tests
- [ ] State / Lifecycle Tests
- [ ] Integration Tests
- [ ] Failure Tests
- [ ] Regression Tests
- [ ] Boundary / Isolation Tests
- [ ] Negative Tests
- [ ] API Surface Tests

### 建议测试类型

- [ ] Concurrency Tests

## Verification Requirements

### Build

- [ ] Project Build
- [ ] Solution Build

### Tests

- [ ] Target Tests
- [ ] Regression Tests

### Architecture

- [ ] Dependency Direction
- [ ] Layer Boundary
- [ ] No Cyclic Dependency

### API

- [ ] API Surface Check
- [ ] No Breaking Change

## Human Gates

需要人工决策的节点：

| Gate | Trigger | Status |
|------|---------|--------|
| H1 | 架构冲突 | PENDING |
| H3 | Breaking Change | PENDING |

## Acceptance Criteria

| # | Criteria | Evidence | Status |
|---|----------|----------|--------|
| 1 | Criteria 1 | evidence-1 | DONE |
| 2 | Criteria 2 | evidence-2 | DONE |

## Deferred Decisions

有意延期的问题：

| Decision | Reason | Target Phase |
|----------|--------|--------------|
| Decision 1 | 等待更多信息 | Phase X |

## Risks

识别的风险：

| Risk | Impact | Mitigation |
|------|--------|------------|
| Risk 1 | High | Mitigation 1 |

## Evidence Chain

```yaml
evidence_chain:
  requirement:
    id: "REQ-XXX"
    status: "APPROVED"
    
  design:
    id: "SPEC-XXX"
    status: "APPROVED"
    links: ["REQ-XXX"]
    
  implementation:
    files: []
    links: ["SPEC-XXX"]
    
  tests:
    count: 0
    passRate: "0%"
    
  verification:
    build: "PENDING"
    tests: "PENDING"
    
  gate:
    status: "PENDING"
```

## Signatures

| Role | Name | Date | Signature |
|------|------|------|-----------|
| AI Engineer | | | |
| Chief Architect | | | |

---

## 使用说明

1. 创建 Phase Contract 后，AI 工程师按本契约执行
2. Phase Contract 是 Single Source of Truth
3. 所有变更必须更新本契约
4. Phase 完成后签署 Signatures
