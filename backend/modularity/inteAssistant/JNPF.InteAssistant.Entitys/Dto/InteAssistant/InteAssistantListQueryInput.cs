using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// 集成助手列表查询输入.
/// </summary>
[SuppressSniffer]
public class InteAssistantListQueryInput : PageInputBase
{
    /// <summary>
    /// 功能类型
    /// 1-事件触发,2-定时触发.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }
}