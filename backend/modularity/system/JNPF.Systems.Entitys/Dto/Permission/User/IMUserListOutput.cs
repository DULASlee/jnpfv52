using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// IM通讯录.
/// </summary>
[SuppressSniffer]
public class IMUserListOutput
{
    /// <summary>
    /// 用户编号.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 用户账号.
    /// </summary>
    public string account { get; set; }

    /// <summary>
    /// 用户名称.
    /// </summary>
    public string realName { get; set; }

    /// <summary>
    /// 用户头像.
    /// </summary>
    public string headIcon { get; set; }

    /// <summary>
    /// 用户部门.
    /// </summary>
    public string department { get; set; }
}