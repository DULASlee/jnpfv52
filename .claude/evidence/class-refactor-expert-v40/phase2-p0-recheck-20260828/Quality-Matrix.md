# Quality Matrix — 两层分离统计（Recheck 2026-08-28）

> §七 硬性要求：Finding-level 与 Class-level **分别统计，不得混成一张表**。

## 第一层 · Finding-level（互斥分类）

| 分类 | 计数 | 明细 |
|------|------|------|
| ALREADY_MITIGATED | 9 | M-01,02,03,04,05a,b,c,d,e |
| FALSE_POSITIVE | 2 | FP-01,02 |
| RESIDUAL | 8 | R-01,02,03,04,05,06,07,08 |
| REGRESSION | **0** | — |
| NEW_FINDING | 1 | R-09 |
| —（处置子层）STOP | 6 | R-01,03,04,06,07,09 |
| —（处置子层）NEED_EVIDENCE | 3 | R-02,05,08 |
| 合计 Finding | 20 | — |

> STOP / NEED_EVIDENCE 是**处置层**，其本质(Nature)已计入 RESIDUAL(含 R-09 NEW_FINDING)；不重复计入 Nature 主分类总数。
> Nature 主分类互斥总数 = 9+2+8+0+1 = **20**。

## 第二层 · Class-level（收敛态，互斥）

| 状态 | 计数 | 类 |
|------|------|----|
| CONVERGED | 9 | FileService(×3 条目)、OrderService、JsonHelper、UserManager、DataInterfaceService、ConfigController、EmailService |
| NEEDS_REVIEW | 1 | BatchDeleteSqlPlanner（R-09 新发现，decision candidate） |
| ESCALATE | 0 | — |
| NEED_EVIDENCE（类级） | 1 | ScheduleService（R-08 N+1 缺证据，未改代码） |
| 合计类条目 | 11 | — |

## 交叉校验

| 指标 | 值 |
|------|----|
| Phase 2 区间内生产变更类 | 7（+3 测试文件） |
| 审计重查类条目 | 11（9 CONVERGED 中 FileService 计 3 条目） |
| 10 维覆盖 | 9 类 × 10 = **90 格，100% 有结果** |
| 已缓解项回归复检 | 9/9 当前树在位，**REGRESSION = 0** |
| 高风险未控 | **0** |
| Production Code Changes（本会话） | **0**（`git diff -- backend` 空） |
