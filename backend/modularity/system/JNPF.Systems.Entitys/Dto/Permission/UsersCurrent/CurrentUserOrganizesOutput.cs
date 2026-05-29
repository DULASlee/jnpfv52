using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.UsersCurrent;

/// <summary>
/// 当前用户 所属组织 输出.
/// </summary>
[SuppressSniffer]
public class CurrentUserOrganizesOutput
{
    /// <summary>
    /// 主键Id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 是否默认 0：否，1：是.
    /// </summary>
    public bool isDefault { get; set; } = false;
}