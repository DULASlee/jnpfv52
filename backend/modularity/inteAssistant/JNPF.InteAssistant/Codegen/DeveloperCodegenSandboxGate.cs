using JNPF.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// D5 — developer 落盘后的 sandbox 门禁（Roslyn 前置 + dotnet build）。
/// </summary>
public interface IDeveloperCodegenSandboxGate
{
    Task<CodeSandboxBuildResult> RunAsync(
        string tenantId,
        long pipelineId,
        string projectId,
        CancellationToken ct = default);
}

public sealed class DeveloperCodegenSandboxGate : IDeveloperCodegenSandboxGate, ITransient
{
    private readonly ICodeSandboxService _sandbox;
    private readonly ILogger<DeveloperCodegenSandboxGate> _logger;

    public DeveloperCodegenSandboxGate(
        ICodeSandboxService sandbox,
        ILogger<DeveloperCodegenSandboxGate> logger)
    {
        _sandbox = sandbox;
        _logger = logger;
    }

    public async Task<CodeSandboxBuildResult> RunAsync(
        string tenantId,
        long pipelineId,
        string projectId,
        CancellationToken ct = default)
    {
        var backendRoot = CodegenWorkspacePaths.ResolveBackendRoot(tenantId, projectId);
        if (!Directory.Exists(backendRoot))
        {
            return CodeSandboxBuildResult.Fail(
                "BuildFail",
                $"workspace 不存在: {backendRoot}");
        }

        var syntax = _sandbox.TryFastSyntaxCheck(backendRoot);
        if (!syntax.Success)
        {
            _logger.LogWarning(
                "Codegen sandbox syntax fail project={ProjectId} phase={Phase}",
                projectId,
                syntax.Phase);
            return syntax;
        }

        var build = await _sandbox.ValidateBuildAsync(backendRoot, ct);
        if (!build.Success)
        {
            _logger.LogWarning(
                "Codegen sandbox build fail project={ProjectId} phase={Phase} exit={ExitCode}",
                projectId,
                build.Phase,
                build.ExitCode);
        }

        return build;
    }
}
