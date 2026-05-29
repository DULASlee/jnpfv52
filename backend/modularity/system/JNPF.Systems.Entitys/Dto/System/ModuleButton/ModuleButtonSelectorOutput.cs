using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleButton;

/// <summary>
/// 功能按钮下拉框输出.
/// </summary>
[SuppressSniffer]
public class ModuleButtonSelectorOutput : TreeModel
{
    /// <summary>
    /// 按钮名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 按钮图标.
    /// </summary>
    public string icon { get; set; }
}