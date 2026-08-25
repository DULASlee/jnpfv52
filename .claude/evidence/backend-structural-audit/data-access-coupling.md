# Backend Structural Audit — S2 Data Access Coupling Inventory

**日期**：2026-08-25 ｜ 方法：全仓 grep 实测（modularity 业务面）

## 1. ORM 耦合面（S2 核心影响面）

| 指标 | 数量 | 说明 |
|------|-----:|------|
| 文件 `using SqlSugar;` | **403** | 占 modularity 1549 业务文件 26% |
| 文件调用 `AsSugarClient()` | **84** | 会话获取点 |
| `ITenantFilter` 使用文件 | 12 | 挂靠点集中（见 tenant-permission-map.md） |
| `.GetCondition` 外部消费点 | 1 | `RunService.cs`（VisualDev 列表数据权限） |

## 2. S2 风险问答（审计规格 §六）

| # | 问题 | 结论（证据） |
|---|------|-------------|
| 1 | 哪些业务 Service 直接依赖 ORM？ | **403 文件直接 using SqlSugar**（含各模块 Service/Manager/Helper），S2 抽象需分层适配而非单点替换 |
| 2 | Domain/Application 代码依赖 Infrastructure？ | ARCH01 测试守护框架边界（92/92 绿）；Common.Core→模块 Entitys 反向依赖已知（历史违规，D1 战役外） |
| 3 | 哪些条件模型由业务层构造？ | `UserManager.GetCondition/GetConditionAsync/GetDataConditionAsync`（CC42/60/60）+ `GetConditionalModel`（23-case）——数据权限条件集中构造点 |
| 4 | 数据权限逻辑位于数据访问层之外？ | 是：`GetCondition*` 在 Common.Core（业务层），消费于 RunService/OrderService——S2 抽象必须保留此分层语义 |
| 5 | 租户过滤依赖隐式上下文？ | 是：ITenantFilter 经 AsSugarClient 会话隐式生效（12 文件挂靠），无显式参数传递 |
| 6 | Repository 承担业务逻辑？ | 抽查：IRepository 薄包装为主（D1 A0 已证 GetCondition 链路）；未发现反向（Repository 混入业务）新证据——登记为观察项 |
| 7 | 查询逻辑跨层？ | `RunSqlCompiler`（CC113 GetListQuerySql/CC72 GetQueryJson）——编译层直接构造 SqlSugar 查询，S2 核心目标对象 |
| 8 | 接口是 ORM 薄包装？ | 是：IRepository 系接口与 SqlSugar 直接对应（AsSugarClient 模式），抽象需重设计契约 |

## 3. SQL 安全健康度（旁证）

- `$"SELECT` 插值：**全 modularity 仅 1 处**（`JNPF.ZxDev/ConfigController.cs`，zxdev 非核心）——参数化纪律良好（hook L0 守护生效），S2 抽象不得倒退

## 4. 关键链路（S2 前必须保持语义）

```text
UserManager.GetCondition/GetConditionAsync（条件模型构造，枚举数值契约）
  → ToJsonString → Utilities.JsonToConditionalModels（匿名对象反序列化契约）
    → List<IConditionalModel> → RunService/OrderService 列表查询数据权限过滤
      → SqlSugar 条件注入（ITenantFilter 租户过滤）
```
