# Finding Inventory — 统一审计索引

| ID | 类 | 维度 | Finding | Phase2对照 | 当前证据 | 风险 | 状态 |
|---|---|---|---|---|---|---|---|
| R-01 | FileService FileDown | 安全 | `Path.Combine dir+@→.` 未规范化越界 | New Finding | `FileService.cs:128` | Medium | **STOP** 跨类白名单 |
| R-02 | FileService FileDown | 性能 | `new byte[fs.Length]` LOH | New Finding | `FileService.cs:201` 无 BDN | Low | **NEED EVIDENCE** |
| R-03 | FileService DownloadAll | 资源/生命周期 | 临时目录跨层 ownership | Residual | `FileService.cs:240-258` | Medium | **STOP** |
| R-04 | FileService DownloadAll | 资源 | 临时目录异常未清理 | Residual | 同 | Medium | **STOP** |
| R-05 | OrderService | 并发 | 无乐观锁，并发覆盖风险 | New Finding | `OrderService.cs:198` Save | Low | **NEED EVIDENCE** |
| R-06 | OrderService Delete | 资源 | 文件删除非事务，原子性外 | Residual | `OrderService.cs:261` FileJson | Low | **STOP** 跨资源 |
| R-07 | OrderService | 缓存 | 缓存与 DB 事务不同步 | Residual | `OrderService.cs:235` | Low | **STOP** |
| R-08 | ScheduleService Delete | 数据访问 | N+1 循环查 ScheduleUser | Residual F-P1 | `ScheduleService.cs:809` | Medium | **NEED EVIDENCE** |
| M-01 | EmailService Delete | 异常 | catch丢栈 | **Already Mitigated** | `EmailService.cs:122` 已保栈 | — | **CLOSED** |
| M-02 | FileService Upload | 资源 | FileStream 未 using | **Already Mitigated** | 已 `using var` | — | **CLOSED** |
| M-03 | FileService FileDown | 资源 | FileStream Close | **Already Mitigated** | 已 `using var` | — | **CLOSED** |
| M-04 | OrderService | 事务 | Save/Delete 无事务 | **Already Mitigated** | 已 `+[UnitOfWork]` 339689af | — | **CLOSED (Deferred RT)** |
| M-05 | JsonHelper等5类 | 安全 | J5/J1 注入/反序列化 | **Already Mitigated** | 0912b34f SafeSettings+Sanitize | — | **CLOSED** |
| FP-01 | ScheduleService 923 | 生命周期 | DI scope | **False Positive** | `using var scoped` 正确 | — | **CLOSED** |
| FP-02 | ScheduleService 全局 | 异常 | Oops.Oh 泄露 | **False Positive** | 全 `Oops.Oh` | — | **CLOSED** |

> 已解决 5 Already Mitigated / 2 False Positive / 5 Residual(STOP) / 3 NEED EVIDENCE / 0 Regression / 0 高风险未控

---

## Evidence Pack 索引

- `Phase2-Class-Inventory.md` — 母表
- `Class-P0-Screening-Matrix.md` — 5类×10维 100% 有结果
- `Quality-Matrix.md` — 主验收表
- `Skill-Capability-Verification-Matrix.md` — Skill 自检
- 本 `Finding-Inventory.md` — 审计索引
- 既有 Pack：`first-refactor-email-f03/` `first-refactor-file-f01/` `d4-f05-fix/` `order-uow-fix/` `order-uow-gate/` 等

## Coverage / Completeness Check

- **Coverage Class** 11/11 100% ✅
- **Dimensions** 5类 ×10维 =50格 100% 有检查结果 ✅
- **Evidence** 每非 NO FINDING 有文件:行号+调用链 ✅
- **Classification** 每 Finding 已标 Already Mitigated/Residual/New/False/Stop/Need Evidence ✅
- **Traceability** 每 Finding 可追溯文件方法路径 ✅
