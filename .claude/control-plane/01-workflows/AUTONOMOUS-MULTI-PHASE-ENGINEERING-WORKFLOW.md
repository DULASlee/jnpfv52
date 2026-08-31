# Autonomous Multi-Phase Engineering Workflow v1.1

> **版本：** v1.1
> 
> **生效日期：** 2026-08-31
> 
> **来源：** 基于用户提供的 Autonomous Multi-Phase Engineering Workflow v1.0，整合现有 WORKFLOW-IRON-01

---

## 流程总览

```
Task Input
    ↓
Phase 0: Discovery & Baseline
    ↓
Phase 1: Requirement & Architecture Analysis
    ↓
Phase 2: Design Specification
    ↓
Phase 3: Chief-Architect Pre-Gate
    ↓
Phase 4: Implementation Plan
    ↓
Phase 5: TDD Design (双 Profile)
    ↓
Phase 6: Implementation
    ↓
Phase 7: Self-Review
    ↓
Phase 8: Adversarial Review
    ↓
Phase 9: Self-Repair Loop
    ↓
Phase 10: Verification Before Completion
    ↓
Phase 11: Documentation & Evidence
    ↓
Phase 12: Acceptance Review
    ↓
Phase Gate
    ↓
Next Phase / Human Gate
```

---

## 禁止模式

**不得采用：**

```
做一步
↓
问一次
↓
做一步
↓
问一次
↓
人工持续驱动
```

**除非发生：**

- H1: 架构方向冲突
- H2: 需求语义冲突
- H3: Breaking Change
- H4: 跨 Section 架构决策
- H5: 安全/数据/生产风险

---

## AI 工程师角色

在每一个 Phase 内，同时承担：

```
Requirements Analyst
System Architect
Designer
Coder (遵守 TDD Profile)
Tester
Reviewer
Adversarial Reviewer
Release Engineer
```

**但必须按顺序执行，而不是混在一起直接写代码。**

---

## Phase 0 — Discovery & Baseline

### 必须完成

1. **Repository Discovery**
   - AGENTS.md
   - Architecture docs
   - Master Plan
   - ADR
   - Section Specifications
   - 现有实现
   - 现有 tests
   - solution/project structure
   - dependency graph

2. **Existing Contract Discovery**
   - Frozen Contract
   - Open Contract
   - Deferred Decision
   - Existing Compatibility Requirement

3. **Change Boundary**
   ```
   IN SCOPE
   OUT OF SCOPE
   DEPENDENCIES
   FORBIDDEN CHANGES
   ```

4. **Baseline**
   ```
   Build baseline
   Test baseline
   API baseline
   Architecture baseline
   Relevant behavioral baseline
   ```

**禁止在没有 baseline 的情况下直接修改核心架构。**

---

## Phase 1 — Requirement & Architecture Analysis

### 必须回答

```
1. 这个 Phase 真正要解决什么问题？
2. 当前架构为什么不能解决？
3. 哪个 Layer 应该拥有该能力？
4. 哪些 Layer 明确不能拥有？
5. 会不会与已有 Contract 冲突？
6. 是否会把 Runtime 退化为 Workflow？
7. 是否会把 Capability 倒灌到 Kernel？
8. 是否产生新的隐式状态？
9. 是否产生公共 API 泄漏？
10. 是否提前建设未来阶段能力？
```

### Architecture Anti-Regression Analysis

主动寻找：

```
职责漂移
依赖反转
God Object / God Service
Capability Leakage
Workflow Leakage
Intelligence Leakage
Public API Expansion
State Machine Pollution
Infrastructure Premature Expansion
```

---

## Phase 2 — Design Specification

### 必须包含

```
1. Objective
2. Context
3. Problem Statement
4. Scope
5. Non-Scope
6. Architecture Position
7. Component Model
8. Public Contract
9. Internal Contract
10. Data Model
11. State Model
12. Lifecycle
13. Failure Model
14. Concurrency Model
15. Error Handling
16. Observability
17. Compatibility
18. Security Boundary
19. Extensibility
20. Deferred Decisions
21. Risks
22. Acceptance Criteria
```

**如果发现原需求无法安全落地：先在 Specification 中解决问题，而不是直接 Coding。**

---

## Phase 3 — Chief-Architect Pre-Gate

### AI 自己先进行 Design Review

检查：

```
Contract Correctness
Architecture Correctness
Layering
Lifecycle
Concurrency
Failure Handling
Backward Compatibility
Testability
Operational Risk
Future Extensibility
```

### 输出

```
DESIGN STATUS

PASS
PASS WITH INTERNAL ASSUMPTIONS
BLOCKED
```

**只有真正涉及 Human Decision 的问题才暂停。否则自己选择最保守、最符合 Frozen Contract 的方案继续。**

---

## Phase 4 — Implementation Plan

**细化到：**

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

**注意：Implementation Plan 是 AI 的内部执行计划，不是要求用户逐条批准。**

---

## Phase 5 — TDD Design

### TDD 双 Profile

**由 Phase Contract 显式指定。**

#### STRICT-TDD

适用于：核心算法、关键业务规则、状态机、生命周期、高风险行为

```
RED
 ↓
Write failing test
 ↓
GREEN
 ↓
Minimal implementation
 ↓
REFACTOR
 ↓
Regression
```

#### CONTRACT-FIRST-TDD

适用于：复杂系统集成、已有 Contract 扩展、大型 Phase、跨模块变化

```
Contract
 ↓
Test Matrix
 ↓
Implementation
 ↓
Verification
 ↓
Regression
```

**禁止自行规定项目全部使用某一个 Profile。**

### 必须设计的测试类型

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

## Phase 6 — Implementation

### 统一顺序（STRICT-TDD）

```
RED
 ↓
Write failing test
 ↓
GREEN
 ↓
Minimal implementation
 ↓
REFACTOR
 ↓
Regression
```

### 绝对禁止

```
为了让测试通过
→ 删除核心行为

为了减少代码
→ 删除架构能力

为了 MVP
→ 将 Agent Runtime 改成 Workflow

为了方便
→ 把 internal API 改 public

为了测试
→ 破坏生命周期封装

为了快速
→ 删除边界验证
```

---

## Phase 7 — Self-Review

### Original Implementer

检查：

```
Does implementation satisfy Specification?
```

### Independent Reviewer

重新阅读：

```
Requirements
Specification
Implementation
Tests
ADR
```

检查是否存在：

```
Spec Drift
Implementation Drift
Contract Drift
Architecture Drift
```

---

## Phase 8 — Adversarial Review

**主动站在"破坏系统"的角度审查：**

```
如果我要把这个 Runtime 退化成 Workflow，
我会在哪里动手？

如果我要绕过 Lifecycle，
我会在哪里动手？

如果我要偷偷扩大 Public API，
我会在哪里动手？

如果我要把 Capability 注入 Core，
我会在哪里动手？

如果我要制造并发 Bug，
我会在哪里动手？

如果我要让测试通过但功能被砍掉，
我会在哪里动手？
```

**然后针对发现的问题写 Negative Tests。**

---

## Phase 9 — Self-Repair Loop

```
Detect
 ↓
Root Cause
 ↓
Repair
 ↓
Retest
 ↓
Regression
```

**禁止：**

```
发现失败
→ 修改测试让它通过
```

**除非：** 测试本身确实违反已批准 Contract，并且要记录原因。

---

## Phase 10 — Verification Before Completion

### 必须验证

#### Build

```
Project Build
Solution Build
```

#### Tests

```
Target Tests
Regression Tests
Full Relevant Test Suite
```

#### Architecture

```
Dependency Direction
Forbidden Dependency
Namespace Placement
Layer Boundary
```

#### Public API

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

## Phase 11 — Documentation & Evidence

### 必须同步维护

```
Specification
Implementation Plan
ADR
API Baseline（如适用）
Test Matrix
Verification Record
Decision Record
```

**不能出现：代码已经变化，文档仍描述旧架构。**

---

## Phase 12 — Acceptance Review

### 必须输出

#### A. What Changed

事实性描述。

#### B. Why

设计原因。

#### C. Evidence

```
Build result
Test result
API diff
Architecture check
Files changed
```

#### D. Contract Impact

```
No Contract Change
Additive Contract Change
Breaking Change → Human Gate Required
```

#### E. Deferred Items

明确哪些问题未解决但有意延期。

#### F. Final Verdict

```
PASS
PASS WITH DEFERRED ITEMS
BLOCKED
```

**不能用模糊的："应该没问题"、"基本完成"、"看起来可以"。**

---

## Phase Gate 机制

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

## 固定输出模板

每个 Phase 必须输出 Phase Completion Report：

```markdown
# Phase N Completion Report

## 1. Objective
## 2. Discovery
## 3. Architecture Decision
## 4. Scope
## 5. Specification
## 6. Implementation
## 7. TDD
## 8. Verification
## 9. Self-Review
## 10. Adversarial Review
## 11. Self-Repair
## 12. Evidence
## 13. Deferred Items
## 14. Contract Impact
## 15. Final Gate (PASS / YELLOW / RED)
## 16. Next Phase Recommendation
```

---

## 关联文档

- `PHASE-EXECUTION-PROTOCOL.md` — Phase 执行协议
- `TDD-WORKFLOW.md` — TDD 工作流（双 Profile）
- `VERIFICATION-WORKFLOW.md` — 验证工作流
- `REVIEW-REPAIR-WORKFLOW.md` — Review 和 Repair 工作流
- `04-templates/PHASE-CONTRACT.md` — Phase Contract 模板
- `04-templates/PHASE-COMPLETION-REPORT.md` — 完成报告模板
