# RunService 引擎化重构总纲（专家施工包对撞修正版）

- **日期**：2026-08-20（v3：2026-08-21 ③混入式拍板：四特性降级版并入战役 1）
- **状态**：四阶段架构交互协议（Phase 1-2）已拍板：方案 C 单轨穿插+双门禁分段裁决 + 回滚轴分工（重构=git revert / 特性=四布尔开关）；战役 0 已闭环；设计事实源已切换至《架构设计规格-运行时基座与RunService引擎化.md》
- **设计文档**：`docs/superpowers/specs/架构设计规格-运行时基座与RunService引擎化.md`（主，含五件套）· `docs/superpowers/specs/2026-08-20-runservice-engine-refactor-design.md`（A+C 引擎化子规格）
- **实施计划**：`docs/superpowers/plans/实施计划-运行时基座与RunService引擎化.md`（混入编排）· `2026-08-21-runservice-engine-refactor.md`（重构轨任务书）
- **上游基线**：`docs/architecture/backend-modular-refactor-plan.md` · 施工包 v2
- **缺口清单**：`docs/architecture/runtime-infrastructure-gap-analysis.md`（B 清单实证核验版，backlog 选题库）

## 1. 专家包实证核验表（Evidence Over Assumption，2026-08-20 采集）

| 专家声明 | 仓库实证 | 判定 |
|---------|---------|------|
| OTel traces+metrics 已通、TraceId 已有 | `ObservabilityModule.cs`：Tracing（AspNetCore/SqlClient/HttpClient）+ Metrics + OTLP | ✅ 属实 |
| Outbox 已建（重试+死信），比多数平台先进 | `EventOutboxMessage` RetryCount/MaxRetry=3/DeadLetter + `RetryDeadLetterAsync` | ✅ 属实 |
| Outbox 无卡死回收器 | Grep 无 Sweeper | ✅ 属实 |
| HTTP 韧性管线为零 | 全仓 `ResiliencePipeline`/`Http.Resilience` 0 匹配 | ✅ 属实 |
| 「Polly 仅在 EventBus 一处使用」 | **v2 双向修正**：NuGet Polly 包 0 引用属实，但存在自研 `PollyRetryHandlerExecutor`（指数退避+jitter+熔断，`EventBusModule` 注册，Stage5 测试覆盖）——B 说法基本属实，此前「全仓 0 匹配」漏掉自研类 | ⚠️ 结论修正：EventBus 一处自研韧性，出站 HTTP 韧性仍为零 |
| 限流全局不可分区 | `RateLimitingModule`：全局 200/s 固定窗口 + login/export，无 tenant 分区 | ✅ 属实 |
| DI Scope 校验未开 | `ValidateScopes`/`ValidateOnBuild` 全仓 0 匹配（连配置都没有） | ⚠️ 方向属实，违规程度待实测 |
| 后台作业异常裸奔 | Hangfire 已有 `HangfireExceptionFilter`（InteAssistant 注册） | ⚠️ 部分不准 |
| 异常「HTTP 层有、非 HTTP 层无」 | `FriendlyExceptionFilter` + `RESTfulResultProvider` + `LogExceptionHandler` | ✅ 属实 |
| RunService 需异步化 | RunService 内 `.Result`=0、`.Wait()`=0，已全异步 | ❌ 对目标不适用 |
| 节点 1.5 RunTreeQueryService | RunService 42 方法无树形查询（树组装在已拆完的 UsersService） | ❌ 幻影节点 |

## 2. 专家包与项目铁律的冲突修正

| 专家包项 | 冲突对象 | 修正决定 |
|---------|---------|---------|
| 0.3 五类异常体系重建 | JNPF 既有 `Oops.Oh/Oops.Bah` + `FriendlyExceptionFilter` + `RESTfulResult`（含 HTTP 600=JWT 约定） | **拒绝重建**，复用现有体系；引擎层仅补结构化异常上下文（FormId/FlowId/TenantId 日志字段） |
| 1.1「CC140→CC≤15」+ 全引擎 CC≤15 门控 | JNPF009 基线铁律（只随迁不上调不新增）+ 绞杀者纯移动纪律 | 降 CC 需改写逻辑 = 行为变更，移至**重构后二期**；本次 CC140 保持 CC140 |
| 1.3.4 幂等键 / 1.4.2 Keyset 分页 / 2.5 日志查询页 | 「重构不改行为」验收硬门控（路由契约逐条不变） | **剥离为 backlog**，不混入重构 |
| 战役 0 全量前置（0.1-0.4） | RunService 零 HTTP 调用、OTel 已六成到位，0.2/0.3/0.4 与重构零阻塞依赖 | **瘦身战役 0**：仅 0.1 DI 清点+约束表 |

## 3. 战役划分（修正后）

### 战役 0：瘦身底座诊断（前置，约 1 节点，已批准开工）

| 步骤 | 内容 | 交付物 |
|------|------|--------|
| 0.1.1 | Development 开 `ValidateScopes + ValidateOnBuild`（配置开关），收集全量违规 | 违规清单（evidence） |
| 0.1.2 | 分级 A/B/C，仅修 visualdev 相关 A 类 | 分级矩阵 + 修复 |
| 0.1.3 | CI 增加启动 Scope 校验门控（Development 全绿） | CI 配置 |
| 0.1.5 | 《A+C 引擎类 DI 注册约束表》 | 设计附件 → 反哺 spec §4 生命周期 |

### 战役 1：RunService A+C 引擎化 + 四特性混入（主线，S0-S5）

拆分为：`RunSqlCompiler`（编译纯函数）/ `IRuntimeDataStore`+SqlSugar 实现（provider 中立）/
`RunDataEngine`（CRUD+流程表单）/ `RunListQueryService` / `RunDataViewService` / `RunService` 缩壳门面。
核心约束：不绑死 SqlSugar（PG/时序兼容）、IRunService 17→7、5 注入点全切换、状态单点收敛。

**v3 混入编排（Phase 1-2 拍板）**：四特性降级版挂载 S0=开关基建 / S2=可查询日志 / S3=Outbox Sweeper / S4=出站韧性 / S5=异常边界+翻牌终审；
纪律红线：**特性门禁红不阻塞重构门禁**（ADR-1）；结构挂靠点声明见架构设计规格 §1.1（引擎构造白名单硬门控）。

### 战役 2：运行时基座 backlog（不阻塞主线，按优先级独立排期）

> 选题库：`runtime-infrastructure-gap-analysis.md`（B 清单实证核验版）。未挂载项独立 CR、独立验收；
> **v3 变更：部分项已降级版挂载战役 1（见状态列），挂载纪律见架构设计规格 §0.3/§3；其余项仍禁混入**（路由快照零差异死线）。工期为单人日预估，含测试与验收。

| # | 项 | 优先 | 预估 | 来源 | v3 状态 |
|---|----|------|------|------|---------|
| 2.1 | Outbox 卡死回收器（Sweeper）+ 分布式锁 | 高 | 2d | 专家包 | **已挂载战役 1 S3（T-F2）** |
| 2.2 | 缓存防击穿（GetOrAdd 合并 + jitter + CSRedis 替换评估）+ L1+L2 混合缓存分层评估（现为 memory/redis 单后端二选一） | 高 | 3d | 专家包 / B-P1-3 | |
| 2.3 | HTTP 出站韧性管线（评估 NuGet Polly v8 ResiliencePipeline，统一 LLM/MCP/附件/sa-service 出站面） | 高 | 4d | 专家包 0.2 / B-P0-3 | **部分挂载战役 1 S4（仅 LLM/MCP，T-F3）；全客户端铺开留 backlog** |
| 2.4 | PartitionedRateLimiter 按 tenant+user 分区 + 固定窗口→滑动窗口（消除窗口边界双倍突发） | 中 | 2d | 专家包 / B-P1-4 | |
| 2.5 | 日志三信号桥接（Serilog→OTel Logs）+ 请求级访问日志 + 脱敏 + Seq 默认化评估 | 中 | 3d | 专家包 0.4 / B-P1-1 | **降级版挂载战役 1 S2（T-F1：文件 JSON+查询 API，无部署依赖）；采集端/看板待运维就绪** |
| 2.6 | Quartz Job 异常边界基类 | 中 | 1d | 专家包 0.3.4（部分） | 部分被 T-F4 覆盖（非 HTTP 边界），余留 Quartz 专项 |
| 2.7 | 就绪探针补全（Redis/MQ/LLM 提供商/日志磁盘）；磁盘检查可复用现成 LogHealthCheckService 逻辑注册进 /health/ready 管道 | 中 | 1d | 专家包 / B-P1-5 | |
| 2.8 | PostgreSQL 方言实装 + 集成测试（ISqlDialectAdapter 已在战役 1 定义） | 低 | 5d | 用户 DB 兼容诉求 | |
| 2.9 | ITimeSeriesStore 时序数据库 provider | 低 | 5d | 用户 DB 兼容诉求 | |
| 2.10 | 降 CC 二期（GetListQuerySql CC140 等巨兽业务改写） | 低 | 5d | 铁律冲突修正 | |
| 2.11 | 写 API 幂等键（Idempotency-Key 中间件） | 低 | 3d | B-P2-1 | **Phase 1 砍除本期：痛点不成立（前端防抖+Outbox 幂等表兜底）** |
| 2.12 | DB schema 版本化迁移（替代纯 CodeFirst） | 低 | 5d | B-P2-2 | T-F4 异常记录升格（加列）的前置 |
| 2.13 | 运行时热路径基准（表单 CRUD/列表查询 P95 基线入 performance-baseline） | 低 | 2d | B-P2-3 | |
| 2.14 | 异常可分析性补救：SysLogEntity 异常记录结构化（type/code/innerChain 分列）+ 非 HTTP 层统一异常边界（HostedService/事件处理器/WebSocket/SSE）；不重建 Oops 契约 | 高 | 3d | 专家 P0-2（遗漏点 M1） | **降级版挂载战役 1 S5（T-F4：Json 内结构化不动 schema）；加列升格待 2.12** |
| 2.15 | RFC 9457 ProblemDetails 错误契约 + 429 响应体与 RESTfulResult 统一（需前端错误负载解析联动） | 低 | 2d | 专家 P2-4（遗漏点 M2） | **Phase 1 砍除本期：涉前端联动** |
| 2.16 | 告警规则/Grafana 面板 as-code 随仓管理（OTel 已在导出） | 低 | 2d | 专家 P2-6（遗漏点 M3） | **Phase 1 砍除本期：依赖 Grafana 基建；F2/F3 指标先产生** |

## 4. 验收门禁总表

| 门禁 | 断言 | 战役 |
|------|------|------|
| DI 健康 | ValidateScopes+ValidateOnBuild Development 全绿 | 0 |
| 路由契约 | api/visualdev 快照逐条零差异 | 1（每阶段） |
| 架构合规 | 引擎类零直接引用 SqlSugar 类型 | 1（S2 起） |
| 复杂度 | JNPF009 只随迁不上调不新增 | 1（每阶段） |
| 单测 | RunSqlCompiler 全编译路径覆盖 | 1（S1） |
| 契约 | IRunService 反射契约测试；瘦身后 WorkFlow 7 方法签名不变 | 1（S0/S5） |
| 全回归 | sln+CI+test:api+架构测试+活体冒烟 | 1（S5） |

## 5. 节点审批纪律

每阶段（含战役 0 各步骤）完成后提交「业务实现 + 质量自检 + 功能证据 + 验收对照」，
未经用户审批不得进入下一阶段（实现完整性铁律）。
