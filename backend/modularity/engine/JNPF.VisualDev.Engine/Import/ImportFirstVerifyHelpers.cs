using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// In-memory ImportFirstVerify steps shared by VisualDev + CodeGen.
/// DB unique lookup stays at call sites (field-name / WHERE join forks).
/// </summary>
/// <remarks>
/// Uses shallow dictionary clones instead of <c>T.Copy()</c> (System.Text.Json):
/// STJ rehydrates Dictionary&lt;string,object&gt; values as JsonElement, which breaks
/// Newtonsoft <c>ToObject&lt;List&lt;...&gt;&gt;</c> on child tables. Shallow clone keeps
/// Excel/import value types; required/unique only mutate errorsInfo or replace child lists.
/// </remarks>
public static class ImportFirstVerifyHelpers
{
    private static Dictionary<string, object> ShallowClone(Dictionary<string, object> item)
        => new(item);

    public static List<Dictionary<string, object>> SeedWithEmptyErrors(
        List<Dictionary<string, object>> list)
    {
        var resList = new List<Dictionary<string, object>>();
        foreach (var item in list)
        {
            var addItem = ShallowClone(item);
            addItem[ImportAssembleErrors.ErrorKey] = string.Empty;
            resList.Add(addItem);
        }

        return resList;
    }

    public static List<string> CollectChildTableVModels(IEnumerable<FieldsModel> allFieldsModel)
        => allFieldsModel
            .Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
            .Select(x => x.__vModel__)
            .ToList();

    /// <summary>
    /// Required-field check (main + child). Mutates rows; preserves leading-comma quirk after Seed.
    /// </summary>
    public static void ValidateRequired(
        List<Dictionary<string, object>> resList,
        List<FieldsModel> allFieldsModel,
        List<string> childTableVModels)
    {
        var requiredList = allFieldsModel.Where(x => x.__config__.required).ToList();
        var vModelList = requiredList.Select(x => x.__vModel__).ToList();
        if (!vModelList.Any())
            return;

        var newResList = new List<Dictionary<string, object>>();
        foreach (var items in resList)
        {
            var newItems = ShallowClone(items);
            foreach (var item in items)
            {
                if (item.Value.IsNullOrEmpty() && vModelList.Contains(item.Key))
                {
                    var errorInfo = requiredList.Find(x => x.__vModel__.Equals(item.Key)).__config__.label + ": 值不能为空";
                    ImportAssembleErrors.Append(newItems, errorInfo);
                }

                if (childTableVModels.Contains(item.Key))
                {
                    foreach (var childItems in item.Value.ToObject<List<Dictionary<string, object>>>())
                    {
                        foreach (var childItem in childItems)
                        {
                            if (childItem.Value.IsNullOrEmpty() && vModelList.Contains(item.Key + "-" + childItem.Key))
                            {
                                var errorInfo = allFieldsModel.Find(x => x.__vModel__.Equals(item.Key)).__config__.children
                                    .Find(x => x.__vModel__.Equals(item.Key + "-" + childItem.Key)).__config__.label + ": 值不能为空";
                                ImportAssembleErrors.Append(newItems, errorInfo);
                            }
                        }
                    }
                }
            }

            newResList.Add(newItems);
        }

        resList.Clear();
        resList.AddRange(newResList);
    }

    /// <summary>
    /// Batch unique check (main + child). dataType "2" keeps last child row per unique key (legacy).
    /// </summary>
    // TODO: CC35 超标，基线锁定于 Task 3.4（maxComplexity=35，只许下降），待拆分重构（Tech-Debt: CC31-Append-Refactor 同批，归因 456e2d6b）
    public static void ValidateBatchUnique(
        List<Dictionary<string, object>> resList,
        List<FieldsModel> allFieldsModel,
        List<string> childTableVModels,
        string? dataType)
    {
        var uniqueList = allFieldsModel.Where(x => x.__config__.unique).ToList();
        var vModelList = uniqueList.Select(x => x.__vModel__).ToList();
        if (!uniqueList.Any())
            return;

        foreach (var items in resList)
        {
            foreach (var item in items.ToList())
            {
                if (vModelList.Contains(item.Key))
                {
                    var vlist = new List<Dictionary<string, object>>();
                    foreach (var it in resList.Where(x => x.ContainsKey(item.Key) && x.ContainsValue(item.Value)))
                    {
                        foreach (var dic in it)
                        {
                            if (dic.Value != null && item.Value != null && dic.Key.Equals(item.Key) && dic.Value.Equals(item.Value))
                            {
                                vlist.Add(it);
                                break;
                            }
                        }
                    }

                    if (vlist.Count > 1)
                    {
                        for (var i = 1; i < vlist.Count; i++)
                        {
                            var errorInfo = allFieldsModel.Find(x => x.__vModel__.Equals(item.Key)).__config__.label + ": 值不能重复";
                            items[ImportAssembleErrors.ErrorKey] = items[ImportAssembleErrors.ErrorKey] + "," + errorInfo;
                        }
                    }
                }

                var updateItemCList = new List<Dictionary<string, object>>();
                var ctItemErrors = new List<string>();
                if (childTableVModels.Contains(item.Key))
                {
                    var itemCList = item.Value.ToObject<List<Dictionary<string, object>>>();
                    foreach (var childItems in itemCList)
                    {
                        if (dataType != null && dataType.Equals("2"))
                        {
                            foreach (var childItem in childItems)
                            {
                                var uniqueKey = item.Key + "-" + childItem.Key;
                                if (vModelList.Contains(uniqueKey))
                                {
                                    var vlist = itemCList.Where(x => x.ContainsKey(childItem.Key) && x.ContainsValue(childItem.Value)).ToList();
                                    if (!updateItemCList.Any(x => x.ContainsKey(childItem.Key) && x.ContainsValue(childItem.Value)))
                                        updateItemCList.Add(vlist.Last());
                                }
                            }
                        }
                        else
                        {
                            foreach (var childItem in childItems)
                            {
                                var uniqueKey = item.Key + "-" + childItem.Key;
                                if (vModelList.Contains(uniqueKey) && childItem.Value != null)
                                {
                                    var vlist = itemCList.Where(x => x.ContainsKey(childItem.Key) && x.ContainsValue(childItem.Value)).ToList();
                                    if (vlist.Count > 1)
                                    {
                                        for (var i = 1; i < vlist.Count; i++)
                                        {
                                            var errorTxt = allFieldsModel.Find(x => x.__vModel__.Equals(uniqueKey)).__config__.label + ": 值不能重复";
                                            if (!ctItemErrors.Any(x => x.Equals(errorTxt)))
                                                ctItemErrors.Add(errorTxt);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (dataType != null && dataType.Equals("2") && updateItemCList.Any())
                    items[item.Key] = updateItemCList;
                if (ctItemErrors.Any())
                {
                    items[ImportAssembleErrors.ErrorKey] = items[ImportAssembleErrors.ErrorKey].IsNullOrEmpty()
                        ? string.Join(",", ctItemErrors)
                        : items[ImportAssembleErrors.ErrorKey] + "," + string.Join(",", ctItemErrors);
                }
            }
        }
    }
}
