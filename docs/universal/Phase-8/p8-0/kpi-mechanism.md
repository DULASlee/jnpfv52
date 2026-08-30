# P8-0 — KPI Mechanism Template

> **Phase**: 8 — P8-0
> **Status**: COMPLETE
> **Version**: v1.0
> **Date**: 2026-08-30

---

## 1. KPI 文件结构（实际建立）

```
docs/universal/Phase-8/kpi/
├── table/
│   └── (per-table kpi templates)
├── batch/
│   └── (per-batch kpi summaries)
├── phase/
│   └── (phase cumulative kpi)
├── stability-gate/
│   └── (stability gate records)
└── problem-routing-log.md   ← P8-0.7
```

---

## 2. Table Unit KPI Template

每张表完成后填入 `kpi/table/{table-name}-kpi.md`：

```markdown
# Table Unit KPI — {Table Name}

## Basic Info
- Table: {name}
- Module: {module}
- Category: {category}
- Batch: {batch-name}
- State: {DISCOVERED|ASSESSED|DESIGNED|READY|REFACTORED|NO-CHANGE|VERIFIED|CLOSED}

## AI Execution Metrics
- AI Start: {ISO timestamp}
- AI End: {ISO timestamp}
- AI Duration: {minutes}
- Evidence items collected: {count}
  - schema: {Y/N}
  - query pattern: {Y/N}
  - index: {Y/N}
  - FK: {Y/N}
  - lifecycle: {Y/N}
  - tenant: {Y/N}

## Findings
- Total findings: {count}
- R0: {n} R1: {n} R2: {n} R3: {n} R4: {n} R5: {n}
- Hard Gate triggers: {HG#n list or None}

## Risk Classification
- AI Risk: {R?}
- Human Risk (if Shadow): {R?}

## Outcome
- Recommended Action: {description}
- Recommended Closure: {TABLE CLOSED | NO-CHANGE | NEEDS_REWORK | ESCALATE}
- Actual Closure: {as decided by gate}

## Safety (4 hard metrics)
- Hard Gate FN: {0 | count}
- P0/P1 decision error: {0 | count}
- Core contamination: {0 | count}
- TABLE CLOSED decision error: {0 | count}

## Efficiency
- Table Completion Time (AI + Human): {minutes}
- Human Review Time: {minutes}
- Rework count: {n}
```

---

## 3. Batch KPI Template

每个 Batch 完成后填入 `kpi/batch/{batch-name}-kpi.md`：

```markdown
# Batch KPI — {Batch Name}

## Batch Summary
- Batch: {nn-name}
- Period: {start} to {end}
- Total Table Units: {n}
- Completed: {n}
- Blocked: {n}
- Rework: {n}

## Safety (cumulative within batch)
- Hard Gate FN: {0}
- P0/P1 decision error: {0}
- Core contamination: {0}
- TABLE CLOSED decision error: {0}
- **Batch Safety: PASS / FAIL**

## Quality
- False Positive total: {n}
- False Negative total: {n}
- FP Rate: {%}
- FN Rate: {%}
- Risk misclassification count: {n}
- Rework Rate: {%}

## Productivity
- Median Table Completion Time: {minutes}
- P90 Table Completion Time: {minutes}
- Tables / AI-hour: {n}

## Human Work
- Human Gate Rate: {%}
- Average Human Review Time / table: {minutes}

## Distribution
- TABLE CLOSED: {n}
- NO-CHANGE: {n}
- NEEDS_REWORK: {n}
- ESCALATE: {n}
```

---

## 4. Phase KPI Template（最终聚合）

```markdown
# Phase KPI — {Phase Name}

## Cumulative Safety
- Hard Gate FN: {0} / Total tables: {n}
- P0/P1 decision error: {0} / Total tables: {n}
- Core contamination: {0} / Total tables: {n}
- TABLE CLOSED decision error: {0} / Total tables: {n}
- **Cumulative Safety: PASS / FAIL**

## Cumulative Quality
- Total AI findings: {n}
- Total False Positive: {n} / FP Rate: {%}
- Total False Negative: {n} / FN Rate: {%}
- Total Rework: {n} / Rework Rate: {%}

## Cumulative Productivity
- Tables Closed: {n} / 289 ({percent})
- Tables Remaining: {n}
- Median Table Completion Time: {min}
- P90 Table Completion Time: {min}
- Tables / AI-hour: {n}

## Cumulative Human Work
- Total Human hours: {n}
- Human Gate Rate: {%}

## Phase Outcome
- Total Batches: {n}
- Total Batch Verifications: {n}
- Total Batch Acceptance: {n}
```

---

## 5. P8-0 KPI 初始（无业务表评估）

P8-0 不采集业务 KPI，仅采集**机制建立指标**：

| 指标 | 值 |
|---|---|
| Inventory tables | 289 |
| Entity mappings | 164 (56.7%) |
| Total FK edges | 14 |
| Module distribution | system 153 / workflow 69 / inteAssistant 50 / visualdata 12 / framework 5 |
| Registry size | 289 entries |
| Batch initial suggestion | 5 batches × 5 tables = 25 (Batch 01-05 example) |
| Routing categories | 6 |
| KPI templates ready | Table / Batch / Phase / Stability Gate |

---

## 6. 后续阶段 KPI 起点

- **P8-A**：Shadow 5 张表 → 建立 Productivity baseline（Median / P90 / Tables per AI-hour）
- **P8-B**：2-3 Batches → 建立 Batch 节奏基线
- **P8-C**：累计 ≥ 30 tables → 比较 vs P8-A baseline
- **P8-D**：累计 ≥ 95% → 比较 vs P8-C baseline
- **P8-E**：289/289 → 最终 cumulative KPI
