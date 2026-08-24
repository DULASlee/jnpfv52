# 实施计划 — 运行时基座与 RunService 引擎化（v10.5 配套）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 按单轨穿插+双门禁分段裁决（规格 ADR-1）执行 RunService 引擎化重构（S0-S5）与四特性降级版（M7-M10）的混入式施工；重构回滚轴=阶段级 git revert，特性回滚轴=四布尔开关（ADR-2）。

**Architecture:** 重构轨纯移动（快照零 diff 门禁），特性轨文件面零交集（行为测试门禁），每阶段重构段门禁绿后开工同窗特性模块；特性门禁红不阻塞重构门禁（铁律）。

**Tech Stack:** .NET 8 / SqlSugar / xUnit / Polly v8（Microsoft.Extensions.Http.Resilience）/ Serilog / JNPF.Startup.Benchmarks harness

**设计事实源：** `docs/superpowers/specs/架构设计规格-运行时基座与RunService引擎化.md`（v10.5 版，下称规格；任务编号与规格 4.N 模块对应；契约台账=`docs/architecture/contract-registry.md`，规格 5.7）。**执行前提（规格 P0）**：运行模式=T1 工具完备（契约 hash 于物化任务计算登记）；人工闸门：G1~G3 已过，G4 批量授权在案，每个 S 段节点审批=实施层门禁；G5 终审待过。

---

## 红线纪律（违反任一=停工）

1. **纯移动纪律**：重构轨方法体逐字不改；行为变更一律归特性轨。唯一豁免：M3 DB 读取参数化剥离（Task 3.3，方法内 DB 读取改参数传入；SQL 输出等价性由特征单测守护，无其他豁免）。
2. **路由快照零差异**：每阶段门禁 `diff s{N}-routes.txt s0-routes-visualdev-baseline.txt` 必须为空（唯一例外：M7 查询 API 新增路由，S5 终审前声明性重录并人审，见规格 4.8.8 验收⑤）。
3. **双门禁分段裁决**：特性门禁红不阻塞重构门禁通过；冒烟红先归因到轨道再处置。
4. **零 schema 变更**：禁止对任何表加列/改列（规格 §5.2 迁移基线，10.5 降级落位）；M8 锁表建表机制未核验前不得写建表代码。
5. **多轨文件面零交集**：各任务 🚫 清单见规格 §3.3；共享文件（Program.cs 特性注册段/App.json RuntimeFoundation 节）归属已声明。
6. **节点审批门禁**：每阶段（S0~S5）完成后暂停，提交「业务实现+质量自检+功能证据+验收对照」，未经用户审批不得进入下一阶段。
7. **假设未验证不动工**：规格 §5.4 A-1~A-4 四条假设在对应任务开工前必须按验证方式闭环（A-2 须用户拍板）。
8. **契约库纪律（规格 5.7）**：字段级约束唯一源=契约库（C# 接口源码+契约测试+契约台账）；物化次序=4.N.9 自检通过后、下游引用前（任务 1.2/2.2/3.2/7.3/8.1/9.1/10.1/11.1/6.4，6.4 承载 C-RS v0→v1 升级重录）；登记契约 ID@版本+SHA256；下游编写/生成前校验 hash，不一致即阻断；禁止凭 4.N.10 摘要生成集成代码。
9. **假设台账管理（规格 5.4，模板 10.5）**：A-1~A-4 按各自验证方式在对应任务开工前闭环，闭环后回填验证状态与证据位置；A-2 未拍板不开工 Task 7.3；假设部分成立按新版本号重新登记不改原条目。

## 命令速查

| 用途 | 命令 | 工作目录 |
|------|------|---------|
| 路由快照 | `dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes --filter "api/visualdev"` | `backend/` |
| 后端构建 | `dotnet build` | `backend/` |
| 架构测试 | `dotnet test --filter FullyQualifiedName~RunEngineSqlSugarBoundary` | `backend/` |
| VisualDev 测试 | `dotnet test --filter FullyQualifiedName~JNPF.Tests.VisualDev` | `backend/` |
| API 冒烟 | `node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser` | repo root |

---

## S0 基建与安全网（11h）

### Task 11.1：M11 开关基建落盘（4h）｜依赖：无

**Files:** ➕ `RuntimeFoundationOptions.cs`（落位先闭环假设 A-1：grep 既有 Options 类归属，10 分钟内）｜ ✏️ `backend/application/JNPF.API.Entry/Configurations/App.json`（新增 `RuntimeFoundation` 节，四布尔位全 false）｜ ✏️ Program.cs 特性注册段（Options 绑定）｜ ➕ 单测×2 ｜ ➕ `f0-log-baseline.txt`（顺带采集当前日志量级，供 Task 7.2 磁盘对照）

- [x] Step 1：grep 既有 Options 类归属目录，闭环 A-1，记录结论（✅落位 `framework/JNPF/Options/`：EventBus.Outbox 仅引 framework、InteAssistant 仅引 Common.Core，此为共同可达最下层）
- [x] Step 2：写失败测试（缺配置→默认全 false；显式配置→正确绑定）（✅）
- [x] Step 3：落 Options 类+配置节+绑定（RuntimeFoundationModule），测试转绿 2/2；C-M11-Options@v1 物化入台账（✅）
- [x] Step 4：开关全 false 启动，采路由快照并与基线比对零 diff（开关零侵入证明，✅ DIFF=0）
- [x] Step 5：采集 `f0-log-baseline.txt`（✅ 峰值日 2.72MB）；提交（✅）

### Task 1.1：路由快照基线（2h）｜依赖：无

**Files:** ➕ `backend/tools/JNPF.Startup.Benchmarks/` 输出落盘 `s0-routes-visualdev-baseline.txt`（存档位置随仓约定：`.claude/evidence/cr-20260820-01/` 或 docs 约定目录，开工时定一处并全程一致）

- [x] Step 1：运行路由快照命令，落盘基线（✅ 1077 路由/107 匹配，存档 `.claude/evidence/runservice-engine-refactor/`）
- [x] Step 2：核对 `[METRIC] route_total/route_matched`，确认 `api/visualdev` 面全覆盖（✅）
- [x] Step 3：提交（基线未落盘前禁止动 RunService）（✅）

### Task 1.2：IRunService 契约测试（3h）｜依赖：无

**Files:** ➕ `backend/tests/JNPF.Tests.VisualDev/RunServiceContractTests.cs`（反射+属性名字符串匹配，零 MVC 类型依赖；18 成员签名冻结【2026-08-24 实测，原载 17 修订，裁决 A】+ WorkFlow 消费 5 方法与全仓消费面 15 成员 nameof 守护）➕ `docs/architecture/contract-registry.md`（契约台账，规格 5.7）

- [x] Step 1：写契约测试（18 成员签名反射断言；5+15 成员 nameof 常量守护）（✅）
- [x] Step 2：运行全绿（契约测试全绿=存量契约逐条确认载体，✅ 3/3）
- [x] Step 3：建契约台账，反向提取登记 C-RS-IRunService@v0（18 成员：ID@版本+SHA256+路径，✅）
- [x] Step 4：提交（✅）

### Task 1.3：委托方归属测试（2h）｜依赖：无

**Files:** ➕ `backend/tests/JNPF.Tests.VisualDev/VisualDevRouteOwnerTests.cs`（三委托方 OnlineDev/Base/ShortLink 的 Name/Route 契约）

- [x] Step 1：写三委托方归属断言（✅ Base/OnlineDev/ShortLink Name+Order+Tag+Route+IDynamicApiController）
- [x] Step 2：运行全绿（✅ 9/9）；提交（✅）

**S0 节点审批**：提交四任务证据 + 基线文件 + A-1 闭环记录，等待批准进入 S1。

---

## S1 编译层（16h）

### Task 3.1：RunSqlCompiler 骨架（1h）｜依赖：1.1-1.3

**Files:** ➕ `backend/modularity/visualdev/JNPF.VisualDev/Runtime/RunSqlCompiler.cs`（ISingleton 空骨架）

- [x] Step 1：建骨架类（构造无参，零 DI 依赖）（✅ 03bffa77；施工实测发现七方法 40+ 处 DB 调用与零 DI 定性冲突 → 用户裁决 A：逐字移动+过渡承载，记录见 `.claude/evidence/runservice-engine-refactor/ruling-task32-db-coupling.md`）
- [x] Step 2：构建 0 错误；提交（✅）

### Task 3.2：七方法纯移动（4h）｜依赖：3.1

**Files:** ✏️ `RunService.cs`（GetListQuerySql/GetInfoQuerySql/GetQueryJson/GetSuperQueryJson/GetSuperQueryInput/GetIConditionalModelListByTableName/GetVisualDevModelDataConfig 移出）｜ ✏️ `Runtime/RunSqlCompiler.cs`

- [x] Step 1：七方法方法体**逐字**迁入 RunSqlCompiler；RunService 调用点改 `_compiler.X`，IRunService 成员保留委托转发（✅ 脚本化逐行抽取+三类机械适配，见裁决记录；RunSqlCompileContext 过渡载体；RunService 4158→2874 行）
- [x] Step 2：构建 0 错误（✅ 全解决方案）
- [x] Step 3：路由快照与基线零 diff；C-M3-RunSqlCompiler@v1 物化入台账（SHA256 登记）；提交（✅ 1077/107 与基线逐行零 diff；存量测试 204/204 绿；快照 `s1-task32-routes.txt`）

### Task 3.3：DB 依赖参数化剥离（4h+，裁决 C 严格口径增量拆解）｜依赖：3.2

> **裁决 C（2026-08-24，用户拍板严格口径）**：验收①「零 SqlSugar 类型引用」含条件模型 DTO 类型；
> 规格 4.3.2 签名口径修订为「语义一致（SqlSugar 类型→平台自有类型）」。
> 施工方案三增量：设计事实源 `.claude/evidence/runservice-engine-refactor/ruling-c-task33-strict-zero-sqlsugar.md`。

**Files:** ✏️ `Runtime/RunSqlCompiler.cs` ｜ ➕ `Runtime/CompileConditionalModels.cs` ➕ `Runtime/CompileConditionalConverter.cs` ➕ `Runtime/RunSqlCompileGateway.cs`（替换并删除 `RunSqlCompileContext.cs`）｜ ✏️ `RunService.cs`（调用侧供数+入口转换）

- [x] Inc-1：平台条件模型类型+双向转换器+JSON 往返等价单测（零行为变更，先行提交）（✅ 6/6 绿：字节等价/双向转换/嵌套结构/Utilities 回解析/JsonIgnore 对齐；实测修正：FieldValueConvertFunc=Func<string,object>+[JsonIgnore]）
- [x] Inc-2：特征捕获前置（3.5 提拉）——剥离前对当前实现捕获代表性输入输出快照为期望值（禁止手写猜测）（✅ 8/8 基线落盘 `.claude/evidence/runservice-engine-refactor/feature-capture/`；覆盖纯路径：GetQueryJson 11 控件分支/GetSuperQueryJson 双组/GetInfoQuerySql 双分支/GetListQuerySql 主表+主副表/GetSuperQueryInput；DB 分支残余风险已登记）
- [x] Inc-3：参数化剥离——gateway 委托供数；变异语义逐处核对（dataRuleList 跨迭代就地删减等）；删除过渡载体；构建 0 错+快照零 diff；提交（✅ RunSqlCompileGateway 十成员供数；平台条件模型全链切换；实测修正：PS -replace 大小写误伤已修复；RunService 2875→~2917 行（网关供数侧）；快照 `s1-task33-routes.txt`）
- [x] 验收：①grep `RunSqlCompiler.cs` 零 SqlSugar（含 using）②特征单测全绿 ③存量测试全绿 ④C-M3 台账重录（过渡注记移除）（✅ ①零命中 ② 8/8 逐字一致 ③ 218/218 ④ `778cfee5…` 重录）

### Task 3.4：JNPF009 基线随迁（2h）｜依赖：3.3

**Files:** ✏️ `backend/tools/JNPF.Analyzers/complexity-baseline.json`（七方法相关条目归属改 RunSqlCompiler）

- [ ] Step 1：`dotnet build /p:CI_BUILD=true` 定位受影响条目
- [ ] Step 2：基线条目随迁（只改归属不改阈值）
- [ ] Step 3：CI 构建绿；提交

### Task 3.5：特征单测（4h）｜依赖：3.3

**Files:** ➕ `backend/tests/JNPF.Tests.VisualDev/RunSqlCompilerFeatureTests.cs`

- [ ] Step 1：**特征捕获**——以真实/代表性输入在迁移后实现上抓取输出快照作为期望值（禁止手写猜测，Evidence Over Assumption）
- [ ] Step 2：落成单测运行全绿
- [ ] Step 3：提交

### Task 3.6：S1 门禁（1h）｜依赖：3.1-3.5

- [ ] Step 1：路由快照零 diff + 契约测试全绿 + 特征单测全绿 + CI 构建绿
- [ ] Step 2：落盘 `s1-routes.txt`；提交门禁证据

**S1 节点审批**：等待批准进入 S2。

---

## S2 数据访问抽象（23h）+ M7 可查询日志（12h）

### Task 2.1：引擎边界架构测试（3h）｜依赖：3.6

**Files:** ➕ `backend/tests/JNPF.Tests.Architecture/RunEngineSqlSugarBoundaryTests.cs`

- [ ] Step 1：写双断言——①引擎类（类型名清单：RunSqlCompiler/RunDataEngine/RunListQueryService/RunDataViewService/RunService）字段/构造/方法签名零 SqlSugar 类型；②引擎构造参数类型 ∈ 白名单 `{RunSqlCompiler, IRuntimeDataStore, ILogger<>, IOptions<>, ICacheManager}`
- [ ] Step 2：S2 阶段 RunService 门面暂列豁免位（豁免理由注释 + S5 恢复的 TODO 标记）
- [ ] Step 3：反向用例——白名单外注入测试桩类时断言确实红
- [ ] Step 4：对 RunSqlCompiler 即刻绿；提交

### Task 2.2：契约与 RuntimeDbLink（2h）｜依赖：2.1

**Files:** ➕ `Runtime/IRuntimeDataStore.cs` ➕ `Runtime/RuntimeDbLink.cs`（签名逐字采用规格 4.4.2）

- [ ] Step 1：落接口与值对象；C-M2-IRuntimeDataStore@v1 物化入台账；构建 0 错误；提交

### Task 2.3：实现与状态迁移（4h）｜依赖：2.2

**Files:** ➕ `Runtime/SqlSugarRuntimeDataStore.cs`（ITransient+IDisposable，承接 `_sqlSugarClient`）｜ ✏️ `RunService.cs`（删 `_sqlSugarClient` 字段，改注入 IRuntimeDataStore）｜ ✏️ DI 注册（约束表：`docs/architecture/runservice-refactor-di-constraints.md` §2）

- [ ] Step 1：实现 8 成员（ResolveDbLink 主库/外部源解析）
- [ ] Step 2：状态与 Dispose 职责迁移；构建 0 错误
- [ ] Step 3：快照零 diff；提交

### Task 2.4：收敛 L 系列 36 处（⚠探索型 6h）｜依赖：2.3

**Files:** ➕ `s2-convergence-ledger.md`（台账 L1-L36：行号/类别/去向）｜ ✏️ `RunService.cs`

- [ ] Step 1：grep 采集 49+8 处调用面，建台账编号（L 系列=Utilities×12/SqlQueryable×7/CurrentConnectionConfig×3/AsTenant×4/Ado 查询类；与 Q 系列不重叠，规格 4.4.2）
- [ ] Step 2：逐处收敛执行入口至 IRuntimeDataStore；每处勾台账
- [ ] Step 3：grep 佐证清零+构建 0 错误+快照零 diff；提交

### Task 2.5：改写 Q 系列 27 处（⚠探索型 6h）｜依赖：2.4

**Files:** ✏️ `RunService.cs` ｜ ✏️ `s2-convergence-ledger.md`（Q1-Q27 / M 系列）

- [ ] Step 1：逐处 LINQ Queryable→SqlQueryable 改写，ToSql 前后比对（Normalize=去空白+参数占位符归一）
- [ ] Step 2：不等价处**禁止保留直调**——经 IRuntimeDataStore 扩展成员承载（台账 M 系列，规格 4.4.1 BR-1 豁免废除）
- [ ] Step 3：实体型 Queryable（平台元数据表）保留原仓储，台账标注不迁移
- [ ] Step 4：RunService 中 AsSugarClient/_sqlSugarClient grep 清零；构建 0 错误+快照零 diff；提交

### Task 2.6：S2 门禁与外部链路冒烟（2h）｜依赖：2.5

- [ ] Step 1：架构测试全绿 + 台账逐行勾全 + 快照零 diff
- [ ] Step 2：外部数据源活体冒烟（RuntimeDbLink 外部源路径实测）
- [ ] Step 3：落盘 `s2-routes.txt`+台账；提交

### Task 7.1：PII 脱敏策略（3h）｜依赖：11.1（S2 重构段门禁绿后开工）

**Files:** ➕ `backend/application/JNPF.API.Entry/Infrastructure/PiiDestructuringPolicy.cs` ➕ 五用例单测

- [ ] Step 1：写失败测试（手机前3后4/身份证前4后4/密码属性词表 `{password, secret, token, apikey}` 整体 ***/无关属性不误伤/嵌套对象穿透）
- [ ] Step 2：实现 IDestructuringPolicy 转绿；提交

### Task 7.2：全级别 sink 与请求日志（3h）｜依赖：7.1

**Files:** ✏️ `SerilogBootstrap.cs`（QueryableLogging=true 时追加 `app-{date}.json` 全级别 sink：按日滚动+50MB 分片+自定义 OTel 字段 formatter【Timestamp/Level/TraceId/SpanId/TenantId/UserId/SourceContext/Message/Exception】；既有 error/warning 两路不动）+ `UseSerilogRequestLogging`（TraceIdMiddleware 之后）

- [ ] Step 1：落 formatter（字段映射：解决 CompactJson 默认 @t/@l 与 OTel 字段名不一致，规格 4.8.1 BR-5）
- [ ] Step 2：开关 true：验证 app 文件含 TraceId+TenantId；开关 false：零侵入（不生成文件无请求行）
- [ ] Step 3：对照 `f0-log-baseline.txt` 记录磁盘放大倍数；提交

### Task 7.3：LogQueryService 查询 API（4h）｜依赖：7.2

**Files:** ➕ `LogQueryService.cs`（IDynamicApiController，`[SecurityDefine]` 权限点）

- [ ] Step 1：实现查询（入参 startTime/endTime/level/traceId/keyword/tenantId/page/pageSize；扫描规则：按日期文件名枚举→时间窗过滤→最多 31 文件→流式合并按时间排序；租户过滤硬约束；文件路径白名单防穿越）
- [ ] Step 2：开关 false 返回业务错误码「功能未启用」（非 503）
- [ ] Step 3：租户越权用例必须红；C-M7-LogQueryApi@v1 物化入台账；提交

### Task 7.4：F1 验证与三跳贯通（2h）｜依赖：7.3

- [ ] Step 1：三跳贯通验证（写入→滚动→按条件查出）证据落盘
- [ ] Step 2：日志可查率抽样：10 条请求凭 TraceId 命中 100%；路由快照零 diff（M7 新增路由除外，登记声明）；提交

**S2 节点审批**：重构段（2.1-2.6）与特性段（7.1-7.4）证据分别提交（双门禁分段裁决）；等待批准进入 S3。

---

## S3 执行层（10h）+ M8 Outbox 可靠性（9h）

### Task 4.1：RunDataEngine 骨架（1h）｜依赖：2.6

- [ ] Step 1：建骨架（ITransient，构造注入 RunSqlCompiler+IRuntimeDataStore）；白名单断言对本类即刻绿；提交

### Task 4.2：二十方法纯移动（⚠探索型 6h）｜依赖：4.1

**Files:** ➕ `Runtime/RunDataEngine.cs` ｜ ✏️ `RunService.cs`（规格 4.5.1 行号清单 20 方法）

- [ ] Step 1：方法体逐字迁移（含存量裸 throw 原样保留，记入 F4 台账登记）
- [ ] Step 2：调用点改委托；构建 0 错误+Helpers 全绿+快照零 diff；提交

### Task 4.3：基线随迁与 CRUD 冒烟（3h）｜依赖：4.2

**Files:** ✏️ `complexity-baseline.json`（CC90/CC81/CC82/CC74 四条随迁）

- [ ] Step 1：基线随迁+CI 构建绿
- [ ] Step 2：CRUD 全链路冒烟（创建/更新/批量/校验路径实测）；落盘 `s3-routes.txt`；提交

### Task 8.1：Outbox DB 互斥锁（3h，含前置核验）｜依赖：11.1（S3 重构段门禁绿后开工）

- [ ] Step 1：**前置核验**（闭环假设 A-3）：核对 EventOutboxMessage 字段（Status/RetryCount/MaxRetry/DeadLetter）+ 定位 Outbox 表现行建表机制；落盘 `f2-outbox-schema-check.txt`；缺字段→停手上报（禁私自加列）
- [ ] Step 2：写失败测试（锁三用例：空闲获取/持锁失败/过期 60s 抢锁，虚拟时钟）
- [ ] Step 3：落 `EventOutboxLock` 实体+`IOutboxLock`/`DbOutboxLock`（条件更新乐观并发）；锁表建表与核验出的机制同源（无机制→SQL 脚本随仓+部署清单登记）；测试转绿；C-M8-IOutboxLock@v1 物化入台账；提交

### Task 8.2：OutboxSweeperService 回收器（4h）｜依赖：8.1

**Files:** ➕ `OutboxSweeperService.cs`（BackgroundService：30s 轮询/抢锁/扫 10min 批 ≤100 条/回置或升死信/ExecuteAsync 全包 try-catch）｜ ✏️ Outbox 模块注册处（`if (options.OutboxSweeper) AddHostedService`）

- [ ] Step 1：写失败测试（回收四用例：超时回收/升死信/持锁跳过/双实例并发，内存库先例 `JNPF.Tests.Stage5/Program.cs`；矩阵外转移显式拦截断言，规格 4.9.2 状态转移矩阵硬性）
- [ ] Step 2：实现转绿；开关 false 验证服务不注册；提交

### Task 8.3：F2 验证与并发证据（2h）｜依赖：8.2

- [ ] Step 1：Stage5 全绿 + 快照复核零 diff；落盘 `f2-sweeper-concurrency.txt`；提交

**S3 节点审批**：双段证据分别提交；等待批准进入 S4。

---

## S4 列表层（8h）+ M9 出站韧性（12h）

### Task 5.1：列表层纯移动（⚠探索型 6h）｜依赖：4.3

**Files:** ➕ `Runtime/RunListQueryService.cs` ｜ ✏️ `RunService.cs` ｜ ✏️ 基线（GetListResult 条目）

- [ ] Step 1：五方法（GetListResult CC85/GetRelationFormList/GetHaveTableInfo/GetHaveTableInfoDetails/GetListChildTable）+专属辅助逐字迁移（CC85 逐块移动）
- [ ] Step 2：构建 0 错误+List*Helpers 既有测试全绿+快照零 diff；提交

### Task 5.2：S4 门禁与冒烟（2h）｜依赖：5.1

- [ ] Step 1：列表路径冒烟+落盘 `s4-routes.txt`；提交

### Task 9.1：管道工厂+失败测试先行（3h）｜依赖：11.1（S4 重构段门禁绿后开工）

**Files:** ➕ `OutboundResiliencePipelineFactory.cs`（InteAssistant 模块）✏️ InteAssistant csproj（Polly v8 + Microsoft.Extensions.Http.Resilience 包引用；**NuGet 漏洞扫描状态记录落盘**）

- [ ] Step 1：写失败测试（工厂可独立实例化+Mock handler：重试 3 次/熔断开启/快速失败/总超时截断四用例——不依赖装载与开关，规格 4.10.8）
- [ ] Step 2：落工厂骨架（超时 150s 总→熔断 5 次/60s 窗 30s 断→重试 3 尝试退避 2s/4s+jitter→单次 45s）；C-M9-Pipeline@v1 物化入台账；提交

### Task 9.2：行为测试转绿（4h）｜依赖：9.1

- [ ] Step 1：实现管道策略组合使四用例转绿
- [ ] Step 2：参数自洽复核（45×3+6=141s < 150s）；提交

### Task 9.3：两处装载与开关门控（3h）｜依赖：9.2

**Files:** ✏️ LlmGatewayService HttpClient 注册处 ✏️ HttpMcpTransport HttpClient 注册处

- [ ] Step 1：`if (options.OutboundResilience) builder.AddResiliencePipeline(...)` 两处装载
- [ ] Step 2：开关 false 时管道不在链（行为=现状）验证；提交

### Task 9.4：F3 门禁与指标注册证据（2h）｜依赖：9.3

- [ ] Step 1：重试计数/熔断状态指标已产生并注册（MeterListener 单测捕获；展示待 2.16 登记）
- [ ] Step 2：LLM/MCP 出站冒烟 200；提交

**S4 节点审批**：双段证据分别提交；等待批准进入 S4b/S5。

---

## S4b+S5 视图层收尾（17h）+ M10 异常边界（9h）

### Task 6.1：视图层纯移动（3h）｜依赖：5.2

**Files:** ➕ `Runtime/RunDataViewService.cs` ｜ ✏️ `RunService.cs`

- [ ] Step 1：视图四方法逐字迁移；构建 0 错误；提交

### Task 6.2：S4b 门禁与视图冒烟（2h）｜依赖：6.1

- [ ] Step 1：视图路径冒烟+快照零 diff；落盘 `s4b-routes.txt`；提交

### Task 6.3：CodeGen CR 起草审批（2h）｜依赖：6.2

**Files:** ➕ `.claude/change-requests/CR-{日期}-{NN}.md`（切换点/目标注入类型/回滚方式）

- [ ] Step 1：起草 CR（ExportImportDataHelper 注入点切换方案）；**未批禁触** `ExportImportDataHelper.cs`
- [ ] Step 2：提交用户审批；批准后 `workflow-state.json` 标 cr-approved

### Task 10.1：入口台账与契约失败测试先行（3h）｜依赖：11.1（挂 S5 窗口，与 6.3 并行）

**Files:** ➕ 非 HTTP 入口台账（grep 采集：BackgroundService/IHostedService/IEventHandlerExecutor/SSE/WebSocket 管道；**OutboxSweeperService 标注自治不接线**；存量裸 throw 技术债登记）➕ `IExceptionBoundary.cs` ➕ 失败测试

- [ ] Step 1：grep 采集入口清单落台账
- [ ] Step 2：写失败测试+落契约（规格 4.11.7 签名）；C-M10-IExceptionBoundary@v1 物化入台账；提交

### Task 6.5：CodeGen 切换执行（3h）｜依赖：6.3 批准

- [ ] Step 1：按批准的 CR 切换 ExportImportDataHelper 注入点
- [ ] Step 2：46 绿导入导出安全网测试（`UsersImportExportContractTests` 等）不回坡；快照零 diff；提交

### Task 10.2：实现与入口接线（4h）｜依赖：10.1

**Files:** ➕ `SysLogExceptionBoundary.cs` ➕ 入口包装器 ➕ `JNPF.Tests.Common/EngineThrowSiteBaselineTests.cs`（**特性轨测试项目**）｜ ✏️ 接线点入口最外层（开关门控）

- [ ] Step 1：实现结构化组装（`{type, code, message, innerChain, entry}`，深度上限 5）+写库失败 Console 降级
- [ ] Step 2：逐入口接线（ExceptionBoundary 开关门控）；抽样验证：人造 HostedService 异常→SysLog Json 结构化可查
- [ ] Step 3：抛出面断言绿（新增受控；存量豁免口径在断言注释显式声明）；提交

### Task 6.4：IRunService 瘦身与门面缩壳（4h）｜依赖：6.5

**Files:** ✏️ `IRunService.cs`（18→15，裁决 A：仅退出零外部消费的 CreateHaveTableSql/UpdateHaveTableSql/GenerateFeilds，保留为门面公开方法）✏️ `RunService.cs`（缩壳）✏️ 架构测试（移除 RunService 豁免位，恢复白名单断言）

- [ ] Step 1：行数统计基线证据先行（缩壳前实测行数落盘），目标 <400 行
- [ ] Step 2：接口切换（保留全仓消费面并集 15 成员；仅退出零外部消费 3 方法，退出成员保留为门面公开方法供内部调用；切换前后契约快照对比，确认只有预期 3 成员退出）；契约台账 C-RS-IRunService 升主版本 v0→v1（破坏性变更双轨过渡，规格 5.7）
- [ ] Step 3：门面缩壳至委托转发；构建 0 错误+全测试绿+快照零 diff；提交

### Task 10.3：F4 门禁与特性终审冒烟（2h）｜依赖：10.2 + 6.4

- [ ] Step 1：F4 门禁：断言绿+开关 false 行为=现状+指标已产生并注册（MeterListener）
- [ ] Step 2：**特性终审**：四开关按序翻牌（ExceptionBoundary→OutboxSweeper→OutboundResilience→QueryableLogging），每翻一位全链冒烟+快照复核；全 true 状态全链冒烟落盘；提交

### Task 6.6：重构终审六门禁（3h）｜依赖：**仅 6.4**（终审拆分：不依赖特性轨）

- [ ] Step 1：六门禁逐项——①路由快照与 S0 基线零 diff（M7 新增路由声明性重录人审）②契约测试全绿（含切换后重录）③特征/Helpers/架构测试全绿 ④CRUD+外部源冒烟 ⑤基线（complexity）CI 绿 ⑥门面行数证据+白名单断言恢复
- [ ] Step 2：落盘 `s5-routes.txt`+终审证据包；提交

**S5 节点审批**：重构终审（6.6）与特性终审（10.3）**分别出具结论**——特性红登记缺陷不阻塞重构轨交付（ADR-1 铁律）。

---

## 工时汇总（唯一口径）

| 段 | 任务 | 工时 | 累计 |
|----|------|------|------|
| S0 | 11.1 + 1.1/1.2/1.3 | 4+2+3+2 = 11h | 11h |
| S1 | 3.1-3.6 | 1+4+4+2+4+1 = 16h | 27h |
| S2 | 2.1-2.6 | 3+2+4+6⚠+6⚠+2 = 23h | 50h |
| S2-M7 | 7.1-7.4 | 3+3+4+2 = 12h | 62h |
| S3 | 4.1-4.3 | 1+6⚠+3 = 10h | 72h |
| S3-M8 | 8.1-8.3 | 3+4+2 = 9h | 81h |
| S4 | 5.1/5.2 | 6⚠+2 = 8h | 89h |
| S4-M9 | 9.1-9.4 | 3+4+3+2 = 12h | 101h |
| S4b+S5 | 6.1 移动 3 + 6.2 门禁 2 + 6.3 CR 2 + 6.5 切换 3 + 6.4 瘦身 4 + 6.6 终审 3 | 17h | 118h |
| S5-M10 | 10.1 台账契约 3 + 10.2 接线 4 + 10.3 门禁与终审 2 | 9h | 127h |

> 注：上表累计为**唯一排期口径（任务级实测）**；按模块归属复核：重构轨 = M1 7 + M3 16 + M2 23 + M4 10 + M5 8 + M6 17（6.1-6.6 全部）= 81h；特性轨 = M11 4 + M7 12 + M8 9 + M9 12 + M10 9 = 46h；合计 127h，与段表一致。规格（v10.5）不含工时数字，本计划为工时唯一载体，无双口径。
> 10.3 时序依赖 6.4 完成；特性终审与重构终审分别出具结论。

**总计：127h ≈ 15.9 人日（重构轨 81h + 特性轨 46h，唯一口径）**

---

## Self-Review 记录

1. **规格覆盖**：规格 4.1-4.11 全部验收标准均有对应任务步骤（11.1/1.1-1.3/3.1-3.6/2.1-2.6/7.1-7.4/4.1-4.3/8.1-8.3/5.1-5.2/9.1-9.4/6.1-6.6/10.1-10.3）；§5.4 四假设均有闭环步骤（A-1→11.1 Step1；A-2→Task 7.3 前置拍板【开工前须用户决策，未闭环不开工 7.3】；A-3→8.1 Step1；A-4→11.1 Step5 采集基线+7.2 对照）。
2. **占位符扫描**：无 TBD/TODO（6.4 豁免位恢复为具名步骤非占位）。
3. **类型一致性**：IRuntimeDataStore/IOutboxLock/IExceptionBoundary/工厂签名与规格 4.N.7 逐字一致。
4. **工时口径**：已统一为唯一口径 127h（任务级实测=模块归属复核），无双口径；规格（v10.5）不含工时，本计划为工时唯一载体。
5. **契约库贯通与终审十查（规格 5.7/10.5）**：9 组契约物化步骤已分布至任务 1.2/3.2/2.2/11.1/7.3/8.1/9.1/10.1/6.4（v0→v1）；hash 登记于物化时生成；规格附录终审十查（含第 6 契约一致/7 局部约束落位/8 回填与占位清零/9 依赖无环/10 安全覆盖）由台账与各门禁证据承载。
