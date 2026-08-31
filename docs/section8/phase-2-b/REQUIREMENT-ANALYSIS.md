# Phase 2-B Requirement & Architecture Analysis

> **Phase:** Section 8 Runtime Foundation — Phase 2-B: Execution Boundary & Hook Pipeline  
> **Date:** 2026-08-31

---

## 1. Core Questions

### Q1: 什么叫一次 Execution？

**Answer:**  
一次 Execution 是 RuntimeSession 内的一次独立工作单元，具有：
- 唯一标识 (ExecutionId)
- 独立生命周期 (Pending → Running → Completed/Failed/Cancelled)
- 关联的 Hook Pipeline
- 独立的 CancellationToken
- 不可变的结果 (ExecutionResult)

**Decision:** Execution 依附于 RuntimeSession，但不是 RuntimeSession 本身。

### Q2: Execution 与 RuntimeSession 的关系是什么？

**Answer:**  
- RuntimeSession 是容器，管理多个 Execution
- RuntimeSession 持有 ExecutionContext 列表
- RuntimeSession 状态 ≠ Execution 状态
- RuntimeSession 可以有多个并发 Execution

**Decision:** One-to-Many relationship.

### Q3: Execution 与 RuntimeState 的关系是什么？

**Answer:**  
- Execution 有独立的 ExecutionState (Pending/Running/Completed/Failed/Cancelled)
- ExecutionState 与 RuntimeState 是正交的
- RuntimeSession 可以 Running (有活跃 Execution)
- 单个 Execution 完成不影响 Session 状态

**Decision:** 保持独立状态机，避免耦合。

### Q4: Execution 是否拥有独立生命周期？

**Answer:**  
Yes. Execution 有自己的状态转换：
```
Pending → Running → Completed
                    → Failed
                    → Cancelled
```

**Decision:** Execution 是状态机，但不是 RuntimeStateMachine 的子类。

### Q5: Hook 属于 Execution 还是 RuntimeSession？

**Answer:**  
Hook 属于 ExecutionContext（per-execution），而非 Session。

**Decision:** Hook 在 CreateExecution() 时从 Session 继承或创建新的 Registry。

### Q6: Hook 是否允许修改 Runtime State？

**Answer:**  
**No.** Hook 只能观察和记录，不能修改 RuntimeState。

**Decision:** Hook 调用 TransitionTo() 是 BLOCKED。违反 = Architecture Violation。

### Q7: Event 与 Hook 的职责差异是什么？

**Answer:**  
| 维度 | Hook | Event |
|------|------|-------|
| 同步/异步 | 同步 (在 pipeline 内) | 异步 (观察者模式) |
| 失败处理 | 可配置 | Fire-and-forget |
| 顺序 | 按 Order | 不保证顺序 |
| 可中断 | 可 Cancel | 不可中断 |

**Decision:** Hook 是同步管道拦截器，Event 是异步通知。

### Q8: Hook 的失败应该终止 Execution 还是记录失败？

**Answer:**  
记录失败，但 Execution 继续（除非是 Before Hook 且配置了 fail-fast）。

**Decision:** Hook 失败写入 ExecutionResult.FailureReason，但不改变 ExecutionState。

### Q9: Hook 是否允许异步？

**Answer:**  
Yes. Hook 方法返回 `Task`，允许 async 操作。

**Decision:** 支持 async hooks，但超时机制必须存在。

### Q10: Hook 是否允许并发执行？

**Answer:**  
**No.** Hook 按 Order 顺序执行，同一时间只执行一个。

**Decision:** Hook Pipeline 是串行的，但 Hook 内部可异步。

### Q11: Hook 是否必须保证顺序？

**Answer:**  
Yes. Hook 按 Order 属性升序执行。

**Decision:** Order 属性必须，默认为 0。

### Q12: Cancellation 如何传播？

**Answer:**  
1. CancellationSource 绑定到 ExecutionContext
2. Hook 在执行前检查 CancellationToken
3. OnCancelled Hook 在 Cancellation 时调用
4. Execution 进入 Cancelled 状态

**Decision:** Cancellation 是 cooperative 的，不是 preemptive 的。

---

## 2. Architecture Anti-Regression Review

### 2.1 Capability Leakage ❌

**检查:** Runtime.Core 不得依赖 Runtime.Capability

**当前状态:** ✅ 无 Capability 依赖

**Phase 2-B 要求:**
- 新增类型不得引入 `JNPF.Runtime.Capability` 引用
- Hook 参数类型必须是 Runtime 内部类型或基础类型

### 2.2 Intelligence Leakage ❌

**检查:** Runtime.Core 不得包含 AI/ML 概念

**当前状态:** ✅ 无 Intelligence 概念

**Phase 2-B 要求:**
- ExecutionContext 不得包含 Model/Prompt/Plan

### 2.3 Workflow Leakage ❌

**检查:** Runtime.Core 不得实现 Workflow Engine

**当前状态:** ✅ 无 Workflow 概念

**Phase 2-B 要求:**
- Hook 只是拦截点，不是工作流步骤定义
- 不得实现 Step/Stage/Transition 逻辑

### 2.4 Lifecycle Bypass ❌

**检查:** RuntimeSession 必须通过 LifecycleController 创建

**当前状态:** ✅ 构造函数 internal

**Phase 2-B 要求:**
- ExecutionContext 必须通过 RuntimeSession.CreateExecution() 创建
- 禁止外部直接 new ExecutionContext()

### 2.5 Public API Leakage ❌

**检查:** 不得暴露不必要的 public 类型

**当前状态:** ✅ API Surface Frozen

**Phase 2-B 要求:**
- IHookRegistry: public (Extension Point)
- ExecutionHookRegistry: internal (默认实现)
- IExecutionHook: public (Consumer Interface)
- Runtime Events: public (观察者协议)

### 2.6 State Machine Pollution ❌

**检查:** 不得污染 RuntimeStateMachine

**当前状态:** ✅ StateMachine 独立

**Phase 2-B 要求:**
- ExecutionState 是独立枚举，不是 RuntimeState 扩展
- 不得在 RuntimeStateMachine 中添加 Execution 转换

---

## 3. Requirement Summary

### 3.1 Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | 创建 Execution 并获得唯一 ExecutionId | MUST |
| FR-2 | Execution 有独立生命周期状态 | MUST |
| FR-3 | Hook 可以在 Before/After/Failure/Cancelled 时机拦截 | MUST |
| FR-4 | Hook 按 Order 属性排序执行 | MUST |
| FR-5 | Runtime Events 可以被订阅和发布 | MUST |
| FR-6 | Execution Result 不可变且包含执行信息 | MUST |
| FR-7 | Cancellation 可以在 Execution 执行中触发 | MUST |
| FR-8 | Hook Registry 线程安全 | MUST |
| FR-9 | Runtime.Core 与 Runtime.Capability 隔离 | MUST |

### 3.2 Non-Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| NFR-1 | Hook 执行不影响 Execution 核心路径延迟 | SHOULD |
| NFR-2 | Event 发布不阻塞 Execution | SHOULD |
| NFR-3 | Hook Registry 支持并发 Register/Unregister | MUST |

### 3.3 Constraints

| ID | Constraint | Source |
|----|------------|--------|
| C-1 | Runtime.Core 不依赖 Runtime.Capability | ADR-006 |
| C-2 | Execution Hook 不得修改 RuntimeState | 本分析 |
| C-3 | Runtime Event 不得修改 RuntimeState | 本分析 |
| C-4 | Hook 失败不阻止 Execution | 本分析 |
| C-5 | Execution 不得承载业务数据 | LOCK-RUNTIME-CTX-01 |

---

## 4. Deferred Decisions

| Decision | Reason | Future Phase |
|----------|--------|--------------|
| Persistence (Execution State) | 保持节奏正确 | Phase 2-C+ |
| Distributed Execution | 单进程 MVP | Phase 2-C+ |
| Hook Timeout Policy | 需要更多 Usage Data | Phase 2-C+ |
| Execution Priority | 不在 MVP Scope | Phase 2-C+ |

---

## 5. Architecture Analysis Output

```
REQUIREMENT-ANALYSIS.md ✅
    ↓
ARCHITECTURE-ANALYSIS.md (本文档)
    ↓
DESIGN-SPEC.md (下一步)
```

---

**Status:** Analysis COMPLETED
**Next:** DESIGN-SPEC.md
