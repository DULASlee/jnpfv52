using System.Text.RegularExpressions;

namespace JNPF.Tests.Agent;

/// <summary>
/// Strict SQL normaliser for query-equivalence assertions.
///
/// v5 — kept identical to v4 (accepted). Performs:
///   1. Whitespace collapse
///   2. Parameter mask to "@p"
///   3. Keyword lower-casing
/// </summary>
public static class SqlSugarQueryCaptureHelper
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex ParameterRegex = new(@"@\w+|__param_\d+__", RegexOptions.Compiled);
    private static readonly Regex KeywordsRegex = new(
        @"\b(SELECT|FROM|WHERE|AND|OR|ORDER BY|GROUP BY|JOIN|LEFT|RIGHT|INNER|OUTER|ON|AS|DESC|ASC|IS|NULL|NOT|IN|IIF|CASE|WHEN|THEN|ELSE|END)\b",
        RegexOptions.Compiled);

    public static string NormalizeSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return string.Empty;
        var n = sql.Trim().TrimEnd(';');
        n = WhitespaceRegex.Replace(n, " ");
        n = ParameterRegex.Replace(n, "@p");
        n = KeywordsRegex.Replace(n, m => m.Value.ToLowerInvariant());
        return n.Trim();
    }
}