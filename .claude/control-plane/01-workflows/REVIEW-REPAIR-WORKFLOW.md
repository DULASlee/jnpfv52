# Review and Repair Workflow

> **目的：** 定义 Review 和 Self-Repair 的标准流程

---

## Review 类型

| 类型 | 阶段 | 目的 |
|------|------|------|
| Self-Review | Phase 7 | 原实现者自检 |
| Adversarial Review | Phase 8 | 主动破坏系统 |
| Reviewer Review | WORKFLOW-IRON-01 | 独立审查 |

---

## Phase 7: Self-Review

### Step 1: Original Implementer

检查：

```
Does implementation satisfy Specification?
```

### Step 2: Independent Reviewer

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

### 输出

```markdown
# Self-Review Report

## Implementation vs Specification
- [符合项]
- [偏离项]

## Drift Analysis
- [ ] Spec Drift: [发现/无]
- [ ] Implementation Drift: [发现/无]
- [ ] Contract Drift: [发现/无]
- [ ] Architecture Drift: [发现/无]

## Issues Found
1. [Issue 1]
2. [Issue 2]

## Recommendations
1. [Recommendation 1]
2. [Recommendation 2]
```

---

## Phase 8: Adversarial Review

### 主动破坏系统

**问自己：**

```
如果我要把这个 Runtime 退化成 Workflow，我会在哪里动手？
如果我要绕过 Lifecycle，我会在哪里动手？
如果我要偷偷扩大 Public API，我会在哪里动手？
如果我要把 Capability 注入 Core，我会在哪里动手？
如果我要制造并发 Bug，我会在哪里动手？
如果我要让测试通过但功能被砍掉，我会在哪里动手？
```

### 针对发现的问题写 Negative Tests

### 输出

```markdown
# Adversarial Review Report

## Destruction Points
1. [破坏点 1]
2. [破坏点 2]

## Vulnerability Analysis
- [ ] Capability Leakage: [发现/无]
- [ ] Workflow Regression: [发现/无]
- [ ] Lifecycle Bypass: [发现/无]
- [ ] Public API Expansion: [发现/无]
- [ ] Concurrency Bug: [发现/无]

## Negative Tests Written
1. [Test 1]
2. [Test 2]

## Recommendations
1. [Recommendation 1]
```

---

## Phase 9: Self-Repair Loop

### 流程

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

### Detect

识别问题：
- 测试失败
- Review 发现问题
- Adversarial Review 发现问题
- 编译错误
- 架构违规

### Root Cause

**必须找到根本原因，不是症状。**

问：
- 问题真正的原因是什么？
- 是什么导致了这个问题？
- 问题的源头在哪里？

### Repair

**修复根本原因，不是症状。**

原则：
- 做最小的改变
- 一次只改一件事
- 不要"顺手"改其他东西

### Retest

运行相关测试，确认修复有效。

### Regression

运行完整测试套件，确保没有破坏其他功能。

### 循环

如果有新问题，返回 Detect。

---

## 禁止行为

```
❌ 发现失败 → 修改测试让它通过
❌ 修复症状，不修复根因
❌ 一次改多件事
❌ "顺手"改其他代码
```

**除非：** 测试本身确实违反已批准 Contract，并且要记录原因。

---

## Reviewer Review (WORKFLOW-IRON-01)

### 架构风险

```
如果未来维护：
  - 是否容易理解？
  - 是否容易扩展？
  - 是否存在技术债？
```

### 工程质量

```
- 是否符合最佳实践？
- 是否存在隐藏 Bug？
- 是否缺少测试？
```

### AI 防退化检查

```
□ 是否简化核心能力？
□ 是否删除必要状态？
□ 是否减少验证流程？
□ 是否使用假实现？
□ 是否把未来能力设计死？
```

**任一失败：必须返回 Self-Repair。**

---

## 关联文档

- `AUTONOMOUS-MULTI-PHASE-ENGINEERING-WORKFLOW.md` — 主工作流
- `PHASE-EXECUTION-PROTOCOL.md` — Phase 执行协议
- `VERIFICATION-WORKFLOW.md` — 验证工作流
