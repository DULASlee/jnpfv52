using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Transfer;

/// <summary>
/// Merge auto field-map rules for flow form data transfer.
/// Replaces O(n²) Any/Find scans with HashSet/Dictionary lookups.
/// </summary>
public static class FlowFormMapRuleMerger
{
    /// <summary>
    /// Auto-map fields that share the same vModel + multiple flag, then fold into <paramref name="mapRule"/>.
    /// Behavior matches the former inline block in SaveDataToDataByFId.
    /// </summary>
    public static List<Dictionary<string, string>> MergeAutoMappedFields(
        IReadOnlyList<FieldsModel> oldFields,
        IReadOnlyList<FieldsModel> newFields,
        List<Dictionary<string, string>>? mapRule)
    {
        mapRule ??= new List<Dictionary<string, string>>();

        // Legacy used Any(vModel && multiple) — index compatible (vModel, multiple) pairs.
        var newCompat = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in newFields)
        {
            if (field.__vModel__.IsNullOrEmpty())
                continue;
            newCompat.Add(CompatKey(field.__vModel__, field.multiple));
        }

        var occupiedKeys = new HashSet<string>(StringComparer.Ordinal);
        var occupiedValues = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in mapRule)
        {
            foreach (var pair in entry)
            {
                occupiedKeys.Add(pair.Key);
                occupiedValues.Add(pair.Value);
            }
        }

        foreach (var item in oldFields)
        {
            if (item.__vModel__.IsNullOrEmpty())
                continue;
            if (!newCompat.Contains(CompatKey(item.__vModel__, item.multiple)))
                continue;
            if (occupiedKeys.Contains(item.__vModel__) || occupiedValues.Contains(item.__vModel__))
                continue;

            mapRule.Add(new Dictionary<string, string> { { item.__vModel__, item.__vModel__ } });
            occupiedKeys.Add(item.__vModel__);
            occupiedValues.Add(item.__vModel__);
        }

        return mapRule;
    }

    /// <summary>
    /// Index fields by vModel for O(1) lookup during transfer (first wins).
    /// </summary>
    public static Dictionary<string, FieldsModel> IndexByVModel(IEnumerable<FieldsModel> fields)
    {
        var map = new Dictionary<string, FieldsModel>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (field.__vModel__.IsNullOrEmpty() || map.ContainsKey(field.__vModel__))
                continue;
            map[field.__vModel__] = field;
        }

        return map;
    }

    private static string CompatKey(string vModel, bool multiple) => multiple ? vModel + "\0m1" : vModel + "\0m0";
}
