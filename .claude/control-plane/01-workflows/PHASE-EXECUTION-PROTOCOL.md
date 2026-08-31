# Phase Execution Protocol

> **目的：** 定义每个 Phase 的具体执行步骤和检查清单

---

## Phase 执行检查清单

### Phase 0: Discovery & Baseline

- [ ] 读取 AGENTS.md
- [ ] 读取 Architecture docs
- [ ] 读取 Master Plan
- [ ] 读取 ADR
- [ ] 读取 Section Specifications
- [ ] 分析现有实现
- [ ] 分析现有 tests
- [ ] 分析 solution/project structure
- [ ] 分析 dependency graph
- [ ] 识别 Frozen Contract
- [ ] 识别 Open Contract
- [ ] 识别 Deferred Decision
- [ ] 识别 Existing Compatibility Requirement
- [ ] 定义 IN SCOPE
- [ ] 定义 OUT OF SCOPE
- [ ] 定义 DEPENDENCIES
- [ ] 定义 FORBIDDEN CHANGES
- [ ] 建立 Build baseline
- [ ] 建立 Test baseline
- [ ] 建立 API baseline
- [ ] 建立 Architecture baseline
- [ ] 建立 Behavioral baseline

---

### Phase 1: Requirement & Architecture Analysis

- [ ] 回答 10 个核心问题
- [ ] 进行 Architecture Anti-Regression Analysis
- [ ] 检查职责漂移
- [ ] 检查依赖反转
- [ ] 检查 God Object / God Service
- [ ] 检查 Capability Leakage
- [ ] 检查 Workflow Leakage
- [ ] 检查 Intelligence Leakage
- [ ] 检查 Public API Expansion
- [ ] 检查 State Machine Pollution
- [ ] 检查 Infrastructure Premature Expansion
- [ ] 输出 Requirement Analysis 文档
- [ ] 输出 Scope / Non-Scope
- [ ] 输出 Frozen Contracts
- [ ] 输出 Dependencies

---

### Phase 2: Design Specification

- [ ] 编写 Objective
- [ ] 编写 Context
- [ ] 编写 Problem Statement
- [ ] 编写 Scope
- [ ] 编写 Non-Scope
- [ ] 编写 Architecture Position
- [ ] 编写 Component Model
- [ ] 编写 Public Contract
- [ ] 编写 Internal Contract
- [ ] 编写 Data Model
- [ ] 编写 State Model
- [ ] 编写 Lifecycle
- [ ] 编写 Failure Model
- [ ] 编写 Concurrency Model
- [ ] 编写 Error Handling
- [ ] 编写 Observability
- [ ] 编写 Compatibility
- [ ] 编写 Security Boundary
- [ ] 编写 Extensibility
- [ ] 编写 Deferred Decisions
- [ ] 编写 Risks
- [ ] 编写 Acceptance Criteria
- [ ] 如有问题，先在 Specification 中解决

---

### Phase 3: Chief-Architect Pre-Gate

- [ ] 进行 Design Review
- [ ] 检查 Contract Correctness
- [ ] 检查 Architecture Correctness
- [ ] 检查 Layering
- [ ] 检查 Lifecycle
- [ ] 检查 Concurrency
- [ ] 检查 Failure Handling
- [ ] 检查 Backward Compatibility
- [ ] 检查 Testability
- [ ] 检查 Operational Risk
- [ ] 检查 Future Extensibility
- [ ] 输出 DESIGN STATUS
- [ ] 判断是否需要 Human Gate

---

### Phase 4: Implementation Plan

- [ ] 定义 Project
- [ ] 定义 Folder
- [ ] 定义 File
- [ ] 定义 Class
- [ ] 定义 Interface
- [ ] 定义 Method
- [ ] 定义 Test
- [ ] 定义 Documentation
- [ ] 分组 Task（如 Task Group A, B）
- [ ] 一次性规划完整 Phase

---

### Phase 5: TDD Design

- [ ] 读取 Phase Contract 的 testingProfile
- [ ] 确定使用 STRICT-TDD 或 CONTRACT-FIRST-TDD
- [ ] 设计 Unit Tests
- [ ] 设计 Contract Tests
- [ ] 设计 State / Lifecycle Tests
- [ ] 设计 Integration Tests
- [ ] 设计 Concurrency Tests（如需要）
- [ ] 设计 Failure Tests
- [ ] 设计 Regression Tests
- [ ] 设计 Boundary / Isolation Tests
- [ ] 设计 Negative Tests
- [ ] 设计 API Surface Tests
- [ ] 输出 Test Matrix

---

### Phase 6: Implementation

**STRICT-TDD 流程：**
- [ ] Write failing test (RED)
- [ ] Run test, verify it fails
- [ ] Write minimal implementation (GREEN)
- [ ] Run test, verify it passes
- [ ] REFACTOR
- [ ] Regression

**CONTRACT-FIRST-TDD 流程：**
- [ ] Verify Contract
- [ ] Write Test Matrix
- [ ] Implementation
- [ ] Verify against Contract
- [ ] Regression

**禁止行为检查：**
- [ ] 没有删除核心行为
- [ ] 没有删除架构能力
- [ ] 没有将 Agent Runtime 改成 Workflow
- [ ] 没有把 internal API 改 public
- [ ] 没有破坏生命周期封装
- [ ] 没有删除边界验证

---

### Phase 7: Self-Review

- [ ] Original Implementer: Does implementation satisfy Specification?
- [ ] Independent Reviewer: 重新阅读 Requirements
- [ ] Independent Reviewer: 重新阅读 Specification
- [ ] Independent Reviewer: 重新阅读 Implementation
- [ ] Independent Reviewer: 重新阅读 Tests
- [ ] Independent Reviewer: 重新阅读 ADR
- [ ] 检查 Spec Drift
- [ ] 检查 Implementation Drift
- [ ] 检查 Contract Drift
- [ ] 检查 Architecture Drift
- [ ] 输出 Self-Review 报告

---

### Phase 8: Adversarial Review

- [ ] 分析：如果要把 Runtime 退化成 Workflow，哪里动手？
- [ ] 分析：如果要绕过 Lifecycle，哪里动手？
- [ ] 分析：如果要偷偷扩大 Public API，哪里动手？
- [ ] 分析：如果要把 Capability 注入 Core，哪里动手？
- [ ] 分析：如果要制造并发 Bug，哪里动手？
- [ ] 分析：如果要让测试通过但功能被砍掉，哪里动手？
- [ ] 针对发现的问题写 Negative Tests
- [ ] 输出 Adversarial Review 报告

---

### Phase 9: Self-Repair Loop

- [ ] Detect 问题
- [ ] 定位 Root Cause
- [ ] 制定 Repair 方案
- [ ] 执行 Repair
- [ ] 重新测试
- [ ] Regression
- [ ] 如有失败，返回 Detect

**禁止：发现失败 → 修改测试让它通过**

---

### Phase 10: Verification Before Completion

- [ ] Project Build
- [ ] Solution Build
- [ ] Target Tests
- [ ] Regression Tests
- [ ] Full Relevant Test Suite
- [ ] Dependency Direction 检查
- [ ] Forbidden Dependency 检查
- [ ] Namespace Placement 检查
- [ ] Layer Boundary 检查
- [ ] Public Types 检查
- [ ] Public Constructors 检查
- [ ] Public Methods 检查
- [ ] Public Properties 检查
- [ ] Public Interfaces 检查
- [ ] Public Events 检查
- [ ] Enum Surface 检查

---

### Phase 11: Documentation & Evidence

- [ ] 同步 Specification
- [ ] 同步 Implementation Plan
- [ ] 同步 ADR
- [ ] 同步 API Baseline（如适用）
- [ ] 同步 Test Matrix
- [ ] 同步 Verification Record
- [ ] 同步 Decision Record
- [ ] 确保代码变化与文档一致

---

### Phase 12: Acceptance Review

- [ ] 编写 What Changed
- [ ] 编写 Why
- [ ] 收集 Evidence (Build result, Test result, API diff, Architecture check, Files changed)
- [ ] 判断 Contract Impact (No Change / Additive / Breaking)
- [ ] 列出 Deferred Items
- [ ] 输出 Final Verdict (PASS / YELLOW / RED)
- [ ] 输出 Next Phase Recommendation

---

## Human Gate 检查

在每个 Phase 结束时检查：

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
- `TDD-WORKFLOW.md` — TDD 工作流
- `VERIFICATION-WORKFLOW.md` — 验证工作流
- `REVIEW-REPAIR-WORKFLOW.md` — Review/Repair 工作流
