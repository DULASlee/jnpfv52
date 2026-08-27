# ORM 框架行为速查表（P2 — v5.0）

> 本文件为 generic-class-refactor-expert v5.0 的 reference 文件。
> 排查 D3（Transaction）、D6（Concurrency）、D8（DI/Lifecycle）维度时，当遇到 ORM 框架操作，对照本表判断。

## 使用方法

1. 识别当前类使用的 ORM（看 `using` 引用和 DI 注入类型）
2. 找到对应框架的速查表
3. 逐操作对照，判断"当前写法在该 ORM 下是否安全"
4. 在 Finding 的 `ORM对照` 字段中引用本表的具体行

## SqlSugar（JNPF 主力 ORM）

### 事务行为（排查 D3 时使用）

| 行号 | 代码写法 | SqlSugar 实际行为 | 安全判断 |
|---|---|---|---|
| D3-S1 | 连续调用 `Insertable/Updateable/Deleteable` 无外层包裹 | 每个操作独立提交，中途失败无法回滚 | ❌ 不安全 |
| D3-S2 | 方法上加 `[UnitOfWork]` | 包裹该方法内所有 SqlSugar 操作为一个事务 | ✅ 安全 |
| D3-S3 | 方法内手动 `BeginTransaction()...Commit()` | 显式事务 | ✅ 安全 |
| D3-S4 | `[UnitOfWork]` + 循环内有文件/HTTP 等非 DB 操作 | 事务仅包裹 DB 操作，文件/HTTP 不在事务内 | ⚠️ 部分安全（跨资源不原子） |

### 并发行为（排查 D6 时使用）

| 行号 | 代码写法 | SqlSugar 实际行为 | 安全判断 |
|---|---|---|---|
| D6-S1 | `Updateable(entity)` 无乐观锁配置 | 直接覆盖，最后一个写入者胜出 | ❌ 不安全（竞态） |
| D6-S2 | `Updateable(entity)` + 实体加 `[SugarColumn(IsEnableUpdateVersionValidation=true)]` | 乐观锁，版本号不匹配时抛异常 | ✅ 安全 |
| D6-S3 | `static` 字段在请求中赋值 | 跨请求共享，后请求覆盖前请求 | ❌ 不安全（竞态） |

### DI/Scope 行为（排查 D8 时使用）

| 行号 | 注册方式 | SqlSugar 实际行为 | 安全判断 |
|---|---|---|---|
| D8-S1 | `services.AddSingleton<ISqlSugarClient>()` | 全局单例，内部自动管理每请求 Scope | ✅ 安全（SqlSugar 推荐方式） |
| D8-S2 | `services.AddScoped<ISqlSugarClient>()` | 每请求一个实例 | ✅ 安全 |
| D8-S3 | Controller 中 `private static SqlSugarScope?` 按请求赋值 | static 跨请求共享，竞态 | ❌ 不安全 |
| D8-S4 | `using var scope = _serviceScopeFactory.CreateScope()` | 手动创建 Scope，using 确保释放 | ✅ 安全 |

## EF Core（备用，如项目使用）

### 事务行为

| 行号 | 场景 | EF Core 默认行为 | 注意事项 |
|---|---|---|---|
| D3-E1 | `SaveChanges()` | 默认开启事务 | 跨 Context 调用不在同一事务 |
| D3-E2 | 显式 `BeginTransaction()` | 手动事务 | 需手动 Commit/Rollback |

### 并发行为

| 行号 | 场景 | EF Core 默认行为 | 注意事项 |
|---|---|---|---|
| D6-E1 | 默认 | 无乐观锁 | 需 `[ConcurrencyCheck]` / `[Timestamp]` |

### 性能行为

| 行号 | 场景 | EF Core 默认行为 | 注意事项 |
|---|---|---|---|
| D5-E1 | 查询 | 默认启用 Change Tracker | 大批量操作考虑 `AsNoTracking()` |
| D5-E2 | 导航属性 | 默认不启用 Lazy Loading | 需 `.Include()` 显式加载 |

## Dapper（备用，如项目使用）

| 行号 | 场景 | Dapper 默认行为 | 注意事项 |
|---|---|---|---|
| D3-D1 | 事务 | 无内置事务 | 需手动 `IDbTransaction` |
| D6-D1 | 变更追踪 | 无 | 所有 Update 为全字段覆盖 |
| D4-D1 | 参数化 | 支持 `@param` | `string.Format` 绕过参数化则不安全 |

## 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v5.0 | 2026-08-28 | 初始版本，覆盖 SqlSugar 完整 + EF Core/Dapper 备用 |
