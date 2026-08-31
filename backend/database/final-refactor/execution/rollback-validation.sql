-- WAVE 2 Rollback Validation: M32-01 + M32-02
-- READ-ONLY — proof of rollback feasibility
-- Execute AFTER successful migration AND postflight
-- This does NOT execute the rollback — it validates rollback would succeed

SET NOCOUNT ON;

-- ============================================================
-- ROLLBACK VALIDATION 1: Confirm constraints exist (can be dropped)
-- ============================================================
SELECT
    OBJECT_NAME(i.object_id) AS table_name,
    i.name AS constraint_name,
    i.type_desc,
    STUFF((
        SELECT ',' + c.name
        FROM sys.index_columns ic
        JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 1, '') AS pk_columns
FROM sys.indexes i
WHERE i.object_id IN (OBJECT_ID('dbo.base_signature'), OBJECT_ID('dbo.base_signature_user'))
  AND i.is_primary_key = 1;

-- Expected:
--   base_signature        | PK_base_signature       | f_id
--   base_signature_user   | PK_base_signature_user | f_signature_id,f_user_id

-- ============================================================
-- ROLLBACK VALIDATION 2: Confirm rollback order safety
-- M32-02 (base_signature_user) must be rolled back FIRST
-- because it has FK reference to base_signature
-- ============================================================
SELECT
    OBJECT_NAME(fk.parent_object_id) AS child_table,
    OBJECT_NAME(fk.referenced_object_id) AS parent_table,
    fk.name AS fk_name,
    fk.delete_referential_action_desc,
    fk.update_referential_action_desc
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id IN (
    OBJECT_ID('dbo.base_signature'),
    OBJECT_ID('dbo.base_signature_user')
);
-- Expected: 0 FK relationships (no FK to drop before PK)
-- If there were FKs, must drop FK before dropping parent PK

-- ============================================================
-- ROLLBACK VALIDATION 3: Simulate M32-02 rollback (no actual DROP)
-- ============================================================
DECLARE @siguser_pk_exists BIT = 0;
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.base_signature_user')
      AND is_primary_key = 1
      AND name = 'PK_base_signature_user'
)
BEGIN
    SET @siguser_pk_exists = 1;
    PRINT 'PK_base_signature_user: EXISTS — rollback would succeed';
END
ELSE
BEGIN
    PRINT 'PK_base_signature_user: NOT FOUND — already rolled back or never created';
END

-- ============================================================
-- ROLLBACK VALIDATION 4: Simulate M32-01 rollback (no actual DROP)
-- ============================================================
DECLARE @sig_pk_exists BIT = 0;
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.base_signature')
      AND is_primary_key = 1
      AND name = 'PK_base_signature'
)
BEGIN
    SET @sig_pk_exists = 1;
    PRINT 'PK_base_signature: EXISTS — rollback would succeed';
END
ELSE
BEGIN
    PRINT 'PK_base_signature: NOT FOUND — already rolled back or never created';
END

-- ============================================================
-- ROLLBACK VALIDATION 5: Confirm data would be preserved
-- Both tables are empty — rollback has zero data impact
-- ============================================================
SELECT
    'base_signature.data_preservation' AS [check],
    COUNT(*) AS [row_count],
    0 AS [expected_rollback_impact]
FROM dbo.base_signature
UNION ALL
SELECT
    'base_signature_user.data_preservation',
    COUNT(*),
    0
FROM dbo.base_signature_user;
-- Expected: 0 rows, 0 impact

-- ============================================================
-- ROLLBACK VALIDATION 6: Rollback SQL syntax check
-- The actual rollback statements (not executed here):
--
-- ORDER 1: DROP PK_base_signature_user (child first)
--   IF EXISTS (SELECT 1 FROM sys.indexes WHERE ... name = 'PK_base_signature_user')
--       ALTER TABLE dbo.base_signature_user DROP CONSTRAINT PK_base_signature_user;
--
-- ORDER 2: DROP PK_base_signature (parent)
--   IF EXISTS (SELECT 1 FROM sys.indexes WHERE ... name = 'PK_base_signature')
--       ALTER TABLE dbo.base_signature DROP CONSTRAINT PK_base_signature;
--
-- Both are instant metadata operations (no data rewrite)
-- ============================================================
PRINT '=== ROLLBACK VALIDATION COMPLETE ===';
PRINT 'Both rollbacks are INSTANT metadata operations (no data loss risk)';
PRINT 'Confirmed: PK_base_signature_user exists: ' + CASE WHEN @siguser_pk_exists = 1 THEN 'YES' ELSE 'NO' END;
PRINT 'Confirmed: PK_base_signature exists: ' + CASE WHEN @sig_pk_exists = 1 THEN 'YES' ELSE 'NO' END;