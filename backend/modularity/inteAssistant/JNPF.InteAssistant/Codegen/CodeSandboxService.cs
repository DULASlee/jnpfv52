using System.Diagnostics;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 阶段四 D3 — codegen sandbox：Roslyn 前置 + dotnet build --no-restore。
/// </summary>
public sealed class CodeSandboxService : ICodeSandboxService, ITransient
{
    private readonly ITenantPipelineQuotaGuard? _quotaGuard;
    private readonly ILogger<CodeSandboxService> _logger;
    private readonly int _buildTimeoutSeconds;
    private readonly string _repoRoot;
    private readonly string _nugetPackagesDir;
    private readonly string _restoreMarker;

    public CodeSandboxService(
        IConfiguration configuration,
        ILogger<CodeSandboxService> logger,
        ITenantPipelineQuotaGuard? quotaGuard = null)
    {
        _logger = logger;
        _quotaGuard = quotaGuard;
        _buildTimeoutSeconds = configuration.GetValue("StudioRuntime:CodegenBuildTimeoutSeconds", 300);
        _repoRoot = VmTemplateCatalog.ResolveRepoRoot();
        _nugetPackagesDir = Path.Combine(_repoRoot, "workspace", "codegen-sandbox", ".nuget", "packages");
        _restoreMarker = Path.Combine(_repoRoot, "workspace", "codegen-sandbox", ".restore-complete");
    }

    public CodeSandboxBuildResult TryFastSyntaxCheck(string backendRoot)
    {
        var sw = Stopwatch.StartNew();
        var errors = new List<string>();

        foreach (var csFile in Directory.EnumerateFiles(backendRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(csFile);
            errors.AddRange(CodegenSyntaxValidator.GetSyntaxErrors(source, csFile));
        }

        sw.Stop();
        if (errors.Count > 0)
        {
            return CodeSandboxBuildResult.Fail(
                "SyntaxCheckFail",
                string.Join("; ", errors.Take(5)),
                elapsed: sw.Elapsed);
        }

        return CodeSandboxBuildResult.Pass("SyntaxCheckPass", sw.Elapsed);
    }

    public async Task<CodeSandboxBuildResult> EnsureRestoredAsync(CancellationToken ct = default)
    {
        if (File.Exists(_restoreMarker))
            return CodeSandboxBuildResult.Pass("NuGetAlreadyRestored", TimeSpan.Zero);

        Directory.CreateDirectory(_nugetPackagesDir);
        var templateDir = Path.Combine(_repoRoot, "workspace", "codegen-sandbox", "template");
        Directory.CreateDirectory(templateDir);

        var csprojPath = Path.Combine(templateDir, "JNPF.Codegen.Sandbox.csproj");
        await File.WriteAllTextAsync(
            csprojPath,
            CodegenWorkspaceWriter.BuildCsprojContent(_repoRoot),
            ct);

        var sw = Stopwatch.StartNew();
        var (exitCode, stdout, stderr) = await RunDotnetAsync(
            $"restore \"{csprojPath}\" --packages \"{_nugetPackagesDir}\"",
            _buildTimeoutSeconds,
            ct);
        sw.Stop();

        if (exitCode != 0)
        {
            return CodeSandboxBuildResult.Fail(
                "NuGetRestoreFail",
                "dotnet restore 失败",
                exitCode,
                stderr,
                stdout,
                sw.Elapsed);
        }

        await File.WriteAllTextAsync(_restoreMarker, DateTime.UtcNow.ToString("O"), ct);
        _logger.LogInformation("Codegen sandbox NuGet 预还原完成 ({Ms}ms)", sw.ElapsedMilliseconds);
        return CodeSandboxBuildResult.Pass("NuGetRestorePass", sw.Elapsed, stdout);
    }

    public async Task<CodeSandboxBuildResult> ValidateBuildAsync(string backendRoot, CancellationToken ct = default)
    {
        var syntax = TryFastSyntaxCheck(backendRoot);
        if (!syntax.Success)
            return syntax;

        var restore = await EnsureRestoredAsync(ct);
        if (!restore.Success)
            return restore;

        var csproj = Path.Combine(backendRoot, "JNPF.Codegen.Sandbox.csproj");
        if (!File.Exists(csproj))
            return CodeSandboxBuildResult.Fail("BuildFail", $"缺少 csproj: {csproj}");

        var restoreSw = Stopwatch.StartNew();
        var (restoreExit, restoreOut, restoreErr) = await RunDotnetAsync(
            $"restore \"{csproj}\" --packages \"{_nugetPackagesDir}\"",
            _buildTimeoutSeconds,
            ct);
        restoreSw.Stop();

        if (restoreExit != 0)
        {
            return CodeSandboxBuildResult.Fail(
                "NuGetRestoreFail",
                "sandbox csproj restore 失败",
                restoreExit,
                restoreErr,
                restoreOut,
                restoreSw.Elapsed);
        }

        var sw = Stopwatch.StartNew();
        var (exitCode, stdout, stderr) = await RunDotnetAsync(
            $"build \"{csproj}\" --no-restore -v q /nodeReuse:false -m:1 -p:BuildInParallel=false -p:RestorePackagesPath=\"{_nugetPackagesDir}\"",
            _buildTimeoutSeconds,
            ct);
        sw.Stop();

        if (exitCode != 0)
        {
            _logger.LogWarning("Codegen sandbox build 失败 exit={ExitCode} stderr={Stderr}", exitCode, stderr);
            return CodeSandboxBuildResult.Fail("BuildFail", "dotnet build 失败", exitCode, stderr, stdout, sw.Elapsed);
        }

        return CodeSandboxBuildResult.Pass("BuildPass", sw.Elapsed, stdout);
    }

    /// <summary>带租户配额的 build（E2 钩子；pipelineId=0 时跳过配额）。</summary>
    public async Task<CodeSandboxBuildResult> ValidateBuildWithQuotaAsync(
        string tenantId,
        long pipelineId,
        string backendRoot,
        CancellationToken ct = default)
    {
        if (_quotaGuard != null && pipelineId > 0 &&
            !_quotaGuard.TryAcquire(tenantId, pipelineId, out var reason, out _))
        {
            return CodeSandboxBuildResult.Fail("QuotaExceeded", reason ?? "quota exceeded");
        }

        try
        {
            return await ValidateBuildAsync(backendRoot, ct);
        }
        finally
        {
            if (_quotaGuard != null && pipelineId > 0)
                _quotaGuard.Release(tenantId, pipelineId);
        }
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunDotnetAsync(
        string arguments,
        int timeoutSeconds,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _repoRoot,
        };

        psi.Environment["NUGET_PACKAGES"] = _nugetPackagesDir;

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore kill errors
            }

            return (-1, string.Empty, $"dotnet 超时 ({timeoutSeconds}s)");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (process.ExitCode, stdout, stderr);
    }
}
