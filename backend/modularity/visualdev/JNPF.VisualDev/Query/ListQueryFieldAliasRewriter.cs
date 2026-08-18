namespace JNPF.VisualDev.Query;

/// <summary>
/// Rewrites quoted JSON field keys to table aliases — extracted from RunService.GetListQuerySql.
/// </summary>
public static class ListQueryFieldAliasRewriter
{
    /// <summary>
    /// Replace every "oldKey" occurrence with "newKey" inside a JSON fragment.
    /// </summary>
    public static string ReplaceQuotedKey(string json, string oldKey, string newKey)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(oldKey))
            return json;
        return json.Replace(string.Format("\"{0}\"", oldKey), string.Format("\"{0}\"", newKey));
    }

    /// <summary>
    /// Apply all AllTableFields mappings (vModel → table.column) to a JSON fragment.
    /// </summary>
    public static string RewriteAll(string json, IEnumerable<KeyValuePair<string, string>> fieldMap)
    {
        if (string.IsNullOrEmpty(json) || fieldMap == null)
            return json;
        foreach (var item in fieldMap)
            json = ReplaceQuotedKey(json, item.Key, item.Value);
        return json;
    }
}
