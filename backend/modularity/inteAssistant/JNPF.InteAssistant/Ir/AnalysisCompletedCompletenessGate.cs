using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;
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

    public AnalysisCompletedCompletenessGate(ISqlSugarClient db) => _db = db;

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
            foreach (var step in IrSaSteps.All)
            {
                if (!completed.Contains(step))
                    return SkillValidationResult.Fail($"EventSpec {eventId} 缺 SA 步骤: {step}");
            }
        }

        return SkillValidationResult.Ok();
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
