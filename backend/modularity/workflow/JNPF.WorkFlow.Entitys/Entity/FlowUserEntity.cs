using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程任务发起人.
/// </summary>
[SugarTable("FLOW_LAUNCH_USER")]
[Tenant(ClaimConst.TENANTID)]
public class FlowUserEntity : CLDEntityBase
{
    /// <summary>
    /// 任务id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 组织主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORGANIZE_ID")]
    public string? OrganizeId { get; set; }

    /// <summary>
    /// 岗位主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_POSITION_ID")]
    public string? PositionId { get; set; }

    /// <summary>
    /// 主管主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MANAGER_ID")]
    public string? ManagerId { get; set; }

    /// <summary>
    /// 上级用户.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUPERIOR")]
    public string? Superior { get; set; }

    /// <summary>
    /// 下属用户.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUBORDINATE")]
    public string? Subordinate { get; set; }

    /// <summary>
    /// 公司下所有部门.
    /// </summary>
    [SugarColumn(ColumnName = "F_DEPARTMENT")]
    public string? Department { get; set; }
}
