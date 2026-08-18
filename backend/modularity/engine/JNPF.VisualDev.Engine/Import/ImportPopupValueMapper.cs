using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Map Excel display labels → stored propsValue for POPUPSELECT / POPUPTABLESELECT.
/// Cache rows from GetDynamicList are multi-column (full remote row), not single id→label pairs.
/// </summary>
public static class ImportPopupValueMapper
{
    public static void Map(
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

        var propsValue = vModel.propsValue;
        if (propsValue.IsNullOrEmpty())
        {
            ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
            return;
        }

        var displayField = ResolveDisplayField(vModel, dicList);

        if (vModel.multiple)
        {
            var addList = new List<object>();
            foreach (var token in rawValue.ToString()!.Split(','))
            {
                var key = FindStoredKey(dicList, displayField, propsValue, token);
                if (key != null)
                    addList.Add(key);
                else
                    ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
            }

            newDataItems[fieldKey] = addList;
        }
        else
        {
            var key = FindStoredKey(dicList, displayField, propsValue, rawValue.ToString()!);
            if (key != null)
                newDataItems[fieldKey] = key;
            else
                ImportAssembleErrors.AppendMismatch(newDataItems, vModel.__config__.label);
        }
    }

    /// <summary>
    /// Prefer relationField; else columnOptions.First().value (same as FormDataParsing display path);
    /// last resort: first key on a cache row.
    /// </summary>
    public static string? ResolveDisplayField(
        FieldsModel vModel,
        List<Dictionary<string, string>> dicList)
    {
        if (vModel.relationField.IsNotEmptyOrNull())
            return vModel.relationField;

        var fromColumns = vModel.columnOptions?.FirstOrDefault()?.value;
        if (fromColumns.IsNotEmptyOrNull())
            return fromColumns;

        var first = dicList?.FirstOrDefault();
        return first?.Keys.FirstOrDefault();
    }

    public static string? FindStoredKey(
        List<Dictionary<string, string>> dicList,
        string? displayField,
        string propsValue,
        string excelToken)
    {
        if (dicList == null || displayField.IsNullOrEmpty())
            return null;

        foreach (var row in dicList)
        {
            if (!row.ContainsKey(propsValue) || !row.ContainsKey(displayField))
                continue;
            if (row[displayField] == excelToken)
                return row[propsValue];
        }

        return null;
    }
}
