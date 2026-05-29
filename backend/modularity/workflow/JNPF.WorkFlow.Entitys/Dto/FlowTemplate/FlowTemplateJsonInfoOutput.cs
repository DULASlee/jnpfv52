using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowTemplate;
[SuppressSniffer]
public class FlowTemplateJsonInfoOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string? templateId { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? flowName { get; set; }

    /// <summary>
    /// 流程类型.
    /// </summary>
    public int? flowType { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 可见范围.
    /// </summary>
    public int? visibleType { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public string? version { get; set; }

    /// <summary>
    /// 流程JOSN包.
    /// </summary>
    public string? flowTemplateJson { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string? creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改人.
    /// </summary>
    public string? lastModifyUser { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 标识.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }
}
