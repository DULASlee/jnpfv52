# TenantContext 详细设计

> 对应 ADR：ADR-003, ADR-004, ADR-013
> 源码：`backend/infrastructure/JNPF.Extras.DatabaseAccessor.SqlSugar/TenantContext/`

---

## 1. AsyncLocal 传播机制

```
                           HTTP Request
                                │
                    ┌───────────▼───────────┐
                    │  TenantMiddleware      │
                    │  解析 TenantId         │
                    │  TenantContext.Set(tid) │
                    └───────────┬───────────┘
                                │
              ┌─────────────────┼─────────────────┐
              ▼                 ▼                 ▼
        Service Layer    EventBus Filter    Schedule Job
              │                 │                 │
              └─────────────────┼─────────────────┘
                                │
                    AsyncLocal<TenantInfo>
                    (跨 async/await 传播)
```

**核心原理：** `AsyncLocal<T>` 随 `ExecutionContext` 在异步调用链中自动流动。每次 `await` 后恢复上下文时，`TenantId` 保持一致。

**实现：**
```csharp
internal class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<TenantInfo> _current = new();

    public string TenantId
    {
        get => _current.Value?.TenantId ?? ResolveFallback();
        set => _current.Value = new TenantInfo { TenantId = value };
    }
}
```

---

## 2. Current 静态访问点 (ADR-003)

```csharp
TenantContext.Current  // 全局静态属性
```

**设计理由：** DataExecuting 委托是 SqlSugar 回调，不经过 DI 容器。它需要通过静态访问点获取当前租户 ID 以自动填充 `TenantId` 字段。

**使用场景：**
- `ConfigureGlobalDataExecuting` 委托中填充插入实体的 `TenantId`
- 非 DI 上下文中需要获取当前租户时（不推荐新代码使用）

---

## 3. 三种入口点

### 3.1 HTTP 入口

```
HTTP Request
  │
  ▼
TenantMiddleware (或 AuthenticationMiddleware)
  │
  ├── JWT claim: tenant_id → TenantContext.TenantId
  ├── Header: X-Tenant-Id → TenantContext.TenantId
  ├── QueryString: ?tenantId=xxx → TenantContext.TenantId
  └── 无 → FallbackTenantResolver
```

### 3.2 EventBus 入口

```
EventBus Message
  │
  ▼
TenantPropagationFilter (EventBus 过滤器)
  │
  └── 从消息 Header 提取 tenant_id → TenantContext.TenantId
```

**设计原因 (ADR-013)：** 事件消费者在独立线程/进程中执行，必须从消息元数据中恢复租户上下文。否则所有事件操作将使用默认租户 → 数据泄露。

### 3.3 定时任务入口

```
Schedule Job Execute
  │
  ▼
FallbackTenantResolver (四级降级)
  │
  └── 任务配置中的 TenantId 或默认值
```

---

## 4. FallbackTenantResolver 四级降级 (ADR-004)

```
1. JWT claim (tenant_id)
     │
     ├── 未找到？
     ▼
2. HTTP Header (X-Tenant-Id)
     │
     ├── 未找到？
     ▼
3. QueryString (?tenantId)
     │
     ├── 未找到？
     ▼
4. 默认租户 (KeyVariable.DefaultTenantId)
```

**匿名端点降级策略 (ADR-004)：** 对于白名单路径（如 `/api/oauth/login`），跳过 JWT 验证但保留 Header/QueryString 租户解析。这允许在登录前通过 URL 参数指定租户。

---

## 5. 线程池污染防护

**问题：** `AsyncLocal<T>` 在线程池线程复用时可能保留上一次请求的 `TenantId`。

**防护：**
- HTTP 入口：请求开始时显式设置 `TenantContext.TenantId`（覆盖旧值）
- HTTP 出口：请求结束时调用 `TenantContext.Reset()`（通过中间件 finally 块）
- EventBus：每条消息处理前设置、处理后重置
- 定时任务：每个 Job 执行前设置、执行后重置

---

## 6. 数据隔离实现分层

| 层 | 机制 | 负责组件 |
|---|---|---|
| QueryFilter | `db.QueryFilter.AddTableFilter<ITenantFilter>()` | SqlSugarConfigureExtensions |
| Updateable/Deleteable | Safe* 方法显式 WHERE TenantId | SqlSugarRepository (ADR-012) |
| Insert 自动填充 | DataExecuting 统一委托 | AppStartup |
| 子查询/Join | 手动添加 `.Where(x => x.TenantId == tenantId)` | 开发者责任 |
| 原生 SQL | 手动添加 TenantId 条件 | 开发者责任 |

---

## 7. 配置

```json
// appsettings.json
{
  "MultiTenancy": {
    "Enabled": true,
    "DefaultTenantId": "default",
    "TenantResolution": {
      "HeaderName": "X-Tenant-Id",
      "QueryStringKey": "tenantId",
      "JwtClaimType": "tenant_id"
    }
  }
}
```
