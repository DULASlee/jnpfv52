using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills.Bugfix;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

public interface IBugfixSkillOrchestrator
{
    Task<BugfixOrchestratorResult> RunAsync(
        long pipelineId,
        string tenantId,
        string projectId,
        BugfixRunContext bugfix,
        CancellationToken ct = default);
}

public sealed class BugfixOrchestratorResult
{
    public string OrchestratorRunId { get; init; } = string.Empty;
    public string Status { get; init; } = "completed";
    public IrDiffResult? Diff { get; init; }
    public BugfixRerunPlan? RerunPlan { get; init; }
    public IReadOnlyList<string> ExecutedSteps { get; init; } = Array.Empty<string>();
    public string? BugFixedEventId { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 阶段五 P5-B02 主链 — diff → 标记 → **增量重算** → 产出物核查 → BugFixed。
/// 对齐文档 13 §2：禁止只 append 事件不跑 Skill。
/// </summary>
public sealed class BugfixSkillOrchestrator : IBugfixSkillOrchestrator, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IIrDiffEngine _diffEngine;
    private readonly IIrEventStoreService _eventStore;
    private readonly ISkillHarness _harness;
    private readonly IDeveloperSkillOrchestrator _developerOrchestrator;
    private readonly IAnalystAffectedStepsRerunService _analystRerun;
    private readonly ISkillExecutionLogger _skillLogger;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<BugfixSkillOrchestrator> _logger;

    public BugfixSkillOrchestrator(
        IIrDiffEngine diffEngine,
        IIrEventStoreService eventStore,
        ISkillHarness harness,
        IDeveloperSkillOrchestrator developerOrchestrator,
        IAnalystAffectedStepsRerunService analystRerun,
        ISkillExecutionLogger skillLogger,
        ISqlSugarClient db,
        ILogger<BugfixSkillOrchestrator> logger)
    {
        _diffEngine = diffEngine;
        _eventStore = eventStore;
        _harness = harness;
        _developerOrchestrator = developerOrchestrator;
        _analystRerun = analystRerun;
        _skillLogger = skillLogger;
        _db = db;
        _logger = logger;
    }

    public async Task<BugfixOrchestratorResult> RunAsync(
        long pipelineId,
        string tenantId,
        string projectId,
        BugfixRunContext bugfix,
        CancellationToken ct = default)
    {
        if (bugfix.FromSequence >= bugfix.ToSequence)
            throw Oops.Bah("fromSequence 须小于 toSequence")
                .StatusCode(StatusCodes.Status400BadRequest);

        var orchestratorRunId = Guid.NewGuid().ToString("N");
        var sw = Stopwatch.StartNew();
        var bugFragmentId = $"bugfix:{projectId}";

        using var logScope = _skillLogger.BeginScope(
            orchestratorRunId, tenantId, projectId, pipelineId, BugfixSkillIds.Bugfix);

        var snapshotsBefore = await _eventStore.ListSnapshotsAsync(projectId, tenantId, ct);
        var payloadHashesBefore = BuildPayloadHashMap(snapshotsBefore);

        _skillLogger.LogPhase("DiffCompute", "start", sw.ElapsedMilliseconds);

        var diff = await _diffEngine.CompareAsync(
            projectId,
            tenantId,
            bugfix.FromSequence,
            bugfix.ToSequence,
            new IrDiffOptions
            {
                ForceUnlock = bugfix.ForceUnlock,
                PropagateDownstream = true,
            },
            ct);

        if (diff.IsEmpty)
        {
            throw Oops.Bah("Bugfix 空 diff：无受影响片段，拒绝 rerun 与 BugFixed")
                .StatusCode(StatusCodes.Status400BadRequest);
        }

        _skillLogger.LogPhase("DiffCompute", "ok", sw.ElapsedMilliseconds,
            message: $"changed={diff.Changed.Count} invalidated={diff.Invalidated.Count}");

        var fragmentMap = snapshotsBefore.ToDictionary(
            s => s.FragmentId,
            s => s.FragmentType,
            StringComparer.Ordinal);

        var rootCauseLayer = BugfixRootCauseClassifier.Classify(
            diff,
            fragmentMap,
            bugfix.RootCauseLayer);

        _skillLogger.LogPhase("RootCauseLocate", rootCauseLayer, sw.ElapsedMilliseconds);

        var plan = BugfixRerunPlanner.Build(
            diff,
            rootCauseLayer,
            fragmentMap,
            bugfix.RevisionType);

        _skillLogger.LogPhase("RerunPlan", string.Join(',', plan.Steps.Select(s => s.SkillId)), sw.ElapsedMilliseconds);

        if (!string.IsNullOrWhiteSpace(bugfix.Description))
        {
            await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.BugReported,
                FragmentId = bugFragmentId,
                FragmentType = IrFragmentTypes.EventSpec,
                FragmentVersion = 1,
                Payload = BugfixManifestBuilder.BuildBugReportedPayload(
                    projectId, orchestratorRunId, bugfix.Description),
                SkillId = BugfixSkillIds.Bugfix,
            }, ct);
        }

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.BugRootCauseLocated,
            FragmentId = bugFragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = 2,
            Payload = BugfixManifestBuilder.BuildBugRootCauseLocatedPayload(
                projectId, orchestratorRunId, rootCauseLayer, bugfix.RevisionType, diff),
            SkillId = BugfixSkillIds.Bugfix,
        }, ct);

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.AffectedFragmentsMarked,
            FragmentId = bugFragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = 3,
            Payload = BugfixManifestBuilder.BuildAffectedFragmentsMarkedPayload(
                projectId, orchestratorRunId, diff),
            SkillId = BugfixSkillIds.Bugfix,
        }, ct);

        var executed = new List<string>();
        var skillOptions = new SkillRunOptions { ProviderCode = null };

        foreach (var step in plan.Steps)
        {
            ct.ThrowIfCancellationRequested();
            _skillLogger.LogPhase("RerunExecute", step.SkillId, sw.ElapsedMilliseconds);

            switch (step.Kind)
            {
                case BugfixRerunPlanner.StepDeveloperOrchestrator:
                {
                    var devResult = await _developerOrchestrator.RunAsync(
                        pipelineId, tenantId, projectId,
                        new DeveloperOrchestratorOptions(),
                        ct);
                    if (devResult.Status is "developer-failed" or "codegen-failed" or "tester-failed")
                    {
                        throw Oops.Oh(devResult.ErrorMessage ?? $"Developer 编排失败: {devResult.Status}")
                            .StatusCode(StatusCodes.Status500InternalServerError);
                    }

                    executed.Add(step.SkillId);
                    break;
                }
                case BugfixRerunPlanner.StepDesignSkill:
                {
                    await _harness.RunAsync(
                        step.SkillId, pipelineId, tenantId, projectId, skillOptions, ct);
                    executed.Add(step.SkillId);
                    break;
                }
                case BugfixRerunPlanner.StepAnalystRerun:
                {
                    await _analystRerun.RunAsync(
                        tenantId,
                        projectId,
                        pipelineId,
                        "BE-001",
                        step.AnalystInput,
                        ct);
                    executed.Add(step.SkillId);
                    break;
                }
                default:
                    throw Oops.Oh($"未知 Bugfix rerun step: {step.Kind}");
            }
        }

        var snapshotsAfter = await _eventStore.ListSnapshotsAsync(projectId, tenantId, ct);
        VerifyPreservedDeliverables(payloadHashesBefore, snapshotsAfter, plan.PreservedFragmentTypes);

        var bugFixedEvt = await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.BugFixed,
            FragmentId = bugFragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = 4,
            Payload = BugfixManifestBuilder.BuildBugFixedPayload(projectId, orchestratorRunId, diff),
            SkillId = BugfixSkillIds.Bugfix,
        }, ct);

        // 记录最近 bugfix 时间（阶段五 P5-B02 DDL）
        await _db.Updateable<AiProjectEntity>()
            .SetColumns(x => x.LastBugfixAt == DateTime.UtcNow)
            .Where(x => x.Id == projectId)
            .ExecuteCommandAsync(ct);

        _logger.LogInformation(
            "Bugfix orchestrator completed pipeline={PipelineId} steps=[{Steps}] elapsed={Ms}ms",
            pipelineId,
            string.Join(',', executed),
            sw.ElapsedMilliseconds);

        return new BugfixOrchestratorResult
        {
            OrchestratorRunId = orchestratorRunId,
            Status = "completed",
            Diff = diff,
            RerunPlan = plan,
            ExecutedSteps = executed,
            BugFixedEventId = bugFixedEvt.Id,
        };
    }

    private static Dictionary<string, string> BuildPayloadHashMap(
        IReadOnlyList<IrFragmentSnapshotDto> snapshots)
    {
        return snapshots.ToDictionary(
            s => s.FragmentType,
            s => HashPayload(s.Payload),
            StringComparer.Ordinal);
    }

    private static void VerifyPreservedDeliverables(
        IReadOnlyDictionary<string, string> hashesBefore,
        IReadOnlyList<IrFragmentSnapshotDto> snapshotsAfter,
        IReadOnlySet<string> preservedTypes)
    {
        foreach (var fragmentType in preservedTypes)
        {
            if (!hashesBefore.TryGetValue(fragmentType, out var beforeHash))
                continue;

            var after = snapshotsAfter.FirstOrDefault(s => s.FragmentType == fragmentType);
            if (after == null)
                continue;

            var afterHash = HashPayload(after.Payload);
            if (!string.Equals(beforeHash, afterHash, StringComparison.Ordinal))
            {
                throw Oops.Bah(
                    $"Bugfix D3 违规：{fragmentType} 快照在重算后发生变化（应 preserved）");
            }
        }
    }

    private static string HashPayload(object? payload)
    {
        var json = payload switch
        {
            null => "{}",
            string s => s,
            _ => JsonSerializer.Serialize(payload, JsonOptions),
        };
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
