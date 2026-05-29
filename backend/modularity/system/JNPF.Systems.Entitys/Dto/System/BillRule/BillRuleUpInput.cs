using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.BillRule;

/// <summary>
/// 单据规则修改输入.
/// </summary>
[SuppressSniffer]
public class BillRuleUpInput : BillRuleCrInput
{
    /// <summary>
    /// id
    /// </summary>
    public string id { get; set; }
}