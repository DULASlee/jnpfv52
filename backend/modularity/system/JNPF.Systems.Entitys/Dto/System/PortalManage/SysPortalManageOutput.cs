using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.PortalManage;

/// <summary>
/// 获取系统下的门户管理输出.
/// </summary>
[SuppressSniffer]
public class SysPortalManageOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 平台.
    /// </summary>
    public string platform { get; set; }
}
