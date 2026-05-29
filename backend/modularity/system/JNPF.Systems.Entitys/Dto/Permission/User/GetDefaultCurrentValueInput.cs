using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 当前用户默认值信息输出.
/// </summary>
[SuppressSniffer]
public class GetDefaultCurrentValueInput
{
    /// <summary>
    /// 部门Ids.
    /// </summary>
    public List<string> DepartIds { get; set; }

    /// <summary>
    /// 用户Ids.
    /// </summary>
    public List<string> UserIds { get; set; }

    /// <summary>
    /// 角色Ids.
    /// </summary>
    public List<string> RoleIds { get; set; }

    /// <summary>
    /// 岗位Ids.
    /// </summary>
    public List<string> PositionIds { get; set; }

    /// <summary>
    /// 分组Ids.
    /// </summary>
    public List<string> GroupIds { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string Keyword { get; set; }
}