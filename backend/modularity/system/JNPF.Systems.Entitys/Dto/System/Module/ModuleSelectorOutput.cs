using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 功能下拉框输出.
/// </summary>
[SuppressSniffer]
public class ModuleSelectorOutput : TreeModel
{
    /// <summary>
    /// 菜单名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 系统Id.
    /// </summary>
    public string systemId { get; set; }
}