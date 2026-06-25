namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// Pipeline SSE 事件（校验进度推送）
/// </summary>
public class PipelineSSEEvent
{
    /// <summary>当前阶段</summary>
    public string? Stage { get; set; }

    /// <summary>校验阶段</summary>
    public string Phase { get; set; } = "";

    /// <summary>思考/日志内容</summary>
    public string Thought { get; set; } = "";

    /// <summary>进度 0-100</summary>
    public int Progress { get; set; }
}
