using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Map display labels → stored keys for select-like controls during import.
/// Stage: 映射 (after 解析 cache / before 写入).
/// </summary>
public static class ImportOptionValueMapper
{
    /// <summary>
    /// CHECKBOX / SWITCH / SELECT / RADIO / TREESELECT mapping.
    /// </summary>
    public static void MapSelectLike(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        List<Dictionary<string, string>> dicList,
        Dictionary<string, object> newDataItems,
        bool clearWhenEmpty)
    {
        if (rawValue.IsNotEmptyOrNull())
        {
            var byLabel = ImportPathSelectMapper.BuildLabelToKeyIndex(dicList);
            if (vModel.multiple || vModel.__config__.jnpfKey.Equals(JnpfKeyConst.CHECKBOX))
            {
                var addList = new List<object>();
                foreach (var it in rawValue.ToString()!.Split(','))
                {
                    if (byLabel.TryGetValue(it, out var key))
                        addList.Add(key);
                    else
                        ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
                }

                newDataItems[fieldKey] = addList;
            }
            else
            {
                if (byLabel.TryGetValue(rawValue.ToString()!, out var key))
                    newDataItems[fieldKey] = key;
                else
                    ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
            }
        }
        else if (clearWhenEmpty)
        {
            newDataItems[fieldKey] = null;
        }
    }

    /// <summary>
    /// DEP/POS/GROUP/ROLE/USER select — match by last path segment.
    /// Caller must gate selectType (all/custom/empty); this only maps or clears empty.
    /// </summary>
    public static void MapOrgPathSelect(
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

        var byLabel = ImportPathSelectMapper.BuildLabelToKeyIndex(dicList);

        if (vModel.multiple)
        {
            var addList = new List<object>();
            foreach (var it in rawValue.ToString()!.Split(','))
            {
                var leaf = it.Split('/').Last();
                if (byLabel.TryGetValue(leaf, out var key))
                    addList.Add(key);
                else
                    ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
            }

            newDataItems[fieldKey] = addList;
        }
        else
        {
            var leaf = rawValue.ToString()!.Split('/').Last();
            if (byLabel.TryGetValue(leaf, out var key))
                newDataItems[fieldKey] = key;
            else
                ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
        }
    }

    public static bool IsAllOrCustomSelectType(FieldsModel vModel)
        => vModel.selectType.IsNullOrEmpty() || vModel.selectType.Equals("all") || vModel.selectType.Equals("custom");
}
