using Xunit;
using Xunit.Abstractions;

namespace JNPF.Tests.Architecture;

/// <summary>
/// NDH-06 guard: every physical table must be mapped by exactly one SugarTable entity.
/// Regression for BASE_AI_Call_LOG double-entity definition (API.Entry vs InteAssistant, removed 2026-08-26).
/// </summary>
public sealed class SugarTableMappingTests
{
    private readonly ITestOutputHelper _output;

    public SugarTableMappingTests(ITestOutputHelper output) => _output = output;

    // bin/Debug/net8.0 → repo root (6 levels up from BaseDirectory)
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

    [Fact]
    public void SugarTable_Mappings_ShouldBe_Unique()
    {
        var backend = Path.Combine(RepoRoot, "backend");
        Assert.True(Directory.Exists(backend), $"backend missing: {backend}");

        var seen = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cs in Directory.EnumerateFiles(backend, "*.cs", SearchOption.AllDirectories))
        {
            var norm = cs.Replace('\\', '/');
            if (norm.Contains("/tests/", StringComparison.OrdinalIgnoreCase) ||
                norm.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
                norm.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                norm.Contains("/tools/", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var line in File.ReadLines(cs))
            {
                var idx = line.IndexOf("[SugarTable(", StringComparison.Ordinal);
                if (idx < 0)
                    continue;

                var open = line.IndexOf('"', idx);
                if (open < 0)
                    continue;

                var close = line.IndexOf('"', open + 1);
                if (close < 0)
                    continue;

                var table = line[(open + 1)..close];
                if (string.IsNullOrWhiteSpace(table))
                    continue;

                if (!seen.TryGetValue(table, out var files))
                    seen[table] = files = new List<string>();
                files.Add(norm[(RepoRoot.Length + 1)..]);
            }
        }

        var duplicates = seen.Where(kv => kv.Value.Count > 1).ToList();
        foreach (var dup in duplicates)
            _output.WriteLine($"DUPLICATE {dup.Key}: {string.Join(" | ", dup.Value)}");

        Assert.True(
            duplicates.Count == 0,
            $"Duplicate SugarTable mappings found: {duplicates.Count}. See test output.");
    }
}
