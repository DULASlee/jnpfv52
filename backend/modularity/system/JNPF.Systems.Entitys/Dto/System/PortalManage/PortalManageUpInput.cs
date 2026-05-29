using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.PortalManage;

/// <summary>
/// 门户管理新建输入.
/// </summary>
[SuppressSniffer]
public class PortalManageUpInput : PortalManageCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}
