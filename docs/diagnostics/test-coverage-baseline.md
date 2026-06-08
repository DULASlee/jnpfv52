# JNPF V5.2 测试覆盖基线报告

> 评估日期：2026-06-07

---

## 测试项目现状

| 项目 | 状态 |
|---|---|
| 现有测试项目 | **无**（仅有 `JNPF.Xunit` 框架库，无实际测试） |
| 测试目录 | `tests/` 目录不存在于 `backend/` 下 |
| 本次新建 | `tests/verifications/SqlSugarVerification/`（阶段 0 验证用） |

## 本次验证测试结果

**验证程序：** `tests/verifications/SqlSugarVerification/Program.cs`
**测试环境：** SQLite 内存数据库
**结果：** 23/23 通过，0 失败

| 任务 | 验证项 | 结果 |
|---|---|---|
| 0.6 DataExecuting | 覆盖 vs 追加 | ✅ 覆盖模式(=) |
| 0.6 DataExecuting | += 语法 | ✅ 不支持（Action<> 属性） |
| 0.6 DataExecuting | CopyNew 继承 AOP | ✅ 继承 |
| 0.6 DataExecuting | 多线程安全 | ✅ 安全 |
| 0.7 CopyNew | 独立连接实例 | ✅ 通过 |
| 0.7 CopyNew | 连接字典继承 | ✅ 共享 |
| 0.7 CopyNew | 性能开销 | ✅ 0.078ms（< 1ms） |
| 0.7 CopyNew | Dispose 隔离 | ✅ 通过 |
| 0.7 CopyNew | GC 回收 | ✅ 8.27 MB |
| 0.10 Outbox | 单数据库跨表事务 | ✅ 通过 |
| 0.10 Outbox | Rollback 验证 | ✅ 通过 |
| 0.10 Outbox | CopyNew 事务隔离 | ✅ 通过 |
| 0.10 Outbox | 跨实体强事务 | ✅ 通过 |
| 0.11 Updateable | Updateable + .Where() | ✅ 生效 |
| 0.11 Updateable | 不带 Where | ✅ 被 SqlSugar 阻止 |
| 0.11 Updateable | QueryFilter 影响 | ✅ 不影响 Updateable |
| 0.11 Updateable | Deleteable + .Where() | ✅ 生效 |

## 无测试覆盖的核心组件

| 组件 | 所属项目 | 风险等级 | 优先级 |
|---|---|---|---|
| SqlSugarDbContextProvider 构造函数 | DatabaseAccessor.SqlSugar | P0 | 阶段 4 补充 |
| TenantContext（待建） | — | P0 | 阶段 2 新建 |
| JwtHandler 权限校验 | API.Entry | P0 | 阶段 0 修复后补充 |
| SqlSugarRepository CRUD | DatabaseAccessor.SqlSugar | P1 | 阶段 4 补充 |
| EventBus 发布/订阅 | EventBus.RabbitMQ | P1 | 阶段 5 补充 |
| UserManager 用户信息 | Common.Core | P1 | 阶段 4 补充 |
| QueryFilter 租户隔离 | DatabaseAccessor.SqlSugar | P0 | 阶段 4 补充 |
| DataExecuting 自动填充 | DatabaseAccessor.SqlSugar | P0 | 已验证（0.6） |
| CopyNew 连接隔离 | DatabaseAccessor.SqlSugar | P0 | 已验证（0.7） |
| Outbox 事务原子性 | — | P0 | 已验证（0.10） |

## 建议

1. **阶段 1：** 建立 CI/CD 测试管线，集成 `dotnet test`
2. **阶段 2：** 为 TenantContext 和 QueryFilter 编写集成测试
3. **阶段 4：** 为 SqlSugarRepository 编写单元测试（覆盖 CRUD + 租户隔离）
4. **阶段 5：** 为 EventBus Outbox 编写集成测试
