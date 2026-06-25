namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// LLM Provider 健康检查结果
/// </summary>
public class ProviderHealth
{
    /// <summary>
    /// 是否健康
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Provider 名称
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// 延迟毫秒
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// 错误信息（如有）
    /// </summary>
    public string Error { get; set; }
}
