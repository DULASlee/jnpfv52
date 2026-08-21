# A+C 引擎类 DI 注册约束表（战役 0.1.5 交付物）

- **日期**：2026-08-20
- **依据**：ValidateScopes+ValidateOnBuild 全量实测（53 异常 → 修复后 0，evidence：`.claude/evidence/cr-20260820-01/di-validation-*.txt`）
- **反哺**：`docs/superpowers/specs/2026-08-20-runservice-engine-refactor-design.md` §4 生命周期裁定

## 1. 战役 0.1 实测结论

| 项 | 结果 |
|----|------|
| 初始独立违规对 | 8（全部同模式：Singleton 消费 Scoped） |
| 波及服务描述符 | 50（含 VisualDevModelDataService、Users* 三兄弟、FlowTask* 等） |
| 根因 | ① CacheManager 误标 IScoped（零状态纯转发）② DataBaseManager(ITransient) 构造注入 Scoped IUserManager，经 TenantManager/IMHandler 传导全图 ③ 订阅者死注入/Scoped JobManager 直注 |
| 修复 | CacheManager→ISingleton；LogEventSubscriber 死注入移除；DataBaseManager/IntegreateEventSubscriber 改 IServiceScopeFactory 按需解析 |
| 终态 | **ValidateScopes+ValidateOnBuild 全绿启动，0 违规，登录+CurrentUser 活体冒烟 200** |

## 2. 引擎类生命周期裁定

| 组件 | 生命周期 | 可注入 | 禁注入 | 依据 |
|------|---------|--------|--------|------|
| `RunSqlCompiler` | **Singleton** | ILogger、IOptions | 任何 DB/请求上下文类型 | 纯函数、零状态、零 DB 依赖（可单测面） |
| `SqlSugarRuntimeDataStore`（IRuntimeDataStore 实现） | **Transient** | ISqlSugarClient（已 Singleton）、IOptions、ILogger | Scoped 请求上下文服务 | 承接原 `_sqlSugarClient` 状态与 Dispose，对齐原 RunService Transient 语义 |
| `RunDataEngine` | **Transient** | RunSqlCompiler、IRuntimeDataStore、ILogger | SqlSugar 类型、Scoped 服务 | 对齐原 RunService |
| `RunListQueryService` | **Transient** | 同上 | 同上 | 同上 |
| `RunDataViewService` | **Transient** | 同上 | 同上 | 同上 |
| `RunService`（缩壳门面） | **Transient**（维持 IRunService 现注册） | 四引擎 | SqlSugar 类型 | 行为等价 |

## 3. 硬规则（引擎类 DI 五律）

1. **引擎构造禁止注入 SqlSugar 类型**——唯一绑定点是 `SqlSugarRuntimeDataStore`（架构测试硬门控，S2 起）。
2. **禁止构造注入 Scoped 服务**——请求上下文（tenantId/userId）一律经**方法参数**传递（沿用现存 `string? tenantId` 参数模式）。
3. **Singleton/长生命周期组件需 Scoped 服务时**：`IServiceScopeFactory` 按需解析 + scope 内完成读取，值不带出 scope（先例：`DbJobPersistence`、本次修复后的 `DataBaseManager.ResolveUserManagerValue`）。
4. **CacheManager 已 Singleton**——引擎可直接注入；`UserManager`/`JobManager` 保持 Scoped 不可提升（依赖请求上下文/IServiceProvider）。
5. **诊断开关常备**：`JNPF_VALIDATE_DI=1`（Program.cs 组合根，默认关闭）；0.1.3 CI 门控候选——Development 定期跑诊断模式采集清单。

## 4. 遗留登记（不阻塞战役 1）

- 0.1.3 CI Scope 校验门控接入（待战役 1 收尾一并评估形式：启动诊断 job vs 反射式 DI 图测试）
- 全仓其余 Singleton-Scoped 隐患经本次修复已清零；新增违规由 ValidateOnBuild 诊断开关兜底发现
