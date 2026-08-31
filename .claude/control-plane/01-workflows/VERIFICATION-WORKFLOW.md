# Verification Workflow

> **目的：** 定义完成前的标准验证流程

---

## 验证阶段

```
Phase 10: Verification Before Completion
    ↓
Build
    ↓
Tests
    ↓
Architecture
    ↓
Public API
    ↓
Evidence Collection
    ↓
Gate
```

---

## 1. Build Verification

### Project Build

```bash
dotnet build [project]
```

### Solution Build

```bash
dotnet build [solution]
```

### 验证项

- [ ] 0 编译错误
- [ ] 0 警告（关键警告）
- [ ] 所有项目引用正确

---

## 2. Test Verification

### Target Tests

运行与本 Phase 相关的测试。

### Regression Tests

运行完整测试套件，确保没有破坏其他功能。

### 测试类型覆盖

- [ ] Unit Tests
- [ ] Contract Tests
- [ ] State / Lifecycle Tests
- [ ] Integration Tests
- [ ] Concurrency Tests（如适用）
- [ ] Failure Tests
- [ ] Regression Tests
- [ ] Boundary / Isolation Tests
- [ ] Negative Tests
- [ ] API Surface Tests

### 验证项

- [ ] 所有 Target Tests PASS
- [ ] 所有 Regression Tests PASS
- [ ] 测试覆盖率满足要求
- [ ] 没有 skip 的测试

---

## 3. Architecture Verification

### Dependency Direction

检查依赖方向是否正确。

### Forbidden Dependency

检查是否有禁止的依赖。

### Namespace Placement

检查命名空间是否正确放置。

### Layer Boundary

检查层级边界是否遵守。

### 验证项

- [ ] 依赖方向正确
- [ ] 无禁止依赖
- [ ] 命名空间正确
- [ ] 层级边界遵守
- [ ] 无循环依赖

---

## 4. Public API Verification

### API Surface

核心模块必须检查：

```
Public Types
Public Constructors
Public Methods
Public Properties
Public Interfaces
Public Events
Enum Surface
```

### API Baseline（如适用）

```yaml
apiBaseline:
  version: "1.0"
  frozenAt: "2026-08-31"
  
  surface:
    - path: /api/xxx
      methods: [GET, POST]
```

### Structural Diff

```
Positive Test → 正向测试通过
Negative Test → 负向测试通过
Recovery Test → 回滚测试通过
```

### 验证项

- [ ] API Surface 无意外变化
- [ ] Positive Tests PASS
- [ ] Negative Tests PASS
- [ ] Recovery Tests PASS
- [ ] 无 Breaking Change（如有，必须 H3）

---

## 5. Evidence Collection

### 必须收集

```
Build result
Test result
API diff
Architecture check
Files changed
```

### Evidence Chain

```
Requirement
    ↓
Design
    ↓
Implementation
    ↓
Tests
    ↓
Verification
    ↓
Evidence
    ↓
Gate
```

### 输出

```yaml
evidence_chain:
  requirement:
    id: "REQ-XXX"
    source: "phase-contract"
    status: "APPROVED"
    
  design:
    id: "SPEC-XXX"
    source: "design-spec.md"
    status: "APPROVED"
    links: ["REQ-XXX"]
    
  implementation:
    files: ["src/..."]
    testCoverage: "XX%"
    links: ["SPEC-XXX"]
    
  tests:
    unit:
      count: XX
      passRate: "XX%"
    contract:
      count: XX
      passRate: "XX%"
    negative:
      count: XX
      passRate: "XX%"
      
  verification:
    build: "PASS"
    unit_tests: "PASS"
    integration_tests: "PASS"
    api_diff: "PASS"
    architecture_check: "PASS"
    
  gate:
    status: "PASS"
    timestamp: "YYYY-MM-DD"
    type: "PHASE_CLOSURE"
```

---

## 6. Gate

### GREEN

```
所有 Contract 满足
测试通过
无已知阻塞
```

→ 自动进入下一 Phase

### YELLOW

```
功能完成
存在 Deferred Risk
但不影响当前 Contract
```

→ 记录后进入下一 Phase

### RED

```
架构冲突
Contract 冲突
安全风险
Breaking Change
不可逆决策
```

→ 暂停，请人工裁决

---

## Human Gate 检查

在验证结束时检查：

| Gate | 触发条件 | 动作 |
|------|---------|------|
| H1 | 架构方向冲突 | PAUSE |
| H2 | 需求语义冲突 | PAUSE |
| H3 | Breaking Change | PAUSE + CR |
| H4 | 跨 Section | PAUSE |
| H5 | 安全/数据风险 | EMERGENCY_PAUSE |

---

## 关联文档

- `AUTONOMOUS-MULTI-PHASE-ENGINEERING-WORKFLOW.md` — 主工作流
- `PHASE-EXECUTION-PROTOCOL.md` — Phase 执行协议
- `REVIEW-REPAIR-WORKFLOW.md` — Review/Repair 工作流
- `09-evidence/README.md` — Evidence Chain
