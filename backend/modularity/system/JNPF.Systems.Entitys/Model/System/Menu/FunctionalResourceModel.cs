using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.Menu;

/// <summary>
/// 模块资源权限模型.
/// </summary>
[SuppressSniffer]
public class FunctionalResourceModel : FunctionalBase
{
    /// <summary>
    /// 条件规则Json.
    /// </summary>
    public string ConditionJson { get; set; }

    /// <summary>
    /// 条件规则描述.
    /// </summary>
    public string ConditionText { get; set; }

    /// <summary>
    /// 功能主键.
    /// </summary>
    public string ModuleId { get; set; }
}