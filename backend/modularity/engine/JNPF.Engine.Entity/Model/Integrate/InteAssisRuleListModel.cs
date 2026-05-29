using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.Integrate;

/// <summary>
/// 集成助手 规则列表模型.
/// </summary>
[SuppressSniffer]
public class InteAssisRuleListModel
{
    public List<GroupRule> groups { get; set; }
}

/// <summary>
/// 组规则.
/// </summary>
[SuppressSniffer]
public class GroupRule
{
    public string fieldValue { get; set; }
}