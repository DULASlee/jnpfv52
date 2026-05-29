using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.PortalManage;

/// <summary>
/// 门户管理新建输入.
/// </summary>
[SuppressSniffer]
public class PortalManageCrInput
{
    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 门户id.
    /// </summary>
    public string portalId { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 状态(1-可用,0-不可用).
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// WEB:网页端 APP:手机端.
    /// </summary>
    public string platform { get; set; }
}
