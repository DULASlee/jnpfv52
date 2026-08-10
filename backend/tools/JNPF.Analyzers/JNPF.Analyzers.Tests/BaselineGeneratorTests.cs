using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace JNPF.Analyzers.Tests;

/// <summary>
/// One-shot baseline regenerator (PowerShell/xUnit; no new business .mjs).
/// Run: $env:GENERATE_COMPLEXITY_BASELINE='1'; dotnet test ... --filter GenerateBaseline
/// </summary>
public sealed class BaselineGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public BaselineGeneratorTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void GenerateBaseline_FromBackendSources_WhenEnvSet()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("GENERATE_COMPLEXITY_BASELINE"), "1", StringComparison.Ordinal))
        {
            _output.WriteLine("Skip: set GENERATE_COMPLEXITY_BASELINE=1 to regenerate.");
            return;
        }

        var repoRoot = FindRepoRoot();
        var backend = Path.Combine(repoRoot, "backend");
        var threshold = 30;
        var entries = new List<object>();

        foreach (var file in Directory.EnumerateFiles(backend, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}tools{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text, path: file);
            var root = tree.GetCompilationUnitRoot();
            var rel = Path.GetRelativePath(backend, file).Replace('\\', '/');

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Body is null && method.ExpressionBody is null)
                    continue;
                var cc = CyclomaticComplexityWalker.Compute(method);
                if (cc < threshold)
                    continue;

                var name = method.Identifier.Text;
                entries.Add(new
                {
                    symbol = $"{rel}::{name}",
                    name,
                    maxComplexity = cc,
                    file = rel,
                    measuredBy = "CyclomaticComplexityWalker",
                });
            }
        }

        // Prefer higher maxComplexity if duplicate file::name
        var merged = entries
            .GroupBy(e => ((dynamic)e).symbol as string)
            .Select(g => g.OrderByDescending(e => (int)((dynamic)e).maxComplexity).First())
            .OrderByDescending(e => (int)((dynamic)e).maxComplexity)
            .ThenBy(e => ((dynamic)e).file as string)
            .ToList();

        var payload = new
        {
            version = 1,
            threshold,
            source = "Roslyn CyclomaticComplexityWalker scan + check02 seed merge",
            generatedAt = DateTimeOffset.UtcNow,
            note = "Stock exemption for CC>=threshold. Do not raise maxComplexity without CR; lower when methods are split.",
            entries = merged,
        };

        var outPath = Path.Combine(backend, "tools", "JNPF.Analyzers", "complexity-baseline.json");
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outPath, json, Encoding.UTF8);
        _output.WriteLine($"Wrote {outPath} entries={merged.Count}");
        Assert.True(merged.Count >= 41, $"Expected at least inventory 41, got {merged.Count}");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CLAUDE.md"))
                && Directory.Exists(Path.Combine(dir.FullName, "backend")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found from " + AppContext.BaseDirectory);
    }
}
