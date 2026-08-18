using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace JNPF.InteAssistant.Ir;

public interface IAnalysisCompletedCompletenessGate
{
    Task<SkillValidationResult> ValidateAsync(
        string tenantId,
        string projectId,
        IrSnapshot snapshot,
        string? excludeRunId = null,
        CancellationToken ct = default);
}

/// <summary>
/// AnalysisCompleted 前置完整性门禁（P2-R03）
/// </summary>
public sealed class AnalysisCompletedCompletenessGate : IAnalysisCompletedCompletenessGate, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISqlSugarClient _db;
    private readonly SaPipelineOptions _pipelineOptions;

    public AnalysisCompletedCompletenessGate(ISqlSugarClient db, IOptions<SaPipelineOptions> pipelineOptions)
    {
        _db = db;
        _pipelineOptions = pipelineOptions.Value;
    }

    public async Task<SkillValidationResult> ValidateAsync(
        string tenantId,
        string projectId,
        IrSnapshot snapshot,
        string? excludeRunId = null,
        CancellationToken ct = default)
    {
        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (skeleton == null)
            return SkillValidationResult.Fail("IR-0 骨架未 stable");

        var eventIds = ParseBusinessEventIds(skeleton.Payload);
        if (eventIds.Count == 0)
            return SkillValidationResult.Fail("IR-0 无 businessEvents");

        if (_db != null)
        {
            var runningList = await _db.Queryable<AiSkillRunEntity>()
                .Where(x => x.ProjectId == projectId && x.TenantId == tenantId
                    && x.SkillId == "analyst-skill" && x.Status == "running")
                .ToListAsync(ct);

            if (!string.IsNullOrEmpty(excludeRunId))
                runningList = runningList.Where(x => x.Id != excludeRunId).ToList();

            if (runningList.Count > 0)
                return SkillValidationResult.Fail("analyst-skill 仍在运行");
        }

        foreach (var eventId in eventIds)
        {
            var fragmentId = $"eventspec:{eventId}";
            var snap = snapshot.Fragments.FirstOrDefault(f => f.FragmentId == fragmentId);
            if (snap == null)
                return SkillValidationResult.Fail($"缺少 EventSpec 片段: {fragmentId}");

            if (snap.StabilityState != IrStabilityStates.Stable && snap.StabilityState != IrStabilityStates.Locked)
                return SkillValidationResult.Fail($"EventSpec 未 stable: {fragmentId}");

            var completed = snap.SaStepsCompleted.ToHashSet(StringComparer.Ordinal);
            var requiredSteps = ParseSaStepsFromPayload(snap.Payload);
            if (requiredSteps.Count == 0)
                return SkillValidationResult.Fail($"EventSpec {eventId} payload 缺少 saStepsCompleted");

            if (completed.Count == 0)
                completed = requiredSteps.ToHashSet(StringComparer.Ordinal);

            foreach (var step in requiredSteps)
            {
                if (!completed.Contains(step))
                    return SkillValidationResult.Fail($"EventSpec {eventId} 缺 SA 步骤: {step}");
            }
        }

        if (_pipelineOptions.IsCompileMode && _db != null)
        {
            var reviewVerdicts = await _db.Queryable<AiIrEventEntity>()
                .Where(x => x.ProjectId == projectId && x.TenantId == tenantId
                    && x.EventType == IrEventTypes.SkillReviewRecorded)
                .OrderByDescending(x => x.Sequence)
                .Take(50)
                .ToListAsync(ct);

            var verdicts = reviewVerdicts
                .Select(ParseReviewVerdict)
                .Where(v => !string.IsNullOrEmpty(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!verdicts.Contains("pm-s2-pass"))
                return SkillValidationResult.Fail("S2 compile 缺少 pm-s2-pass 双审");
            if (!verdicts.Contains("analyst-s2-pass"))
                return SkillValidationResult.Fail("S2 compile 缺少 analyst-s2-pass 双审");
        }

        return SkillValidationResult.Ok();
    }

    private static string? ParseReviewVerdict(AiIrEventEntity evt)
    {
        try
        {
            using var doc = JsonDocument.Parse(evt.Payload ?? "{}");
            if (doc.RootElement.TryGetProperty("verdict", out var v))
                return v.GetString();
        }
        catch
        {
            /* ignore */
        }

        return null;
    }

    private static List<string> ParseSaStepsFromPayload(object? payload)
    {
        var list = new List<string>();
        try
        {
            var json = payload switch
            {
                string s => s,
                null => "{}",
                _ => JsonSerializer.Serialize(payload, JsonOptions),
            };
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("saStepsCompleted", out var steps)
                && steps.ValueKind == JsonValueKind.Array)
            {
                foreach (var step in steps.EnumerateArray())
                {
                    var name = step.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                        list.Add(name);
                }
            }
        }
        catch
        {
            /* ignore */
        }

        return list;
    }

    private static List<string> ParseBusinessEventIds(string skeletonJson)
    {
        var list = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(skeletonJson);
            if (!doc.RootElement.TryGetProperty("businessEvents", out var events))
                return list;

            foreach (var evt in events.EnumerateArray())
            {
                if (evt.TryGetProperty("eventId", out var idEl))
                {
                    var id = idEl.GetString();
                    if (!string.IsNullOrWhiteSpace(id))
                        list.Add(id);
                }
            }
        }
        catch
        {
            /* ignore */
        }

        return list;
    }
}
