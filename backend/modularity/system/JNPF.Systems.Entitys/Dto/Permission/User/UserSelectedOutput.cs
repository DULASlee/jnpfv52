using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 选择用户输出.
/// </summary>
[SuppressSniffer]
public class UserSelectedOutput
{
    /// <summary>
    /// Id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 前端协议字段，以后将改回realName.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 用户头像.
    /// </summary>
    public string headIcon { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    public string gender { get; set; }

    /// <summary>
    /// 手机号.
    /// </summary>
    public string mobilePhone { get; set; }

    /// <summary>
    /// 组织信息.
    /// </summary>
    public string organize { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 组织id树.
    /// </summary>
    public List<string> organizeIds { get; set; }

    /// <summary>
    /// 类型..
    /// </summary>
    public string type { get; set; }
}