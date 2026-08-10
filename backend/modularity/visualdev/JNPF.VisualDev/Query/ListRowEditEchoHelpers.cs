using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Row-edit (type=4) list echo: suffix copies before parse, rebuild key/_name after parse.
/// Extracted from RunService.GetListResult.
/// </summary>
public static class ListRowEditEchoHelpers
{
    private static readonly HashSet<string> SystemEchoJnpfKeys = new(StringComparer.Ordinal)
    {
        JnpfKeyConst.TIME,
        JnpfKeyConst.CREATETIME,
        JnpfKeyConst.CREATEUSER,
        JnpfKeyConst.MODIFYTIME,
        JnpfKeyConst.MODIFYUSER,
        JnpfKeyConst.CURRDEPT,
        JnpfKeyConst.CURRORGANIZE,
        JnpfKeyConst.CURRPOSITION,
    };

    /// <summary>
    /// Duplicate each cell as key+suffix (skip RowIndex) for row-edit front-end echo.
    /// </summary>
    public static void AttachSuffixCopies(List<Dictionary<string, object>> list, string roweditId)
    {
        if (list == null || list.Count == 0 || roweditId.IsNullOrEmpty())
            return;

        foreach (var items in list)
        {
            var addItem = new Dictionary<string, object>();
            foreach (var item in items)
            {
                if (item.Key != "RowIndex")
                    addItem.Add(item.Key + roweditId, item.Value);
            }

            foreach (var item in addItem)
                items.Add(item.Key, item.Value);
        }
    }

    /// <summary>
    /// Rebuild rows: suffix keys → display value + _name; keep flow/id/system fields.
    /// </summary>
    public static List<Dictionary<string, object>> RebuildEchoRows(
        List<Dictionary<string, object>> list,
        string roweditId,
        IReadOnlyCollection<FieldsModel> allFieldsModel)
    {
        var systemVModels = BuildSystemEchoVModels(allFieldsModel);
        var newList = new List<Dictionary<string, object>>();

        foreach (var items in list)
        {
            var newItem = new Dictionary<string, object>();
            foreach (var item in items)
            {
                if (item.Key.Contains(roweditId))
                    MapSuffixCell(items, item, roweditId, newItem);

                if (item.Key.Equals("flowState") || item.Key.Equals("flowState_name")
                    || item.Key.Equals("flowId") || item.Key.Equals("flowId_name"))
                    newItem.Add(item.Key, item.Value);

                if (item.Key.Equals("id") && !newItem.ContainsKey(item.Key))
                    newItem.Add(item.Key, item.Value);

                if (systemVModels.Contains(item.Key))
                    newItem[item.Key] = items[item.Key];
            }

            newList.Add(newItem);
        }

        return newList;
    }

    public static HashSet<string> BuildSystemEchoVModels(IReadOnlyCollection<FieldsModel> allFieldsModel)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (allFieldsModel == null)
            return set;

        foreach (var x in allFieldsModel)
        {
            if (x?.__vModel__ == null || x.__config__ == null)
                continue;
            if (SystemEchoJnpfKeys.Contains(x.__config__.jnpfKey))
                set.Add(x.__vModel__);
        }

        return set;
    }

    private static void MapSuffixCell(
        Dictionary<string, object> items,
        KeyValuePair<string, object> item,
        string roweditId,
        Dictionary<string, object> newItem)
    {
        var baseKey = item.Key.Replace(roweditId, string.Empty);
        if (item.Value.IsNotEmptyOrNull())
        {
            var obj = item.Value;
            var text = obj.ToString();
            if (text != null && text.Contains("[["))
                obj = text.ToObject<List<List<object>>>();
            else if (text != null && text.Contains("["))
                obj = text.ToObject<List<object>>();

            items.TryGetValue(baseKey, out var value);
            if (value.IsNullOrEmpty())
                obj = null;
            if (!newItem.ContainsKey(baseKey))
                newItem.Add(baseKey, obj);
            if (!newItem.ContainsKey(baseKey + "_name"))
                newItem.Add(baseKey + "_name", value);
        }
        else
        {
            if (!newItem.ContainsKey(baseKey))
                newItem.Add(baseKey, null);
            if (!newItem.ContainsKey(baseKey + "_name"))
                newItem.Add(baseKey + "_name", null);
        }
    }
}
