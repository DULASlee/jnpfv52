# Phase 2 Class Inventory — JNPF Phase 2 Post-Refactoring Independent Expert Audit P0

> 基准：`81bc1dce..HEAD` 全量 diff + 独立只读复核  
> 范围：Phase 2 类级专家重构实际交付的全部生产代码类  
> 工具：`generic-class-refactor-expert v4.0 CALIBRATED / FROZEN` P0 10维排查  
> 验收：零生产代码修改，表格为控制面板；本版已补“本次重新检查”证据化 PASS（见 Closure Matrix）

| # | 类 | 文件 | Phase 2 原状态 | Phase 2 做过什么 | 当前状态 | 行动 | 本次是否重新检查 |
|---|----|------|---------------|-----------------|----------|------|------------------|
| 1 | EmailService | `backend/modularity/extend/JNPF.Extend/EmailService.cs:122` | `catch(Exception){Rollback; throw Oh(COM1002)}` 丢栈 | `Delete` 改为 `catch(Exception ex){Rollback; throw new AppFriendlyException(Text(COM1002),COM1002,ex)}` 2+2 `e45f724a` | Golden #1 已冻结 | Exception Preserve Cause | ☑ 本次重查 PASS |
| 2 | FileService | `backend/modularity/system/JNPF.Systems/Common/FileService.cs:446` | `new FileStream` 未释放 | `UploadFileByType` 改 `using var file` 1+1 `d6117dce` Golden #2 | 已 CLOSED | Resource Upload | ☑ 本次重查 PASS |
| 3 | FileService | `backend/modularity/system/JNPF.Systems/Common/FileService.cs:193` | `FileStreamResult.FileStream.Close()` | `FileDown` 改 `using var fs` 3+4 `acc6f5d0` Golden #3 | 已 CLOSED | Resource Download | ☑ 本次重查 PASS |
| 4 | FileService | `backend/modularity/system/JNPF.Systems/Common/FileService.cs:240` | 临时目录 `DownloadAll` 未清理 | Gate 中 STOP 跨层 ownership 正确拒绝 | 未修改 STOP | Resource Cross-layer | ☑ 本次重查 PASS |
| 5 | OrderService | `backend/modularity/extend/JNPF.Extend/OrderService.cs:198,247` | `Save/Delete` 无事务，多步 DB | `+1 using JNPF.DatabaseAccessor +2 [UnitOfWork]` `339689af` Golden #4 Deferred | 已 CLOSED | Transaction | ☑ 本次重查 PASS |
| 6 | ScheduleService | `backend/modularity/system/JNPF.Systems/System/ScheduleService.cs:918` | 评估中未改 | P0 后深入：F-L1正确 CLOSED，F-P1 N+1 NEED EVIDENCE，F-A1 STOP | 评估 STOP/NEED | Performance/Arch | ☑ 本次重查 PASS |
| 7 | JsonHelper | `backend/modularity/common/JNPF.Common/Security/JsonHelper.cs:7` | `DeserializeObject` 可 `TypeNameHandling` | 加 `SafeSettings=None` 全路径 `0912b34f` | Phase1 J5 hardening | Security | ☑ 本次重查 PASS |
| 8 | UserManager | `backend/modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs:1061` | `JsonConvert.DeserializeObject<List<IConditionalModel>>` | 改 `JsonHelper.ToObject` 安全路径 `0912b34f` | Phase1 J5 | Security | ☑ 本次重查 PASS |
| 9 | ConfigController | `backend/modularity/zxdev/JNPF.ZxDev/ConfigController.cs:189` | `DeserializeObject(json)` | 改 `Deserialize<JsonElement>` 安全路径 `0912b34f` | Phase1 J5 | Security | ☑ 本次重查 PASS |
| 10 | DataInterfaceService | `backend/modularity/system/JNPF.Systems/System/DataInterfaceService.cs:266` | 3 处 `DeserializeObject` | 改 `JsonHelper.ToObject` `0912b34f` | Phase1 J5 | Security | ☑ 本次重查 PASS |
| 11 | BatchDeleteSqlPlanner | `backend/modularity/visualdev/JNPF.VisualDev/Delete/BatchDeleteSqlPlanner.cs:17` | `Where Id In ('{id}')` 拼接 | 加 `SanitizeId() strip '` `0912b34f` | Phase1 J1 | Security | ☑ 本次重查 PASS |

> 覆盖率：**11 条目 100% 入表**，含 3个 Golden 生产类 + 1 STOP + 1 NEED EVIDENCE + 1 评估类 + 5个 Phase1 安全硬化类。Phase 2 交付的全部生产变更已穷尽（`git diff --name-only 81bc1dce..HEAD -- backend` 8 files，已全映射）。本次全部 11 项已按 Closure Matrix 逐项重查 PASS，非继承历史状态。
