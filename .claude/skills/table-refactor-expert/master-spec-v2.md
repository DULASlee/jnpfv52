# Master Spec v2.0 — Table Schema Evolution Expert

> **Status**: DRAFT (Phase 1 verification)
> **Version**: v2.0 (extends v1.0 with Gap Analysis Layer + 10 Iron Laws + 7 Skill DoD + 3 Simulation Cases)
> **Supersedes**: v1.0 (chapters retained, new chapters added)
> **Canonical reference for**: all technical standards, risk levels, Hard Gates, DoD, KPI

This document defines the **technical standards** (WHAT is correct). The Execution Manual v2.0 defines the **procedures** (HOW to execute). This Skill does not restate either source.

---

## 1. Target Schema Contract (8 Dimensions)

### 1.1 Standard Schema

```yaml
target_schema_contract:
  table_name: <required>
  classification: PRODUCT_CORE | BUSINESS_ENTITY | DYNAMIC_FORM | USER_EXTENDED | LEGACY_WAREHOUSE | OUT_OF_SCOPE

  # Dimension 1: Column Naming
  column_naming:
    rule: <lowercase_with_f_prefix | PascalCase_no_prefix | ...>
    allowed_patterns: [list]
    forbidden_patterns: [list]

  # Dimension 2: Data Type
  data_type:
    id: <type>
    code: <type>
    name: <type>
    ...

  # Dimension 3: Nullable Contract
  nullable_contract:
    id: NOT NULL
    f_creator_time: NOT NULL DEFAULT GETDATE()
    ...

  # Dimension 4: Tenant Model
  tenant_model:
    type: SHARED_COLUMN | SHARED_SCHEMA | SHARED_DB
    column: f_tenant_id
    isolation_level: STRICT | LAX

  # Dimension 5: Audit Model
  audit_model:
    soft_delete: true | false
    fields: { created_at, created_by, updated_at, updated_by, deleted_at, deleted_by, delete_flag }

  # Dimension 6: Index Contract
  index_contract:
    primary_key: f_id
    required_indexes:
      - { name, columns, type: PRIMARY_KEY|UNIQUE|NONCLUSTERED }

  # Dimension 7: Constraint Contract
  constraint_contract:
    check_constraints:
      - { name, expression }
    foreign_keys: <list | null>

  # Dimension 8: Security Boundary
  security_boundary:
    priority: P0_SECURITY | P1_BUSINESS | N/A
    sensitive_columns: [list]
    password_storage: { target_algo, target_field, forbidden }
```

### 1.2 Contract Inheritance (3 Levels)

```
Project Default Contract (all tables inherit)
    ↓ overrides
Module Override Contract (module-specific)
    ↓ overrides
Table-Specific Contract (table-specific)
```

---

## 2. Gap Analysis Layer (NEW in v2.0)

### 2.1 6 Gap Types

| Type | Description | Severity |
|------|-------------|----------|
| **column_gaps** | Column missing/excess/wrong-type | G0/G1/G2 |
| **type_gaps** | Data type doesn't match contract | G1/G2 |
| **constraint_gaps** | CHECK/UNIQUE/FK missing | G0/G1 |
| **index_gaps** | Required index missing/inefficient | G0/G1/G2 |
| **security_gaps** | P0-Security field/constraint violation | **G0 always** |
| **performance_gaps** | Missing benchmark or known slow query | G1/G2 |

### 2.2 Gap Severity Levels

| Level | Trigger | Skill Behavior |
|-------|---------|----------------|
| **G0 Critical** | Security / Tenant / UNIQUE violation | REFACTORED (P0-Security, must do) |
| **G1 Major** | Type / Constraint / Index gap with measurable impact | REFACTORED (P1, schedule) |
| **G2 Minor** | Naming / Audit field / low-priority index | REFACTORED or NO-CHANGE w/ evidence |
| **G3 OK** | Fully compliant | NO-CHANGE w/ 8-dim evidence |

### 2.3 Gap Analysis Report Schema

```json
{
  "table_name": "string",
  "analysis_timestamp": "ISO8601",
  "gaps": {
    "column_gaps": [{"column": "...", "current": "...", "target": "...", "severity": "..."}],
    "type_gaps": [...],
    "constraint_gaps": [...],
    "index_gaps": [...],
    "security_gaps": [...],
    "performance_gaps": [...]
  },
  "overall_verdict": "REFACTORED_REQUIRED | NO-CHANGE_OK | MANUAL_GOVERNANCE",
  "migration_type": "A | B | C",
  "iron_laws_triggered": ["IRON-TABLE-XX", ...]
}
```

---

## 3. Migration Types (Schema Migration Governance)

### 3.1 Type A: Pure Technical

**Triggers**:
- Pure spelling error (e.g., `fOdrCode`)
- Pure case inconsistency
- Pure naming style unification

**SQL Pattern**:

```sql
-- Forward
EXEC sp_rename 'schema.table.old_col', 'new_col', 'COLUMN';

-- Rollback
EXEC sp_rename 'schema.table.new_col', 'old_col', 'COLUMN';

-- Validation
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = '<table>' AND COLUMN_NAME IN ('old_col', 'new_col');
-- Expect: 1 row (new only)
```

**Entity Sync**: Required (`[SugarColumn(ColumnName = "new_col")]`)

### 3.2 Type B: Semantic Change

**Triggers**:
- Field semantic changed (e.g., `F_ApplyUser` → `F_InputPerson`)
- Field definition changed (length/type/nullable)
- Field added/removed with data migration

**SQL Pattern**:

```sql
-- Forward (Step 1: Add new)
ALTER TABLE schema.table ADD new_col nvarchar(50) NULL;

-- Forward (Step 2: Data migration)
UPDATE schema.table SET new_col = old_col WHERE new_col IS NULL;

-- Wait 6 months (dual-write period)

-- Forward (Step 3: Drop old, separate CR)
ALTER TABLE schema.table DROP COLUMN old_col;

-- Rollback (immediate)
ALTER TABLE schema.table DROP COLUMN new_col;
```

**Entity Sync**: Required (`[Obsolete]` on old property, dual-property period)

### 3.3 Type C: Low-Code Dynamic

**Triggers**:
- Table name starts with `wform_*` or `lowcode_*`
- Table name starts with `ext_*` AND is runtime user-extended

**Skill Behavior**: SKIP_AUTO_REFACTORING

**Output**:

```yaml
verdict: MANUAL_GOVERNANCE_REQUIRED
reason: "Table is dynamically configured by low-code designer"
required_steps:
  - "Business team written confirmation of semantic change scope"
  - "Impact assessment on all dynamic form instances"
  - "Progressive migration plan (by instance_id batch)"
  - "Application-layer codegen adaptation"
  - "Application-layer flow_form_data_json compatibility"
```

---

## 4. 10 Iron Laws (Constitution)

See `SKILL.md` § "10 Iron Laws (Constitution)" for full definitions. Summary:

| # | Law | Key Requirement |
|---|-----|-----------------|
| IRON-TABLE-01 | No Change ≠ No Action | NO-CHANGE must prove 8-dimension compliance |
| IRON-TABLE-02 | Mapping Is Not Migration | Aliases ≠ real migration; Type A/B/C only |
| IRON-TABLE-03 | Every Table Needs Target Contract | No Contract = no Skill invocation |
| IRON-TABLE-04 | Security Boundary First | P0 tables audited first, 4-dim evidence |
| IRON-TABLE-05 | Performance Claim Requires Measurement | Before/After benchmark mandatory |
| IRON-TABLE-06 | Migration First-Class | 4-file Migration Bundle (forward+rollback+verify+evidence) |
| IRON-TABLE-07 | Runtime Compatibility First | 7-layer runtime chain verified |
| IRON-TABLE-08 | Dynamic Platform Exception | wform_*/lowcode_*/runtime ext_* skipped |
| IRON-TABLE-09 | Evidence Over Declaration | Completion claims bind to evidence files |
| IRON-TABLE-10 | Batch Completion Requires Representative Proof | 1 complex + 1 normal + 1 dynamic |

---

## 5. Risk Levels (v1.0 Retained)

| Level | Trigger | Gate | Behavior |
|-------|---------|------|----------|
| **R0** | Schema-correct, no risk | Auto-Close | Auto execute |
| **R1** | Minor risk (additive index) | Auto-Apply | Auto execute |
| **R2** | Standard risk (most cases) | Evidence-DrivenAuto | Auto execute with evidence |
| **R3+** | High risk (core table, legacy) | Human Approval | Human must approve |
| **R4** | Cross-table | Product + Architecture decision | Cross-batch impact |
| **R5** | Destructive | Product + Architecture + Pilot Dry-run | Highest gate |

**JNPF-specific risk modifiers**:
- Type C low-code → always R3+ regardless of base risk
- P0-Security table → always R3+ regardless of base risk
- f_tenant_id NOT NULL → always R2 (must verify no data loss first)

---

## 6. Evidence Taxonomy (v1.0 Retained)

5 labels only. No second taxonomy permitted.

| Label | Meaning | Example |
|-------|---------|---------|
| `[KNOWN]` | Confirmed via direct measurement | `row_count = 12345 from SELECT COUNT(*)` |
| `[COMPUTED]` | Calculated from known facts | `bytes = rows × avg_row_size` |
| `[INFERRED]` | Logical conclusion from evidence | `password is MD5 based on length = 32` |
| `[GUESS]` | Speculative, low confidence | `user count growth rate is ~20%/year` |
| `[DESIGN]` | Spec-defined value, not measured | `target = datetime2(7) per Target Contract` |

---

## 7. Evidence Thresholds (Sufficiency Stop)

For each Finding Type (A–G), threshold = **minimum evidence to make decision**. Continue searching past threshold is a violation (IRON-TABLE-05 spirit).

| Finding | Threshold |
|---------|-----------|
| A. Schema gap | Current schema + Target contract match |
| B. Integrity violation | Constraint missing + impact assessment |
| C. Index gap | Query pattern + existing index coverage |
| D. Lifecycle gap | Row count + delete ratio + retention need |
| E. Query pattern | Slow query log + EXPLAIN plan |
| F. DDD violation | Entity-Table mismatch + Domain evidence |
| G. Readiness gap | Migration path + rollback feasibility |

---

## 8. 7 Skill DoD (Phase 1 Frozen Gate)

See `SKILL.md` § "7 Skill DoD" for full details. Each DoD requires executable verification:

```bash
# DoD-01: Table Contract Matrix
python -m tsee.contract-matrix --output markdown

# DoD-02: Gap Analysis Layer
python -m tsee.gap-analysis base_user --output json

# DoD-03: Migration Decision Engine
python -m tsee.decide base_user.f_password --output json

# DoD-04: No Change Validator
python -m tsee.no-change-validate flow_task --output yaml

# DoD-05: Evidence Collector
python -m tsee.evidence-collect base_user --bundle evidence/

# DoD-06: Rollback Validator
python -m tsee.rollback-validate V20260831_base_user_password_fields

# DoD-07: Human Gate Boundary
python -m tsee.human-gate-check --auto-only
```

**Frozen requirement**: All 7 DoD 100% PASS.

---

## 9. 3 Simulation Test Cases (Phase 1 Verification)

See `docs/superpowers/specs/2026-08-30-表级重构专家Skill-v2.0-Simulation-Tests.md` for full case definitions.

| Case | Table Type | Expected Skill Behavior |
|------|-----------|------------------------|
| **Case A** | Normal business (e.g., `ext_order`) | Auto-detect Type A → DIRECT_RENAME |
| **Case B** | Low-code dynamic (e.g., `wform_contractapproval`) | Auto-detect Type C → SKIP, require human |
| **Case C** | P0-Security (e.g., `base_user`) | Auto-detect 3 security gaps → REFACTORED_P0, require human |

**Frozen requirement**: All 3 Simulation Cases PASS.

---

## 10. R2-COMP Validation (Phase 1 Frozen Gate)

10 tables total:

- **Round 1 (5 normal)**: Gap Analysis stability check
- **Round 2 (5 adversarial)**: Type B/C boundary check

See `docs/superpowers/plans/2026-08-30-表级重构Skill-v2.0-R2-COMP-验证计划.md` for details.

**Frozen requirement**: 10/10 PASS + 4/4 Safety Gates + 0 critical errors.

---

## 11. Closed Gate (5 Conditions)

A Table Unit is CLOSED only when **all 5 conditions** met:

1. ✅ Evidence sufficient (per §7 thresholds)
2. ✅ Target settled (Target Contract present + Gap Analysis complete)
3. ✅ Refactor or no-change decision made (per IRON-TABLE-01/02)
4. ✅ Verification passed (13 v1.0 DoD + 7 v2.0 DoD + IRON-TABLE-05/07)
5. ✅ No blocking (no Hard Gate triggered, no SVR outstanding)

---

## 12. v1.0 DoD (Retained — 13 Items)

13 v1.0 DoD items retained unchanged. See v1.0 Master Spec §13.2 for full list.

---

## 13. KPI (Phase 8 Retained + v2.0 Additions)

### 13.1 Phase 8 KPIs (Retained)

| KPI | Target | Source |
|-----|--------|--------|
| Hard Gate False Negative | 0 | R2-COMP |
| P0/P1 Error | 0 | R2-COMP |
| Scope Violation | 0 | R2-COMP |
| Closure Error | 0 | R2-COMP |

### 13.2 v2.0 New KPIs

| KPI | Target | Source |
|-----|--------|--------|
| Mapping Bypass Rate | 0 | IRON-TABLE-02 |
| NO-CHANGE Without Evidence | 0 | IRON-TABLE-01 |
| Type C Mis-classified | 0 | IRON-TABLE-08 |
| Migration Without Rollback | 0 | IRON-TABLE-06 |
| Performance Claim Without Measurement | 0 | IRON-TABLE-05 |
| Production DDL Without Human Approval | 0 | DoD-07 |
| Batch Without Representative Proof | 0 | IRON-TABLE-10 |

---

## 14. Purity Gate (v1.0 Retained)

Modifications to this Master Spec require Purity Gate process:

1. Propose change in CR or ADR
2. Cross-check against all 10 Iron Laws (no conflict)
3. Verify against 7 Skill DoD (no dependency break)
4. Test against 3 Simulation Cases (no regression)
5. Run R2-COMP Round (no false positive/negative)
6. Chief Architect approval

---

## 15. Out of Scope (Explicit)

| Item | Reason |
|------|--------|
| Auto Repository code generation | v3.0 candidate |
| Cross-database dialect (MySQL/PG) | v1.0/v2.0 SQL Server only |
| DML data migration | DDL only |
| Auto FK enhancement | JNPF application-layer suffices |
| CQRS / Outbox / Event Sourcing / Microservice split | Future architecture phase |
| Primary key bigint conversion | JNPF GUID is required |

---

## Appendix A · Cross-Reference

| Document | Purpose |
|----------|---------|
| `SKILL.md` | Skill router + 10 Iron Laws summary |
| `master-spec-v2.md` (this file) | Technical standards (WHAT) |
| `execution-manual-v2.md` | Procedures (HOW) |
| `target-contract-template.yaml` | 8-dimension YAML template |
| `JNPF-Target-Schema-Contract.md` | JNPF-specific contracts |
| `Phase1-Verification.md` | 7 DoD + 3 Simulation + R2-COMP results |