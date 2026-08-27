# 重构后质量判定表 — Phase 2 Post-Refactoring Quality Matrix

> 验收主表：每类一行，可直接读结论

| 类 | Phase 2 状态 | 当前 Finding 数 | 高风险 | 已解决 | Residual | Regression | STOP | NEED EVIDENCE | 当前结论 |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| EmailService | CLOSED Golden#1 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | **Converged** |
| FileService Upload | CLOSED Golden#2 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | **Converged** |
| FileService Download | CLOSED Golden#3 | 1 | 0 | 1 | 0 | 0 | 1(J1路径) | 1(LOH) | **Converged (known STOPs)** |
| FileService DownloadAll | STOP | 2 | 0 | 0 | 2 | 0 | 2 | 0 | **Converged (correctly STOP)** |
| OrderService | CLOSED Golden#4 Deferred | 2 | 0 | 1 | 2 | 0 | 2 | 1(并发) | **Converged (Deferred Runtime)** |
| ScheduleService | NEED EVIDENCE | 1 | 0 | 0 | 1 | 0 | 3 | 1 | **Needs Review** |
| JsonHelper 等5安全类 | CLOSED Phase1 | 0 | 0 | 5 | 0 | 0 | 0 | 0 | **Converged** |
| **合计** | — | **6** | **0** | **8** | **5** | **0** | **8** | **3** | — |

> 解读：高风险 0，Regression 0，Residual 5 均为已知的 STOP/NEED 边界内问题（跨层 ownership/跨资源原子性/路径白名单/并发），无遗漏高风险；唯一 Needs Review 是 Schedule F-P1 需实测。
