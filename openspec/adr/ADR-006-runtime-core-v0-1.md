# ADR-006：Runtime.Core v0.1 — Layer 0 Runtime Kernel (Section 8)

| 字段 | 内容 |
|------|------|
| 状态 | **已接受** |
| 日期 | 2026-08-31 |
| 决策者 | Chief Architect + AI 原生链施工 |
| 关联 | Phase 2-A · Section 8 Runtime Foundation · Chief Architect Review 2026-08-31 |

---

## 背景

Section 8 Runtime Foundation 是 Agent OS 的 Layer 0 Kernel，为 Section 9 Mode Integration 提供真实宿主。

Phase 2-A 实施前，存在两种路线选择：

| 方案 | 描述 | 问题 |
|------|------|------|
| A | 用 Stub Runtime 推进 S9-3 ModeTransitionController | 引入架构债，Contract Drift |
| B | 先实现 Runtime Kernel，再集成 Mode | 正确依赖顺序，长期可持续 |

**决策：选择方案 B。**

---

## 决策

### 1. Runtime.Core 作为独立 Layer 0 Assembly

```
JNPF.Runtime.Core (Layer 0)
      ↑
      |
JNPF.Runtime.Capability (Layer 1)
```

**约束：**
- `JNPF.Runtime.Core` 不依赖 `JNPF.Runtime.Capability`
- `JNPF.Runtime.Capability` 不依赖 `JNPF.Runtime.Core`（已在 Phase 1 验证）

**违反后果：** Runtime Kernel 退化为 Capability Container。

### 2. RuntimeContext 为纯 Data Carrier（R12 三元组）

```csharp
public sealed class RuntimeContext
{
    public string TenantId { get; }
    public string ProjectId { get; }
    public string PipelineId { get; }
    public string CreatorUserId { get; }
    public DateTime CreatedAtUtc { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
```

**LOCK-RUNTIME-CTX-01：** RuntimeContext MUST NOT contain execution capability (Agent/Capability/Memory/Tool/Model/Prompt/Skill/Plan/Workflow)。

**违反后果：** Layer 0 污染，Runtime 退化为 Workflow Engine。

### 3. RuntimeSession 构造函数为 internal

```csharp
internal RuntimeSession(RuntimeContext context)
```

**理由：**
- RuntimeSession 必须通过 RuntimeLifecycleController 创建
- 防止 Application Layer 绕过生命周期管理直接 `new RuntimeSession()`
- 测试通过 `[assembly: InternalsVisibleTo("JNPF.Tests.Runtime.Core")]` 访问

**违反后果：** 非法生命周期绕过。

### 4. State Machine 强制单向转换

```
Created → Initialized → Running ↔ Paused → Completed
                              ↘ Failed
(任意非 Disposed) ────────────────────────────→ Disposed
```

**约束：**
- 禁止反向转换（Completed → Running 等）
- Disposed 是终态，不可转换到任何状态
- Disposed 是所有非终态的逃生口（资源清理）

**违反后果：** Agent Recovery 逻辑混乱。

### 5. RuntimeLifecycleController 单一会话约束

```csharp
public sealed class RuntimeLifecycleController : IRuntimeLifecycleController
{
    public RuntimeSession? CurrentSession { get; private set; }

    // 同时只允许一个活跃会话
    public Task<RuntimeSession> InitializeAsync(...)
    {
        if (CurrentSession != null)
            throw new InvalidOperationException("A session already exists.");
        // ...
    }
}
```

**当前实现：** In-memory synchronized Dictionary（Prototype Store）

**Future ADR：** Distributed Runtime Store（Redis/DB）

**禁止：** 当前阶段引入持久化机制（保持节奏正确）。

---

## 公开 API Surface（v0.1 Frozen）

| 类型 | 可见性 | 说明 |
|------|--------|------|
| `RuntimeContext` | public | 三元组载体 |
| `RuntimeSession` | public | 会话（构造函数 internal） |
| `RuntimeState` | public | 7 状态枚举 |
| `IRuntimeLifecycleController` | public | 生命周期接口 |
| `RuntimeLifecycleController` | public | 默认实现 |
| `RuntimeStateMachine` | public | 状态转换规则 |

**禁止在 v0.2 前添加：**
- Hook/Event Pipeline
- Mode Integration
- Profile/Knowledge 概念
- LLM/Prompt/Reasoner

---

## 测试覆盖（v0.1）

| 测试类 | 数量 | 覆盖 |
|--------|------|------|
| `RuntimeContextTests` | 8 | 三元组创建、不可变性、元数据 |
| `RuntimeStateMachineTests` | 25 | 合法/非法转换、终态约束 |
| `RuntimeLifecycleControllerTests` | 12 | 完整生命周期、错误处理 |
| `RuntimeContractTests` | 31 | 隔离/并发/销毁安全/状态矩阵 |

**总计：76 tests PASS**

---

## 后果

### 正面

- Layer 0 / Layer 1 边界清晰，无循环依赖
- Runtime Kernel 不包含任何 Intelligence/Workflow 概念
- 状态机提供强生命周期保证
- 测试覆盖 Contract Boundary（隔离/并发/销毁安全）

### 负面

- 当前 Session 存储在内存，进程重启丢失
- 无 Hook Pipeline，Runtime 事件未暴露给外部
- 单会话模型不适合多 Agent 并发场景

---

## 相关文件

| 模块 | 路径 |
|------|------|
| Runtime.Core 项目 | `backend/modularity/runtime/JNPF.Runtime.Core/` |
| RuntimeContext | `backend/modularity/runtime/JNPF.Runtime.Core/RuntimeContext.cs` |
| RuntimeSession | `backend/modularity/runtime/JNPF.Runtime.Core/RuntimeSession.cs` |
| RuntimeState | `backend/modularity/runtime/JNPF.Runtime.Core/RuntimeState.cs` |
| IRuntimeLifecycleController | `backend/modularity/runtime/JNPF.Runtime.Core/IRuntimeLifecycleController.cs` |
| RuntimeLifecycleController | `backend/modularity/runtime/JNPF.Runtime.Core/RuntimeLifecycleController.cs` |
| RuntimeStateMachine | `backend/modularity/runtime/JNPF.Runtime.Core/RuntimeStateMachine.cs` |
| Contract Tests | `backend/tests/JNPF.Tests.Runtime.Core/RuntimeContractTests.cs` |
| 测试项目 | `backend/tests/JNPF.Tests.Runtime.Core/` |

---

## 下一步

| Phase | 内容 | 前置条件 |
|-------|------|----------|
| Phase 2-A.1（当前） | Contract Hardening Gate | ✅ CLOSED |
| Phase 2-B | Hook Registry + Execution Boundary | Phase 2-A.1 审批 |
| Phase 2-C | Section 9 Integration | Phase 2-B 审批 |
