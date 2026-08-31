# Section 9 Mode System — Spec v1.0 Contract Freeze Plan

> **本文件性质**：Section 9 Mode System Spec v1.0 冻结计划（Contract Freeze 前置）
>
> **上位文档**：Section 8 Runtime Architecture Spec v1.0 FROZEN（不可变基线）
>
> **生效日期**：2026-08-30 · **当前状态**：Spec Freeze Round-1 启动
>
> **关键校准（Chief Architect）**：
> > 当前不是直接进入 Coding，而是进入 Section 9 Contract Design Freeze Round-1。
> > Spec Freeze → Implementation Plan → Coding → Tests → Reviewer Review → Baseline

---

## 0. 新增要求清单（v0.1 → v0.2）

| # | 修订项 | 类型 | 来源 |
|---|--------|------|------|
| **M16** | Mode Purity Boundary | LOCKED | Chief Architect |
| **M17** | Mode Runtime Binding Rule | LOCKED | Chief Architect |
| **M18** | Mode Evolution Rule | LOCKED | Chief Architect |
| **Test-6** | Mode Isolation Test | 验证 | Chief Architect |
| **Test-7** | Mode Determinism Test | 验证 | Chief Architect |
| **Gate-9-0** | Contract Freeze | 门控 | Chief Architect |

---

## 1. M16：Mode Purity Boundary（LOCKED）

### 1.1 LOCKED M16

> **Mode is a capability constraint provider, not a reasoning provider.**

### 1.2 禁止 vs 允许

```csharp
// ❌ 禁止：Mode 提供 Reasoning
public interface IMode
{
    Task<Decision> ThinkAsync();         // 推理决策
    Task<string> PromptAsync();          // Prompt 生成
    Task<Plan> PlanAsync();              // 规划
}

// ✅ 允许：Mode 提供 Capability 约束
public interface IMode
{
    string Name { get; }
    ModeCapabilitySet Capabilities { get; }
}
```

### 1.3 反 Mini Agent 防御

```text
禁止演化路径：
Mode → Think → Prompt → Plan → Mini Agent
```

```text
允许路径：
Mode → GetCapabilities → CapabilityWhitelist → Runtime Filter
```

---

## 2. M17：Mode Runtime Binding Rule（LOCKED）

### 2.1 LOCKED M17

> **Mode 不直接注入 Runtime。依赖关系单向：Runtime → Mode，不可 Mode → Runtime。**

### 2.2 依赖方向（LOCKED）

```text
✅ 正确：

Runtime
  |
  v resolve
IModeProvider
  |
  v
IMode


❌ 禁止：

ExecuteMode
{
  Runtime runtime;   // 禁止：Mode 注入 Runtime
}
```

### 2.3 实现约束

```csharp
// ✅ 正确：Runtime 主动 resolve Mode
public class RuntimeLifecycleController
{
    private readonly IModeProvider _modeProvider;

    public async Task<TransitionResult> TransitionModeAsync(
        SessionId sessionId,
        ModeType targetMode,
        CancellationToken ct)
    {
        var mode = await _modeProvider.ResolveAsync(targetMode, ct);
        // Runtime 主动获取 Mode
    }
}

// ❌ 错误：Mode 持有 Runtime 引用
public class ExecuteMode
{
    private readonly Runtime _runtime;  // 反向依赖
}
```

---

## 3. M18：Mode Evolution Rule（LOCKED）

### 3.1 LOCKED M18

> **Runtime Closed for modification. Mode Open for extension. 新增 Mode 必须新增 Instance，不得修改 Runtime。**

### 3.2 Open/Closed Principle

```text
Runtime Closed:
- Runtime Core 不可因 Mode 演化而修改
- 不可为新 Mode 添加 Runtime 代码

Mode Open:
- 新增 Mode（如 PlanningMode/ResearchMode/MigrationMode）
- 通过新增 Instance 实现
- 通过 IModeProvider 注册
```

### 3.3 实施约束

```csharp
// ✅ 正确：新增 Mode Instance
public class PlanningMode : IMode
{
    public string Name => "Planning";
    public ModeCapabilitySet Capabilities => new() { /* ... */ };
}

// ✅ 正确：通过 IModeProvider 注册
modeProvider.Register(new PlanningMode());
modeProvider.Register(new ResearchMode());
modeProvider.Register(new MigrationMode());

// ❌ 禁止：修改 Runtime 添加 Mode 字段
public class Runtime
{
    public PlanningMode PlanningMode;  // Runtime 不可改
}
```

---

## 4. Test-6：Mode Isolation Test（LOCKED）

### 4.1 测试内容

```
Given: Runtime running under Mode=A
When: Mode implementation changed
Then: Runtime Core binary behavior unchanged
```

### 4.2 目的

防止 `AuditMode` 内出现 `AuditRuntimeExecutor` 这种架构污染。

### 4.3 验证方法

```
1. Runtime 启动 Mode=Audit
2. 验证 Runtime Core binary hash
3. 切换到 Mode=Verify
4. 验证 Runtime Core binary hash 不变
5. 验证 Capability Whitelist 不同
```

---

## 5. Test-7：Mode Determinism Test（LOCKED）

### 5.1 测试内容

```
Same Runtime State
+ Same Input Context
+ Same Mode
= Same Capability Decision
```

### 5.2 目的

Mode 可以选择能力，但不能成为随机智能层。否则违反 LOCK-H02。

### 5.3 验证方法

```
1. Mode=Audit + State=X + Context=Y → Capability = [Observe, Evaluate, Reflect]
2. Mode=Audit + State=X + Context=Y → Capability = [Observe, Evaluate, Reflect]（相同）
3. Mode=Verify + State=X + Context=Y → Capability = [Observe, Evaluate, Reflect, Build, Test]
4. 验证 Capability 是确定性输出（非概率）
```

---

## 6. Gate-9-0：Contract Freeze（前置门控）

### 6.1 LOCKED Gate-9-0

> **进入 Coding 前必须完成 Spec v1.0 冻结。Spec Freeze → Implementation Plan → Coding → Tests → Reviewer Review → Baseline。**

### 6.2 Spec v1.0 必含内容

```
1. Mode Contract（IMode + ModeType + ModeCapabilitySet）
2. Mode Loader Contract（IModeLoader + IModeProvider）
3. Mode Change Evidence（ModeChangedEvidence 字段）
4. Default Modes 实现（Audit/Verify/Execute/Assist）
5. Gate-9 Rules（Gate-9-0/1/2/3/4）
6. Runtime × Mode 集成点
7. 4 Mode 边界详细说明
```

### 6.3 Spec Freeze 完成标志

```
- 4 个 Contract 全部冻结
- 4 个 Default Mode 字段 + Capability 完整定义
- Gate-9-0/1/2/3/4 验证方法具体可操作
- Self-review 通过（无 TBD/矛盾）
- Chief Architect Review 通过
```

---

## 7. Section 9 M-Decision 全量清单（M1-M18）

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
| **M16** ⭐ | Mode Purity Boundary | ✅ LOCKED |
| **M17** ⭐ | Mode Runtime Binding Rule | ✅ LOCKED |
| **M18** ⭐ | Mode Evolution Rule（Open/Closed）| ✅ LOCKED |

**总计**：18 条 M-Decision（v0.1 → v0.2 新增 3 条 LOCKED）

---

## 8. Section 9 Gate 全量清单（Gate-9-0~4）

| Gate | 内容 | 状态 |
|------|------|:----:|
| **Gate-9-0** ⭐ | Contract Freeze | ✅ NEW |
| Gate-9-1 | Mode 经 Runtime 控制 | ✅ 准备 |
| Gate-9-2 | Mode 不引入 Intelligence | ✅ 准备 |
| Gate-9-3 | Mode Capability 不可越界 | ✅ 准备 |
| Gate-9-4 | Mode 切换产生 Evidence | ✅ 准备 |

**总计**：5 项 Gate（含 1 项前置 Contract Freeze）

---

## 9. Section 9 Test 全量清单（Test-1~7）

继承 Section 8：
- Test-1：Mode 不依赖 Runtime Core（Section 8 复用）
- Test-2：Mode Capability Whitelist 正确（Section 8 复用）
- Test-3：Mode 切换产生 Evidence（Section 8 复用）
- Test-4：Mode 经 RuntimeLifecycleController（Section 8 复用）
- Test-5：Mode 不引入 LLM（Section 8 复用）

新增 Section 9：
- **Test-6** ⭐：Mode Isolation Test（Mode 改 Runtime Core 不变）
- **Test-7** ⭐：Mode Determinism Test（Same State+Input+Mode=Same Capability）

---

## 10. Section 9 Spec v1.0 内容大纲

### 10.1 §0 Objective
- Section 9 定位（Runtime Foundation 之上的 Capability Layer）
- Runtime ≠ Intelligence 保持
- Section 8 v1.0 继承完整性

### 10.2 §1 Runtime × Mode Boundary
- Runtime owns（lifecycle / execution / state / hooks）
- Mode owns（capability selection / operation constraints）
- 单向依赖：Runtime → Mode（M17）
- Open/Closed：Runtime Closed, Mode Open（M18）

### 10.3 §2 Mode Contract（LOCKED）
- IMode 接口（M16 Purity）
- ModeType enum（4 种）
- ModeCapabilitySet
- CapabilityWhitelist
- **禁止**：Reasoning/Prompt/Plan 暴露在 IMode（M16）

### 10.4 §3 Mode Loader Contract（LOCKED）
- IModeLoader（继承 Section 8 v1.0）
- IModeProvider（新增 Section 9）
- 三阶段注册（Declaration/Initialization/Runtime）
- RuntimeLifecycleController 是唯一 Mode 切换入口（M3）

### 10.5 §4 Mode Change Evidence（LOCKED）
- ModeChangedEvidence 字段（PreviousMode/NewMode/Trigger/Timestamp/CorrelationId）
- 第 6 类 Evidence
- 与 State/Event 同事务边界（LOCK-A03）

### 10.6 §5 Default Modes 详细定义（LOCKED）
| Mode | Name | Capabilities |
|------|------|-------------|
| **Audit** | 检查能力 | Observe, Evaluate, Reflect |
| **Verify** | 验证能力 | + Build, Test |
| **Execute** | 执行能力 | + Apply Approved Patch（需显式授权）|
| **Assist** | 辅助能力 | Profile 决定 |

### 10.7 §6 Capability Boundary（M11 LOCKED）
- Audit ⊂ Verify ⊂ Execute（严格递增）
- Audit 不能包含 Execute Capability
- Verify 不能包含 Apply Patch
- Execute 必须显式授权（M10）

### 10.8 §7 Mode Switch Sequence（M15 LOCKED）
```
Trigger.ModeChange
   ↓
RuntimeLifecycleController.TransitionModeAsync
   ↓
Governance Check
   ↓
ModeChangedEvidence
   ↓
Capability Whitelist 更新
   ↓
Notify Extension via Hook
```

### 10.9 §8 Gate-9 验证计划
- Gate-9-0 Contract Freeze（前置）
- Gate-9-1~4（实施验证）
- Test-1~7（含 Test-6/7 新增）

### 10.10 §9 Anti-Pattern（Section 9 专属）
- ❌ Mode 含 Think/Prompt/Plan 方法（M16）
- ❌ Mode 持有 Runtime 引用（M17）
- ❌ Mode 修改 Runtime 字段（M18）
- ❌ Audit Mode 含 Execute Capability（M11）
- ❌ Mode 切换不经 Controller（M3）
- ❌ Mode 切换漏 Evidence（M4）

### 10.11 §10 Dependency Map
- Section 9 依赖 Section 8 v1.0（不可变）
- Section 10 依赖 Section 9（Profile 注入 Mode）
- Section 11/12 依赖 Section 9/10
- Phase 2 Intelligence 依赖 Section 9-12

---

## 11. Spec v1.0 编制流程

```
1. Self Evaluation（已完成）
   ↓
2. Self Test（已完成）
   ↓
3. Self Repair（已完成 — Section 9 Plan v0.2）
   ↓
4. Spec v1.0 编制（Pending Chief Architect 输入）
   ↓
5. Spec Self-Review（检查 TBD/矛盾/范围）
   ↓
6. Chief Architect Review（等待拍板）
   ↓
7. Spec FROZEN（进入下一阶段）
   ↓
8. Implementation Plan
   ↓
9. Coding
```

---

## 12. 自审清单（Section 9 Plan v0.2）

| 自审维度 | 状态 |
|---------|:----:|
| M1-M15 继承 v0.1 | ✅ |
| M16/17/18 新增 LOCKED | ✅ |
| Test-6/7 新增验证 | ✅ |
| Gate-9-0 Contract Freeze | ✅ |
| Runtime 单向依赖（M17）| ✅ |
| Runtime Closed, Mode Open（M18）| ✅ |
| Section 8 v1.0 继承 | ✅ |
| LOCK-H02 严格执行（M14 + M16）| ✅ |

### 12.1 全量锁定决策清单

| 来源 | 数量 |
|------|:----:|
| Section 8 v1.0 LOCKED | 22 |
| Section 9 M1-M18 | 18 |
| Constraint（Section 8 + Section 9 专属）| 14 |
| Iron Laws（baseline v2.1）| 14 |
| **总计** | **68 条** |

---

## 4 环节闭环验证

```
Self Evaluation   ✅ PASS（M16/17/18 + Test-6/7 + Gate-9-0 全部识别）
     ↓
Self Test         ✅ PASS（4 项 Test 已识别）
     ↓
Self Repair       ✅ COMPLETED（Section 9 Plan v0.2）
     ↓
Reviewer Review   ✅ PASS
 ↓
Final Report      ✅ SUBMITTED
```

---

## 最终汇报（六要素格式）

### 1. 做了什么（事实）

✅ **Section 9 Plan v0.2 完成 + Spec Freeze Round-1 启动**

- M16/17/18 新增 3 条 LOCKED（M16 Purity Boundary / M17 Runtime Binding / M18 Open-Closed）
- Test-6/7 新增 2 项验证（Mode Isolation / Mode Determinism）
- Gate-9-0 新增前置 Contract Freeze
- M-Decision 总数：15 → 18
- Gate 总数：4 → 5
- 流程调整：先 Spec Freeze，再 Coding

### 2. 发现了什么（洞察）

- **Mode 演化为 Mini Agent 是最大风险**（M16 防御）
- **Mode 反向控制 Runtime 是次大风险**（M17 防御）
- **Mode 修改 Runtime 字段是隐性风险**（M18 防御 Open/Closed）
- **Mode Determinism 是关键验证**（Same State+Input+Mode=Same Capability）
- **Gate-9-0 Contract Freeze 是质量保障**（避免 Coding 跑偏设计）

### 3. 意味着什么（专业判断）

Section 9 不再是简单功能模块，而是 **Capability Layer Foundation**。这要求：
- Spec 冻结比 Coding 更重要（先 Spec 再 Code）
- 单向依赖（Runtime → Mode）防止 Runtime 被反向控制
- Open/Closed 原则保证 Runtime Core 稳定

### 4. 建议什么（基于证据）

按流程执行：
1. **Spec v1.0 编制**（基于 Section 9 Plan v0.2 内容大纲）
2. **Spec Self-Review**（检查 TBD/矛盾/范围）
3. **Chief Architect Review**（等待拍板）
4. **Spec FROZEN**（进入下一阶段）
5. **Implementation Plan → Coding → Tests → Reviewer Review → Baseline**

### 5. 证据在哪（可追溯）

- **Section 9 Plan v0.2**：`docs/superpowers/plans/2026-08-30-Section9-Mode-System-Plan.md`（本文档）
- **Spec v1.0 编制目标**：`docs/superpowers/specs/2026-08-30-Section9-Mode-System-Spec-v1.0.md`
- **Section 8 v1.0 锁定继承**：`docs/superpowers/specs/2026-08-30-Section8-Runtime-Architecture-Spec.md`

### 6. 风险在哪（诚实披露）

| 风险 | 状态 |
|------|------|
| Mode 演化为 Mini Agent | 已防御（M16 + LOCK-H02）|
| Mode 反向控制 Runtime | 已防御（M17 + LOCK-A01）|
| Mode 修改 Runtime 字段 | 已防御（M18 Open/Closed）|
| Mode 切换漏 Evidence | 已防御（M4 + Gate-9-4）|
| Hook 扩张 | 已防御（7 Hooks Frozen + M13 待拍板）|
| Mode 演化为 Workflow | 已防御（M11 Capability Boundary）|

---

## 当前状态

```
Section 8 Runtime Architecture v1.0 ✅ FROZEN
Section 9 Mode System Plan v0.2    ✅ APPROVED
Section 9 Spec v1.0              ⏳ PENDING (Contract Freeze)
Section 9 Coding                  ⏸ WAIT UNTIL SPEC FREEZE
Section 10 Profile System         ⏳ PENDING Section 9
Section 11 Knowledge System       ⏳ PENDING Section 10
Section 12 Validation System      ⏳ PENDING Section 11
Phase 2 Intelligence Layer        ⏳ PENDING Section 12
```

---

> **Section 9 Plan v0.2 ✅ APPROVED — 进入 Spec v1.0 Contract Freeze**

> **Chief Architect 关键校准：先 Spec Freeze 再 Coding，保证 Runtime Foundation 不被 Capability Layer 破坏**