# ScheduleService 深入分析 — F-L1 / F-P1 / F-A1

> **状态**：只读分析，不改生产代码  
> **目标类**：`JNPF.Systems.System.ScheduleService`  
> **文件**：`backend/modularity/system/JNPF.Systems/System/ScheduleService.cs`  
> **大小**：1469 行 / 62.7 KB

---

## 类级结构模型

### 系统角色

```
ScheduleService
├── 上游调用者：前端 API（HTTP 请求）
├── 下游依赖：
│   ├── ISqlSugarRepository<ScheduleEntity>（数据库）
│   ├── IUserManager（用户上下文）
│   ├── ITaskQueue（异步任务队列）
│   ├── ICacheManager（缓存）
│   ├── IMessageManager（消息通知）
│   └── IServiceScopeFactory（DI 作用域）
├── 生命周期：ITransient（每请求新建）
├── 数据访问：直接通过 _repository 和 _serviceScopeFactory
├── 缓存：通过 _cacheManager 管理日程推送缓存
├── 异步/后台任务：通过 _taskQueue 延迟执行推送任务
├── 外部副作用：发送消息通知、HTTP 调用本地 API
├── 事务边界：无显式事务（依赖 SqlSugar 默认行为）
├── 并发边界：无显式并发控制
└── 关键热路径：GetList、GetAppList、Create、Update
```

### 职责拆分（行为族）

```
ScheduleService
├── Scheduling（核心）
│   ├── 查询日程（GetList, GetAppList, GetInfo, GetDetalInfo）
│   ├── 创建日程（Create）
│   ├── 修改日程（Update）
│   └── 删除日程（Delete）
├── Repetition（重复逻辑）
│   ├── 每天/每周/每月/每年重复
│   └── 复杂的时间计算和实体生成
├── Reminder（提醒）
│   ├── 计算推送时间（GetPushTime）
│   ├── 添加推送任务队列（AddPushTaskQueue）
│   └── 获取当天推送列表（GetCalendarDayPushList）
├── Notification（通知）
│   └── 发送日程消息（SendScheduleMsg）
├── Cache（缓存）
│   └── 管理日程推送缓存
├── User（用户管理）
│   └── 添加日程参与人（AddScheduleUser）
└── Logging（日志）
    └── 添加日程日志（AddScheduleLog）
```

---

## F-L1：DI 作用域泄漏分析

### 代码位置

1. **GetCalendarDayPushList**（918-937 行）
2. **AddPushTaskQueue**（948-999 行）

### Ownership 分析

#### GetCalendarDayPushList（923-936 行）

```csharp
using var scoped = _serviceScopeFactory.CreateScope();  // ← 创建 scope
var sqlSugarClient = scoped.ServiceProvider.GetRequiredService<ISqlSugarClient>();
var dataBaseManager = scoped.ServiceProvider.GetService<IDataBaseManager>();

if (sqlSugarClient.CurrentConnectionConfig.ConfigId.ToString() != tenantId)
{
    sqlSugarClient = dataBaseManager.GetTenantSqlSugarClient(tenantId);
}

var entityList = await sqlSugarClient.Queryable<ScheduleEntity>()
    .Where(it => it.DeleteMark == null && it.PushTime >= DateTime.Now && it.PushTime < endTime && it.ReminderTime != -2)
    .ToListAsync();

return entityList;
// ← using var 自动 Dispose scope
```

**判定**：✅ **正确**
- 创建方：当前方法
- 拥有方：当前方法（局部变量 `scoped`）
- 释放方：当前方法（`using var` 自动释放）
- 异步边界：无（同步方法内 await）
- 生命周期：方法结束即释放

#### AddPushTaskQueue（954-998 行）

```csharp
await _taskQueue.EnqueueAsync(
    async (provider, token) =>
    {
        using var scoped = provider.CreateScope();  // ← 创建 scope
        var sqlSugarClient = scoped.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var dataBaseManager = scoped.ServiceProvider.GetService<IDataBaseManager>();

        var server = scoped.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;

        if (sqlSugarClient.CurrentConnectionConfig.ConfigId.ToString() != tenantId)
        {
            sqlSugarClient = dataBaseManager.GetTenantSqlSugarClient(tenantId);
        }
        
        // ... 查询、缓存检查、HTTP 调用 ...
        
        await ValueTask.CompletedTask;
    }, (int)ts.TotalMilliseconds);  // ← 延迟执行
// ← using var 自动 Dispose scope
```

**判定**：✅ **正确**
- 创建方：任务队列回调
- 拥有方：任务队列回调（局部变量 `scoped`）
- 释放方：任务队列回调（`using var` 自动释放）
- 异步边界：有（延迟执行），但 scope 在回调内创建和释放
- 生命周期：回调结束即释放

### 结论

**F-L1 不成立**。两处 DI scope 使用都正确：
- 都使用 `using var` 确保释放
- 都在创建方法内完成生命周期
- 没有跨方法/跨异步边界的 ownership 问题

**Decision**：**STOP** — 无问题

---

## F-P1：N+1 查询分析

### 代码位置

1. **Delete 方法 case 2**（809-811 行）
2. **Delete 方法 case 3**（841-843 行）
3. **Delete 方法 case 3**（852-854 行）

### 证据分析

#### Delete case 2（809-811 行）

```csharp
foreach (var item in dataList)
{
    var dataUser = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
        .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id))
        .ToListAsync();  // ← 循环内查询
    scheduleLogList.Add(AddScheduleLog(item, string.Join(",", dataUser), "3"));
}
```

**问题**：
- 查询次数 = `dataList.Count`
- 每次查询 `ScheduleUserEntity` 表
- 如果 `dataList` 有 100 个日程，则执行 100 次查询

**N 的规模**：
- `dataList` 来自 800-802 行：同组 ID 下的所有日程
- 重复日程可能产生大量数据（每天/每周/每月/每年重复）
- 理论上 N 可以很大（如每天重复一年 = 365 个日程）

**是否存在缓存**：❌ 无缓存

**ORM 是否批处理**：❌ 未使用 `Includes` 或批量查询

**是否存在业务上必须逐项查询的情况**：❌ 可以一次性查询所有 `ScheduleUserEntity`

**SQL 是否真的落库**：✅ 是（每次循环都执行 SQL）

**实际调用路径是不是热路径**：⚠️ 中等（删除操作不频繁，但重复日程场景下 N 可能较大）

**修复后的查询形态是否改变语义**：❌ 不改变（可以一次性查询后分组）

#### Delete case 3（841-843 行）

```csharp
foreach (var item in allDataList)
{
    var userList = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
        .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id))
        .ToListAsync();  // ← 循环内查询
    scheduleList.Add(item);
    scheduleLogList.Add(AddScheduleLog(item, string.Join(",", userList), "3"));
}
```

**问题**：同上，循环内查询 `ScheduleUserEntity`

#### Delete case 3（852-854 行）

```csharp
foreach (var item in allDataList)
{
    var user = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
        .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id) && it.ToUserId.Equals(_userManager.UserId))
        .FirstAsync();  // ← 循环内查询
    if (user.IsNotEmptyOrNull())
    {
        scheduleParticipantsList.Add(user);
        scheduleLogList.Add(AddScheduleLog(item, user.ToUserId, "4"));
    }
}
```

**问题**：同上，循环内查询 `ScheduleUserEntity`

### 修复方案

**方案 1：批量查询 + 分组**

```csharp
// 一次性查询所有 ScheduleUserEntity
var allScheduleUsers = await _repository.AsSugarClient()
    .Queryable<ScheduleUserEntity>()
    .Where(it => it.DeleteMark == null && allDataList.Select(s => s.Id).Contains(it.ScheduleId))
    .ToListAsync();

// 按 ScheduleId 分组
var userGroups = allScheduleUsers.GroupBy(u => u.ScheduleId)
    .ToDictionary(g => g.Key, g => g.ToList());

foreach (var item in allDataList)
{
    var userList = userGroups.ContainsKey(item.Id) ? userGroups[item.Id] : new List<ScheduleUserEntity>();
    scheduleList.Add(item);
    scheduleLogList.Add(AddScheduleLog(item, string.Join(",", userList), "3"));
}
```

**收益**：
- 查询次数从 N 次降为 1 次
- 减少数据库往返
- 改善性能（特别是重复日程场景）

**风险**：
- 如果 `allDataList` 很大，一次性查询可能占用较多内存
- 但相比 N+1 问题，这是可接受的权衡

### 结论

**F-P1 成立**。存在明确的 N+1 查询问题：
- 查询次数随 N 增长
- N 的实际规模可能较大（重复日程）
- 无缓存、无批处理
- 可以优化为批量查询 + 分组

**Decision**：**GO** — 满足 6 要素准入

---

## F-A1：职责过载分析

### 职责拆分

按行为族拆分后：

| 职责 | 方法数 | 依赖 | 变化频率 | 生命周期 |
|------|--------|------|----------|----------|
| Scheduling（CRUD） | 7 | Repository, UserManager | 高 | 请求级 |
| Repetition（重复逻辑） | 4（私有） | 无 | 中 | 请求级 |
| Reminder（提醒） | 3 | TaskQueue, CacheManager | 中 | 请求级 |
| Notification（通知） | 1 | MessageManager, UserManager | 中 | 请求级 |
| Cache（缓存） | 分散在各方法 | CacheManager | 低 | 请求级 |
| User（用户管理） | 1（私有） | Repository | 低 | 请求级 |
| Logging（日志） | 1（私有） | Repository | 低 | 请求级 |

### 独立变化轴分析

**是否真的存在独立变化？**

| 职责 | 独立变化？ | 理由 |
|------|------------|------|
| Scheduling | ✅ 是 | CRUD 逻辑独立，可能引入新的查询方式 |
| Repetition | ✅ 是 | 重复逻辑复杂，可能引入新的重复模式 |
| Reminder | ✅ 是 | 提醒逻辑独立，可能引入新的提醒方式 |
| Notification | ✅ 是 | 通知逻辑独立，可能引入新的通知渠道 |
| Cache | ❌ 否 | 缓存逻辑分散，不独立 |
| User | ❌ 否 | 用户管理逻辑简单，不独立 |
| Logging | ❌ 否 | 日志逻辑简单，不独立 |

**独立依赖？**

| 职责 | 独立依赖？ | 依赖 |
|------|------------|------|
| Scheduling | ✅ 是 | Repository, UserManager |
| Repetition | ❌ 否 | 无独立依赖 |
| Reminder | ✅ 是 | TaskQueue, CacheManager |
| Notification | ✅ 是 | MessageManager, UserManager |

**独立生命周期？**

所有职责都是请求级（ITransient），无独立生命周期。

### 结论

**F-A1 部分成立**。存在 4 个独立变化轴：
1. **Scheduling**（CRUD）
2. **Repetition**（重复逻辑）
3. **Reminder**（提醒）
4. **Notification**（通知）

但：
- 所有职责都是请求级生命周期
- 职责之间耦合度不高
- 拆分收益有限（不会显著改善可测试性或可维护性）

**Decision**：**STOP** — 架构 Finding 的准入门槛必须高于普通局部代码问题。当前类虽然大，但职责拆分收益有限，不构成必须重构的证据。

---

## 决策矩阵

| Finding | 技术性质 | 证据强度 | 改造半径 | 当前决定 |
|---------|----------|----------|----------|----------|
| F-L1 | Lifecycle | ✅ 已证明无问题 | — | **STOP**（无问题） |
| F-P1 | Performance | ✅ 已证明存在 N+1 | 单类单点 | **GO**（满足准入） |
| F-A1 | Architecture | ⚠️ 部分成立 | 跨类 | **STOP**（收益有限） |

---

## 下一步

### 推荐：进入 F-P1 准入门控阶段

**理由**：
1. F-P1 是唯一满足 6 要素准入的 Finding
2. 改造半径为单类单点（Delete 方法内的 3 处循环查询）
3. 修复收益明确（N 次查询降为 1 次）
4. 验证成本低（build + 删除操作回归）

**下一步行动**：
1. 提交 F-P1 Gate Pack（6 要素 + 10 要素 + 验证方案）
2. 等待批准后进入实现阶段
3. 单点修复 Delete 方法内的 3 处 N+1 查询

---

> **本包证明**：ScheduleService 深入分析完成，F-L1 无问题（DI scope 使用正确），F-P1 成立（N+1 查询），F-A1 部分成立但收益有限。Decision = **GO for F-P1**。
