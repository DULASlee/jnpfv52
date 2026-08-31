# Design to Implementation Workflow

> **目的：** 定义从设计到实现的标准路径

---

## 流程总览

```
Design Specification
    ↓
Implementation Plan
    ↓
TDD Design (双 Profile)
    ↓
Implementation
    ↓
Self-Review
    ↓
Adversarial Review
    ↓
Self-Repair
    ↓
Verification
```

---

## Step 1: Design Specification

### 必须完成

1. **Objective** — 明确目标
2. **Context** — 上下文
3. **Problem Statement** — 问题陈述
4. **Scope** — 范围
5. **Non-Scope** — 非范围
6. **Architecture Position** — 架构位置
7. **Component Model** — 组件模型
8. **Public Contract** — 公共契约
9. **Internal Contract** — 内部契约
10. **Data Model** — 数据模型
11. **State Model** — 状态模型
12. **Lifecycle** — 生命周期
13. **Failure Model** — 故障模型
14. **Concurrency Model** — 并发模型
15. **Error Handling** — 错误处理
16. **Observability** — 可观测性
17. **Compatibility** — 兼容性
18. **Security Boundary** — 安全边界
19. **Extensibility** — 可扩展性
20. **Deferred Decisions** — 延期决策
21. **Risks** — 风险
22. **Acceptance Criteria** — 验收标准

---

## Step 2: Implementation Plan

### 细化到

```
Project
Folder
File
Class
Interface
Method
Test
Documentation
```

### Task Group 划分

```yaml
Task Group A
  A1 Contract
  A2 Implementation
  A3 Tests

Task Group B
  B1 Integration
  B2 Compatibility
  B3 Regression
```

**注意：Implementation Plan 是 AI 的内部执行计划，不是要求用户逐条批准。**

---

## Step 3: TDD Design

### 根据 Phase Contract 选择 Profile

```yaml
testingProfile: STRICT-TDD
# 或
testingProfile: CONTRACT-FIRST-TDD
```

### 设计测试矩阵

```
A. Unit Tests
B. Contract Tests
C. State / Lifecycle Tests
D. Integration Tests
E. Concurrency Tests
F. Failure Tests
G. Regression Tests
H. Boundary / Isolation Tests
I. Negative Tests
J. API Surface Tests
```

---

## Step 4: Implementation

### 遵循 TDD Profile

**STRICT-TDD：**
```
RED → GREEN → REFACTOR → REGRESSION
```

**CONTRACT-FIRST-TDD：**
```
Contract → Test Matrix → Implementation → Verification → Regression
```

### 绝对禁止

```
❌ 为了让测试通过 → 删除核心行为
❌ 为了减少代码 → 删除架构能力
❌ 为了 MVP → 将 Agent Runtime 改成 Workflow
❌ 为了方便 → 把 internal API 改 public
❌ 为了测试 → 破坏生命周期封装
❌ 为了快速 → 删除边界验证
```

---

## Step 5: Self-Review

### 检查项

```
Does implementation satisfy Specification?
```

### 检查类型

```
Spec Drift
Implementation Drift
Contract Drift
Architecture Drift
```

---

## Step 6: Adversarial Review

### 破坏点分析

```
如果我要把这个 Runtime 退化成 Workflow，我会在哪里动手？
如果我要绕过 Lifecycle，我会在哪里动手？
如果我要偷偷扩大 Public API，我会在哪里动手？
如果我要把 Capability 注入 Core，我会在哪里动手？
如果我要制造并发 Bug，我会在哪里动手？
```

---

## Step 7: Self-Repair

### 流程

```
Detect → Root Cause → Repair → Retest → Regression
```

### 禁止

```
发现失败 → 修改测试让它通过
```

---

## Step 8: Verification

### Build

```
Project Build
Solution Build
```

### Tests

```
Target Tests
Regression Tests
Full Relevant Test Suite
```

### Architecture

```
Dependency Direction
Forbidden Dependency
Namespace Placement
Layer Boundary
```

### Public API

```
Public Types
Public Constructors
Public Methods
Public Properties
Public Interfaces
Public Events
Enum Surface
```

---

## 关联文档

- `AUTONOMOUS-MULTI-PHASE-ENGINEERING-WORKFLOW.md` — 主工作流
- `PHASE-EXECUTION-PROTOCOL.md` — Phase 执行协议
- `TDD-WORKFLOW.md` — TDD 工作流
- `VERIFICATION-WORKFLOW.md` — 验证工作流
