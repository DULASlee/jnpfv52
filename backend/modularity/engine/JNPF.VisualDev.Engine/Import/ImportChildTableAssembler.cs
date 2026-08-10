using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Child TABLE field import: prefix keys → recursive assemble → unprefix + bubble errors.
/// Shared by VisualDev and CodeGen ImportDataAssemble.
/// </summary>
public static class ImportChildTableAssembler
{
    public static async Task MapAsync(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        Dictionary<string, List<Dictionary<string, string>>> cDataList,
        Dictionary<string, object> newDataItems,
        Func<List<FieldsModel>, List<Dictionary<string, object>>, Dictionary<string, List<Dictionary<string, string>>>, Task<List<Dictionary<string, object>>>> assembleAsync,
        bool clearWhenEmpty)
    {
        if (rawValue == null)
        {
            if (clearWhenEmpty)
                newDataItems[fieldKey] = null;
            return;
        }

        var prefix = vModel.__vModel__ + "-";
        var valueList = rawValue.ToObject<List<Dictionary<string, object>>>();
        var newValueList = new List<Dictionary<string, object>>();
        foreach (var it in valueList)
        {
            var addValue = new Dictionary<string, object>();
            foreach (var value in it)
                addValue.Add(prefix + value.Key, value.Value);
            newValueList.Add(addValue);
        }

        var res = await assembleAsync(vModel.__config__.children, newValueList, cDataList);
        MergeChildErrors(newDataItems, res);
        newDataItems[fieldKey] = StripPrefix(res, prefix);
    }

    /// <summary>Prefix child row keys for recursive assemble (pure; testable).</summary>
    public static List<Dictionary<string, object>> PrefixRows(string tableVModel, IEnumerable<Dictionary<string, object>> rows)
    {
        var prefix = tableVModel + "-";
        var list = new List<Dictionary<string, object>>();
        foreach (var it in rows)
        {
            var addValue = new Dictionary<string, object>();
            foreach (var value in it)
                addValue.Add(prefix + value.Key, value.Value);
            list.Add(addValue);
        }

        return list;
    }

    /// <summary>Strip table prefix from assembled row keys (pure; testable).</summary>
    public static List<Dictionary<string, object>> StripPrefix(List<Dictionary<string, object>> rows, string prefix)
    {
        var result = new List<Dictionary<string, object>>();
        foreach (var it in rows)
        {
            var addValue = new Dictionary<string, object>();
            foreach (var value in it)
                addValue.Add(value.Key.Replace(prefix, string.Empty), value.Value);
            result.Add(addValue);
        }

        return result;
    }

    public static void MergeChildErrors(
        Dictionary<string, object> parentRow,
        List<Dictionary<string, object>> childRows)
    {
        var errorKey = ImportAssembleErrors.ErrorKey;
        if (!childRows.Any(x => x.ContainsKey(errorKey)))
            return;

        var errRow = childRows.First(x => x.ContainsKey(errorKey));
        ImportAssembleErrors.Append(parentRow, errRow[errorKey]?.ToString() ?? string.Empty);
        childRows.Remove(errRow);
    }
}
