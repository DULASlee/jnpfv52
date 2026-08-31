# JNPF Table Refactoring — Current State Snapshot

> **状态卡** | 2026-08-31T19:35:00 | 新 Session 必读

---

## Status

```
PROJECT: CLOSED
FINAL ACCEPTANCE: APPROVED
SKILL: v2.0 (DRAFT, Phase 1 verification)
```

---

## Final Metrics

| 指标 | 数值 |
|:---|:---|
| ACTUALLY_FIXED | 2 |
| NO_CHANGE | 10 |
| DEFERRED | 7 |
| FALSE_POSITIVE | 17 |
| G0_CRITICAL | 0 |
| G1_MAJOR | 0 |
| Total Gaps | 19 |

---

## Schema Changes (Executed)

| Table | Change | Migration ID | Status |
|:---|:---|:---|:---|
| BASE_SIGNATURE | ADD PK (f_id) | M32-01 | ACTUALLY_FIXED |
| BASE_SIGNATURE_USER | ADD PK (f_signature_id, f_user_id) | M32-02 | ACTUALLY_FIXED |

---

## Known Pre-existing Issues

| Issue | Classification | Impact |
|:---|:---|:---|
| `SugarTable_Mappings_ShouldBe_Unique` 失败 | PRE_EXISTING | 1 个测试失败，与本次迁移无关 |
| `AiCallLogEntity` 重复定义 | 基础设施 Bug | 两路径重复，Project 级问题 |

---

## Rollback Status

```
DESIGNED: YES
VALIDATED: YES (live DB confirmed)
EXECUTED: NO (environment policy)
```

---

## Deferred Items (7)

| Gap ID | Table | Reason | Trigger |
|:---|:---|:---|:---|
| FR-004 | BASE_APP_DATA | 空表 | Production data >100 rows |
| FR-009 | BASE_IM_CONTENT | 数据质量全 NULL | Fix NULLs first |
| FR-010 | BASE_IM_REPLY | 数据质量全 NULL | Fix NULLs first |
| FR-012 | BASE_INTEGRATE_NODE | 空表 + ORM 未确认 | Production + ORM review |
| FR-013 | BASE_ORGANIZE_RELATION | 空表 + ORM 未知 | Production + ORM review |
| FR-016 | BASE_SIGNATURE | 空表 + 非 tenant-aware | Entity reclassified |
| FR-017 | BASE_SIGNATURE_USER | 空表 + 非 tenant-aware | Entity reclassified |

---

## Key Decisions

| Decision | ADR | Status |
|:---|:---|:---|
| BASE_SIGNATURE_USER 使用复合 PK (f_signature_id, f_user_id) | ADR-025 | APPROVED |
| Tenant Index 全部 Deferred | ADR-026 | APPROVED |
| Skill v2.0 作为治理标准 | ADR-024 | APPROVED |

---

## Skill v2.0 10 Iron Laws (摘要)

| ID | 规则 |
|:---|:---|
| IRON-TABLE-01 | NO_CHANGE ≠ No Action — 必须用 8 维度证据证明 |
| IRON-TABLE-02 | Mapping ≠ Migration — 三种有效路径 |
| IRON-TABLE-03 | 每张表必须有 Target Contract |
| IRON-TABLE-04 | 安全边界优先（P0-Security 表）|
| IRON-TABLE-05 | 性能声称必须提供 Before/After 数据 |
| IRON-TABLE-06 | Migration 是一等公民（4 文件 Bundle）|
| IRON-TABLE-07 | Runtime 兼容性优先（7 层验证）|
| IRON-TABLE-08 | 动态平台特殊分类（DYNAMIC_FORM / USER_EXTENDED 禁止自动改名）|
| IRON-TABLE-09 | Evidence Over Declaration |
| IRON-TABLE-10 | Batch 完成必须有代表性证明 |

---

## Important Lessons

| Lesson | 内容 |
|:---|:---|
| Lesson 01 | NO_CHANGE ≠ 没有发现问题 |
| Lesson 02 | Mapping ≠ Migration |
| Lesson 03 | row < 100 ≠ 不需要 Index |
| Lesson 04 | Dynamic Table ≠ Ordinary Table |
| Lesson 05 | Missing PK ≠ 自动增加 PK |
| Lesson 06 | READY_TO_EXECUTE ≠ FIXED |
| Lesson 07 | DESIGNED ≠ VALIDATED ≠ EXECUTED |
| Lesson 08 | Test Failure ≠ Regression |
| Lesson 09 | Report ≠ Evidence |
| Lesson 10 | Deferred ≠ Unfinished |

---

## Last Validation

```
Date: 2026-08-31T19:10:00
Build: 0 errors
Tests: 728/729 (1 PRE_EXISTING)
Live DB: Both PKs confirmed in sys.indexes
Rollback: VALIDATED (not executed)
```

---

## AI 必读路径（新 Session）

1. `docs/architecture/v52/database-modernization/JNPF-Table-Refactoring-Registry.yaml` — Artifact Registry
2. `docs/architecture/v52/database-modernization/JNPF-Table-Refactoring-Current-State.md` — 本文件
3. `backend/database/final-refactor/JNPF-Final-Refactoring-Matrix-vFinal.json` — 机器可读事实源
4. `docs/architecture/v52/database-modernization/JNPF-Table-Refactoring-Playbook.md` — 施工手册

**不需要读取历史 batch-* 目录。**