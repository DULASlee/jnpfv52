using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Import;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Pure helpers for RunService.GenerateFeilds (save-time system field generation).
/// Bill/position/organize I/O stays at the call site.
/// </summary>
public static class SystemFieldGenerateHelpers
{
    public const string FlowDelegateCurrPosition = "Jnpf_FlowDelegate_CurrPosition";
    public const string FlowDelegateCurrOrganize = "Jnpf_FlowDelegate_CurrOrganize";

    /// <summary>
    /// systemControlList present → treat CURR* as create semantics (legacy <c>create</c> flag).
    /// </summary>
    public static bool ForceCreateSemantics(IEnumerable<string>? systemControlList)
        => systemControlList.IsNotEmptyOrNull();

    /// <summary>
    /// On update: strip create-user/time; strip curr pos/org unless systemControlList is set;
    /// strip matching child table system keys.
    /// </summary>
    public static void StripSystemFieldsOnUpdate(
        List<FieldsModel> fieldsModelList,
        Dictionary<string, object> allDataMap,
        List<string>? systemControlList)
    {
        fieldsModelList.ForEach(item =>
        {
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.CREATETIME:
                case JnpfKeyConst.CREATEUSER:
                    allDataMap.Remove(item.__vModel__);
                    break;
                case JnpfKeyConst.CURRPOSITION:
                case JnpfKeyConst.CURRORGANIZE:
                    if (systemControlList == null) allDataMap.Remove(item.__vModel__);
                    break;
                case JnpfKeyConst.TABLE:
                    var fList = item.__config__.children.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.CREATETIME)
                        || x.__config__.jnpfKey.Equals(JnpfKeyConst.CREATEUSER)
                        || x.__config__.jnpfKey.Equals(JnpfKeyConst.CURRPOSITION)
                        || x.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE)).ToList();
                    fList.ForEach(child =>
                    {
                        if (allDataMap.ContainsKey(item.__vModel__))
                        {
                            var cDataMap = allDataMap[item.__vModel__].ToObject<List<Dictionary<string, object>>>();
                            cDataMap.ForEach(x => x.Remove(child.__vModel__));
                            allDataMap[item.__vModel__] = cDataMap;
                        }
                    });
                    break;
            }
        });
    }

    public static void ApplyBillNumber(Dictionary<string, object> target, string key, string billNumber)
        => ImportSystemFieldAssembler.MapBillRule(key, billNumber, target);

    /// <summary>
    /// Child BILLRULE regenerate when create, or row has no id, or value empty.
    /// </summary>
    public static bool ShouldGenerateChildBillRule(bool isCreate, IDictionary<string, object> childRow, string fieldKey)
        => isCreate
           || (!isCreate && !childRow.ContainsKey("id"))
           || childRow[fieldKey].IsNullOrEmpty();

    public static string FormatTimestamp(DateTime now)
        => string.Format("{0:yyyy-MM-dd HH:mm:ss}", now);

    public static void ApplyCreateUser(IDictionary<string, object> map, string key, string userId, bool isCreate)
    {
        if (isCreate) map[key] = userId;
    }

    public static void ApplyModifyUser(IDictionary<string, object> map, string key, string userId, bool isCreate)
    {
        if (!isCreate) map[key] = userId;
    }

    public static void ApplyCreateTime(IDictionary<string, object> map, string key, DateTime now, bool isCreate)
    {
        if (isCreate) map[key] = FormatTimestamp(now);
    }

    public static void ApplyModifyTime(IDictionary<string, object> map, string key, DateTime now, bool isCreate)
    {
        if (!isCreate) map[key] = FormatTimestamp(now);
    }

    public static bool TryTakeFlowDelegate(
        IDictionary<string, object> allDataMap,
        string delegateKey,
        string targetKey)
    {
        if (!allDataMap.ContainsKey(delegateKey))
            return false;
        allDataMap[targetKey] = allDataMap[delegateKey];
        return true;
    }

    public static void ApplyPositionId(Dictionary<string, object> map, string key, string? positionId)
        => ImportSystemFieldAssembler.MapCurrPosition(key, positionId, map);

    /// <summary>Legacy non-null tree → JSON array string.</summary>
    public static string OrganizeTreeToJson(string organizeIdTree)
        => organizeIdTree.Split(",").ToJsonString();

    /// <summary>Null/empty tree → empty string; else JSON array (last-level department path).</summary>
    public static string OrganizeTreeToJsonOrEmpty(string? organizeIdTree)
        => organizeIdTree.IsNotEmptyOrNull() ? OrganizeTreeToJson(organizeIdTree!) : string.Empty;

    public static void EnsureUploadDefault(IDictionary<string, object> map, string key)
    {
        if (!map.ContainsKey(key) || map[key].IsNullOrEmpty())
            map[key] = Array.Empty<string>();
    }
}
