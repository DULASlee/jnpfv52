using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills.Cognitive;

namespace JNPF.InteAssistant.Ir;

public interface IEventSpecRevisionService
{
    Task<ReviseEventSpecResult> ReviseAsync(
        string projectId,
        string tenantId,
        string pipelineId,
        string fragmentId,
        ReviseEventSpecInput input,
        CancellationToken ct = default);
}

public sealed class EventSpecRevisionService : IEventSpecRevisionService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IIrEventStoreService _eventStore;
    private readonly IExperienceRecorder _experience;

    public EventSpecRevisionService(IIrEventStoreService eventStore, IExperienceRecorder experience)
    {
        _eventStore = eventStore;
        _experience = experience;
    }

    public async Task<ReviseEventSpecResult> ReviseAsync(
        string projectId,
        string tenantId,
        string pipelineId,
        string fragmentId,
        ReviseEventSpecInput input,
        CancellationToken ct = default)
    {
        if (!EventSpecRevisionPlanner.IsKnownRevisionType(input.RevisionType))
            throw Oops.Bah($"未知修订类型: {input.RevisionType}");

        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId);
        var snap = snapshots.FirstOrDefault(s =>
            string.Equals(s.FragmentId, fragmentId, StringComparison.Ordinal));

        if (snap == null)
            throw Oops.Bah($"片段不存在: {fragmentId}");

        if (!string.Equals(snap.FragmentType, IrFragmentTypes.EventSpec, StringComparison.Ordinal)
            && !fragmentId.StartsWith("eventspec:", StringComparison.OrdinalIgnoreCase))
            throw Oops.Bah("仅支持 EventSpec 片段增量修订");

        var affectedSteps = EventSpecRevisionPlanner.GetAffectedSteps(input.RevisionType).ToList();
        if (affectedSteps.Count == 0)
            throw Oops.Bah("修订类型未映射到 SA 步骤");

        var previousCompleted = snap.SaStepsCompleted ?? Array.Empty<string>();
        var retainedSteps = EventSpecRevisionPlanner.TrimCompletedSteps(previousCompleted, affectedSteps);
        var newVersion = snap.CurrentVersion + 1;
        var beforeJson = PayloadToJson(snap.Payload);

        var mergedPayload = MergePayload(snap.Payload, input.PayloadPatch, input.RevisionType, affectedSteps);

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.FragmentInvalidated,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = newVersion,
            Payload = JsonSerializer.Serialize(new
            {
                fragmentId,
                reason = input.RevisionType,
                affectedSteps,
            }, JsonOptions),
            SkillId = "analyst-skill",
        });

        var revised = await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.EventSpecRevised,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = newVersion,
            Payload = mergedPayload,
            SkillId = "analyst-skill",
        });

        await _experience.RecordHumanCorrectionAsync(
            projectId, tenantId, "analyst-skill", fragmentId,
            beforeJson, mergedPayload,
            input.RevisionType ?? "manual-revision",
            ct);

        return new ReviseEventSpecResult
        {
            EventId = revised.Id,
            FragmentId = fragmentId,
            RevisionType = input.RevisionType,
            AffectedSteps = affectedSteps,
            RetainedSteps = retainedSteps,
            RemovedSteps = affectedSteps.Where(a => previousCompleted.Contains(a, StringComparer.Ordinal)).ToList(),
            NewVersion = newVersion,
            AutoRerunRequested = input.AutoRerunAffected == true,
        };
    }

    private static string MergePayload(
        object? existingPayload,
        string? patchJson,
        string revisionType,
        IReadOnlyList<string> affectedSteps)
    {
        var baseObj = existingPayload switch
        {
            null => new Dictionary<string, object>(),
            JsonElement el => JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), JsonOptions) ?? new(),
            string s when !string.IsNullOrWhiteSpace(s) =>
                JsonSerializer.Deserialize<Dictionary<string, object>>(s, JsonOptions) ?? new(),
            _ => JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(existingPayload, JsonOptions), JsonOptions) ?? new(),
        };

        if (!string.IsNullOrWhiteSpace(patchJson))
        {
            var patch = JsonSerializer.Deserialize<Dictionary<string, object>>(patchJson, JsonOptions);
            if (patch != null)
            {
                foreach (var kv in patch)
                    baseObj[kv.Key] = kv.Value;
            }
        }

        baseObj["revisionType"] = revisionType;
        baseObj["affectedSteps"] = affectedSteps;
        baseObj["revisedAt"] = DateTime.UtcNow.ToString("O");
        return JsonSerializer.Serialize(baseObj, JsonOptions);
    }

    private static string PayloadToJson(object? payload) => payload switch
    {
        null => "{}",
        string s => s,
        JsonElement el => el.GetRawText(),
        _ => JsonSerializer.Serialize(payload, JsonOptions),
    };
}
