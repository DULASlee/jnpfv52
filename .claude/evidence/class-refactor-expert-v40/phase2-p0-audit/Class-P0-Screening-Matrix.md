# Class-by-Class P0 Screening Matrix — 10 维度统一排查

> 维度：结构/职责 | 生命周期 | 异常 | 并发 | 数据访问 | 性能 | 资源 | 缓存 | 安全 | 可维护性  
> 状态：`CLOSED/NO FINDING` 或 Finding 带风险；禁止强行找优化点

## 1. EmailService `EmailService.cs:122 Delete`

| 维度 | 排查问题 | 当前证据 | Finding | 风险 | 状态 |
|---|---|---|---|---|---|
| 结构/职责 | 职责过载？ | Delete 单一职责 | — | — | CLOSED |
| 生命周期 | 创建/拥有/释放 | 1.5章三问：创建者=Delete方法，拥有者=Delete方法，已改 `new AppFriendlyException(...,ex)` 保栈 | Already Mitigated Golden#1 | — | CLOSED |
| 异常 | 异常语义/保栈 | `catch(Exception ex){Rollback; throw new(...ex)}` + `Oops.Text` 保码 | Already Mitigated | — | CLOSED |
| 并发 | 共享状态/竞态 | 无静态共享，无并发写 | — | — | CLOSED |
| 数据访问 | N+1/事务 | 单事务内 Delete 无 N+1 | — | — | CLOSED |
| 性能 | 大结果集/LOH | 无热路径，无大分配 | — | — | CLOSED |
| 资源 | Stream/File | 无资源持有 | — | — | CLOSED |
| 缓存 | 生命周期 | 非缓存类 | — | — | CLOSED |
| 安全 | 注入/权限 | 无 SQL 拼接，无路径 | — | — | CLOSED |
| 可维护性 | 重复/复杂度 | 单一异常路径，无重复 | — | — | CLOSED |

> 结论：**Converged**，1 Already Mitigated，无 Residual/Regression。

## 2. FileService `FileService.cs`

### 2a UploadFileByType:446

| 维度 | 排查问题 | 当前证据 | Finding | 风险 | 状态 |
|---|---|---|---|---|---|
| 结构/职责 | 职责过载？ | UploadFileByType 单一上传 | — | — | CLOSED |
| 生命周期 | 创建/拥有/释放 | `using var file = new FileStream(...); await Upload(file)` 三问通过 | Already Mitigated Golden#2 | — | CLOSED |
| 异常 | 异常语义 | 无吞栈，正常传播 | — | — | CLOSED |
| 并发 | 共享状态 | 局部 file，无共享 | — | — | CLOSED |
| 数据访问 | N+1/事务 | 单次上传，无 DB N+1 | — | — | CLOSED |
| 性能 | LOH/热路径 | 无热路径 | — | — | CLOSED |
| 资源 | Stream/File | 已确定性释放 | Already Mitigated | — | CLOSED |
| 缓存 | — | 无 | — | — | CLOSED |
| 安全 | 注入/路径 | 路径由 `FileManager` 托管，非拼接 | — | — | CLOSED |
| 可维护性 | — | 单点 using | — | — | CLOSED |

### 2b FileDown:193

| 维度 | 排查问题 | 当前证据 | Finding | 风险 | 状态 |
|---|---|---|---|---|---|
| 结构/职责 | 职责过载？ | FileDown 单一下载 | — | — | CLOSED |
| 生命周期 | 创建/拥有/释放 | `using var fs = fileStreamResult.FileStream` 替代 `Close()` | Already Mitigated Golden#3 | — | CLOSED |
| 异常 | 异常语义 | 无吞栈 | — | — | CLOSED |
| 并发 | 共享状态 | 局部 fs | — | — | CLOSED |
| 数据访问 | N+1 | 单次 DownloadFileByType | — | — | CLOSED |
| 性能 | LOH `new byte[fs.Length]` | 无 BDN 证据，静态非热路径 | NEED EVIDENCE | Low | NEED EVIDENCE |
| 资源 | Stream | 已释放 | Already Mitigated | — | CLOSED |
| 缓存 | — | 无 | — | — | CLOSED |
| 安全 | 路径遍历 | 仍存 `Path.Combine dir+@→.` 未规范化，跨类白名单所需 | STOP | Medium | STOP |
| 可维护性 | — | 可接受 | — | — | CLOSED |

### 2c DownloadAll:240

| 维度 | 排查问题 | 当前证据 | Finding | 风险 | 状态 |
|---|---|---|---|---|---|
| 结构/职责 | 职责过载？ | DownloadAll 打包+压缩 | — | — | CLOSED |
| 生命周期 | 创建/拥有/释放 | 临时目录 `DownloadAll`创建→`DownloadFile`消费，跨层异步 | Residual F-L3 | Medium | **STOP** 跨层 ownership |
| 异常 | 异常语义 | 中间失败无清理，孤儿目录 | Residual | Medium | STOP |
| 并发 | 共享状态 | 临时目录名随机，无共享 | — | — | CLOSED |
| 数据访问 | N+1 | 无 DB | — | — | CLOSED |
| 性能 | 压缩 | 非热路径 | — | — | CLOSED |
| 资源 | 临时目录 | 创建者≠最终释放者，需全局清理 | Residual | Medium | STOP |
| 缓存 | `CACHEKEYSCHEDULE` 等 | 缓存键在 DownloadAll 未直接相关 | — | — | CLOSED |
| 安全 | 路径 | 同 FileDown | STOP | Medium | STOP |
| 可维护性 | 重复 CopyFile | 循环 Copy 可接受 | — | — | CLOSED |

> FileService 结论：**Converged (with known STOPs)**，2 Already Mitigated，2 STOP 跨层/路径，1 NEED EVIDENCE LOH，无 Regression。

## 3. OrderService `OrderService.cs:198 Save / 247 Delete`

| 维度 | 排查问题 | 当前证据 | Finding | 风险 | 状态 |
|---|---|---|---|---|---|
| 结构/职责 | 职责过载？ | Save/Delete 聚焦订单聚合，未超 God | — | — | CLOSED |
| 生命周期 | 创建/拥有/释放 | Save/Delete 加 `[UnitOfWork]`，AOP 已注册 `SqlSugarConfigureExtensions.cs:54` | Already Mitigated Golden#4 | — | CLOSED(DEFERRED RT) |
| 异常 | 异常语义 | `AOP OnActionExecutionAsync await next()` → Exception→Rollback，未改 `Oops` | Already Mitigated | — | CLOSED |
| 并发 | 共享状态/竞态 | 共享：`_cacheManager` `CommonConst.CACHEKEYBILLRULE` 唯账单号场景；无锁/乐观锁 | Residual 可能 | Low | **NEED EVIDENCE** (并发模型未实测) |
| 数据访问 | N+1/事务 | 原多步 Delete/Insert/Update 已同事务；仍存 `IsAny`→`Deleteable` 序列，但无 N+1 循环 | Already Mitigated(事务) | — | CLOSED |
| 性能 | 大结果集 | 无大结果集；N+1 已由事务边界间接约束 | — | — | CLOSED |
| 资源 | Stream/File | Delete 中 `foreach FileJson→DeleteFile` 无 try/finally | Residual | Low | **STOP** 跨资源原子性 |
| 缓存 | 一致性 | `Save` 删除账单缓存，Delete 未清；成功/失败与 DB 事务不同步 | Residual | Low | STOP 跨资源 |
| 安全 | 注入/权限 | 无拼接，所有写经 `Safe*` 或参数化 | — | — | CLOSED |
| 可维护性 | 重复 | Save/Delete 各4-5步 DB，重复但边界内 | — | — | CLOSED |

> 结论：**Converged (Deferred Runtime)**，1 Already Mitigated(Golden#4)，2 Residual(文件/缓存) STOP 跨资源，1 NEED EVIDENCE(并发)，无 Regression 丢失事务。

## 4. ScheduleService `ScheduleService.cs:918`

| 维度 | 排查问题 | 当前证据 | Finding | 风险 | 状态 |
|---|---|---|---|---|---|
| 结构/职责 | 职责过载？ | 1469行，7职责族但均为请求级，经 Gate 不拆 | STOP收益不足 | Medium | **STOP** |
| 生命周期 | 创建/拥有/释放 | `GetCalendarDayPushList 923 / AddPushTaskQueue 954` 均 `using var scoped` 正确 | **False Positive** 原 F-L1关闭 | — | CLOSED |
| 异常 | 异常语义 | 全 `Oops.Oh` 无泄露，无空吞 | **False Positive** | — | CLOSED |
| 并发 | 共享状态/竞态 | 无共享字段/静态，重复日程并发未实测 | NEED EVIDENCE? | Low | **STOP** (收益有限) |
| 数据访问 | N+1 | Delete 809/841/852 循环查 `ScheduleUser` N+1 形态成立 | **Residual** F-P1 | Medium | **NEED EVIDENCE** 实测收益 |
| 性能 | 大结果集 | GetList 无分页 `ToListAsync()` | STOP 缺运行时证据 | Medium | STOP/冻结 |
| 资源 | TaskQueue/Cache | TaskQueue 回调 scoped 正确 | CLOSED | — | CLOSED |
| 缓存 | 过期 | `CacheManager.Set` 有 TTL | — | — | CLOSED |
| 安全 | 权限 | 权限过滤由框架统一 | — | — | CLOSED |
| 可维护性 | 重复 | 重复日程 4 case 分支重复 | STOP 单类收益不足 | Low | STOP |

> 结论：**Needs Review (1 Residual NEED EVIDENCE)**，无 Regression。

## 5. Phase1 安全硬化 5 类 (JsonHelper/UserManager/ConfigController/DataInterfaceService/BatchDeleteSqlPlanner)

| 维度 | 排查问题 | 当前证据 | Finding | 风险 | 状态 |
|---|---|---|---|---|---|
| 结构 | — | 5文件均为安全加固，无职责过载 | — | — | CLOSED |
| 生命周期 | — | 无资源 | — | — | CLOSED |
| 异常 | — | 无新增异常 | — | — | CLOSED |
| 并发 | — | 无共享 | — | — | CLOSED |
| 数据访问 | — | BatchDelete 加 `SanitizeId` 去单引号 | Already Mitigated J1 | — | CLOSED |
| 性能 | — | 无热路径 | — | — | CLOSED |
| 资源 | — | 无 | — | — | CLOSED |
| 缓存 | — | 无 | — | — | CLOSED |
| 安全 | J5/J1 | `SafeSettings TypeNameHandling.None` + `JsonHelper.ToObject` + Sanitize | Already Mitigated 6文件 | High→Closed | **Already Mitigated** |
| 可维护性 | — | 27/27 测试 新增 | — | — | CLOSED |

> 结论：**Converged**，安全维度已解决，无新 Finding。

---

> Coverage：5类 ×10维 =50格 100% 有结果；无维度强行产生 Finding，CLOSED 明确记录。
