using System.Reflection;
using System.Text;
using NetArchTest.Rules;
using Xunit;
using Xunit.Abstractions;

namespace JNPF.Tests.Architecture;

/// <summary>
/// ARCH-01: framework/common must not depend on InteAssistant*.
/// W4: Common.Core hard-fails (bridge); Message.Interfaces cleared via IntegrateTaskMessageDto.
/// Remaining ProjectReference exemption: API.Entry composition root only.
/// </summary>
public sealed class LayeringTests
{
    private readonly ITestOutputHelper _output;

    public LayeringTests(ITestOutputHelper output) => _output = output;

    // bin/Debug/net8.0 → … → repo root (6 levels up from BaseDirectory)
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

    private static readonly string EvidenceDir = Path.Combine(RepoRoot, ".claude", "evidence", "backend-quality-check");

    /// <summary>
    /// Known remaining ProjectReference exemptions (not Common.Core / not Message.Interfaces).
    /// </summary>
    private static readonly HashSet<string> ProjectReferenceExemptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "backend/application/JNPF.API.Entry/JNPF.API.Entry.csproj", // composition root
    };

    [Fact]
    public void ARCH01_JNPF_Framework_ShouldNot_DependOn_InteAssistant()
    {
        var jnpf = typeof(JNPF.App).Assembly;
        var result = Types.InAssembly(jnpf)
            .ShouldNot()
            .HaveDependencyOnAny(
                "JNPF.InteAssistant",
                "JNPF.InteAssistant.Entitys",
                "JNPF.InteAssistant.Engine")
            .GetResult();

        WriteArchResult("arch01-jnpf-framework", result, jnpf);
        Assert.True(result.IsSuccessful, FormatFail(result, "JNPF → InteAssistant*"));
    }

    [Fact]
    public void ARCH01_CommonCore_ShouldNot_DependOn_InteAssistant()
    {
        var commonCore = typeof(JNPF.Common.Core.Manager.UserManager).Assembly;
        var result = Types.InAssembly(commonCore)
            .ShouldNot()
            .HaveDependencyOnAny(
                "JNPF.InteAssistant",
                "JNPF.InteAssistant.Entitys",
                "JNPF.InteAssistant.Engine")
            .GetResult();

        WriteArchResult("arch01-common-core", result, commonCore);
        Assert.True(result.IsSuccessful, FormatFail(result, "Common.Core → InteAssistant*"));
    }

    [Fact]
    public void ARCH01_ProjectReference_Scan_CommonCore_Cleared_ExemptionsOnly()
    {
        var backend = Path.Combine(RepoRoot, "backend");
        Assert.True(Directory.Exists(backend), $"backend missing: {backend}");
        var hits = new List<string>();
        foreach (var csproj in Directory.EnumerateFiles(backend, "*.csproj", SearchOption.AllDirectories))
        {
            if (csproj.Contains($"{Path.DirectorySeparatorChar}inteAssistant{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                continue;
            if (csproj.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                continue;
            if (csproj.Contains($"{Path.DirectorySeparatorChar}tools{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                continue;

            var text = File.ReadAllText(csproj);
            if (text.Contains("InteAssistant", StringComparison.OrdinalIgnoreCase))
            {
                hits.Add(Path.GetRelativePath(RepoRoot, csproj).Replace('\\', '/'));
            }
        }

        Directory.CreateDirectory(EvidenceDir);
        var path = Path.Combine(EvidenceDir, "arch01-project-references.json");
        File.WriteAllText(
            path,
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    generatedAt = DateTimeOffset.UtcNow,
                    repoRoot = RepoRoot,
                    count = hits.Count,
                    hits,
                    exemptions = ProjectReferenceExemptions.OrderBy(x => x).ToArray(),
                    commonCoreCleared = !hits.Any(h =>
                        h.Equals("backend/modularity/common/JNPF.Common.Core/JNPF.Common.Core.csproj", StringComparison.OrdinalIgnoreCase)),
                },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine($"Wrote {path} count={hits.Count}");

        Assert.DoesNotContain(
            hits,
            h => h.Equals("backend/modularity/common/JNPF.Common.Core/JNPF.Common.Core.csproj", StringComparison.OrdinalIgnoreCase));

        var unexpected = hits.Where(h => !ProjectReferenceExemptions.Contains(h)).ToList();
        Assert.True(
            unexpected.Count == 0,
            "Unexpected InteAssistant ProjectReference outside exemption list:\n - " + string.Join("\n - ", unexpected));
    }

    private void WriteArchResult(string name, TestResult result, Assembly assembly)
    {
        Directory.CreateDirectory(EvidenceDir);
        var failing = result.FailingTypeNames?.Take(50).ToArray() ?? Array.Empty<string>();
        var payload = new
        {
            generatedAt = DateTimeOffset.UtcNow,
            rule = name,
            assembly = assembly.GetName().Name,
            isSuccessful = result.IsSuccessful,
            failingTypeCount = result.FailingTypeNames?.Count() ?? 0,
            failingTypeNamesSample = failing,
        };
        var path = Path.Combine(EvidenceDir, name + ".json");
        File.WriteAllText(
            path,
            System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine($"Wrote {path}");
    }

    private static string FormatFail(TestResult result, string title)
    {
        var sb = new StringBuilder();
        sb.Append(title).AppendLine();
        foreach (var t in result.FailingTypeNames?.Take(30) ?? Array.Empty<string>())
            sb.Append(" - ").AppendLine(t);
        return sb.ToString();
    }
}
