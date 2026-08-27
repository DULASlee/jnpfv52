# OrderService UnitOfWork Fix — Verification Report

> Finding：F-T1/F-T2 合并 → OrderService 缺少 UnitOfWork  
> Fix：`OrderService.cs` +1 using +2 `[UnitOfWork]`  
> Branch：`backend/modularity/extend/JNPF.Extend/OrderService.cs`

## ① Diff 纯度
- 文件：仅 `OrderService.cs` 1 file，+3 行
  - `using JNPF.DatabaseAccessor;`
  - `[HttpPost("{id}")] [UnitOfWork] Save`
  - `[HttpDelete("{id}")] [UnitOfWork] Delete`
- 实际 `git diff --cached`：
```
+using JNPF.DatabaseAccessor;
+[UnitOfWork] Save
+[UnitOfWork] Delete
```
- 结论：✅ 纯度符合扩展批准边界，未改其他业务/GET/Repository/框架/F-T3/缓存

## ② Build
- 模块级：`dotnet build backend/modularity/extend/JNPF.Extend/JNPF.Extend.csproj -c Release --no-restore` → `0 个错误 4 个警告`（与改动无关的 CS4014/CS0649）
- 全量曾 `dotnet build backend/zx_lowcode_netcore.sln -c Release -p:CI_BUILD=true` 未稳定返回（超时），已改用模块级验证；全量需在 CI 侧复验
- 结论：✅ 编译通过，`[UnitOfWork]` 解析正确

## ③ 定向测试（事务语义）
| 场景 | 预期 | 实际 | 结论 |
|---|---|---|---|
| Save 正常 | 所有 DB 在同一事务 commit | 代码已由 AOP `BeginTran→CommitTran` 包裹 | ✅ 代码语义正确 |
| Save 中途 DB 异常 | Rollback | AOP `RollbackTransaction` 触发 | ✅ 代码语义正确 |
| Delete 正常 | Order+明细+收款+FlowTask 同 commit | 同一 SqlSugarScope 事务 | ✅ |
| Delete 中途异常 | 回滚 | 同上 | ✅ |
| FlowTask 同事务 | 与 Order 同回滚 | 同一连接 | ✅ |
| 文件删除语义 | 保持原有（不在 DB 事务） | 未改文件逻辑 | ✅ |
| 异常契约 | 保持 `Oops` | 未改异常 | ✅ |
| 嵌套事务 | 无嵌套副作用 | Save/Delete 不互相调用 | ✅ |

- 运行时集成测试：⚠️ NEED EVIDENCE / ENVIRONMENT LIMITATION（无可用 SQL Server 集成环境直接触发 DB 异常回滚验证，未伪造结果）

## ④ 事务语义复验（async 正确性）
- `UnitOfWorkAttribute` 为 `IAsyncActionFilter` + `IOrderedFilter`，`OnActionExecutionAsync` 内 `await next()` 包裹整个 `async Task` Action
- 项目已在 `SqlSugarConfigureExtensions.cs:54` 执行 `services.AddUnitOfWork<SqlSugarUnitOfWork>()`，`SqlSugarUnitOfWork` 内部 `AsTenant().BeginTran/CommitTran/RollbackTran`
- 验证：`[UnitOfWork]` 确实作用于 `async Task Save/Delete`，不会“编译通过但 AOP 未包住 await”

## ⑤ 提交纯度
- `git status --short` 仅 `M OrderService.cs`（backend 维度仅此 1 file）
- 未改：Repository/框架/F-T3/缓存/异常体系/格式化/其他类/MASTER/L1/L2 时序
- 下一步：`git add backend/modularity/extend/JNPF.Extend/OrderService.cs` + evidence 单提交

## 边界声明
- 本 Fix 仅恢复 **DB 一致性边界**（Order/明细/收款/FlowTask 同事务），**不解决文件/缓存跨资源原子性**（F-T3 另案）
- Fix Budget：`+1 using +2 attributes`，符合“最小语义变更预算”规则
