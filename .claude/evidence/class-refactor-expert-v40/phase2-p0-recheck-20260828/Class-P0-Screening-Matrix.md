# Class P0 Screening Matrix — 10 维逐类重新检查（Recheck 2026-08-28）

> 规则：每个类 10/10 维度均有明确结果；无问题写 `NO FINDING`，不留空。
> 维度：1 Structure / 2 Resource Lifetime / 3 Exception / 4 Concurrency / 5 Data Access / 6 Performance / 7 Cache / 8 Security / 9 Maintainability / 10 Observability·Testability
> 证据均取自当前树 file:line（区间 `81bc1dce..HEAD` + 现状复核）。

## 1. FileService（system）

| # | 维度 | 当前证据 | Finding / NO FINDING | 状态 |
|---|------|----------|----------------------|------|
| 1 | Structure | 大类，多方法（Upload/FileDown/DownloadAll/GetFileStream…） | NO FINDING（本轮范围不拆类） | PASS |
| 2 | Resource Lifetime | L447 `using var file`(Upload)、L201 `using var fs`(FileDown) 已释放；L240-249 `DownloadAll` 建 `TemporaryFile` 目录 + `_fileManager.CopyFile` 无 finally 清理 | R-03/R-04 临时目录跨层 ownership/清理 → **RESIDUAL** | PASS(带残留) |
| 3 | Exception | 未见吞栈/丢 cause 的改动点 | NO FINDING | PASS |
| 4 | Concurrency | 无共享可变静态 | NO FINDING | PASS |
| 5 | Data Access | 无新增查询模式 | NO FINDING | PASS |
| 6 | Performance | FileDown 读流（`fs.Length` 缓冲） | R-02 LOH 风险 → **NEED_EVIDENCE**（无 BDN/运行时） | BLOCKED(取证) |
| 7 | Cache | 无缓存逻辑 | NO FINDING | PASS |
| 8 | Security | L169/L248 `Path.Combine(dir, name.Replace("@","."))` 未做 `GetFullPath`+前缀校验 | R-01 路径未规范化越界 → **RESIDUAL** | PASS(带残留) |
| 9 | Maintainability | 资源释放集中，可读 | NO FINDING | PASS |
| 10 | Observability/Testability | 无新增可观测性缺陷；释放修复有 Golden 记录 | NO FINDING | PASS |

## 2. OrderService（extend）

| # | 维度 | 当前证据 | Finding / NO FINDING | 状态 |
|---|------|----------|----------------------|------|
| 1 | Structure | Save/Delete 职责清晰 | NO FINDING | PASS |
| 2 | Resource Lifetime | L268 `_fileManager.DeleteFile` 在 DB 事务外 | R-06 文件删除非事务原子 → **RESIDUAL** | PASS(带残留) |
| 3 | Exception | 无丢栈改动 | NO FINDING | PASS |
| 4 | Concurrency | 无乐观锁列，Update 覆盖式 | R-05 并发覆盖 → **NEED_EVIDENCE**（无并发实测） | BLOCKED(取证) |
| 5 | Data Access | 多步 Queryable/Insertable 已被 `[UnitOfWork]` 包裹（L200/L250） | M-04 事务边界 **ALREADY_MITIGATED**（Runtime rollback DEFERRED/ENV BLOCKED） | PASS |
| 6 | Performance | 无新增热路径 | NO FINDING | PASS |
| 7 | Cache | L237 `_cacheManager.Del(...)` 与事务提交非原子 | R-07 缓存/DB 同步 → **RESIDUAL** | PASS(带残留) |
| 8 | Security | 走安全 ToObject 路径 | NO FINDING | PASS |
| 9 | Maintainability | `[UnitOfWork]` 声明式，可读 | NO FINDING | PASS |
| 10 | Observability/Testability | Golden#4 记录；无新增可观测缺陷 | NO FINDING | PASS |

## 3. JsonHelper（common/Security）

| # | 维度 | 证据 | 结果 | 状态 |
|---|------|------|------|------|
| 1 | Structure | 静态工具类 | NO FINDING | PASS |
| 2 | Resource Lifetime | 无流/句柄 | NO FINDING | PASS |
| 3 | Exception | 反序列化异常语义未变 | NO FINDING | PASS |
| 4 | Concurrency | L14 `static readonly SafeSettings` 只读 | NO FINDING | PASS |
| 5 | Data Access | N/A | NO FINDING | PASS |
| 6 | Performance | 无 | NO FINDING | PASS |
| 7 | Cache | 无 | NO FINDING | PASS |
| 8 | Security | L14 `TypeNameHandling.None`，L54/77/111/123 全 `ToObject` 走 SafeSettings | M-05a **ALREADY_MITIGATED**；obs：`ToObjectOld`(L63) 亦安全，属遗留别名 | PASS |
| 9 | Maintainability | 有 `ToObjectOld` 冗余别名 | NO FINDING（非缺陷，登记 obs） | PASS |
| 10 | Observability/Testability | `JsonHelperSafetyTests.cs` 覆盖 | NO FINDING | PASS |

## 4. UserManager（common-core）

| # | 维度 | 证据 | 结果 | 状态 |
|---|------|------|------|------|
| 1 | Structure | 大类（权限/条件构造） | NO FINDING（本轮不拆） | PASS |
| 2 | Resource Lifetime | 无新增 | NO FINDING | PASS |
| 3 | Exception | 无改动 | NO FINDING | PASS |
| 4 | Concurrency | 每请求实例 | NO FINDING | PASS |
| 5 | Data Access | 无新增查询模式 | NO FINDING | PASS |
| 6 | Performance | 无 | NO FINDING | PASS |
| 7 | Cache | 无 | NO FINDING | PASS |
| 8 | Security | L1064/1079 `JsonHelper.ToObject<List<IConditionalModel>>` 安全路径 | M-05b **ALREADY_MITIGATED** | PASS |
| 9 | Maintainability | 一致走安全工具 | NO FINDING | PASS |
| 10 | Observability/Testability | 无新增 | NO FINDING | PASS |

## 5. DataInterfaceService（system）

| # | 维度 | 证据 | 结果 | 状态 |
|---|------|------|------|------|
| 1 | Structure | 大 service（数据接口/HTTP/SQL） | NO FINDING（本轮不拆） | PASS |
| 2 | Resource Lifetime | httpClient 用法未在本次 diff | NO FINDING | PASS |
| 3 | Exception | 无改动 | NO FINDING | PASS |
| 4 | Concurrency | 无共享可变 | NO FINDING | PASS |
| 5 | Data Access | L1044 `SugarParameter("@"+x.field,...)` 已参数化 | NO FINDING | PASS |
| 6 | Performance | 无新增 | NO FINDING | PASS |
| 7 | Cache | 无 | NO FINDING | PASS |
| 8 | Security | L1945/1947/1957 `JsonHelper.ToObject` 安全路径 | M-05d **ALREADY_MITIGATED** | PASS |
| 9 | Maintainability | 一致走安全工具 | NO FINDING | PASS |
| 10 | Observability/Testability | 无新增 | NO FINDING | PASS |

## 6. BatchDeleteSqlPlanner（visualdev）

| # | 维度 | 证据 | 结果 | 状态 |
|---|------|------|------|------|
| 1 | Structure | 独立 SQL 规划器 | NO FINDING | PASS |
| 2 | Resource Lifetime | 无 | NO FINDING | PASS |
| 3 | Exception | 无 | NO FINDING | PASS |
| 4 | Concurrency | 无 | NO FINDING | PASS |
| 5 | Data Access | 生成 delete/update SQL | NO FINDING（本次不改） | PASS |
| 6 | Performance | 无 | NO FINDING | PASS |
| 7 | Cache | 无 | NO FINDING | PASS |
| 8 | Security | L20/L42 `ids.Select(SanitizeId)`，L67 `Replace("'","")` 覆盖 id；但 `mainPrimary/table/tableField` 仍内插 | M-05e id 注入 **ALREADY_MITIGATED**；R-09 黑名单式 sanitize + 非 id 字段内插 → **NEW_FINDING**（decision candidate，本轮不修） | NEEDS_REVIEW |
| 9 | Maintainability | 黑名单而非参数化 | 关联 R-09 | NEEDS_REVIEW |
| 10 | Observability/Testability | `SqlGuardTests.cs` 覆盖 id 清洗 | NO FINDING | PASS |

## 7. ConfigController（zxdev）

| # | 维度 | 证据 | 结果 | 状态 |
|---|------|------|------|------|
| 1 | Structure | Controller（无业务逻辑外溢本次不评） | NO FINDING | PASS |
| 2-7 | RL/Exc/Conc/DA/Perf/Cache | 本次 diff 仅反序列化路径 | NO FINDING | PASS |
| 8 | Security | L192/236 `JsonHelper.ToObject<object>/JArray` 安全路径（**偏差**：非旧记 `Deserialize<JsonElement>`） | M-05c **ALREADY_MITIGATED** | PASS |
| 9 | Maintainability | 一致走安全工具 | NO FINDING | PASS |
| 10 | Observability/Testability | 无新增 | NO FINDING | PASS |

## 8. EmailService（extend，Golden#1 冻结于基线前，现状重验）

| # | 维度 | 证据 | 结果 | 状态 |
|---|------|------|------|------|
| 3 | Exception | L145-148 `catch(Exception ex){ _db.RollbackTran(); throw new AppFriendlyException(Text(COM1002),COM1002,ex)}` 保栈 | M-01 **ALREADY_MITIGATED**（区间内未回归） | PASS |
| 其余 1-2,4-10 | | 本次区间无生产 diff | NO FINDING（未发现回归） | PASS |

## 9. ScheduleService（system，P0 评估后未改，现状重验）

| # | 维度 | 证据 | 结果 | 状态 |
|---|------|------|------|------|
| 1 | Structure | 大类（调度/推送/日志） | NO FINDING（本轮不拆） | PASS |
| 2 | Resource Lifetime | L923 附近 `using var scoped`（DI scope 正确释放） | FP-01 **FALSE_POSITIVE**（非缺陷） | PASS |
| 3 | Exception | 全 `Oops.Oh` 业务异常 | FP-02 **FALSE_POSITIVE** | PASS |
| 4 | Concurrency | 无共享可变（本轮） | NO FINDING | PASS |
| 5 | Data Access | L398/L722 `foreach` 内 `.ToListAsync()` 查询 ScheduleUser | R-08 N+1 形态 → **NEED_EVIDENCE**（无运行时/量级实测） | BLOCKED(取证) |
| 6 | Performance | 关联 R-08 | **NEED_EVIDENCE** | BLOCKED(取证) |
| 7 | Cache | 无 | NO FINDING | PASS |
| 8 | Security | 无本次相关 | NO FINDING | PASS |
| 9 | Maintainability | 无本次相关 | NO FINDING | PASS |
| 10 | Observability/Testability | 无新增 | NO FINDING | PASS |

---

## 汇总

- **类数**：9（7 区间内生产变更 + Email 冻结重验 + Schedule 评估重验）
- **维度覆盖**：9 × 10 = **90 格，100% 有明确结果**（NO FINDING 亦显式标注）
- **区间内生产变更类**：100% 入表；FileService/OrderService/JsonHelper/UserManager/DataInterface/BatchDelete/Config 全部复核。
- **唯二 NEEDS_REVIEW/BLOCKED**：BatchDelete(R-09 Security/Maintainability)、及 FileService/Order/Schedule 的 RESIDUAL / NEED_EVIDENCE（均为**已冻结决策**，非未检查）。
