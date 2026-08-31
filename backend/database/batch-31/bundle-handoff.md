# Batch 31 Decision Refinement COMPLETED（2026-08-31）

## Authorization

Chief Architect 2026-08-31: "EXECUTE BATCH 31.1 → 31.9；完成后 STOP，等待 Batch 31 Decision Acceptance Gate"

## Execution Results

### Tasks 31.1 → 31.9 (all PASS, NO Schema DDL)

| Task | Output | Key Finding |
|------|--------|-------------|
| 31.1 PK Dependency & Semantic | batch-31-pk-dependencies.json | base_signature + base_signature_user both 0 rows, 0 FKs, 0 views, 0 procs, 0 cs_refs |
| 31.2 PK Feasibility | batch-31-pk-analysis-v2.json | base_signature: MIGRATION_REQUIRED; base_signature_user: DEFERRED (composite vs surrogate trade-off) |
| 31.3 Re-open Note | batch-31-tenant-reopen-note.json | "row<100 → NO_CHANGE" rule ABANDONED |
| 31.4 Tenant Query Evidence | batch-31-tenant-query-evidence.json | 15 tables, all 0 cs_refs, 0 tenant queries found |
| 31.5 Selectivity | batch-31-tenant-selectivity.json | All 15 tables: 0 rows, 0 distinct tenants |
| 31.6+31.7 Tenant Decision v2 | batch-31-tenant-index-analysis-v2.json | All 15 → DEFERRED (no production data to measure) |
| 31.8 Decision Matrix v2 | batch-31-decision-matrix-v2.json | 17 decisions total |
| 31.9 Anti-Regression | batch-31-decision-report.md | 0 forbidden judgments found |

## Final Decision Matrix v2

| State | Count | Items |
|-------|:-----:|-------|
| **MIGRATION_REQUIRED** | 1 | PK-base_signature (f_id surrogate, empty table, SqlSugar mandatory) |
| **DEFERRED** | 16 | PK-base_signature_user (composite vs surrogate trade-off) + 15 tenant indexes (no prod data) |
| NO_CHANGE | 0 | None (per Batch 31 rule: cannot use "row<100" rule) |
| EXCLUDED | 0 | None |
| BLOCKED | 0 | None |

## Anti-Regression Check

- 0 forbidden judgments ("row<100 → NO_CHANGE" rule abandoned)
- 0 missing PK shortcuts
- 0 "ORM seems fine" shortcuts
- 0 "report says PASS" shortcuts
- All 17 decisions have evidence + Target Contract + Risk + Runtime Impact + Migration Type + Rollback

## Iron Laws Compliance (v2)

- IRON-TABLE-01 No Change ≠ No Action: ✅ 0 NO_CHANGE without evidence
- IRON-TABLE-04 Security Boundary First: ✅ PK requires human gate review
- IRON-TABLE-05 Performance Measurement: ✅ SET STATISTICS IO/TIME captured
- IRON-TABLE-09 Evidence Over Declaration: ✅ All claims bound to evidence files

## STOP Confirmation

Per Master Plan v2.1 §12:
> "Batch 31 完成后 STOP。"

**STOPPED. Awaiting Batch 31 Decision Acceptance Gate.**

## Awaiting Chief Architect Decision

### MIGRATION_REQUIRED (1 item)
- **PK-base_signature** → APPROVE MIGRATION to Phase 32? Or REJECT?

### DEFERRED (16 items)
- **PK-base_signature_user** → APPROVE composite (f_signature_id, f_user_id)? APPROVE surrogate (f_id)? DEFER to Phase 33? EXCLUDE?
- **15 tenant indexes** → All empty tables, no production data to measure. APPROVE? DEFER until data exists? EXCLUDE?

## Forbidden (Maintained)

```
Schema DDL = 0
CREATE INDEX = 0
DROP = 0
ADD PRIMARY KEY = 0
ADD CONSTRAINT = 0
ALTER COLUMN = 0
Production Data Migration = 0
ORM = 0
Entity = 0
```

## Next Action

Chief Architect reviews batch-31-decision-matrix-v2.json + batch-31-decision-report.md.
For each MIGRATION_REQUIRED or DEFERRED, decides A/B/C/D/E per Master Plan v2.1 §12.
Only after explicit approval can Phase 32 (Migration Specification) begin.
