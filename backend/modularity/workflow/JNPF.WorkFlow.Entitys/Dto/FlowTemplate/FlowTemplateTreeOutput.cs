using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowTemplate;
[SuppressSniffer]
public class FlowTemplateTreeOutput : TreeModel
{
    /// <summary>
    /// 流程编号.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 流程类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 流程分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 表单类型(数据字典-流程表单类型).
    /// </summary>
    public int? formType { get; set; }

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
    /// 图标.
    /// </summary>
    public string? icon { get; set; }

    /// <summary>
    /// 图标背景.
    /// </summary>
    public string? iconBackground { get; set; }

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
    /// 说明.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// 标识.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 流程主表id.
    /// </summary>
    public string? templateId { get; set; }
}
