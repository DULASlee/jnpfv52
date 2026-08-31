-- M32-Preflight: base_signature + base_signature_user PK validation
-- READ-ONLY — no modifications to schema or data
-- Run before M32-Migration

SET NOCOUNT ON;

-- ============================================================
-- Pre-Check 1: Row count must be 0 (empty table safe migration)
-- ============================================================
SELECT 'base_signature.row_count' AS [check], COUNT(*) AS [value]
FROM dbo.base_signature;
-- Expected: 0 (migration safe per Batch 31 evidence)

SELECT 'base_signature_user.row_count' AS [check], COUNT(*) AS [value]
FROM dbo.base_signature_user;
-- Expected: 0 (migration safe per Batch 31 evidence)

-- ============================================================
-- Pre-Check 2: f_id uniqueness on base_signature
-- ============================================================
SELECT 'base_signature.f_id_unique' AS [check],
    COUNT(*) AS [total], COUNT(DISTINCT f_id) AS [distinct_f_id]
FROM dbo.base_signature;
-- Expected: total = distinct (no duplicates)
-- If total != distinct → STOP, do not proceed

SELECT 'base_signature.f_id_null' AS [check], COUNT(*) AS [null_count]
FROM dbo.base_signature WHERE f_id IS NULL;
-- Expected: 0 NULLs (PK requires NOT NULL)
-- If > 0 → STOP

-- ============================================================
-- Pre-Check 3: Composite uniqueness on base_signature_user
-- ============================================================
SELECT 'base_signature_user.composite_unique' AS [check],
    COUNT(*) AS [total],
    COUNT(DISTINCT (f_signature_id + '|' + f_user_id)) AS [distinct_composite]
FROM dbo.base_signature_user;
-- Expected: total = distinct_composite
-- If not → STOP, decide data cleanup before composite PK

SELECT 'base_signature_user.signature_id_null' AS [check],
    COUNT(*) AS [null_count]
FROM dbo.base_signature_user WHERE f_signature_id IS NULL;
-- Expected: 0 NULLs

SELECT 'base_signature_user.user_id_null' AS [check],
    COUNT(*) AS [null_count]
FROM dbo.base_signature_user WHERE f_user_id IS NULL;
-- Expected: 0 NULLs

-- ============================================================
-- Pre-Check 4: Existing constraints on target tables
-- ============================================================
SELECT 'base_signature.pk_count' AS [check], COUNT(*) AS [pk_count]
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.base_signature') AND is_primary_key = 1;
-- Expected: 0 (no PK yet)

SELECT 'base_signature_user.pk_count' AS [check], COUNT(*) AS [pk_count]
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.base_signature_user') AND is_primary_key = 1;
-- Expected: 0 (no PK yet)

-- ============================================================
-- Pre-Check 5: FK references that would break if PK added
-- ============================================================
SELECT
    fk.name AS fk_name,
    OBJECT_NAME(fk.parent_object_id) AS parent_table,
    OBJECT_NAME(fk.referenced_object_id) AS ref_table
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id IN (
    OBJECT_ID('dbo.base_signature'),
    OBJECT_ID('dbo.base_signature_user')
);
-- Expected: 0 (per Batch 31.1 evidence)

-- ============================================================
-- Pre-Check 6: Estimate of migration lock time
-- ============================================================
-- For empty tables, this is instant (< 1ms)
-- For non-empty tables, ADD PK takes metadata lock + writes index

SELECT 'base_signature.lock_estimate_ms' AS [check], 1 AS [est_ms];
SELECT 'base_signature_user.lock_estimate_ms' AS [check], 1 AS [est_ms];
-- Empty table: instant. Non-empty: 1-10 sec per million rows.

-- ============================================================
-- GATE: M32-Preflight
-- ============================================================
-- All checks above must complete without ERROR.
-- Any row count > 0 or any NULL > 0 requires MANUAL REVIEW before M32-Migration.
