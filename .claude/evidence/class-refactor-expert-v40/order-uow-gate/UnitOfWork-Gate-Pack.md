# OrderService UnitOfWork Gate Pack — F-T1/F-T2 合并

> **状态**：Gate 阶段，不改生产代码  
> **Finding**：F-T1/F-T2 合并 → OrderService 缺少 UnitOfWork 保护  
> **Fix 方案**：Save 和 Delete 方法各添加 1 个 `[UnitOfWork]` 特性  
> **改动预算**：最多 2 个 `[UnitOfWork]` 特性；禁止修改 F-T3、文件删除、缓存、其他 Finding

---

## 1. `[UnitOfWork]` 放置位置

### 特性可用范围

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class UnitOfWorkAttribute : Attribute, IAsyncActionFilter, IOrderedFilter
```

**可以放置**：
- ✅ 方法级别（推荐）
- ✅ 类级别

### 方案对比

| 方案 | 优点 | 缺点 |
|------|------|------|
| **方法级别**（Save + Delete） | 精确控制，只影响这两个方法 | 需要添加 2 个特性 |
| **类级别**（OrderService） | 简单，只需添加 1 个特性 | 影响所有方法（包括 GET） |

### 推荐方案：**方法级别**

**理由**：
1. GET 方法（GetList, GetInfo 等）通常不应该参与事务（只读操作）
2. 精确控制事务边界
3. 与 ProductService 的做法一致（方法级别）

---

## 2. AOP 事务生命周期分析

### OnActionExecutionAsync 流程（来自 UnitOfWorkAttribute.cs:89-141）

```
1. CreateTransactionScope()                    ← 创建事务作用域
2. BeginTransaction()                          ← 开启 DB 事务
3. await next()                                 ← 执行 Action 方法
   ├── 多个 DB 操作在同一 SqlSugar 连接上执行
   └── 每个操作自动加入当前事务
4. CommitTransaction() or RollbackTransaction()  ← 根据 Exception 决定
5. transactionScope?.Complete()                ← 仅在成功时
6. transactionScope?.Dispose()                 ← finally
```

### Save 方法事务生命周期

```
Controller → [UnitOfWork] AOP Filter → Save 方法
                │
                ├── BeginTransaction (DB)
                ├── await next()
                │   ├── _repository.IsAny()         ← 同一事务
                │   ├── Deleteable<OrderEntry>     ← 同一事务
                │   ├── Deleteable<OrderReceivable> ← 同一事务
                │   ├── Insertable(orderEntry)     ← 同一事务
                │   ├── Insertable(orderReceivable) ← 同一事务
                │   ├── Updateable(orderEntity)     ← 同一事务
                │   └── _cacheManager.Del()         ← 外部副作用（不在事务内）
                ├── CommitTransaction (成功) or RollbackTransaction (异常)
                └── transactionScope.Complete() (成功) or Dispose() (异常)
```

### Delete 方法事务生命周期

```
Controller → [UnitOfWork] AOP Filter → Delete 方法
                │
                ├── BeginTransaction (DB)
                ├── await next()
                │   ├── _repository.GetFirstAsync()           ← 同一事务
                │   ├── Deleteable<OrderEntry>               ← 同一事务
                │   ├── Deleteable<OrderReceivable>           ← 同一事务
                │   ├── Updateable<OrderEntity>                ← 同一事务
                │   ├── Queryable<FlowTask>.FirstAsync()      ← 同一事务
                │   ├── Updateable<FlowTask>                   ← 同一事务
                │   └── foreach _fileManager.DeleteFile()     ← 外部副作用（不在事务内）
                ├── CommitTransaction (成功) or RollbackTransaction (异常)
                └── transactionScope.Complete() (成功) or Dispose() (异常)
```

---

## 3. 异步方法参与确认

### [UnitOfWork] 支持异步

- `UnitOfWorkAttribute` 实现 `IAsyncActionFilter`
- `OnActionExecutionAsync` 使用 `await next()`
- **完全支持 async Task 方法**（如 Save 和 Delete）

### SqlSugar 异步支持

- `ExecuteCommandAsync()` 是异步方法
- 在 UnitOfWork 事务内执行
- 异常会触发 RollbackTransaction

**结论**：✅ 异步方法正确参与 UnitOfWork

---

## 4. 嵌套事务风险分析

### 当前风险

- OrderService.Save 和 Delete 不存在嵌套调用
- 不会产生嵌套事务

### ProductService 参考

ProductService.Create 也使用 `[UnitOfWork]`，没有嵌套问题。

**结论**：✅ 无嵌套事务风险

---

## 5. 异常触发 Rollback 确认

### AOP Filter 异常处理（UnitOfWorkAttribute.cs:107-141）

```csharp
var resultContext = await next();  // 执行 Action

if (resultContext.Exception == null)
{
    _unitOfWork.CommitTransaction(resultContext, unitOfWorkAttribute);
    transactionScope?.Complete();
}
else
{
    _unitOfWork.RollbackTransaction(resultContext, unitOfWorkAttribute);
}

// 异常会被 ASP.NET Core 框架处理（返回 500 或 Oops）
```

### 异常触发场景

| 场景 | | 是否触发 Rollback |
|------|---|-------------------|
| DB 异常（如连接失败） | ✅ 是 | 是 |
| DB 约束违反（如外键） | ✅ 是 | 是 |
| `_cacheManager.Del()` 异常 | ⚠️ | **否**（缓存不在事务内） |
| `_fileManager.DeleteFile()` 异常 | ⚠️ | **否**（文件不在事务内） |

**结论**：✅ DB 异常正确触发 Rollback；外部副作用异常不影响 DB 事务

---

## 6. 返回成功时 Commit 确认

### Commit 触发条件

```csharp
if (resultContext.Exception == null)
{
    _unitOfWork.CommitTransaction(resultContext, unitOfWorkAttribute);
    transactionScope?.Complete();
}
```

**Commit 触发**：
- Action 方法正常返回（无异常）
- 所有 DB 操作成功
- transactionScope.Complete() 提交

**结论**：✅ 返回成功时正确 Commit

---

## 7. FlowTask DB 操作一致性确认

### Delete 方法中的 FlowTask 操作

```csharp
// 软删除 FlowTaskEntity
var flowTaskEntity = await _repository.AsSugarClient().Queryable<FlowTaskEntity>().FirstAsync(x => x.Id == entity.Id);
if (flowTaskEntity.IsNotEmptyOrNull())
{
    await _repository.AsSugarClient().Updateable(flowTaskEntity)...ExecuteCommandHasChangeAsync();
}
```

**同一事务确认**：
- ✅ FlowTask 查询和更新都在 `_repository.AsSugarClient()` 上执行
- ✅ 同一 SqlSugar 连接 = 同一事务
- ✅ 如果 Order 删除失败回滚，FlowTask 删除也会回滚

**结论**：✅ FlowTask DB 操作在同一 UnitOfWork 内

---

## 8. 文件删除一致性分析（重要边界）

### 关键判断

**本次 Fix 的目标是**：**恢复数据库一致性边界**，不是声称解决数据库 + 文件系统的跨资源原子性。

### 文件删除行为

```csharp
foreach (var item in entity.FileJson.ToList<AnnexModel>())
{
    if (item.IsNotEmptyOrNull())
    {
        await _fileManager.DeleteFile(Path.Combine(FileVariable.SystemFilePath, item.FileName));
        // ← 不在 DB 事务内
    }
}
```

### 可能的场景

| 场景 | 数据库状态 | 文件系统状态 | 一致性 |
|------|------------|--------------|--------|
| 全部成功 | 已删除 | 已删除 | ✅ 一致 |
| 文件删除失败 | 已删除 | 残留 | ⚠️ 不一致 |

### 结论

- **本次 Fix 不解决文件删除一致性**
- 文件删除失败会产生孤儿文件
- 这是 F-T3（文件删除一致性），**不在本次 Fix 范围**
- 建议作为后续 Finding 单独处理

---

## 9. 验证方案（8 项验证）

### 验证 1：正常 Save → Commit

**操作**：
- 创建新订单，传入完整 goodsList 和 collectionPlanList

**预期**：
- Order 表插入成功
- OrderEntry 表插入成功
- OrderReceivable 表插入成功
- 所有数据在数据库中可见

### 验证 2：Save 中途 DB 异常 → Rollback

**操作**：
- 创建订单，传入重复的 OrderId（外键冲突）

**预期**：
- Order 表未插入
- OrderEntry 表未插入
- OrderReceivable 表未插入
- 数据库无变化

### 验证 3：正常 Delete → 所有 DB 变更 Commit

**操作**：
- 删除订单，订单有明细、收款计划、工作流任务、附件

**预期**：
- Order 表软删除成功
- OrderEntry 表删除成功
- OrderReceivable 表删除成功
- FlowTask 表软删除成功
- 所有 DB 变更生效

### 验证 4：Delete 中途 DB 异常 → DB Changes Rollback

**操作**：
- 删除订单，模拟 FlowTask 删除失败

**预期**：
- Order 表回滚（未软删除）
- OrderEntry 表回滚（未删除）
- OrderReceivable 表回滚（未删除）
- FlowTask 表回滚（未软删除）

### 验证 5：FlowTask 相关 DB 操作是否随同 Rollback

**操作**：
- 删除订单，模拟 OrderEntry 删除失败

**预期**：
- Order 表回滚
- OrderEntry 表回滚
- FlowTask 表回滚（虽然还没执行）
- 所有 DB 状态保持原样

### 验证 6：文件删除行为保持原有语义

**操作**：
- 删除订单，附件文件存在

**预期**：
- 文件删除行为不变（仍然尝试删除）
- 文件删除失败不影响 DB 事务
- 可能在文件系统留下孤儿文件（已知问题，不在本次 Fix 范围）

### 验证 7：没有改变现有异常契约

**操作**：
- 删除不存在的订单

**预期**：
- 异常类型不变（Oops.Oh）
- 错误码不变
- HTTP 状态码不变

### 验证 8：没有产生嵌套/重复事务副作用

**操作**：
- Save 后立即 Delete 同一订单

**预期**：
- Save 和 Delete 各自的事务独立
- 没有嵌套事务
- 没有重复 Commit/Rollback

### 验证基础设施

- ✅ 优先使用现有测试机制
- ✅ 不建立新事务测试基础设施
- ✅ 集成测试可以验证 DB 状态
- ✅ 单元测试可以验证业务逻辑

---

## 10. 改动预算（最终）

### 允许的改动

| 位置 | 改动 | 行数 |
|------|------|------|
| `Save` 方法 | 添加 `[UnitOfWork]` 特性 | +1 |
| `Delete` 方法 | 添加 `[UnitOfWork]` 特性 | +1 |
| **总计** | | **+2** |

### 禁止的改动

| Finding | 状态 |
|---------|------|
| F-T3（文件删除一致性） | 🛑 Stop（不在本次 Fix 范围） |
| F-P1（多次查询） | 🛑 Stop（不在本次 Fix 范围） |
| F-C1（无乐观锁） | 🛑 Stop（不在本次 Fix 范围） |
| F-S1/F-S2（硬编码数据） | 🛑 Stop（不在本次 Fix 范围） |
| F-S3（DataTable.Select） | 🛑 Stop（不在本次 Fix 范围） |
| F-R1（文件删除异常） | 🛑 Stop（不在本次 Fix 范围） |
| F-R2（日志缺失） | 🛑 Stop（不在本次 Fix 范围） |

---

## 11. 6 要素 + 10 要素最终判断

### 6 要素

| 要素 | 判定 | 理由 |
|------|------|------|
| 1. Evidence 确认 | ✅ 满足 | OrderService 无 [UnitOfWork]，ProductService 有 |
| 2. Contract violation | ✅ 满足 | 业务事务 Contract（数据库一致性边界） |
| 3. 单点边界 | ✅ 满足 | 仅 Save 和 Delete 方法 |
| 4. 门控通过 | ✅ 满足 | Risk Medium + 业务事务 + Budget 极低（+2 行） |
| 5. 回归路径 | ✅ 满足 | build + 订单 CRUD 回归 + 8 项验证 |
| 6. 不扩 Contract | ✅ 满足 | 对外行为不变，仅内部事务保护 |

### 10 要素

| 要素 | 判定 |
|------|------|
| 1. 只有猜测无证据 | ❌ 不命中（有 ProductService 参考） |
| 2. 仅 Capability 缺失 | ❌ 不命中 |
| 3. 仅 Test gap | ❌ 不命中 |
| 4. Not a defect | ❌ 不命中（ProductService 已证明正确用法） |
| 5. 需扩大公共 Contract | ❌ 不命中 |
| 6. 需引入新架构 | ❌ 不命中（使用现有 UnitOfWork） |
| 7. 需高级优化但无性能证据 | ❌ 不命中 |
| 8. 无法保持单点 | ❌ 不命中 |
| 9. 会牵连其他类/模块 | ❌ 不命中 |
| 10. 回归无法验证 | ❌ 不命中（8 项验证可用） |

---

## 12. 最终 Gate Decision

### **GO — 批准单提交实现**

**理由**：
1. **修改极简**：仅 2 个 `[UnitOfWork]` 特性
2. **风险可控**：JNPF 框架已有成熟机制，ProductService 是参考
3. **验证充分**：8 项验证覆盖所有关键路径
4. **边界清晰**：本次 Fix 只恢复 DB 一致性边界，不解决文件系统原子性
5. **无跨类影响**：仅 OrderService 内部修改

**Fix 方案**：
```csharp
[HttpPost("{id}")]
[UnitOfWork]  // ← +1 添加
public async Task Save(string id, [FromBody] OrderCrInput input)
{
    // ... 现有代码不变
}

[HttpDelete("{id}")]
[UnitOfWork]  // ← +1 添加
public async Task Delete(string id)
{
    // ... 现有代码不变
}
```

**严格边界**：
- ✅ 只添加 `[UnitOfWork]` 特性
- ❌ 不修改方法签名
- ❌ 不修改业务逻辑
- ❌ 不修复 F-T3（文件删除）
- ❌ 不修改缓存逻辑
- ❌ 不修复其他 Finding

---

## 13. 风险声明

### 本次 Fix 解决的问题

- ✅ 数据库一致性边界恢复（Order + OrderEntry + OrderReceivable + FlowTask）
- ✅ 中途失败自动回滚
- ✅ 异常路径数据一致性

### 本次 Fix 不解决的问题

- ❌ 文件系统原子性（文件删除失败会产生孤儿文件）
- ❌ 并发控制（无乐观锁）
- ❌ 性能优化（N+1 查询、大结果集）
- ❌ 硬编码数据（测试数据？）

### 后续建议

- F-T3（文件删除一致性）作为独立 Finding 处理
- F-C1/F-C2（并发控制）作为独立 Finding 处理
- F-P1/F-P2（性能优化）作为独立 Finding 处理

---

> **本包结论**：Gate Decision = **GO**，修改极简（+2 行），风险可控，验证充分。