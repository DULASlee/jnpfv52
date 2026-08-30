-- Check Views (to exclude from Table Units)
SELECT 
    s.name AS SchemaName,
    v.name AS ViewName
FROM sys.views v
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
WHERE v.is_ms_shipped = 0
ORDER BY s.name, v.name;
GO

-- Check for any custom types / table types
SELECT 
    s.name AS SchemaName,
    t.name AS TypeName,
    t.is_table_type AS IsTableType
FROM sys.types t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.is_user_defined = 1
ORDER BY s.name, t.name;
GO

-- Check for tables with >0 row count (to identify dynamic / active tables)
SELECT TOP 50
    t.name AS TableName,
    p.rows AS RowCount
FROM sys.tables t
INNER JOIN sys.dm_db_partition_stats p ON t.object_id = p.object_id
WHERE t.is_ms_shipped = 0
  AND p.index_id IN (0, 1)
ORDER BY p.rows DESC;
GO
