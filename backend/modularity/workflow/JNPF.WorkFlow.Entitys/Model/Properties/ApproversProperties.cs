using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

[SuppressSniffer]
public class ApproversProperties
{
    /// <summary>
    /// 标题.
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    /// 审批类型（类型参考FlowTaskOperatorEnum类）.
    /// </summary>
    public int assigneeType { get; set; }

    /// <summary>
    /// 进度.
    /// </summary>
    public string? progress { get; set; }

    /// <summary>
    /// 驳回类型(1:重新审批 2:从当前节点审批).
    /// </summary>
    public int? rejectType { get; set; }

    /// <summary>
    /// 驳回节点.
    /// </summary>
    public string? rejectStep { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// 自定义抄送人.
    /// </summary>
    public bool isCustomCopy { get; set; }

    /// <summary>
    /// 发起人主管级别.
    /// </summary>
    public int managerLevel { get; set; } = 1;

    /// <summary>
    /// 发起人主管级别.
    /// </summary>
    public int departmentLevel { get; set; } = 1;

    /// <summary>
    /// 会签比例.
    /// </summary>
    public CounterSignRuleConfig counterSignConfig { get; set; } = new CounterSignRuleConfig();

    /// <summary>
    /// 审批类型（0：或签 1：会签 2：依次审批） .
    /// </summary>
    public int? counterSign { get; set; } = 0;

    /// <summary>
    /// 表单字段.
    /// </summary>
    public string? formField { get; set; }

    /// <summary>
    /// 指定复审审批节点.
    /// </summary>
    public string? nodeId { get; set; }

    /// <summary>
    /// 服务 请求路径.
    /// </summary>
    public string? getUserUrl { get; set; }

    /// <summary>
    /// 是否有签名.
    /// </summary>
    public bool hasSign { get; set; }

    /// <summary>
    /// 是否有审批意见.
    /// </summary>
    public bool hasOpinion { get; set; } = true;

    /// <summary>
    /// 是否可以加签.
    /// </summary>
    public bool hasFreeApprover { get; set; }

    /// <summary>
    /// 打印id.
    /// </summary>
    public List<string> printId { get; set; } = new List<string>();

    /// <summary>
    /// 表单字段审核方式的类型(1-用户 2-部门).
    /// </summary>
    public int formFieldType { get; set; }

    /// <summary>
    /// 是否条件分支.
    /// </summary>
    public bool isBranchFlow { get; set; }

    /// <summary>
    /// 开启自动同意.
    /// </summary>
    public bool hasAgreeRule { get; set; }

    /// <summary>
    /// 自动同意规则,默认不启用.
    /// 1:不启用、2:审批人为发起人、3:审批人与上一审批节点处理人相同、4:审批人审批过.
    /// </summary>
    public List<string> agreeRules { get; set; } = new List<string>();

    /// <summary>
    /// 附加条件,默认无附加条件.
    /// 1:无附加条件、2:同一部门、3:同一岗位、4:发起人上级、5:发起人下属、6:同一公司.
    /// </summary>
    public string extraRule { get; set; } = "1";

    /// <summary>
    /// 抄送附加条件,默认无附加条件.
    /// 1:无附加条件、2:同一部门、3:同一岗位、4:发起人上级、5:发起人下属、6:同一公司.
    /// </summary>
    public string extraCopyRule { get; set; } = "1";

    /// <summary>
    /// 表单权限数据.
    /// </summary>
    public List<object>? formOperates { get; set; }

    /// <summary>
    /// 定时器到时时间.
    /// </summary>
    public List<TimerProperties> timerList { get; set; } = new List<TimerProperties>();

    /// <summary>
    /// 表单id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 继承父流程字段数据.
    /// </summary>
    public List<AssignItem>? assignList { get; set; }

    /// <summary>
    /// 是否抄送发起人.
    /// </summary>
    public bool isInitiatorCopy { get; set; }

    /// <summary>
    /// 是否抄送表单变量.
    /// </summary>
    public bool isFormFieldCopy { get; set; }

    /// <summary>
    /// 抄送表单字段.
    /// </summary>
    public string copyFormField { get; set; }

    /// <summary>
    /// 抄送表单字段类型.
    /// </summary>
    public string copyFormFieldType { get; set; }

    /// <summary>
    /// 转向节点.
    /// </summary>
    public string swerveNode { get; set; } = string.Empty;

    #region 人员

    /// <summary>
    /// 指定审批人.
    /// </summary>
    public List<string> approvers { get; set; } = new List<string>();

    /// <summary>
    /// 指定抄送人.
    /// </summary>
    public List<string> circulateUser { get; set; } = new List<string>();

    /// <summary>
    /// 依次审批人.
    /// </summary>
    public List<string> approversSortList { get; set; } = new List<string>();

    /// <summary>
    /// 接口服务.
    /// </summary>
    public FuncConfig interfaceConfig { get; set; } = new FuncConfig();
    #endregion

    #region 消息

    /// <summary>
    /// 审核通过.
    /// </summary>
    public MsgConfig? approveMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核驳回.
    /// </summary>
    public MsgConfig? rejectMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核抄送.
    /// </summary>
    public MsgConfig? copyMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核超时.
    /// </summary>
    public MsgConfig? overTimeMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核提醒.
    /// </summary>
    public MsgConfig? noticeMsgConfig { get; set; } = new MsgConfig();
    #endregion

    #region 节点事件

    /// <summary>
    /// 审核通过事件.
    /// </summary>
    public FuncConfig? approveFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核驳回事件.
    /// </summary>
    public FuncConfig? rejectFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核撤回事件.
    /// </summary>
    public FuncConfig? recallFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核超时事件.
    /// </summary>
    public FuncConfig? overTimeFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核提醒事件.
    /// </summary>
    public FuncConfig? noticeFuncConfig { get; set; } = new FuncConfig();

    #endregion

    #region 按钮

    /// <summary>
    /// 是否保存.
    /// </summary>
    public bool hasSaveBtn { get; set; }

    /// <summary>
    /// 保存按钮.
    /// </summary>
    public string? saveBtnText { get; set; } = "暂 存";

    /// <summary>
    /// 是否打印.
    /// </summary>
    public bool hasPrintBtn { get; set; } = false;

    /// <summary>
    /// 打印.
    /// </summary>
    public string? printBtnText { get; set; } = "打 印";

    /// <summary>
    /// 是否通过.
    /// </summary>
    public bool hasAuditBtn { get; set; } = true;

    /// <summary>
    /// 通过按钮.
    /// </summary>
    public string? auditBtnText { get; set; } = "通 过";

    /// <summary>
    /// 是否拒绝.
    /// </summary>
    public bool hasRejectBtn { get; set; } = true;

    /// <summary>
    /// 拒绝按钮.
    /// </summary>
    public string? rejectBtnText { get; set; } = "退 回";

    /// <summary>
    /// 是否撤回.
    /// </summary>
    public bool hasRevokeBtn { get; set; } = true;

    /// <summary>
    /// 撤回按钮.
    /// </summary>
    public string? revokeBtnText { get; set; } = "撤 回";

    /// <summary>
    /// 是否转办.
    /// </summary>
    public bool hasTransferBtn { get; set; } = true;

    /// <summary>
    /// 转办按钮.
    /// </summary>
    public string? transferBtnText { get; set; } = "转 办";

    /// <summary>
    /// 是否加签.
    /// </summary>
    public bool hasFreeApproverBtn { get; set; } = true;

    /// <summary>
    /// 加签按钮.
    /// </summary>
    public string? hasFreeApproverBtnText { get; set; } = "加 签";

    /// <summary>
    /// 是否驳回.
    /// </summary>
    public bool hasCancelBtn { get; set; } = true;

    /// <summary>
    /// 驳回按钮.
    /// </summary>
    public string? cancelBtnText { get; set; } = "驳 回";
    #endregion

    #region 超时

    /// <summary>
    /// 限时.
    /// </summary>
    public TimeOutConfig? timeLimitConfig { get; set; } = new TimeOutConfig();

    /// <summary>
    /// 超时.
    /// </summary>
    public TimeOutConfig? overTimeConfig { get; set; } = new TimeOutConfig();

    /// <summary>
    /// 提醒.
    /// </summary>
    public TimeOutConfig? noticeConfig { get; set; } = new TimeOutConfig();
    #endregion
}
