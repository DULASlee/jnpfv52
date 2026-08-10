using JNPF.DependencyInjection;

namespace JNPF.Systems.Common.ModuleImport;

/// <summary>
/// Pure helpers for ModuleService.ImportData / ActionsImport
/// (copy-suffix naming, duplicate-key accumulation, sub-table conflict messages, conditionJson id remap).
/// DB AnyAsync / Storageable / Insertable stay in the service.
/// </summary>
[SuppressSniffer]
public static class ModuleImportHelpers
{
    /// <summary>
    /// Append-import rename: "{name}.副本{randomSuffix}".
    /// </summary>
    public static string FormatImportCopySuffix(string? name, string randomSuffix)
    {
        return string.Format("{0}.副本{1}", name, randomSuffix);
    }

    /// <summary>
    /// Accumulate conflict values under a Chinese key label, joined with顿号.
    /// </summary>
    public static void RecordImportDuplicateKey(
        Dictionary<string, string> dic,
        string keyLabel,
        string? value)
    {
        var text = value ?? string.Empty;
        if (dic.ContainsKey(keyLabel))
            dic[keyLabel] = string.Format("{0}、{1}", dic[keyLabel], text);
        else
            dic.Add(keyLabel, text);
    }

    /// <summary>
    /// Build sub-table skip message: "{listName}：编码(x)、名称(y)重复".
    /// Dictionary enumeration order is preserved as insertion order (.NET Core+).
    /// </summary>
    public static string FormatSubTableDuplicateMessage(
        string listName,
        Dictionary<string, string> duplicateDic)
    {
        var parts = new List<string>();
        foreach (var item in duplicateDic)
            parts.Add(string.Format("{0}({1})", item.Key, item.Value));

        return string.Format("{0}：{1}重复", listName, string.Join("、", parts));
    }

    /// <summary>
    /// Append rename for FullName + EnCode (same random suffix).
    /// </summary>
    public static (string FullName, string EnCode) ApplyAppendRename(
        string? fullName,
        string? enCode,
        string randomSuffix)
    {
        return (FormatImportCopySuffix(fullName, randomSuffix), enCode + randomSuffix);
    }

    /// <summary>
    /// Replace authorize entity ids inside scheme ConditionJson using append id map.
    /// Returns <paramref name="conditionJson"/> unchanged when null/empty or map empty.
    /// </summary>
    public static string? RewriteConditionJsonIds(
        string? conditionJson,
        IReadOnlyDictionary<string, string>? idMap)
    {
        if (string.IsNullOrEmpty(conditionJson) || idMap == null || idMap.Count == 0)
            return conditionJson;

        var result = conditionJson;
        foreach (var key in idMap.Keys)
            result = result.Replace(key, idMap[key]);
        return result;
    }
}
