namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// Pipeline SSE 事件载荷
/// 通过 SignalR 推送给前端
/// </summary>
public class PipelineEventPayload
{
    /// <summary>事件类型</summary>
    public string EventType { get; set; } = "";

    /// <summary>流水线 ID</summary>
    public string PipelineId { get; set; } = "";

    /// <summary>当前阶段</summary>
    public string Stage { get; set; } = "";

    /// <summary>用户 ID（定向推送）</summary>
    public string? UserId { get; set; }

    /// <summary>原因/消息</summary>
    public string? Reason { get; set; }

    /// <summary>操作按钮列表</summary>
    public List<PipelineAction>? Actions { get; set; }
}

/// <summary>
/// Pipeline 操作按钮
/// </summary>
public class PipelineAction
{
    public string Type { get; set; } = "";
    public string Url { get; set; } = "";
}
