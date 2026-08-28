# Finding Inventory — 独立重分类（Recheck 2026-08-28）

> §五 互斥 Finding-level 分类：`ALREADY_MITIGATED / FALSE_POSITIVE / RESIDUAL / REGRESSION / NEW_FINDING / STOP / NEED_EVIDENCE`
> 为不混淆两个语义层，本表分两列：**Nature（本质）** 与 **Disposition（处置决策）**。
> §七 要求：Nature 统计与 Disposition 统计分别列出，不得合并成一个表。
> Regression≠Residual；NEED_EVIDENCE≠STOP；Already Mitigated≠No Review（每条均有当前代码证据）。

| ID | 类 | 维度 | Finding | 原分类(上轮) | 当前证据(file:line) | 风险 | Nature | Disposition |
|----|----|------|---------|-------------|--------------------|------|--------|-------------|
| M-01 | EmailService | Exception | catch 丢栈 | Already Mitigated | L145-148 `throw new AppFriendlyException(...,ex)` | — | ALREADY_MITIGATED | CLOSED |
| M-02 | FileService Upload | Resource | FileStream 未 using | Already Mitigated | L447 `using var file` | — | ALREADY_MITIGATED | CLOSED |
| M-03 | FileService FileDown | Resource | FileStream.Close | Already Mitigated | L201 `using var fs` | — | ALREADY_MITIGATED | CLOSED |
| M-04 | OrderService | Data/Transaction | Save/Delete 无事务 | Already Mitigated | L200/L250 `[UnitOfWork]` | — | ALREADY_MITIGATED | CLOSED (Runtime DEFERRED / ENV BLOCKED) |
| M-05a | JsonHelper | Security | TypeNameHandling 反序列化 | Already Mitigated | L14 `SafeSettings=None` | — | ALREADY_MITIGATED | CLOSED |
| M-05b | UserManager | Security | 不安全 DeserializeObject | Already Mitigated | L1064/1079 安全 ToObject | — | ALREADY_MITIGATED | CLOSED |
| M-05c | ConfigController | Security | 不安全 DeserializeObject | Already Mitigated | L192/236 安全 ToObject（**偏差修正**） | — | ALREADY_MITIGATED | CLOSED |
| M-05d | DataInterfaceService | Security | 不安全 DeserializeObject | Already Mitigated | L1945/1947/1957 | — | ALREADY_MITIGATED | CLOSED |
| M-05e | BatchDeleteSqlPlanner | Security | id 拼接 SQL 注入 | Already Mitigated | L20/L42 `SanitizeId` | — | ALREADY_MITIGATED | CLOSED |
| FP-01 | ScheduleService | Resource | DI scope 未释放（疑） | False Positive | `using var scoped` 正确 | — | FALSE_POSITIVE | CLOSED |
| FP-02 | ScheduleService | Exception | Oops 泄露（疑） | False Positive | 全 `Oops.Oh` 规范 | — | FALSE_POSITIVE | CLOSED |
| R-01 | FileService | Security | 路径未 `GetFullPath`+前缀校验 | New Finding | L169 / L248 | Medium | RESIDUAL | STOP（跨层白名单/契约扩张） |
| R-02 | FileService FileDown | Performance | `byte[fs.Length]` LOH | New Finding | L201 区域，无 BDN | Low | RESIDUAL | NEED_EVIDENCE（缺运行时基准） |
| R-03 | FileService DownloadAll | Resource | 临时目录跨层 ownership | Residual | L240-249 无 finally 清理 | Medium | RESIDUAL | STOP |
| R-04 | FileService DownloadAll | Resource | 临时目录异常未清理 | Residual | 同 | Medium | RESIDUAL | STOP |
| R-05 | OrderService | Concurrency | 无乐观锁并发覆盖 | New Finding | L201 Save | Low | RESIDUAL | NEED_EVIDENCE（缺并发实测） |
| R-06 | OrderService Delete | Resource | 文件删除非事务原子 | Residual | L268 DeleteFile 事务外 | Low | RESIDUAL | STOP（跨资源） |
| R-07 | OrderService | Cache | 缓存与 DB 事务不同步 | Residual | L237 `_cacheManager.Del` | Low | RESIDUAL | STOP |
| R-08 | ScheduleService Delete | Data Access | N+1 循环查 ScheduleUser | Residual(F-P1) | L398/L722 foreach 内查询 | Medium | RESIDUAL | NEED_EVIDENCE（区间未改代码，缺量级证据） |
| **R-09** | BatchDeleteSqlPlanner | Security/Maint. | 黑名单 `Replace("'","")` + `mainPrimary/table/tableField` 内插 | **(本轮新发现)** | L22-23/L46-55/L67 | Medium(条件性) | **NEW_FINDING** | STOP-for-now（Phase1 基线，越本轮范围；登记 decision candidate） |

> 注：R-02 上轮记 "New Finding"，本轮独立判定其本质为已存在方法的残留性能风险（区间内 FileDown 被改），改归 RESIDUAL/NEED_EVIDENCE。

## Nature 统计（互斥 · Finding-level）

| Nature | 计数 | 明细 |
|--------|------|------|
| ALREADY_MITIGATED | 9 | M-01,02,03,04,05a-e |
| FALSE_POSITIVE | 2 | FP-01,02 |
| RESIDUAL | 7 | R-01,02,03,04,05,06,07,08 → 去 R-09 |
| REGRESSION | **0** | — |
| NEW_FINDING | 1 | R-09 |

（R-08 与 R-01..07 合计 RESIDUAL = 8；其中 R-02/R-05/R-08 disposition=NEED_EVIDENCE，R-01/03/04/06/07 disposition=STOP。）

## Disposition 统计（互斥 · 决策层）

| Disposition | 计数 | 明细 |
|-------------|------|------|
| CLOSED（已缓解/误报） | 11 | M-01..05e(9) + FP-01,02(2) |
| STOP（证据支持"现在不该做"） | 6 | R-01,03,04,06,07,09 |
| NEED_EVIDENCE（证据不足，冻结） | 3 | R-02,05,08 |
| REGRESSION 修复 | 0 | — |

**关键判定：REGRESSION = 0** —— 9 项缓解全部在当前树原样在位，未被 Phase 1 安全批量提交(0912b34f)或后续 skill 提交回归。**High Risk 未控 = 0**（残留均为 Medium/Low 且已带决策冻结）。
