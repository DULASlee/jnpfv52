using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Role;

/// <summary>
/// 获取角色列表输入.
/// </summary>
[SuppressSniffer]
public class RoleConditionInput : KeywordInput
{
    /// <summary>
    /// 选择的角色id.
    /// </summary>
    public List<string> ids { get; set; } = new List<string>();
}