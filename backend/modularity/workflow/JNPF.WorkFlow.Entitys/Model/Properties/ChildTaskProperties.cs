using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

[SuppressSniffer]
public class ChildTaskProperties
{
    /// <summary>
    /// 子流程标题.
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    /// 子流程发起人（类型参考FlowTaskOperatorEnum类）.
    /// </summary>
    public int initiateType { get; set; }

    /// <summary>
    /// 主管级别.
    /// </summary>
    public int managerLevel { get; set; }

    /// <summary>
    /// 主管级别.
    /// </summary>
    public int departmentLevel { get; set; }

    /// <summary>
    /// 自定义人员.
    /// </summary>
    public List<string>? initiator { get; set; }

    /// <summary>
    /// 子流程引擎.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 继承父流程字段数据.
    /// </summary>
    public List<AssignItem>? assignList { get; set; }

    /// <summary>
    /// 子流程节点下子流程任务id.
    /// </summary>
    public List<string> childTaskId { get; set; } = new List<string>();

    /// <summary>
    /// 子流程数据.
    /// </summary>
    public string? formData { get; set; }

    /// <summary>
    /// 同步异步(异步:true).
    /// </summary>
    public bool isAsync { get; set; }

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
    /// 发起通知.
    /// </summary>
    public MsgConfig? launchMsgConfig { get; set; }

    /// <summary>
    /// // 表单字段审核方式的类型(1-用户 2-部门).
    /// </summary>
    public int formFieldType { get; set; }

    /// <summary>
    /// 异常处理规则
    /// 1:超级管理员处理、2:指定人员处理、3:上一节点审批人指定处理人、4:默认审批通过、5:无法提交、6：流程发起人.
    /// </summary>
    public string errorRule { get; set; } = "1";

    /// <summary>
    /// 异常处理人.
    /// </summary>
    public List<string>? errorRuleUser { get; set; } = new List<string>();

    /// <summary>
    /// 自动提交.
    /// </summary>
    public bool autoSubmit { get; set; }

    /// <summary>
    /// 接口服务.
    /// </summary>
    public FuncConfig interfaceConfig { get; set; } = new FuncConfig();
}
