using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.WorkFlow.Entitys.Enum;

/// <summary>
/// 任务节点事件枚举.
/// </summary>
[SuppressSniffer]
public enum FuncConfigEnum
{
    /// <summary>
    /// 发起事件.
    /// </summary>
    [Description("发起事件")]
    init,

    /// <summary>
    /// 结束事件.
    /// </summary>
    [Description("结束事件")]
    end,

    /// <summary>
    /// 撤回事件.
    /// </summary>
    [Description("撤回事件")]
    flowRecall,

    /// <summary>
    /// 同意事件.
    /// </summary>
    [Description("同意事件")]
    approve,

    /// <summary>
    /// 退回事件.
    /// </summary>
    [Description("退回事件")]
    reject,

    /// <summary>
    /// 审核撤回事件.
    /// </summary>
    [Description("审核撤回事件")]
    recall,

    /// <summary>
    /// 超时事件.
    /// </summary>
    [Description("超时事件")]
    overTime,

    /// <summary>
    /// 提醒事件.
    /// </summary>
    [Description("提醒事件")]
    notice,
}
