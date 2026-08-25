SET NOCOUNT ON;
SELECT t.name + CHAR(9) +
       LEFT(t.name, CHARINDEX('_', t.name + '_') - 1) + CHAR(9) +
       CAST(p.rows AS varchar(12)) + CHAR(9) +
       CAST((SELECT COUNT(*) FROM sys.columns c2 WHERE c2.object_id = t.object_id) AS varchar(6)) + CHAR(9) +
       ISNULL((SELECT TOP 1 c3.name FROM sys.columns c3 WHERE c3.object_id = t.object_id AND c3.name IN ('f_tenant_id','F_TenantId','tenant_id')), 'NONE') + CHAR(9) +
       ISNULL((SELECT TOP 1 ty.name FROM sys.key_constraints kc
                 JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
                 JOIN sys.columns c4 ON c4.object_id = ic.object_id AND c4.column_id = ic.column_id
                 JOIN sys.types ty ON c4.user_type_id = ty.user_type_id
                WHERE kc.parent_object_id = t.object_id AND kc.type = 'PK'), 'NONE')
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
ORDER BY t.name;
