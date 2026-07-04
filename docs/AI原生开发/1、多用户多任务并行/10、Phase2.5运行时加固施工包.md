# Phase 2.5 运行时加固施工包

> 文档编号：10 | 版本：v1.0 | 周期：**3～5 个工作日**（条件触发，非固定排期）  
> 上级总体计划：[`7、skills构建方案.md`](./7、skills构建方案.md) §2 阶段二 / §2 阶段六  
> 前置依赖：[`9、全链条第二阶段开发计划.md`](./9、全链条第二阶段开发计划.md) — 阶段二功能主干 + **§14 质量红线**（P2-R01～R04）  
> 后续依赖：阶段三（设计类 Skill）— **本施工包通过后方可启动**  
> 文档定位：**阶段二与阶段三之间的条件性运行时加固冲刺**；补齐 §14.1「阶段 2.5 可选增强」列，消除进入阶段三前的 NFR 风险

---

## 文档索引

| 章节 | 内容 |
|---|---|
| §0 | 触发条件与定位 |
| §1 | 与 §14 / 阶段六的能力边界 |
| §2 | 加固范围与组件设计 |
| §3 | 冲刺计划（D1-D5） |
| §4 | Definition of Done（G1-G8） |
| §5 | 工程 Ticket 清单 |
| §6 | 风险与缓解 |
| §7 | 与阶段三衔接检查表 |

---

## 0. 触发条件与定位

### 0.1 何时启动 Phase 2.5

**必须启动（任一满足即开）：**

| # | 触发条件 | 来源 |
|---|---|---|
| T1 | 阶段二 DoD **D13–D16** 中有 **≥2 项红灯** | 文档 9 §7 |
| T2 | 双租户并行 Analyst 出现 sa-service 数据串味 | D14 / §14.3.2 |
| T3 | 切换 pipeline ×10 后 `_channels.Count` 线性增长且 P2-R04 TTL 未生效 | D16 |
| T4 | `inferred` 规则未标记 pending 即写入 stable，下游设计 Skill 消费脏 IR | 阶段三预检 |

**建议启动（可选，但推荐）：**

| # | 条件 | 理由 |
|---|---|---|
| T5 | 阶段二按时完成 D1–D12，但团队希望阶段三前统一运行时契约 | 降低阶段三 4 Skill 并行时的隔离风险 |
| T6 | sa-service 仍混用 `console.log`，无法与 C# `runId` 关联排障 | 运维可追溯性 |

**不启动 Phase 2.5 即可进阶段三：** D1–D16 全部绿灯 + 双租户压测通过 + Tech Lead 签字。

### 0.2 在全链条中的位置

```
阶段一 W1-W2   ✅ IR 基础设施
阶段二 W3-W4   PM + Analyst + Skill-local Harness（§14 MUST）
    │
    ├── [D13-D16 全绿] ──────────────────────► 阶段三 W5-W6
    │
    └── [≥2 项红灯 或 T2-T4] ──► Phase 2.5（本文档，3-5 人日）
                                      │
                                      └── [G1-G8 全绿] ──► 阶段三 W5-W6

阶段六 W11-W12  平台级 Harness（Token Budget / OTel / etcd）— 见文档 7 §阶段六 + 文档 9 §14.3.3
```

### 0.3 核心问题

Phase 2.5 要回答：**Skill-local Harness MVP 是否已足够支撑阶段三 4 个设计 Skill 并行接入，且在多租户、多 pipeline 并发下仍可控、可观测、不泄漏？**

### 0.4 交付物总览

```
后端（JNPF.InteAssistant）
    ├── Runtime/SkillExecutionScope.cs          ← AsyncLocal 执行上下文
    ├── Runtime/TenantPipelineQuotaGuard.cs     ← 每租户并发 pipeline ≤3
    ├── Ir/InferredRuleStabilityPolicy.cs       ← inferred 规则 soft-block stable
    └── Skills/SkillHarness.cs                  ← 接入 Scope + Quota

sa-service（TypeScript）
    ├── lib/structuredLogger.ts                 ← JSON 日志 + runId
    ├── storage/TenantScopedSessionStore.ts     ← tenantId+projectId 键隔离
    └── server.ts                               ← step 完成后 session 清理

前端（jnpf-web-vue3）
    └── composables/usePmSkill / useAnalystSkill ← pipeline 切换时 abort + disconnect（若阶段二未完全覆盖）

验收
    └── G1-G8 + 双租户 × 3 pipeline 压测报告
```

---

## 1. 与 §14 / 阶段六的能力边界

> 完整分层表见 [`9、全链条第二阶段开发计划.md`](./9、全链条第二阶段开发计划.md) **§14.1**；阶段六交叉引用见 [`7、skills构建方案.md`](./7、skills构建方案.md) **§阶段六**。

| 能力 | 阶段二 §14 MUST | **Phase 2.5（本文档）** | 阶段六 |
|---|---|---|---|
| runId + Serilog 步骤日志 | ✅ P2-R01 | sa-service JSON 格式统一 | OpenTelemetry 跨进程 Trace |
| SkillRunGuard 409 | ✅ P2-R02 | 租户 pipeline 配额 Guard | 动态 Worker 扩缩容 |
| per-project 并行 ≤5 | ✅ P2-B08 | — | 租户级 LLM 并发池 |
| CT 透传 | ✅ P2-R02 | sa-service 取消监听 | — |
| AnalysisCompleted 门禁 | ✅ P2-R03 | inferred soft-block | Eval 回归门禁 |
| SSE orphan TTL | ✅ P2-R04 | 压测验证 + 指标暴露 | 7×24 泄漏监控 |
| SkillExecutionScope | — | **✅ P2.5-B01** | Harness 全局 DAG |
| Token Budget 四级降级 | — | — | **✅ 阶段六** |
| etcd 软路由 | — | — | **✅ 阶段六** |

**原则：** Phase 2.5 **不**实现 Token Budget、OpenTelemetry、etcd；**只**加固阶段二遗留的运行时缺口。

---

## 2. 加固范围与组件设计

### 2.1 SkillExecutionScope（P2.5-B01）

**问题：** Skill 后台线程除 `RequestContext` 外，缺少统一的 AsyncLocal 作用域，阶段三多 Skill 并行时日志/配额/取消难以一致透传。

**实现：**

```csharp
// backend/modularity/inteAssistant/JNPF.InteAssistant/Runtime/SkillExecutionScope.cs
public sealed class SkillExecutionScope : IDisposable
{
    private static readonly AsyncLocal<SkillExecutionScope?> Current = new();

    public static SkillExecutionScope? CurrentScope => Current.Value;

    public required string RunId { get; init; }
    public required string TenantId { get; init; }
    public required string ProjectId { get; init; }
    public required long PipelineId { get; init; }
    public required string SkillId { get; init; }
    public CancellationToken CancellationToken { get; init; }

    public static SkillExecutionScope Begin(SkillContext ctx, string runId, CancellationToken ct)
    {
        var scope = new SkillExecutionScope { RunId = runId, ... };
        Current.Value = scope;
        return scope;
    }

    public void Dispose() => Current.Value = null;
}
```

**接入点：**

- `SkillHarness.RunAsync`：`using var scope = SkillExecutionScope.Begin(...)`
- `SkillExecutionLogger`：优先读 `CurrentScope`，fallback `SkillContext`
- `SaOrchestratorAdapter`：HTTP Header 从 Scope 读取

**路径索引：** `Skills/SkillHarness.cs` · `Runtime/SkillExecutionScope.cs`

### 2.2 TenantPipelineQuotaGuard（P2.5-B02）

**问题：** 单 pipeline 有 SkillRunGuard，但同一租户可无限开 pipeline 并行 Analyst，打满 sa-service 与 LLM。

**规则：**

```
每 tenantId 同时 running 的 pipeline（含 PM / Analyst SkillRun）≤ 3
超出 → HTTP 429 + SSE error { code: 'TENANT_PIPELINE_QUOTA_EXCEEDED' }
```

**实现锚点：**

```csharp
// Runtime/TenantPipelineQuotaGuard.cs
public sealed class TenantPipelineQuotaGuard
{
    private const int MaxConcurrentPipelinesPerTenant = 3;
    // ConcurrentDictionary<string, HashSet<long>> _tenantActivePipelines

    public bool TryAcquire(string tenantId, long pipelineId, out string? rejectReason);
    public void Release(string tenantId, long pipelineId);
}
```

**接入：** `SkillsApiService` 的 `POST .../run` 入口，在 `SkillRunGuard` 之前检查。

**配置：** `Configurations/StudioRuntime.json`（可选）`MaxConcurrentPipelinesPerTenant`，默认 3。

### 2.3 sa-service 结构化日志 + 租户隔离（P2.5-S01 + P2.5-S02）

**问题（§14.3.2）：** `InMemorySADatabase` 进程级单例可能导致多租户串数据；`console.log` 无法与 C# `runId` 关联。

#### 2.3.1 结构化日志

```typescript
// sa-service/src/lib/structuredLogger.ts
export function logStep(event: {
  level: 'info' | 'warn' | 'error';
  runId: string;
  tenantId: string;
  projectId: string;
  eventId?: string;
  stepName?: string;
  elapsedMs?: number;
  message: string;
}): void;
```

**要求：** 替换 `server.ts` / `SAOrchestrator.ts` 中所有 `console.log`；输出单行 JSON 到 stdout。

#### 2.3.2 TenantScopedSessionStore

```typescript
// sa-service/src/storage/TenantScopedSessionStore.ts
// Key: `${tenantId}:${projectId}:${eventId}:${stepName}`
export class TenantScopedSessionStore {
  get(key: string): StepSession | undefined;
  set(key: string, session: StepSession, ttlMs?: number): void;
  deleteByProject(tenantId: string, projectId: string): void;  // step 完成 / CT 取消
  purgeExpired(): void;  // 定时任务，默认 30min
}
```

**验收：** 租户 A/B 各跑 2 个 project 并行 SA，`GET /sa/debug/sessions`（Dev only）显示 key 前缀隔离，无交叉读写。

**路径索引：** `sa-service/src/server.ts` · `sa-service/src/orchestrator/SAOrchestrator.ts`

### 2.4 InferredRuleStabilityPolicy（P2.5-B03）

**问题：** 阶段二允许 `inferred` 规则标记 pending 但不阻塞 Analyst；阶段三设计 Skill 消费 stable IR-1 时可能含未确认推断规则。

**策略（soft-block）：**

```
EventSpec 含 businessRules[].source == 'inferred'
  且 无对应 UserConfirmedInvariant 事件
→ StabilityGateService 不得将该 EventSpec 升为 stable
→ 门控 Tab 显示「待确认推断规则 N 条」
→ AnalysisCompletedCompletenessGate 仍拒绝（与 §14.4.2 一致）
```

**用户确认路径（MVP）：** Dev 按钮「确认推断规则」→ 写 `InferredRulesAcknowledged` 事件 → 允许 stable。

**路径索引：** `Ir/StabilityGateService.cs` · `Ir/InferredRuleStabilityPolicy.cs`

### 2.5 前端 pipeline 切换生命周期（P2.5-F01，若阶段二未完全覆盖）

**检查清单：**

| 动作 | 组件 |
|---|---|
| abort 进行中的 fetch SSE | `AiChatPanel.vue` |
| disconnect EventSource | `usePmSkill` / `useAnalystSkill` |
| 清空 observatory 缓存 | `useIrObservatory.ts` |
| POST cancel（若后端暴露） | `api/studio/skills.ts` |

**后端可选：** `POST /api/studio/skills/{pipelineId}/cancel` → `BackgroundTaskRunner.CancelTask` + `SkillRunGuard.Release`

---

## 3. 冲刺计划（D1-D5）

> 3 人日版（最小）：B01 + S01 + Q01  
> 5 人日版（完整）：全部 Ticket

| 天 | Ticket | 任务 | 当日验收 |
|---|---|---|---|
| D1 | P2.5-B01 | `SkillExecutionScope` + Harness 接入 | 后台线程日志自动带 runId/tenantId |
| D1 | P2.5-S01 | sa-service structuredLogger | stdout 单行 JSON，含 runId |
| D2 | P2.5-S02 | TenantScopedSessionStore + step 清理 | 双租户 session key 隔离单测 |
| D2 | P2.5-B02 | TenantPipelineQuotaGuard | 第 4 个并行 pipeline → 429 |
| D3 | P2.5-B03 | InferredRuleStabilityPolicy | inferred 未 ack → 不得 stable |
| D3 | P2.5-F01 | 前端 pipeline 切换 abort（补漏） | D16 脚本 10 次通过 |
| D4 | P2.5-Q01 | 双租户 × 3 pipeline 压测 | 报告：无串味、Channel 不泄漏 |
| D5 | P2.5-Q02 | G1-G8 签字 + 阶段三检查表 | Tech Lead 批准进入阶段三 |

---

## 4. Definition of Done（G1-G8）

| # | 条款 | 操作 | 预期 |
|---|---|---|---|
| G1 | SkillExecutionScope | 触发 PM Skill，查 Serilog | 后台线程日志含 runId，无需手动传参 |
| G2 | 租户 pipeline 配额 | 同租户开 4 条 pipeline 并行 Analyst | 第 4 条 429；前 3 条正常 |
| G3 | sa-service 日志 | 跑完 1 个 event 九步 | stdout JSON 可 `grep runId` 关联 C# 日志 |
| G4 | sa-service 隔离 | 租户 A/B 各 2 project 并行 SA | session store 无交叉 key |
| G5 | inferred soft-block | EventSpec 含 inferred 规则未 ack | 门控 Tab 非 stable；AnalysisCompleted 拒绝 |
| G6 | 泄漏复检 | D16 脚本 ×10 | `_channels` 不线性增长；前端无 duplicate SSE |
| G7 | CT 取消 | Analyst 50% 时离开页面 | sa-service 日志出现 cancelled；session 清理 |
| G8 | 阶段二回归 | D1–D16 全量重跑 | 无功能回退 |

**Phase 2.5 通过：G1–G8 全部绿灯。**

---

## 5. 工程 Ticket 清单

| Ticket | 标题 | 估时 | 依赖 |
|---|---|---|---|
| **运行时上下文** | | | |
| P2.5-B01 | `SkillExecutionScope` + Harness/Logger 接入 | 0.5d | 阶段二 P2-B01 |
| P2.5-B02 | `TenantPipelineQuotaGuard` + 429 响应 | 0.5d | P2.5-B01 |
| P2.5-B03 | `InferredRuleStabilityPolicy` + Dev ack API | 0.5d | 阶段二 P2-R03 |
| **sa-service** | | | |
| P2.5-S01 | `structuredLogger.ts` + 替换 console.log | 0.5d | 阶段二 P2-S01 |
| P2.5-S02 | `TenantScopedSessionStore` + purge + 单测 | 1d | P2.5-S01 |
| **前端 + 集成** | | | |
| P2.5-F01 | pipeline 切换 abort/disconnect 补漏 | 0.5d | 阶段二 P2-F03 |
| P2.5-B04 | `POST .../skills/{id}/cancel`（可选） | 0.5d | P2.5-B01 |
| **质量** | | | |
| P2.5-Q01 | 双租户 × 3 pipeline 压测 + 报告 | 1d | S02, B02 |
| P2.5-Q02 | G1-G8 验收 + 阶段三放行签字 | 0.5d | 全部 |

| 版本 | 合计人日 | 适用场景 |
|---|---|---|
| **最小版** | **3d** | P2.5-B01 + P2.5-S01 + P2.5-S02 + P2.5-Q01 | T2 sa-service 串味 |
| **标准版** | **4d** | 最小版 + B02 + B03 | T1 多项 D13-D16 红灯 |
| **完整版** | **5d** | 标准版 + F01 + B04 + Q02 | T5 主动加固 |

---

## 6. 风险与缓解

| 风险 | 缓解 |
|---|---|
| Phase 2.5 与阶段二 P2-R01～R04 重复建设 | 本文档 §1 边界表；Ticket 描述明确「仅补 §14.1 阶段 2.5 列」 |
| 配额 Guard 误杀合法并发 | 429 响应含 `activePipelineIds`；Admin 可调 `MaxConcurrentPipelinesPerTenant` |
| inferred soft-block 阻塞演示 | Dev ack 按钮；生产 HITL 留阶段三 UI |
| sa-service session purge 误删进行中 step | purge 仅清理 `completedAt` 或 TTL 过期项 |
| 压测环境与生产差异 | Q01 报告注明硬件/LLM mock 条件 |

---

## 7. 与阶段三衔接检查表

Phase 2.5 完成后、阶段三启动前：

| 检查项 | 来源 | 状态 |
|---|---|---|
| D1–D16 全绿 | 文档 9 §7 | ☐ |
| G1–G8 全绿 | 本文档 §4 | ☐ |
| `SkillExecutionScope` 可被设计 Skill 复用 | P2.5-B01 | ☐ |
| stable IR-1 无未 ack inferred 规则 | P2.5-B03 | ☐ |
| sa-service 双租户隔离报告归档 | P2.5-Q01 | ☐ |
| 文档 9 §14.6 Skill-local Harness 契约未变更 | 阶段三仅增 Skill 注册 | ☐ |

**阶段三第一天动作：** 订阅 `AnalysisCompleted` → 注册 `architect-skill` / `db-design-skill` / `ui-design-skill`，**复用** `SkillHarness` + `SkillExecutionScope`，不新建执行链路。详见 [`11、全链条第三阶段开发计划.md`](./11、全链条第三阶段开发计划.md) §12。

---

## 8. 任务依赖图

```
阶段二 P2-R01～R04
        ↓
P2.5-B01 SkillExecutionScope
    ├── P2.5-B02 TenantPipelineQuotaGuard
    ├── P2.5-S01 structuredLogger
    │       └── P2.5-S02 TenantScopedSessionStore
    ├── P2.5-B03 InferredRuleStabilityPolicy
    └── P2.5-F01 前端切换补漏
            ↓
    P2.5-Q01 压测
            ↓
    P2.5-Q02 G1-G8 放行 → 阶段三
```

---

*文档版本 v1.0 | 2026-07-04*  
*系列文档：1～7 设计与总体计划；8 阶段一；9 阶段二；10 Phase 2.5；**11 阶段三**（见 [`11、全链条第三阶段开发计划.md`](./11、全链条第三阶段开发计划.md)）*
