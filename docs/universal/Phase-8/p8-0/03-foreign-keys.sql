-- Foreign Key relationships
SELECT 
    fk.name AS FKName,
    SCHEMA_NAME(fk.schema_id) AS FKSchema,
    OBJECT_NAME(fk.parent_object_id) AS FromTable,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS FromColumn,
    OBJECT_NAME(fk.referenced_object_id) AS ToTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ToColumn,
    fk.delete_referential_action_desc AS DeleteAction,
    fk.update_referential_action_desc AS UpdateAction
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
ORDER BY OBJECT_NAME(fk.parent_object_id), OBJECT_NAME(fk.referenced_object_id);
GO

-- FK count by table
SELECT 
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COUNT(*) AS OutgoingFKCount
FROM sys.foreign_keys fk
GROUP BY OBJECT_NAME(fk.parent_object_id)
ORDER BY OutgoingFKCount DESC;
GO

-- Reverse: tables referenced by other tables (incoming FK)
SELECT 
    OBJECT_NAME(fk.referenced_object_id) AS TableName,
    COUNT(*) AS IncomingFKCount
FROM sys.foreign_keys fk
GROUP BY OBJECT_NAME(fk.referenced_object_id)
ORDER BY IncomingFKCount DESC;
GO
