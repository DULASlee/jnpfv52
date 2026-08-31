# Migration Evidence Plan — Phase 32

> **Purpose**: Document the evidence required to validate Phase 32 / 33 / 34
> **Method**: Pre-flight + Post-flight + Runtime + Performance

---

## 1. Evidence Layers

### Layer 1: Pre-Migration Evidence (Phase 32 — READ-ONLY)

| Evidence Type | File | Validator |
|---------------|------|-----------|
| Current schema snapshot | `migration-preflight.sql` (Query 1-7) | SQL Server `INFORMATION_SCHEMA` + `sys.indexes` |
| Row count | `migration-preflight.sql` (Query 1) | `COUNT(*)` |
| f_id uniqueness | `migration-preflight.sql` (Query 2) | `COUNT(*) - COUNT(DISTINCT)` |
| NULL check | `migration-preflight.sql` (Query 2, 3) | `WHERE col IS NULL` |
| Composite uniqueness | `migration-preflight.sql` (Query 3) | `COUNT(*) - COUNT(DISTINCT ...)` |
| Existing PK | `migration-preflight.sql` (Query 4) | `sys.indexes WHERE is_primary_key = 1` |
| FK references | `migration-preflight.sql` (Query 5) | `sys.foreign_keys` |
| Estimated lock | `migration-preflight.sql` (Query 6) | row count → ms estimate |

### Layer 2: Migration Evidence (Phase 33 — MUTATION)

| Evidence Type | Source |
|---------------|--------|
| Transaction log entry | `sys.dm_tran_database_transactions` |
| Lock duration | `sys.dm_exec_requests` during execution |
| DDL execution time | `SET STATISTICS TIME ON` + client-side timer |
| Constraint creation | `ALTER TABLE` output messages |
| Row count post-migration | `COUNT(*)` post-DDL |

### Layer 3: Post-Migration Evidence (Phase 33 — MUTATION)

| Evidence Type | File | Validator |
|---------------|------|-----------|
| PK existence | `migration-validation.sql` (Query 1) | `sys.indexes WHERE is_primary_key = 1` |
| Row count unchanged | `migration-validation.sql` (Query 2) | `COUNT(*)` |
| Data integrity | `migration-validation.sql` (Query 3) | `NULL/DISTINCT` checks |
| Composite uniqueness | `migration-validation.sql` (Query 4) | `DISTINCT` check |
| SqlSugar compat | `migration-validation.sql` (Query 5) | PK_EXISTS check |
| FK references | `migration-validation.sql` (Query 6) | `sys.foreign_keys` |
| Index size impact | `migration-validation.sql` (Query 7) | `sys.dm_db_partition_stats` |

### Layer 4: Runtime Evidence (Phase 34 — READ-ONLY, may include temporary test data)

| Evidence Type | Method |
|---------------|--------|
| Insertable test | SqlSugar test script (see `runtime-impact.md` §9) |
| Queryable test | SqlSugar test script |
| Composite PK constraint test | Attempt duplicate insert, expect failure |
| Navigation test | Includes() query for SignatureUser |
| API test | SignatureService integration test |
| Dapper test | Direct SQL execution (if used) |

### Layer 5: Performance Evidence (Phase 34 — READ-ONLY)

| Evidence Type | Condition |
|---------------|-----------|
| Actual execution plan capture | Only for queries impacted by PK addition |
| Logical reads comparison | For representative queries |
| Duration comparison | For representative queries |

**Note**: Per Chief Architect directive 2026-08-31: "性能只对真正受影响的查询做验证，不再为了'完成指标'制造无意义 benchmark".

---

## 2. Evidence Storage Plan

| File | Content | Path |
|------|---------|------|
| Pre-migration evidence | `migration-preflight.sql` output | `backend/database/phase-32/evidence/pre-migration.txt` |
| Migration execution log | DDL execution output | `backend/database/phase-32/evidence/migration-execution.txt` |
| Post-migration evidence | `migration-validation.sql` output | `backend/database/phase-32/evidence/post-migration.txt` |
| Runtime test results | Test script output | `backend/database/phase-32/evidence/runtime-tests.txt` |
| Performance test results | Execution plan + statistics | `backend/database/phase-32/evidence/performance.txt` (only if needed) |

---

## 3. Evidence Acceptance Criteria

For Phase 32 Acceptance Gate:
- [ ] `migration-preflight.sql` executed against current DB, all 7 query groups pass
- [ ] `migration-spec.md` reviewed and approved
- [ ] `runtime-impact.md` reviewed and approved
- [ ] Entity class change spec for `SignatureUserEntity` defined

For Phase 33 Authorization:
- [ ] Phase 32 bundle approved by Chief Architect
- [ ] Entity class change for `SignatureUserEntity` ready (in PR or local)
- [ ] Migration window scheduled (low-traffic)
- [ ] Rollback window scheduled (after migration verification)

For Phase 33 → Phase 34 Handoff:
- [ ] `migration.sql` executed successfully
- [ ] `rollback.sql` rehearsed (run on test DB, NOT production)
- [ ] `migration-validation.sql` all 7 query groups pass
- [ ] Row count unchanged
- [ ] No FK violations
- [ ] No production data loss

For Phase 34 → Final Acceptance Handoff:
- [ ] SqlSugar Insertable/Updateable/Deleteable functional
- [ ] Navigation to SignatureUser functional
- [ ] API smoke tests pass
- [ ] No regression in dependent tables
- [ ] No production errors in 24h post-migration monitoring

---

## 4. Evidence Workflow

```text
Phase 32 (READ-ONLY)
   ↓
   pre-migration evidence captured
   ↓
   Chief Architect Acceptance Gate
   ↓
Phase 33 (MUTATION)
   ↓
   pre-flight validation (last time)
   ↓
   backup/snapshot (per Master Plan v2.1 §33.1)
   ↓
   ALTER TABLE base_signature ADD PK
   ↓
   ALTER TABLE base_signature_user ADD Composite PK
   ↓
   post-flight validation
   ↓
   migration evidence captured
   ↓
Phase 34 (UNIFIED VALIDATION)
   ↓
   runtime evidence captured
   ↓
   performance evidence (if needed)
   ↓
FINAL ACCEPTANCE
```

---

## 5. Evidence Sufficiency Stop Rule (per IRON-TABLE-04 in Master Spec v2.1)

> "Need evidence for Finding X? ... Collect until minimum threshold is met. STOP — do not continue searching."

Evidence collection STOPS when:
- All Layer 1-3 queries return expected results
- All Layer 4 runtime tests pass
- No new evidence would change the decision

**Do not** collect additional evidence after acceptance criteria met.

---

**STOP. Awaiting Phase 32 Migration Acceptance Gate.**

**No evidence collection begins until Phase 33 authorized.**
