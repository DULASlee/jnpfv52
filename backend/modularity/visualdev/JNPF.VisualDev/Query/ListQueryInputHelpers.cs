using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Pure pre/post helpers for RunService.GetListResult query shaping
/// (search-list enrichment, page size, flow primary remap).
/// </summary>
public static class ListQueryInputHelpers
{
    public const int UnlimitedPageSize = 999999;

    /// <summary>
    /// Append missing queryJson keys into searchList (PC or App column search).
    /// Legacy inner searchMultiple branch is dead (!Any then Any) — preserved.
    /// </summary>
    public static void EnrichSearchListFromQuery(
        Dictionary<string, object>? queryJson,
        List<IndexSearchFieldModel> searchList,
        List<FieldsModel> allFieldsModel)
    {
        if (queryJson == null)
            return;

        foreach (KeyValuePair<string, object> item in queryJson)
        {
            if (!searchList.Any(it => it.id.Equals(item.Key)) && !item.Key.Equals(JnpfKeyConst.JNPFKEYWORD))
            {
                var vmodel = allFieldsModel.Find(it => it.__vModel__.Equals(item.Key));
                // Legacy dead branch: outer !Any makes this Any always false.
                if (searchList.Any(it => it.id.Equals(item.Key)))
                {
                    vmodel.searchMultiple = searchList.Find(it => it.id.Equals(item.Key)).searchMultiple;
                }
                var searchModel = vmodel.ToObject<IndexSearchFieldModel>();
                searchModel.id = item.Key;
                searchList.Add(searchModel);
            }
        }
    }

    /// <summary>
    /// Tree table (type 5) or pagination-off → unlimited page size.
    /// </summary>
    public static int ResolveEffectivePageSize(int pageSize, bool hasPage, int columnType)
    {
        if (columnType.Equals(5) || !hasPage)
            return UnlimitedPageSize;
        return pageSize;
    }

    /// <summary>
    /// Remap list primary values: find map entry where Value equals current primary, write Key.
    /// Used after GetPIdsByFlowIds (flow form auto-increment id → flow id).
    /// </summary>
    public static void RemapPrimaryKeysByValue(
        List<Dictionary<string, object>> list,
        string primaryKey,
        IEnumerable<KeyValuePair<string, string>> idMap)
    {
        if (list == null || list.Count == 0 || idMap == null || string.IsNullOrEmpty(primaryKey))
            return;

        var mapList = idMap as IList<KeyValuePair<string, string>> ?? idMap.ToList();
        foreach (var item in list)
        {
            item[primaryKey] = mapList.First(x => x.Value.Equals(item[primaryKey].ToString())).Key;
        }
    }
}
