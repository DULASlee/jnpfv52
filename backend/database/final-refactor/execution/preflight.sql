-- WAVE 2 Preflight: M32-01 (base_signature) + M32-02 (base_signature_user)
-- READ-ONLY — no modifications to schema or data
-- Execute BEFORE migration.sql
-- Chief Architect Authorization: APPROVED 2026-08-31

SET NOCOUNT ON;

-- ============================================================
-- M32-01 PREFLIGHT: base_signature (single-column PK on F_ID)
-- ============================================================

-- Check 1: Row count (empty = safe migration)
SELECT 'base_signature.row_count' AS [check], COUNT(*) AS [value]
FROM dbo.base_signature;
-- Expected: 0

-- Check 2: F_ID uniqueness
SELECT 'base_signature.f_id_unique' AS [check],
    COUNT(*) AS [total], COUNT(DISTINCT f_id) AS [distinct_f_id]
FROM dbo.base_signature;
-- Expected: total = distinct_f_id

-- Check 3: F_ID null count
SELECT 'base_signature.f_id_null' AS [check], COUNT(*) AS [null_count]
FROM dbo.base_signature WHERE f_id IS NULL;
-- Expected: 0 NULLs

-- Check 4: Existing PK
SELECT 'base_signature.pk_count' AS [check], COUNT(*) AS [pk_count]
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.base_signature') AND is_primary_key = 1;
-- Expected: 0

-- Check 5: FK references TO base_signature
SELECT
    fk.name AS fk_name,
    OBJECT_NAME(fk.parent_object_id) AS child_table,
    OBJECT_NAME(fk.referenced_object_id) AS parent_table
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID('dbo.base_signature');
-- Expected: 0 rows

-- ============================================================
-- M32-02 PREFLIGHT: base_signature_user (composite PK on F_SIGNATURE_ID, F_USER_ID)
-- ============================================================

-- Check 6: Row count
SELECT 'base_signature_user.row_count' AS [check], COUNT(*) AS [value]
FROM dbo.base_signature_user;
-- Expected: 0

-- Check 7: F_SIGNATURE_ID null count
SELECT 'base_signature_user.f_signature_id_null' AS [check], COUNT(*) AS [null_count]
FROM dbo.base_signature_user WHERE f_signature_id IS NULL;
-- Expected: 0

-- Check 8: F_USER_ID null count
SELECT 'base_signature_user.f_user_id_null' AS [check], COUNT(*) AS [null_count]
FROM dbo.base_signature_user WHERE f_user_id IS NULL;
-- Expected: 0

-- Check 9: Composite uniqueness
SELECT 'base_signature_user.composite_unique' AS [check],
    COUNT(*) AS [total],
    COUNT(DISTINCT (f_signature_id + '|' + f_user_id)) AS [distinct_composite]
FROM dbo.base_signature_user;
-- Expected: total = distinct_composite (0 if empty)

-- Check 10: Existing PK on base_signature_user
SELECT 'base_signature_user.pk_count' AS [check], COUNT(*) AS [pk_count]
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.base_signature_user') AND is_primary_key = 1;
-- Expected: 0

-- Check 11: FK references TO base_signature_user
SELECT
    fk.name AS fk_name,
    OBJECT_NAME(fk.parent_object_id) AS child_table,
    OBJECT_NAME(fk.referenced_object_id) AS parent_table
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID('dbo.base_signature_user');
-- Expected: 0 rows

-- ============================================================
-- MIGRATION GATE: ALL CHECKS MUST PASS
-- Any non-zero/null count in null checks = STOP
-- Any duplicate in uniqueness checks = STOP
-- Any existing PK = STOP
-- Any FK dependency found = STOP
-- ============================================================
PRINT '=== WAVE 2 PREFLIGHT COMPLETE ===';
PRINT 'If all checks returned expected values (0/null), proceed to migration.sql';