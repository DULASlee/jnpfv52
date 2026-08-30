-- Row count analysis - corrected column name
SELECT 
    t.name AS TableName,
    SUM(CASE WHEN p.index_id IN (0, 1) THEN p.row_count ELSE 0 END) AS TotalRows
FROM sys.tables t
INNER JOIN sys.dm_db_partition_stats p ON t.object_id = p.object_id
WHERE t.is_ms_shipped = 0
GROUP BY t.name
ORDER BY TotalRows DESC, t.name;
GO
