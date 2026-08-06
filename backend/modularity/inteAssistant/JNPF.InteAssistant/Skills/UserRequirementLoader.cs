using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 从 IR / 事件流 / 流水线消息解析用户需求文本（供设计 Skill Harness 使用）。
/// </summary>
public interface IUserRequirementLoader
{
    Task<string> LoadAsync(string tenantId, string projectId, long pipelineId, CancellationToken ct = default);
}

public sealed class UserRequirementLoader : IUserRequirementLoader, ITransient
{
    private readonly IIrEventStoreService _eventStore;
    private readonly ISqlSugarClient _db;

    public UserRequirementLoader(IIrEventStoreService eventStore, ISqlSugarClient db)
    {
        _eventStore = eventStore;
        _db = db;
    }

    public async Task<string> LoadAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct = default)
    {
        var snapshots = await _eventStore.ListSnapshotsAsync(
            projectId, tenantId, pipelineId.ToString(), ct);
        var fragments = snapshots.Select(d => new IrSnapshotFragment
        {
            FragmentId = d.FragmentId,
            FragmentType = d.FragmentType,
            StabilityState = d.StabilityState,
            Payload = d.Payload is string s ? s : JsonSerializer.Serialize(d.Payload),
        }).ToList();
        var snapshot = new IrSnapshot { Fragments = fragments };

        var reqFragment = snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Requirement);
        var text = ExtractRequirementText(reqFragment);
        if (!string.IsNullOrWhiteSpace(text)) return text.Trim();

        foreach (var eventType in new[]
                 {
                     IrEventTypes.RequirementRefined,
                     IrEventTypes.RequirementEnhanced,
                     IrEventTypes.RequirementSpecRendered,
                 })
        {
            var payload = await _eventStore.GetLatestEventPayloadAsync(
                projectId, tenantId, pipelineId.ToString(), eventType, ct);
            text = ExtractRequirementTextFromPayload(payload);
            if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
        }

        var msg = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString()
                && x.TenantId == tenantId
                && x.Role == "user")
            .OrderByDescending(x => x.CreatorTime)
            .FirstAsync(ct);
        if (!string.IsNullOrWhiteSpace(msg?.Content))
            return msg.Content.Trim();

        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .Select(x => new { x.Name })
            .FirstAsync(ct);
        if (!string.IsNullOrWhiteSpace(pipeline?.Name))
            return pipeline.Name.Trim();

        throw Oops.Bah(
            "设计 Skill 缺少用户需求文本：IR 无 Requirement 片段且流水线无用户消息，请先完成需求分析");
    }

    private static string? ExtractRequirementText(IrSnapshotFragment? fragment)
    {
        if (fragment == null || string.IsNullOrWhiteSpace(fragment.Payload)) return null;
        try
        {
            using var doc = JsonDocument.Parse(fragment.Payload);
            if (doc.RootElement.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
            return fragment.Payload;
        }
        catch (JsonException)
        {
            return fragment.Payload;
        }
    }

    private static string? ExtractRequirementTextFromPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
        }
        catch (JsonException) { /* ignore */ }

        return null;
    }
}
