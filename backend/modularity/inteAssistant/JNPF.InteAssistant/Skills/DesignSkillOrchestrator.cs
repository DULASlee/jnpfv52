using System.Collections.Concurrent;
using System.Text.Json;
using JNPF;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Constraints;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
    /// <summary>历史字段：EventSpec stable。设计启动以 <see cref="AnalysisFinalized"/> 为准。</summary>
    public bool Ir1Stable { get; init; }
    /// <summary>AnalysisCompleted.finalized=true（25 §6 / Round 3 工程保障完成）。</summary>
    public bool AnalysisFinalized { get; init; }
    /// <summary>ai_entity_field 三元组下字段行数 &gt; 0。</summary>
    public bool HasEntityFields { get; init; }
    public int EntityFieldCount { get; init; }
    /// <summary>可启动设计：finalized ∧ 有实体字段 ∧ 质量门控（有评分则 PassesGate；无评分仅拦 CRITICAL）。</summary>
    public bool CanRunDesign { get; init; }
    /// <summary>是否已有 sa_quality_score 行。</summary>
    public bool HasQualityScore { get; init; }
    public decimal? QualityTotalScore { get; init; }
    public int QualityCriticalCount { get; init; }
    public bool QualityGatePasses { get; init; }
    /// <summary>PM 终评 ≥85 / forceConfirm / 或 S2 StageConfirmed。</summary>
    public bool PmReviewGatePasses { get; init; }
    public int? PmReviewScore { get; init; }
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
    private readonly EntityDesignRepository _entityDesignRepo;
    private readonly IUserRequirementLoader _requirementLoader;
    private readonly ILogger<DesignSkillOrchestrator> _logger;

    public DesignSkillOrchestrator(
        ISkillHarness harness,
        ISkillLlmBudgetGuard budgetGuard,
        IIrEventStoreService eventStore,
        ISkillRegistry registry,
        ISqlSugarClient db,
        IConstraintEngineService constraintEngine,
        EntityDesignRepository entityDesignRepo,
        IUserRequirementLoader requirementLoader,
        ILogger<DesignSkillOrchestrator> logger)
    {
        _harness = harness;
        _budgetGuard = budgetGuard;
        _eventStore = eventStore;
        _registry = registry;
        _db = db;
        _constraintEngine = constraintEngine;
        _entityDesignRepo = entityDesignRepo;
        _requirementLoader = requirementLoader;
        _logger = logger;
    }

    public async Task<DesignOrchestratorResult> RunAsync(
        long pipelineId, string tenantId, string projectId, DesignOrchestratorOptions? options, CancellationToken ct)
    {
        var orchestratorRunId = Guid.NewGuid().ToString("N");
        await ValidatePreconditionsAsync(pipelineId, tenantId, projectId, ct);
        await _budgetGuard.ValidateProjectBudgetAsync(projectId, tenantId, 0.95, ct);

        var userRequirement = await _requirementLoader.LoadAsync(tenantId, projectId, pipelineId, ct);
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString(), ct);

        if (IsDesignComplete(snapshots))
        {
            _logger.LogInformation(
                "Design orchestrator skipped: SystemDesign already locked pipeline={PipelineId}",
                pipelineId);
            return new DesignOrchestratorResult
            {
                OrchestratorRunId = orchestratorRunId,
                Status = "completed",
                SkillResults = Array.Empty<SkillRunResult>(),
            };
        }

        var projectLock = ProjectLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await projectLock.WaitAsync(ct);

        try
        {
            var parallelGate = new SemaphoreSlim(3, 3);
            var skillOptions = new SkillRunOptions
            {
                ProviderCode = options?.ProviderCode,
                UserRequirement = userRequirement,
            };
            var results = new List<SkillRunResult>();
            var errors = new List<string>();

            var parallelTasks = ParallelSkills.Select(async skillId =>
            {
                await parallelGate.WaitAsync(ct);
                try
                {
                    if (!_registry.TryGet(skillId, out _))
                    {
                        lock (errors) errors.Add($"Skill 未注册: {skillId}");
                        return;
                    }

                    var fragmentType = MapSkillToFragmentType(skillId);
                    if (fragmentType != null && IsFragmentStable(snapshots, fragmentType))
                    {
                        _logger.LogInformation(
                            "Design parallel skill skipped (fragment stable): {SkillId} pipeline={PipelineId}",
                            skillId, pipelineId);
                        lock (results) results.Add(new SkillRunResult
                        {
                            SkillId = skillId,
                            Status = "skipped",
                        });
                        return;
                    }

                    // 并行 Skill 各用独立 DI 作用域（同 scope 并发共享 SkillHarness/IrEventStore → 连接关闭）
                    using var skillScope = App.RootServices.CreateScope();
                    var harness = skillScope.ServiceProvider.GetRequiredService<ISkillHarness>();
                    var result = await harness.RunAsync(skillId, pipelineId, tenantId, projectId, skillOptions, ct);
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
                    SkillRunResult sysResult;
                    if (IsFragmentStable(snapshots, IrFragmentTypes.SystemDesign))
                    {
                        _logger.LogInformation(
                            "SystemDesign skill skipped (fragment locked): pipeline={PipelineId}",
                            pipelineId);
                        sysResult = new SkillRunResult
                        {
                            SkillId = DesignSkillIds.SystemDesign,
                            Status = "skipped",
                        };
                    }
                    else
                    {
                        sysResult = await _harness.RunAsync(
                            DesignSkillIds.SystemDesign, pipelineId, tenantId, projectId, skillOptions, ct);
                    }
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

        var analysisFinalized = await AnalysisFinalizedGate.HasFinalizedAsync(
            _eventStore, tenantId, projectId, pipelineId, ct);
        var entityFieldCount = await _entityDesignRepo.CountFieldsAsync(
            tenantId, projectId, pipelineId.ToString(), ct);
        var hasEntityFields = entityFieldCount > 0;
        var quality = await QualityDesignGate.EvaluateAsync(
            _db, tenantId, projectId, pipelineId.ToString(), ct);
        var pmReview = await EvaluatePmReviewGateAsync(tenantId, projectId, pipelineId, ct);
        var pmGatePasses = pmReview.Passes;
        var canRunDesign = analysisFinalized && hasEntityFields && quality.Passes && pmGatePasses;

        var designComplete = snapshots.Any(s =>
            s.FragmentType == IrFragmentTypes.SystemDesign
            && s.StabilityState == IrStabilityStates.Locked);

        var runs = await _db.Queryable<AiSkillRunEntity>()
            .Where(x => x.ProjectId == projectId
                && x.TenantId == tenantId
                && x.PipelineId == pipelineId.ToString())
            .OrderByDescending(x => x.StartedAt)
            .Take(20)
            .ToListAsync(ct);

        var allSkillIds = ParallelSkills.Append(DesignSkillIds.SystemDesign).ToArray();
        var phases = allSkillIds.Select(skillId =>
        {
            var last = runs.FirstOrDefault(r => r.SkillId == skillId);
            var fragmentType = MapSkillToFragmentType(skillId);

            var fragmentStable = fragmentType != null && IsFragmentStable(snapshots, fragmentType);

            var phase = fragmentStable
                ? "stable"
                : last?.Status switch
            {
                "running" => "running",
                "failed" => "failed",
                "completed" => "completed",
                _ => "pending",
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
            AnalysisFinalized = analysisFinalized,
            HasEntityFields = hasEntityFields,
            EntityFieldCount = entityFieldCount,
            CanRunDesign = canRunDesign,
            HasQualityScore = quality.HasScore,
            QualityTotalScore = quality.TotalScore,
            QualityCriticalCount = quality.CriticalCount,
            QualityGatePasses = quality.Passes,
            PmReviewGatePasses = pmGatePasses,
            PmReviewScore = pmReview.Score,
            DesignComplete = designComplete,
            Phases = phases,
            TokenConsumed = project?.TokenConsumed ?? 0,
            TokenBudget = project?.TokenBudget ?? LlmBudgetDefaults.DefaultProjectTokenBudget,
            BudgetStatus = project?.LlmBudgetStatus ?? "green",
            ConstraintCriticalCount = constraintResult.CriticalCount,
            ConstraintWarningCount = constraintResult.WarningCount,
        };
    }

    private sealed record PmReviewGateSnapshot(bool Passes, int? Score);

    /// <summary>
    /// PM 终评门禁：≥85 / forceConfirm / 或 S2 StageConfirmed（新 PM 主链用户已确认说明书并完成 Finalize）。
    /// </summary>
    private async Task<PmReviewGateSnapshot> EvaluatePmReviewGateAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        int? latestScore = null;
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        foreach (var evt in events)
        {
            if (string.Equals(evt.EventType, IrEventTypes.StageConfirmed, StringComparison.Ordinal))
            {
                try
                {
                    using var doc = JsonDocument.Parse(evt.PayloadPreview);
                    if (doc.RootElement.TryGetProperty("stage", out var stage)
                        && string.Equals(stage.GetString(), "S2", StringComparison.OrdinalIgnoreCase))
                    {
                        // 新 PM 主链：用户确认说明书 + Finalize 后投 StageConfirmed(S2) → 设计准入
                        return new PmReviewGateSnapshot(true, latestScore);
                    }
                }
                catch (JsonException)
                {
                    continue;
                }
            }

            if (string.Equals(evt.EventType, IrEventTypes.RequirementSpecPmReviewed, StringComparison.Ordinal))
            {
                try
                {
                    using var doc = JsonDocument.Parse(evt.PayloadPreview);
                    if (doc.RootElement.TryGetProperty("score", out var scoreEl)
                        && scoreEl.TryGetInt32(out var value))
                    {
                        latestScore = value;
                        if (value >= 85)
                            return new PmReviewGateSnapshot(true, value);
                    }
                    if (doc.RootElement.TryGetProperty("forceConfirm", out var reviewForce)
                        && reviewForce.ValueKind == JsonValueKind.True)
                        return new PmReviewGateSnapshot(true, latestScore);
                }
                catch (JsonException)
                {
                    continue;
                }
            }
        }

        return new PmReviewGateSnapshot(false, latestScore);
    }

    private static string? MapSkillToFragmentType(string skillId) => skillId switch
    {
        DesignSkillIds.Architect => IrFragmentTypes.Architecture,
        DesignSkillIds.DbDesign => IrFragmentTypes.DDL,
        DesignSkillIds.UiDesign => IrFragmentTypes.FormPageIR,
        DesignSkillIds.SystemDesign => IrFragmentTypes.SystemDesign,
        _ => null,
    };

    private static bool IsFragmentStable(
        IReadOnlyList<Entitys.Dto.Ir.IrFragmentSnapshotDto> snapshots, string fragmentType)
        => snapshots.Any(s =>
            s.FragmentType == fragmentType
            && s.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked);

    private static bool IsDesignComplete(IReadOnlyList<Entitys.Dto.Ir.IrFragmentSnapshotDto> snapshots)
        => snapshots.Any(s =>
            s.FragmentType == IrFragmentTypes.SystemDesign
            && s.StabilityState == IrStabilityStates.Locked);

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
        // 25 §6：设计启动以 Finalize 为准，不以「EventSpec stable」替代
        var finalized = await AnalysisFinalizedGate.HasFinalizedAsync(
            _eventStore, tenantId, projectId, pipelineId, ct);
        if (!finalized)
            throw Oops.Bah(AnalysisFinalizedGate.NotFinalizedMessage)
                .StatusCode(StatusCodes.Status400BadRequest);

        var fieldCount = await _entityDesignRepo.CountFieldsAsync(
            tenantId, projectId, pipelineId.ToString(), ct);
        if (fieldCount <= 0)
            throw Oops.Bah("ai_entity_field 无投影字段，请先完成 Round 3 工程保障（EntityDesignProjector）")
                .StatusCode(StatusCodes.Status400BadRequest);

        await QualityDesignGate.EnsureCanRunDesignAsync(
            _db, tenantId, projectId, pipelineId.ToString(), _logger, ct);
    }
}
