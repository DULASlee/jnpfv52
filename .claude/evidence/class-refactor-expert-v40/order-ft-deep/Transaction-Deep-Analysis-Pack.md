# OrderService Transaction Deep Analysis Pack — F-T1 / F-T2

> **状态**：只读分析，不改生产代码  
> **目标**：建立完整的事务边界模型，分析 F-T1/F-T2 独立性  
> **关键问题**：OrderService 是否事务真正的 owner？

---

## 1. Save 调用链分析

### 代码路径（Save 方法 198-240 行）

```csharp
[HttpPost("{id}")]  // ← 没有 [UnitOfWork] 特性
public async Task Save(string id, [FromBody] OrderCrInput input)
{
    var orderEntity = input.Adapt<OrderEntity>();
    orderEntity.Id = id;
    var orderEntryList = input.goodsList.Adapt<List<OrderEntryEntity>>();
    var orderReceivableList = input.collectionPlanList.Adapt<List<OrderReceivableEntity>>();
    
    // 1. 设置 ID 和 SortCode（内存操作）
    if (orderEntryList.IsNotEmptyOrNull()) { foreach (...) { itemEntity.Id = SnowflakeIdHelper.NextId(); } }
    if (orderEntryList.IsNotEmptyOrNull()) { foreach (...) { itemEntity.Id = YitIdHelper.NextId().ToString(); } }

    if (_repository.IsAny(x => x.Id == orderEntity.Id))  // ← 2. 查询订单是否存在
    {
        // 3. 删除旧明细
        await _repository.AsSugarClient().Deleteable<OrderEntryEntity>(x => x.OrderId == id).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<OrderReceivableEntity>(x => x.OrderId == id).ExecuteCommandAsync();
        
        // 4. 插入新明细
        await _repository.AsSugarClient().Insertable(orderEntryList).ExecuteCommandAsync();
        await _repository.AsSugarClient().Insertable(orderReceivableList).ExecuteCommandAsync();
        
        // 5. 更新订单主表
        await _repository.AsSugarClient().Updateable(orderEntity)...ExecuteCommandAsync();
    }
    else
    {
        // 6. 删除缓存
        _cacheManager.Del(string.Format("{0}{1}_{2}", CommonConst.CACHEKEYBILLRULE, _userManager.TenantId, _userManager.UserId + "OrderNumber"));
        
        // 7. 插入新明细 + 订单主表
        await _repository.AsSugarClient().Insertable(orderEntryList).ExecuteCommandAsync();
        await _repository.AsSugarClient().Insertable(orderReceivableList).ExecuteCommandAsync();
        await _repository.AsSugarClient().Insertable(orderEntity)...ExecuteCommandAsync();
    }
}
```

### 调用链

```
Controller (HTTP POST /api/extend/CrmOrder/{id})
  ↓
OrderService.Save
  ↓
_repository.IsAny()                              ← DB 查询（无事务）
  ↓
_repository.AsSugarClient().Deleteable().ExecuteCommandAsync()  ← DB 操作 1（无事务）
  ↓
_repository.AsSugarClient().Deleteable().ExecuteCommandAsync()  ← DB 操作 2（无事务）
  ↓
_repository.AsSugarClient().Insertable().ExecuteCommandAsync()   ← DB 操作 3（无事务）
  ↓
_repository.AsSugarClient().Insertable().ExecuteCommandAsync()   ← DB 操作 4（无事务）
  ↓
_repository.AsSugarClient().Updateable().ExecuteCommandAsync()   ← DB 操作 5（无事务）
  ↓
_cacheManager.Del()                              ← 外部副作用
```

### 事务状态

- ❌ **没有 [UnitOfWork] 特性**
- ❌ **没有显式 BeginTransaction/CommitTransaction**
- ❌ **没有外层 UoW 包装**
- ✅ **每个 ExecuteCommandAsync 是独立事务**

### 问题分析

**如果第 4 步（Insertable(orderEntryList)）失败**：
- 前面的 Delete 和 Insert 已经提交
- 数据不一致：订单明细被删除，但新明细未插入
- 没有回滚机制

**如果第 5 步（Updateable(orderEntity)）失败**：
- 明细已插入，但订单主表未更新
- 数据不一致：明细属于"旧订单"，但订单状态未更新

---

## 2. Delete 调用链分析

### 代码路径（Delete 方法 248-269 行）

```csharp
[HttpDelete("{id}")]  // ← 没有 [UnitOfWork] 特性
public async Task Delete(string id)
{
    var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);  // ← 1. 查询订单
    
    if (entity != null)
    {
        // 2. 删除订单明细
        await _repository.AsSugarClient().Deleteable<OrderEntryEntity>(x => x.OrderId == id).ExecuteCommandAsync();
        
        // 3. 删除收款计划
        await _repository.AsSugarClient().Deleteable<OrderReceivableEntity>(x => x.OrderId == id).ExecuteCommandAsync();
        
        // 4. 软删除订单主表
        await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete())...ExecuteCommandHasChangeAsync();
        
        // 5. 查询工作流任务
        var flowTaskEntity = await _repository.AsSugarClient().Queryable<FlowTaskEntity>().FirstAsync(x => x.Id == entity.Id);
        
        // 6. 软删除工作流任务
        if (flowTaskEntity.IsNotEmptyOrNull())
        {
            await _repository.AsSugarClient().Updateable(flowTaskEntity)...ExecuteCommandHasChangeAsync();
        }
        
        // 7. 删除附件文件（外部副作用）
        foreach (var item in entity.FileJson.ToList<AnnexModel>())
        {
            if (item.IsNotEmptyOrNull())
            {
                await _fileManager.DeleteFile(Path.Combine(FileVariable.SystemFilePath, item.FileName));
                // ← 无异常处理
            }
        }
    }
}
```

### 调用链

```
Controller (HTTP DELETE /api/extend/CrmOrder/{id})
  ↓
OrderService.Delete
  ↓
_repository.GetFirstAsync()                              ← DB 查询（无事务）
  ↓
_repository.AsSugarClient().Deleteable<OrderEntryEntity>().ExecuteCommandAsync()    ← DB 操作 1（无事务）
  ↓
_repository.AsSugarClient().Deleteable<OrderReceivableEntity>().ExecuteCommandAsync()  ← DB 操作 2（无事务）
  ↓
_repository.AsSugarClient().Updateable<OrderEntity>()...ExecuteCommandHasChangeAsync()    ← DB 操作 3（无事务）
  ↓
_repository.AsSugarClient().Queryable<FlowTaskEntity>().FirstAsync()    ← DB 查询（无事务）
  ↓
_repository.AsSugarClient().Updateable<FlowTaskEntity>()...ExecuteCommandHasChangeAsync()    ← DB 操作 4（无事务）
  ↓
_fileManager.DeleteFile()                                 ← 外部副作用（文件系统）
```

### 事务状态

- ❌ **没有 [UnitOfWork] 特性**
- ❌ **没有显式 BeginTransaction/CommitTransaction**
- ✅ **每个 ExecuteCommandAsync 是独立事务**

### 问题分析

**如果第 3 步（删除收款计划）失败**：
- 订单明细已删除，但收款计划仍存在
- 数据不一致：孤儿收款计划

**如果第 4 步（软删除订单主表）失败**：
- 明细已删除，但订单主表未标记删除
- 数据不一致：明细被删除，但订单仍显示存在

**如果第 6 步（软删除工作流任务）失败**：
- 订单已删除，但工作流任务仍存在
- 数据不一致：孤儿工作流任务

**如果第 7 步（删除文件）失败**：
- 数据库已删除，但文件仍存在
- 数据不一致：孤儿文件

---

## 3. 现有事务机制分析

### JNPF 框架的事务机制

| 机制 | 说明 | 适用场景 |
|------|------|----------|
| `[UnitOfWork]` 特性 | AOP Filter，自动管理事务 | 标注在 Service 方法上 |
| `BeginTransaction()` / `Commit()` / `Rollback()` | 手动事务管理 | Service 内部手动控制 |
| Repository 隐式事务 | 每个 ExecuteCommandAsync 独立事务 | 默认行为 |

### 对比 ProductService

**ProductService（正确做法）**：
```csharp
[HttpPost("")]
[UnitOfWork]  // ← 使用 [UnitOfWork] 特性
public async Task Create([FromBody] ProductCrInput input)
{
    // 多步数据库操作
    await _repository.Insertable(entity).ExecuteCommandAsync();
    await _repository.Insertable(productEntryList).ExecuteCommandAsync();
    // ← 所有操作在同一事务中，失败自动回滚
}
```

**OrderService（当前做法）**：
```csharp
[HttpPost("{id}")]  // ← 没有 [UnitOfWork] 特性
public async Task Save(string id, [FromBody] OrderCrInput input)
{
    // 多步数据库操作
    await _repository.Deleteable<OrderEntryEntity>().ExecuteCommandAsync();
    await _repository.Insertable(orderEntryList).ExecuteCommandAsync();
    await _repository.Updateable(orderEntity).ExecuteCommandAsync();
    // ← 每个操作独立事务，失败不回滚
}
```

### 结论

- **JNPF 框架已有成熟的事务机制**（[UnitOfWork] 特性）
- **ProductService 已正确使用该机制**
- **OrderService 未使用该机制**
- **OrderService 不是事务真正的 owner，但需要事务保护**

---

## 4. 数据库操作清单 vs 外部副作用清单

### Save 方法

| 操作 | 类型 | 必须原子？ | 能否回滚？ |
|------|------|------------|------------|
| `_repository.IsAny()` | DB 查询 | ❌ | ✅ |
| `Deleteable<OrderEntryEntity>()` | DB 删除 | ✅ | ✅ |
| `Deleteable<OrderReceivableEntity>()` | DB 删除 | ✅ | ✅ |
| `Insertable(orderEntryList)` | DB 插入 | ✅ | ✅ |
| `Insertable(orderReceivableList)` | DB 插入 | ✅ | ✅ |
| `Updateable(orderEntity)` | DB 更新 | ✅ | ✅ |
| `_cacheManager.Del()` | 缓存删除 | ⚠️ | ❌ |

**Database Consistency Boundary**：Order + OrderEntry + OrderReceivable（必须原子）

### Delete 方法

| 操作 | 类型 | 必须原子？ | 能否回滚？ |
|------|------|------------|------------|
| `_repository.GetFirstAsync()` | DB 查询 | ❌ | ✅ |
| `Deleteable<OrderEntryEntity>()` | DB 删除 | ✅ | ✅ |
| `Deleteable<OrderReceivableEntity>()` | DB 删除 | ✅ | ✅ |
| `Updateable<OrderEntity>()` | DB 软删除 | ✅ | ✅ |
| `Queryable<FlowTaskEntity>().FirstAsync()` | DB 查询 | ❌ | ✅ |
| `Updateable<FlowTaskEntity>()` | DB 软删除 | ✅ | ✅ |
| `_fileManager.DeleteFile()` | 文件系统删除 | ⚠️ | ❌ |

**Database Consistency Boundary**：Order + OrderEntry + OrderReceivable + FlowTask（必须原子）

**External Side-Effect Boundary**：文件系统（不能参与 DB 事务）

---

## 5. F-T1 / F-T2 独立性分析

### F-T1：Save 方法业务事务

| 项 | 内容 |
|----|------|
| **问题** | Save 方法包含 5+ 次数据库操作，无事务保护 |
| **证据** | 代码实查：无 [UnitOfWork]，无显式事务 |
| **影响** | 中途失败导致数据不一致（订单明细缺失、订单状态错误） |
| **单点修复** | ✅ 添加 `[UnitOfWork]` 特性即可 |
| **是否需要跨类修改** | ❌ 否，仅 OrderService 内部修改 |
| **是否改变事务语义** | ❌ 否，与 ProductService 一致 |

### F-T2：Delete 方法业务事务

| 项 | 内容 |
|----|------|
| **问题** | Delete 方法包含 4+ 次数据库操作 + 1 次文件删除，无事务保护 |
| **证据** | 代码实查：无 [UnitOfWork]，无显式事务 |
| **影响** | 中途失败导致数据不一致（孤儿收款计划、孤儿工作流任务、孤儿文件） |
| **单点修复** | ⚠️ 部分可修复：添加 `[UnitOfWork]` 保护 DB 操作，但文件删除需要单独处理 |
| **是否需要跨类修改** | ❌ 否，仅 OrderService 内部修改 |
| **是否改变事务语义** | ❌ 否，与 ProductService 一致 |

### 独立性结论

| 问题 | F-T1 | F-T2 |
|------|------|------|
| 共同根因 | ✅ 是（缺少 UnitOfWork） | ✅ 是（缺少 UnitOfWork） |
| 是否同一问题 | ✅ 是（同根因） | ✅ 是（同根因） |
| 是否可合并 | ✅ 建议合并 | ✅ 建议合并 |

**合并理由**：
- 两个 Finding 的根因相同（缺少 [UnitOfWork]）
- 修复方式相同（添加 [UnitOfWork]）
- 合并后形成"OrderService 缺少 UnitOfWork 保护"的统一 Finding

---

## 6. 6 要素初步门控

| 要素 | 判定 | 理由 |
|------|------|------|
| 1. Evidence 确认 | ✅ 满足 | 代码实查无 [UnitOfWork]，多次 DB 操作 |
| 2. Contract violation | ✅ 满足 | 业务事务 Contract（订单数据一致性） |
| 3. 单点边界 | ✅ 满足 | 仅 OrderService.Save/Delete 方法 |
| 4. 门控通过 | ✅ 满足 | Risk Medium + 业务事务 + Budget 低成本 |
| 5. 回归路径 | ✅ 满足 | build + 订单 CRUD 回归 |
| 6. 不扩 Contract | ✅ 满足 | 对外行为不变，仅内部事务保护 |

**结论**：✅ 6 要素全部满足

---

## 7. 跨类影响

| 项 | 影响 |
|----|------|
| OrderEntity | 无影响（保持不变） |
| OrderEntryEntity | 无影响 |
| OrderReceivableEntity | 无影响 |
| FlowTaskEntity | 无影响（OrderService.Delete 内已有逻辑） |
| Controller | 无影响（[UnitOfWork] 标注在 Service 方法上） |
| Database Schema | 无影响 |

**结论**：✅ 无跨类影响

---

## 8. 异常路径分析

### Save 方法异常路径

| 失败步骤 | 后果 | 当前行为 | 期望行为 |
|----------|------|----------|----------|
| Deleteable<OrderEntryEntity>() | 部分删除 | 已提交 | 回滚 |
| Deleteable<OrderReceivableEntity>() | 部分删除 | 已提交 | 回滚 |
| Insertable(orderEntryList) | 明细缺失 | 已提交（删除已生效） | 回滚 |
| Insertable(orderReceivableList) | 收款计划缺失 | 已提交 | 回滚 |
| Updateable(orderEntity) | 订单状态错误 | 已提交 | 回滚 |

### Delete 方法异常路径

| 失败步骤 | 后果 | 当前行为 | 期望行为 |
|----------|------|----------|----------|
| Deleteable<OrderEntryEntity>() | 孤儿收款计划 | 已提交 | 回滚 |
| Deleteable<OrderReceivableEntity>() | 孤儿工作流任务 | 已提交 | 回滚 |
| Updateable<OrderEntity>() | 孤儿工作流任务 | 已提交 | 回滚 |
| Updateable<FlowTaskEntity>() | 孤儿文件 | 已提交 | 回滚 + 文件清理重试 |
| _fileManager.DeleteFile() | 孤儿文件 | 已提交 | 单独处理 |

---

## 9. 最终决策

### **GO — 批准合并 F-T1 + F-T2 → 单一 Finding: OrderService 缺少 UnitOfWork**

**理由**：
1. F-T1 和 F-T2 是同一根因（缺少 [UnitOfWork]）
2. 6 要素全部满足
3. 无跨类影响
4. 修改极简（仅添加 2 个 [UnitOfWork] 特性）
5. 有 ProductService 作为参考实现
6. JNPF 框架已有成熟的事务机制

**Fix 方案**：
```csharp
[HttpPost("{id}")]
[UnitOfWork]  // ← 添加此特性
public async Task Save(string id, [FromBody] OrderCrInput input)
{
    // ... 现有代码不变
}

[HttpDelete("{id}")]
[UnitOfWork]  // ← 添加此特性
public async Task Delete(string id)
{
    // ... 现有代码不变
}
```

**剩余风险**：
- Delete 方法中的文件删除（_fileManager.DeleteFile）不在 DB 事务内
- 建议作为后续 Finding 单独处理（F-T3）

---

## 10. 建议

1. **合并 F-T1 + F-T2** → 单一 Finding：OrderService.Save/Delete 缺少 UnitOfWork
2. **下一步**：提交 Gate Pack，等待批准实现
3. **后续 Finding**：F-T3（文件删除一致性）单独处理

---

> **本包结论**：F-T1 和 F-T2 是同一根因，合并后 Decision = **GO**。修复方案极简（2 个特性），无跨类影响。