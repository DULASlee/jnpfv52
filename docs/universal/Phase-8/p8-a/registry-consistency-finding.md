# Registry Consistency Finding — P8-0

> **Phase**: 8 — P8-A (logged during execution)
> **Status**: LOGGED — non-blocking
> **Date**: 2026-08-30

---

## Issue

P8-0 Calibration reported:

```
Physical tables: 289
Entity-mapped tables: 164
Tables WITHOUT entity mapping: 128
```

Math check:
```
164 + 128 = 292
289 (actual physical tables) = 289
```

**Discrepancy**: 292 vs 289 → 3 table difference.

---

## Likely Causes (to verify)

1. **One-to-many mapping**: Some entities map to multiple physical tables (e.g., [SugarTable("name1")] and [SugarTable("name2")] on different entities → counted twice in 164 but only once in 289)
2. **Case sensitivity**: Some table names appear with mixed case in code (e.g., `BASE_AI_Call_LOG` from `AiCallLogEntity.cs` vs `BASE_AI_CALL_LOG` from `AiCallLogEntity.cs`) — PowerShell Sort-Object -Unique is case-INsensitive by default but case-SENSITIVE for some operations
3. **Entity-DB name alias**: Some tables have both an entity with `[SugarTable("X")]` AND another table with similar name (e.g., base_user vs BASE_USER)

---

## Resolution Plan (non-blocking)

- [ ] **P8-0 Maintenance Task**: Recount with case-sensitive comparison
- [ ] **Verify** if any entity has duplicate `[SugarTable]` mappings
- [ ] **Identify** 3 phantom entities (if any) or 3 missed tables
- [ ] **Update Registry** to reflect precise mapping
- [ ] **Do NOT** re-open P8-0 Calibration — log and fix during maintenance window

---

## Current Action

This is logged. P8-A execution continues.

```
P8-0: CLOSED ✅
Registry Consistency: LOGGED 📋
P8-A.2: IN PROGRESS
```

The Registry will be corrected during next maintenance cycle without re-opening P8-0.
