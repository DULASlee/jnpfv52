namespace JNPF.VisualDev.Delete;

/// <summary>
/// Pure SQL planner for batch delete of VisualDev table-backed data.
/// Extracted from RunService.BatchDelHaveTableData — same SQL shapes, no I/O.
/// </summary>
public static class BatchDeleteSqlPlanner
{
    /// <summary>
    /// Soft-delete (logical) UPDATE for the main table.
    /// </summary>
    public static string BuildLogicalDeleteSql(
        string mainTableName,
        string mainPrimary,
        string userId,
        DateTime deleteTime,
        IReadOnlyList<string> ids,
        int deleteRule)
    {
        var idList = string.Join("','", ids);
        var predicate = deleteRule.Equals(0)
            ? $"{mainPrimary} not in ('{idList}')"
            : $"{mainPrimary} in ('{idList}')";
        return string.Format(
            "update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3}",
            mainTableName,
            userId,
            deleteTime,
            predicate);
    }

    /// <summary>
    /// Hard-delete DELETE statements: main table + child tables (typeId == "0").
    /// </summary>
    public static List<string> BuildHardDeleteSql(
        string mainTable,
        string mainPrimary,
        IReadOnlyList<(string table, string tableField)> childTables,
        IReadOnlyList<string> ids,
        int deleteRule)
    {
        var idList = string.Join("','", ids);
        var sql = new List<string>();

        if (deleteRule.Equals(0))
            sql.Add(string.Format("delete from {0} where {1} not in ('{2}');", mainTable, mainPrimary, idList));
        else
            sql.Add(string.Format("delete from {0} where {1} in ('{2}');", mainTable, mainPrimary, idList));

        foreach (var (table, tableField) in childTables)
        {
            if (deleteRule.Equals(0))
                sql.Add(string.Format("delete from {0} where {1} not in ('{2}');", table, tableField, idList));
            else
                sql.Add(string.Format("delete from {0} where {1} in ('{2}');", table, tableField, idList));
        }

        return sql;
    }

    /// <summary>
    /// Truncate-style deletes when no id list is supplied (all tables).
    /// </summary>
    public static List<string> BuildClearAllTablesSql(IEnumerable<string> tableNames)
        => tableNames.Select(t => string.Format("delete from {0}", t)).ToList();
}
