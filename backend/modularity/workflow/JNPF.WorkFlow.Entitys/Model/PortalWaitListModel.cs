using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model;

[SuppressSniffer]
public class PortalWaitListModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 表单类型.
    /// </summary>
    public int? formType { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 实例.
    /// </summary>
    public string? processId { get; set; }

    /// <summary>
    /// 节点id.
    /// </summary>
    public string? taskNodeId { get; set; }

    /// <summary>
    /// 进程id.
    /// </summary>
    public string? taskOperatorId { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 委托发起人.
    /// </summary>
    public string? delegateUser { get; set; }
}
