using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.WorkFlow.Entitys.Enum;

/// <summary>
/// 审批人类型枚举.
/// </summary>
[SuppressSniffer]
public enum FlowTaskOperatorEnum
{
    /// <summary>
    /// 发起者主管.
    /// </summary>
    [Description("发起者主管")]
    LaunchCharge = 1,

    /// <summary>
    /// 发起者部门主管.
    /// </summary>
    [Description("发起者部门主管")]
    DepartmentCharge = 2,

    /// <summary>
    /// 发起者本人.
    /// </summary>
    [Description("发起者本人")]
    InitiatorMe = 3,

    /// <summary>
    /// 获取表单某个值为审批人.
    /// </summary>
    [Description("变量")]
    VariableApprover = 4,

    /// <summary>
    /// 之前节点的审批人.
    /// </summary>
    [Description("环节")]
    LinkApprover = 5,

    /// <summary>
    /// 候选审批人.
    /// </summary>
    [Description("候选审批人")]
    CandidateApprover = 7,

    /// <summary>
    /// 服务（调用指定接口获取数据）.
    /// </summary>
    [Description("服务")]
    ServiceApprover = 9,

    /// <summary>
    /// 子流程.
    /// </summary>
    [Description("子流程")]
    SubProcesses = 10
}