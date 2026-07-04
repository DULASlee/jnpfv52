using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Sa;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

public sealed class AnalystSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISaOrchestratorAdapter _saAdapter;
    private readonly IEventParallelScheduler _scheduler;
    private readonly IIoiValidatorService _ioiValidator;
    private readonly IAnalysisCompletedCompletenessGate _completenessGate;
    private readonly IIrEventStoreService _eventStore;
    private readonly ILogger<AnalystSkillService> _logger;

    public AnalystSkillService(
        ISaOrchestratorAdapter saAdapter,
        IEventParallelScheduler scheduler,
        IIoiValidatorService ioiValidator,
        IAnalysisCompletedCompletenessGate completenessGate,
        IIrEventStoreService eventStore,
        ILogger<AnalystSkillService> logger)
    {
        _saAdapter = saAdapter;
        _scheduler = scheduler;
        _ioiValidator = ioiValidator;
        _completenessGate = completenessGate;
        _eventStore = eventStore;
        _logger = logger;
    }

    public string SkillId => "analyst-skill";
    public string Version => "1.0.0-mvp";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[] { IrFragmentTypes.Skeleton },
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.SaStepCompleted,
            IrEventTypes.EventSpecConfirmed,
            IrEventTypes.AnalysisCompleted,
        },
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (skeleton == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-0 骨架未 stable，请先确认骨架"));
        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var skeleton = context.Snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)!;
        var businessEvents = ParseBusinessEvents(skeleton.Payload);
        if (businessEvents.Count == 0)
            throw new InvalidOperationException("IR-0 无 businessEvents");

        var confirmedEvents = new ConcurrentBag<AppendIrEventRequest>();

        await _scheduler.RunAsync(businessEvents.Select(e => e.EventId).ToList(), async (eventId, token) =>
        {
            var meta = businessEvents.First(e => e.EventId == eventId);
            var seed = context.SeedMatches.FirstOrDefault(s =>
                meta.EventName.Contains(s.EventNamePattern, StringComparison.OrdinalIgnoreCase)
                || s.EventNamePattern.Contains(meta.EventName, StringComparison.OrdinalIgnoreCase));

            if (meta.ComplexityHint == "auto" && seed != null && seed.CoverageScore >= 0.85m)
            {
                await RunAutoEventAsync(context, skeleton.Payload, eventId, meta, seed, confirmedEvents, token);
                return;
            }

            var previousSteps = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var step in SaStepMapping.IrStepOrder)
            {
                token.ThrowIfCancellationRequested();
                var result = await _saAdapter.RunStepAsync(
                    context.TenantId, context.ProjectId, eventId, step,
                    context.UserRequirement, skeleton.Payload, previousSteps, context.RunId, token);
                previousSteps[step] = result.Output;
            }

            var eventSpecPayload = BuildEventSpecPayload(eventId, meta, previousSteps);
            _ioiValidator.Validate(eventSpecPayload);
            confirmedEvents.Add(new AppendIrEventRequest
            {
                EventType = IrEventTypes.EventSpecConfirmed,
                FragmentId = $"eventspec:{eventId}",
                FragmentType = IrFragmentTypes.EventSpec,
                Payload = eventSpecPayload,
                SkillId = SkillId,
            });
        }, ct);

        foreach (var evt in confirmedEvents)
            yield return evt;

        var freshSnapshot = await BuildSnapshotAsync(context.TenantId, context.ProjectId, ct);
        var gate = await _completenessGate.ValidateAsync(
            context.TenantId, context.ProjectId, freshSnapshot, context.RunId, ct);
        if (!gate.IsValid)
            throw new InvalidOperationException(gate.ErrorMessage ?? "AnalysisCompleted 完整性门禁未通过");

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.AnalysisCompleted,
            Payload = JsonSerializer.Serialize(new
            {
                projectId = context.ProjectId,
                eventSpecCount = businessEvents.Count,
                allStable = true,
            }, JsonOptions),
            SkillId = SkillId,
        };
    }

    private async Task RunAutoEventAsync(
        SkillContext context,
        string skeletonJson,
        string eventId,
        BusinessEventMeta meta,
        SeedTemplateMatch seed,
        ConcurrentBag<AppendIrEventRequest> confirmedEvents,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Auto seed path event={EventId} template={TemplateId} — 跳过 sa-service LLM",
            eventId, seed.TemplateId);

        foreach (var step in SaStepMapping.IrStepOrder)
        {
            ct.ThrowIfCancellationRequested();
            await _eventStore.AppendAsync(context.ProjectId, context.TenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.SaStepCompleted,
                FragmentId = $"eventspec:{eventId}",
                FragmentType = IrFragmentTypes.EventSpec,
                SaStepName = step,
                Payload = JsonSerializer.Serialize(new
                {
                    eventId,
                    step,
                    source = "seed-auto",
                    seedTemplateId = seed.TemplateId,
                }, JsonOptions),
                SkillId = SkillId,
            }, ct);
        }

        var payload = JsonSerializer.Serialize(new
        {
            eventId,
            eventName = meta.EventName,
            version = 1,
            confirmedFields = new[] { new { name = "id", type = "string", required = true } },
            businessRules = new[] { new { ruleId = "R1", description = seed.EventNamePattern, source = "seed-data" } },
            ioiInvariants = Array.Empty<object>(),
            saStepsCompleted = SaStepMapping.IrStepOrder,
            seedTemplateId = seed.TemplateId,
            autoSeed = true,
        }, JsonOptions);

        _ioiValidator.Validate(payload);
        confirmedEvents.Add(new AppendIrEventRequest
        {
            EventType = IrEventTypes.EventSpecConfirmed,
            FragmentId = $"eventspec:{eventId}",
            FragmentType = IrFragmentTypes.EventSpec,
            Payload = payload,
            SkillId = SkillId,
        });
    }

    public Task<SkillValidationResult> ValidateOutputAsync(IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (!events.Any(e => e.EventType == IrEventTypes.AnalysisCompleted))
            return Task.FromResult(SkillValidationResult.Fail("缺少 AnalysisCompleted 事件"));
        return Task.FromResult(SkillValidationResult.Ok());
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
    {
        return JsonSerializer.Serialize(new
        {
            eventId,
            eventName = meta.EventName,
            version = 1,
            confirmedFields = new[] { new { name = "id", type = "string", required = true } },
            businessRules = new[] { new { ruleId = "R1", description = meta.EventName, source = "user-stated" } },
            ioiInvariants = Array.Empty<object>(),
            saStepsCompleted = SaStepMapping.IrStepOrder,
            previousSteps,
        }, JsonOptions);
    }

    private static List<BusinessEventMeta> ParseBusinessEvents(string skeletonJson)
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
        catch { /* ignore */ }

        return list;
    }

    private sealed record BusinessEventMeta(string EventId, string EventName, string ComplexityHint);
}
