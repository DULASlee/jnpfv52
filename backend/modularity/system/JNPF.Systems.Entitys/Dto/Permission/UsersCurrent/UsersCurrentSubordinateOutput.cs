using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.UsersCurrent;

/// <summary>
/// 个人资料下属输出.
/// </summary>
[SuppressSniffer]
public class UsersCurrentSubordinateOutput
{
    /// <summary>
    /// 用户id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 头像地址.
    /// </summary>
    public string avatar { get; set; }

    /// <summary>
    /// 用户名称.
    /// </summary>
    public string userName { get; set; }

    /// <summary>
    /// 部门.
    /// </summary>
    public string department { get; set; }

    /// <summary>
    /// 岗位.
    /// </summary>
    public string position { get; set; }

    /// <summary>
    /// 是否有下级.
    /// </summary>
    public bool isLeaf { get; set; }
}