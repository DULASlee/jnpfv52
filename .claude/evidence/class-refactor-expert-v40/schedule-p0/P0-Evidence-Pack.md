# ScheduleService 类级 P0 取证 — 门户日程服务

> **状态**：只读分析，不改生产代码  
> **目标类**：`JNPF.Systems.System.ScheduleService`  
> **文件**：`backend/modularity/system/JNPF.Systems/System/ScheduleService.cs`  
> **大小**：1469 行 / 62.7 KB  
> **选择理由**：
> - 具有多种技术特征（任务队列、缓存、消息通知、DI 生命周期）
> - 不是最核心的权限/用户类
> - 有一定的复杂度，可以验证 Skill 在不同技术特征上的适用性
> - 与已分析的 FileService（资源生命周期）、EmailService（异常上下文）形成异质对比

---

## P0.1 代码事实（静态）

### 基本信息

| 项 | 值 |
|----|----|
| 行数 | 1469 |
| 方法数 | ~30（公开 API + 私有方法） |
| 字段数 | 6（_repository, _userManager, _taskQueue, _cacheManager, _messageManager, _serviceScopeFactory） |
| DI 生命周期 | `ITransient`（每请求新建） |
| 模块 | `system`（核心模块） |
| 职责 | 门户日程管理（CRUD + 提醒 + 重复日程） |

### 依赖注入

```csharp
public ScheduleService(
    ISqlSugarRepository<ScheduleEntity> repository,      // 数据库仓储
    IUserManager userManager,                             // 用户管理
    ITaskQueue taskQueue,                                 // 任务队列（异步）
    ICacheManager cacheManager,                           // 缓存管理
    IMessageManager messageManager,                       // 消息通知
    IServiceScopeFactory serviceScopeFactory)             // DI 作用域工厂
```

### 技术特征

| 特征 | 涉及方法 | 潜在问题 |
|------|----------|----------|
| **任务队列** | `Create`, `Modify`, `Delete` | 异步任务管理、失败重试、并发 |
| **缓存管理** | 多处 | 缓存一致性、过期策略、内存泄漏 |
| **消息通知** | `Create`, `Modify` | 异步通知、失败处理 |
| **DI 作用域** | 任务队列回调 | 作用域生命周期、内存泄漏 |
| **复杂查询** | `GetList`, `GetAppList`, `GetInfo` | N+1、性能、内存分配 |
| **重复日程** | `AddSchedule`, `CreateScheduleLog` | 递归、状态管理、并发 |

---

## P0.2 运行时事实（推断，待验证）

### 性能热点（推断）

| 方法 | 潜在热点 | 推断依据 |
|------|----------|----------|
| `GetAppList` | N+1 查询 | `while (input.startTime <= input.endTime)` 循环内查询 |
| `GetList` | 大结果集 | `ToListAsync()` 无分页 |
| `GetDetalInfo` | 多次查询 | 多次 `Subqueryable` 子查询 |

### 并发风险（推断）

| 场景 | 潜在风险 | 推断依据 |
|------|----------|----------|
| 重复日程创建 | 并发冲突 | 多个用户同时创建相同重复日程 |
| 任务队列回调 | 作用域泄漏 | `IServiceScopeFactory` 使用不当 |
| 缓存更新 | 一致性问题 | 缓存与数据库不同步 |

### 资源泄漏（推断）

| 资源 | 潜在泄漏 | 推断依据 |
|------|----------|----------|
| DI 作用域 | 未释放 | `IServiceScopeFactory.CreateScope()` 未 `using` |
| 任务队列 | 未清理 | 任务完成后未清理 |
| 缓存 | 未过期 | 缓存项未设置过期时间 |

---

## P0.3 架构事实

### 依赖方向

```
ScheduleService (System)
  ↓
ISqlSugarRepository<ScheduleEntity> (Data)
IUserManager (Common)
ITaskQueue (TaskQueue)
ICacheManager (Common)
IMessageManager (Message)
IServiceScopeFactory (DI)
```

**合规性**：✅ 符合洋葱架构（Service → Repository/Manager）

### 模块边界

| 依赖 | 模块 | 边界检查 |
|------|------|----------|
| `ISqlSugarRepository<ScheduleEntity>` | Data | ✅ 合规 |
| `IUserManager` | Common | ✅ 合规 |
| `ITaskQueue` | TaskQueue | ⚠️ 跨模块（需检查接口定义） |
| `ICacheManager` | Common | ✅ 合规 |
| `IMessageManager` | Message | ⚠️ 跨模块（需检查接口定义） |
| `IServiceScopeFactory` | DI | ✅ 合规 |

### 职责分析

**主要职责**：
1. 日程 CRUD（GetList, GetInfo, Create, Modify, Delete）
2. 日程提醒（任务队列 + 消息通知）
3. 重复日程（递归创建）
4. 日程用户管理（多用户共享日程）

**潜在职责过载**：
- 同时处理 CRUD、提醒、重复逻辑、用户管理
- 可能需要拆分为多个服务

---

## P0.4 测试事实

| 项 | 值 |
|----|----|
| 单测覆盖 | ❌ 无（`backend/tests` 中无 ScheduleService 测试） |
| 集成测试 | ❌ 无 |
| 行为特征考卷 | ❌ 未命中（不在 30 条基线中） |

---

## P0.5 风险定级

### 总体风险：**Medium-High**

| 风险类别 | 等级 | 依据 |
|----------|------|------|
| 性能 | Medium | N+1 查询、大结果集 |
| 并发 | Medium | 重复日程创建、任务队列回调 |
| 资源泄漏 | Medium-High | DI 作用域、缓存、任务队列 |
| 架构 | Medium | 职责过载、跨模块依赖 |
| 安全 | Low | 无明显安全漏洞 |
| 可测试性 | High | 无测试覆盖、依赖复杂 |

---

## Finding Inventory（初步）

### 性能类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-P1 | F2 | N+1 查询 | `GetAppList` 138-146 | `while` 循环内查询，日期范围越大查询越多 | High | 代码实查 |
| F-P2 | F1 | 大结果集 | `GetList` 124 | `ToListAsync()` 无分页，可能返回大量数据 | Medium | 代码实查 |
| F-P3 | F4 | 多次子查询 | `GetDetalInfo` 241-263 | 多次 `Subqueryable` 子查询 | Medium | 代码实查 |

### 并发类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-C1 | D1 | 并发冲突 | `Create` 290+ | 重复日程创建无并发控制 | Medium | 代码实查 |
| F-C2 | D3 | 静态可变状态 | 多处 | 需检查是否有静态字段/缓存 | Low | 待查 |

### 资源泄漏类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-L1 | A3 | DI 作用域泄漏 | 任务队列回调 | `IServiceScopeFactory.CreateScope()` 可能未 `using` | High | 待查 |
| F-L2 | A2 | 缓存泄漏 | 多处 | 缓存项可能未设置过期时间 | Medium | 待查 |
| F-L3 | A5 | 任务队列泄漏 | 任务队列回调 | 任务完成后可能未清理 | Medium | 待查 |

### 异常类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-E1 | E2 | 异常吞没 | 任务队列回调 | 任务执行失败可能未记录日志 | Medium | 待查 |
| F-E2 | E4 | 异常信息泄露 | 多处 | 需检查是否有 `ex.Message` 直接返回 | Low | 待查 |

### 架构类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-A1 | L1 | 职责过载 | 整个类 | 同时处理 CRUD、提醒、重复逻辑、用户管理 | Medium | 代码实查 |
| F-A2 | I1 | 跨模块依赖 | `ITaskQueue`, `IMessageManager` | 需检查接口定义是否合规 | Low | 待查 |

---

## 下一步

### 需要深入分析的 Finding

1. **F-L1（DI 作用域泄漏）**：需要查看任务队列回调代码，确认 `IServiceScope` 是否正确释放
2. **F-P1（N+1 查询）**：需要量化日期范围对性能的影响
3. **F-A1（职责过载）**：需要评估是否值得拆分

### 可以直接 Gate 的 Finding

1. **F-P2（大结果集）**：可以立即判断是否需要分页
2. **F-E2（异常信息泄露）**：可以快速扫描是否有 `ex.Message` 返回

---

## 决策

### 当前状态：**P0 取证完成，进入 Finding 深入分析阶段**

### 下一步行动

1. **深入分析 F-L1（DI 作用域泄漏）**：查看任务队列回调代码
2. **Gate F-P2（大结果集）**：判断是否需要分页
3. **Gate F-E2（异常信息泄露）**：快速扫描

### 预期产出

- Finding Inventory 完善
- 风险分级完成
- Gate 判定完成
- 选择第一个值得修改的 Finding

---

> **本包证明**：ScheduleService 类级 P0 取证完成，识别出 ~12 个潜在 Finding，涵盖性能、并发、资源泄漏、异常、架构等多个维度。下一步将深入分析并 Gate。
