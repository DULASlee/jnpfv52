namespace JNPF.VisualDev.Query;

/// <summary>
/// Pure SQL fragment builders for RunService.GetListQuerySql
/// (IN-subquery wraps, child-table empty match, outer list wrap).
/// DB ToSqlString / tenant column checks stay in the service.
/// </summary>
public static class ListQuerySqlFragmentHelpers
{
    public const string SelectFromTemplate = "select {0} from {1} ";

    /// <summary>
    /// select {idField} from {table}  (idField falls back to primaryKey).
    /// </summary>
    public static string BuildSelectFrom(string? idField, string primaryKey, string tableName)
        => string.Format(SelectFromTemplate, string.IsNullOrEmpty(idField) ? primaryKey : idField, tableName);

    /// <summary>
    /// ({primary} in ({itemSql})) — standard match fragment.
    /// </summary>
    public static string BuildPrimaryInSubquery(string primaryKey, string itemSql)
        => string.Format(" ({0} in ({1}))", primaryKey, itemSql);

    /// <summary>
    /// Child-table empty/null ConditionalType 11/14: IN match OR NOT IN all rows of child table.
    /// </summary>
    public static string BuildChildTableEmptyOrMatch(
        string primaryKey,
        string itemSql,
        string childIdField,
        string childTableName)
        => string.Format(
            " ({0} in ({1}) OR {0} NOT IN ( SELECT {2} FROM {3} ))",
            primaryKey,
            itemSql,
            childIdField,
            childTableName);

    /// <summary>
    /// Soft-delete filter as IN subquery against main table.
    /// </summary>
    public static string BuildSoftDeleteInSubquery(string primaryKey, string mainTableName)
        => string.Format(
            " ( {0} in ({1}) ) ",
            primaryKey,
            string.Format(" select {0} from {1} where f_delete_mark is null ", primaryKey, mainTableName));

    /// <summary>
    /// Tenant isolation filter as IN subquery (caller supplies IsolationField value).
    /// </summary>
    public static string BuildTenantIsolationInSubquery(
        string primaryKey,
        string mainTableName,
        string isolationField)
        => string.Format(
            " ( {0} in ({1}) ) ",
            primaryKey,
            string.Format(" select {0} from {1} where f_tenant_id='{2}'", primaryKey, mainTableName, isolationField));

    /// <summary>
    /// Fallback when no query filters: all primary keys from main table.
    /// </summary>
    public static string BuildUnfilteredPrimaryInSubquery(string primaryKey, string mainTableName)
        => string.Format(
            " ( {0} in ({1}) ) ",
            primaryKey,
            string.Format(SelectFromTemplate, primaryKey, mainTableName));

    /// <summary>
    /// Outer wrap: select * from (inner) mt where filters…
    /// </summary>
    public static string WrapOuterListQuery(
        string innerSql,
        IEnumerable<string> querySqlList,
        string? superQuerySqlCondition,
        string? dataRuleSqlCondition,
        string? dataPermissionsSqlCondition)
        => string.Format(
            "select * from ({0}) mt where {1} {2} {3} {4}",
            innerSql,
            string.Join(" and ", querySqlList ?? Array.Empty<string>()),
            superQuerySqlCondition ?? string.Empty,
            dataRuleSqlCondition ?? string.Empty,
            dataPermissionsSqlCondition ?? string.Empty);

    /// <summary>
    /// Merge a new WHERE fragment into an existing IN-subquery SQL that shares the same prefix.
    /// Legacy shape: oldSql split by WHERE → prefix + newWhere + and + oldTail.
    /// </summary>
    public static string MergeWhereIntoExistingInSubquery(string oldSql, string itemWhere)
    {
        var oldParts = oldSql.Split("WHERE");
        var whereParts = itemWhere.Split("WHERE");
        return string.Format(
            "{0}WHERE{1}and{2}",
            oldParts.FirstOrDefault(),
            whereParts.LastOrDefault(),
            oldParts.LastOrDefault());
    }

    /// <summary>
    /// Append WHERE clause into itemSql that already ends with …WHERE).
    /// </summary>
    public static string InjectWhereIntoExistingSubquery(string itemSqlWithWhereTail, string itemWhere)
    {
        var itemParts = itemSqlWithWhereTail.Split("WHERE");
        var whereParts = itemWhere.Split("WHERE");
        return string.Format(
            "{0}WHERE{1}{2}",
            itemParts.FirstOrDefault(),
            whereParts.LastOrDefault(),
            itemParts.LastOrDefault());
    }

    /// <summary>
    /// Whether JSON marks ConditionalType empty/null (11 or 14) — used with child-table empty match.
    /// </summary>
    public static bool IsEmptyOrNullConditionalTypeJson(string? conditionalJson)
        => !string.IsNullOrEmpty(conditionalJson)
           && (conditionalJson.Contains("\"ConditionalType\":11")
               || conditionalJson.Contains("\"ConditionalType\":14"));

    /// <summary>
    /// Rewrite data-permission JSON FieldName for single main-table FIELD_i aliases.
    /// </summary>
    public static string RewriteMainTablePermissionFieldNames(
        string pvalueJson,
        IEnumerable<KeyValuePair<string, string>> tableFieldKeyValue,
        string mainTableName)
    {
        if (string.IsNullOrEmpty(pvalueJson) || tableFieldKeyValue == null)
            return pvalueJson;

        foreach (var item in tableFieldKeyValue)
        {
            pvalueJson = pvalueJson.Replace(
                string.Format("\"FieldName\":\"{0}\",", mainTableName + "." + item.Value),
                string.Format("\"FieldName\":\"{0}\",", item.Key));
        }

        return pvalueJson;
    }

    /// <summary>
    /// Rewrite data-permission JSON FieldName for joined tables (AllTableFields map + strip main prefix).
    /// </summary>
    public static string RewriteJoinedPermissionFieldNames(
        string pvalueJson,
        IEnumerable<KeyValuePair<string, string>> tableFieldKeyValue,
        IReadOnlyDictionary<string, string>? allTableFields,
        string mainTableName)
    {
        if (string.IsNullOrEmpty(pvalueJson) || tableFieldKeyValue == null)
            return pvalueJson;

        foreach (var item in tableFieldKeyValue)
        {
            string? newValue = item.Value;
            if (allTableFields != null && allTableFields.ContainsKey(item.Value))
                newValue = allTableFields[item.Value];

            if (pvalueJson.Contains(newValue))
            {
                pvalueJson = pvalueJson.Replace(
                    string.Format("\"FieldName\":\"{0}\",", newValue),
                    string.Format("\"FieldName\":\"{0}\",", item.Key));
            }
            else
            {
                if (newValue.Contains(mainTableName))
                    newValue = newValue.Replace(mainTableName + ".", string.Empty);
                if (pvalueJson.Contains(newValue))
                {
                    pvalueJson = pvalueJson.Replace(
                        string.Format("\"FieldName\":\"{0}\",", newValue),
                        string.Format("\"FieldName\":\"{0}\",", item.Key));
                }
            }
        }

        return pvalueJson;
    }
}
