using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Transfer;

/// <summary>
/// In-memory form-data mapping for SaveDataToDataByFId (child/main transfer + result assemble).
/// DB / HTTP persistence stays at the call site.
/// </summary>
public static class FlowFormDataMapper
{
    public const string DefaultChildTableSplitKey = "tablefield";
    public const string SpecialFormChildTableSplitKey = "-";

    private static readonly HashSet<string> SpecialFormEnCodes = new(StringComparer.Ordinal)
    {
        "leaveApply", "salesOrder", "crmOrder",
    };

    /// <summary>
    /// leaveApply / salesOrder / crmOrder use "-" as child-table marker; others use "tablefield".
    /// </summary>
    public static string ResolveChildTableSplitKey(params string?[] formEnCodes)
    {
        foreach (var code in formEnCodes)
        {
            if (code != null && SpecialFormEnCodes.Contains(code))
                return SpecialFormChildTableSplitKey;
        }

        return DefaultChildTableSplitKey;
    }

    /// <summary>
    /// FormType=1: coerce non-modify/table keys to COMINPUT (legacy system-form path).
    /// </summary>
    public static void CoerceSystemFormFieldsToComInput(IEnumerable<FieldsModel> fields)
    {
        if (fields == null)
            return;
        foreach (var it in fields)
        {
            if (!it.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME)
                && !it.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER)
                && !it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                it.__config__.jnpfKey = JnpfKeyConst.COMINPUT;
        }
    }

    /// <summary>
    /// Apply mapRule transfers onto <paramref name="formData"/> (mutates in place).
    /// </summary>
    // TODO: CC37 超标，基线锁定于 Task 3.4（maxComplexity=37，只许下降），待拆分重构（Tech-Debt: CC31-Append-Refactor 同批，归因 456e2d6b）
    public static void ApplyMapRules(
        Dictionary<string, object> formData,
        List<Dictionary<string, string>> mapRule,
        IReadOnlyDictionary<string, FieldsModel> oldFieldsByVModel,
        IReadOnlyDictionary<string, FieldsModel> newFieldsByVModel,
        string childTableSplitKey)
    {
        foreach (var items in mapRule)
        {
            var item = items.First();
            oldFieldsByVModel.TryGetValue(item.Key, out var oldModel);
            newFieldsByVModel.TryGetValue(item.Value, out var newModel);
            if (oldModel == null || newModel == null
                || oldModel.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME)
                || oldModel.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER))
            {
                formData[item.Key] = string.Empty;
                continue;
            }
            if (oldModel.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) || newModel.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                continue;

            if (oldModel.__vModel__.ToLower().Contains(childTableSplitKey) && newModel.__vModel__.ToLower().Contains(childTableSplitKey))
            {
                if (FlowFormDataTransferRules.CanTransfer(oldModel, newModel))
                {
                    var oldCTable = oldModel.__vModel__.Split("-").First();
                    var oldCField = oldModel.__vModel__.Split("-").Last();
                    var newCTable = newModel.__vModel__.Split("-").First();
                    var newCField = newModel.__vModel__.Split("-").Last();
                    if (oldCField.IsNullOrWhiteSpace() || newCField.IsNullOrWhiteSpace()) continue;

                    if (!formData.ContainsKey(newCTable)) formData.Add(newCTable, new List<Dictionary<string, object>>());
                    if (formData.ContainsKey(oldCTable) && formData[oldCTable] != null && formData[oldCTable].ToString() != "[]")
                    {
                        var oldCTData = formData[oldCTable].ToObject<List<Dictionary<string, object>>>();
                        var newCTData = formData.ContainsKey(newCTable)
                            ? formData[newCTable].ToObject<List<Dictionary<string, object>>>()
                            : new List<Dictionary<string, object>>();

                        for (var i = 0; i < oldCTData.Count; i++)
                        {
                            if (oldCTData[i].ContainsKey(oldCField))
                            {
                                if (newCTData.Count > i) newCTData[i][newCField] = oldCTData[i][oldCField];
                                else newCTData.Add(new Dictionary<string, object>() { { newCField, oldCTData[i][oldCField] } });
                            }
                        }
                        formData[newCTable] = newCTData;
                    }
                }
            }
            else if (oldModel.__vModel__.ToLower().Contains(childTableSplitKey) || newModel.__vModel__.ToLower().Contains(childTableSplitKey))
            {
                if (FlowFormDataTransferRules.CanTransfer(oldModel, newModel))
                {
                    if (oldModel.__vModel__.ToLower().Contains(childTableSplitKey) && !newModel.__vModel__.ToLower().Contains(childTableSplitKey))
                    {
                        var childTable = oldModel.__vModel__.Split("-").First();
                        var childField = oldModel.__vModel__.Split("-").Last();
                        var childTableData = formData[childTable].ToObject<List<Dictionary<string, object>>>();
                        if (childTableData.Any() && childTableData.Any(x => x.ContainsKey(childField)))
                        {
                            if (formData.ContainsKey(oldModel.__vModel__)) formData[oldModel.__vModel__] = childTableData.First()[childField];
                            else formData.Add(oldModel.__vModel__, childTableData.First()[childField]);
                        }
                    }

                    if (!oldModel.__vModel__.ToLower().Contains(childTableSplitKey) && newModel.__vModel__.ToLower().Contains(childTableSplitKey))
                    {
                        var childKey = newModel.__vModel__.Split("-");
                        var childTableKey = childKey.First();
                        var childFieldKey = childKey.Last();
                        if (formData.ContainsKey(oldModel.__vModel__))
                        {
                            var childFieldValue = formData[oldModel.__vModel__];

                            if (!formData.ContainsKey(childTableKey)) formData.Add(childTableKey, new List<Dictionary<string, object>>());

                            var childItems = formData[childTableKey].ToObject<List<Dictionary<string, object>>>();
                            if (!childItems.Any())
                            {
                                childItems.Add(new Dictionary<string, object>() { { childFieldKey, childFieldValue } });
                            }
                            else
                            {
                                if (childItems.Any(x => x.ContainsKey(childFieldKey))) childItems.First()[childFieldKey] = childFieldValue;
                                else childItems.First().Add(childFieldKey, childFieldValue);
                            }

                            formData[childTableKey] = childItems;
                        }
                    }
                }
            }
            else
            {
                if (!childTableSplitKey.Equals("-") && !FlowFormDataTransferRules.CanTransfer(oldModel, newModel))
                    formData[oldModel.__vModel__] = null;
            }
        }
    }

    /// <summary>
    /// Project formData onto target keys from mapRule (plus child-table buckets).
    /// </summary>
    public static Dictionary<string, object> BuildResult(
        Dictionary<string, object> formData,
        List<Dictionary<string, string>> mapRule,
        string childTableSplitKey)
    {
        var res = new Dictionary<string, object>();
        foreach (var dicItems in mapRule)
        {
            var dicItem = dicItems.First();
            if (formData.ContainsKey(dicItem.Key) && dicItem.Value.IsNotEmptyOrNull())
            {
                var itemValue = formData.First(x => x.Key.Equals(dicItem.Key)).Value;
                if (!res.ContainsKey(dicItem.Value)) res.Add(dicItem.Value, itemValue);
            }
            if (dicItem.Value.ToLower().Contains(childTableSplitKey))
            {
                var cTableKey = dicItem.Value.Split("-").First();
                var itemValue = formData.First(x => x.Key.Equals(cTableKey));
                if (!res.ContainsKey(cTableKey)) res.Add(itemValue.Key, itemValue.Value);
            }
        }

        return res;
    }

    /// <summary>
    /// Inject previous-node form id into mapped target (and child-table cells when key contains tablefield).
    /// </summary>
    public static void ApplyPrevNodeFormId(
        Dictionary<string, object> res,
        Dictionary<string, object> formData,
        List<Dictionary<string, string>> mapRule)
    {
        if (!mapRule.Any(x => x.ContainsKey("@prevNodeFormId")))
            return;

        var key = mapRule.Find(x => x.ContainsKey("@prevNodeFormId")).First().Value;
        if (key.ToLower().Contains("tablefield"))
        {
            var ctKey = key.Split('-');
            var ctValues = res[ctKey.FirstOrDefault()].ToObject<List<Dictionary<string, object>>>();
            ctValues.ForEach(item => item[ctKey.LastOrDefault()] = formData["id"]);
            res[ctKey.FirstOrDefault()] = ctValues;
        }
        res[mapRule.Find(x => x.ContainsKey("@prevNodeFormId")).First().Value] = formData["id"];
    }
}
