using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 打印模板配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_PRINT_TEMPLATE")]
[Tenant(ClaimConst.TENANTID)]
public class PrintDevEntity : CLDSEntityBase
{
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
    /// 分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 数据连接id.
    /// </summary>
    [SugarColumn(ColumnName = "F_DB_LINK_ID")]
    public string DbLinkId { get; set; }

    /// <summary>
    /// sql模板.
    /// </summary>
    [SugarColumn(ColumnName = "F_SQL_TEMPLATE")]
    public string SqlTemplate { get; set; }

    /// <summary>
    /// 左侧字段.
    /// </summary>
    [SugarColumn(ColumnName = "F_LEFT_FIELDS")]
    public string LeftFields { get; set; }

    /// <summary>
    /// 打印模板.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRINT_TEMPLATE")]
    public string PrintTemplate { get; set; }

    /// <summary>
    /// 纸张参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_PAGE_PARAM")]
    public string PageParam { get; set; }

    /// <summary>
    /// 数据来源.
    /// </summary>
    [SugarColumn(ColumnName = "F_SOURCE_TYPE")]
    public int sourceType { get; set; }

    /// <summary>
    /// 数据接口id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTERFACE_ID")]
    public string InterfaceId { get; set; }

    /// <summary>
    /// 数据接口参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARAMETER_JSON")]
    public string ParameterJson { get; set; }
}