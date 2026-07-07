# 21、CognitiveSkill 统一模具施工包（R0–R5）

> ⚠️ **R12 三元组适配声明（2026-07-07 追加）：** 本施工包涉及的所有 Skill 上下文 MUST 携带三元组 `(tenantId, projectId, pipelineId)`；模具迭代不应破坏三元组透传。详见 `.cursor/rules/triple-key-iron-law.mdc`（宪法级，永远生效）。

> 状态：R0 施工中 · 创建：2026-07-05
> 上游输入:`20、skills的初稿.md`（八层灵魂设计）+ `19、全链条补充开发详细任务计划.md`（SUP 系列）
> 本施工包是 20 号初稿经现有代码兼容性修正后的**唯一施工蓝本**。初稿与本包冲突处，以本包为准。

---

## 1. 目标与红线

### 1.1 目标

把每个 Skill 铸成同一副模具:**骨架（分类学）→ 经络（MCP 工具总线）→ 血液(IR 事件流）→ 大脑（LLM 网关 + ToT）→ 心脏（经验回流）**，全部运行在现有 Agent Runtime（`SkillHarness` + `SkillRegistry` + `StageConfirmSkillTrigger`）之内。

### 1.2 红线（违反即返工）

| # | 红线 | 依据 |
|---|---|---|
| RL-1 | **禁止 fallback 假输出**——LLM/SA 失败必须抛 `Oops.Bah/Oops.Oh` 或返回失败态，不得编造骨架/规格 | 用户明令 |
| RL-2 | **所有 LLM 调用必须走 `ILlmGatewayService`**（审计入 `BASE_AI_CALL_LOG`），Skill 内禁止直连 HttpClient | 现有网关审计链 |
| RL-3 | **不破坏 `IBaseSkill` 契约**——`SkillRegistry`（`Skills/SkillRegistry.cs` 经 `GetServices<IBaseSkill>()` 聚合）与 `SkillHarness` 不感知迁移 | 绞杀者模式 |
| RL-4 | **IR 事件是唯一血液**——Skill 产物只能以 `AppendIrEventRequest` 流出，落库走 `IIrEventStoreService.AppendAsync`（内含 Schema/IOI 校验 + 投影 + SSE） | 事件溯源架构 |
| RL-5 | 多租户:一切显式 SQL 带 `TenantId`;运行时上下文取 `SkillExecutionScope.CurrentScope` | R4 红线 |

---

## 2. 与 20 号初稿的差异修正（评审结论固化）

| 初稿设计 | 问题 | 本包修正 |
|---|---|---|
| `CognitiveSkill<TInput, TOutput>` 泛型基类直接做运行时契约 | `SkillRegistry`/`SkillHarness` 按非泛型 `IBaseSkill` 分发，泛型无法统一注册 | **非泛型 `CognitiveSkill` 基类实现 `IBaseSkill`**;类型安全放在 `ThinkAsync` 内部感知模型（`SkillPerception`） |
| Skill 自己管理并发/配额/日志 | 与 `SkillHarness`（并发闸）、`SkillLlmBudgetGuard`（预算）、`SkillExecutionLogger` 职责重叠 | 运行时职责**留在 Harness**，模具只管认知生命周期 |
| MCP 一步到位 HTTP 微服务 | 现阶段工具全在进程内（`IDomainSeedService`、`IIoiValidatorService`） | **先 InProc 传输**（`IMcpToolHandler` DI 聚合），协议接口 `IMcpClient` 保持传输无关，后续可换 HTTP |
| 进化层独立存储 | 另建存储违反"IR 事件是唯一血液" | 经验事件**复用 IR 事件流**（新增 3 个事件类型，投影引擎 default→null 天然兼容） |

---

## 3. 模具解剖（落地版）

```
┌────────────────────────── CognitiveSkill（非泛型抽象基类）──────────────────────────┐
│ 骨架   SkillId / Version / SkillLayer / SkillMission / InformationNeeds / Outputs │
│ 生命周期 ReasonAsync = PerceiveAsync(虚) → ThinkAsync(抽象,流式) → 自动盖 SkillId 戳  │
│ 焊接   实现 IBaseSkill → SkillRegistry 自动收编 → SkillHarness 原样调度              │
│ 兵器   protected Llm(ILlmGatewayService) · Mcp(IMcpClient)                        │
│ 血液   protected Events(IEventStream) · Experience(IExperienceRecorder)           │
│ 质检   ValidateOutputAsync 默认强制:产出事件类型 ⊆ Outputs 声明                      │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### 3.1 骨架——技能分类学

文件:`backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/Cognitive/SkillTaxonomy.cs`

- `enum SkillLayer { Decision, Refinement, Execution }`
- `enum SkillMission { DefineBoundary, RefineSpecification, GenerateArtifact, DiagnoseAndRepair }`

### 3.2 经络——MCP 工具总线（InProc 先行）

文件:`Skills/Cognitive/Mcp/`(同项目)

| 类型 | 职责 |
|---|---|
| `IMcpClient.CallToolAsync(toolName, argumentsJson, ct)` → `McpToolResult` | 传输无关调用面 |
| `IMcpClient.ListTools()` → `McpToolDescriptor[]` | 工具清单（manifest） |
| `IMcpToolHandler`（`Descriptor` + `ExecuteAsync`） | 进程内工具实现契约,DI 聚合 |
| `InProcMcpClient : IMcpClient, ITransient` | 按 toolName 路由;未知工具返回失败态（不抛） |

R0 内置工具（真实包装，非 stub）:

| 工具名 | 包装目标 | 文件 |
|---|---|---|
| `kg.search-seeds` | `IDomainSeedService.MatchAsync`（`Skills/ContextBuilderService.cs:48`） | `Mcp/Tools/KnowledgeGraphTools.cs` |
| `kg.score-candidate` | `IDomainSeedService.ScoreCandidate` | 同上 |
| `ioi.validate` | `IIoiValidatorService.Validate`（`Ir/IoiValidatorService.cs:7`） | `Mcp/Tools/IoiValidateTool.cs` |

### 3.3 血液——IEventStream 门面

文件:`Skills/Cognitive/IEventStream.cs`

- `AppendAsync(AppendIrEventRequest, ct)`:project/tenant 从 `SkillExecutionScope.CurrentScope`（`Runtime/SkillExecutionScope.cs`）取;无作用域抛 `Oops.Oh`
- `AppendAsync(projectId, tenantId, request, ct)`:显式重载（API 层/人工纠偏场景）
- 实现 `IrEventStreamFacade` 直接委托 `IIrEventStoreService.AppendAsync`——Schema 校验、IOI 校验、投影、SSE 推送全部复用既有管线（`Ir/IrEventStoreService.cs:97-108`），门面不复制任何逻辑

### 3.4 心脏——经验回流（进化层地基）

新增 IR 事件类型(`JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`):

| 常量 | 值 | 语义 |
|---|---|---|
| `SkillReviewRecorded` | `SkillReviewRecorded` | 评审结论（人/Guard 对 Skill 产物的裁决） |
| `SkillFailureRecorded` | `SkillFailureRecorded` | 失败经验（异常种类+消息+运行上下文） |
| `HumanCorrectionRecorded` | `HumanCorrectionRecorded` | 人工纠偏 before/after diff |

投影安全性:`IrProjectionEngine.ProjectEventAsync` switch `_ => null`（`Ir/IrProjectionEngine.cs:79`），新事件只入事件表不动快照——**零投影改造**。

接口 `IExperienceRecorder`（`Skills/Cognitive/IExperienceRecorder.cs`）:

- `RecordReviewAsync(projectId, tenantId, skillId, runId, verdict, detailJson)`
- `RecordFailureAsync(projectId, tenantId, skillId, runId, errorKind, message)`
- `RecordHumanCorrectionAsync(projectId, tenantId, skillId, fragmentId, beforeJson, afterJson, reason)`

### 3.5 大脑——TreeSearch(真 ToT 地基)

`ILlmGatewayService` 新增:

```csharp
Task<TreeSearchResult> TreeSearchAsync(TreeSearchRequest request, CancellationToken ct = default);
```

- DTO:`JNPF.InteAssistant.Entitys/Dto/InteAssistant/TreeSearchModels.cs`
- 语义:同一 prompt 按温度梯度（`TreeSearchPlanner.BuildTemperatureSchedule`，纯函数可单测）并行发 N 路 `ChatAsync`——每路独立审计入 `BASE_AI_CALL_LOG`
- **只生成候选不做裁决**:打分/剪枝由 Skill 用 `kg.score-candidate` 等工具完成（生成与评估分离）
- **无 fallback**:全部候选失败 → `TreeSearchResult.IsSuccess=false` + 逐路错误,绝不编造内容

---

## 4. 分期计划(R0–R5)

| 期 | 内容 | 交付物 | 验收 |
|---|---|---|---|
| **R0 契约铸造(本期)** | 分类学枚举、MCP InProc 总线+3 真工具、IEventStream、IExperienceRecorder+3 事件类型、TreeSearchAsync、`CognitiveSkill` 基类 | 上述全部代码,零现有 Skill 改动 | `dotnet build` 0 error;PhaseB `cognitive-r0` 套件全绿(温度梯度≥2 候选、模具盖戳、产出白名单质检、MCP 路由) |
| **R1 PM 迁移 ✅** | `PmSkillService` 改继承 `CognitiveSkill`,ToT 换 `TreeSearchAsync`+`kg.score-candidate` 真评分,**删除 `BuildFallbackSkeleton`** | PM Skill 新模具版 | phase2 E2E 绿;LLM 断链时 run 状态=failed 而非假骨架 |
| **R2 Analyst 迁移 ✅** | `AnalystSkillService` 入模;删 `RunAutoEventAsync`/`BuildFallbackOutput`;IOI 走 `ioi.validate`;删 bulk `/api/sa/run` | Analyst 新模具版 | SA 断链 → run failed;无 seed-auto 假规格 |
| **R3 设计四 Skill 迁移 ✅** | architect/db/ui/system-design 入模;删三处 LLM fallback;Architect `BudgetGuardTreeSearch` ToT | 四 Skill 新模具版 | `design-r3` 全绿;DesignSkillOrchestrator 不变 |
| **R4 进化闭环 ✅** | Harness 失败→`SkillFailureRecorded`;评审 API+confirm-skeleton→`SkillReviewRecorded`;EventSpecRevision→`HumanCorrectionRecorded` | 经验事件全链路 | `experience-r4` 全绿 |
| **R5 MCP 升级 ✅** | `McpTools.json` Manifest;`RoutingMcpClient`+`HttpMcpTransport`;`McpGatewayService` HTTP 网关;`sa.run-step` 跨进程路由 | HTTP MCP 客户端 | `mcp-r5` 契约测试全绿;`phase2-skills-e2e` 绿 |

### 4.1 SUP 系列并轨(回写 19 号计划)

| 19 号 SUP | 归入 |
|---|---|
| SUP-02(PM ToT 真实现) | R1 |
| SUP-03(Analyst 去 fallback) | R2 |
| SUP-05(设计 Skill 收口) | R3 |
| SUP-07(经验回流) | R4 |
| 其余 SUP(附件/交付物 S0 系列) | 已完成(见 20260705 会话,`PipelineDeliverableService`) |

---

## 5. R0 文件清单

| # | 文件(相对 `backend/modularity/inteAssistant/`) | 动作 |
|---|---|---|
| 1 | `JNPF.InteAssistant/Skills/Cognitive/SkillTaxonomy.cs` | 新增 |
| 2 | `JNPF.InteAssistant/Skills/Cognitive/Mcp/McpContracts.cs` | 新增(IMcpClient/IMcpToolHandler/DTO) |
| 3 | `JNPF.InteAssistant/Skills/Cognitive/Mcp/InProcMcpClient.cs` | 新增 |
| 4 | `JNPF.InteAssistant/Skills/Cognitive/Mcp/Tools/KnowledgeGraphTools.cs` | 新增 |
| 5 | `JNPF.InteAssistant/Skills/Cognitive/Mcp/Tools/IoiValidateTool.cs` | 新增 |
| 6 | `JNPF.InteAssistant/Skills/Cognitive/IEventStream.cs` | 新增(接口+门面实现) |
| 7 | `JNPF.InteAssistant/Skills/Cognitive/IExperienceRecorder.cs` | 新增(接口+实现) |
| 8 | `JNPF.InteAssistant/Skills/Cognitive/CognitiveSkill.cs` | 新增(模具本体+`CognitiveSkillToolkit`) |
| 9 | `JNPF.InteAssistant/Skills/Cognitive/TreeSearchPlanner.cs` | 新增(纯函数温度梯度) |
| 10 | `JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs` | 追加 3 常量 |
| 11 | `JNPF.InteAssistant.Entitys/Dto/InteAssistant/TreeSearchModels.cs` | 新增 |
| 12 | `JNPF.InteAssistant/Interfaces/ILlmGatewayService.cs` | 追加 `TreeSearchAsync` |
| 13 | `JNPF.InteAssistant/LlmGatewayService.cs` | 实现 `TreeSearchAsync` |
| 14 | `backend/tests/JNPF.Tests.PhaseB/CognitiveSkillR0Tests.cs` | 新增 |
| 15 | `backend/tests/JNPF.Tests.PhaseB/TestRunner.cs` + `Program.cs` | 挂载 `cognitive-r0` |

**不改**:`SkillHarness.cs`、`SkillRegistry.cs`、任何现有 Skill、`IrProjectionEngine.cs`、前端。

---

## 6. R0 验收命令

```powershell
cd D:\JNPF-v52\backend
dotnet build modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj   # 0 error
dotnet run --project tests/JNPF.Tests.PhaseB -- cognitive-r0                          # 全绿
```
