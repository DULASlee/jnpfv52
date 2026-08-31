-- M32-Validation: Post-migration verification
-- READ-ONLY — verification only, no modifications

SET NOCOUNT ON;

-- ============================================================
-- Validation 1: PK constraints exist
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
-- Validation 2: Row count unchanged (0 rows expected)
-- ============================================================
SELECT 'base_signature' AS [table], COUNT(*) AS [row_count] FROM dbo.base_signature
UNION ALL
SELECT 'base_signature_user', COUNT(*) FROM dbo.base_signature_user;
-- Expected: 0 / 0 (no data migration occurred)

-- ============================================================
-- Validation 3: Data integrity preserved
-- ============================================================
SELECT
    OBJECT_NAME(object_id) AS table_name,
    SUM(CASE WHEN f_id IS NULL THEN 1 ELSE 0 END) AS null_count,
    COUNT(*) - COUNT(DISTINCT f_id) AS duplicate_count
FROM dbo.base_signature
GROUP BY object_id
UNION ALL
SELECT
    OBJECT_NAME(object_id),
    SUM(CASE WHEN f_id IS NULL THEN 1 ELSE 0 END),
    COUNT(*) - COUNT(DISTINCT f_id)
FROM dbo.base_signature_user
GROUP BY object_id;
-- Expected: all zeros

-- ============================================================
-- Validation 4: Composite uniqueness on base_signature_user
-- ============================================================
SELECT
    COUNT(*) - COUNT(DISTINCT (f_signature_id + '|' + f_user_id)) AS duplicate_composite_count
FROM dbo.base_signature_user;
-- Expected: 0

-- ============================================================
-- Validation 5: SqlSugar can map (validation through schema metadata)
-- ============================================================
-- SqlSugar Insertable/Updateable requires PK on target table.
-- Verify PK exists on both tables.
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
-- Validation 6: FK references unaffected
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
-- Expected: 0 (per Batch 31.1 evidence)

-- ============================================================
-- Validation 7: Index size impact
-- ============================================================
SELECT
    OBJECT_NAME(i.object_id) AS table_name,
    i.name AS index_name,
    i.type_desc,
    p.rows,
    SUM(ps.used_page_count) * 8 / 1024.0 AS size_mb
FROM sys.indexes i
JOIN sys.dm_db_partition_stats ps ON i.object_id = ps.object_id AND i.index_id = ps.index_id
JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
WHERE i.object_id IN (OBJECT_ID('dbo.base_signature'), OBJECT_ID('dbo.base_signature_user'))
GROUP BY i.object_id, i.name, i.type_desc, p.rows;
-- For empty tables: 0 rows, ~8KB per page

-- ============================================================
-- GATE: M32-Validation
-- All checks above must return expected results.
-- Any deviation = STOP, do not proceed to Phase 33.
