# Golden Example #4 — Business Transaction / UnitOfWork Boundary

> Commit: `339689af`  
> Finding: F-T1/F-T2 合并 → OrderService 缺少 UnitOfWork  
> Fix: `OrderService.cs` +1 `using JNPF.DatabaseAccessor;` +2 `[UnitOfWork]` (Save/Delete 方法级)  
> Gate: ` .claude/evidence/class-refactor-expert-v40/order-uow-gate/UnitOfWork-Gate-Pack.md` → GO

## 验证分级（显式标记完整度）

| 维度 | 状态 |
|---|---|
| Implementation | VERIFIED |
| Code-level semantics (AOP 注册 + async 参与 + FlowTask 同事务) | VERIFIED |
| Build (模块/全量) | VERIFIED (模块0错) |
| Runtime rollback (集成环境异常→回滚) | **DEFERRED / ENVIRONMENT BLOCKED — no claim of runtime verification** |

> 工程师未伪造数据库故障回归，诚实记录环境限制。Decision 可 Close，运行时证据待补。

## Fix 纹理

- DB 一致性边界：Order + OrderEntry + OrderReceivable + FlowTask 同一 `SqlSugarUnitOfWork.AsTenant().BeginTran/CommitTran/RollbackTran` (`SqlSugarUnitOfWork.cs:31` / `SqlSugarConfigureExtensions.cs:54`)
- 外部副作用：`_fileManager.DeleteFile` / `_cacheManager.Del` 明确不在 DB 事务内（F-T3 另案）
- 预算：`+3 insertions` 仅 `OrderService.cs:7` / `OrderService.cs:199` / `OrderService.cs:250`

## 与 #1-#3 关系

- #1 Exception Semantics — Preserve Cause
- #2 Resource Lifetime / UploadFileByType `using var`
- #3 Resource Lifetime / FileDown `using var`（#2/#3 同性质异场景）
- #4 **Business Transaction / UnitOfWork Boundary**（异质技术性质）

> GO / STOP / NEED EVIDENCE 三种决策已有真实案例：#2/#3 GO，F-L3 STOP，Schedule F-P1 NEED EVIDENCE，#4 GO(Deferred Runtime)
