using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.Extensions;
using JNPF.VisualDev.Runtime;
using SqlSugar;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Pure helpers for RunService.GetListChildTable (column map / FK SQL / row attach).
/// I/O (SqlSugar Where/ToSqlString, DB query, GetKeyData) stays in RunService.
/// </summary>
public static class ListChildTableHelpers
{
    /// <summary>
    /// Collect parent primary-key values from the current page list (legacy ForEach order).
    /// </summary>
    public static List<object> CollectParentIds(
        IEnumerable<Dictionary<string, object>> list,
        string primaryKey)
    {
        var ids = new List<object>();
        foreach (var item in list)
            ids.Add(item[primaryKey]);
        return ids;
    }

    /// <summary>
    /// TABLE controls → physical column names per child table (vModel suffix after last '-').
    /// </summary>
    public static Dictionary<string, List<string>> BuildChildTableSelectColumns(
        IEnumerable<FieldsModel> allFieldsModel)
    {
        var childTableList = new Dictionary<string, List<string>>();
        var fields = allFieldsModel?.ToList() ?? new List<FieldsModel>();
        foreach (var ctitem in fields.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
        {
            var prefix = ctitem.__vModel__ + "-";
            foreach (var item in fields.Where(x => x.__vModel__.Contains(prefix)))
            {
                var value = item.__vModel__.Split("-").Last();
                if (value.IsNotEmptyOrNull())
                {
                    var tableName = ctitem.__config__.tableName;
                    if (childTableList.ContainsKey(tableName)) childTableList[tableName].Add(value);
                    else childTableList.Add(tableName, new List<string> { value });
                }
            }
        }

        return childTableList;
    }

    /// <summary>
    /// First-seen FK column per child table from ChildTableFieldsModelList × AllTable.
    /// </summary>
    public static Dictionary<string, string> BuildRelationFields(
        IEnumerable<FieldsModel> childTableFieldsModelList,
        IEnumerable<TableModel> allTable)
    {
        var relationField = new Dictionary<string, string>();
        var tables = allTable?.ToList() ?? new List<TableModel>();
        foreach (var item in childTableFieldsModelList ?? Enumerable.Empty<FieldsModel>())
        {
            var tableName = item.__config__.tableName;
            var tableField = tables.Find(tf => tf.table == tableName)?.tableField;
            if (!relationField.ContainsKey(tableName))
                relationField.Add(tableName, tableField);
        }

        return relationField;
    }

    /// <summary>
    /// Replace quoted logical keys with physical values in serialized rule JSON (legacy Contains+Replace).
    /// </summary>
    public static string RewriteQuotedMapKeys(
        string json,
        IReadOnlyDictionary<string, string> allTableFields)
    {
        if (json.IsNullOrEmpty() || allTableFields == null || allTableFields.Count == 0)
            return json;

        var dataRuleJson = json;
        foreach (var item in allTableFields)
        {
            var from = string.Format("\"{0}\"", item.Key);
            if (dataRuleJson.IsNotEmptyOrNull() && dataRuleJson.Contains(from))
                dataRuleJson = dataRuleJson.Replace(from, string.Format("\"{0}\"", item.Value));
        }

        return dataRuleJson;
    }

    /// <summary>
    /// Map logical child field names → physical "table.col" on ConditionalCollections (mutates).
    /// </summary>
    public static void RewriteChildFieldNames(
        IEnumerable<ConditionalCollections> collections,
        IReadOnlyDictionary<string, string> childTableFields)
    {
        if (collections == null || childTableFields == null || childTableFields.Count == 0)
            return;

        foreach (var item in collections)
        {
            if (item?.ConditionalList == null) continue;
            foreach (var sitem in item.ConditionalList)
            {
                if (sitem.Value != null && childTableFields.ContainsKey(sitem.Value.FieldName))
                    sitem.Value.FieldName = childTableFields[sitem.Value.FieldName];
            }
        }
    }

    /// <summary>
    /// 平台条件模型重载（裁决 C：编译层边界平台类型；逻辑与 SqlSugar 版逐句一致）.
    /// </summary>
    public static void RewriteChildFieldNames(
        IEnumerable<CompileConditionalCollections> collections,
        IReadOnlyDictionary<string, string> childTableFields)
    {
        if (collections == null || childTableFields == null || childTableFields.Count == 0)
            return;

        foreach (var item in collections)
        {
            if (item?.ConditionalList == null) continue;
            foreach (var sitem in item.ConditionalList)
            {
                if (sitem.Value != null && childTableFields.ContainsKey(sitem.Value.FieldName))
                    sitem.Value.FieldName = childTableFields[sitem.Value.FieldName];
            }
        }
    }

    /// <summary>
    /// Ensure FK column is selected, then build legacy IN-list SQL (ids joined with ',').
    /// Mutates <paramref name="selectColumns"/> by appending the relation field.
    /// </summary>
    public static string BuildChildTableInSql(
        List<string> selectColumns,
        string tableName,
        string relationField,
        IEnumerable<object> ids)
    {
        selectColumns.Add(relationField);
        return string.Format(
            "select {0} from {1} where {2} in('{3}')",
            string.Join(",", selectColumns),
            tableName,
            relationField,
            string.Join("','", ids));
    }

    /// <summary>
    /// Append SqlSugar Where fragment when ToSqlString contains WHERE (legacy Split Last).
    /// </summary>
    public static string AppendAndWhereFragment(string sql, string? itemWhere)
    {
        if (itemWhere.IsNullOrEmpty() || !itemWhere.Contains("WHERE"))
            return sql;
        return string.Format(" {0} and {1} ", sql, itemWhere.Split("WHERE").Last());
    }

    /// <summary>
    /// Keep serialized condition objects whose JSON contains "{tableName}.".
    /// </summary>
    public static List<object> FilterObjectsContainingTablePrefix(
        IEnumerable<object> all,
        string tableName)
    {
        var needle = tableName + ".";
        var list = new List<object>();
        foreach (var it in all ?? Enumerable.Empty<object>())
        {
            if (it.ToJsonString().Contains(needle))
                list.Add(it);
        }

        return list;
    }

    /// <summary>
    /// Match child rows whose FK equals parent primary key (string Equals, legacy).
    /// </summary>
    public static List<Dictionary<string, object>> MatchRowsByRelation(
        IEnumerable<Dictionary<string, object>> childRows,
        string relationField,
        object parentPrimaryValue)
    {
        var parentKey = parentPrimaryValue?.ToString();
        return childRows
            .Where(x => x[relationField].ToString().Equals(parentKey))
            .ToList();
    }

    /// <summary>
    /// Drop MainData marker + FK column from converted child rows (Copy first).
    /// </summary>
    public static List<Dictionary<string, object>> StripChildRowMeta(
        IEnumerable<Dictionary<string, object>> datas,
        string relationField)
    {
        var newDatas = new List<Dictionary<string, object>>();
        foreach (var data in datas)
        {
            var newData = data.Copy();
            newData.Remove("JnpfKeyConst_MainData");
            newData.Remove(relationField);
            newDatas.Add(newData);
        }

        return newDatas;
    }
}
