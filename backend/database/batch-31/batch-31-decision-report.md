# Batch 31 — Decision Report (Final)

> **Status**: ✅ **AWAITING CHIEF ARCHITECT ACCEPTANCE GATE**
> **Date**: 2026-08-31T15:31:14.705896
> **Master Plan**: v2.1
> **Authorization**: Chief Architect directive 2026-08-31 ("EXECUTE BATCH 31")

---

## 1. Anti-Regression Check

Forbidden judgments found: **0**
- None — all decisions evidence-backed, no forbidden shortcuts

---

## 2. Final Decision Matrix v2


### MIGRATION_REQUIRED: 1 decisions
- **PK-base_signature** — base_signature (primary_key): f_id is the established surrogate key (CLDSEntityBase). Adding PK on f_id is mandatory for SqlSugar ORM operations on this aggregate root. Empty table...

### DEFERRED: 16 decisions
- **PK-base_signature_user** — base_signature_user (primary_key): Two viable PK strategies: composite (business-correct) vs surrogate (ORM-consistent). Cannot determine which without: (a) existing data uniqueness, (b...
- **TI-base_advanced_query_scheme** — base_advanced_query_scheme (tenant_index): NULL tenant values exist (2 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_app_data** — base_app_data (tenant_index): Empty table; cannot measure selectivity. Defer to when data exists (post-Migration, then re-evaluate)
- **TI-base_columns_purview** — base_columns_purview (tenant_index): NULL tenant values exist (1 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_data_interface_user** — base_data_interface_user (tenant_index): NULL tenant values exist (1 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_data_interface_variate** — base_data_interface_variate (tenant_index): NULL tenant values exist (1 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_db_link** — base_db_link (tenant_index): NULL tenant values exist (1 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_im_content** — base_im_content (tenant_index): NULL tenant values exist (9 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_im_reply** — base_im_reply (tenant_index): NULL tenant values exist (2 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_integrate** — base_integrate (tenant_index): NULL tenant values exist (3 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_integrate_node** — base_integrate_node (tenant_index): Empty table; cannot measure selectivity. Defer to when data exists (post-Migration, then re-evaluate)
- **TI-base_organize_relation** — base_organize_relation (tenant_index): Empty table; cannot measure selectivity. Defer to when data exists (post-Migration, then re-evaluate)
- **TI-base_portal** — base_portal (tenant_index): NULL tenant values exist (2 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_portal_data** — base_portal_data (tenant_index): NULL tenant values exist (9 rows); index on f_tenant_id would be partial; needs Data Safety review first
- **TI-base_signature** — base_signature (tenant_index): Empty table; cannot measure selectivity. Defer to when data exists (post-Migration, then re-evaluate)
- **TI-base_signature_user** — base_signature_user (tenant_index): Empty table; cannot measure selectivity. Defer to when data exists (post-Migration, then re-evaluate)


---

## 3. Iron Laws Compliance

- IRON-TABLE-01 No Change ≠ No Action: ✅ All NO_CHANGE have evidence (selectivity + access pattern)
- IRON-TABLE-04 Security Boundary: ✅ PK decisions escalated to human review
- IRON-TABLE-05 Performance Measurement: ✅ Performance evidence collected (SET STATISTICS IO/TIME)
- IRON-TABLE-06 Migration First-Class: ✅ 0 migrations executed; decisions documented
- IRON-TABLE-09 Evidence Over Declaration: ✅ All claims bound to evidence files
- IRON-TABLE-10 Batch Representative: ✅ 17 Gaps reviewed

---

## 4. STOP Confirmation

Per Master Plan v2.1 §15:
> "Batch 31 完成后 STOP。"

**STOPPED. Awaiting Batch 31 Decision Acceptance Gate.**

### Next Action (Chief Architect only)
- **APPROVE MIGRATION** for any MIGRATION_REQUIRED items
- **APPROVE EXCLUDE** for any DEFERRED → EXCLUDED transitions
- **REJECT** with feedback for any decision

### Forbidden in Batch 31
```
ALTER TABLE / CREATE INDEX / DROP / ADD PRIMARY KEY / ADD CONSTRAINT / ALTER COLUMN / UPDATE production data
```

All such operations remain blocked until Chief Architect Approval Gate.

---

**Report complete. STOP confirmed.**
