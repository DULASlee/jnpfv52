-- Row count analysis for all tables
SELECT 
    t.name AS TableName,
    SUM(p.rows) AS TotalRows
FROM sys.tables t
INNER JOIN sys.dm_db_partition_stats p ON t.object_id = p.object_id
WHERE t.is_ms_shipped = 0
  AND p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY SUM(p.rows) DESC, t.name;
GO
