using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Path / cascader / multi-user import mappers (COMSELECT·ADDRESS·USERSSELECT·CASCADER).
/// Builds label→key index once to avoid O(n) ContainsValue scans per token.
/// </summary>
public static class ImportPathSelectMapper
{
    /// <param name="allowMunicipalDistrictShortcut">
    /// CodeGen ADDRESS quirk: level==3 accepts paths with 2 '/' that contain 市辖区.
    /// VisualDev ADDRESS does not.
    /// </param>
    public static void MapComOrAddress(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        List<Dictionary<string, string>> dicList,
        Dictionary<string, object> newDataItems,
        bool clearWhenEmpty,
        bool allowMunicipalDistrictShortcut)
    {
        if (!rawValue.IsNotEmptyOrNull())
        {
            if (clearWhenEmpty)
                newDataItems[fieldKey] = null;
            return;
        }

        var byLabel = BuildLabelToKeyIndex(dicList);

        if (vModel.multiple)
        {
            var addList = new List<object>();
            foreach (var it in rawValue.ToString()!.Split(','))
            {
                if (!PassesLevelGate(vModel, it, allowMunicipalDistrictShortcut))
                {
                    ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
                    continue;
                }

                if (byLabel.TryGetValue(it, out var key))
                    addList.Add(key.Split(',').ToList());
                else
                    ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
            }

            newDataItems[fieldKey] = addList;
        }
        else
        {
            var text = rawValue.ToString();
            if (!PassesLevelGate(vModel, text, allowMunicipalDistrictShortcut))
            {
                ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
                return;
            }

            if (byLabel.TryGetValue(text!, out var key))
                newDataItems[fieldKey] = key.Split(',').ToList();
            else
                ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
        }
    }

    public static void MapUsersSelect(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        List<Dictionary<string, string>> dicList,
        Dictionary<string, object> newDataItems)
    {
        if (!rawValue.IsNotEmptyOrNull() || !ImportOptionValueMapper.IsAllOrCustomSelectType(vModel))
        {
            newDataItems[fieldKey] = null;
            return;
        }

        var byLabel = BuildLabelToKeyIndex(dicList);

        if (vModel.multiple)
        {
            var addList = new List<object>();
            foreach (var it in rawValue.ToString()!.Split(','))
            {
                if (byLabel.TryGetValue(it, out var key) || byLabel.TryGetValue(it.Split('/').Last(), out key))
                    addList.Add(key);
                else
                    ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
            }

            newDataItems[fieldKey] = addList;
        }
        else
        {
            var text = rawValue.ToString()!;
            if (byLabel.TryGetValue(text, out var key) || byLabel.TryGetValue(text.Split('/').Last(), out key))
                newDataItems[fieldKey] = key;
            else
                ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
        }
    }

    public static void MapCascader(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        List<Dictionary<string, string>> dicList,
        Dictionary<string, object> newDataItems,
        bool clearWhenEmpty)
    {
        if (!rawValue.IsNotEmptyOrNull())
        {
            if (clearWhenEmpty)
                newDataItems[fieldKey] = null;
            return;
        }

        var byLabel = BuildLabelToKeyIndex(dicList);
        var sep = vModel.separator;

        if (vModel.multiple)
        {
            var addsList = new List<object>();
            foreach (var its in rawValue.ToString()!.Split(','))
            {
                var add = new List<object>();
                foreach (var it in its.Split(sep))
                {
                    if (byLabel.TryGetValue(it, out var key))
                        add.Add(key);
                    else
                        ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
                }

                addsList.Add(add);
            }

            newDataItems[fieldKey] = addsList;
        }
        else
        {
            var addList = new List<object>();
            foreach (var it in rawValue.ToString()!.Split(sep))
            {
                if (byLabel.TryGetValue(it, out var key))
                    addList.Add(key);
                else
                    ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
            }

            newDataItems[fieldKey] = addList;
        }
    }

    /// <summary>
    /// First-wins label→key across cache rows (matches Where(ContainsValue).FirstOrDefault()).
    /// Within a single-entry row First/Last pair are equivalent to legacy usage.
    /// </summary>
    public static Dictionary<string, string> BuildLabelToKeyIndex(List<Dictionary<string, string>> dicList)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (dicList == null)
            return map;

        foreach (var row in dicList)
        {
            if (row == null || row.Count == 0)
                continue;
            foreach (var pair in row)
            {
                if (pair.Value != null && !map.ContainsKey(pair.Value))
                    map[pair.Value] = pair.Key;
            }
        }

        return map;
    }

    private static bool PassesLevelGate(FieldsModel vModel, string? path, bool allowMunicipalDistrictShortcut)
    {
        if (vModel.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT))
            return true;
        if (path == null)
            return false;

        var slashCount = path.Count(x => x == '/');
        if (slashCount == vModel.level)
            return true;
        if (allowMunicipalDistrictShortcut
            && vModel.level == 3
            && slashCount == 2
            && path.Contains("市辖区"))
            return true;
        return false;
    }
}
