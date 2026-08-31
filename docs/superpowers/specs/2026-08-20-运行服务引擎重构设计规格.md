# RunService 引擎化重构设计（A+C 方案）

- **日期**：2026-08-20（2026-08-21 补：v3 混入式集成指针）
- **状态**：已确认方向，实施计划已产出；**v3：四特性降级版已混入战役 1，主编排/五件套以《架构设计规格-运行时基座与RunService引擎化.md》为准，本规格为 A+C 引擎化子规格**
- **关联 CR**：CR-20260820-01（`.claude/change-requests/CR-20260820-01.md`）
- **关联总纲**：`docs/architecture/runservice-refactor-master-plan.md`
- **上游基线**：`docs/architecture/backend-modular-refactor-plan.md`（唯一事实基线）· 施工包 v2

## 1. 背景与目标

`RunService`（`backend/modularity/visualdev/JNPF.VisualDev/RunService.cs`，4157 行）是可视化开发运行时的上帝类：
把**模型编译、SQL 生成、数据执行、列表编排、数据视图、DB 路由**六种职责糅在一个类中，
携带唯一可变状态 `_sqlSugarClient`（SqlSugarScope），且深度绑定 SqlSugar 具体类型。

**重构目标（用户定调：最彻底最完善，以企业通用低代码平台架构最佳实践为标准）：**

1. **模型-编译-执行三层分离**——对齐成熟低代码平台（OutSystems/Mendix 类）运行时架构；
2. **不绑死 SqlSugar**——运行时引擎经 provider 中立抽象访问数据，为 PostgreSQL / 时序数据库兼容预留；
3. **接口隔离（ISP）**——`IRunService` 契约从 17 方法瘦身至 WorkFlow 真实消费的 7 方法；
4. **5 处具体类注入点全部切换**至正确的引擎类，零门面残留（含 Common.CodeGen 跨模块点，走 CR 审批）；
5. **状态单点收敛**——`_sqlSugarClient` 收敛至数据访问实现内部。

**不可变前提（继承主方案 D1/D5 + 用户修订）：**

- 保留 IDynamicApiController 机制（RunService 委托方 VisualDevModelDataService 等的 API 契约逐条不变）；
- **D1 修订**：SqlSugar 从「引擎直接依赖」降级为「可替换实现之一」，存量 SqlSugar 用法不强制迁移，
  仅运行时引擎新代码禁止直接引用 SqlSugar 类型（架构测试硬门控）；
- 路由快照硬门控：`api/visualdev` 全量零差异；
- JNPF009 基线只随迁、不上调、不新增（CC140 保持 CC140，降 CC 属二期业务改写，不在本重构内）；
- 绞杀者式纯移动，禁推倒重写；每阶段独立验收 + 节点审批门禁；
- 异常体系**复用** Oops.Oh/Oops.Bah + FriendlyException + RESTfulResult，不重建五类异常。

## 2. 实证基线（2026-08-20 采集）

| 事实 | 数值 |
|------|------|
| 文件行数 | 4157 |
| 方法总数 | 42（public 25 + private 17，含 Dispose 与构造函数） |
| JNPF009 基线条目 | 8 条（含全仓最高 CC140 `GetListQuerySql`） |
| `IRunService` 接口方法 | 17；WorkFlow 实际仅消费 7 个 |
| WorkFlow 消费面 | SaveFlowFormData×4、GetFlowFormDataDetails×11、SaveDataToDataByFId×2、GetDbLink×2、GetVisualDevModelDataConfig×2、GetCreateSqlByTemplate×1、GetUpdateSqlByTemplate×1 |
| 具体类注入点 | 5 处（含 Common.CodeGen 跨模块） |
| `_visualDevRepository.AsSugarClient()` | 49 处：Queryable×27、Utilities×12、SqlQueryable×7、CurrentConnectionConfig×3 |
| `_sqlSugarClient` 直接调用 | 8 处：AsTenant×4、SqlQueryable×4 |
| `.Ado.*` 裸 SQL / `.Result` / `.Wait()` | 0 / 0 / 0（已全异步、无 Ado 直调） |
| 直接针对 RunService 的测试 | 0 |

## 3. 架构与组件边界

```
                     ┌───────────────────────────────────┐
 WorkFlow(IRunService 7方法) ─►│     RunService（编排门面，<400 行）      │
 VisualDevModelData 等 5 注入 ─►└──┬──────────┬──────────┬─────────┘
                        │          │          │
             ┌──────────┘          │          └────────────┐
             ▼                     ▼                       ▼
    RunListQueryService     RunDataViewService        RunDataEngine
    （列表编排 ~800 行）      （数据视图 ~400 行）   （CRUD/流程表单 ~1500 行）
             │                     │                       │
             └──────────┬──────────┴───────────────────────┘
                        ▼ （接口依赖，DI 注入）
              RunSqlCompiler（编译层：纯函数、零 DB 依赖、可单测，~1600 行）
              IRuntimeDataStore（provider 中立数据访问抽象）
                        ▲
        SqlSugarRuntimeDataStore（SqlSugar 实现 —— 唯一 provider 绑定点）
```

### 3.1 组件职责

| 组件 | 职责（行业对标） | 归位方法（源文件行号） | 规模估算 |
|------|-----------------|----------------------|---------|
| `RunSqlCompiler` | **编译层**：可视化查询条件/模型 JSON → SQL + IConditionalModel。无状态、零 DB 依赖、纯函数化 | GetListQuerySql(2302,CC140)、GetInfoQuerySql(2907)、GetQueryJson(2967,CC72)、GetSuperQueryJson(3517)、GetSuperQueryInput(2189)、GetIConditionalModelListByTableName(2896)、GetVisualDevModelDataConfig(2016,CC71) | ~1600 行 |
| `IRuntimeDataStore` + `SqlSugarRuntimeDataStore` | **provider 中立运行时数据访问**：SQL 执行/查询/事务/租户切换/外部数据源路由；`_sqlSugarClient` 状态收敛于此 | （抽象层，无对应源方法；承接 49+8 处调用） | ~400 行 |
| `RunDataEngine` | **执行层**：CRUD + 唯一校验 + 乐观锁 + 流程表单数据 | Create(615)、CreateHaveTableSql(670)、GetCreateSqlByTemplate(677)、GenerateFeilds(1748,CC81)、FieldBindDefaultValue(1995,CC82)、UniqueVerify(2201)、Update(878)、BatchUpdate(937)、UpdateHaveTableSql(1026)、GetUpdateSqlByTemplate(1032)、DelHaveTableInfo(1495)、DelInteAssistant(1593)、BatchDelHaveTableData(1637)、DeleteRootFlowTasks(1727)、GetAllowDeleteFlowTaskList(2178)、SaveFlowFormData(1250)、GetFlowFormDataDetails(1316)、SaveDataToDataByFId(1362,CC90)、OptimisticLocking(3808)、DataTransferVerify(3864) | ~1500 行 |
| `RunListQueryService` | **列表编排**：分页/子表/关联表装配 | GetListResult(168,CC85)、GetRelationFormList(312)、GetHaveTableInfo(418)、GetHaveTableInfoDetails(509)、GetListChildTable(3577) | ~800 行 |
| `RunDataViewService` | **数据视图引擎** | GetDataViewResults(3873)、GetDataViewQuery(4038)、AddDataViewId(4015)、GetPageToDataTable(3998) | ~400 行 |
| `RunService`（缩壳） | **编排门面**：仅保留瘦身后 `IRunService`（7 方法）+ 对引擎的委托编排；GetDbLink/GetPrimary/GetPIdsByFlowIds/GetLocalAddress/SyncField 等基础设施方法下沉至 DataStore 或引擎 | 其余 IRunService 方法下沉为引擎类 public | <400 行 |

> 说明：`RunTreeQueryService`（外部专家包提议）经实证**不适用**——RunService 42 方法中无树形查询，树组装位于已拆完的 UsersService。

### 3.2 数据库兼容策略（核心用户约束）

- **PostgreSQL**：两层保障——
  ① 引擎层只依赖 `IRuntimeDataStore`，零 SqlSugar 类型（架构测试硬门控）；
  ② 方言差异经 `Dialect` 标识 + `ISqlDialectAdapter`（标识符引用/分页语法/类型映射）隔离，
  PG 接入仅需新增实现与配置，引擎层零改动。
- **时序数据库**：访问模式（写入为主/时间范围查询）与关系型不同，**不塞进** `IRuntimeDataStore`，
  设计为独立 provider 接口 `ITimeSeriesStore`（本期仅占位契约，不实现），避免抽象污染。
- **Queryable LINQ 链（27 处）**：涉及运行时业务表的改写为 SqlQueryable（编译后 SQL，provider 中立）；
  实体型 Queryable（VisualDevEntity 等平台元数据表）保留原仓储用法，不迁移（D1 修订边界内）。

### 3.3 结构挂靠点声明（副作用唯一漏斗，v3 硬约束）

本拆分创造的抽象同时是未来基座特性的唯一挂靠点（详见架构设计规格 §1.1）：

| 特性 | 挂靠点 | 结构前提 |
|------|--------|---------|
| 韧性管线（T-F3 及未来全量 2.3） | `IRuntimeDataStore` 装饰器层 | 引擎零绕道 |
| 事务/幂等类（F2/2.11） | `RunInTransactionAsync` | 事务语义单点 |
| 异常边界（T-F4/2.14） | 引擎统一 `Oops` 抛出面 | 不吞异常不自建层级 |
| 可查询日志（T-F1/2.5） | `ILogger` + TraceIdMiddleware 全局上下文 | 不自建日志通道 |

**硬约束**：引擎类禁止绕道第二 DB 通道。执行手段：架构测试构造白名单断言——
引擎构造参数类型仅允许 `{RunSqlCompiler, IRuntimeDataStore, ILogger, IOptions, ICacheManager}`（落 `RunEngineSqlSugarBoundaryTests` 扩展）。

## 4. 依赖流与接口契约

**依赖方向（单向、无环）：**

```
RunService(门面) → { RunListQueryService, RunDataViewService, RunDataEngine }
                        → { RunSqlCompiler, IRuntimeDataStore }
                              ← SqlSugarRuntimeDataStore（唯一 SqlSugar 绑定）
```

**`IRuntimeDataStore` 契约**（自 49+8 处真实调用面提取，只含现有能力，YAGNI）：

```csharp
public interface IRuntimeDataStore
{
    /// <summary>数据库方言标识：sqlserver / mysql / postgresql / ...</summary>
    string Dialect { get; }

    Task<object?> ExecuteScalarAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<int> ExecuteCommandAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<List<Dictionary<string, object>>> SqlQueryAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<DataTable> GetDataTableAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<bool> AnyAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task RunInTransactionAsync(Func<Task> action, string? tenantId = null);

    /// <summary>外部数据源链接路由（承接 GetDbLink + _sqlSugarClient 连接切换）</summary>
    RuntimeDbLink? ResolveDbLink(string linkId, string? tenantId = null);
}
```

约束：
- 所有 SQL **参数化**（铁律 L0，禁字符串插值）；
- 租户切换（AsTenant×4）收敛至本层统一处理；
- Utilities 调用（12 处）中 provider 相关部分收敛至本层，纯工具逻辑上移引擎；
- 实现类 `SqlSugarRuntimeDataStore` 持有原 `_sqlSugarClient` 状态与 Dispose 语义。

**引擎类间协作**：引擎依赖 `IRuntimeDataStore` + `RunSqlCompiler` 经构造函数注入；
具体生命周期（Transient/Singleton）由瘦身战役 0.1 产出的《A+C 引擎类 DI 注册约束表》裁定。

## 5. 前置：瘦身战役 0（已批准，**已执行闭环**：8 独立违规对→0，evidence `cr-20260820-01/di-validation-*.txt`）

RunService 重构前置仅做 DI 健康诊断（其余底座项入 backlog，不阻塞本重构）：

| 步骤 | 内容 | 交付物 |
|------|------|--------|
| 0.1.1 | Development 环境临时开 `ValidateScopes + ValidateOnBuild`（经配置开关，非硬编码），收集全量启动违规 | 违规清单 |
| 0.1.2 | 分级 A/B/C；**仅修复与 visualdev 模块相关的 A 类**（不铺开全仓） | 分级矩阵 + 修复 |
| 0.1.5 | 《A+C 引擎类 DI 注册约束表》：每引擎类生命周期、可注入/禁注入清单 | 设计附件（本 spec §4 生命周期输入） |

被拒绝/移出的专家包项（详见 master-plan §2）：异常体系五类重建（与 Oops/FriendlyException 冲突）、
HTTP 韧性前置（RunService 零 HTTP 调用）、日志三信号前置（OTel 已六成到位）、
RunTreeQueryService 幻影节点、CC≤15 强制降 CC（违反基线铁律）、幂等键/Keyset 分页（新功能混入重构）。

## 6. 分阶段实施（每阶段独立验收 + 节点审批）

| 阶段 | 内容 | 验收硬条件 |
|------|------|-----------|
| **S0 安全网** | `api/visualdev` 全量路由快照基线 + IRunService 契约反射测试 + Compiler 纯函数单测先行 | 基线落盘 `.claude/evidence/cr-20260820-01/` |
| **S1 编译层** | `RunSqlCompiler` 7 方法纯移动 + JNPF009 基线随迁 + **Compiler 纯函数单测（新增，重构后首个可单测面）** | 路由快照零 diff + 单测绿 + 架构测试 |
| **S2 数据访问抽象** | `IRuntimeDataStore` + `SqlSugarRuntimeDataStore` + 49+8 处调用收敛 | 架构测试（引擎层零 SqlSugar 引用）+ 活体冒烟（含外部数据源链路实测） |
| **S3 执行层** | `RunDataEngine` 拆分（CRUD/流程表单/乐观锁） | 快照零 diff + API 冒烟 CRUD 全链路 |
| **S4 列表/视图层** | `RunListQueryService` + `RunDataViewService` | 快照零 diff + 列表分页/数据视图冒烟 |
| **S5 收尾** | 门面缩壳 + `IRunService` 17→7 + 5 处注入点全切换（Common.CodeGen 走 CR） | 全回归链：快照+契约+架构+sln+CI JNPF009+test:api+活体冒烟 |

## 7. 风险与错误处理

| # | 风险 | 对策 |
|---|------|------|
| 1 | `_sqlSugarClient` 外部数据源动态连接切换行为等价性 | S2 验收项：外部数据源链路活体实测；切换语义收敛至 `ResolveDbLink` 单点 |
| 2 | Queryable LINQ 表达式树不可中立抽象 | 运行时业务表 → SqlQueryable（编译 SQL）；元数据实体 Queryable 保留原状（D1 修订边界） |
| 3 | JNPF009 8 条基线随迁 | 只随迁不上调不新增；CC140 保持原值（降 CC 为二期） |
| 4 | 跨模块注入点切换（Common.CodeGen） | 单独 CR 审批；切换与拆分分阶段，独立可回滚 |
| 5 | 零测试存量 | S0 契约反射测试 + S1 起 Compiler 单测先行，覆盖率只增不减 |
| 6 | 回滚 | 每阶段绞杀者纯移动，git 独立可 revert |
| 7 | S2 Queryable→SqlQueryable 改写是行为敏感点（A+C 抽象化的必然代价，非纯移动） | 逐处改写前后 SQL 等价比对（SqlSugar ToSql 抓取）+ 每处配活体冒烟；无法等价改写处保留原用法并登记豁免 |

异常处理：引擎内部统一 `Oops.Bah()`（业务）/ `Oops.Oh()`（系统），
携带结构化上下文（FormId/FlowId/TenantId 经日志结构化字段），不引入新异常类型层级。

## 8. 验收门禁总表

| 门禁 | 断言 | 阶段 |
|------|------|------|
| 路由契约 | `api/visualdev` 快照逐条零差异 | 每阶段 |
| 架构合规 | 引擎类零直接引用 SqlSugar 类型（元数据实体仓储除外） | S2 起 |
| 复杂度 | JNPF009 基线只随迁不上调不新增 | 每阶段 |
| 单测 | RunSqlCompiler 覆盖全部编译路径（JOIN/过滤/子查询/分页/超级查询） | S1 |
| 契约 | IRunService 反射契约测试；瘦身后 WorkFlow 消费 7 方法签名不变 | S0/S5 |
| 全回归 | sln Debug/Release + CI JNPF009 + test:api + 架构测试 + 活体冒烟 | S5 |
