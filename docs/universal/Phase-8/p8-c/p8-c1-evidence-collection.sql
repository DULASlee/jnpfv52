-- P8-C.1 Production Scope Classification - Evidence Collection
-- Generated: 2026-08-30
-- Purpose: Gather evidence for all 289 physical tables to classify them

SET NOCOUNT ON;

PRINT '=== Evidence Collection for 289 Physical Tables ===';

SELECT
    t.TABLE_NAME AS [Table],
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME) AS Cols,
    ISNULL((SELECT SUM(ps.row_count) FROM sys.dm_db_partition_stats ps
        WHERE ps.object_id = OBJECT_ID(t.TABLE_NAME) AND ps.index_id IN (0,1)), 0) AS [RowCount],
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(t.TABLE_NAME) AND name LIKE 'PK[_]%')
        THEN 1 ELSE 0 END AS HasPK,
    CASE WHEN t.TABLE_NAME = 'sysdiagrams' THEN 1 ELSE 0 END AS IsSystem,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME AND (c.COLUMN_NAME = 'f_tenant_id' OR c.COLUMN_NAME = 'F_TenantId' OR c.COLUMN_NAME = 'F_TENANT_ID')) THEN 1 ELSE 0 END AS HasTenant,
    -- Name pattern matches
    CASE WHEN t.TABLE_NAME LIKE 'base[_]%' OR t.TABLE_NAME LIKE 'BASE[_]%' THEN 1 ELSE 0 END AS P_base,
    CASE WHEN t.TABLE_NAME LIKE 'ext[_]%' THEN 1 ELSE 0 END AS P_ext,
    CASE WHEN t.TABLE_NAME LIKE 'Demo[_]%' THEN 1 ELSE 0 END AS P_demo,
    CASE WHEN t.TABLE_NAME LIKE 'mt%' AND LEN(t.TABLE_NAME) > 14 AND PATINDEX('%[^0-9]%', SUBSTRING(t.TABLE_NAME, 3, 20)) = 0 THEN 1 ELSE 0 END AS P_mt,
    CASE WHEN t.TABLE_NAME LIKE 'wform[_]%' THEN 1 ELSE 0 END AS P_wform,
    CASE WHEN t.TABLE_NAME LIKE 'WH[_]%' OR t.TABLE_NAME LIKE 'WM[_]%' THEN 1 ELSE 0 END AS P_WH,
    CASE WHEN t.TABLE_NAME LIKE 'sa[_]%' THEN 1 ELSE 0 END AS P_sa,
    CASE WHEN t.TABLE_NAME LIKE 'ai[_]%' THEN 1 ELSE 0 END AS P_ai,
    CASE WHEN t.TABLE_NAME LIKE 'kg[_]%' THEN 1 ELSE 0 END AS P_kg,
    CASE WHEN t.TABLE_NAME LIKE 'flow[_]%' THEN 1 ELSE 0 END AS P_flow,
    CASE WHEN t.TABLE_NAME LIKE 'blade[_]%' OR t.TABLE_NAME LIKE 'report%' OR t.TABLE_NAME LIKE 'BASE_REPORT' OR t.TABLE_NAME LIKE 'data[_]report' THEN 1 ELSE 0 END AS P_visual,
    CASE WHEN t.TABLE_NAME LIKE '%[_]BAK[_]%' OR t.TABLE_NAME LIKE '%backup%' OR t.TABLE_NAME LIKE '%bak%' THEN 1 ELSE 0 END AS P_BAK,
    CASE WHEN t.TABLE_NAME LIKE 'student' OR t.TABLE_NAME LIKE 'domain[_]model' OR t.TABLE_NAME LIKE 'student' THEN 1 ELSE 0 END AS P_other_misc
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_SCHEMA = 'dbo' AND t.TABLE_TYPE = 'BASE TABLE'
ORDER BY t.TABLE_NAME;
