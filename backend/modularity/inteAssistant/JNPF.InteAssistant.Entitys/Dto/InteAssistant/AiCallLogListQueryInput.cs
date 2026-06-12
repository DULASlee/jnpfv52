using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// AI 调用日志列表查询输入
/// </summary>
[SuppressSniffer]
public class AiCallLogListQueryInput : PageInputBase
{
    /// <summary>
    /// 模型名称
    /// </summary>
    public string? model { get; set; }

    /// <summary>
    /// HTTP 状态码筛选
    /// </summary>
    public int? statusCode { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? endTime { get; set; }
}
