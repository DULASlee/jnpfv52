using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Role;

/// <summary>
/// 角色列表输入.
/// </summary>
[SuppressSniffer]
public class RoleListInput : CommonInput
{
    /// <summary>
    /// 机构ID 0 :全局，其他：组织.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 角色类型.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 组织Id集合.
    /// </summary>
    public List<string> organizeIds { get; set; }

    /// <summary>
    /// 角色Id.
    /// </summary>
    public string roleId { get; set; }
}