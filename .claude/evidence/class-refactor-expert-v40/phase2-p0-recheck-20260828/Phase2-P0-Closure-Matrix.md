# Phase 2 P0 Closure Matrix — 主验收控制表（Recheck 2026-08-28）

> 本轮最重要交付物。每行必须终态 `PASS / FAIL / BLOCKED`，禁止 "☐ 已检查" 未完成态。
> 列义：10 维重查 / 原 Finding 重验 / 新 Finding 扫描 / Regression 检查 / Evidence / 当前结论。
> "结论" = Class-level 收敛态（见 Quality-Matrix 两层统计）。

| Class | 10维重查 | 原Finding重验 | 新Finding扫描 | Regression检查 | Evidence | 当前结论 |
|-------|---------|--------------|--------------|---------------|----------|----------|
| FileService Upload | PASS 10/10 | PASS M-02(L447) | PASS 无新 | PASS R=0 | Screening §1 | **CONVERGED** |
| FileService FileDown | PASS 10/10 | PASS M-03(L201) | PASS R-01/R-02 已登记 | PASS R=0 | Screening §1 / Findings R-01,R-02 | **CONVERGED**（残留 R-01 STOP / R-02 NEED EVIDENCE） |
| FileService DownloadAll | PASS 10/10 | PASS R-03/R-04 仍残留 | PASS 无新 | PASS R=0 | Screening §1 / Findings R-03,R-04 | **CONVERGED**（跨层 ownership → STOP，正确拒绝局部修） |
| OrderService Save/Delete | PASS 10/10 | PASS M-04(L200/250) | PASS R-05/06/07 已登记 | PASS R=0 | Screening §2 | **CONVERGED**（UoW 已缓解；Runtime rollback DEFERRED；R-05 NEED EVIDENCE、R-06/07 STOP） |
| JsonHelper | PASS 10/10 | PASS M-05a(L14) | PASS 无新 | PASS R=0 | Screening §3 | **CONVERGED** |
| UserManager | PASS 10/10 | PASS M-05b(L1064) | PASS 无新 | PASS R=0 | Screening §4 | **CONVERGED** |
| DataInterfaceService | PASS 10/10 | PASS M-05d(L1945) | PASS 无新 | PASS R=0 | Screening §5 | **CONVERGED** |
| ConfigController | PASS 10/10 | PASS M-05c(L192) | PASS 无新（偏差修正：走安全 ToObject） | PASS R=0 | Screening §7 / Inventory B-偏差 | **CONVERGED** |
| BatchDeleteSqlPlanner | PASS 10/10 | PASS M-05e(L20/42) | **R-09 新发现**（黑名单式 + 非 id 字段内插） | PASS R=0 | Screening §6 / Findings R-09 | **NEEDS_REVIEW**（R-09 decision candidate，本轮不修，越 Phase2 范围） |
| EmailService | PASS 10/10 | PASS M-01(L145) 区间内未回归 | PASS 无新 | PASS R=0 | Screening §8 | **CONVERGED** |
| ScheduleService | PASS 10/10 | PASS FP-01/02 仍误报；R-08 残留 | PASS 无新 | PASS R=0 | Screening §9 / Findings R-08 | **NEED_EVIDENCE**（N+1 形态真实，缺运行时/量级证据；区间未改代码） |

## 终态计数（每行必为 PASS/FAIL/BLOCKED 之一，无未完成态）

| 维度 | PASS | FAIL | BLOCKED |
|------|------|------|---------|
| 10 维重查（11 行） | 11 | 0 | 0 |
| 原 Finding 重验 | 11 | 0 | 0 |
| 新 Finding 扫描 | 11（其中 BatchDelete 检出 1 项） | 0 | 0 |
| Regression 检查 | 11（全 R=0） | 0 | 0 |

- **BLOCKED 仅存在于个别 Finding 的处置层**（R-02/R-05/R-08 NEED EVIDENCE = 取证 BLOCKED），**类级验收行无 BLOCKED/FAIL**。
- Class-level 收敛：CONVERGED 9 / NEEDS_REVIEW 1 / NEED_EVIDENCE 1（见 Quality-Matrix 两层分离）。
