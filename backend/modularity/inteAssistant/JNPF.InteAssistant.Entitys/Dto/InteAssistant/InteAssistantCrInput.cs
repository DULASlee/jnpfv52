using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// 集成助手信息输出.
/// </summary>
[SuppressSniffer]
public class InteAssistantCrInput
{
    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 启用标识.
    /// </summary>
    public int enabledMark { get; set; }

    /// <summary>
    /// 集成模板.
    /// </summary>
    public string templateJson { get; set; }

    /// <summary>
    /// 触发类型(1-事件，2-定时 ).
    /// </summary>
    public int type { get; set; }
}