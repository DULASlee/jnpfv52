# Execution Manual v2.0 — Table Schema Evolution Expert Skill

> **Version**: v2.0 (companion to master-spec-v2.md)
> **Purpose**: Procedural reference (HOW to execute) — what master-spec-v2.md defines as standards (WHAT is correct)
> **Status**: DRAFT — pending Phase 1.6 Task Group B verification

---

## 1. 5-Step SOP (v2.0 enhanced)

```
Step 1: DISCOVER     → Identify target table + load Current Schema (sys.columns, sys.indexes)
Step 2: CONTRACT     → Load Target Schema Contract (YAML 8 dimensions)
Step 3: GAP_ANALYZE  → Compute Gap Analysis (6 gap types, 4 severity levels)
Step 4: DECIDE       → Migration Type A/B/C + Iron Laws triggered + Human Gate decision
Step 5: EXECUTE      → Migration Bundle (4 files) + Evidence + Verification + Rollback dry-run
```

Each step has 6 mandatory fields per Master Spec §2: **Input / Action / Evidence / Output / Stop / Escalation**

---

## 2. Step-by-Step Procedures

### Step 1: DISCOVER

**Input**: Table name (e.g., `base_user`)

**Action**:
```python
# Pseudo-code
schema = query_sql_server(
    f"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table_name}'"
)
indexes = query_sql_server(
    f"SELECT * FROM sys.indexes WHERE OBJECT_NAME(object_id) = '{table_name}'"
)
row_count = query_sql_server(
    f"SELECT COUNT(*) FROM dbo.{table_name}"
)
```

**Evidence**:
- Schema snapshot (JSON)
- Index usage stats from `sys.dm_db_index_usage_stats`
- Row count

**Output**: `current_schema.json` with columns, indexes, row_count

**Stop**: All 3 metrics successfully captured

**Escalation**: If table doesn't exist → Hard Gate (Table Not Found)

---

### Step 2: CONTRACT

**Input**: Table name + Project context (e.g., JNPF)

**Action**:
```python
# Pseudo-code
project_default = load_yaml("contracts/project-default-contract.yaml")
module_override = load_yaml(f"contracts/{module_name}-override.yaml")
table_specific = load_yaml(f"contracts/{table_name}-contract.yaml")

target_contract = merge_three_levels(
    project_default,
    module_override,
    table_specific
)
```

**8 Dimensions Required** (per IRON-TABLE-03):
1. `column_naming` — pattern, allowed, forbidden
2. `data_type` — per field
3. `nullable_contract` — NOT NULL / NULL per field
4. `tenant_model` — type, column, isolation_level
5. `audit_model` — created_at/by, updated_at/by, deleted_at/by
6. `index_contract` — primary_key, required_indexes
7. `constraint_contract` — check_constraints, foreign_keys
8. `security_boundary` — priority, sensitive_columns, password_storage

**Output**: `target_contract.yaml`

**Stop**: All 8 dimensions present

**Escalation**: If any dimension missing → Hard Gate (Contract Incomplete)

---

### Step 3: GAP_ANALYZE

**Input**: current_schema.json + target_contract.yaml

**Action**:
```python
# Pseudo-code
gaps = {
    "column_gaps": compare_columns(current, target),
    "type_gaps": compare_data_types(current, target),
    "constraint_gaps": compare_constraints(current, target),
    "index_gaps": compare_indexes(current, target),
    "security_gaps": compare_security(current, target),  # ONLY for P0 tables
    "performance_gaps": check_benchmark_exists(current, target),
}
```

**Severity Mapping**:
- G0 Critical → mandatory REFACTORED
- G1 Major → recommended REFACTORED
- G2 Minor → optional REFACTORED
- G3 OK → NO-CHANGE (with 8-dim evidence)

**Output**: `gap_analysis.json`

**Stop**: All 6 gap types evaluated (or N/A for non-P0 tables)

**Escalation**: If G0_CRITICAL count > 0 → Hard Gate + Decision Brief

---

### Step 4: DECIDE

**Input**: gap_analysis.json

**Action — Migration Type Decision** (per IRON-TABLE-02 + IRON-TABLE-08):
```python
def classify_table(table_name):
    # B-3 FIX: case normalization FIRST
    normalized = table_name.lower()

    # Type C: Low-Code Dynamic
    if normalized.startswith(("wform_", "lowcode_")):
        return TableType.DYNAMIC_FORM
    if normalized.startswith("ext_") and is_user_extended(normalized):
        return TableType.USER_EXTENDED
    return TableType.BUSINESS_ENTITY

def decide_migration_type(table, column, current_def, target_def):
    table_type = classify_table(table)

    if table_type in (TableType.DYNAMIC_FORM, TableType.USER_EXTENDED):
        return MigrationType.TYPE_C  # SKIP_AUTO

    if semantic_changed(column, current_def, target_def):
        return MigrationType.TYPE_B
    if pure_naming_error(column):
        return MigrationType.TYPE_A
    return MigrationType.TYPE_B  # conservative default

def iron_laws_triggered(gaps, decision):
    triggered = []
    if decision.migration_type and not decision.has_8_dim_evidence:
        triggered.append("IRON-TABLE-01")  # No Change ≠ No Action
    if any("F_X AS F_Y" in gap.evidence for gap in gaps):
        triggered.append("IRON-TABLE-02")  # Mapping Is Not Migration
    if decision.migration_type == MigrationType.TYPE_C and not decision.skip_auto:
        triggered.append("IRON-TABLE-08")  # Dynamic Platform Exception
    if any(gap.severity == "G0_CRITICAL" for gap in gaps):
        triggered.append("IRON-TABLE-04")  # Security Boundary First
    if decision.performance_claimed and not decision.before_after_data:
        triggered.append("IRON-TABLE-05")  # Performance Claim Requires Measurement
    if decision.forward_sql and not decision.rollback_sql:
        triggered.append("IRON-TABLE-06")  # Migration First-Class
    return triggered

def human_gate_decision(table_type, gaps):
    # B-4 FIX: NO --human-approved boolean flag
    # Human Gate decision is based on table type + gap severity
    if table_type in (TableType.DYNAMIC_FORM, TableType.USER_EXTENDED):
        return "REQUIRED"  # Type C always needs human
    if any(gap.severity == "G0_CRITICAL" for gap in gaps):
        return "REQUIRED"  # G0 always needs human
    if table_type == TableType.BUSINESS_ENTITY:
        return "NOT_REQUIRED"  # Type A on business entity can auto-execute
    return "REQUIRED"
```

**Output**: `decision.json` with migration_type, iron_laws, human_gate

**Stop**: Migration Type + Iron Laws + Human Gate all determined

**Escalation**: If unresolvable → Hard Gate (Decision Blocked)

---

### Step 5: EXECUTE

**Input**: decision.json

**Action**:

**5a. Generate Migration Bundle (4 files)** — per IRON-TABLE-06:
```
/database/migrations/
├── V<YYYYMMDD>_<change_id>.sql            # Forward Migration
├── V<YYYYMMDD>_<change_id>_down.sql       # Rollback Migration
├── V<YYYYMMDD>_<change_id>_verify.sql     # Validation Script
└── V<YYYYMMDD>_<change_id>_evidence.json  # Evidence Bundle
```

**5b. Approval Gate Validation** — per IRON-TABLE-07 + DoD-07:
```python
def validate_approval(approval_record_path):
    """B-4 FIX: --approval-record token (NOT boolean flag)"""
    record = load_yaml(approval_record_path)

    # Mandatory fields
    required = ["id", "reviewer", "reviewer_email", "timestamp", "scope", "decision", "signature_hash"]
    for field in required:
        if field not in record:
            raise ApprovalError(f"Missing required field: {field}")

    # Validate scope matches requested tables
    requested_tables = current_request.tables
    if not all(t in record["scope"] for t in requested_tables):
        raise ApprovalError(f"Scope mismatch: {record['scope']} vs {requested_tables}")

    # Validate signature (HMAC or notarized hash)
    if not verify_signature(record["signature_hash"]):
        raise ApprovalError("Invalid signature")

    # Validate expiry
    if record.get("expiry") and parse(record["expiry"]) < now():
        raise ApprovalError("Approval expired")

    return ApprovalValid(record)
```

**5c. 7-Layer Runtime Compatibility Check** — per IRON-TABLE-07:
```
Layer 1: Database        — DDL execution success + transaction atomicity
Layer 2: ORM             — SqlSugar Entity 序列化/反序列化一致
Layer 3: Repository      — IRepository<T>.CRUD 全部通过
Layer 4: Dynamic SQL     — codegen SQL 解析无报错
Layer 5: Form Engine     — lowcode_field 配置不丢
Layer 6: Workflow Engine — flow_form_data_json 引用不破
Layer 7: Permission      — authorize 关联不破
```

**5d. Performance Before/After Measurement** — per IRON-TABLE-05:
```sql
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
-- Execute same query BEFORE and AFTER migration
-- Record: logical_reads, cpu_ms, duration_ms
-- Verdict: PERFORMANCE_VERIFIED only if logical_reads_reduction >= 50%
```

**5e. Rollback Dry-run Validation** — per IRON-TABLE-06 + DoD-06:
```python
def validate_rollback(forward_sql, rollback_sql):
    """Run both in test DB, verify row_count unchanged + schema restored"""
    pre = snapshot_schema(test_db)
    execute_sql(test_db, forward_sql)
    mid = snapshot_schema(test_db)
    execute_sql(test_db, rollback_sql)
    post = snapshot_schema(test_db)
    assert pre == post, "Rollback did not restore original schema"
    return ROLLBACK_VERIFIED
```

**Output**: `migration_complete.json` with all 4 file paths + verdicts

**Stop**: All sub-steps PASS

**Escalation**: If any sub-step FAIL → Block + Decision Brief

---

## 3. Closed Gate (5 Conditions per Master Spec §11)

A table is CLOSED only when **ALL 5** met:
1. Evidence sufficient (per Master Spec §7 thresholds)
2. Target settled (Contract complete + Gap Analysis complete)
3. Refactor or no-change decision made (with 8-dim evidence for no-change)
4. Verification passed (13 v1.0 DoD + 7 v2.0 DoD + Iron Laws satisfied)
5. No blocking (no Hard Gate triggered, no SVR outstanding)

---

## 4. Error Recovery

| Failure | Recovery |
|---------|----------|
| Migration fails | Run rollback + revert + Decision Brief |
| 7-layer runtime broken | Stop propagation + Decision Brief |
| Type C misclassified | Re-classify to SKIP |
| Evidence bundle missing | Regenerate + re-collect |
| Rollback dry-run fails | Fix forward migration before retry |
| Approval signature invalid | Re-request human approval |

---

## 5. Mandatory Iron Law Compliance Check (Before Each Step)

Before Step 4 (DECIDE), verify Iron Laws:

```
IRON-TABLE-01: NO-CHANGE has 8-dim evidence? PASS/FAIL
IRON-TABLE-02: Any Mapping Bypass detected? PASS/FAIL (zero tolerance)
IRON-TABLE-03: Target Contract complete (8 dims)? PASS/FAIL
IRON-TABLE-04: P0-Security audit done (if applicable)? PASS/FAIL
IRON-TABLE-05: Performance has Before/After? PASS/FAIL
IRON-TABLE-06: Migration Bundle (4 files)? PASS/FAIL
IRON-TABLE-07: 7-layer runtime check done? PASS/FAIL
IRON-TABLE-08: Type C tables skipped (no auto-migration)? PASS/FAIL
IRON-TABLE-09: All claims bound to evidence? PASS/FAIL
IRON-TABLE-10: Batch has representative proof? PASS/FAIL

If ANY FAIL → Hard Gate triggered, STOP + escalate
```

---

## 6. Tooling Boundary

This Execution Manual does **not** implement tools. Tools live in:
- `tsee/` Python module (see Phase 1.6 Task Group B)

The Manual describes WHAT to do. The module describes HOW to do it programmatically.

---

## 7. Out of Scope

- Auto Repository code generation (v3.0 candidate)
- Cross-database dialect (SQL Server only)
- DML data migration (DDL only)
- CQRS / Outbox / Event Sourcing
- Primary key bigint conversion

---

**Version**: v0.1 (DRAFT, 2026-08-31)
**Companion to**: `master-spec-v2.md`
**Next**: Phase 1.6 Task Group B (executable Gate Layer)