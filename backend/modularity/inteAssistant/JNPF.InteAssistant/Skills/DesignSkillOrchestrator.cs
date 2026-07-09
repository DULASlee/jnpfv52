using System.Collections.Concurrent;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Constraints;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 设计四 Skill 编排器（P3-B01）：AnalysisCompleted → 3 并行 + 1 串行。
/// </summary>
public interface IDesignSkillOrchestrator
{
    Task<DesignOrchestratorResult> RunAsync(
        long pipelineId, string tenantId, string projectId, DesignOrchestratorOptions? options, CancellationToken ct);
    Task<DesignOrchestratorStatus> GetStatusAsync(long pipelineId, string tenantId, string projectId, CancellationToken ct);
}

public sealed class DesignOrchestratorOptions
{
    public bool SkipSystemDesign { get; init; }
    public string? ProviderCode { get; init; }
}

public sealed class DesignOrchestratorResult
{
    public string OrchestratorRunId { get; init; } = string.Empty;
    public string Status { get; init; } = "completed";
    public IReadOnlyList<SkillRunResult> SkillResults { get; init; } = Array.Empty<SkillRunResult>();
    public string? ErrorMessage { get; init; }
}

public sealed class DesignOrchestratorStatus
{
    public long PipelineId { get; init; }
    public string ProjectId { get; init; } = string.Empty;
    public bool Ir1Stable { get; init; }
    public bool DesignComplete { get; init; }
    public IReadOnlyList<DesignSkillPhaseStatus> Phases { get; init; } = Array.Empty<DesignSkillPhaseStatus>();
    public long TokenConsumed { get; init; }
    public long TokenBudget { get; init; }
    public string BudgetStatus { get; init; } = "green";
    public int ConstraintCriticalCount { get; init; }
    public int ConstraintWarningCount { get; init; }
}

public sealed class DesignSkillPhaseStatus
{
    public string SkillId { get; init; } = string.Empty;
    public string Phase { get; init; } = "pending";
    public string? LastRunId { get; init; }
    public string? LastStatus { get; init; }
}

public sealed class DesignSkillOrchestrator : IDesignSkillOrchestrator, ITransient
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProjectLocks = new(StringComparer.Ordinal);

    private static readonly string[] ParallelSkills =
    {
        DesignSkillIds.Architect,
        DesignSkillIds.DbDesign,
        DesignSkillIds.UiDesign,
    };

    private readonly ISkillHarness _harness;
    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly IIrEventStoreService _eventStore;
    private readonly ISkillRegistry _registry;
    private readonly ISqlSugarClient _db;
    private readonly IConstraintEngineService _constraintEngine;
    private readonly ILogger<DesignSkillOrchestrator> _logger;

    public DesignSkillOrchestrator(
        ISkillHarness harness,
        ISkillLlmBudgetGuard budgetGuard,
        IIrEventStoreService eventStore,
        ISkillRegistry registry,
        ISqlSugarClient db,
        IConstraintEngineService constraintEngine,
        ILogger<DesignSkillOrchestrator> logger)
    {
        _harness = harness;
        _budgetGuard = budgetGuard;
        _eventStore = eventStore;
        _registry = registry;
        _db = db;
        _constraintEngine = constraintEngine;
        _logger = logger;
    }

    public async Task<DesignOrchestratorResult> RunAsync(
        long pipelineId, string tenantId, string projectId, DesignOrchestratorOptions? options, CancellationToken ct)
    {
        var orchestratorRunId = Guid.NewGuid().ToString("N");
        await ValidatePreconditionsAsync(pipelineId, tenantId, projectId, ct);
        await _budgetGuard.ValidateProjectBudgetAsync(projectId, tenantId, 0.95, ct);

        var projectLock = ProjectLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await projectLock.WaitAsync(ct);

        try
        {
            var parallelGate = new SemaphoreSlim(3, 3);
            var skillOptions = new SkillRunOptions { ProviderCode = options?.ProviderCode };
            var results = new List<SkillRunResult>();
            var errors = new List<string>();

            var parallelTasks = ParallelSkills.Select(async skillId =>
            {
                await parallelGate.WaitAsync(ct);
                try
                {
                    if (!_registry.TryGet(skillId, out _))
                    {
                        errors.Add($"Skill 未注册: {skillId}");
                        return;
                    }

                    var result = await _harness.RunAsync(skillId, pipelineId, tenantId, projectId, skillOptions, ct);
                    lock (results) results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Design parallel skill failed: {SkillId}", skillId);
                    lock (errors) errors.Add($"{skillId}: {ex.Message}");
                }
                finally
                {
                    parallelGate.Release();
                }
            }).ToList();

            await Task.WhenAll(parallelTasks);

            if (errors.Count > 0)
            {
                return new DesignOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = "partial-failed",
                    SkillResults = results,
                    ErrorMessage = string.Join("; ", errors),
                };
            }

            var constraintCheck = await _constraintEngine.CheckProjectAsync(
                projectId, tenantId, persistReport: true, "design-orchestrator", ct);
            if (constraintCheck.CriticalCount > 0)
            {
                _logger.LogWarning(
                    "Design orchestrator blocked by {Count} critical constraint violations",
                    constraintCheck.CriticalCount);

                return new DesignOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = "constraint-blocked",
                    SkillResults = results,
                    ErrorMessage = $"存在 {constraintCheck.CriticalCount} 条 critical 约束违规，SystemDesign 未执行",
                };
            }

            if (options?.SkipSystemDesign != true)
            {
                if (_registry.TryGet(DesignSkillIds.SystemDesign, out _))
                {
                    var sysResult = await _harness.RunAsync(
                        DesignSkillIds.SystemDesign, pipelineId, tenantId, projectId, skillOptions, ct);
                    results.Add(sysResult);

                    await _db.Updateable<AiProjectEntity>()
                        .SetColumns(x => new AiProjectEntity
                        {
                            CurrentPhase = "design-complete",
                            DesignCompletedAt = DateTime.UtcNow,
                            LastModifyTime = DateTime.UtcNow,
                        })
                        .Where(x => x.Id == projectId && x.TenantId == tenantId)
                        .ExecuteCommandAsync(ct);
                }
            }

            return new DesignOrchestratorResult
            {
                OrchestratorRunId = orchestratorRunId,
                Status = "completed",
                SkillResults = results,
            };
        }
        finally
        {
            projectLock.Release();
        }
    }

    public async Task<DesignOrchestratorStatus> GetStatusAsync(
        long pipelineId, string tenantId, string projectId, CancellationToken ct)
    {
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        var ir1Stable = snapshots.Any(s =>
            s.FragmentType == IrFragmentTypes.EventSpec
            && s.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked);

        var designComplete = snapshots.Any(s =>
            s.FragmentType == IrFragmentTypes.SystemDesign
            && s.StabilityState == IrStabilityStates.Locked);

        var runs = await _db.Queryable<AiSkillRunEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .OrderByDescending(x => x.StartedAt)
            .Take(20)
            .ToListAsync(ct);

        var allSkillIds = ParallelSkills.Append(DesignSkillIds.SystemDesign).ToArray();
        var phases = allSkillIds.Select(skillId =>
        {
            var last = runs.FirstOrDefault(r => r.SkillId == skillId);
            var fragmentType = skillId switch
            {
                DesignSkillIds.Architect => IrFragmentTypes.Architecture,
                DesignSkillIds.DbDesign => IrFragmentTypes.DDL,
                DesignSkillIds.UiDesign => IrFragmentTypes.FormPageIR,
                DesignSkillIds.SystemDesign => IrFragmentTypes.SystemDesign,
                _ => null,
            };

            var fragmentStable = fragmentType != null && snapshots.Any(s =>
                s.FragmentType == fragmentType
                && s.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked);

            var phase = last?.Status switch
            {
                "running" => "running",
                "failed" => "failed",
                "completed" when fragmentStable => "stable",
                "completed" => "completed",
                _ => fragmentStable ? "stable" : "pending",
            };

            return new DesignSkillPhaseStatus
            {
                SkillId = skillId,
                Phase = phase,
                LastRunId = last?.Id,
                LastStatus = last?.Status,
            };
        }).ToList();

        var project = await _db.Queryable<AiProjectEntity>()
            .FirstAsync(x => x.Id == projectId && x.TenantId == tenantId, ct);

        var constraintSnapshot = BuildIrSnapshot(snapshots);
        var constraintResult = _constraintEngine.Evaluate(constraintSnapshot);

        return new DesignOrchestratorStatus
        {
            PipelineId = pipelineId,
            ProjectId = projectId,
            Ir1Stable = ir1Stable,
            DesignComplete = designComplete,
            Phases = phases,
            TokenConsumed = project?.TokenConsumed ?? 0,
            TokenBudget = project?.TokenBudget ?? 500_000,
            BudgetStatus = project?.LlmBudgetStatus ?? "green",
            ConstraintCriticalCount = constraintResult.CriticalCount,
            ConstraintWarningCount = constraintResult.WarningCount,
        };
    }

    private static IrSnapshot BuildIrSnapshot(IReadOnlyList<Entitys.Dto.Ir.IrFragmentSnapshotDto> dtos)
    {
        var fragments = dtos.Select(d => new IrSnapshotFragment
        {
            FragmentId = d.FragmentId,
            FragmentType = d.FragmentType,
            StabilityState = d.StabilityState,
            Payload = d.Payload is string s ? s : System.Text.Json.JsonSerializer.Serialize(d.Payload),
            SaStepsCompleted = d.SaStepsCompleted ?? Array.Empty<string>(),
        }).ToList();
        return new IrSnapshot { Fragments = fragments };
    }

    private async Task ValidatePreconditionsAsync(long pipelineId, string tenantId, string projectId, CancellationToken ct)
    {
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        var hasAnalysis = snapshots.Any(s =>
            s.FragmentType == IrFragmentTypes.EventSpec
            && s.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked);

        if (!hasAnalysis)
            throw Oops.Oh("IR-1 未 stable，请先完成 Analyst Skill")
                .StatusCode(StatusCodes.Status400BadRequest);
    }
}
