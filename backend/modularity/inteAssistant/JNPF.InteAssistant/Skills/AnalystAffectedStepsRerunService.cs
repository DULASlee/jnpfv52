using JNPF.Common.Core.MultiTenancy;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Sa;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

public interface IAnalystAffectedStepsRerunService
{
    Task<RerunAffectedStepsResult> RunAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        string eventId,
        RerunAffectedStepsInput? input,
        CancellationToken ct = default);
}

/// <summary>
/// EventSpecRevised 后仅重跑受影响 SA 步骤（P2-B10 / D11）
/// </summary>
public sealed class AnalystAffectedStepsRerunService : IAnalystAffectedStepsRerunService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISaOrchestratorAdapter _saAdapter;
    private readonly IIrEventStoreService _eventStore;
    private readonly IIoiValidatorService _ioiValidator;
    private readonly ISkillRunGuard _runGuard;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<AnalystAffectedStepsRerunService> _logger;

    public AnalystAffectedStepsRerunService(
        ISaOrchestratorAdapter saAdapter,
        IIrEventStoreService eventStore,
        IIoiValidatorService ioiValidator,
        ISkillRunGuard runGuard,
        ISqlSugarClient db,
        ILogger<AnalystAffectedStepsRerunService> logger)
    {
        _saAdapter = saAdapter;
        _eventStore = eventStore;
        _ioiValidator = ioiValidator;
        _runGuard = runGuard;
        _db = db;
        _logger = logger;
    }

    public async Task<RerunAffectedStepsResult> RunAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        string eventId,
        RerunAffectedStepsInput? input,
        CancellationToken ct = default)
    {
        var fragmentId = eventId.StartsWith("eventspec:", StringComparison.OrdinalIgnoreCase)
            ? eventId
            : $"eventspec:{eventId}";
        var bareEventId = fragmentId["eventspec:".Length..];

        var runId = Guid.NewGuid().ToString("N");
        if (!_runGuard.TryAcquire(tenantId, pipelineId, "analyst-skill", runId, out var conflictRunId))
            throw Oops.Oh($"Analyst 已在运行中 (runId={conflictRunId})")
                .StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status409Conflict);

        try
        {
            var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString(), ct);
            var skeleton = snapshots.FirstOrDefault(s =>
                s.FragmentType == IrFragmentTypes.Skeleton
                && s.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked);
            var eventSpec = snapshots.FirstOrDefault(s =>
                string.Equals(s.FragmentId, fragmentId, StringComparison.Ordinal));

            if (skeleton == null)
                throw Oops.Bah("IR-0 骨架未 stable，无法重跑 SA 步骤");
            if (eventSpec == null)
                throw Oops.Bah($"EventSpec 不存在: {fragmentId}");

            var affectedSteps = await ResolveAffectedStepsAsync(input, projectId, tenantId, pipelineId.ToString(), fragmentId, ct);
            if (affectedSteps.Count == 0)
                throw Oops.Bah("无受影响 SA 步骤可重跑");

            var orderedAffected = SaStepMapping.IrStepOrder
                .Where(affectedSteps.Contains)
                .ToList();
            if (orderedAffected.Count == 0)
                throw Oops.Bah("受影响步骤不在 SA 九步序列中");

            var skeletonJson = SerializePayload(skeleton.Payload);
            var requirement = await LoadUserRequirementAsync(pipelineId, ct);
            var previousSteps = await BuildPreviousStepsAsync(
                projectId, tenantId, pipelineId.ToString(), fragmentId, orderedAffected, eventSpec, ct);

            _logger.LogInformation(
                "Rerun affected SA steps: pipeline={PipelineId} event={EventId} steps=[{Steps}]",
                pipelineId, bareEventId, string.Join(",", orderedAffected));

            foreach (var step in orderedAffected)
            {
                ct.ThrowIfCancellationRequested();
                var result = await _saAdapter.RunStepAsync(
                    tenantId, projectId, bareEventId, step,
                    requirement, skeletonJson, previousSteps, runId, ct);
                previousSteps[step] = result.Output;
            }

            var freshSnapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString(), ct);
            var freshSpec = freshSnapshots.FirstOrDefault(s =>
                string.Equals(s.FragmentId, fragmentId, StringComparison.Ordinal));
            var completed = freshSpec?.SaStepsCompleted ?? Array.Empty<string>();
            var allComplete = SaStepMapping.IrStepOrder.All(completed.Contains);

            var rerunResult = new RerunAffectedStepsResult
            {
                RunId = runId,
                FragmentId = fragmentId,
                EventId = bareEventId,
                RerunSteps = orderedAffected,
                SaStepsCompleted = completed.ToList(),
                EventSpecReconfirmed = false,
            };

            if (allComplete)
            {
                var payload = BuildEventSpecPayload(bareEventId, eventSpec, previousSteps, freshSpec?.CurrentVersion ?? 1);
                _ioiValidator.Validate(payload);
                await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
                {
                    EventType = IrEventTypes.EventSpecConfirmed,
                    FragmentId = fragmentId,
                    FragmentType = IrFragmentTypes.EventSpec,
                    FragmentVersion = freshSpec?.CurrentVersion ?? 1,
                    Payload = payload,
                    SkillId = "analyst-skill",
                }, ct);
                rerunResult = rerunResult with { EventSpecReconfirmed = true };
            }

            return rerunResult;
        }
        finally
        {
            _runGuard.Release(tenantId, pipelineId, "analyst-skill");
        }
    }

    private async Task<List<string>> ResolveAffectedStepsAsync(
        RerunAffectedStepsInput? input,
        string projectId,
        string tenantId,
        string pipelineId,
        string fragmentId,
        CancellationToken ct)
    {
        if (input?.Steps is { Count: > 0 })
            return input.Steps.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

        if (!string.IsNullOrWhiteSpace(input?.RevisionType))
            return EventSpecRevisionPlanner.GetAffectedSteps(input.RevisionType).ToList();

        return await GetAffectedStepsFromLastRevisionAsync(projectId, tenantId, pipelineId, fragmentId, ct);
    }

    private async Task<List<string>> GetAffectedStepsFromLastRevisionAsync(
        string projectId, string tenantId, string pipelineId, string fragmentId, CancellationToken ct)
    {
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId, ct);
        var revised = events
            .Where(e => e.EventType == IrEventTypes.EventSpecRevised
                && string.Equals(e.FragmentId, fragmentId, StringComparison.Ordinal))
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();

        if (revised?.PayloadPreview == null)
            return new List<string>();

        return ParseAffectedStepsFromPayload(revised.PayloadPreview);
    }

    private static List<string> ParseAffectedStepsFromPayload(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("affectedSteps", out var stepsEl)
                && stepsEl.ValueKind == JsonValueKind.Array)
            {
                return stepsEl.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .ToList();
            }
        }
        catch { /* ignore */ }

        return new List<string>();
    }

    private async Task<Dictionary<string, object>> BuildPreviousStepsAsync(
        string projectId,
        string tenantId,
        string pipelineId,
        string fragmentId,
        IReadOnlyList<string> affectedSteps,
        IrFragmentSnapshotDto eventSpec,
        CancellationToken ct)
    {
        var affectedSet = affectedSteps.ToHashSet(StringComparer.Ordinal);
        var previousSteps = new Dictionary<string, object>(StringComparer.Ordinal);

        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId, ct);
        foreach (var evt in events.Where(e =>
            e.EventType == IrEventTypes.SaStepCompleted
            && string.Equals(e.FragmentId, fragmentId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(e.SaStepName)))
        {
            if (affectedSet.Contains(evt.SaStepName!))
                continue;

            var output = ParseStepOutput(evt.PayloadPreview);
            if (output != null)
                previousSteps[evt.SaStepName!] = output;
        }

        TryMergePayloadPreviousSteps(eventSpec.Payload, affectedSet, previousSteps);
        return previousSteps;
    }

    private static void TryMergePayloadPreviousSteps(
        object? payload,
        HashSet<string> affectedSet,
        Dictionary<string, object> previousSteps)
    {
        try
        {
            var json = payload is string s ? s : JsonSerializer.Serialize(payload, JsonOptions);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("previousSteps", out var prevEl)
                || prevEl.ValueKind != JsonValueKind.Object)
                return;

            foreach (var prop in prevEl.EnumerateObject())
            {
                if (affectedSet.Contains(prop.Name) || previousSteps.ContainsKey(prop.Name))
                    continue;
                previousSteps[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText(), JsonOptions)
                    ?? new { };
            }
        }
        catch { /* ignore */ }
    }

    private static object? ParseStepOutput(string? payloadPreview)
    {
        if (string.IsNullOrWhiteSpace(payloadPreview))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payloadPreview);
            if (doc.RootElement.TryGetProperty("output", out var outputEl))
                return JsonSerializer.Deserialize<object>(outputEl.GetRawText(), JsonOptions);
        }
        catch { /* ignore */ }

        return null;
    }

    private static string BuildEventSpecPayload(
        string bareEventId,
        IrFragmentSnapshotDto eventSpec,
        IReadOnlyDictionary<string, object> previousSteps,
        int version)
    {
        var eventName = bareEventId;
        try
        {
            var json = eventSpec.Payload is string s ? s : JsonSerializer.Serialize(eventSpec.Payload, JsonOptions);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("eventName", out var nameEl))
                eventName = nameEl.GetString() ?? bareEventId;
        }
        catch { /* ignore */ }

        return JsonSerializer.Serialize(new
        {
            eventId = bareEventId,
            eventName,
            version,
            confirmedFields = new[] { new { name = "id", type = "string", required = true } },
            businessRules = new[] { new { ruleId = "R1", description = eventName, source = "rerun-affected" } },
            ioiInvariants = Array.Empty<object>(),
            saStepsCompleted = SaStepMapping.IrStepOrder,
            previousSteps,
            rerunAt = DateTime.UtcNow.ToString("O"),
        }, JsonOptions);
    }

    private async Task<string> LoadUserRequirementAsync(long pipelineId, CancellationToken ct)
    {
        var tenantId = TenantResolver.Resolve().ToString();
        var msg = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString() && x.TenantId == tenantId && x.Role == "user")
            .OrderByDescending(x => x.CreatorTime)
            .FirstAsync(ct);
        return msg?.Content ?? string.Empty;
    }

    private static string SerializePayload(object? payload)
        => payload is string s ? s : JsonSerializer.Serialize(payload, JsonOptions);
}
