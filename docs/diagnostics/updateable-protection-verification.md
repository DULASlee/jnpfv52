# Updateable/Deleteable 租户保护验证报告

> 验证日期：2026-06-07
> 测试环境：SQLite 内存数据库

## 验证结果

| 验证项 | 结果 | 结论 |
|---|---|---|
| 1. Updateable + .Where() | 生效 | Repository 可通过 .Where() 附加租户条件 |
| 2. Updateable 不带 Where | 更新所有行 | 无 WHERE 条件会更新所有行（危险！） |
| 3. QueryFilter 影响 Updateable | 不影响 | SqlSugar 的 QueryFilter 仅对 Queryable 生效 |
| 4. Deleteable + .Where() | 生效 | Repository 可通过 .Where() 附加租户条件 |

## ADR-012 最终实现策略

**确认：QueryFilter 不影响 Updateable/Deleteable**

阶段 4 的 Repository 实现策略：
- Repository 覆写 `UpdateAsync` / `DeleteAsync` 方法
- 内部使用 `Updateable(entity).Where(tenantCondition).ExecuteCommandAsync()`
- 同时记录 WARNING 日志提醒开发者
- **禁止裸调用 `Updateable(entity).ExecuteCommandAsync()`（无 WHERE 条件）**

## 安全风险

- `Updateable` 不带 `WHERE` 会更新全表数据 → Repository 必须强制附加租户条件
- `Deleteable` 同理 → 必须附加租户条件
- 这是 SqlSugar 的设计：QueryFilter 仅影响查询，不影响写操作
