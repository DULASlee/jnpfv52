# Phase 2 Class Inventory — Independent Re-model (P0 Recheck 2026-08-28)

> 审计基准：`81bc1dce..HEAD`（HEAD=b3b8acde）全量 diff，独立只读复核
> 工具：`generic-class-refactor-expert v4.0 CALIBRATED / FROZEN` P0 10 维排查
> 选类依据：**git diff 驱动，不凭历史印象**。每条映射到当前树 file:line。
> 本轮性质：P0 全量取证 + 状态重新建模，**零生产代码修改**。

## A. 生产代码变更类（区间内有 diff，权威范围）

`git diff --name-status 81bc1dce..HEAD -- backend` → 7 源文件 + 3 测试文件。

| # | Class | File（当前树） | Phase 2 变更 | 触发提交 | 原始 Finding | 当前复核（file:line） | Rechecked |
|---|-------|---------------|-------------|----------|--------------|----------------------|-----------|
| 1 | FileService | `.../JNPF.Systems/Common/FileService.cs` | `UploadFileByType` → `using var file` | d6117dce (Golden#2) | Resource Lifetime: FileStream 未释放 | L447 `using var file = new FileStream(...)` 在位 | ✅ |
| 2 | FileService | 同上 | `FileDown` → `using var fs` | acc6f5d0 (Golden#3) | Resource Lifetime: FileStream.Close | L201 `using var fs = fileStreamResult.FileStream;` 在位 | ✅ |
| 3 | OrderService | `.../JNPF.Extend/OrderService.cs` | `Save/Delete` +`[UnitOfWork]` | 339689af (Golden#4) | 事务边界：多步 DB 无事务 | L200/L250 `[UnitOfWork]`、L10 `using JNPF.DatabaseAccessor` 在位 | ✅ |
| 4 | JsonHelper | `.../JNPF.Common/Security/JsonHelper.cs` | 反序列化加 `SafeSettings` | 0912b34f (J5) | Security: TypeNameHandling 反序列化 | L14 `SafeSettings = TypeNameHandling.None`，各 ToObject 走之 | ✅ |
| 5 | UserManager | `.../JNPF.Common.Core/Manager/User/UserManager.cs` | `DeserializeObject<List<IConditionalModel>>`→安全路径 | 0912b34f (J5) | Security | L1064/L1079 `JsonHelper.ToObject<List<IConditionalModel>>(...)` | ✅ |
| 6 | DataInterfaceService | `.../JNPF.Systems/System/DataInterfaceService.cs` | 多处 `DeserializeObject`→安全 `ToObject` | 0912b34f (J5) | Security | L1945/1947/1957 `JsonHelper.ToObject<...>` | ✅ |
| 7 | BatchDeleteSqlPlanner | `.../JNPF.VisualDev/Delete/BatchDeleteSqlPlanner.cs` | id 拼接加 `SanitizeId()` | 0912b34f (J1) | Security: SQL 注入 | L20/L42 `ids.Select(SanitizeId)`，L67 `Replace("'","")` | ✅ |
| 8 | ConfigController | `.../JNPF.ZxDev/ConfigController.cs` | `DeserializeObject`→安全路径 | 0912b34f (J5) | Security | **偏差**：现 L192/236 `JsonHelper.ToObject<object>/JArray`（旧清单误记 `Deserialize<JsonElement>`） | ✅ |
| — | (tests) | `JsonHelperSafetyTests.cs` / `SqlGuardTests.cs` / `WechatMiniProgramServiceSecretTests.cs` | 新增 | 0912b34f | Test | 3 测试文件在位；`WechatMiniProgram...` 无对应区间内生产改动（observation） | ✅ |

## B. 审计范围内但区间内无生产 diff（分析过 / 冻结于基线前）

| # | Class | File | 状态 | 当前复核 | Rechecked |
|---|-------|------|------|----------|-----------|
| 9 | EmailService | `.../JNPF.Extend/EmailService.cs` | Golden#1，修复提交 `e45f724a` **早于基线 81bc1dce**（区间内无 diff） | L145-148 `catch(Exception ex){RollbackTran; throw new AppFriendlyException(...,ex)}` 保栈**仍在位**，未被后续提交回归 | ✅ |
| 10 | ScheduleService | `.../JNPF.Systems/System/ScheduleService.cs` | P0 后 STOP/NEED EVIDENCE，**未改代码** | L398/L722 `foreach` 内查询 → N+1 形态仍在（R-08，区间无 diff）；FP-01/FP-02 复核仍为误报 | ✅ |

## C. 覆盖率与完整性

- **区间内生产变更文件**：7 源 + 3 测试 = 10 → **100% 入表**（A 段）。
- **历史审计条目**（Email/Golden#1、Schedule/评估）保留在 B 段以完成原始 Finding 重验，但明确标注**非区间内生产变更**。
- 偏差登记：① ConfigController 缓解形式与旧清单描述不一致（已按当前树修正）；② `WechatMiniProgramServiceSecretTests` 在区间内无配对生产改动。
- 本表与旧 `phase2-p0-audit/Phase2-Class-Inventory.md` 并存（版本链保留，未覆盖）。
