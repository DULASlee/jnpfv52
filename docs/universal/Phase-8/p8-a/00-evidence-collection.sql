-- P8-A.2 Evidence Collection - 5 Shadow Tables
-- Schema, Indexes, Foreign Keys for all 5 tables

-- Table 1: base_sys_config
SELECT 'base_sys_config' AS TableName, c.name AS ColumnName, tp.name AS DataType, c.max_length AS MaxLen, c.is_nullable AS Nullable
FROM sys.columns c
INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
WHERE c.object_id = OBJECT_ID('BASE_SYS_CONFIG')
ORDER BY c.column_id;
GO

SELECT 'base_sys_config' AS TableName, i.name AS IndexName, i.type_desc AS IndexType, i.is_primary_key AS IsPK,
    STUFF((SELECT ', ' + c2.name FROM sys.index_columns ic JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, '') AS Columns
FROM sys.indexes i WHERE i.object_id = OBJECT_ID('BASE_SYS_CONFIG') AND i.is_hypothetical = 0 ORDER BY i.index_id;
GO

-- Table 2: base_user
SELECT 'base_user' AS TableName, c.name AS ColumnName, tp.name AS DataType, c.max_length AS MaxLen, c.is_nullable AS Nullable
FROM sys.columns c
INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
WHERE c.object_id = OBJECT_ID('BASE_USER')
ORDER BY c.column_id;
GO

SELECT 'base_user' AS TableName, i.name AS IndexName, i.type_desc AS IndexType, i.is_primary_key AS IsPK,
    STUFF((SELECT ', ' + c2.name FROM sys.index_columns ic JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, '') AS Columns
FROM sys.indexes i WHERE i.object_id = OBJECT_ID('BASE_USER') AND i.is_hypothetical = 0 ORDER BY i.index_id;
GO

-- Table 3: base_visual_dev
SELECT 'base_visual_dev' AS TableName, c.name AS ColumnName, tp.name AS DataType, c.max_length AS MaxLen, c.is_nullable AS Nullable
FROM sys.columns c
INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
WHERE c.object_id = OBJECT_ID('BASE_VISUAL_DEV')
ORDER BY c.column_id;
GO

SELECT 'base_visual_dev' AS TableName, i.name AS IndexName, i.type_desc AS IndexType, i.is_primary_key AS IsPK,
    STUFF((SELECT ', ' + c2.name FROM sys.index_columns ic JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, '') AS Columns
FROM sys.indexes i WHERE i.object_id = OBJECT_ID('BASE_VISUAL_DEV') AND i.is_hypothetical = 0 ORDER BY i.index_id;
GO

-- Table 4: ext_table_example
SELECT 'ext_table_example' AS TableName, c.name AS ColumnName, tp.name AS DataType, c.max_length AS MaxLen, c.is_nullable AS Nullable
FROM sys.columns c
INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
WHERE c.object_id = OBJECT_ID('EXT_TABLE_EXAMPLE')
ORDER BY c.column_id;
GO

SELECT 'ext_table_example' AS TableName, i.name AS IndexName, i.type_desc AS IndexType, i.is_primary_key AS IsPK,
    STUFF((SELECT ', ' + c2.name FROM sys.index_columns ic JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, '') AS Columns
FROM sys.indexes i WHERE i.object_id = OBJECT_ID('EXT_TABLE_EXAMPLE') AND i.is_hypothetical = 0 ORDER BY i.index_id;
GO

-- Table 5: sa_data_dictionary
SELECT 'sa_data_dictionary' AS TableName, c.name AS ColumnName, tp.name AS DataType, c.max_length AS MaxLen, c.is_nullable AS Nullable
FROM sys.columns c
INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
WHERE c.object_id = OBJECT_ID('sa_data_dictionary')
ORDER BY c.column_id;
GO

SELECT 'sa_data_dictionary' AS TableName, i.name AS IndexName, i.type_desc AS IndexType, i.is_primary_key AS IsPK,
    STUFF((SELECT ', ' + c2.name FROM sys.index_columns ic JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, '') AS Columns
FROM sys.indexes i WHERE i.object_id = OBJECT_ID('sa_data_dictionary') AND i.is_hypothetical = 0 ORDER BY i.index_id;
GO
