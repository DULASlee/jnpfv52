using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.Item;

[SuppressSniffer]
public class AssignItem
{
    /// <summary>
    /// 上一节点编码.
    /// </summary>
    public string? nodeId { get; set; }

    /// <summary>
    /// 上一节点名称.
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    /// 父流程字段.
    /// </summary>
    public List<RuleItem> ruleList { get; set; }
}

public class RuleItem
{
    /// <summary>
    /// 父流程字段.
    /// </summary>
    public string? parentField { get; set; }

    /// <summary>
    /// 子流程字段.
    /// </summary>
    public string? childField { get; set; }
}
