# Phase 8 — KPI Tracking

**Phase**: 8 — P8-A / P8-B / P8-C
**Status**: PREPARED
**Version**: v1.0
**Date**: 2026-08-30

---

## 1. 目的

建立 Phase 8 全生命周期的 KPI 采集体系。

Phase 8 从"验证 Skill 是否正确"转向"验证 Skill 能否进入生产"，因此 KPI 分两个维度：

1. **Safety KPIs**：确保生产安全
2. **Productivity KPIs**：衡量高质量 Table Units 的持续产出

---

## 2. KPI 采集层级

```
Phase 8
    ↓
Table Unit Level（每张表）
    ↓
Batch Level（每个 Batch）
    ↓
Phase Level（Phase 8 全局）
```

---

## 3. Table Unit KPI（每张表填写）

### 3.1 AI Execution

```
Table Unit: ___________
Batch: ___________
AI Execution Start: ___________
AI Execution End: ___________
Duration (AI time, minutes): ___________

EVIDENCE COLLECTED:
- Schema evidence: Yes / No — ___________
- Query pattern evidence: Yes / No — ___________
- Index evidence: Yes / No — ___________
- FK evidence: Yes / No — ___________
- Lifecycle evidence: Yes / No — ___________
- Tenant isolation evidence: Yes / No — ___________
- Total evidence items: ___

FINDINGS:
Total findings: ___
R0: ___  R1: ___  R2: ___  R3: ___  R4: ___  R5: ___
Hard Gate triggers: HG#__ ___ HG#__ ___ HG#__ ___ / None

RECOMMENDED ACTIONS:
Total actions: ___
DDL changes: ___  Index changes: ___  No-change: ___  Other: ___

RECOMMENDED CLOSURE STATUS:
TABLE CLOSED / NO-CHANGE / NEEDS_REWORK / ESCALATE
```

### 3.2 Human Independent Review

```
REVIEWER: ___________
REVIEW DATE: ___________
REVIEW DURATION (human hours): ___________

INDEPENDENT FINDINGS:
Total independent findings: ___
R0: ___  R1: ___  R2: ___  R3: ___  R4: ___  R5: ___
Hard Gate triggers: HG#__ ___ HG#__ ___ HG#__ ___ / None

INDEPENDENT CLOSURE STATUS:
TABLE CLOSED / NO-CHANGE / NEEDS_REWORK / ESCALATE
```

### 3.3 AI / Human Comparison

```
FINDING ACCURACY:
AI findings: ___  Human findings: ___
Match: ___  AI-only: ___  Human-only: ___
False Positive (AI found, Human disagreed): ___
False Negative (Human found, AI missed): ___
Finding Accuracy: ___%

RISK CLASSIFICATION:
AI Risk Distribution: R0:__ R1:__ R2:__ R3:__ R4:__ R5:__
Human Risk Distribution: R0:__ R1:__ R2:__ R3:__ R4:__ R5:__
Risk Mismatch count: ___
Risk Misclassification Rate: ___%

HARD GATE ACCURACY:
AI HG count: ___  Human HG count: ___
HG Match: Yes / No
HG False Negative (AI missed HG Human caught): ___
HG False Positive (AI triggered HG Human disagreed): ___
HG Accuracy: ___%

CLOSURE ACCURACY:
AI Closure: ___________
Human Closure: ___________
Closure Match: Yes / No
Closure Error: Yes / No

DIVERGENCE:
Divergence count: ___
Critical: ___  High: ___  Medium: ___  Low: ___
Divergence Types: FP / FN / Risk Misclassification / HG Error / Closure Error / Other
```

### 3.4 Safety KPIs（Table Unit Level）

```
HARD GATE FALSE NEGATIVE: 0 / 1
P0/P1 DECISION ERROR: 0 / 1
UNIVERSAL CORE CONTAMINATION: 0 / 1
TABLE CLOSED DECISION ERROR: 0 / 1

SAFETY RESULT: PASS / FAIL
```

### 3.5 Productivity KPIs（Table Unit Level）

```
TABLE COMPLETION TIME (AI + Human, minutes): ___
HUMAN REVIEW WORKLOAD (human hours): ___
EFFICIENCY: Tables / AI-hour = 1 / (AI_duration_hours) = ___
```

---

## 4. Batch KPI（每个 Batch 填写）

### 4.1 Batch Summary

```
BATCH: ___________
Batch Execution Period: ___________ to ___________
Total Table Units: ___

SAFETY SUMMARY:
Hard Gate FN total: ___（累计）
P0/P1 error total: ___（累计）
Core contamination total: ___（累计）
TABLE CLOSED error total: ___（累计）

BATCH SAFETY RESULT: PASS / FAIL

FINDING QUALITY:
Total AI findings: ___  Total Human findings: ___
False Positive total: ___  False Negative total: ___
FP Rate: ___%  FN Rate: ___%
Risk misclassification total: ___

HUMAN WORKLOAD:
Total human hours: ___
Average human hours / table: ___

EFFICIENCY:
Total AI time (minutes): ___
Total Human time (minutes): ___
Median Table Completion Time (AI+Human): ___ minutes
P90 Table Completion Time: ___ minutes
Tables / AI-hour: ___

HUMAN GATE RATE:
Tables requiring Human Gate: ___ / ___
Human Gate Rate: ___%

REWORK:
Tables requiring rework: ___ / ___
Rework Rate: ___%

CLOSURE DISTRIBUTION:
TABLE CLOSED: ___  NO-CHANGE: ___  NEEDS_REWORK: ___  ESCALATE: ___
```

### 4.2 Batch Productivity Baseline

```
Median completion time (batch): ___ min
P90 completion time (batch): ___ min
Tables / AI-hour (batch): ___
Human hours / table (batch): ___
```

---

## 5. Phase 8 Global KPI（Phase 结束时填写）

### 5.1 Cumulative Safety

```
CUMULATIVE STATS (all batches):
Total Table Units: ___
Total AI time (hours): ___
Total Human time (hours): ___
Hard Gate FN (cumulative): ___  Target: = 0
P0/P1 error (cumulative): ___  Target: = 0
Core contamination (cumulative): ___  Target: = 0
TABLE CLOSED error (cumulative): ___  Target: = 0

SAFETY CUMULATIVE RESULT: PASS / FAIL
```

### 5.2 Cumulative Quality

```
FINDING QUALITY (cumulative):
Total AI findings: ___  Total Human findings: ___
False Positive (cumulative): ___  FP Rate: ___%
False Negative (cumulative): ___  FN Rate: ___%
Risk misclassification (cumulative): ___  Rate: ___%
```

### 5.3 Cumulative Efficiency

```
THROUGHPUT (cumulative):
Total Table Units Closed: ___ / Total Started: ___
Median Table Completion Time (all): ___ min
P90 Table Completion Time (all): ___ min
Tables / AI-hour (cumulative): ___
Human Gate Rate (cumulative): ___%
Rework Rate (cumulative): ___%
```

### 5.4 Phase 8 Production Baseline

```
PHASE 8 BASELINE ESTABLISHED:
Quality: Safety cumulative PASS / FAIL
Quality: FP Rate ___% / FN Rate ___% / Risk Misclassification Rate ___%
Efficiency: Median ___ min / table | P90 ___ min / table
Efficiency: ___ tables / AI-hour
Human: Human Gate Rate ___% | Human hours / table ___
Rework: Rework Rate ___%
```

---

## 6. P8-B Stability Gate

P8-B Controlled Production 进入 P8-C 前，必须满足：

```
SHADOW 5 → CONTROLLED BATCH 01 → CONTROLLED BATCH 02

STABILITY GATE CHECKPOINTS:

Batch 01 vs Batch 02 Trend:
- Hard Gate FN: ___ vs ___ （应 = 0 两个都是）
- P0/P1 error: ___ vs ___ （应 = 0 两个都是）
- Rework Rate: ___% vs ___%（应下降或持平）
- Human Gate Rate: ___% vs ___%（应下降或持平）
- Median completion time: ___ vs ___（应下降或持平）
- Tables / AI-hour: ___ vs ___（应提升或持平）

STABILITY CRITERIA:
Safety: Hard Gate FN = 0 AND P0/P1 error = 0 in both batches?  YES / NO
Safety: No Core contamination in both batches?  YES / NO
Quality: Rework Rate not increasing?  YES / NO
Quality: Human Gate Rate not increasing?  YES / NO
Efficiency: Median time not increasing?  YES / NO

STABILITY GATE RESULT: PASS / FAIL

If PASS → Enter P8-C
If FAIL → Stay in P8-B / Investigate divergence
```

---

## 7. Problem Routing Log

每个 divergence 或 rework 项必须路由到对应通道：

```
PROBLEM ROUTING LOG
Date | Table | Divergence Type | Classification | Routed To | Status

_________ | _________ | _________ | JNPF-specific | JNPF Extension | _________
_________ | _________ | _________ | Skill execution | Skill Evolution | _________
_________ | _________ | _________ | Universal rule | Master Spec Evolution | _________
_________ | _________ | _________ | BBB gap | BBB Product Backlog | _________
_________ | _________ | _________ | Business ambiguity | Human Decision | _________
_________ | _________ | _________ | Provider constraint | Target/Provider Profile | _________
```

### 路由定义

| 分类 | 定义 | 路由通道 |
|---|---|---|
| JNPF-specific | JNPF 特有的业务逻辑、命名、数据语义 | JNPF Extension |
| Skill execution | Skill 执行过程中的 bug、漏判、误判 | Skill Evolution (Level A/B/C) |
| Universal rule | Universal Core 规则本身的问题 | Master Spec Evolution |
| BBB gap | BBB Generic Repository 能力不足 | BBB Product Backlog |
| Business ambiguity | 业务语义不清晰、无法判断 | Human Decision |
| Provider constraint | 数据库/Provider 层面的限制 | Target/Provider Profile |

**硬规则**：JNPF-specific 不得直接修改 Universal Core。

---

## 8. KPI 记录文件结构

```
docs/universal/Phase-8/kpi/
  table/
    table-01-{name}-kpi.md
    table-02-{name}-kpi.md
    ...
  batch/
    batch-01-p8a-shadow-kpi.md
    batch-02-p8b-controlled-kpi.md
    ...
  phase/
    phase-8-cumulative-kpi.md
  stability-gate/
    stability-gate-01.md
    stability-gate-02.md
    ...
  problem-routing-log.md
```

---

## 9. 核心生产力原则

Phase 8 衡量的是：

> **High-quality Table Units Closed / AI-hour**

不是：

- Finding 数量
- 修改行数
- Index 数量
- DDL 数量

两张表对比：

```
Table A: 10 findings → 0 修复（全部误报）

Table B: 2 findings → 2 个真实问题全部解决
```

Table B 的工程价值可能比 Table A 高得多。Skill 的价值体现在**有效关闭高质量 Table Units**，不是找到更多问题。

---

## 10. 当前状态

```
Phase 8                    🟢 OPEN
P8-A Shadow Preparation    ✅ PREPARED
P8-B Controlled Production ⏸ NOT REACHED
P8-C Autonomous Batch      ⏸ NOT REACHED

KPI Tracking Ready: YES
Baseline will be established at: P8-A Shadow completion
Efficiency targets will be set after: P8-A baseline established
```

P8-A 完成（Shadow Gate PASS）后，此文档建立真实生产基线，供 P8-B / P8-C 效率目标参考。
