using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 分析师 Skill — S2 compile 模式走 SaNineViewCompiler；agent 模式走 sa-service run-async（回归对比）。
/// </summary>
public sealed class AnalystSkillService : CognitiveSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISaOrchestratorAdapter _saAdapter;
    private readonly ISaNineViewCompiler _compiler;
    private readonly SaPipelineOptions _pipelineOptions;
    private readonly IAnalysisCompletedCompletenessGate _completenessGate;
    private readonly IIrEventStoreService _eventStore;
    private readonly IExperienceRecorder _experience;
    private readonly ILogger<AnalystSkillService> _logger;

    public AnalystSkillService(
        ICognitiveSkillToolkit toolkit,
        ISaOrchestratorAdapter saAdapter,
        ISaNineViewCompiler compiler,
        IOptions<SaPipelineOptions> pipelineOptions,
        IAnalysisCompletedCompletenessGate completenessGate,
        IIrEventStoreService eventStore,
        IExperienceRecorder experience,
        ILogger<AnalystSkillService> logger)
        : base(toolkit)
    {
        _saAdapter = saAdapter;
        _compiler = compiler;
        _pipelineOptions = pipelineOptions.Value;
        _completenessGate = completenessGate;
        _eventStore = eventStore;
        _experience = experience;
        _logger = logger;
    }

    public override string SkillId => "analyst-skill";
    public override string Version => "2.0.0-cognitive";
    public override SkillLayer Layer => SkillLayer.Refinement;
    public override SkillMission Mission => SkillMission.RefineSpecification;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[] { IrFragmentTypes.Skeleton },
        RequiredStability = IrStabilityStates.Stable,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.SaStepCompleted,
            IrEventTypes.EventSpecConfirmed,
            IrEventTypes.AnalysisCompleted,
        },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (skeleton == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-0 骨架未 stable，请先确认骨架"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (!events.Any(e => e.EventType == IrEventTypes.AnalysisCompleted))
            return Task.FromResult(SkillValidationResult.Fail("缺少 AnalysisCompleted 事件"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var skeleton = context.Snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)!;
        var businessEvents = ParseBusinessEvents(skeleton.Payload);
        if (businessEvents.Count == 0)
            throw Oops.Bah("IR-0 无 businessEvents，无法启动分析师 Skill");

        // 断点续跑：读取 Snapshot 中已完成的事件，跳过无需重跑的
        var alreadyConfirmed = context.Snapshot.Fragments
            .Where(f => f.FragmentType == IrFragmentTypes.EventSpec
                     && f.StabilityState == IrStabilityStates.Stable
                     && !string.IsNullOrEmpty(f.FragmentId))
            .Select(f => f.FragmentId!.Replace("eventspec:", ""))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pendingEvents = businessEvents
            .Where(e => !alreadyConfirmed.Contains(e.EventId))
            .ToList();

        _logger.LogInformation(
            "AnalystSkill: 共 {Total} 个事件，{Pending} 个待分析，{Done} 个已完成（断点续跑）",
            businessEvents.Count, pendingEvents.Count, alreadyConfirmed.Count);

        if (pendingEvents.Count > 0)
        {
            SaProjectResult saResult;
            SaNineViewCompileResult? compileResult = null;

            if (_pipelineOptions.IsCompileMode)
            {
                compileResult = _compiler.CompileFromSkeletonJson(
                    skeleton.Payload,
                    context.UserRequirement ?? skeleton.Payload);

                _logger.LogInformation(
                    "SaNineViewCompiler 完成：{EventCount} 事件，{Duration}ms hash={Hash}",
                    compileResult.EventResults.Count, compileResult.CompileDurationMs, compileResult.BundleHash);

                yield return new AppendIrEventRequest
                {
                    EventType = IrEventTypes.SaNineViewCompiled,
                    Payload = JsonSerializer.Serialize(new
                    {
                        tenantId = context.TenantId,
                        projectId = context.ProjectId,
                        pipelineId = context.PipelineId,
                        bundleHash = compileResult.BundleHash,
                        compileMs = compileResult.CompileDurationMs,
                        eventCount = compileResult.EventResults.Count,
                        bundle = new
                        {
                            projectSteps = compileResult.ProjectSteps,
                            eventResults = compileResult.EventResults,
                            compileDurationMs = compileResult.CompileDurationMs,
                            bundleHash = compileResult.BundleHash,
                        },
                    }, JsonOptions),
                    SkillId = SkillId,
                };

                saResult = compileResult.ToProjectResult();
            }
            else
            {
                saResult = await _saAdapter.RunProjectAsync(
                    context.TenantId, context.ProjectId, context.PipelineId,
                    context.UserRequirement ?? skeleton.Payload,
                    businessEvents.Select(e => new SaSkeletonEventInput(e.EventId, e.EventName, e.ComplexityHint)).ToList(),
                    context.RunId, ct);

                _logger.LogInformation(
                    "SA agent 完成：{EventCount} 个事件，耗时 {Duration}ms",
                    saResult.EventResults.Count, saResult.TotalDurationMs);
            }

            foreach (var eventResult in saResult.EventResults)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var (stepName, stepOutput) in eventResult.Steps)
                {
                    yield return new AppendIrEventRequest
                    {
                        EventType = IrEventTypes.SaStepCompleted,
                        FragmentId = $"eventspec:{eventResult.EventId}",
                        FragmentType = IrFragmentTypes.EventSpec,
                        Payload = JsonSerializer.Serialize(new
                        {
                            eventId = eventResult.EventId,
                            step = stepName,
                            output = stepOutput,
                            source = _pipelineOptions.IsCompileMode ? "SaNineViewCompiler" : "sa-service",
                        }, JsonOptions),
                        SkillId = SkillId,
                        SaStepName = stepName,
                    };
                }

                if (string.IsNullOrEmpty(eventResult.Error))
                {
                    var meta = pendingEvents.FirstOrDefault(e => e.EventId == eventResult.EventId)
                            ?? new BusinessEventMeta(eventResult.EventId, eventResult.EventName, eventResult.Complexity);

                    var steps = eventResult.Steps.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value,
                        StringComparer.Ordinal);

                    var eventSpecPayload = EventSpecAssembler.BuildPayloadJson(eventResult.EventId, meta, steps);
                    await ValidateEventSpecViaIoiAsync(eventSpecPayload, ct);

                    yield return new AppendIrEventRequest
                    {
                        EventType = IrEventTypes.EventSpecConfirmed,
                        FragmentId = $"eventspec:{eventResult.EventId}",
                        FragmentType = IrFragmentTypes.EventSpec,
                        Payload = eventSpecPayload,
                    };
                }
                else
                {
                    _logger.LogWarning("事件 {EventId} SA 失败（{Error}），跳过 EventSpecConfirmed",
                        eventResult.EventId, eventResult.Error);
                }
            }

            if (_pipelineOptions.IsCompileMode)
            {
                await RecordS2DualReviewAsync(context, compileResult!, businessEvents.Count, ct);
            }
        }

        // 完整性门禁
        var freshSnapshot = await BuildSnapshotAsync(context.TenantId, context.ProjectId, ct);
        var gate = await _completenessGate.ValidateAsync(
            context.TenantId, context.ProjectId, freshSnapshot, context.RunId, ct);
        if (!gate.IsValid)
            throw Oops.Bah(gate.ErrorMessage ?? "AnalysisCompleted 完整性门禁未通过");

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.AnalysisCompleted,
            Payload = JsonSerializer.Serialize(new
            {
                tenantId = context.TenantId,
                projectId = context.ProjectId,
                pipelineId = context.PipelineId,
                eventSpecCount = businessEvents.Count,
                s2Mode = _pipelineOptions.S2Mode,
                allStable = true,
            }, JsonOptions),
        };
    }

    private async Task RecordS2DualReviewAsync(
        SkillContext context,
        SaNineViewCompileResult compileResult,
        int skeletonEventCount,
        CancellationToken ct)
    {
        if (compileResult.EventResults.Count != skeletonEventCount)
            throw Oops.Bah($"Compiler 事件数 {compileResult.EventResults.Count} 与骨架 {skeletonEventCount} 不一致");

        var pmDetail = JsonSerializer.Serialize(new
        {
            source = "compile-dual-review",
            pipelineId = context.PipelineId,
            bundleHash = compileResult.BundleHash,
            eventCount = compileResult.EventResults.Count,
        }, JsonOptions);

        await _experience.RecordReviewAsync(
            context.ProjectId, context.TenantId, "pm-skill", context.RunId,
            "pm-s2-pass", pmDetail, ct);

        await _experience.RecordReviewAsync(
            context.ProjectId, context.TenantId, SkillId, context.RunId,
            "analyst-s2-pass", pmDetail, ct);
    }

    private async Task ValidateEventSpecViaIoiAsync(string eventSpecPayload, CancellationToken ct)
    {
        var args = JsonSerializer.Serialize(new { eventSpecPayload }, JsonOptions);
        var result = await Mcp.CallToolAsync("ioi.validate", args, ct);
        if (!result.IsSuccess)
            throw Oops.Bah($"ioi.validate 工具失败: {result.Error}");

        using var doc = JsonDocument.Parse(result.ContentJson);
        if (doc.RootElement.TryGetProperty("valid", out var validEl)
            && validEl.ValueKind == JsonValueKind.False)
        {
            var reason = doc.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : null;
            throw Oops.Bah(reason ?? "EventSpec IOI 不变量校验失败");
        }

        if (!doc.RootElement.TryGetProperty("valid", out var okEl) || !okEl.GetBoolean())
            throw Oops.Bah("EventSpec IOI 不变量校验失败");
    }

    private async Task<IrSnapshot> BuildSnapshotAsync(string tenantId, string projectId, CancellationToken ct)
    {
        var dtos = await _eventStore.ListSnapshotsAsync(projectId, tenantId, ct);
        return new IrSnapshot
        {
            Fragments = dtos.Select(d => new IrSnapshotFragment
            {
                FragmentId = d.FragmentId,
                FragmentType = d.FragmentType,
                StabilityState = d.StabilityState,
                Payload = d.Payload is string s ? s : JsonSerializer.Serialize(d.Payload, JsonOptions),
                SaStepsCompleted = d.SaStepsCompleted ?? Array.Empty<string>(),
            }).ToList(),
        };
    }

    private static string BuildEventSpecPayload(
        string eventId, BusinessEventMeta meta, IReadOnlyDictionary<string, object> previousSteps)
        => EventSpecAssembler.BuildPayloadJson(eventId, meta, previousSteps);

    public static List<BusinessEventMeta> ParseBusinessEvents(string skeletonJson)
    {
        var list = new List<BusinessEventMeta>();
        try
        {
            using var doc = JsonDocument.Parse(skeletonJson);
            if (!doc.RootElement.TryGetProperty("businessEvents", out var events))
                return list;

            foreach (var evt in events.EnumerateArray())
            {
                var eventId = evt.TryGetProperty("eventId", out var idEl) ? idEl.GetString() : null;
                var eventName = evt.TryGetProperty("eventName", out var nameEl) ? nameEl.GetString() : eventId;
                var hint = evt.TryGetProperty("complexityHint", out var hintEl) ? hintEl.GetString() : "simple";
                if (string.IsNullOrWhiteSpace(eventId)) continue;
                list.Add(new BusinessEventMeta(eventId, eventName ?? eventId, hint ?? "simple"));
            }
        }
        catch (JsonException)
        {
            // 非法 JSON 由调用方以空列表 + Oops.Bah 处理
        }

        return list;
    }

    public sealed record BusinessEventMeta(string EventId, string EventName, string ComplexityHint);
}
