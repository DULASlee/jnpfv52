-- P8-0 Inventory Extraction Script
-- Output: All user tables in current database
SELECT 
    s.name AS SchemaName,
    t.name AS TableName,
    t.type_desc AS TableType,
    t.create_date AS CreatedDate,
    t.modify_date AS ModifiedDate
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;
GO

-- Table count by schema
SELECT 
    s.name AS SchemaName,
    COUNT(*) AS TableCount
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.is_ms_shipped = 0
GROUP BY s.name
ORDER BY s.name;
GO

-- Total table count
SELECT COUNT(*) AS TotalUserTables
FROM sys.tables t
WHERE t.is_ms_shipped = 0;
GO
