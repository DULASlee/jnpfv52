-- P8-C.1 Production Scope Classification (simplified)
SET NOCOUNT ON;

PRINT '=== P8-C.1 Classification Summary ===';

WITH Evidence AS (
    SELECT
        t.TABLE_NAME AS [Table],
        ISNULL((SELECT SUM(ps.row_count) FROM sys.dm_db_partition_stats ps
            WHERE ps.object_id = OBJECT_ID(t.TABLE_NAME) AND ps.index_id IN (0,1)), 0) AS [RowCount],
        CASE WHEN t.TABLE_NAME = 'sysdiagrams' THEN 1 ELSE 0 END AS IsSystem,
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
        CASE WHEN t.TABLE_NAME LIKE '%[_]BAK[_]%' OR t.TABLE_NAME LIKE '%backup%' OR t.TABLE_NAME LIKE '%bak%' THEN 1 ELSE 0 END AS P_BAK
    FROM INFORMATION_SCHEMA.TABLES t
    WHERE t.TABLE_SCHEMA = 'dbo' AND t.TABLE_TYPE = 'BASE TABLE'
)
SELECT
    Class,
    ClassName,
    Eligibility,
    COUNT(*) AS TableCount
FROM (
    SELECT e.[Table], e.[RowCount],
        CASE
            WHEN e.P_mt = 1 THEN 'D'
            WHEN e.P_BAK = 1 THEN 'D'
            WHEN e.IsSystem = 1 THEN 'D'
            WHEN e.P_demo = 1 THEN 'C'
            WHEN e.[Table] = 'ext_table_example' THEN 'C'
            WHEN e.[Table] = 'student' THEN 'C'
            WHEN e.P_wform = 1 THEN 'B'
            WHEN e.P_ext = 1 THEN 'B'
            WHEN e.[Table] LIKE 'zx[_]%' THEN 'U'
            WHEN e.P_base = 1 OR e.P_WH = 1 OR e.P_sa = 1 OR e.P_ai = 1 OR e.P_kg = 1
                 OR e.P_flow = 1 OR e.P_visual = 1 THEN 'A'
            WHEN e.[Table] IN ('SYS_PROCESSED_EVENT', 'SYS_EVENT_OUTBOX_MESSAGE', 'undo_log',
                               'SchemaVersions', 'PROCESSED_EVENT', 'EVAL_METRIC',
                               'BASE_TENANT_GLOSSARY', 'BASE_TENANT_INDUSTRY',
                               'BASE_FOUNDER_AUTH_LOG', 'BASE_SANDBOX', 'domain_model') THEN 'A'
            ELSE 'U'
        END AS Class,
        CASE
            WHEN e.P_mt = 1 OR e.P_BAK = 1 OR e.IsSystem = 1 THEN 'TEST_FIXTURE'
            WHEN e.P_demo = 1 OR e.[Table] = 'ext_table_example' OR e.[Table] = 'student' THEN 'DEMO_SAMPLE'
            WHEN e.P_wform = 1 OR e.P_ext = 1 THEN 'SYSTEM_TEMPLATE'
            WHEN e.[Table] LIKE 'zx[_]%' THEN 'UNKNOWN'
            ELSE 'PRODUCT_CORE'
        END AS ClassName,
        CASE
            WHEN e.P_mt = 1 OR e.P_BAK = 1 OR e.IsSystem = 1 THEN '3 - OUT_OF_SCOPE'
            WHEN e.P_demo = 1 OR e.[Table] = 'ext_table_example' OR e.[Table] = 'student' THEN '3 - OUT_OF_SCOPE'
            WHEN e.P_wform = 1 OR e.P_ext = 1 THEN '2 - CONDITIONAL'
            WHEN e.[Table] LIKE 'zx[_]%' THEN '4 - HUMAN_DECISION'
            ELSE '1 - IN_SCOPE'
        END AS Eligibility
    FROM Evidence e
) sub
GROUP BY Class, ClassName, Eligibility
ORDER BY Class;

PRINT '';
PRINT '=== Detail: Per-Table Classification ===';

WITH Evidence AS (
    SELECT
        t.TABLE_NAME AS [Table],
        ISNULL((SELECT SUM(ps.row_count) FROM sys.dm_db_partition_stats ps
            WHERE ps.object_id = OBJECT_ID(t.TABLE_NAME) AND ps.index_id IN (0,1)), 0) AS [RowCount],
        CASE WHEN t.TABLE_NAME = 'sysdiagrams' THEN 1 ELSE 0 END AS IsSystem,
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
        CASE WHEN t.TABLE_NAME LIKE '%[_]BAK[_]%' OR t.TABLE_NAME LIKE '%backup%' OR t.TABLE_NAME LIKE '%bak%' THEN 1 ELSE 0 END AS P_BAK
    FROM INFORMATION_SCHEMA.TABLES t
    WHERE t.TABLE_SCHEMA = 'dbo' AND t.TABLE_TYPE = 'BASE TABLE'
)
SELECT
    e.[Table],
    e.[RowCount],
    CASE
        WHEN e.P_mt = 1 OR e.P_BAK = 1 OR e.IsSystem = 1 THEN 'D'
        WHEN e.P_demo = 1 OR e.[Table] = 'ext_table_example' OR e.[Table] = 'student' THEN 'C'
        WHEN e.P_wform = 1 OR e.P_ext = 1 THEN 'B'
        WHEN e.[Table] LIKE 'zx[_]%' THEN 'U'
        WHEN e.P_base = 1 OR e.P_WH = 1 OR e.P_sa = 1 OR e.P_ai = 1 OR e.P_kg = 1
             OR e.P_flow = 1 OR e.P_visual = 1 THEN 'A'
        WHEN e.[Table] IN ('SYS_PROCESSED_EVENT', 'SYS_EVENT_OUTBOX_MESSAGE', 'undo_log',
                           'SchemaVersions', 'PROCESSED_EVENT', 'EVAL_METRIC',
                           'BASE_TENANT_GLOSSARY', 'BASE_TENANT_INDUSTRY',
                           'BASE_FOUNDER_AUTH_LOG', 'BASE_SANDBOX', 'domain_model') THEN 'A'
        ELSE 'U'
    END AS Class
FROM Evidence e
ORDER BY
    CASE
        WHEN e.P_mt = 1 OR e.P_BAK = 1 OR e.IsSystem = 1 THEN 1
        WHEN e.P_demo = 1 OR e.[Table] = 'ext_table_example' OR e.[Table] = 'student' THEN 2
        WHEN e.[Table] LIKE 'zx[_]%' THEN 3
        WHEN e.P_wform = 1 OR e.P_ext = 1 THEN 4
        ELSE 5
    END,
    e.[Table];
