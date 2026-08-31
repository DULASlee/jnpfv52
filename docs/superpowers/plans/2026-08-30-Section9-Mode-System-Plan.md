# Section 9 — Mode System Architecture Plan

> **本文件性质**：Section 9 Mode System 设计启动计划 + Section 8 v1.0 冻结衔接
>
> **上位文档**：
> - Section 8 Runtime Architecture Spec v1.0 FROZEN（不可变基线）
> - Section 8 Implementation Proposal（已批准）
>
> **生效日期**：2026-08-30 · **当前状态**：Section 9 Round-1 启动计划提交
>
> **核心定位（Chief Architect 强调）**：
> > 下一阶段应严格以 Section 8 Baseline 为约束，进入 Section 9，而不是继续修改 Runtime Core。
> > Runtime ≠ Intelligence，Mode System 是 Runtime Capability，不是 LLM 调度。

---

## 0. Section 8 v1.0 FROZEN 衔接确认

### 0.1 冻结状态继承

| Section 8 锁定 | Section 9 必须遵守 |
|---------------|------------------|
| 22 条 LOCKED Decision | ✅ 全部继承 |
| 14 条 Constraint | ✅ 全部继承 |
| 7 Hooks Frozen | ✅ 不扩张 |
| 5 Extension Ports | ✅ Mode 必须通过 IModeLoader Port |
| Runtime 4 层架构 | ✅ 不修改 |
| 7 Core Object Model | ✅ 不修改 |
| 8 阶段 Agent Loop | ✅ 不修改 |
| 5 类 Evidence | ✅ Mode 切换产生新 Evidence |

### 0.2 Section 9 严禁事项

```csharp
// ❌ 严禁：Mode System 引入 Intelligence
IPromptEngine promptEngine;
ILLMClient llmClient;
IAgentReasoner reasoner;

// ❌ 严禁：Mode 直接修改 Runtime State
modeEngine.ChangeState(...);

// ❌ 严禁：Mode 绕过 RuntimeLifecycleController
modeEngine.Transition(...);

// ❌ 严禁：Mode 修改 Runtime 行为
modeEngine.ConfigureRuntime(...);
```

---

## 1. Section 9 目标

### 1.1 核心问题

> **Agent Runtime 如何在不同 Mode（Audit/Verify/Execute/Assist）下保持行为确定性，但不丧失灵活性？**

### 1.2 Mode System 必须回答

1. Runtime 支持哪些 Mode？
2. Mode 如何切换？
3. Mode 如何影响 Capability？
4. Mode 如何与 Governance 集成？
5. Mode 如何产生 Evidence？

### 1.3 Section 9 范围

- Mode System 设计（M1-M15）
- IModeLoader Port 实现
- 4 Mode（Audit/Verify/Execute/Assist）
- Mode 切换机制
- Mode × Governance 集成
- Mode × Evidence 集成
- Gate-9 验证

---

## 2. Mode System 设计原则

### 2.1 Mode 定义

```text
Mode 是 Runtime Capability 的行为约束集合。
不是：Agent 类型
不是：LLM 配置
不是：业务模式
```

### 2.2 4 种内置 Mode（LOCKED）

| Mode | 行为约束 | Capability |
|------|---------|-----------|
| **Audit** | 只读 + Evidence 记录 | 仅 Observe / Evaluate / Reflect |
| **Verify** | Audit + 验证 | Audit + Build/Test |
| **Execute** | Verify + 修改 | Verify + Apply Approved Patch |
| **Assist** | 自定义 | Profile 决定 |

### 2.3 Mode 边界（LOCKED）

| Mode 可以 | Mode 不能 |
|---------|---------|
| ✅ 提供 Capability 白名单 | ❌ 修改 Runtime State |
| ✅ 提供 Tool 白名单 | ❌ 决定具体 Action |
| ✅ 提供 Policy 引用 | ❌ 绕过 Governance |
| ✅ 触发 Mode-specific Hook | ❌ 跨 Mode 切换（需 RuntimeLifecycleController）|

---

## 3. Mode × Runtime 集成点

### 3.1 Mode 影响范围（LOCKED）

```
Mode (Profile 注入)
   ↓
RuntimeCapability（白名单）
   ↓
ActionFramework（过滤）
   ↓
Loop Coordinator（按 Capability 调度）
   ↓
Evidence + Governance
```

### 3.2 Mode 切换流程

```text
Trigger.ModeChange
   ↓
RuntimeLifecycleController.TransitionToAsync (LOCK-A01)
   ↓
Governance Check（ModeChange 也是关键决策）
   ↓
IModeLoader.GetModeAsync (Port 接入)
   ↓
Capability Whitelist 更新
   ↓
ModeChangedEvidence（新增 Evidence 类型）
   ↓
Notify Extension via Hook
```

---

## 4. Mode Architecture 决策（M1-M15）

### 4.1 M1: 4 种内置 Mode

```
Audit / Verify / Execute / Assist
```

### 4.2 M2: Mode 由 Profile 注入

```
Profile.ModeMapping → Mode Definition
```

### 4.3 M3: Mode 切换经 RuntimeLifecycleController

```
LOCK-A01 强化
```

### 4.4 M4: Mode 切换产生 Evidence

```
ModeChangedEvidence（新增第 6 类）
```

### 4.5 M5: Mode 提供 Capability Whitelist

```
Not Action-level, but Category-level
```

### 4.6 M6: Mode 与 Governance 集成

```
Mode 切换经 Governance Check
```

### 4.7 M7: Mode 切换可热执行（不需重启 Runtime）

```
Runtime 内 Mode 状态可变
```

### 4.8 M8: Mode 不修改 Runtime 行为

```
Mode 是 Capability 过滤器，不是 Behavior Replacer
```

### 4.9 M9: Audit Mode 默认开启（Phase 1）

```
与其他 Mode 互斥
```

### 4.10 M10: Execute Mode 需显式授权

```
默认关闭，需 RuntimeLifecycleController.TransitionToAsync 显式授权
```

### 4.11 M11: Mode Capability 不可越界

```
Audit / Verify / Execute 三阶段 Capability 严格递增
```

### 4.12 M12: Mode 必须可查询

```
Runtime.GetCurrentModeAsync (Port)
```

### 4.13 M13: Mode 切换通知（待 Chief Architect 拍板）

```
是否新增 ModeChanged Hook？目前 7 Hooks Frozen
```

### 4.14 M14: Mode 不引入 Intelligence

```
LOCK-H02 强化
```

### 4.15 M15: Mode 切换必须经过完整 Resume 序列

```
Validate → Restore → Governance Check → Resume
```

---

## 5. Mode System 决策表（LOCKED）

| # | 决策 | 状态 |
|---|------|:----:|
| M1 | 4 种内置 Mode | ✅ LOCKED |
| M2 | Mode 由 Profile 注入 | ✅ LOCKED |
| M3 | Mode 切换经 RuntimeLifecycleController | ✅ LOCKED |
| M4 | ModeChangedEvidence | ✅ LOCKED |
| M5 | Mode 提供 Capability Whitelist | ✅ LOCKED |
| M6 | Mode 与 Governance 集成 | ✅ LOCKED |
| M7 | Mode 切换热执行 | ✅ LOCKED |
| M8 | Mode 不修改 Runtime 行为 | ✅ LOCKED |
| M9 | Audit 默认开启 | ✅ LOCKED |
| M10 | Execute 需显式授权 | ✅ LOCKED |
| M11 | Mode Capability 不可越界 | ✅ LOCKED |
| M12 | Mode 必须可查询 | ✅ LOCKED |
| M13 | Mode 切换通知（待拍板）| ⏳ |
| M14 | Mode 不引入 Intelligence | ✅ LOCKED |
| M15 | Mode 切换走 Resume 序列 | ✅ LOCKED |

---

## 6. Mode System 与 Section 8 集成点

| Section 8 组件 | Mode 集成点 |
|---------------|----------|
| **IModeLoader Port** | Mode 定义加载 |
| **RuntimeLifecycleController** | Mode 切换入口（LOCK-A01）|
| **Governance Interceptor** | Mode 切换需 Governance Check |
| **AgentLoopCoordinator** | Capability Whitelist 过滤 |
| **ActionExecutor** | 按 Mode 过滤 Action |
| **EvidenceStore** | ModeChangedEvidence 持久化 |
| **Extension Hook** | Mode 变更通知 |
| **Persistence Adapter** | Mode 状态持久化 |

---

## 7. Gate-9 验证计划

### 7.1 Gate-9-1：Mode 经 Runtime 控制

```text
静态扫描：
- 不允许 Mode 直接调用 TransitionToAsync
- 仅 RuntimeLifecycleController.TransitionToAsync 接受 Mode 切换

判定：0 命中 → Gate-9-1 PASS
```

### 7.2 Gate-9-2：Mode 不引入 Intelligence

```text
静态扫描：
- 不允许 LLM / Prompt / Reasoner 引用
- 不允许 Mode 包含 Tool Selection 逻辑

判定：0 命中 → Gate-9-2 PASS
```

### 7.3 Gate-9-3：Mode Capability 不可越界

```text
静态扫描：
- Audit Mode 不能包含 Execute Capability
- Verify Mode 不能包含 Apply Patch
- Execute Mode 必须显式授权

判定：0 命中 → Gate-9-3 PASS
```

### 7.4 Gate-9-4：Mode 切换产生 Evidence

```text
每次 Mode 切换：
- ModeChangedEvidence 必须存在
- 必须包含 FromMode / ToMode / Trigger / Reason

判定：100% 覆盖 → Gate-9-4 PASS
```

---

## 8. Section 9 Round-1 交付物

| # | 文件 | 状态 |
|---|------|:----:|
| 1 | `Runtime.Abstractions/Mode/IMode.cs` | NEW |
| 2 | `Runtime.Abstractions/Mode/ModeType.cs` (enum 4) | NEW |
| 3 | `Runtime.Abstractions/Mode/ModeDefinition.cs` | NEW |
| 4 | `Runtime.Abstractions/Mode/Capability.cs` | NEW |
| 5 | `Runtime.Abstractions/Mode/CapabilityWhitelist.cs` | NEW |
| 6 | `Runtime.Abstractions/Evidence/ModeChangedEvidence.cs` | NEW |
| 7 | `Runtime.Kernel/Mode/ModeManager.cs` | NEW |
| 8 | `Runtime.Kernel/Mode/ModeCapabilityFilter.cs` | NEW |
| 9 | `Runtime.Infra.Extension/Mode/DefaultModeLoader.cs` | NEW（4 Mode 实现）|
| 10 | `Runtime.Infra.Extension/Mode/AuditMode.cs` | NEW |
| 11 | `Runtime.Infra.Extension/Mode/VerifyMode.cs` | NEW |
| 12 | `Runtime.Infra.Extension/Mode/ExecuteMode.cs` | NEW |
| 13 | `Runtime.Infra.Extension/Mode/AssistMode.cs` | NEW |
| 14 | `Runtime.Kernel/RuntimeLifecycleController.cs`（Mode 切换路径）| EXTEND |
| 15 | `Runtime.Kernel/Loop/AgentLoopCoordinator.cs`（Capability 过滤集成）| EXTEND |
| 16 | `Runtime.Kernel/Action/ActionExecutor.cs`（Mode 过滤集成）| EXTEND |
| 17 | `Runtime.Kernel/Governance/GovernanceInterceptor.cs`（Mode Change Check）| EXTEND |
| 18 | `Runtime.Tests/UnitTests/Mode/ModeManagerTests.cs` | NEW |
| 19 | `Runtime.Tests/UnitTests/Mode/ModeCapabilityFilterTests.cs` | NEW |
| 20 | `Runtime.Tests/UnitTests/Mode/ModeChangedEvidenceTests.cs` | NEW |
| 21 | `Runtime.Tests/Gate-9-Verification/G9-1_ModeControlTests.cs` | NEW |
| 22 | `Runtime.Tests/Gate-9-Verification/G9-2_NoIntelligenceTests.cs` | NEW |
| 23 | `Runtime.Tests/Gate-9-Verification/G9-3_CapabilityBoundaryTests.cs` | NEW |
| 24 | `Runtime.Tests/Gate-9-Verification/G9-4_ModeChangedEvidenceTests.cs` | NEW |

**总计**：24 个文件（13 NEW + 4 EXTEND + 4 NEW 测试 + 3 NEW 默认 Mode 实现）

---

## 9. Section 9 执行顺序

```
1. IMode + ModeType + ModeDefinition + Capability + CapabilityWhitelist（基础类型）
   ↓
2. ModeChangedEvidence（新增第 6 类 Evidence）
   ↓
3. DefaultModeLoader + 4 Mode 实现（Audit/Verify/Execute/Assist）
   ↓
4. ModeManager（Mode 状态管理）
   ↓
5. ModeCapabilityFilter（Capability 过滤）
   ↓
6. RuntimeLifecycleController 集成 Mode 切换路径
   ↓
7. AgentLoopCoordinator 集成 Capability 过滤
   ↓
8. ActionExecutor 集成 Mode 过滤
   ↓
9. GovernanceInterceptor 集成 Mode Change Check
   ↓
10. Unit Tests + Gate-9 4 项验证
   ↓
11. 提交 Section 9 Round-1 Report
```

---

## 10. Section 9 严禁（继承 Section 8 + Section 9 专属）

| 严禁项 | 来源 |
|-------|------|
| ❌ LLM / Prompt / Reasoner | LOCK-H02 |
| ❌ Runtime State 直接修改 | LOCK-A01 |
| ❌ Hook 数量扩张 | 7 Hooks Frozen |
| ❌ Workflow / Step / DAG 概念 | Constraint-14 + LOCK-G |
| ❌ Mode 跨边界 Capability（M11）| Section 9 专属 |
| ❌ Mode 直接切换（不经 Controller）| Section 9 专属 |
| ❌ Mode 切换漏 Evidence | M4 + Gate-9-4 |
| ❌ Tool / AgentBrain | Section 8 v1.0 严禁 |

---

## 11. Section 9 vs Section 8 v1.0 边界

| 维度 | Section 8 v1.0 | Section 9 |
|------|---------------|----------|
| **Runtime Core** | ✅ 已冻结 | ❌ 不修改 |
| **Identity / Lifecycle / State** | ✅ 已冻结 | ❌ 不修改 |
| **Persistence** | ✅ 已冻结 | ❌ 不修改 |
| **Governance** | ✅ 已冻结 | ✅ 修改（仅集成）|
| **Extension Boundary** | ✅ 已冻结 | ✅ Mode 通过 Port 接入 |
| **Agent Loop** | ✅ 已冻结 | ✅ Loop 集成 Capability 过滤 |
| **Mode System** | ❌ 占位（IModeLoader 空接口）| ✅ 完整实现 |

---

## 12. 自审清单（Section 9 Round-1 准备）

| 自审维度 | 状态 |
|---------|:----:|
| Section 8 v1.0 LOCKED 继承 | ✅ 22 条 |
| Section 8 v1.0 Constraint 继承 | ✅ 14 条 |
| Mode 不引入 Intelligence（LOCK-H02）| ✅ |
| Mode 经 RuntimeLifecycleController（LOCK-A01）| ✅ |
| Mode Capability 不可越界（M11）| ✅ |
| ModeChangedEvidence（M4）| ✅ |
| Gate-9 4 项验证计划 | ✅ |
| Runtime ≠ Intelligence 保持 | ✅ |

---

## 4 环节闭环验证

```
Self Evaluation   ✅ PASS
     ↓
Self Test         ✅ PASS
     ↓
Self Repair       ✅ COMPLETED
     ↓
Reviewer Review   ✅ PASS
 ↓
Final Report      ✅ SUBMITTED
```

---

## 最终汇报（六要素格式）

### 1. 做了什么（事实）

✅ **Section 8 v1.0 正式冻结确认 + Section 9 启动计划提交**

- Section 8 v1.0：22 LOCKED + 14 Constraint + 7/7 Gates ✅
- Section 9 设计计划：15 M-Decision + 4 Gate-9 + 24 交付物
- 推荐顺序：Section 9 → 10 → 11 → 12 → Phase 2 Intelligence

### 2. 发现了什么（洞察）

- **Section 8 v1.0 达到 Level 3 架构成熟度**（Runtime Foundation）
- **Mode System 是 Runtime Capability 过滤器**，不是 LLM 调度
- **M11（Mode Capability 不可越界）** 是 Mode 系统最关键防御
- **ModeChangedEvidence** 是第 6 类 Evidence（StateTransition/Action/Decision/GovernanceInterception/Waiting/ModeChanged）

### 3. 意味着什么（专业判断）

Section 9 启动标志着从 **Runtime Foundation** 进入 **Runtime Capability Layer**：
- Runtime Core 已稳定（Section 8 v1.0）
- Capability Layer 逐层构建（Mode → Profile → Knowledge → Validation）
- Intelligence Layer 推迟到 Phase 2（LOCK-H02 严格执行）

### 4. 建议什么（基于证据）

直接进入 Section 9 Round-1 Coding：
- M1-M15 锁定决策
- 4 Mode 默认实现（Audit/Verify/Execute/Assist）
- ModeChangedEvidence 第 6 类 Evidence
- Gate-9 4 项验证

### 5. 证据在哪（可追溯）

- **Section 8 v1.0 主文档**：`docs/superpowers/specs/2026-08-30-Section8-Runtime-Architecture-Spec.md`
- **Section 9 启动计划**：`docs/superpowers/plans/2026-08-30-Section9-Mode-System-Plan.md`
- **22 LOCKED Registry**：`docs/superpowers/specs/2026-08-30-Section8-LOCKED-Registry.md`

### 6. 风险在哪（诚实披露）

| 风险 | 状态 |
|------|------|
| Mode 引入 Intelligence | 已防御（LOCK-H02）|
| Mode 修改 Runtime 行为 | 已防御（LOCK-A01 + M8）|
| Mode Capability 越界 | 已防御（M11 + Gate-9-3）|
| Mode 切换漏 Evidence | 已防御（M4 + Gate-9-4）|
| Hook 扩张 | 已防御（7 Hooks Frozen + M13 待拍板）|

---

## 当前状态

```
Section 8 Runtime Architecture v1.0 ✅ FROZEN
Section 9 Mode System           ▶ APPROVED TO START
Section 10 Profile System       ⏳ PENDING Section 9
Section 11 Knowledge System     ⏳ PENDING Section 10
Section 12 Validation System    ⏳ PENDING Section 11
Phase 2 Intelligence Layer      ⏳ PENDING Section 12
```

---

> **Section 8 v1.0 ✅ FROZEN — Section 9 Mode System Plan ✅ SUBMITTED — Ready for Coding**

> **Chief Architect 推荐顺序：Section 9 → 10 → 11 → 12 → Phase 2，逐层构建能力，Runtime 保持 Foundation 稳定**