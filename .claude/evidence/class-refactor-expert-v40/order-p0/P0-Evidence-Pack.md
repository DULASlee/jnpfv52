# OrderService 类级 P0 取证 — 订单管理服务

> **状态**：只读分析，不改生产代码  
> **目标类**：`JNPF.Extend.OrderService`  
> **文件**：`backend/modularity/extend/JNPF.Extend/OrderService.cs`  
> **大小**：324 行 / 75 KB  
> **选择理由**：
> - 与 FileService（资源生命周期）、ScheduleService（CRUD + 重复逻辑）、EmailService（异常上下文）形成异质对比
> - 涉及**业务事务**、**数据权限**、**工作流集成**等新维度
> - 有真实生产价值（订单业务）
> - 不是最核心的权限/用户类
> - 可以验证 Skill 在业务事务场景下的能力

---

## P0.1 代码事实（静态）

### 基本信息

| 项 | 值 |
|----|----|
| 行数 | 324 |
| 方法数 | 12（8 GET + 2 POST + 2 Private） |
| 字段数 | 5（_repository, _usersService, _cacheManager, _fileManager, _userManager） |
| DI 生命周期 | `ITransient`（每请求新建） |
| 模块 | `extend`（扩展模块） |
| 职责 | 订单管理（CRUD + 工作流 + 数据权限 + 缓存） |

### 依赖注入

```csharp
public OrderService(
    ISqlSugarRepository<OrderEntity> repository,
    IUserManager userManager,
    IUsersService usersService,
    ICacheManager cacheManager)
{
    _repository = repository;
    _userManager = userManager;
    _usersService = usersService;
    _cacheManager = cacheManager;
}
```

### 技术特征

| 特征 | 涉及方法 | 潜在问题 |
|------|----------|----------|
| **数据权限** | `GetList` | 数据权限过滤性能、并发 |
| **工作流集成** | `Save`, `Delete` | 事务一致性、并发 |
| **缓存管理** | `Save` | 缓存一致性、过期策略 |
| **业务事务** | `Save`, `Delete` | 事务边界、并发控制 |
| **文件删除** | `Delete` | 资源生命周期、异常路径 |
| **硬编码数据** | `GetCustomerList`, `GetGoodsList` | 可维护性、安全 |
| **批量操作** | `Save` | foreach 循环 |

---

## P0.2 运行时事实（推断，待验证）

### 性能热点（推断）

| 方法 | 潜在热点 | 推断依据 |
|------|----------|----------|
| `GetList` | 数据权限过滤 | `_userManager.GetConditionAsync` 可能多次查询 |
| `GetInfo` | 多次查询 | 查询主表 + 收款计划 + 商品明细 + 用户名 |
| `Save` | 批量操作 | 多次 Insert/Update/Delete |

### 并发风险（推断）

| 场景 | 潜在风险 | 推断依据 |
|------|----------|----------|
| 订单并发修改 | 覆盖问题 | 无乐观锁 |
| 订单并发删除 | 引用问题 | 无事务保护 |
| 缓存一致性 | 缓存与数据库不同步 | 无缓存失效机制 |

### 业务事务风险（推断）

| 场景 | 潜在风险 | 推断依据 |
|------|----------|----------|
| Save 失败回滚 | 部分数据提交 | 无显式事务 |
| Delete 级联删除 | 部分删除 | 无显式事务 |
| 工作流同步 | 状态不一致 | 无事务保护 |

---

## P0.3 架构事实

### 依赖方向

```
OrderService (Extend)
  ↓
ISqlSugarRepository<OrderEntity> (Extend Entitys)
IUserManager (System Common)
IUsersService (System Permission)
ICacheManager (Common)
IFileManager (Common Core Manager)
FlowTaskEntity (Workflow Entitys)
```

**合规性**：✅ 符合洋葱架构

### 模块边界

| 依赖 | 模块 | 边界检查 |
|------|------|----------|
| `ISqlSugarRepository<OrderEntity>` | Extend Entitys | ✅ 合规 |
| `IUserManager` | System Common | ✅ 合规 |
| `IUsersService` | System Permission | ✅ 合规 |
| `ICacheManager` | Common | ✅ 合规 |
| `FlowTaskEntity` | Workflow Entitys | ⚠️ 跨模块直接引用实体 |

### 职责分析

**主要职责**：
1. 订单 CRUD（GetList, GetInfo, Save, Delete）
2. 数据权限过滤（GetList）
3. 工作流集成（Delete FlowTaskEntity）
4. 缓存管理（订单号缓存）
5. 文件删除（订单附件）

**职责多样性**：
- 业务事务（Save, Delete）
- 数据权限（GetList）
- 工作流集成（Delete）
- 缓存管理（Save）
- 文件管理（Delete）

---

## P0.4 测试事实

| 项 | 值 |
|----|----|
| 单测覆盖 | ❌ 无 |
| 集成测试 | ❌ 无 |
| 行为特征考卷 | ❌ 未命中 |

---

## P0.5 风险定级

### 总体风险：**Medium**

| 风险类别 | 等级 | 依据 |
|----------|------|------|
| 性能 | Medium | 数据权限过滤、多次查询 |
| 并发 | Medium | 无乐观锁、事务不一致 |
| 业务事务 | Medium-High | Save/Delete 无显式事务 |
| 资源泄漏 | Low | 文件删除可能泄漏 |
| 安全 | Low-Medium | 硬编码数据 |
| 可测试性 | High | 无测试覆盖 |

---

## Finding Inventory（初步）

### 业务事务类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-T1 | 事务 | 业务事务 | `Save` 225-239 | Save 方法包含多次 Insert/Update/Delete，无显式事务保护 | High | 代码实查 |
| F-T2 | 事务 | 级联删除 | `Delete` 248-269 | Delete 方法包含多次 Delete/Update，无显式事务 | High | 代码实查 |

### 并发类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-C1 | 并发 | 乐观锁 | `Save` | 无乐观锁，并发修改可能覆盖 | Medium | 代码实查 |
| F-C2 | 并发 | 数据权限 | `GetList` | 数据权限过滤可能受并发影响 | Medium | 代码实查 |

### 性能类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-P1 | 性能 | 多次查询 | `GetInfo` 142-152 | 查询主表 + 收款 + 商品 + 用户名，多次查询 | Medium | 代码实查 |
| F-P2 | 性能 | 数据权限 | `GetList` 81-83 | `GetConditionAsync` 可能多次查询 | Medium | 代码实查 |

### 安全/可维护性类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-S1 | 安全 | 硬编码数据 | `GetCustomerList` 160-169 | 硬编码客户数据 JSON（测试数据？） | Low | 代码实查 |
| F-S2 | 安全 | 硬编码数据 | `GetGoodsList` 177-186 | 硬编码商品数据 JSON（测试数据？） | Low | 代码实查 |
| F-S3 | 可维护性 | `DataTable.Select` | `GetDataFilter` 280-304 | 使用 `DataTable.Select(condition)` 字符串过滤，不安全 | Low | 代码实查 |

### 资源/可观测性类

| # | 维度 | 规则 | 位置 | 问题摘要 | 影响面 | 证据 |
|---|------|------|------|----------|--------|------|
| F-R1 | 资源 | 文件删除 | `Delete` 261-267 | foreach 循环内删除文件，无异常处理 | Low | 代码实查 |
| F-R2 | 可观测性 | 日志 | 全部 | 关键操作无日志 | Low | 代码实查 |

---

## 下一步

### 需要深入分析的 Finding

1. **F-T1（Save 业务事务）**：需要确认多次数据库操作的执行顺序和失败回滚语义
2. **F-T2（Delete 级联删除）**：需要确认级联删除的事务一致性
3. **F-S1/F-S2（硬编码数据）**：需要确认是测试数据还是生产代码

### 可以直接 Gate 的 Finding

1. **F-R1（文件删除）**：需要确认异常路径

---

## 决策

### 当前状态：**P0 取证完成，进入 Finding 深入分析阶段**

### 下一步行动

1. **深入分析 F-T1/F-T2（业务事务）**：这是本类最关键的技术维度
2. **确认 F-S1/F-S2（硬编码数据）**：是否为遗留测试数据
3. **Gate F-R1（文件删除）**：异常路径分析

---

> **本包证明**：OrderService 类级 P0 取证完成，识别出 ~10 个潜在 Finding，涵盖业务事务、并发、性能、安全、资源等多个维度。下一步将深入分析业务事务类 Finding（F-T1/F-T2）。