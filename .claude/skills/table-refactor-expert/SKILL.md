---
name: table-refactor-expert-v2
description: Use when AI must perform systematic schema evolution on a relational table (gap analysis, real migration planning, evidence-backed verification), especially when current/target schema contract exists and JNPF-style low-code platform runtime compatibility is required (SqlSugar Entity, Repository, Dynamic SQL, Form Engine, Workflow Engine, Permission Engine chain).
---

# Table Schema Evolution Expert — Skill v2.0

> **Version**: v2.0 (DRAFT, Phase 1 verification)
> **Frozen**: Pending (R2-COMP + R1 + 7 DoD + 3 Simulation Cases required)
> **Supersedes**: v1.0-FROZEN (Phase 8 evidence retained, but Skill upgraded to v2.0)
> **Skill Type**: Schema Evolution Architect (was Database Audit in v1.0)

## Overview

This Skill transforms `Current Schema + Target Schema Contract` into validated, evidence-backed Migrations. It is NOT an audit tool — every SchemaChange must produce a runnable Migration Artifact (forward + rollback + validation + evidence bundle).

**Core principle**: A Migration is complete only when the evidence bundle is closed (forward committed, rollback dry-run passed, 8-dimension contract verified, runtime compatibility checked across all 7 layers).

---

## When to Use

Use when **all** of the following are true:

- **Current Schema** is known (sys.columns, sys.indexes, sys.foreign_keys)
- **Target Schema Contract** exists (YAML 8 dimensions — column_naming, data_type, nullable_contract, tenant_model, audit_model, index_contract, constraint_contract, security_boundary)
- Migration decision must be evidence-backed (forward SQL + rollback SQL + validation SQL + evidence bundle)
- JNPF-style runtime compatibility matters (SqlSugar Entity / Repository / Dynamic SQL / Form Engine / Workflow Engine / Permission Engine chain)

Do NOT use when:

- Pure read-only audit without migration intent (use database audit tool)
- Class-level / service-level refactoring (use class-refactor-expert skill)
- Pure query tuning without schema change (use database performance expert)
- No Target Schema Contract exists (must define Contract first per IRON-TABLE-03)
- Schema migration execution without expert assessment (use migration tool directly, not this skill)

---

## 10 Iron Laws (Constitution)

These laws are non-negotiable. Violation = immediate stop + Decision Brief.

### IRON-TABLE-01  No Change ≠ No Action

NO-CHANGE is a valid final state, but must prove **compliance with Target Schema Contract** via 8-dimension evidence. No "I scanned and found nothing" shortcuts.

### IRON-TABLE-02  Mapping Is Not Migration

Column name aliases (e.g., `SELECT F_InputPerson AS F_ApplyUser`) **do not equal** Schema Migration. Three valid paths only:

- **Type A** (Pure Technical): direct `sp_rename`
- **Type B** (Semantic Change): dual-write with 6-month compatibility
- **Type C** (Low-Code Dynamic): skip — manual governance required

### IRON-TABLE-03  Every Table Needs Target Contract

Every table processed by this Skill must have a Target Schema Contract (YAML, 8 dimensions). No Contract = no Skill invocation.

### IRON-TABLE-04  Security Boundary First

Tables involving Identity / Tenant / Permission / User (P0-Security list) are audited first. Output must include security_boundary_audit with 4 evidence dimensions.

### IRON-TABLE-05  Performance Claim Requires Measurement

`Added Index = Performance Improved` is forbidden logic. Every REFACTORED table must produce Before/After measurement: `logical_reads`, `cpu_ms`, `duration_ms`. `logical_reads_reduction >= 50%` to be valid.

### IRON-TABLE-06  Migration First-Class

The deliverable is **executable Migration Artifact**, not a report. Every SchemaChange must produce 4 files:

```
/database/migrations/
├── V<YYYYMMDD>_<change_id>.sql            # Forward
├── V<YYYYMMDD>_<change_id>_down.sql       # Rollback
├── V<YYYYMMDD>_<change_id>_verify.sql     # Validation
└── V<YYYYMMDD>_<change_id>_evidence.json  # Evidence Bundle
```

### IRON-TABLE-07  Runtime Compatibility First

Schema change is not complete until 7-layer runtime chain is verified:

```
Database → ORM (SqlSugar Entity) → Repository (IRepository<T>) 
       → Dynamic SQL (codegen) → Form Engine → Workflow Engine → Permission Engine
```

### IRON-TABLE-08  Dynamic Platform Exception

Low-code platform tables have special classification:

| Type | Rule |
|------|------|
| SYSTEM_CORE (`base_*` P0-Security) | Strict refactoring (IRON-TABLE-04) |
| BUSINESS_ENTITY (flow_*, etc.) | Migration governance (IRON-TABLE-02/03) |
| DYNAMIC_FORM (`wform_*`) | Metadata only, **forbid auto-rename** |
| USER_EXTENDED (`ext_*`) | Forbid auto-rename |

### IRON-TABLE-09  Evidence Over Declaration

Declarations like "X has been refactored" without evidence are invalid. Every completion claim must bind to:

- Forward SQL reference
- Validation SQL with row count
- Rollback dry-run result
- Performance before/after data

### IRON-TABLE-10  Batch Completion Requires Representative Proof

Every Batch close must include representative proof (1 complex + 1 normal + 1 dynamic). No pure-NO-CHANGE batches allowed.

---

## 7 Skill DoD (Phase 1 Frozen Gate)

Skill v2.0 itself must pass **all 7 DoDs** before FROZEN:

| # | DoD | Verification |
|---|-----|-------------|
| **DoD-01** | Target Schema Contract executable (generates Table Contract Matrix) | `python -m tsee.contract-matrix` |
| **DoD-02** | Gap Analysis Layer (6 gap types: column/type/constraint/index/security/performance) | `python -m tsee.gap-analysis <table>` |
| **DoD-03** | Migration Decision Engine (auto Type A/B/C) | `python -m tsee.decide <table>.<column>` |
| **DoD-04** | No Change Validator (8-dimension evidence mandatory) | `python -m tsee.no-change-validate <table>` |
| **DoD-05** | Evidence Collector (auto-collects schema/row_count/index/performance/diff) | `python -m tsee.evidence-collect <table>` |
| **DoD-06** | Rollback Validator (forward+rollback pair + dry-run) | `python -m tsee.rollback-validate <change_id>` |
| **DoD-07** | Human Gate Boundary (explicit AI-auto vs human-required list) | `python -m tsee.human-gate-check --auto-only` |

---

## 5-Layer Architecture

```
Layer 5 · Governance (R2-COMP + R1 + Hard Gate + 7 DoD + 3 Simulation Cases)
Layer 4 · Gap Analysis Layer (NEW) ← Current + Target → 6 gap types
Layer 3 · Migration Planning Layer (NEW) ← Type A/B/C + Real Migration vs Mapping Bypass
Layer 2 · Execution Layer (v1.0 upgraded) ← Risk + DDL + Schema drift + Real Migration tools + Performance Measurement
Layer 1 · Evidence & Verification Layer (v1.0 retained) ← 13 DoD + 5 Closed Gate + Performance Archive
```

**v1.0 → v2.0 key shift**: `Current → Audit → Action` becomes `Current + Target → Gap → Migration → Verification`.

---

## Operational Sequence (10 Steps)

```
1. Load Master Spec v2.0 (canonical reference)
2. Load Execution Manual v2.0 (canonical reference)
3. (Optional) Load Project Profile (JNPF-specific mappings)
4. (Optional) Load Target Profile (target-specific contracts)
5. Initialize TableState = DISCOVERED
6. For each Step in Execution Manual v2.0 §3:
   a. runStep → routeDoc → collect evidence (Sufficiency Stop per IRON-TABLE-05)
   b. Update Ledger (include 8-dimension no-change evidence per IRON-TABLE-01)
   c. If Hard Gate triggered → Decision Brief + STOP (per IRON-TABLE-04)
   d. If Approval Gate required → gate evaluation (per DoD-07)
   e. If Refactor required → applyRefactor with 4-file Migration Bundle (IRON-TABLE-06)
   f. Verify 7-layer runtime chain (IRON-TABLE-07)
   g. Run performance benchmark Before/After (IRON-TABLE-05)
   h. Evaluate Batch representative proof (IRON-TABLE-10)
   i. Evaluate Closed Gate (5 conditions)
7. Transition to CLOSED or escalate
```

---

## Migration Type Decision Matrix

| Type | Trigger | Processing |
|------|---------|------------|
| **A** | Pure technical naming error (typo, case inconsistency) | `sp_rename` + Entity sync |
| **B** | Semantic change (field meaning changed) | Dual-write 6 months + Entity dual-field with `[Obsolete]` |
| **C** | Low-code dynamic (`wform_*`, `lowcode_*`, runtime `ext_*`) | **SKIP** — manual governance |

Decision logic (`Migration Decision Engine` per DoD-03):

```python
def decide_migration_type(table_name, column_name, current_def, target_def):
    if table_name.startswith(("wform_", "lowcode_")):
        return MigrationType.TYPE_C
    if table_name.startswith("ext_") and is_user_extended(table_name):
        return MigrationType.TYPE_C
    if semantic_changed(column_name, current_def, target_def):
        return MigrationType.TYPE_B
    if pure_naming_error(column_name):
        return MigrationType.TYPE_A
    return MigrationType.TYPE_B  # conservative default
```

---

## Output Contract (per table)

| Output | Format | Required |
|--------|--------|----------|
| Evidence Ledger | JSON/YAML | ✅ Always |
| Target Schema Contract | YAML (8 dimensions) | ✅ Always |
| Gap Analysis Report | JSON (6 gap types) | ✅ Always |
| Migration Type | A / B / C | ✅ Always |
| Performance Measurement | Before/After JSON | ✅ REFACTORED |
| Security Audit | YAML | ✅ P0 tables |
| Table Contract Matrix | Markdown Table | ✅ Batch complete |
| Migration Script Bundle | V*.sql + V*_down.sql + V*_verify.sql + evidence.json | ✅ REFACTORED |
| Human Gate Decision | REQUIRED / NOT_REQUIRED | ✅ Always |

---

## Hard Gates (Trigger → STOP)

| Trigger | Action |
|---------|--------|
| P0-Security table missing security_boundary_audit | Decision Brief + STOP |
| Type C table marked REFACTORED (instead of SKIP) | Decision Brief + STOP |
| Migration without rollback script | Decision Brief + STOP |
| Performance claim without Before/After data | Decision Brief + STOP |
| NO-CHANGE without 8-dimension evidence | Decision Brief + STOP |
| Batch without representative proof | Decision Brief + STOP |
| Production DDL without human approval | Decision Brief + STOP |
| DROP COLUMN without Type B 6-month wait | Decision Brief + STOP |

---

## Human Gate Boundary (DoD-07)

**AI auto-authorized** (no human approval):

- Target Schema Contract comparison (read-only)
- Gap Analysis Report generation
- Migration Type classification (A/B/C)
- Forward/Rollback/Validation SQL generation
- Evidence Bundle collection
- Performance benchmark execution
- Dry-run rollback testing

**Human required**:

- Production Forward Migration execution
- Any Type C (low-code) field change
- P0-Security table destructive change
- DROP COLUMN operations
- TRUNCATE TABLE operations
- Batch DDL > 1 table
- Rollback decision after rollback triggered

---

## v1.0 Compatibility (Backward)

| Asset | Compatibility |
|-------|---------------|
| Phase 8 248-table governance | Retained, not retroactive |
| v1.0 SKILL.md | Preserved at v1.5 ARCHIVED |
| v1.0 Master Spec | Retained, v2.0 chapters added |
| Evidence files | Backward-compatible field additions |
| Decision Briefs | v1.0 format still readable |

---

## Quick Start

```bash
# 1. Discover
python -m tsee.discover --tables base_user,base_message,flow_task

# 2. Load contracts (from JNPF Target Schema Contract)
python -m tsee.contract-load --project JNPF

# 3. Gap Analysis
python -m tsee.gap-analysis base_user

# 4. Migration Decision
python -m tsee.decide base_user.f_password

# 5. Generate Migration Bundle (if Type A/B and human-approved)
python -m tsee.migrate base_user --human-approved

# 6. Verify (with 7-layer runtime check + Before/After benchmark)
python -m tsee.verify base_user

# 7. Close (with 8-dimension evidence + Batch representative proof)
python -m tsee.close base_user --batch batch-01
```

---

## Failure Modes

| Failure | Detection | Recovery |
|---------|-----------|----------|
| Migration fails | Validation SQL | Reverse migration + rollback to last good state |
| Runtime layer broken | 7-layer check | Stop propagation + Decision Brief |
| Type C misclassified | Hard Gate | Re-classify to SKIP |
| Evidence bundle missing | DoD-05 check | Regenerate + re-collect |
| Rollback dry-run fails | DoD-06 check | Fix forward migration |

---

## Out of Scope (Explicit)

- Implementing auto Repository code generation (v3.0 candidate)
- Cross-database dialect (MySQL/PG) — v1.0/v2.0 SQL Server only
- DML data migration (DDL only)
- Auto FK enhancement (JNPF doesn't need)
- CQRS / Outbox / Event Sourcing / Microservice split (future architecture phase)
- Primary key bigint conversion (JNPF GUID is required)

---

## Phase 1 Verification (Required for FROZEN)

Before Skill v2.0 can be marked FROZEN, must complete:

1. ✅ 7 DoD all PASS (DoD-01 through DoD-07)
2. ✅ 3 Simulation Cases all PASS (Case A: Type A normal table, Case B: Type C low-code, Case C: P0-Security)
3. ✅ R2-COMP 10/10 PASS (5 normal + 5 adversarial)
4. ✅ R1 Human Governance 5/5 PASS
5. ✅ ADR-024 (v2.0 FROZEN decision) published
6. ✅ v1.0 FROZEN → v1.5 ARCHIVED

See `docs/superpowers/specs/2026-08-30-表级重构专家Skill-v2.0设计规格.md` for full design rationale.

See `docs/superpowers/specs/2026-08-30-JNPF-Target-Schema-Contract.md` for JNPF-specific contracts.

See `docs/superpowers/specs/2026-08-30-表级重构专家Skill-v2.0-Phase1-Verification.md` for verification results.