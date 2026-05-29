using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.PortalManage;

/// <summary>
/// 门户管理信息.
/// </summary>
[SuppressSniffer]
public class PortalManageInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 门户id.
    /// </summary>
    public string portalId { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 门户名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 分类ID.
    /// </summary>
    public string categoryId { get; set; }

    /// <summary>
    /// 分类名称.
    /// </summary>
    public string categoryName { get; set; }

    /// <summary>
    /// 按钮状态(1-可用,0-不可用).
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

}
