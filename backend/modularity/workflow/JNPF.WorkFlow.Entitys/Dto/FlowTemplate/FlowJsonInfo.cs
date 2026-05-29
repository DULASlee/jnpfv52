using JNPF.WorkFlow.Entitys.Model;

namespace JNPF.WorkFlow.Entitys.Dto.FlowTemplate;

public class FlowJsonInfo
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 流程编码.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 流程JOSN包.
    /// </summary>
    public FlowTemplateJsonModel? flowTemplateJson { get; set; }

    /// <summary>
    /// 是否删除.
    /// </summary>
    public bool? isDelete { get; set; }
}
