using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// AI 调用日志列表输出
/// </summary>
[SuppressSniffer]
public class AiCallLogListOutput
{
    /// <summary>
    /// 主键
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string model { get; set; }

    /// <summary>
    /// 输入 Token
    /// </summary>
    public int? promptTokens { get; set; }

    /// <summary>
    /// 输出 Token
    /// </summary>
    public int? completionTokens { get; set; }

    /// <summary>
    /// 延迟毫秒
    /// </summary>
    public long? latencyMs { get; set; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int? statusCode { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建用户
    /// </summary>
    public string creatorUser { get; set; }
}
