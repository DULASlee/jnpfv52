using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.PortalManage;

/// <summary>
/// 门户管理列表输出.
/// </summary>
[SuppressSniffer]
public class PortalManageListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 门户名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 分类id.
    /// </summary>
    public string categoryId { get; set; }

    /// <summary>
    /// 分类名称.
    /// </summary>
    public string categoryName { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 状态(1-可用,0-不可用).
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 门户id.
    /// </summary>
    public string portalId { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改人.
    /// </summary>
    public string lastModifyUser { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

}
