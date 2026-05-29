using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.WorkFlow.Entitys.Enum;

/// <summary>
/// 任务节点类型枚举.
/// </summary>
[SuppressSniffer]
public enum FlowTaskNodeTypeEnum
{
    /// <summary>
    /// 开始.
    /// </summary>
    [Description("开始")]
    start,

    /// <summary>
    /// 审批.
    /// </summary>
    [Description("审批")]
    approver,

    /// <summary>
    /// 子流程.
    /// </summary>
    [Description("子流程")]
    subFlow,

    /// <summary>
    /// 条件.
    /// </summary>
    [Description("条件")]
    condition,

    /// <summary>
    /// 定时器.
    /// </summary>
    [Description("定时器")]
    timer,

    /// <summary>
    /// 结束.
    /// </summary>
    [Description("结束")]
    end,
}