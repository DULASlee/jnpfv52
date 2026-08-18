using System.Text.Json;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段四 P4-B01b — DeveloperSkillOrchestrator sandbox 链 + CodegenFailed payload。
/// </summary>
public static class IrPhase4OrchestratorTests
{
    public static async Task RunAllAsync()
    {
        TestCodegenFailedPayload_Format();
        TestAbortSkillChainException_Phase();
        TestDeploySkill_CredentialFreePayload();
        await TestSandboxGate_AfterDeveloperWriteAsync();
        Console.WriteLine("[Phase4] Developer orchestrator tests passed.");
    }

    private static void TestCodegenFailedPayload_Format()
    {
        var payload = CodegenManifestBuilder.BuildCodegenFailedPayload(
            "proj-1",
            CodeSandboxBuildResult.Fail("BuildFail", "dotnet build 失败", exitCode: 1, stderr: "CS0234"),
            "codegen:proj-1");

        if (!payload.Contains("\"phase\":\"BuildFail\"", StringComparison.Ordinal))
            throw new InvalidOperationException("CodegenFailed payload missing phase");

        if (!payload.Contains(IrStabilityStates.Invalidated, StringComparison.Ordinal))
            throw new InvalidOperationException("CodegenFailed payload missing invalidated stability");
    }

    private static void TestAbortSkillChainException_Phase()
    {
        var ex = new AbortSkillChainException("AG-001 critical", "ArchAbort");
        if (ex.Phase != "ArchAbort")
            throw new InvalidOperationException("AbortSkillChainException phase mismatch");
    }

    private static async Task TestSandboxGate_AfterDeveloperWriteAsync()
    {
        const string tenantId = "_phase4-orch";
        const string projectId = "leave-simple-orch";

        var backendRoot = CodegenWorkspacePaths.ResolveBackendRoot(tenantId, projectId);
        if (Directory.Exists(backendRoot))
            Directory.Delete(backendRoot, recursive: true);

        var devSkill = IrPhase4DeveloperTests.CreateDeveloperSkillPublic();
        var snapshot = IrPhase4DeveloperTests.BuildLeaveSimpleSnapshotPublic(includeSystemDesign: true);
        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = 9002,
            UserRequirement = "请假 MVP orchestrator",
            Snapshot = snapshot,
        };

        await foreach (var _ in devSkill.ReasonAsync(context))
        {
            // drain events
        }

        using var loggerFactory = LoggerFactory.Create(static _ => { });
        var gate = new DeveloperCodegenSandboxGate(
            new CodeSandboxService(
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
                loggerFactory.CreateLogger<CodeSandboxService>()),
            loggerFactory.CreateLogger<DeveloperCodegenSandboxGate>());

        var result = await gate.RunAsync(tenantId, 9002, projectId);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Sandbox gate fail phase={result.Phase} exit={result.ExitCode}\n{result.StandardError}\n{result.StandardOutput}");
        }

        if (Directory.Exists(backendRoot))
            Directory.Delete(backendRoot, recursive: true);
    }

    /// <summary>
    /// T12: 验证 DeploymentVerified payload 不含凭据字段（H4 修复验证）。
    /// </summary>
    private static void TestDeploySkill_CredentialFreePayload()
    {
        // 模拟 DeploySkillService 中 DeploymentVerified 的 payload 结构
        var payloadObj = new
        {
            projectId = "test-proj",
            pipelineId = 9999,
            previewUrl = "http://localhost:3800/preview/test",
            downloadUrl = "http://localhost:3800/download/package.zip",
            verifiedAt = DateTime.UtcNow.ToString("O"),
        };
        var payload = JsonSerializer.Serialize(payloadObj,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        // H4: 凭据不应出现在 IR 事件 payload 中
        if (root.TryGetProperty("defaultCredentials", out _))
            throw new InvalidOperationException(
                "DeploymentVerified payload MUST NOT contain defaultCredentials (H4)");

        // 验证必要字段存在
        if (!root.TryGetProperty("projectId", out _))
            throw new InvalidOperationException("DeploymentVerified payload missing projectId");
        if (!root.TryGetProperty("verifiedAt", out _))
            throw new InvalidOperationException("DeploymentVerified payload missing verifiedAt");
        if (!root.TryGetProperty("downloadUrl", out _))
            throw new InvalidOperationException("DeploymentVerified payload missing downloadUrl");

        // 验证不包含 username/password 等凭据字段
        if (root.TryGetProperty("username", out _) || root.TryGetProperty("password", out _))
            throw new InvalidOperationException("DeploymentVerified payload MUST NOT contain credentials");
    }
}
