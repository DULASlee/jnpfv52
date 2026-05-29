using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.VisualDev.Entitys;

/// <summary>
/// 门户表.
/// </summary>
[SugarTable("BASE_PORTAL")]
[Tenant(ClaimConst.TENANTID)]
public class PortalEntity : CLDSEntityBase
{
    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 分类(数据字典维护).
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 类型(0-页面设计,1-自定义路径).
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 静态页面路径.
    /// </summary>
    [SugarColumn(ColumnName = "F_CUSTOM_URL")]
    public string CustomUrl { get; set; }

    /// <summary>
    /// 链接类型(0-页面,1-外链).
    /// </summary>
    [SugarColumn(ColumnName = "F_LINK_TYPE")]
    public int? LinkType { get; set; }

    /// <summary>
    /// 锁定（0-锁定，1-自定义）.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENABLED_LOCK")]
    public int? EnabledLock { get; set; }

    /// <summary>
    /// 状态（0-未发步，1-已发布，2-已修改）.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATE")]
    public int? State { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    [SugarColumn(ColumnName = "F_PLATFORM_RELEASE")]
    public string PlatformRelease { get; set; }
}