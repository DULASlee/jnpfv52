-- Self-referencing tables (recursive FKs)
SELECT 
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = fk.referenced_object_id
ORDER BY TableName;
GO

-- Tenant column presence (F_TENANT_ID)
SELECT 
    t.name AS TableName,
    COUNT(*) AS TenantColCount,
    MAX(CASE WHEN c.name = 'F_TENANT_ID' THEN c.name ELSE NULL END) AS HasTenantCol
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.is_ms_shipped = 0
  AND c.name = 'F_TENANT_ID'
GROUP BY t.name
ORDER BY t.name;
GO

-- Soft delete column presence
SELECT 
    t.name AS TableName,
    MAX(CASE WHEN c.name = 'F_DELETE_MARK' OR c.name = 'F_DELETEMARK' OR c.name = 'DELETE_MARK' THEN 1 ELSE 0 END) AS HasSoftDelete
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.is_ms_shipped = 0
GROUP BY t.name
HAVING MAX(CASE WHEN c.name = 'F_DELETE_MARK' OR c.name = 'F_DELETEMARK' OR c.name = 'DELETE_MARK' THEN 1 ELSE 0 END) = 1
ORDER BY t.name;
GO

-- Column count per table
SELECT TOP 30
    t.name AS TableName,
    COUNT(c.column_id) AS ColumnCount
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.is_ms_shipped = 0
GROUP BY t.name
ORDER BY COUNT(c.column_id) DESC;
GO
