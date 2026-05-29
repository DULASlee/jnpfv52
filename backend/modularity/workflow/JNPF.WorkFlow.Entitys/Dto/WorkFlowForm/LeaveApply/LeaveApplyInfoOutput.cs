using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.WorkFlowForm.LeaveApply;

[SuppressSniffer]
public class LeaveApplyInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int status { get; set; }

    /// <summary>
    /// 单据号.
    /// </summary>
    public string billNo { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string flowTitle { get; set; }

    /// <summary>
    /// 重要等级.
    /// </summary>
    public int? flowUrgent { get; set; }

    /// <summary>
    /// 请假类型.
    /// </summary>
    public string leaveType { get; set; }

    /// <summary>
    /// 请假原因.
    /// </summary>
    public string leaveReason { get; set; }

    /// <summary>
    /// 假期结束时间.
    /// </summary>
    public DateTime? leaveEndTime { get; set; }

    /// <summary>
    /// 假期开始时间.
    /// </summary>
    public DateTime? leaveStartTime { get; set; }

    /// <summary>
    /// 请假天数.
    /// </summary>
    public string leaveDayCount { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 申请日期.
    /// </summary>
    public DateTime? applyDate { get; set; }

    /// <summary>
    /// 申请部门.
    /// </summary>
    public string applyDept { get; set; }

    /// <summary>
    /// 申请职位.
    /// </summary>
    public string applyPost { get; set; }

    /// <summary>
    /// 申请人员.
    /// </summary>
    public string applyUser { get; set; }

    /// <summary>
    /// 相关附件.
    /// </summary>
    public string fileJson { get; set; }

    /// <summary>
    /// 请假小时.
    /// </summary>
    public string leaveHour { get; set; }
}
