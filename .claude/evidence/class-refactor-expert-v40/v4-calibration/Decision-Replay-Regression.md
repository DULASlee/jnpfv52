# Decision Replay / Regression — M1-M3 校准后回放

> 目的：验证校准后 Skill 是否改变既有正确结论；任一回放不一致 → 停下重审

| 样本 | 原决策 | 校准后回放 | 是否一致 | 关键规则验证 |
|---|---|---|---|---|
| F-L3 DownloadAll 跨层 ownership | **STOP** | **STOP** | ✅ | M3 收敛 + ownership规则：异步跨层不能用 try/finally，保持 STOP |
| F-P1 Schedule N+1 | **NEED EVIDENCE** | **NEED EVIDENCE** | ✅ | M2 显式区分：静态形态成立但无实测收益，仍为 NEED EVIDENCE，不因压力转 GO |
| F-T1/F-T2 OrderService UnitOfWork | **GO** | **GO** | ✅ | M1 语义预算：+1 using 属语义中性，仍满足6要素，GO 不变 |
| F-A1 大类拆分 | **STOP** | **STOP** | ✅ | 复杂度预算阈值未变，仍收益不足 |
| F-L1 Schedule DI scope | **CLOSED 无问题** | **CLOSED** | ✅ | ownership 正确，无需改 |
| F-E2 异常泄露 | **CLOSED 无问题** | **CLOSED** | ✅ | 无问题样本保持 |

**结论：** 6/6 回放一致，校准未破坏既有正确决策。

**补充检查：** S1/S2 未落盘，符合“暂缓观察”要求，未引入不必要复杂度。

> 本回放作为 Skill Regression Test 基线，后续任何规则变更需重跑此表。
