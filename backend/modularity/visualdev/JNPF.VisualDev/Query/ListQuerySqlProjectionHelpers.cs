using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Pure FIELD_i projection + query-input remaps for RunService.GetListQuerySql
/// (main-table and joined-table branches).
/// </summary>
public static class ListQuerySqlProjectionHelpers
{
    /// <summary>
    /// Remap PC/App searchList entry ids to table.column aliases when query hits that key.
    /// </summary>
    public static void RemapSearchListFieldAliases(
        IndexSearchFieldModel? pcSearch,
        IndexSearchFieldModel? appSearch,
        string alias,
        List<string> fieldsSink)
    {
        if (pcSearch != null)
        {
            pcSearch.__vModel__ = alias;
            pcSearch.prop = alias;
            pcSearch.id = alias;
            fieldsSink.Add(alias);
        }

        if (appSearch != null)
        {
            appSearch.__vModel__ = alias;
            appSearch.prop = alias;
            appSearch.id = alias;
            fieldsSink.Add(alias);
        }
    }

    /// <summary>
    /// Seed system columns shared by main / join projections.
    /// </summary>
    public static void SeedSystemProjectionFields(
        List<string> fields,
        Dictionary<string, string> tableFieldKeyValue,
        string primaryKey,
        bool includeFlowId,
        string? mainTablePrefix)
    {
        var pkSelect = string.IsNullOrEmpty(mainTablePrefix)
            ? primaryKey
            : mainTablePrefix + "." + primaryKey;
        var inteSelect = string.IsNullOrEmpty(mainTablePrefix)
            ? "f_inte_assistant"
            : mainTablePrefix + ".f_inte_assistant";
        var flowSelect = string.IsNullOrEmpty(mainTablePrefix)
            ? "f_flow_id"
            : mainTablePrefix + ".f_flow_id";

        fields.Add(pkSelect);
        fields.Add(inteSelect);
        if (includeFlowId) fields.Add(flowSelect);

        tableFieldKeyValue.Add(primaryKey.ToUpper(), primaryKey);
        tableFieldKeyValue.Add("f_flow_id".ToUpper(), "f_flow_id");
        tableFieldKeyValue.Add("f_inte_assistant".ToUpper(), "f_inte_assistant");
    }

    /// <summary>
    /// Apply FIELD_i alias into queryJson / superQueryJson / dataRuleJson / sidx / searchList.
    /// Main-table branch only remaps search __vModel__; joined branch also remaps search id.
    /// </summary>
    public static void RemapQueryInputsToFieldAlias(
        VisualDevModelListQueryInput input,
        Dictionary<string, object>? inputJson,
        List<IndexSearchFieldModel>? searchList,
        string originalVModel,
        string fieldAlias,
        bool remapSearchId)
    {
        if (inputJson != null && inputJson.Count > 0 && inputJson.ContainsKey(originalVModel)
            && input.queryJson.IsNotEmptyOrNull())
        {
            input.queryJson = input.queryJson.Replace(
                "\"" + originalVModel + "\":",
                "\"" + fieldAlias + "\":");
        }

        if (input.superQueryJson.IsNotEmptyOrNull())
        {
            input.superQueryJson = input.superQueryJson.Replace(
                string.Format("\"field\":\"{0}\"", originalVModel),
                string.Format("\"field\":\"{0}\"", fieldAlias));
        }

        if (input.dataRuleJson.IsNotEmptyOrNull())
        {
            input.dataRuleJson = input.dataRuleJson.Replace(
                string.Format("\"FieldName\":\"{0}\"", originalVModel),
                string.Format("\"FieldName\":\"{0}\"", fieldAlias));
        }

        if (searchList != null)
        {
            foreach (var item in searchList.Where(x => x.__vModel__ == originalVModel))
            {
                if (remapSearchId && item.id != null)
                    item.id = item.id.Replace(originalVModel, fieldAlias);
                if (item.__vModel__ != null)
                    item.__vModel__ = item.__vModel__.Replace(originalVModel, fieldAlias);
            }
        }

        if (input.sidx.IsNotEmptyOrNull())
            input.sidx = input.sidx.Replace(originalVModel, fieldAlias);
    }

    /// <summary>
    /// Build join ON clauses: main.relationField = aux.tableField.
    /// </summary>
    public static List<string> BuildAuxiliaryJoinPredicates(
        IEnumerable<string> auxiliaryTableNames,
        IEnumerable<TableModel> allTable,
        string mainTableName)
    {
        var relationKey = new List<string>();
        var tables = allTable as IList<TableModel> ?? allTable.ToList();
        foreach (var tName in auxiliaryTableNames)
        {
            var tableField = tables.First(tf => tf.table == tName);
            relationKey.Add(mainTableName + "." + tableField.relationField + "=" + tName + "." + tableField.tableField);
        }

        return relationKey;
    }
}
