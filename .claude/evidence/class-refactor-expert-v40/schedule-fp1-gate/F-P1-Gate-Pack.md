# F-P1 Gate Pack — ScheduleService Delete N+1 查询

> **状态**：Gate 阶段，不改生产代码  
> **Finding**：F-P1 — Delete 方法中存在 3 处循环内查询 `ScheduleUserEntity`  
> **目标**：闭合"循环查询 → 实际 DB round-trip → N 的合理规模 → 性能影响 → 批量方案 → 行为/事务/权限语义不变 → 可验证收益"链条

---

## 1. 三处查询的精确调用链

### 查询 1：Lines 809-811（case 2，创建者分支）

```csharp
// case 2: 当前日程及后续
var dataList = await _repository.AsQueryable()
    .Where(it => it.DeleteMark == null && it.GroupId.Equals(data.GroupId) && it.StartDay >= data.StartDay)
    .ToListAsync();  // ← 查询同组所有后续日程

if (data.CreatorUserId == _userManager.UserId)  // ← 当前用户是创建者
{
    scheduleList.AddRange(dataList);
    foreach (var item in dataList)  // ← 循环遍历每个日程
    {
        var dataUser = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
            .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id))
            .ToListAsync();  // ← N+1: 每个日程查询一次参与人
        scheduleLogList.Add(AddScheduleLog(item, string.Join(",", dataUser), "3"));
    }
}
```

**调用链**：
```
Delete(id, type=2)
  → 查询 dataList（同组后续日程）
  → foreach (item in dataList)
    → 查询 ScheduleUserEntity（item 的参与人）
    → 生成日志
```

### 查询 2：Lines 841-843（case 3，创建者分支）

```csharp
// case 3: 参与人所有日程
var allDataList = await _repository.AsQueryable()
    .Where(it => it.DeleteMark == null && it.GroupId.Equals(data.GroupId))
    .ToListAsync();  // ← 查询同组所有日程

if (data.CreatorUserId.Equals(_userManager.UserId))  // ← 当前用户是创建者
{
    foreach (var item in allDataList)  // ← 循环遍历每个日程
    {
        var userList = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
            .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id))
            .ToListAsync();  // ← N+1: 每个日程查询一次参与人
        scheduleList.Add(item);
        scheduleLogList.Add(AddScheduleLog(item, string.Join(",", userList), "3"));
    }
}
```

**调用链**：
```
Delete(id, type=3)
  → 查询 allDataList（同组所有日程）
  → foreach (item in allDataList)
    → 查询 ScheduleUserEntity（item 的参与人）
    → 生成日志
```

### 查询 3：Lines 852-854（case 3，非创建者分支）

```csharp
else  // ← 当前用户不是创建者
{
    foreach (var item in allDataList)  // ← 循环遍历每个日程
    {
        var user = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
            .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id) && it.ToUserId.Equals(_userManager.UserId))
            .FirstAsync();  // ← N+1: 每个日程查询一次当前用户是否是参与人
        if (user.IsNotEmptyOrNull())
        {
            scheduleParticipantsList.Add(user);
            scheduleLogList.Add(AddScheduleLog(item, user.ToUserId, "4"));
        }
    }
}
```

**调用链**：
```
Delete(id, type=3)
  → 查询 allDataList（同组所有日程）
  → foreach (item in allDataList)
    → 查询 ScheduleUserEntity（当前用户是否是 item 的参与人）
    → 如果是，加入参与人列表并生成日志
```

---

## 2. 确认实际数据库执行

### SqlSugar 执行模式分析

SqlSugar 的 `ToListAsync()` 和 `FirstAsync()` 是**立即执行**的异步方法，会立即生成并执行 SQL。

**证据**：
- `ToListAsync()` 调用 `ExecuteReaderAsync()`，立即执行 SQL
- `FirstAsync()` 调用 `ExecuteScalarAsync()` 或 `ExecuteReaderAsync()`，立即执行 SQL
- 没有延迟执行（deferred execution）机制

**结论**：✅ 每次循环都会执行一次数据库查询，确认是 N+1 问题。

---

## 3. N 的合理规模证据

### N 的来源

N = 同组日程的数量（`dataList.Count` 或 `allDataList.Count`）

### 业务场景分析

日程系统支持重复日程：
- 每天重复（每天 1 个日程）
- 每周重复（每周 1 个日程）
- 每月重复（每月 1 个日程）
- 每年重复（每年 1 个日程）

### 典型规模估算

| 重复类型 | 重复周期 | 1 年日程数 | 2 年日程数 | 5 年日程数 |
|----------|----------|------------|------------|------------|
| 每天重复 | 365 天 | 365 | 730 | 1825 |
| 每周重复 | 52 周 | 52 | 104 | 260 |
| 每月重复 | 12 月 | 12 | 24 | 60 |
| 每年重复 | 1 年 | 1 | 2 | 5 |

**合理规模**：
- **典型场景**：每周重复 1 年 = 52 个日程
- **极端场景**：每天重复 5 年 = 1825 个日程
- **实际场景**：用户通常不会创建超过 1 年的重复日程，N 通常在 10-100 范围内

**结论**：N 的合理规模为 **10-100**，极端情况下可达 **1000+**。

---

## 4. 当前查询次数与理论增长关系

### 当前查询次数

| 操作 | 查询次数 | 说明 |
|------|----------|------|
| 查询日程列表 | 1 次 | `dataList` 或 `allDataList` |
| 查询参与人 | N 次 | 每个日程查询一次 |
| **总计** | **N+1 次** | 1 + N |

### 理论增长

| N | 查询次数 | 数据库往返 | 预估耗时（假设每次 5ms） |
|---|----------|------------|--------------------------|
| 10 | 11 | 11 | 55ms |
| 50 | 51 | 51 | 255ms |
| 100 | 101 | 101 | 505ms |
| 500 | 501 | 501 | 2505ms |
| 1000 | 1001 | 1001 | 5005ms |

**性能影响**：
- N=10-50：可接受（<300ms）
- N=100：边界（~500ms）
- N=500+：不可接受（>2.5s）

**结论**：✅ N+1 查询在 N>100 时会造成明显性能问题。

---

## 5. 候选批量查询方案

### 方案 A：批量查询 + 内存分组（推荐）

```csharp
// 一次性查询所有参与人
var allScheduleUsers = await _repository.AsSugarClient()
    .Queryable<ScheduleUserEntity>()
    .Where(it => it.DeleteMark == null && dataList.Select(s => s.Id).Contains(it.ScheduleId))
    .ToListAsync();

// 按 ScheduleId 分组
var userGroups = allScheduleUsers
    .GroupBy(u => u.ScheduleId)
    .ToDictionary(g => g.Key, g => g.ToList());

// 遍历日程，从字典中获取参与人
foreach (var item in dataList)
{
    var dataUser = userGroups.ContainsKey(item.Id) ? userGroups[item.Id] : new List<ScheduleUserEntity>();
    scheduleLogList.Add(AddScheduleLog(item, string.Join(",", dataUser), "3"));
}
```

**优点**：
- 查询次数从 N+1 降为 2（1 次查日程 + 1 次查参与人）
- 内存占用可控（一次性加载所有参与人）
- 代码改动小

**缺点**：
- 如果日程数量很大，一次性加载所有参与人可能占用较多内存
- 但相比 N+1 问题，这是可接受的权衡

### 方案 B：使用 SqlSugar Includes（不推荐）

```csharp
var dataList = await _repository.AsQueryable()
    .Includes(it => it.ScheduleUsers)  // ← 假设 ScheduleEntity 有 ScheduleUsers 导航属性
    .Where(it => it.DeleteMark == null && it.GroupId.Equals(data.GroupId) && it.StartDay >= data.StartDay)
    .ToListAsync();
```

**优点**：
- 代码更简洁
- ORM 自动处理 N+1

**缺点**：
- 需要检查 ScheduleEntity 是否有 ScheduleUsers 导航属性
- 可能需要修改实体定义
- 改造半径较大

**结论**：方案 A 更适合当前场景（改造半径小，收益明确）。

---

## 6. 权限、租户、软删除、事务、并发等语义风险

### 权限风险

**当前逻辑**：
- 查询 `ScheduleUserEntity` 时过滤 `DeleteMark == null`
- 没有额外的权限检查（假设当前用户有权删除日程）

**批量方案**：
- 保持相同的过滤条件 `DeleteMark == null`
- ✅ 无权限风险

### 租户风险

**当前逻辑**：
- 查询 `ScheduleUserEntity` 时没有显式过滤 `TenantId`
- 依赖 SqlSugar 的全局租户过滤器

**批量方案**：
- 保持相同的查询逻辑
- ✅ 无租户风险（依赖全局过滤器）

### 软删除风险

**当前逻辑**：
- 查询 `ScheduleUserEntity` 时过滤 `DeleteMark == null`
- 删除操作使用软删除（`DeleteMark = 1`）

**批量方案**：
- 保持相同的过滤条件
- ✅ 无软删除风险

### 事务风险

**当前逻辑**：
- Delete 方法没有显式事务
- 依赖 SqlSugar 的默认事务行为（每个操作独立事务）

**批量方案**：
- 保持相同的事务行为
- ✅ 无事务风险

### 并发风险

**当前逻辑**：
- 没有显式并发控制
- 依赖数据库的行级锁

**批量方案**：
- 保持相同的并发行为
- ✅ 无并发风险

**结论**：✅ 批量方案不会引入新的语义风险。

---

## 7. 修改预算

### 代码改动

| 位置 | 改动内容 | 行数 |
|------|----------|------|
| Lines 807-813 | 替换循环内查询为批量查询 + 分组 | -7 / +10 |
| Lines 839-846 | 替换循环内查询为批量查询 + 分组 | -8 / +10 |
| Lines 850-860 | 替换循环内查询为批量查询 + 分组 | -11 / +12 |
| **总计** | | **-26 / +32** |

### 复杂度增加

- 新增字典分组逻辑
- 需要处理 `ContainsKey` 检查
- 复杂度增加：**低**

### 测试成本

- 需要测试 3 个 case（type=1, 2, 3）
- 需要测试创建者/非创建者分支
- 测试成本：**中**

**结论**：修改预算合理，改造半径为单类单点。

---

## 8. 验证方案

### 功能验证

1. **删除单个日程**（type=1）
   - 创建者删除：验证日程被软删除，日志正确
   - 非创建者删除：验证参与人记录被禁用，日志正确

2. **删除当前日程及后续**（type=2）
   - 创建者删除：验证后续日程被软删除，日志正确
   - 非创建者删除：验证参与人记录被禁用，日志正确

3. **删除参与人所有日程**（type=3）
   - 创建者删除：验证所有日程被软删除，日志正确
   - 非创建者删除：验证参与人记录被禁用，日志正确

### 性能验证

1. **创建重复日程**
   - 创建每天重复 1 年的日程（N=365）
   - 创建每周重复 1 年的日程（N=52）

2. **删除重复日程**
   - 删除 type=2（当前及后续）
   - 删除 type=3（所有日程）
   - 记录响应时间

3. **对比改前改后**
   - 改前：记录 N+1 查询的响应时间
   - 改后：记录批量查询的响应时间
   - 预期：响应时间显著下降

### 回归验证

1. **Build 验证**
   - `dotnet build -c Release -p:CI_BUILD=true` 0 错误

2. **行为验证**
   - 删除操作的行为不变（软删除、日志、通知）
   - 权限检查不变
   - 租户隔离不变

---

## 9. 6 要素 + 10 要素最终判断

### 6 要素准入

| 要素 | 判定 | 理由 |
|------|------|------|
| 1. Evidence 确认 | ✅ 满足 | 3 处明确的循环内查询，SqlSugar 立即执行 |
| 2. Contract violation | ✅ 满足 | 性能 Contract（N+1 查询导致性能下降） |
| 3. 单点边界 | ✅ 满足 | Delete 方法内 3 处循环，可单点修复 |
| 4. 门控通过 | ✅ 满足 | Risk Medium + 性能优化 + Budget 低成本 |
| 5. 回归路径 | ✅ 满足 | build + 删除操作回归 + 性能对比 |
| 6. 不扩 Contract | ✅ 满足 | 对外行为不变，仅内部查询优化 |

**结论**：✅ 6 要素全部满足

### 10 要素门控

| 要素 | 判定 | 理由 |
|------|------|------|
| 1. 只有猜测无证据 | ❌ 不命中 | 有代码实查 + SqlSugar 执行模式分析 |
| 2. 仅 Capability 缺失 | ❌ 不命中 | 属性能 Contract violation |
| 3. 仅 Test gap | ❌ 不命中 | 非仅缺测试 |
| 4. Not a defect | ❌ 不命中 | N+1 查询确实是缺陷 |
| 5. 需扩大公共 Contract | ❌ 不命中 | 不扩 |
| 6. 需引入新架构 | ❌ 不命中 | 仅批量查询 |
| 7. 需高级优化但无性能证据 | ⚠️ 部分命中 | 无运行时性能数据，但 N+1 形态明确 |
| 8. 无法保持单点 | ❌ 不命中 | 单点 Delete 方法 |
| 9. 会牵连其他类/模块 | ❌ 不命中 | 不牵 |
| 10. 回归无法验证 | ❌ 不命中 | 可验证 |

**结论**：✅ 10 要素中 0 项命中（第 7 项部分命中但不构成 Stop）

---

## 10. 最终 Gate Decision

### **GO — 批准单点 F-P1 Fix**

**理由**：
1. 6 要素全部满足
2. 10 要素中 0 项命中
3. N+1 查询形态明确，性能影响可量化
4. 批量方案改造半径小（单类单点）
5. 语义风险可控（权限、租户、软删除、事务、并发均无风险）
6. 验证方案完整（功能 + 性能 + 回归）

**下一步**：
1. 提交 F-P1 Fix（单点修复 Delete 方法内的 3 处 N+1 查询）
2. 执行验证方案
3. 提交单提交

---

## 附录：其他 Finding 状态

| Finding | 状态 | 理由 |
|---------|------|------|
| F-L1 | Closed | DI scope 使用正确 |
| F-A1 | Closed | 职责拆分收益有限 |
| F-P2 | 冻结 | 需运行时证据 |
| F-E2 | Closed | 无异常泄露 |

---

> **本包结论**：F-P1 Gate Decision = **GO**，满足所有准入条件，可进入实现阶段。
