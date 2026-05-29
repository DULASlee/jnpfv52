using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

[SuppressSniffer]
public class ConditionProperties
{
    /// <summary>
    /// 标题.
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    /// 条件明细.
    /// </summary>
    public List<GropsItem>? conditions { get; set; }

    /// <summary>
    /// 是否默认.
    /// </summary>
    public bool isDefault { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string? matchLogic { get; set; }

    /// <summary>
    /// 条件类型 0-默认 1-转向.
    /// </summary>
    public int conditionType { get; set; }

    /// <summary>
    /// 转向节点.
    /// </summary>
    public string swerveNode { get; set; }
}
