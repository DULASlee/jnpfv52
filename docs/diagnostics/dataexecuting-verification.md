# DataExecuting 行为验证报告

> 验证日期：2026-06-07
> 测试环境：SQLite 内存数据库
> 验证程序：tests/verifications/SqlSugarVerification/Program.cs

## 测试结果

| 验证项 | 结果 | 详情 |
|---|---|---|
| 1. 覆盖 vs 追加 | 覆盖 | DataExecuting 使用 = 赋值，后者覆盖前者 |
| 2. += 语法 | Action<> 属性 | 非 event，不支持 += 追加 |
| 3. CopyNew 继承 AOP | 继承 | CopyNew 后子实例保留父实例的 DataExecuting |
| 4. 多线程安全 | 安全 | CopyNew 后各实例 AOP 回调互不干扰 |

## 决策输出（ADR-002）

**结论：情况 B — CopyNew 继承 AOP，需要在 SetDbAop 中组装统一委托**

- DataExecuting 是覆盖模式（=），不支持 += 追加
- CopyNew 后子实例继承父实例的 AOP 配置
- 策略：在 `SqlSugarConfigureExtensions.SetDbAop` 中组装一个统一的 DataExecuting 委托，包含所有维度（TenantId + ZxSystemId + 未来扩展）
- 已有的 `SqlSugarDbContextProvider.ApplyDataExecutingFilter` 已采用此策略（合并了 TenantId 和 ZxSystemId）

## 对阶段 4 的影响

- Repository 的 CopyNew 实例会自动继承全局 DataExecuting 配置
- 无需在每个 Repository 构造函数中重复设置 DataExecuting
- 如需额外维度的过滤，在统一委托中追加逻辑即可
