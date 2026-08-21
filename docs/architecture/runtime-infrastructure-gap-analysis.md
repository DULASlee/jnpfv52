# 运行时基座缺口分析（runtime-infrastructure-gap-analysis）

- **日期**：2026-08-20（v2：2026-08-21 遗漏点分析报告核准并入）
- **来源**：第二位工程师全量缺陷清单（B 清单）+ 第三份专家评估（2025-2026 业界对标），均经 D爷裁决吸收 + 第一位工程师逐条实证核验
- **裁决**：全部登记为 backlog（独立 CR 选题库），**不插队 RunService 重构**；路由快照零差异为死线
- **集成位置**：`runservice-refactor-master-plan.md` §3 战役 2「运行时基座 backlog」

## 0. 裁决前的关键事实修正（Evidence Over Assumption）

### 0.1 Polly 事实修正（双向修正）

B 清单称「Polly 仅接在事件处理器执行器一处」；第一位工程师此前核验结论为「全仓 Polly 0 匹配」。**双方都需要修正**，2026-08-20 重新全仓 grep 实锤：

| 事实 | 实证 |
|------|------|
| NuGet `Polly` 包**未引用**（所有 csproj 0 匹配） | `PackageReference.*Polly` 全仓 0 |
| 存在**自研 Polly 风格**执行器 `PollyRetryHandlerExecutor`（指数退避 1s→16s + ±20% jitter + 10 次失败熔断 30s），注册于 `EventBusModule` 替换默认 `RetryEventHandlerExecutor` | `JNPF.Common.Core/EventBus/PollyRetryHandlerExecutor.cs` + Stage5 测试 T3/T3b/T3c 覆盖 |
| **结论**：B 的说法基本属实（EventBus 一处、自研非 NuGet）；第一位工程师此前 grep 漏掉自研类。后续技术选型（2.3 HTTP 韧性）应评估引入 NuGet Polly v8 `ResiliencePipeline`，而非继续自研 | — |

### 0.2 出站 HTTP 调用面实锤（P0-3 佐证）

| 调用点 | 现状 |
|--------|------|
| `LlmGatewayService` / `ModelProviderService`（LLM 出站） | IHttpClientFactory，无重试/熔断/超时策略统一 |
| `HttpMcpTransport`（MCP HTTP） | 仅 5min 超时，无重试 |
| `PipelineAttachmentService` / `AIDevelopmentPipelineService`（附件下载） | 已补超时+Authorization，无韧性管线 |
| `SaOrchestratorAdapter`（sa-service 自调用） | 无韧性管线 |
| `IntegreateEventSubscriber.InteAssistantHttpClient` | 手写 3 次重试（旧模式） |

全仓 `ResiliencePipeline`/`Http.Resilience` 0 匹配——**出站韧性管线确实为零**，但注意：RunService 本体零 HTTP 调用，不阻塞战役 1。

## 1. B 清单逐条核验与处置

| # | 项 | 实证判定 | 处置 |
|---|----|---------|------|
| P0-1 | DI Captive Dependency | ✅ 属实且已**修复完毕**（战役 0：8 独立对→0，ValidateOnBuild 全绿，evidence `cr-20260820-01/di-validation-*.txt`） | ✅ 已闭环 |
| P0-3 | 出站调用零韧性 | ✅ 属实（§0.2 实锤），B 的 Polly 表述修正为自研执行器 | backlog 2.3 → **v3：部分挂载战役 1 S4（T-F3 仅 LLM/MCP）** |
| P1-1 | 日志可查询只兑现一半 | ⚠️→✅ **v2 实锤升级**：`Logging.json` Seq.Enabled=false；`SerilogBootstrap` 文件 sink 仅 error/warning 两路，**info 确实不落盘**（仅 Console）；`UseSerilogRequestLogging` 全仓 0 匹配（无请求级访问日志）；Serilog→OTel Logs 未桥接 | backlog 2.5 → **v3：降级版挂载战役 1 S2（T-F1）** |
| P1-3 | 缓存无防击穿/雪崩 | ✅ 属实：`CacheManager` 全类无 `GetOrAdd`/`SemaphoreSlim`/jitter（grep 0 匹配），纯转发命名缓存 | backlog 2.2 |
| P1-4 | 限流不可分区 | ✅ 属实：`RateLimitingModule` 全局 fixed 200/s（PermitLimit=200, Window=1s, Queue=20）+ login 20/min + export 10/min，无 tenant/user 分区 | backlog 2.4 |
| P1-5 | 就绪探针不完整 | ✅ **v2 实锤**：`HealthCheckModule` 仅 `AddSqlServer`；磁盘检查有现成 `LogHealthCheckService` API 端点但**未注册进 /health/ready 管道** | backlog 2.7 |
| P0-2后半 | 异常拦截仅 HTTP 层 + 入库不可分析 | ✅ 属实：`LogExceptionHandler.OnExceptionAsync(ExceptionContext)` 仅 MVC 管道；`Json = Message + "\n" + StackTrace` 单字段平铺；全仓 `IExceptionHandler` 0 实现 | backlog 2.14（遗漏点 M1）→ **v3：降级版挂载战役 1 S5（T-F4）** |
| P2-4 | 无 RFC 9457 错误契约 | ✅ 属实：全仓仅 `ValidationProblemDetails` 1 处；429 响应体手写 JSON 与 RESTfulResult 不一致 | backlog 2.15（遗漏点 M2） |
| P2-6 | 无告警规则落盘 | ✅ 属实：仓内无 Grafana 面板/告警规则资产，OTel 已导出 | backlog 2.16（遗漏点 M3） |
| P2-1 | 无幂等键（写 API） | ✅ 真实缺口；与「重构不改行为」门禁冲突，剥离 | backlog 2.11 |
| P2-2 | 无版本化迁移（DB schema） | ✅ 真实缺口（SqlSugar CodeFirst 现状） | backlog 2.12 |
| P2-3 | 无热路径基准 | ✅ 真实缺口（`performance-baseline.md` 仅启动/DI 基准，无运行时热路径） | backlog 2.13 |
| Outbox | 无卡死回收器 | ✅ 属实（无 Sweeper） | backlog 2.1 → **v3：已挂载战役 1 S3（T-F2）** |

## 2. 纪律约束（D爷裁决）

1. **本清单全部项不得混入战役 1**——RunService 重构每阶段路由快照零差异是死线。
2. 每项 backlog 独立 CR、独立验收；取活顺序按 §3 优先级列。
3. 引擎层（战役 1 产物）仅允许补**结构化异常上下文**（FormId/FlowId/TenantId 日志字段），复用现有 `Oops.Oh/Oops.Bah` + `FriendlyExceptionFilter`，**禁止重建异常体系**（2.14 亦遵守：只补记录结构与边界，不动契约）。
4. 后续独立 CR 引用本文档编号（2.x），完成后在 master-plan backlog 表标记闭环。

## 3. 驳回登记（第三份专家评估，2026-08-21）

| 专家提案 | 驳回理由 |
|---------|---------|
| 战役 0 插队 0.1-0.4 四节点 | 与 D爷裁决的瘦身战役 0 冲突；0.2/0.3/0.4 全入 backlog |
| RunInfrastructure 引擎顺带承载分布式锁+幂等键 | 违反「零新功能混入重构」死线；幂等键已在 2.11 |
| P0-1「必须最先否则引擎复制存量违规」 | 方向对但已由战役 0 提前闭环；DI 约束表五律防引擎复制暗病 |

## 4. 待决事项

- **D2（已决）**：「日志可查询」落地形态——Phase 1 拍板：无部署能力，降级为「OTel 规范文件 JSON + 内置查询 API」（T-F1）；Seq/LGTM 部署栈待运维侧就绪后挂载（2.5 余留部分）。
- **v3 集成指针**：四特性降级版混入战役 1 的编排/纪律/五件套见《架构设计规格-运行时基座与RunService引擎化.md》与《实施计划-运行时基座与RunService引擎化.md》。
