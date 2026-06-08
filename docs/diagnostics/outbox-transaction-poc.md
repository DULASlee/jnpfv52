# Outbox 事务原子性 PoC 报告

> 验证日期：2026-06-07
> 测试环境：SQLite 内存数据库

## PoC 结果

| PoC | 场景 | 结果 | 详情 |
|---|---|---|---|
| 1 | 单数据库跨表事务 | 通过 | BeginTran/CommitTran 跨表原子提交 |
| 1b | Rollback 验证 | 通过 | 异常后数据正确回滚 |
| 2 | CopyNew 事务隔离 | 需确认 | SQLite 内存库可能共享事务，需 SQL Server 验证 |
| 3 | 跨实体强事务 | 通过 | 第三步失败时前两步正确回滚 |

## 决策输出

- **PoC 1/1b/3 通过** → 单数据库跨表事务可行，Outbox 强事务设计成立
- **PoC 2 需 SQL Server 验证** → SQLite 内存库的隔离级别可能与 SQL Server 不同
- **建议**：阶段 4 在 SQL Server 环境下重新验证 PoC 2

## 阶段 5 的调整建议

- Outbox 写入使用与业务数据相同的数据库连接（单数据库事务）
- CopyNew 后的事务隔离需在 SQL Server 上确认
- 如隔离不足，Outbox 写入应使用父实例而非 CopyNew 实例
