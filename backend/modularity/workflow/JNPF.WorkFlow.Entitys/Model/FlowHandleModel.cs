using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model;

[SuppressSniffer]
public class FlowHandleModel : PageInputBase
{
    /// <summary>
    /// 意见.
    /// </summary>
    public string? handleOpinion { get; set; }

    /// <summary>
    /// 加签人.
    /// </summary>
    public string? freeApproverUserId { get; set; }

    /// <summary>
    /// 加签类型 1.前 2 后.
    /// </summary>
    public string? freeApproverType { get; set; }

    /// <summary>
    /// 自定义抄送人.
    /// </summary>
    public string? copyIds { get; set; }

    /// <summary>
    /// 流程编码.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 表单数据.
    /// </summary>
    public object? formData { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 流程监控指派节点.
    /// </summary>
    public string? nodeCode { get; set; }

    /// <summary>
    /// 电子签名.
    /// </summary>
    public string? signImg { get; set; }

    /// <summary>
    /// 候选人.
    /// </summary>
    public Dictionary<string, List<string>>? candidateList { get; set; }

    /// <summary>
    /// 异常处理人.
    /// </summary>
    public Dictionary<string, List<string>>? errorRuleUserList { get; set; }

    /// <summary>
    /// 批量id.
    /// </summary>
    public List<string> ids { get; set; } = new List<string>();

    /// <summary>
    /// 批量类型.
    /// </summary>
    public int batchType { get; set; }

    /// <summary>
    /// 选择分支.
    /// </summary>
    public List<string> branchList { get; set; } = new List<string>();

    /// <summary>
    /// 变更节点.
    /// </summary>
    public string taskNodeId { get; set; }

    /// <summary>
    /// 任务id.
    /// </summary>
    public string taskId { get; set; }

    /// <summary>
    /// 任务id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// false 变更 true 复活.
    /// </summary>
    public bool resurgence { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    public List<object> fileList { get; set; } = new List<object>();

    /// <summary>
    /// 驳回节点.
    /// </summary>
    public string rejectStep { get; set; }

    /// <summary>
    /// true 全部 flase 主流程.
    /// </summary>
    public bool suspend { get; set; }

    /// <summary>
    /// 驳回类型.
    /// </summary>
    public string rejectType { get; set; }

    /// <summary>
    /// 会签是否完成.
    /// </summary>
    public bool countersignOver { get; set; }
}
