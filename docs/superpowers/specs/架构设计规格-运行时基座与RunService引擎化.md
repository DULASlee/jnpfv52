# 架构设计规格 — 运行时基座与 RunService 引擎化

- **日期**：2026-08-21
- **状态**：Phase 1-2 已拍板，Phase 3 交付（配套：`实施计划-运行时基座与RunService引擎化.md`）
- **上游**：`docs/superpowers/specs/2026-08-20-runservice-engine-refactor-design.md`（A+C 引擎化 spec）· `runservice-refactor-master-plan.md` v2.1 · `runtime-infrastructure-gap-analysis.md` v2
- **关联 CR**：CR-20260820-01

## 0. 范围定义（先减法后设计的落盘记录）

### 0.1 本期范围（四特性降级版 + 引擎化重构）

| # | 特性 | 降级形态（Phase 1 砍刀产物） | 挂载阶段 |
|---|------|------------------------------|---------|
| F1 | 可查询日志 | OTel 规范文件 JSON + 请求日志 + PII 脱敏 + 内置查询 API；**采集端点/看板移出本期**（无部署能力，待运维侧就绪再挂载） | S2 |
| F2 | Outbox 可靠性 | 卡死回收器 Sweeper + 基于 DB 的分布式互斥锁（防多实例抢批） | S3 |
| F3 | 出站韧性 | Polly v8（Microsoft.Extensions.Http.Resilience）仅覆盖 **LLM/MCP 热路径**命名客户端；非全客户端 | S4 |
| F4 | 异常边界 | 非 HTTP 入口统一拦截 + 异常记录 **Json 字段内结构化**（不动表 schema）；复用 Oops 契约 | S5 |
| F0 | 特性开关基建 | `App.json` 四布尔位，默认 false | S0 |
| — | RunService 引擎化 | A+C spec 原样（S0-S5，纯移动 + IRuntimeDataStore 抽象） | 全程 |

### 0.2 明确砍除（本期不做，留 backlog 或永久砍除）

- RFC 9457 ProblemDetails（2.15，涉前端联动）· 告警规则 as-code（2.16，依赖 Grafana 基建）
- P2-1 全局幂等键（Phase 1 拍板整体砍除：痛点不成立）· L1+L2 混合缓存 · 限流滑动窗口
- 异常表 schema 变更（type/code/innerChain 加列）→ 降级为 Json 内结构化，2.12 版本化迁移落地后再升格

### 0.3 组织形态（Phase 2 博弈一 · 方案 C 拍板）

**单轨穿插 + 阶段内双门禁分段裁决**：
- 利用实证事实——四特性文件面与重构文件面**零交集**（特性面：Filter/订阅者/Outbox/InteAssistant/Serilog；重构面：RunService/Runtime/）
- 每阶段验收窗口：重构工作 → 重构门禁绿 → 特性工作 → 特性门禁独立跑 → 一次节点审批**分段裁决**
- **红线纪律（ADR-1 固化）**：**特性门禁红不阻塞重构门禁通过**——重构部分照常获批合入，特性修复顺延下个窗口；反向亦然
- 挂载表：S0=开关基建 · S1=纯净 · S2=F1 · S3=F2 · S4=F3 · S5=F4

### 0.4 回滚轴分工（Phase 2 博弈二 · 方案 B 拍板）

| 对象 | 回滚轴 | 理由 |
|------|--------|------|
| 重构（纯移动、行为等价） | **阶段级 git revert** | 开关两侧行为相同，运行时开关语义为空；双路并存=拆分作废。每阶段独立 commit + 快照零 diff，revert 即完整回退 |
| 四特性（真实行为变更） | **运行时开关，特性级粒度** | 精确熔断：只关出问题的单个特性，不株连 |

## 1. 核心架构

### 1.1 结构挂靠点声明（副作用唯一漏斗——本规格新增硬约束）

| 未来/本期特性 | 挂靠点 | 结构前提 |
|--------------|--------|---------|
| F3 韧性（及未来全量韧性 2.3） | `IRuntimeDataStore` 装饰器层 | 引擎零绕道（唯一漏斗成立） |
| F2/事务类 | `RunInTransactionAsync` | 事务语义单点收敛 |
| F4 非 HTTP 异常边界 | 引擎统一 `Oops` 抛出面 + `IExceptionBoundary` 包装 | 引擎不吞异常、不自建层级 |
| F1 可查询日志（及 2.5 完整版） | `ILogger` + TraceIdMiddleware 全局上下文 | 引擎不自建日志通道 |

**硬约束**：引擎类禁止绕道第二 DB 通道。执行手段 = 架构测试构造白名单断言：引擎构造参数类型仅允许 `{RunSqlCompiler, IRuntimeDataStore, ILogger, IOptions, ICacheManager}`（落 `RunEngineSqlSugarBoundaryTests` 扩展）。

### 1.2 特性开关机制（F0）

`App.json` 新增节（JNPF 配置约定自动绑定 IOptions）：

```json
"RuntimeFoundation": {
    "QueryableLogging": false,
    "OutboxSweeper": false,
    "OutboundResilience": false,
    "ExceptionBoundary": false
}
```

```csharp
/// <summary>运行时基座特性开关（Phase 2 博弈二拍板：特性级粒度，默认全关）.</summary>
public class RuntimeFoundationOptions
{
    public const string Section = "RuntimeFoundation";
    public bool QueryableLogging { get; set; }
    public bool OutboxSweeper { get; set; }
    public bool OutboundResilience { get; set; }
    public bool ExceptionBoundary { get; set; }
}
```

翻牌纪律：**开关只翻一次、只在 S5 终审冒烟时按序翻**（F1→F2→F3→F4，每翻一个跑一轮冒烟，红即翻回）。任何阶段中途禁止翻牌。

## 2. 任务设计（T-F0 至 T-F4）

### 任务 T-F0：特性开关基建（S0 挂载，~1d）

#### 1. 基础定义与验收契约（纯后台型）
- 功能：四布尔位配置 + `RuntimeFoundationOptions` 绑定 + 单测守护默认值全 false。
- 触发契约：启动期绑定；各特性在自身挂载阶段读取。
- 监控：无需独立指标（基建件）。

#### 2. 工程五件套
- **2.1 ADR**：背景=③混入式需定点熔断；备选=单总开关（0.5d，熔断粒度不足，一损俱损）vs 四特性位（1d）；**决策=四特性位**，否决总开关理由：精确熔断价值 > 0.5d 差价（Phase 2 拍板）。
- **2.2 安全合规**：配置节无敏感数据；`App.json` 已有版本管理。无额外落点。
- **2.3 灰度**：纯新增配置节，向后兼容（缺节=全 false=现状行为）。
- **2.4 风险回滚**：配置错误→默认 false 兜底（GetSection 缺省值语义）。回滚=删配置节。
- **2.5 测试与 SLO**：单测 `RuntimeFoundationOptions_Defaults_AllFalse`、`Binds_FromAppJson`。

#### 3. 六大硬腿
全部 N/A——配置绑定件，无性能/内存/CLR 敏感面；组件化=IOptions 标准模式，无对标需求。

---

### 任务 T-F1：可查询日志降级版（S2 挂载，~3d）

#### 1. 基础定义与验收契约（纯后台型 + 新增只读 API）
- 功能四件：① SerilogBootstrap 增 `app-.json` 全级别文件 sink（JSON 字段对齐 OTel Logs 规范：`Timestamp/Level/TraceId/SpanId/SourceContext/Message/Exception`）；② 注册 `UseSerilogRequestLogging`（方法/路径/状态码/耗时）；③ PII 脱敏 Destructuring 策略（与①②同批交付——合规硬要求，防日志面扩大违规）；④ 新增只读 API `GET api/system/LogQuery`（时间窗/级别/TraceId 过滤，读 `logs/app-*.json`）。
- 触发契约：日志=管道自动；查询 API=管理员按需。
- 落库报表：无（文件即存储，保留策略沿用滚动 14 天）。

#### 2. 工程五件套
- **2.1 ADR**：背景=无部署能力（Phase 1 拍板），LGTM/Seq 部署栈移出；备选=内置查询 API（文件 grep 式，0 部署依赖）vs 先落 Serilog 不建查询（「可查询」目标未达成）；**决策=内置查询 API**，否决完整日志栈理由：Conway 约束（无 DevOps 角色），产生标准信号即可，看板待运维就绪挂载。
- **2.2 安全合规**：① 查询 API 必须声明 `[SecurityDefine]` 或限定管理员角色（L0 硬门控，API 权限铁律）；② 查询响应**按 TenantId 过滤**（日志事件含租户上下文，防跨租户泄漏——多租户铁律）；③ PII 脱敏策略：手机号/身份证/密码字段 Destructuring 时中位 `***`（PIPL 落地点，与日志面扩大同批）；④ 查询路径只读、文件路径白名单（仅 LogDir 内，防路径穿越）。
- **2.3 灰度**：开关 `QueryableLogging`；sink/中间件在 false 时按现状（error/warning 双路 + Console），**零行为变化**。
- **2.4 风险回滚**：已知风险=全级别落盘磁盘占用上升（对策：`app-.json` 单文件 50MB 上限 + 14 天保留 + LogDiskGuardService 现有 Error-only 降级自动兜底）。回滚=开关翻 false（sink 停止、查询 API 返回 503 说明）。
- **2.5 测试与 SLO**：单测=脱敏策略用例（手机号/身份证/密码三形态）、查询 API 时间窗与租户过滤断言；E2E=冒烟期产生一条带 TraceId 的日志→查询 API 按 TraceId 命中。SLO：查询 API P95 < 500ms（10 万行文件内）；告警分级=N/A（无独立 Oncall 载体，磁盘已有 LogDiskGuard 告警）。

#### 3. 六大硬腿
- **3.1 性能**：查询 API 为逐行流式读 + 提前终止（命中条数上限），不整文件载入内存；文件按天滚动天然分片。
- **3.2 内存**：流式 `StreamReader` + yield，禁止 `File.ReadAllLines`。
- **3.3 组件化**：脱敏策略为独立 `PiiDestructuringPolicy` 类（后续 2.5 完整版复用）。
- **3.4 健壮性**：查询路径对损坏 JSON 行跳过不抛（边界防御）。
- **3.5 CLR**：N/A——非热路径，Span 优化属过度设计。
- **3.6 对标**：Seq 查询模型（过滤表达式+时间窗）的极简子集；砍表达式引擎、只留三过滤器，规避自建查询引擎的维护黑洞。

---

### 任务 T-F2：Outbox Sweeper + DB 互斥锁（S3 挂载，~2d）

#### 1. 基础定义与验收契约（纯后台型）
- 功能：① `OutboxSweeperService : BackgroundService` 周期扫描（30s）`Processing` 状态超时消息（超 10 分钟）回置 `Pending` 并 `RetryCount+1`（超 MaxRetry 转 DeadLetter，复用现有死信路径）；② 扫描前获取 DB 互斥锁，防多实例重复回收。
- 触发契约：Cron 式 30s 轮询；开关门控。
- 监控指标：`outbox_sweeper_recovered_total`（回收条数，OTel metric 自定义源，ObservabilityModule 已有自定义源先例）；告警载体=N/A（指标先产生，告警规则属 2.16 已砍）。

#### 2. 工程五件套
- **2.1 ADR**：背景=进程崩在 MarkProcessing 后消息永久滞留（实证：GetPendingAsync 只取 Pending，无回收）；备选=Redis 分布式锁（引入 Redis 依赖前置，当前 Cache.json 支持 memory/redis 二选一，不可假设 Redis 在场）vs DB 锁表（一张 `EVENT_OUTBOX_LOCK` 表 + 实例标识 + 心跳时间戳，SqlSugar 即达）；**决策=DB 锁表**，否决 Redis 锁理由：不新增基建假设，Outbox 本就依赖 DB。
- **2.2 安全合规**：无新增 API、无敏感数据；回收 SQL 全参数化（L0）。
- **2.3 灰度**：开关 `OutboxSweeper`；false 时 BackgroundService 注册但首轮自检即退出（行为=现状）。锁表为新增表，CodeFirst 自动建（现有模式）。
- **2.4 风险回滚**：已知风险=误回收正在慢处理的真消息（对策：10 分钟超时阈值 >> 现有最长重试退避 16s×N；幂等消费表兜底重复消费）；锁表死锁（对策：心跳过期 60s 自动抢锁）。回滚=开关 false。
- **2.5 测试与 SLO**：单测=`Sweeper_RecoversTimedOutProcessing`、`Sweeper_SkipsWhenLockHeld`、`Sweeper_EscalatesToDeadLetter_WhenMaxRetryExceeded`（xUnit + SqlSugarClient 内存库，Stage5 已有先例）；SLO：卡死消息滞留时长 P99 < 10 分 30 秒；错误率指标=N/A（回收失败计入现有 Outbox 死信统计）。

#### 3. 六大硬腿
- **3.1 性能**：N/A——30s 一轮、单表索引扫描（Processing+UpdateTime），量级天然小。
- **3.2 内存**：N/A——批量上限 100 条/轮。
- **3.3 组件化**：锁抽象 `IOutboxLock`（DB 实现），未来 Redis 在场可换实现不改 Sweeper（防腐层）。
- **3.4 健壮性**：Sweeper 自身 try-catch 全包（后台服务异常裸奔正是 F4 要治的病，自身不得犯）。
- **3.5 CLR**：N/A。
- **3.6 对标**：Hangfire 的 JobExpirationTimeout 回收模型 + MassTransit Outbox sweeper 语义；取其「超时回置+重试上限升死信」，砍其独立存储依赖。

---

### 任务 T-F3：出站韧性管线（S4 挂载，~4d）

#### 1. 基础定义与验收契约（纯后台型）
- 功能：引入 NuGet `Microsoft.Extensions.Http.Resilience`（Polly v8 内置），为 **LLM 出站与 MCP 两个命名 HttpClient** 挂 `AddStandardResilienceHandler`（超时/重试/熔断标准组）；参数按 LLM 场景调优（总超时 120s——LLM 流式响应慢，重试仅对非流式端点，熔断 5 连败开 30s）。
- 触发契约：出站调用自动经过管线；开关门控。
- 监控指标：Polly v8 原生诊断事件桥接现有 OTel metrics（resilience 事件计数）；告警载体=N/A（2.16 已砍，指标先产生）。

#### 2. 工程五件套
- **2.1 ADR**：背景=出站零韧性（实证：LLM 挂起拖死线程池风险；全仓 ResiliencePipeline 0 引用；仅自研 PollyRetryHandlerExecutor 覆盖 EventBus 一处）；备选=继续自研（已有先例，0 新依赖）vs NuGet Polly v8 标准管线（业界 2025 标准，一行接入，微软维护）；**决策=Polly v8**，否决自研理由：自研版无隔舱、熔断状态无持久化、无诊断事件，补齐成本 > 直接引入（gap-analysis §0.1 结论）。
- **2.2 安全合规**：韧性配置不含敏感数据；重试不得重放携带一次性凭证的请求（LLM 调用幂等语义为只读推理，可重试；附件下载已有一处手工重试——**本期不收敛它**，避免扩大面，登记遗留）。注入防范=N/A（无用户输入进管线）。
- **2.3 灰度**：开关 `OutboundResilience`；false 时命名客户端不带 handler（行为=现状）。**API 向后兼容**：出站行为变化对上游 API 透明，无版本问题。
- **2.4 风险回滚**：已知风险=① 重试放大 LLM 配额消耗（对策：重试仅 2 次且退避，流式端点禁重试）；② 熔断误开导致 LLM 全面不可用（对策：熔断阈值 5 连败 + 30s 半开探测，开关兜底）。回滚=开关 false，管线摘除即刻生效。
- **2.5 测试与 SLO**：单测=用 MockHttpHandler 注入失败序列断言重试次数/熔断开启（Polly v8 测试原生支持虚拟时间）；集成=S4 冒烟期 LLM 端点真实调用 200。SLO：出站调用 P99 总耗时 < 120s（此前=无上界）；熔断打开事件进 OTel metrics。

#### 3. 六大硬腿
- **3.1 性能**：N/A——管线开销微秒级，对比 LLM 秒级响应可忽略。
- **3.2 内存**：N/A。
- **3.3 组件化**：管线经 `AddStandardResilienceHandler` 扩展点配置，未来全客户端铺开（2.3 完整版）= 复用同一扩展方法。
- **3.4 健壮性**：本任务本身就是健壮性方案（断路器/重试/超时三件套 + 隔舱由标准组内含）。
- **3.5 CLR**：N/A。
- **3.6 对标**：即业界 GA 标准本身（Microsoft.Extensions.Http.Resilience）；取舍=接受其固定五段管线结构，不自定义段（YAGNI）。

---

### 任务 T-F4：异常边界降级版（S5 挂载，~3d）

#### 1. 基础定义与验收契约（纯后台型）
- 功能三件：① 非 HTTP 入口统一异常拦截——HostedService 基类包装、事件处理器执行器外层、SSE/WebSocket 管道出口，统一经 `IExceptionBoundary` 记录后按各入口语义处置（后台=记日志+吞掉防进程崩；事件=交 Outbox 重试路径）；② 异常记录结构化——`LogExceptionHandler` 及新边界的入库 `Json` 字段改为结构化 JSON（`{type, code, message, innerChain[], context{FormId,FlowId,TenantId,TraceId}}`），**不加列不动 schema**；③ 引擎抛出面统一校验——架构测试断言引擎类不出现裸 `throw new Exception`。
- 触发契约：异常发生即触发。
- 监控指标：`exception_boundary_caught_total`（按入口类型 label）；告警载体=N/A（2.16）。

#### 2. 工程五件套
- **2.1 ADR**：背景=实证异常拦截仅 MVC 管道（ExceptionContext），入库平铺 `Message+"\n"+StackTrace` 不可聚合；备选=DB 加列结构化（type/code/innerChain 分列，可 SQL 聚合，但 schema 变更涉 2.12 版本化迁移缺失现状）vs Json 字段内结构化（零 schema 风险，聚合靠日志查询/未来迁移后升格）；**决策=Json 内结构化**，否决加列理由：当前无版本化迁移能力（2.12 在 backlog），裸 ALTER 违反自身立下的纪律；升格路径已登记。
- **2.2 安全合规**：异常上下文仅含业务标识（FormId/FlowId/TenantId）与 TraceId，**禁止入栈变量值**（防敏感数据进日志库——PIPL）；异常入库沿用现有 `Log:CreateExLog` 事件路径，权限面不变。
- **2.3 灰度**：开关 `ExceptionBoundary`；false 时边界不注册、入库格式维持旧平铺（字节级兼容）。**过渡策略**：翻 true 后新旧格式共存于 Json 字段，查询侧按首字符 `{` 判格式（兼容读）。
- **2.4 风险回滚**：已知风险=边界吞异常掩盖故障（对策：吞之前必记 Error 日志 + metric 计数，「吞」仅指不炸进程）；回滚=开关 false。
- **2.5 测试与 SLO**：单测=`Boundary_CapturesStructuredJson`、`Boundary_DoesNotSwallowSilently_MetricIncremented`、`LogExceptionHandler_JsonFormat_IsStructured_WhenFlagOn`；集成=S5 冒烟人为触发一次后台作业异常，确认入库结构化且进程存活。SLO：非 HTTP 未捕获异常导致的进程崩溃次数 = 0（当前无基线，本期建立）。

#### 3. 六大硬腿
- **3.1 性能**：N/A——异常路径低频。
- **3.2 内存**：innerChain 深度上限 5（防异常链序列化爆内存）。
- **3.3 组件化**：`IExceptionBoundary` 策略接口 + 每入口一个薄适配器；复用 Oops 契约不新建异常层级（铁律）。
- **3.4 健壮性**：本任务即健壮性基座；边界自身零抛异常（内部全包 try-catch）。
- **3.5 CLR**：N/A。
- **3.6 对标**：ASP.NET Core 8 `IExceptionHandler`（HTTP 层已有等价物 FriendlyExceptionFilter）的非 HTTP 延伸；取 .NET Aspire 的 host 级异常边界语义，砍其 Dashboard 依赖。

## 3. 与重构轨的接口约定（双轨纪律落盘）

1. 特性轨**禁止触碰**重构文件面（RunService.cs / Runtime/ / IRunService.cs）；重构轨**禁止消费**特性开关。唯一交汇点：S5 终审冒烟的开关翻牌序列。
2. 每阶段节点审批材料固定双段：**重构门禁段**（快照零 diff + 测试 + CI）与**特性门禁段**（该阶段特性验收五项）；任一段红只阻塞本段。
3. 特性轨证据独立命名 `f{N}-*.txt` 落 `.claude/evidence/cr-20260820-01/`，与重构轨 `s{N}-*.txt` 并列不混。

## 4. Phase 4 预埋：实施校准钩子

| 钩子 | 验证时机 | 验证内容 |
|------|---------|---------|
| Sweeper 并发正确性 | S3 特性验收 | 双实例模拟（两个 Sweeper 实例同跑）断言同一批消息只被回收一次 |
| 韧性管线真实生效 | S4 特性验收 | 注入 3 次失败断言第 3 次经重试成功 + 熔断事件计数=1 |
| 挂靠点未被破坏 | S5 终审 | 白名单架构测试绿 + grep 引擎层零 `IDataBaseManager`/`ISqlSugarRepository` 引用 |
| 日志可查闭环 | S2 特性验收 | TraceId 从请求头贯穿到文件 JSON 到查询 API 命中（三跳贯通） |
| **决策回溯触发条件** | 全程 | 若 S4 韧性导致 LLM 配额异常放大 >30%，回 Phase 2 博弈二重评重试参数；若磁盘占用因 F1 翻倍，重评全级别落盘决策 |
