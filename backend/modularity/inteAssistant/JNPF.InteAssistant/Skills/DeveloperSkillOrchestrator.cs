using System.Collections.Concurrent;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 阶段四 P4-B01b/D7 — developer-skill → sandbox build → arch-guard → promote stable。
/// </summary>
public interface IDeveloperSkillOrchestrator
{
    Task<DeveloperOrchestratorResult> RunAsync(
        long pipelineId,
        string tenantId,
        string projectId,
        DeveloperOrchestratorOptions? options,
        CancellationToken ct);

    Task<DeveloperOrchestratorStatus> GetStatusAsync(
        long pipelineId,
        string tenantId,
        string projectId,
        CancellationToken ct);
}

public sealed class DeveloperOrchestratorOptions
{
    public string? ProviderCode { get; init; }
}

public sealed class DeveloperOrchestratorResult
{
    public string OrchestratorRunId { get; init; } = string.Empty;
    public string Status { get; init; } = "completed";
    public SkillRunResult? DeveloperSkillResult { get; init; }
    public SkillRunResult? TesterSkillResult { get; init; }
    public CodeSandboxBuildResult? SandboxResult { get; init; }
    public ArchGuardScanResult? ArchGuardResult { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class DeveloperOrchestratorStatus
{
    public long PipelineId { get; init; }
    public string ProjectId { get; init; } = string.Empty;
    public bool DesignLocked { get; init; }
    public bool CodegenDraft { get; init; }
    public bool SandboxBuildPassed { get; init; }
    public bool ArchGuardPassed { get; init; }
    public int ArchWarningCount { get; init; }
    public string CodegenStability { get; init; } = IrStabilityStates.Draft;
    public string? LastDeveloperRunId { get; init; }
    public string? LastDeveloperStatus { get; init; }
}

public sealed class DeveloperSkillOrchestrator : IDeveloperSkillOrchestrator, ITransient
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProjectLocks = new(StringComparer.Ordinal);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISkillHarness _harness;
    private readonly IDeveloperCodegenSandboxGate _sandboxGate;
    private readonly IArchGuardService _archGuard;
    private readonly IIrEventStoreService _eventStore;
    private readonly ISystemDesignLockedCompletenessGate _completenessGate;
    private readonly ILogger<DeveloperSkillOrchestrator> _logger;

    public DeveloperSkillOrchestrator(
        ISkillHarness harness,
        IDeveloperCodegenSandboxGate sandboxGate,
        IArchGuardService archGuard,
        IIrEventStoreService eventStore,
        ISystemDesignLockedCompletenessGate completenessGate,
        ILogger<DeveloperSkillOrchestrator> logger)
    {
        _harness = harness;
        _sandboxGate = sandboxGate;
        _archGuard = archGuard;
        _eventStore = eventStore;
        _completenessGate = completenessGate;
        _logger = logger;
    }

    public async Task<DeveloperOrchestratorResult> RunAsync(
        long pipelineId,
        string tenantId,
        string projectId,
        DeveloperOrchestratorOptions? options,
        CancellationToken ct)
    {
        var orchestratorRunId = Guid.NewGuid().ToString("N");
        var snapshot = await LoadSnapshotAsync(tenantId, projectId, ct);
        await ValidatePreconditionsAsync(snapshot, ct);

        var projectLock = ProjectLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await projectLock.WaitAsync(ct);

        try
        {
            SkillRunResult devResult;
            try
            {
                devResult = await _harness.RunAsync(
                    DevelopmentSkillIds.Developer,
                    pipelineId,
                    tenantId,
                    projectId,
                    new SkillRunOptions { ProviderCode = options?.ProviderCode },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Developer skill failed pipeline={PipelineId}", pipelineId);
                return new DeveloperOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = "developer-failed",
                    ErrorMessage = ex.Message,
                };
            }

            var fragmentId = $"codegen:{projectId}";
            var sandbox = await _sandboxGate.RunAsync(tenantId, pipelineId, projectId, ct);
            if (!sandbox.Success)
            {
                await AppendCodegenFailedAsync(projectId, tenantId, fragmentId, sandbox, ct);
                return new DeveloperOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = "codegen-failed",
                    DeveloperSkillResult = devResult,
                    SandboxResult = sandbox,
                    ErrorMessage = sandbox.ErrorMessage ?? sandbox.Phase,
                };
            }

            await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.CodegenBuildValidated,
                FragmentId = fragmentId,
                FragmentType = IrFragmentTypes.GeneratedCode,
                FragmentVersion = 2,
                Payload = CodegenManifestBuilder.BuildCodegenBuildValidatedPayload(projectId, sandbox, fragmentId),
                SkillId = DevelopmentSkillIds.Developer,
            }, ct);

            var archResult = await _archGuard.ScanAndPersistAsync(
                projectId,
                tenantId,
                snapshot,
                DevelopmentSkillIds.Developer,
                ct);

            if (archResult.CriticalCount > 0)
            {
                var first = archResult.CriticalViolations[0];
                throw new AbortSkillChainException(
                    $"[{first.RuleId}] {first.Message}",
                    "ArchAbort");
            }

            await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.CodeGeneratedStablePromoted,
                FragmentId = fragmentId,
                FragmentType = IrFragmentTypes.GeneratedCode,
                FragmentVersion = 3,
                Payload = CodegenManifestBuilder.BuildCodeGeneratedStablePromotedPayload(
                    projectId, sandbox, archResult, fragmentId),
                SkillId = DevelopmentSkillIds.Developer,
            }, ct);

            SkillRunResult testerResult;
            try
            {
                var archWarnings = archResult.WarningViolations
                    .Select(v => new SkillArchWarning
                    {
                        RuleId = v.RuleId,
                        Message = v.Message,
                        FilePath = v.FilePath,
                    })
                    .ToList();

                testerResult = await _harness.RunAsync(
                    DevelopmentSkillIds.Tester,
                    pipelineId,
                    tenantId,
                    projectId,
                    new SkillRunOptions
                    {
                        ProviderCode = options?.ProviderCode,
                        ArchGuardWarnings = archWarnings,
                    },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tester skill failed pipeline={PipelineId}", pipelineId);
                return new DeveloperOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = "tester-failed",
                    DeveloperSkillResult = devResult,
                    SandboxResult = sandbox,
                    ArchGuardResult = archResult,
                    ErrorMessage = ex.Message,
                };
            }

            _logger.LogInformation(
                "Developer orchestrator completed pipeline={PipelineId} scenarios={Scenarios} archWarnings={Warnings} elapsed={Ms}ms",
                pipelineId,
                testerResult.EventsAppended,
                archResult.WarningCount,
                sandbox.Elapsed.TotalMilliseconds);

            return new DeveloperOrchestratorResult
            {
                OrchestratorRunId = orchestratorRunId,
                Status = archResult.WarningCount > 0 ? "completed-with-warnings" : "completed",
                DeveloperSkillResult = devResult,
                TesterSkillResult = testerResult,
                SandboxResult = sandbox,
                ArchGuardResult = archResult,
            };
        }
        finally
        {
            projectLock.Release();
        }
    }

    public async Task<DeveloperOrchestratorStatus> GetStatusAsync(
        long pipelineId,
        string tenantId,
        string projectId,
        CancellationToken ct)
    {
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, ct);
        var ir3 = snapshots.FirstOrDefault(s => s.FragmentType == IrFragmentTypes.GeneratedCode);
        var designLocked = snapshots.Any(s =>
            s.FragmentType == IrFragmentTypes.SystemDesign
            && s.StabilityState == IrStabilityStates.Locked);

        var sandboxPassed = false;
        var archWarningCount = 0;
        if (ir3?.Payload is string payloadJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                if (doc.RootElement.TryGetProperty("sandboxBuild", out var sb)
                    && sb.TryGetProperty("passed", out var passed)
                    && passed.ValueKind == JsonValueKind.True)
                {
                    sandboxPassed = true;
                }

                if (doc.RootElement.TryGetProperty("promotionGate", out var gate)
                    && gate.TryGetProperty("archGuardWarnings", out var warnings)
                    && warnings.TryGetInt32(out var w))
                {
                    archWarningCount = w;
                }
            }
            catch
            {
                // ignore
            }
        }

        var codegenStable = ir3?.StabilityState == IrStabilityStates.Stable;

        return new DeveloperOrchestratorStatus
        {
            PipelineId = pipelineId,
            ProjectId = projectId,
            DesignLocked = designLocked,
            CodegenDraft = ir3 != null && ir3.StabilityState == IrStabilityStates.Draft,
            SandboxBuildPassed = sandboxPassed || codegenStable,
            ArchGuardPassed = codegenStable,
            ArchWarningCount = archWarningCount,
            CodegenStability = ir3?.StabilityState ?? IrStabilityStates.Draft,
        };
    }

    private async Task<IrSnapshot> LoadSnapshotAsync(string tenantId, string projectId, CancellationToken ct)
    {
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, ct);
        return new IrSnapshot
        {
            Fragments = snapshots.Select(s => new IrSnapshotFragment
            {
                FragmentId = s.FragmentId,
                FragmentType = s.FragmentType,
                StabilityState = s.StabilityState,
                Payload = s.Payload is string str ? str : JsonSerializer.Serialize(s.Payload, JsonOptions),
            }).ToList(),
        };
    }

    private async Task AppendCodegenFailedAsync(
        string projectId,
        string tenantId,
        string fragmentId,
        CodeSandboxBuildResult sandbox,
        CancellationToken ct)
    {
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.CodegenFailed,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.GeneratedCode,
            FragmentVersion = 2,
            Payload = CodegenManifestBuilder.BuildCodegenFailedPayload(projectId, sandbox, fragmentId),
            SkillId = DevelopmentSkillIds.Developer,
        }, ct);

        _logger.LogWarning(
            "CodegenFailed appended project={ProjectId} phase={Phase}",
            projectId,
            sandbox.Phase);
    }

    private async Task ValidatePreconditionsAsync(IrSnapshot snapshot, CancellationToken ct)
    {
        var gate = await _completenessGate.ValidateAsync(snapshot, ct);
        if (!gate.IsValid)
            throw Oops.Oh(gate.ErrorMessage ?? "SystemDesign 前置条件未满足")
                .StatusCode(StatusCodes.Status400BadRequest);
    }
}
