using System.Diagnostics;
using JNPF.InteAssistant.Codegen;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// D11-D12 P4-B06 — codegen-host-demo 注入 leave-simple + 全工程 build。
/// </summary>
public static class CodegenHostDemoTests
{
    public static async Task RunAllAsync()
    {
        await TestHostDemo_InjectLeaveSimpleAsync();
        Console.WriteLine("[Phase4] Host demo inject passed (full build: scripts/phase4-d11-host-build.mjs).");
    }

    private static async Task TestHostDemo_InjectLeaveSimpleAsync()
    {
        const string tenantId = "_d3-gate";
        const string projectId = "leave-simple";
        var repoRoot = VmTemplateCatalog.ResolveRepoRoot();
        var sourceRoot = CodegenWorkspacePaths.ResolveBackendRoot(tenantId, projectId);
        var hostDir = Path.Combine(repoRoot, "workspace", "codegen-host-demo");
        var hostSln = Path.Combine(hostDir, "JNPF.Codegen.HostDemo.sln");
        var targetRoot = Path.Combine(hostDir, "Modules", "Generated");

        if (!Directory.Exists(sourceRoot) || !Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).Any())
        {
            await CodegenSandboxGateTests.RunAllAsync();
        }

        if (!Directory.Exists(sourceRoot))
            throw new InvalidOperationException($"Generated backend missing: {sourceRoot}");

        CopyTree(sourceRoot, targetRoot);

        var csCount = Directory.EnumerateFiles(targetRoot, "*.cs", SearchOption.AllDirectories).Count();
        if (csCount < 4)
            throw new InvalidOperationException($"Injected Modules/Generated has too few .cs files: {csCount}");

        if (!File.Exists(hostSln))
            throw new InvalidOperationException($"Host solution missing: {hostSln}");

        Console.WriteLine($"  host inject OK ({csCount} cs) → {targetRoot}");
    }

    /// <summary>完整宿主 build（慢路径，供 scripts/phase4-d11-host-build.mjs 调用）。</summary>
    public static async Task RunFullBuildAsync()
    {
        const string tenantId = "_d3-gate";
        const string projectId = "leave-simple";
        var repoRoot = VmTemplateCatalog.ResolveRepoRoot();
        var sourceRoot = CodegenWorkspacePaths.ResolveBackendRoot(tenantId, projectId);
        var hostDir = Path.Combine(repoRoot, "workspace", "codegen-host-demo");
        var hostSln = Path.Combine(hostDir, "JNPF.Codegen.HostDemo.sln");
        var targetRoot = Path.Combine(hostDir, "Modules", "Generated");
        var nugetPackages = Path.Combine(repoRoot, "workspace", "codegen-sandbox", ".nuget", "packages");

        if (!Directory.Exists(sourceRoot))
            await CodegenSandboxGateTests.RunAllAsync();

        CopyTree(sourceRoot, targetRoot);

        Directory.CreateDirectory(nugetPackages);
        var hostMarker = Path.Combine(hostDir, ".restore-complete");
        if (!File.Exists(hostMarker))
        {
            var restoreExit = await RunDotnetAsync(
                repoRoot,
                nugetPackages,
                $"restore \"{hostSln}\" --packages \"{nugetPackages}\"",
                1800);
            if (restoreExit != 0)
                throw new InvalidOperationException($"Host restore failed exit={restoreExit}");
            await File.WriteAllTextAsync(hostMarker, DateTime.UtcNow.ToString("O"));
        }

        var buildExit = await RunDotnetAsync(
            repoRoot,
            nugetPackages,
            $"build \"{hostSln}\" --no-restore -v q /p:RestorePackagesPath=\"{nugetPackages}\"",
            1800);
        if (buildExit != 0)
            throw new InvalidOperationException($"Host build failed exit={buildExit}");

        Console.WriteLine($"  host-demo full build OK → {hostSln}");
    }

    private static void CopyTree(string sourceRoot, string targetRoot)
    {
        if (Directory.Exists(targetRoot))
            Directory.Delete(targetRoot, recursive: true);
        Directory.CreateDirectory(targetRoot);

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            var rel = Path.GetRelativePath(sourceRoot, file);
            if (rel.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || rel.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || rel.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || rel.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var dest = Path.Combine(targetRoot, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private static async Task<int> RunDotnetAsync(
        string workingDirectory,
        string nugetPackages,
        string arguments,
        int timeoutSeconds)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.Environment["NUGET_PACKAGES"] = nugetPackages;

        using var process = Process.Start(psi)!;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return -1;
        }

        return process.ExitCode;
    }
}
