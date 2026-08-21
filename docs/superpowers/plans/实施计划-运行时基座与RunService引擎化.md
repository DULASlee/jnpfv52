# 实施计划 — 运行时基座与 RunService 引擎化

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 按方案 C（单轨穿插+双门禁分段裁决）执行 RunService 引擎化重构（S0-S5）与四特性降级版（F0-F4）的混入式施工，重构回滚轴=阶段级 git revert，特性回滚轴=四布尔开关。

**Architecture:** 重构轨（纯移动+IRuntimeDataStore 抽象）与特性轨（文件面零交集）在同一阶段窗口内顺序施工、分段裁决；开关只在 S5 终审按序翻牌。

**Tech Stack:** .NET 8 / SqlSugar / xUnit / Polly v8（Microsoft.Extensions.Http.Resilience）/ Serilog / JNPF.Startup.Benchmarks harness

**设计事实源：** `docs/superpowers/specs/架构设计规格-运行时基座与RunService引擎化.md`（下称规格）
**重构轨详细任务书：** `docs/superpowers/plans/2026-08-21-runservice-engine-refactor.md`（S0-S5 十任务原样执行，本计划不重复其步骤，只定义混入编排）
**上游：** A+C spec · master-plan v3 · gap-analysis v2

---

## 0. 总编排表（S0-S5 双轨挂载）

| 阶段 | 重构轨（先） | 特性轨（后） | 双门禁裁决点 |
|------|-------------|-------------|-------------|
| S0 | Task 1-1：快照基线+契约测试 | **T-F0 开关基建** + 日志基线采集（记录当前 logs/ 目录日均体积，供 F1 磁盘风险对照） | 审批材料双段：重构段（快照+契约绿）/ 特性段（F0 单测绿） |
| S1 | Task 2-3：编译层移动+特征单测 | —（保持纯净） | 单段 |
| S2 | Task 4-5：DataStore 抽象+49+8 收敛 | **T-F1 可查询日志降级版** | 双段；F1 红不阻塞 S2 重构段 |
| S3 | Task 6：RunDataEngine | **T-F2 Outbox Sweeper + DB 锁** | 双段 |
| S4 | Task 7-8：列表/视图层 | **T-F3 出站韧性（LLM/MCP）** | 双段 |
| S5 | Task 9-10：CR+门面缩壳+17→7 | **T-F4 异常边界** + **开关按序翻牌** + 终审冒烟 | 终审：全回归六门禁 + 四开关全 true 冒烟 |

## 1. 红线纪律（ADR-1 固化，每阶段生效）

1. **双门禁分段裁决**：特性门禁红不阻塞重构门禁通过；重构门禁红则整阶段停（特性工作同步顺延）。反之亦然。
2. **文件面隔离**：特性轨禁触 `RunService.cs`/`Runtime/`/`IRunService.cs`；重构轨禁消费特性开关。
3. **开关翻牌唯一窗口**：仅 S5 终审，序列 `QueryableLogging → OutboxSweeper → OutboundResilience → ExceptionBoundary`，每翻一个跑一轮冒烟，红即翻回并登记缺陷，**禁止阶段中途翻牌**。
4. **回滚轴分工**：重构问题→`git revert` 该阶段 commit；特性问题→开关翻 false。两轴不得混用。
5. **证据命名**：重构轨 `s{N}-*.txt`、特性轨 `f{N}-*.txt`，同落 `.claude/evidence/cr-20260820-01/`。
6. 全局铁律（快照零 diff / JNPF009 只随迁 / 节点审批 / Oops 复用 / DI 约束表）继承重构轨任务书第 0 节，全部有效。

---

## 2. S0 特性轨：T-F0 特性开关基建（~1d）

**Files:**
- Modify: `backend/application/JNPF.API.Entry/Configurations/App.json`（新增 RuntimeFoundation 节）
- Create: `backend/framework/JNPF/RuntimeFoundationOptions.cs`（或 JNPF.Common.Core 约定位置，开工时按 Options 类现行归属目录对齐）
- Test: `backend/tests/JNPF.Tests.Common/RuntimeFoundationOptionsTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using Microsoft.Extensions.Configuration;
using Xunit;

public class RuntimeFoundationOptionsTests
{
    [Fact]
    public void Defaults_AllFalse()
    {
        var options = new RuntimeFoundationOptions();
        Assert.False(options.QueryableLogging);
        Assert.False(options.OutboxSweeper);
        Assert.False(options.OutboundResilience);
        Assert.False(options.ExceptionBoundary);
    }

    [Fact]
    public void Binds_FromConfigurationSection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RuntimeFoundation:OutboxSweeper"] = "true",
        }).Build();
        var options = config.GetSection(RuntimeFoundationOptions.Section).Get<RuntimeFoundationOptions>()!;
        Assert.True(options.OutboxSweeper);
        Assert.False(options.QueryableLogging); // 未配置项保持 false
    }
}
```

- [ ] **Step 2: 跑测试确认失败** — `dotnet test backend/tests/JNPF.Tests.Common --filter FullyQualifiedName~RuntimeFoundationOptionsTests`，Expected: FAIL（类型不存在）。
- [ ] **Step 3: 实现 Options 类 + App.json 配置节**（代码见规格 §1.2）；`App.json` 追加 `"RuntimeFoundation": { "QueryableLogging": false, "OutboxSweeper": false, "OutboundResilience": false, "ExceptionBoundary": false }`。
- [ ] **Step 4: 跑测试确认通过** — Expected: PASS 2/2。
- [ ] **Step 5: 日志基线采集** — 记录当前 `logs/` 目录体积与日均增量（`Get-ChildItem logs | Measure-Object Length -Sum`），落盘 `f0-log-baseline.txt`（F1 磁盘风险对照基线）。
- [ ] **Step 6: Commit** — `git commit -m "feat(foundation): S0 特性开关基建 RuntimeFoundation 四布尔位 [CR-20260820-01]"`
- [ ] **Step 7: S0 双段审批** — 重构段（继承重构轨任务书 Task 1 产出）+ 特性段（F0 测试绿+基线采集），等待批准。

---

## 3. S2 特性轨：T-F1 可查询日志降级版（~3d）

**Files:**
- Modify: `backend/application/JNPF.API.Entry/Infrastructure/SerilogBootstrap.cs`（app-*.json sink + 请求日志 + 脱敏，全部开关门控）
- Create: `backend/application/JNPF.API.Entry/Infrastructure/PiiDestructuringPolicy.cs`
- Create: `backend/application/JNPF.API.Entry/Services/LogQueryService.cs`（IDynamicApiController 只读查询）
- Modify: `backend/application/JNPF.API.Entry/Program.cs` 或启动链（UseSerilogRequestLogging 门控注册）
- Test: `backend/tests/JNPF.Tests.Common/PiiDestructuringPolicyTests.cs`

**前置**：S2 重构段门禁绿之后开工（纪律红线 1）。

- [ ] **Step 1: PII 脱敏策略 TDD** — 先写测试（手机号中 4 位 `***`、身份证中 10 位、password/secret/token 命名属性整体 `***`），跑红，实现 `PiiDestructuringPolicy : IDestructuringPolicy`，跑绿。
- [ ] **Step 2: SerilogBootstrap 扩展** — 读取 `RuntimeFoundationOptions`（经 IConfiguration，Serilog 配置期 DI 未就绪，直接读配置）；`QueryableLogging=true` 时追加：

```csharp
// OTel 规范对齐的全级别文件 sink（字段：Timestamp/Level/TraceId/SpanId/SourceContext/Message/Exception）
.WriteTo.File(new Serilog.Formatting.Compact.CompactJsonFormatter(),
    path: Path.Combine(logDir, "app-.json"),
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 14,
    fileSizeLimitBytes: 50 * 1024 * 1024)
```
（TraceId/SpanId 经 `Enrich.FromLogContext()` + Activity 已有注入，无需新增 enricher；CompactJsonFormatter 需确认包引用，缺则用现有 `JsonFormatter(renderMessage: true)` 等价替代并登记差异。）
- [ ] **Step 3: 请求日志门控注册** — 启动链中 `if (options.QueryableLogging) app.UseSerilogRequestLogging();`（位置在 TraceIdMiddleware 之后）。
- [ ] **Step 4: LogQueryService 只读 API** — `IDynamicApiController` + `[SecurityDefine]`（权限点命名对齐 Systems 模块惯例，开工核对）；参数：`startTime/endTime/level/traceId/keyword/page`；实现=流式 `StreamReader` 逐行读 `app-{yyyyMMdd}.json` + `yield` 提前终止（命中上限 200 条）；损坏行跳过；**按当前用户 TenantId 过滤**（日志行含 TenantId 字段；无租户上下文的管理员查询放行，开工核对 UserManager 语义）；路径白名单仅限 `Logging:File:LogDir`。
- [ ] **Step 5: 验证** — `dotnet build backend` + 新增测试全绿 + 快照比对（查询 API 属新增路由，api/visualdev 快照**必须仍零 diff**——新增路由不得出现在该过滤域内，出现即违规）。
- [ ] **Step 6: 开关 false 行为回归** — 确认默认配置下启动行为与改前一致（sink 不生成 app 文件、无请求日志行）。
- [ ] **Step 7: Commit** — `git commit -m "feat(foundation): S2 可查询日志降级版 — OTel 文件信号+请求日志+PII 脱敏+内置查询 API（开关门控）[CR-20260820-01]"`
- [ ] **Step 8: S2 双段审批** — 特性段证据：三跳贯通演示（请求头 TraceId→app-*.json→查询 API 命中）记录 `f1-traceid-chain.txt`。**此时不翻牌**（S5 统一翻）。

---

## 4. S3 特性轨：T-F2 Outbox Sweeper + DB 互斥锁（~2d）

**Files:**
- Create: `backend/infrastructure/JNPF.Extras.EventBus.Outbox/OutboxSweeperService.cs`
- Create: `backend/infrastructure/JNPF.Extras.EventBus.Outbox/IOutboxLock.cs` + `DbOutboxLock.cs` + 锁表实体
- Modify: Outbox 模块注册处（开关门控 BackgroundService）
- Test: `backend/tests/JNPF.Tests.Stage5/` 增 Sweeper 用例（Stage5 已有 Outbox 测试先例）

- [ ] **Step 1: 锁抽象 TDD** — 测试：`Acquire_ReturnsTrue_WhenFree`、`Acquire_ReturnsFalse_WhenHeldByOther`、`Acquire_StealsExpiredLock`（心跳 60s 过期）；实现 `EVENT_OUTBOX_LOCK` 单行锁表（实例标识+心跳时间戳，SqlSugar CodeFirst 自动建表）。
- [ ] **Step 2: Sweeper TDD** — 测试：`RecoversTimedOutProcessing`（Processing 超 10 分钟→Pending+RetryCount+1）、`EscalatesToDeadLetter_WhenMaxRetryExceeded`、`SkipsWhenLockHeld`、`ConcurrentSweepers_OnlyOneRecovers`（双实例模拟，规格 §4 校准钩子）；实现 `OutboxSweeperService : BackgroundService`（30s 周期、批量上限 100、全参 SQL、try-catch 全包）。
- [ ] **Step 3: 开关门控注册** — `OutboxSweeper=false` 时服务不注册（不是注册后退出——更干净）；metric `outbox_sweeper_recovered_total` 经 ObservabilityModule 现有自定义 Meter 源。
- [ ] **Step 4: 验证** — Stage5 测试全绿 + 构建全绿 + api/visualdev 快照零 diff。
- [ ] **Step 5: Commit** — `git commit -m "feat(foundation): S3 Outbox Sweeper 卡死回收 + DB 互斥锁（开关门控）[CR-20260820-01]"`
- [ ] **Step 6: S3 双段审批** — 特性段证据：并发回收断言结果 `f2-sweeper-concurrency.txt`。

---

## 5. S4 特性轨：T-F3 出站韧性管线（~4d）

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj`（新增 PackageReference）
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/PipelineSchedulingModule.cs`（HttpClient 管线配置）
- Test: `backend/tests/JNPF.Tests.Phase6/` 或新建韧性用例（MockHttpHandler）

- [ ] **Step 1: 引入依赖** — `Microsoft.Extensions.Http.Resilience`（NuGet 华为镜像已配）；`dotnet restore` 成功为前置。
- [ ] **Step 2: 写失败测试** — MockHttpHandler 注入失败序列：① 前 2 次 503 第 3 次 200→断言最终成功且请求计数=3；② 5 连败→断言熔断打开（后续请求不经 handler）；用 Polly v8 测试工具类断言。
- [ ] **Step 3: 管线配置（开关门控）** — LLM 与 MCP 两个命名 HttpClient：

```csharp
services.AddHttpClient("LlmGateway", client => { /* 现有配置保留 */ })
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120); // LLM 慢响应
            options.Retry.MaxRetryAttempts = 2;                              // 配额放大防线
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        });
```
开关 `OutboundResilience=false` 时不追加 `AddStandardResilienceHandler`（行为=现状）。**流式端点禁重试**：若同一 client 混用流式与非流式，开工核验后把流式调用迁独立命名客户端（零重试配置）——此为 Step 3 内必须完成的核验项，不是可选项。
- [ ] **Step 4: 验证** — 测试绿 + 构建绿 + S4 冒烟期真实 LLM 端点调用 200（证据 `f3-resilience-live.txt`）+ api/visualdev 快照零 diff。
- [ ] **Step 5: Commit** — `git commit -m "feat(foundation): S4 出站韧性 Polly v8 标准管线（LLM/MCP，开关门控）[CR-20260820-01]"`
- [ ] **Step 6: S4 双段审批** — 特性段含配额放大观测口径（重试次数 metric 基线）登记。

---

## 6. S5 特性轨：T-F4 异常边界降级版（~3d）+ 开关翻牌终审

**Files:**
- Create: `backend/modularity/common/JNPF.Common.Core/ExceptionBoundary/IExceptionBoundary.cs` + `ExceptionBoundary.cs`
- Modify: 非 HTTP 入口三处接线（HostedService 基类/包装、事件处理器执行器外层、SSE/WebSocket 出口——开工先 grep 实际入口清单落台账）
- Modify: `backend/modularity/common/JNPF.Common.Core/Filter/LogExceptionHandler.cs`（Json 结构化，开关门控双格式）
- Test: `backend/tests/JNPF.Tests.Common/ExceptionBoundaryTests.cs`

- [ ] **Step 1: 入口台账** — grep 全仓 `BackgroundService`/`IHostedService`/`IEventHandlerExecutor`/SSE/WebSocket 管道实际清单，落 `f4-entry-inventory.md`（边界覆盖面=台账全覆盖，缺一即漏）。
- [ ] **Step 2: 边界 TDD** — 测试：`CapturesStructuredJson`（type/code/message/innerChain≤5/context）、`DoesNotSwallowSilently_MetricIncremented`、`BackgroundEntry_SurvivesHandlerException`（进程不崩）；实现 `IExceptionBoundary`（记 Error 日志+metric+结构化入库后按入口语义处置）。
- [ ] **Step 3: LogExceptionHandler 双格式** — `ExceptionBoundary=true` 时 Json 字段写结构化 JSON；false 维持 `Message+"\n"+StackTrace`（字节级兼容）；查询侧按首字符 `{` 兼容读（与 2.12 升格路径对齐）。
- [ ] **Step 4: 引擎抛出面架构断言** — 扩展架构测试：引擎类方法体零裸 `throw new Exception`（IL/源码扫描二选一，开工定；源码正则即可）。
- [ ] **Step 5: 验证** — 测试绿 + 构建绿 + 人为触发后台异常冒烟（进程存活+入库结构化，证据 `f4-boundary-live.txt`）。
- [ ] **Step 6: Commit** — `git commit -m "feat(foundation): S5 异常边界降级版 — 非 HTTP 统一拦截+Json 结构化（开关门控）[CR-20260820-01]"`

### S5 终审：开关按序翻牌 + 全回归

- [ ] **Step 7: 翻牌序列**（唯一翻牌窗口）— App.json 逐个翻 true：QueryableLogging→全量冒烟→绿；OutboxSweeper→冒烟→绿；OutboundResilience→冒烟（含 LLM 实调）→绿；ExceptionBoundary→冒烟→绿。任一红：翻回 false、登记缺陷、该特性转修复任务，**不阻塞其余翻牌与重构终审**。
- [ ] **Step 8: 全回归（双轨合并口径）** — 重构轨六门禁（继承重构轨任务书 Task 10 Step 7：sln Debug/Release、CI JNPF009、VisualDev 测试、架构测试含白名单断言、快照零 diff、test:api）+ 特性轨四开关全 true 活体冒烟（登录/CurrentUser/OnlineDev 三类端点/日志查询 API/Sweeper 指标可见）。证据 `s5-final-*.txt` + `f4-final-flip.txt`。
- [ ] **Step 9: S5 终审审批** — 双段材料：重构段（spec §8 六门禁逐项）+ 特性段（F0-F4 验收逐项+翻牌记录）；通过后 CR-20260820-01 归档、master-plan 战役 1 标记闭环。

---

## 7. Self-Review 记录

1. **规格覆盖**：T-F0~T-F4 五任务一一对应规格 §2；挂靠点声明→继承重构轨 Task 4 白名单断言；双轨纪律三条→§1 红线；Phase 4 校准钩子→各阶段验收步骤内嵌（S3 Step 2 并发断言/S4 Step 2 韧性断言/S2 Step 8 三跳贯通/S5 Step 4 抛出面断言）。✅
2. **占位符扫描**：开工核对项（CompactJsonFormatter 包引用、权限点命名、UserManager 租户语义、流式端点核验、入口台账）均为**实证型开工动作**而非设计缺口——每项给出了核验方法与兜底路径。✅
3. **一致性**：开关名与规格 §1.2 完全一致；翻牌序列与规格一致；证据命名与纪律红线 5 一致；任务书引用（重构轨 Task 编号）与既有英文计划一致。✅
