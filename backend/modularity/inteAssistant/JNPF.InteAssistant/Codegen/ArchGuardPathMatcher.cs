using System.Text.RegularExpressions;

namespace JNPF.InteAssistant.Codegen;

internal static class ArchGuardPathMatcher
{
    private const string BackendMarker = "/backend/";

    public static IEnumerable<string> EnumerateTargetFiles(string backendRoot, IReadOnlyList<ArchGuardTargetEntry> targets)
    {
        if (!Directory.Exists(backendRoot))
            yield break;

        var globs = MergePathGlobs(targets);
        foreach (var file in Directory.EnumerateFiles(backendRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(backendRoot, file).Replace('\\', '/');
            if (globs.Count == 0 || globs.Any(g => MatchesGlob(relative, NormalizeBackendGlob(g))))
                yield return file;
        }
    }

    public static bool MatchesGlob(string relativePath, string pattern)
    {
        relativePath = relativePath.Replace('\\', '/');
        pattern = NormalizeBackendGlob(pattern).Replace('\\', '/');

        var candidates = new List<string> { pattern };
        if (pattern.StartsWith("**/", StringComparison.Ordinal))
            candidates.Add(pattern[3..]);

        return candidates.Any(p => Regex.IsMatch(relativePath, GlobToRegex(p), RegexOptions.IgnoreCase));
    }

    private static string GlobToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", "[^/]") + "$";
    }

    public static string NormalizeBackendGlob(string pathGlob)
    {
        var normalized = pathGlob.Replace('\\', '/');
        var idx = normalized.IndexOf(BackendMarker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            return normalized[(idx + BackendMarker.Length)..];
        return normalized;
    }

    public static List<string> MergePathGlobs(IReadOnlyList<ArchGuardTargetEntry> targets)
    {
        var globs = new List<string>();
        foreach (var entry in targets)
        {
            if (entry.PathGlob?.Values is { Count: > 0 })
                globs.AddRange(entry.PathGlob.Values);
        }

        return globs;
    }

    public static string? MergeFragmentType(IReadOnlyList<ArchGuardTargetEntry> targets)
    {
        foreach (var entry in targets)
        {
            if (!string.IsNullOrWhiteSpace(entry.FragmentType))
                return entry.FragmentType;
        }

        return null;
    }
}
