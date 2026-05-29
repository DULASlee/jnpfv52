using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.System;

/// <summary>
/// 行政区划.
/// </summary>
[SugarTable("BASE_PROVINCE_ATLAS")]
public class ProvinceAtlasEntity : CLDSEntityBase
{
    /// <summary>
    /// 区域上级.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 区域编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 区域名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 快速查询.
    /// </summary>
    [SugarColumn(ColumnName = "F_QUICK_QUERY")]
    public string QuickQuery { get; set; }

    /// <summary>
    /// 区域类型：1-省份、2-城市、3-县区、4-街道.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public string Type { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 行政区划编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_DIVISION_CODE")]
    public string DivisionCode { get; set; }

    /// <summary>
    /// 中心经纬度.
    /// </summary>
    [SugarColumn(ColumnName = "F_ATLAS_CENTER")]
    public string AtlasCenter { get; set; }
}
