using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.Integrate;

/// <summary>
/// 集成助手任务纲要.
/// </summary>
[SuppressSniffer]
public class InteAssiTaskOutlineModel
{
    /// <summary>
    /// 集成主键.
    /// </summary>
    public string integrateId { get; set; }

    /// <summary>
    /// 集成助手类型 
    /// 1-触发,2-定时.
    /// </summary>
    public int inteAssisType { get; set; }

    /// <summary>
    /// 触发表单/接口id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 触发事件
    /// 1-新增,2-修改,3-删除.
    /// </summary>
    public int triggerEvent { get; set; }

    /// <summary>
    /// 触发条件规则.
    /// </summary>
    public List<object> ruleList { get; set; }

    /// <summary>
    /// 触发条件规则匹配逻辑.
    /// </summary>
    public string ruleMatchLogic { get; set; }

    /// <summary>
    /// 节点属性.
    /// </summary>
    public List<TaskOutlineNode> nodeAttributes { get; set; }

    /// <summary>
    /// 触发开始时间.
    /// </summary>
    public DateTime? startTime { get; set; }
}

/// <summary>
/// 任务纲要节点.
/// </summary>
[SuppressSniffer]
public class TaskOutlineNode : NodeGenerality
{
    /// <summary>
    /// 类型.
    /// </summary>
    public NodeType type { get; set; }

    /// <summary>
    /// 节点ID.
    /// </summary>
    public string nodeId { get; set; }

    /// <summary>
    /// 下个节点ID.
    /// </summary>
    public string nextNodeId { get; set; }

    /// <summary>
    /// 通知人.
    /// </summary>
    public List<string> msgUserIds { get; set; }
}