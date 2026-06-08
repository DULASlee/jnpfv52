# Outbox 事件管道详细设计

> 对应 ADR：ADR-008, ADR-010, ADR-011, ADR-015
> 源码：`EventBusModule.cs`, `EventOutboxMessage`, `ProcessedEvent`, `OutboxDispatcher`

---

## 1. 管道流程图

```
┌──────────────────┐
│   业务代码        │
│   await repo     │
│     .SafeUpdate* │
│   await eventBus │
│     .Publish(e)  │
└──────┬───────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│         EventBusModule.PublishAsync       │
│                                          │
│  ┌────────────┐   ┌───────────────────┐ │
│  │ 业务实体    │   │ EventOutboxMessage│ │
│  │ SaveChanges │   │   .InsertAsync()  │ │
│  └─────┬──────┘   └────────┬──────────┘ │
│        │                   │            │
│        └────────┬──────────┘            │
│                 ▼                        │
│         DB 事务提交 (原子性)              │
└────────────────┬─────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────┐
│            OutboxDispatcher              │
│                                          │
│  触发方式：                               │
│  ├── WriteAsync → Channel.Writer 实时唤醒│
│  └── BackgroundService 30s 兜底轮询     │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  SELECT * FROM EventOutboxMessage  │ │
│  │  WHERE Status = Pending            │ │
│  │  AND CreatedAt < NOW()             │ │
│  │  ORDER BY CreatedAt                │ │
│  │  (UPDLOCK READPAST 行锁)           │ │
│  └──────────────┬─────────────────────┘ │
│                 │                        │
│                 ▼                        │
│  ┌────────────────────────────────────┐ │
│  │         Polly 重试管道              │ │
│  │  ├── 指数退避 (2^n seconds)         │ │
│  │  ├── 熔断 (连续失败 N 次 → 暂停)    │ │
│  │  └── 死信 (MaxRetryCount → Dead)   │ │
│  └──────────────┬─────────────────────┘ │
│                 │                        │
│                 ▼                        │
│  ┌────────────────────────────────────┐ │
│  │     ProcessedEvent 幂等检查         │ │
│  │  PK: (EventId, HandlerName)        │ │
│  │  IF NOT EXISTS → 执行业务 → INSERT  │ │
│  └────────────────────────────────────┘ │
└──────────────────────────────────────────┘
```

---

## 2. EventOutboxMessage 表结构

| 列名 | 类型 | 说明 |
|---|---|---|
| `F_ID` | uniqueidentifier | 主键 (GUID) |
| `F_EVENT_NAME` | nvarchar | 事件名 (如 `UserCreated`) |
| `F_EVENT_PAYLOAD` | text | 事件负载 (JSON) |
| `F_CREATED_AT` | datetime | 创建时间 |
| `F_PROCESSED_AT` | datetime | 处理时间 (可空) |
| `F_RETRY_COUNT` | int | 当前重试次数 |
| `F_MAX_RETRY_COUNT` | int | 最大重试次数 |
| `F_STATUS` | int | 0=Pending, 1=Processing, 2=Completed, 3=DeadLetter |
| `F_ERROR` | text | 最后一次错误信息 (可空) |

**索引：**
- `(F_STATUS, F_CREATED_AT)` — 分发器轮询使用
- `(F_EVENT_NAME)` — 按事件名查询

---

## 3. 数据库行锁策略 (ADR-008)

**多实例安全：**

多个服务实例同时轮询 Outbox 时，必须防止同一消息被重复处理。

```sql
-- SQL Server
SELECT TOP 100 * 
FROM SYS_EVENT_OUTBOX_MESSAGE WITH (UPDLOCK, READPAST)
WHERE F_STATUS = 0
ORDER BY F_CREATED_AT
```

- `UPDLOCK`: 对选中行加更新锁，阻止其他事务获取
- `READPAST`: 跳过已锁定的行，而非等待

**事务包裹：**
```
BEGIN TRANSACTION
  SELECT ... WITH (UPDLOCK, READPAST)
  UPDATE SET F_STATUS = 1 (Processing)
COMMIT TRANSACTION
-- 执行业务逻辑（事务外）
UPDATE SET F_STATUS = 2 (Completed)
```

---

## 4. Polly 重试与熔断

```
指数退避策略:
  第 1 次失败 → 等待 2s
  第 2 次失败 → 等待 4s
  第 3 次失败 → 等待 8s
  ...
  第 N 次 (最大重试) → 标记 DeadLetter

熔断策略:
  连续 M 次失败 → 暂停处理 30s
  半开状态 → 试探处理 1 条
  成功 → 恢复正常
  失败 → 继续熔断
```

---

## 5. 幂等处理

**ProcessedEvent 表：**

| 列 | 类型 | 说明 |
|---|---|---|
| `EventId` | uniqueidentifier | 关联 EventOutboxMessage.F_ID |
| `HandlerName` | nvarchar | 处理器全名 |
| `ProcessedAt` | datetime | 处理时间 |

**复合主键：** `(EventId, HandlerName)`

**幂等逻辑：**
```csharp
// 处理前检查
if (await db.Queryable<ProcessedEvent>()
    .AnyAsync(p => p.EventId == message.Id && p.HandlerName == handlerName))
{
    // 已处理，跳过
    return;
}

// 执行业务逻辑
await handler.Handle(message.Payload);

// 记录处理
await db.Insertable(new ProcessedEvent 
{ 
    EventId = message.Id, 
    HandlerName = handlerName, 
    ProcessedAt = DateTime.UtcNow 
}).ExecuteCommandAsync();
```

---

## 6. 优雅停机 (ADR-015)

```
Host 请求停止
  │
  ▼
  ├── CancellationToken 触发
  ├── Channel.Writer.Complete() — 拒绝新消息
  ├── Channel.Reader.ReadAllAsync() — 排空剩余消息
  ├── 最后一批已排空，标记 Pending → 下次启动处理
  └── 退出
```

**超时保护：** `ShutdownTimeout = 30s`，超过则强制退出。

---

## 7. LogEventSubscriber 批量缓冲

事件日志订阅器接收所有事件，按以下策略写入日志：

- 内存缓冲队列 (ConcurrentQueue)
- 批量写入 (每 100 条或每 5 秒)
- 落盘超时自动 flush

---

## 8. BypassOutbox 权限控制

`[BypassOutbox]` 属性允许跳过 Outbox 管道，但需满足：

1. 方法注释中明确跳过理由
2. 仅限系统心跳、健康检查等低风险场景
3. JNPF004 分析器强制执行注释要求

```csharp
// 系统心跳事件 — 高频低价值，跳过 Outbox 减少数据库压力
[BypassOutbox]
public async Task PublishHeartbeat()
{
    await eventBus.Publish(new HeartbeatEvent());
}
```
