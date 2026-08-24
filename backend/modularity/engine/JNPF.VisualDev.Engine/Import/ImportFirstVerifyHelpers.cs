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
    /// D1 拆分（战役 D1 · 规格 §2.4）：纯结构重构，行为不变量 I1-I6 由
    /// ImportFirstVerifyHelpersTests 13 用例全分支锁定（含 Q8 N-1 计数 / Q9 粗筛保真，注释以 I 编号标注）。
    /// </summary>
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

        var isKeepLastMode = dataType != null && dataType.Equals("2");

        foreach (var items in resList)
        {
            foreach (var item in items.ToList())
            {
                if (vModelList.Contains(item.Key))
                    CheckMainFieldUnique(items, item, resList, allFieldsModel);

                var updateItemCList = new List<Dictionary<string, object>>();
                var ctItemErrors = new List<string>();
                if (childTableVModels.Contains(item.Key))
                {
                    var itemCList = item.Value.ToObject<List<Dictionary<string, object>>>();
                    if (isKeepLastMode)
                        DedupChildKeepLast(itemCList, item.Key, vModelList, updateItemCList);
                    else
                        CollectChildUniqueErrors(itemCList, item.Key, vModelList, allFieldsModel, ctItemErrors);
                }

                if (isKeepLastMode && updateItemCList.Any())
                    items[item.Key] = updateItemCList;
                if (ctItemErrors.Any())
                    AppendErrors(items, ctItemErrors);
            }
        }
    }

    /// <summary>
    /// I1+I5+I6 主字段唯一：候选粗筛（ContainsKey+ContainsValue，Q9 保真）+ 内层键值相等精确入集；
    /// 重复集 N>1 时追加精确 N-1 条同文错误（Q8 for i=1 循环，保真）；双空值守卫.
    /// </summary>
    private static void CheckMainFieldUnique(
        Dictionary<string, object> items,
        KeyValuePair<string, object> item,
        List<Dictionary<string, object>> resList,
        List<FieldsModel> allFieldsModel)
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

    /// <summary>
    /// I3 dataType "2" 模式：子表按唯一键保留最后一行（vlist.Last()），收集入 updateItemCList 供门面整体替换.
    /// </summary>
    private static void DedupChildKeepLast(
        List<Dictionary<string, object>> itemCList,
        string tableKey,
        List<string> vModelList,
        List<Dictionary<string, object>> updateItemCList)
    {
        foreach (var childItems in itemCList)
        {
            foreach (var childItem in childItems)
            {
                var uniqueKey = tableKey + "-" + childItem.Key;
                if (vModelList.Contains(uniqueKey))
                {
                    var vlist = itemCList.Where(x => x.ContainsKey(childItem.Key) && x.ContainsValue(childItem.Value)).ToList();
                    if (!updateItemCList.Any(x => x.ContainsKey(childItem.Key) && x.ContainsValue(childItem.Value)))
                        updateItemCList.Add(vlist.Last());
                }
            }
        }
    }

    /// <summary>
    /// I4+I6 错误模式：子表唯一键命中且重复集 >1 时按文案去重收集（收尾由门面一次性串接）；null 子值跳过.
    /// </summary>
    private static void CollectChildUniqueErrors(
        List<Dictionary<string, object>> itemCList,
        string tableKey,
        List<string> vModelList,
        List<FieldsModel> allFieldsModel,
        List<string> ctItemErrors)
    {
        foreach (var childItems in itemCList)
        {
            foreach (var childItem in childItems)
            {
                var uniqueKey = tableKey + "-" + childItem.Key;
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

    /// <summary>
    /// I2 错误串接：行已有错误以逗号追加（保持 Seed 后的前导逗号形态）；空则直连.
    /// </summary>
    private static void AppendErrors(Dictionary<string, object> items, List<string> errors)
    {
        items[ImportAssembleErrors.ErrorKey] = items[ImportAssembleErrors.ErrorKey].IsNullOrEmpty()
            ? string.Join(",", errors)
            : items[ImportAssembleErrors.ErrorKey] + "," + string.Join(",", errors);
    }
}
