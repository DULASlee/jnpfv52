using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// AI 调用日志详情输出
/// </summary>
[SuppressSniffer]
public class AiCallLogInfoOutput
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
    /// 请求体 JSON
    /// </summary>
    public string requestBody { get; set; }

    /// <summary>
    /// 响应体 JSON
    /// </summary>
    public string responseBody { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建用户
    /// </summary>
    public string creatorUser { get; set; }
}
