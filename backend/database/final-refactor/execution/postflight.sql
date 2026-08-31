-- WAVE 2 Postflight: M32-01 + M32-02 validation
-- READ-ONLY — verification only
-- Execute AFTER migration.sql completes successfully

SET NOCOUNT ON;

-- ============================================================
-- POSTFLIGHT 1: Verify PK constraints exist
-- ============================================================
SELECT
    OBJECT_NAME(i.object_id) AS table_name,
    i.name AS pk_name,
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
--   base_signature         | PK_base_signature         | CLUSTERED | f_id
--   base_signature_user    | PK_base_signature_user    | CLUSTERED | f_signature_id,f_user_id

-- ============================================================
-- POSTFLIGHT 2: Verify row counts unchanged (0 rows expected)
-- ============================================================
SELECT 'base_signature' AS [table], COUNT(*) AS [row_count] FROM dbo.base_signature
UNION ALL
SELECT 'base_signature_user', COUNT(*) FROM dbo.base_signature_user;
-- Expected: 0 / 0 (no data migration occurred)

-- ============================================================
-- POSTFLIGHT 3: Data integrity — no nulls in PK columns
-- ============================================================
SELECT
    'base_signature.f_id_null' AS [check],
    SUM(CASE WHEN f_id IS NULL THEN 1 ELSE 0 END) AS [null_count]
FROM dbo.base_signature;

SELECT
    'base_signature_user.f_signature_id_null' AS [check],
    SUM(CASE WHEN f_signature_id IS NULL THEN 1 ELSE 0 END) AS [null_count]
FROM dbo.base_signature_user;

SELECT
    'base_signature_user.f_user_id_null' AS [check],
    SUM(CASE WHEN f_user_id IS NULL THEN 1 ELSE 0 END) AS [null_count]
FROM dbo.base_signature_user;
-- Expected: all zeros

-- ============================================================
-- POSTFLIGHT 4: Composite uniqueness on base_signature_user
-- ============================================================
SELECT
    'base_signature_user.composite_unique' AS [check],
    COUNT(*) - COUNT(DISTINCT (f_signature_id + '|' + f_user_id)) AS [duplicate_composite_count]
FROM dbo.base_signature_user;
-- Expected: 0 duplicates

-- ============================================================
-- POSTFLIGHT 5: SqlSugar compatibility check
-- Verify PK exists so ORM Insertable/Updateable operations work
-- ============================================================
SELECT
    OBJECT_NAME(object_id) AS table_name,
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.' + OBJECT_NAME(object_id)) AND is_primary_key = 1
    ) THEN 'PK_EXISTS_SqlSugar_COMPATIBLE' ELSE 'PK_MISSING' END AS sqlsugar_compat
FROM dbo.base_signature
UNION ALL
SELECT
    OBJECT_NAME(object_id),
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.' + OBJECT_NAME(object_id)) AND is_primary_key = 1
    ) THEN 'PK_EXISTS_SqlSugar_COMPATIBLE' ELSE 'PK_MISSING' END
FROM dbo.base_signature_user;
-- Expected: PK_EXISTS_SqlSugar_COMPATIBLE for both

-- ============================================================
-- POSTFLIGHT 6: No unexpected schema changes on other objects
-- ============================================================
SELECT
    OBJECT_NAME(object_id) AS table_name,
    type_desc,
    name AS index_name,
    is_primary_key,
    is_unique
FROM sys.indexes
WHERE object_id IN (OBJECT_ID('dbo.base_signature'), OBJECT_ID('dbo.base_signature_user'))
ORDER BY object_id, is_primary_key DESC;
-- Expected: exactly 1 PK per table, no extra indexes

-- ============================================================
-- POSTFLIGHT 7: FK references still intact
-- ============================================================
SELECT
    OBJECT_NAME(fk.parent_object_id) AS parent_table,
    OBJECT_NAME(fk.referenced_object_id) AS ref_table,
    fk.name AS fk_name
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id IN (
    OBJECT_ID('dbo.base_signature'),
    OBJECT_ID('dbo.base_signature_user')
);
-- Expected: 0 (no FK dependencies found)

-- ============================================================
-- POSTFLIGHT 8: SqlSugar [Navigate] FK column integrity
-- Verify f_signature_id values reference valid f_id in base_signature
-- ============================================================
SELECT
    COUNT(*) - COUNT(DISTINCT f.f_signature_id) AS orphaned_fk_count
FROM dbo.base_signature_user f
LEFT JOIN dbo.base_signature s ON s.f_id = f.f_signature_id
WHERE f.f_signature_id IS NOT NULL;
-- Expected: 0 orphaned FKs (table is empty, so this should be 0)

-- ============================================================
-- POSTFLIGHT 9: Estimated index size (empty table = ~8KB per table)
-- ============================================================
SELECT
    OBJECT_NAME(i.object_id) AS table_name,
    i.name AS index_name,
    p.rows,
    SUM(ps.used_page_count) * 8 / 1024.0 AS size_mb
FROM sys.indexes i
JOIN sys.dm_db_partition_stats ps ON i.object_id = ps.object_id AND i.index_id = ps.index_id
JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
WHERE i.object_id IN (OBJECT_ID('dbo.base_signature'), OBJECT_ID('dbo.base_signature_user'))
GROUP BY i.object_id, i.name, i.type_desc, p.rows;
-- For empty tables: 0 rows, ~8KB per PK index

-- ============================================================
-- GATE: WAVE 2 Postflight
-- All checks must return expected results.
-- Any deviation = STOP, record evidence, do NOT proceed.
-- ============================================================
PRINT '=== WAVE 2 POSTFLIGHT COMPLETE ===';