namespace JNPF.InteAssistant.Codegen;

public interface ICodeSandboxService
{
    /// <summary>Roslyn 语法树前置校验（E1）。</summary>
    CodeSandboxBuildResult TryFastSyntaxCheck(string backendRoot);

    /// <summary>首次 restore sandbox NuGet 缓存（workspace/codegen-sandbox）。</summary>
    Task<CodeSandboxBuildResult> EnsureRestoredAsync(CancellationToken ct = default);

    /// <summary>dotnet build --no-restore，120s 超时。</summary>
    Task<CodeSandboxBuildResult> ValidateBuildAsync(string backendRoot, CancellationToken ct = default);

    /// <summary>带租户 pipeline 配额的 build（E2）。</summary>
    Task<CodeSandboxBuildResult> ValidateBuildWithQuotaAsync(
        string tenantId,
        long pipelineId,
        string backendRoot,
        CancellationToken ct = default);
}
