using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Skills.Cognitive;

/// <summary>
/// 认知技能的"心脏"——经验回流记录器（施工包 21 §3.4 进化层地基）。
/// 三类经验事件全部写入 IR 事件流（唯一血液），不另建存储；
/// 投影引擎对未知事件类型返回 null，只入事件表不动快照。
/// </summary>
public interface IExperienceRecorder
{
    /// <summary>记录评审结论（人或 Guard 对技能产物的裁决）。</summary>
    Task RecordReviewAsync(
        string projectId, string tenantId, string skillId, string runId,
        string verdict, string detailJson, CancellationToken ct = default);

    /// <summary>记录失败经验（异常种类 + 消息 + 运行上下文）。</summary>
    Task RecordFailureAsync(
        string projectId, string tenantId, string skillId, string runId,
        string errorKind, string message, CancellationToken ct = default);

    /// <summary>记录人工纠偏 before/after diff。</summary>
    Task RecordHumanCorrectionAsync(
        string projectId, string tenantId, string skillId, string? fragmentId,
        string beforeJson, string afterJson, string reason, CancellationToken ct = default);
}

public sealed class ExperienceRecorder : IExperienceRecorder, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IEventStream _events;

    public ExperienceRecorder(IEventStream events) => _events = events;

    public Task RecordReviewAsync(
        string projectId, string tenantId, string skillId, string runId,
        string verdict, string detailJson, CancellationToken ct = default)
    {
        return _events.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.SkillReviewRecorded,
            SkillId = skillId,
            Payload = JsonSerializer.Serialize(new
            {
                runId,
                verdict,
                detail = TryParse(detailJson),
                recordedAt = DateTime.UtcNow,
            }, JsonOptions),
        }, ct);
    }

    public Task RecordFailureAsync(
        string projectId, string tenantId, string skillId, string runId,
        string errorKind, string message, CancellationToken ct = default)
    {
        return _events.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.SkillFailureRecorded,
            SkillId = skillId,
            Payload = JsonSerializer.Serialize(new
            {
                runId,
                errorKind,
                message,
                recordedAt = DateTime.UtcNow,
            }, JsonOptions),
        }, ct);
    }

    public Task RecordHumanCorrectionAsync(
        string projectId, string tenantId, string skillId, string? fragmentId,
        string beforeJson, string afterJson, string reason, CancellationToken ct = default)
    {
        return _events.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.HumanCorrectionRecorded,
            SkillId = skillId,
            FragmentId = fragmentId,
            Payload = JsonSerializer.Serialize(new
            {
                before = TryParse(beforeJson),
                after = TryParse(afterJson),
                reason,
                recordedAt = DateTime.UtcNow,
            }, JsonOptions),
        }, ct);
    }

    /// <summary>合法 JSON 保持结构化嵌入，否则按原始字符串记录。</summary>
    private static object TryParse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new { };
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
