using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SysConfig;

/// <summary>
/// 用户信息输出.
/// </summary>
[SuppressSniffer]
public class AdminUserOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 账户.
    /// </summary>
    public string account { get; set; }

    /// <summary>
    /// 用户姓名.
    /// </summary>
    public string realName { get; set; }
}
