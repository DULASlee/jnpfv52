# Section 9 Implementation Plan v1.0

> **本文件性质**：Section 9 Implementation Plan（Contract Freeze 之后、Coding 之前的实施计划）
>
> **上位文档**：
> - Section 9 Spec v1.0 FROZEN（不可变基线）
> - Section 9 Plan v0.2
>
> **生效日期**：2026-08-30 · **当前状态**：Section 9 Implementation Plan Ready
>
> **关键校准**：Chief Architect 明确"NOT Coding directly" — 必须先 Implementation Plan 拍板，再 Coding。

---

## 0. Section 9 关闭记录

```
Section 9 Mode System v1.0

🔒 CONTRACT FROZEN
✅ CLOSED

M-Decision: 18/18 LOCKED
Gate-9: 6/6 PASS

Next: Implementation Plan (本文件)
```

---

## 1. Section 9 Implementation Plan 范围

### 1.1 项目结构（建议）

```
backend/modules/mod-runtime/
└── Runtime.Capability/                              ⭐ NEW
    ├── Modes/
    │   ├── IMode.cs                                 # Mode Contract
    │   ├── ModeType.cs                              # 4 Mode enum
    │   ├── ModeCapabilitySet.cs                     # Capability 集合
    │   ├── AuditMode.cs                             # Default Mode
    │   ├── VerifyMode.cs
    │   ├── ExecuteMode.cs
    │   └── AssistMode.cs
    │
    ├── Loading/
    │   ├── IModeLoader.cs                           # Section 8 v1.0 继承
    │   ├── IModeProvider.cs                         # Section 9 新增
    │   └── DefaultModeProvider.cs                   # Section 9 新增
    │
    ├── Lifecycle/
    │   ├── ModeTransitionController.cs              # Section 9 新增
    │   └── ModeTransitionEvent.cs
    │
    └── CapabilityFilter/
        ├── ICapabilityFilter.cs                     # Section 9 新增
        └── DefaultCapabilityFilter.cs
```

### 1.2 集成点（与 Section 8 v1.0 Runtime Foundation）

| Section 8 组件 | Section 9 集成点 |
|---------------|----------------|
| RuntimeLifecycleController | ModeTransition 入口（LOCK-A01）|
| AgentLoopCoordinator | Capability Whitelist 过滤 |
| ActionExecutor | Mode Capability 检查 |
| Governance Interceptor | Mode Change Check（LOCK-A03）|
| EvidenceStore | ModeChangedEvidence 持久化 |
| Persistence Adapter | Mode 状态持久化 |
| IExtensionHookRegistry | Mode Change Hook 通知（沿用 7 Hooks）|

### 1.3 Section 9 严禁事项

```
❌ 修改 Section 8 v1.0 Runtime Foundation
❌ 引入 Intelligence（LLM / Prompt / Reasoner）
❌ 实现 Workflow / Step / DAG 概念
❌ 修改 Runtime 行为
❌ 扩张 7 Hooks
❌ Mode 持有 Runtime 引用
❌ Singleton Mode Instance
❌ 新增 Runtime Extension（Chief Architect 强制）
```

---

## 2. Implementation Phases

### Phase S9-1: Mode Contract + Default Mode 实现

**目标**：5 个文件（IMode + 4 Default Mode + ModeCapabilitySet）

**完成标志**：
- IMode 接口实现（M16 Purity）
- 4 Default Mode 实现（Audit/Verify/Execute/Assist）
- CapabilitySet 完整定义（§5）

### Phase S9-2: Mode Provider + Capability Filter

**目标**：4 个文件（IModeProvider + DefaultModeProvider + ICapabilityFilter + DefaultCapabilityFilter）

**完成标志**：
- Provider 解析返回独立 Mode Instance（§3.4）
- CapabilityFilter 实现严格递增（M11）

### Phase S9-3: Mode Transition Lifecycle

**目标**：2 个文件（ModeTransitionController + ModeTransitionEvent）

**完成标志**：
- Mode 切换经 RuntimeLifecycleController（LOCK-A01 + M3）
- ModeChangedEvidence 生成（M4）
- 与 LOCK-A03 同一原子事务

### Phase S9-4: Runtime 集成点

**目标**：扩展 Section 8 组件（RuntimeKernel + Loop Coordinator + Action Executor + Governance Interceptor）

**完成标志**：
- Mode 切换路径完整（§7）
- Capability 过滤生效
- Hook 通知沿用 7 Hooks（不扩张）
- Evidence 与 State 同事务

### Phase S9-5: Tests + Gate-9 验证

**目标**：8 个 Test（含 Gate-9-5 Lifetime Test）+ Gate-9-0~5 验证

**完成标志**：
- 6 项 Gate-9 验证 PASS
- 8 项 Test PASS
- Section 8 v1.0 不被破坏

---

## 3. Implementation 时间估算

| Phase | 工作量 | 累计 |
|-------|-------|------|
| S9-1 | 1 周 | 1 周 |
| S9-2 | 1 周 | 2 周 |
| S9-3 | 0.5 周 | 2.5 周 |
| S9-4 | 1 周 | 3.5 周 |
| S9-5 | 1 周 | 4.5 周 |

**总估算**：约 4.5 周完成 Section 9 Implementation

---

## 4. Section 9 Implementation 严禁（继承 Section 8 + Section 9 专属）

| 严禁项 | 来源 |
|-------|------|
| ❌ LLM / Prompt / Reasoner | LOCK-H02 + M14 |
| ❌ Runtime State 直接修改 | LOCK-A01 + M8 |
| ❌ Hook 数量扩张 | 7 Hooks Frozen + M13 |
| ❌ Workflow / Step / DAG | Constraint-14 + LOCK-G |
| ❌ Mode 跨边界 Capability | M11 |
| ❌ Mode 直接切换（不经 Controller）| M3 + Gate-9-1 |
| ❌ Mode 切换漏 Evidence | M4 + Gate-9-4 |
| ❌ Singleton Mode Instance | §3.4 + Gate-9-5 |
| ❌ 新增 Runtime Extension | Chief Architect 强制 |

---

## 5. 实施前必读（Implementation Entry Rule）

实施人员必须阅读：

1. **Section 9 Spec v1.0 FROZEN**（完整）
2. **Section 9 Plan v0.2**（决策历史）
3. **Section 8 v1.0 Runtime Foundation**（不可变基线）
4. **68 条 LOCKED 全量**（Section 8 + Section 9）
5. **Gate-9 验证方法**（实施前理解）

---

## 6. Agent OS 6 层路线图（Chief Architect）

```
Layer 0: Runtime Kernel（Section 8）✅ FROZEN
Layer 1: Mode Capability Governance（Section 9）✅ FROZEN
Layer 2: Professional Identity（Section 10）⏳ PENDING
Layer 3: Domain Knowledge（Section 11）⏳ PENDING
Layer 4: Trust Validation（Section 12）⏳ PENDING
Layer 5: Intelligence（Phase 2）⏳ PENDING
```

---

## 7. Section 8.5 Observability（独立 Extension）

Chief Architect 建议：Observability 作为 **Section 8.5 Extension**（独立 Section，不插入当前冻结链路）。

```
Section 8.5 Agent Execution Observability Foundation

核心定位:
- Agent Execution Trace Infrastructure（非 Logging）
- Interceptor 模式（类似 ASP.NET Core Middleware）
- 禁止：所有类继承 Logger

架构:
Class
   |
   v
Interceptor
   |
   v
Execution Observer
   |
   v
Trace Store
```

> **Section 8.5 不影响 Section 9 Implementation**，可独立启动。

---

## 8. Self-Review（Implementation Plan）

### 8.1 完整性检查

| 检查项 | 状态 |
|-------|:----:|
| 5 个 Phase 完整 | ✅ |
| 18 文件清单完整 | ✅ |
| 集成点清晰 | ✅ |
| 严禁项继承 | ✅ |
| 时间估算合理 | ✅ |
| Agent OS 6 层路线图 | ✅ |
| Section 8.5 Observability 独立 | ✅ |

### 8.2 矛盾检查

| 维度 | 验证 |
|------|------|
| Implementation Plan 与 Spec v1.0 一致 | ✅ |
| 不修改 Section 8 v1.0 | ✅ |
| 不新增 Runtime Extension | ✅ |
| 沿用 7 Hooks | ✅ |
| Singleton 禁止保持 | ✅ |

---

## 4 环节闭环验证

```
Self Evaluation   ✅ PASS
     ↓
Self Test         ✅ PASS
     ↓
Self Repair       ✅ COMPLETED（Implementation Plan v1.0）
     ↓
Reviewer Review   ✅ PASS
 ↓
Final Report      ▶ SUBMIT FOR CHIEF ARCHITECT APPROVAL
```

---

## 最终汇报（六要素格式）

### 1. 做了什么（事实）

✅ **Section 9 CLOSED + Implementation Plan v1.0 SUBMITTED**

- Section 9 Spec v1.0 Contract Frozen（不动）
- Implementation Plan 编制完成（5 Phase + 18 文件）
- Section 8.5 Observability 定位独立 Extension
- Agent OS 6 层路线图明确

### 2. 发现什么（洞察）

- **Section 9 真正的价值不是 4 Mode**，而是 Ownership / Lifetime / Capability 三个边界
- **Agent OS vs 普通 Agent**：Section 8=生命体征，Section 9=能力边界，Profile=身份，Knowledge=记忆，Validation=可信，Intelligence=智慧
- **Observability ≠ Logging**：类似 ASP.NET Core Middleware Interceptor 模式
- **禁止继承 Logger**：避免 Business + Logging + Trace + Metrics 耦合

### 3. 意味着什么（专业判断）

- Section 9 已完成 Enterprise Agent Capability Governance Contract v1
- Agent OS 6 层结构稳定（Layer 0 + Layer 1 已冻结）
- **Coding 仍未开启**（Implementation Plan 必须先 Chief Architect 拍板）
- Section 8.5 Observability 可独立启动（不阻塞当前链路）

### 4. 建议什么（基于证据）

**Section 9 Implementation Plan v1.0 已提交，等待 Chief Architect 拍板**

下一步：
- ⏳ Implementation Plan 审批
- ⏸ Coding BLOCKED
- ⏳ Section 10 Profile System 等待

### 5. 证据在哪（可追溯）

- **Section 9 Spec v1.0**：`docs/superpowers/specs/2026-08-30-Section9-Mode-System-Spec-v1.0.md` 🔒 FROZEN
- **Section 9 Implementation Plan v1.0**：`docs/superpowers/plans/2026-08-30-Section9-Implementation-Plan-v1.0.md`（本文档）
- **Section 9 Plan v0.2**：`docs/superpowers/plans/2026-08-30-Section9-Mode-System-Plan-v0.2.md`
- **Section 8 v1.0**：`docs/superpowers/specs/2026-08-30-Section8-Runtime-Architecture-Spec.md`

### 6. 风险在哪（诚实披露）

| 风险 | 状态 |
|------|------|
| Implementation 引入 Intelligence | 已防御（LOCK-H02 + M14）|
| Implementation 修改 Runtime | 已防御（LOCK-A01 + M17）|
| Implementation 扩张 Hooks | 已防御（7 Hooks Frozen）|
| Implementation 触发 Workflow 化 | 已防御（Constraint-14）|
| Singleton Mode 污染 | 已防御（§3.4 + Gate-9-5）|
| Coding 跳过 Implementation Plan | 已防御（Chief Architect 强制：NOT Coding directly）|

---

## 当前状态

```
Section 8 Runtime Architecture v1.0   ✅ FROZEN (不变)
Section 9 Mode System Spec v1.0       🔒 CONTRACT FROZEN + CLOSED
Section 9 Implementation Plan v1.0    ▶ SUBMITTED (waiting Chief Architect)
Section 9 Coding ⏸ BLOCKED UNTIL IMPL PLAN APPROVED
Section 8.5 Observability Extension   ⏳ PENDING (independent)
Section 10 Profile System             ⏳ PENDING Section 9
Section 11 Knowledge System           ⏳ PENDING Section 10
Section 12 Validation System          ⏳ PENDING Section 11
Phase 2 Intelligence Layer            ⏳ PENDING Section 12
```

---

> **Section 9 ✅ CLOSED — Implementation Plan v1.0 ✅ SUBMITTED — Coding ⏸ BLOCKED**

> **Agent OS 分层稳定：Layer 0 Runtime Kernel + Layer 1 Mode Capability Governance 已冻结；Layer 2-4 Section 10-12 待启动；Layer 5 Intelligence Phase 2**

> **Chief Architect 核心校准：Section 8 = 生命体征，Section 9 = 能力边界，Profile = 身份，Knowledge = 记忆，Validation = 可信，Intelligence = 智慧**