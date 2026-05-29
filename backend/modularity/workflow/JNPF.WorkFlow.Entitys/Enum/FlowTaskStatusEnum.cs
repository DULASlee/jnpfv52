using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.WorkFlow.Entitys.Enum;

/// <summary>
/// 任务状态枚举.
/// </summary>
[SuppressSniffer]
public enum FlowTaskStatusEnum
{
    /// <summary>
    /// 等待提交.
    /// </summary>
    [Description("等待提交")]
    Draft = 0,

    /// <summary>
    /// 等待审核.
    /// </summary>
    [Description("等待审核")]
    Handle = 1,

    /// <summary>
    /// 审核通过.
    /// </summary>
    [Description("审核通过")]
    Adopt = 2,

    /// <summary>
    /// 审核驳回.
    /// </summary>
    [Description("审核驳回")]
    Reject = 3,

    /// <summary>
    /// 审核撤销.
    /// </summary>
    [Description("审核撤销")]
    Revoke = 4,

    /// <summary>
    /// 审核作废.
    /// </summary>
    [Description("审核作废")]
    Cancel = 5,

    /// <summary>
    /// 审核挂起.
    /// </summary>
    [Description("审核挂起")]
    Suspend = 6,

    /// <summary>
    /// 发起撤回(重新提交).
    /// </summary>
    [Description("发起撤回(重新提交)")]
    RevokeDraft = 7,
}