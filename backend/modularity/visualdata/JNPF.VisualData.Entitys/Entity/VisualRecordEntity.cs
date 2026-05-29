using JNPF.Common.Const;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.VisualData.Entity;

/// <summary>
/// 可视化数据源表.
/// </summary>
[SugarTable("BLADE_VISUAL_RECORD")]
[Tenant(ClaimConst.TENANTID)]
public class VisualRecordEntity : ITenantFilter
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "ID", ColumnDescription = "主键", IsPrimaryKey = true)]
    public string Id { get; set; }

    /// <summary>
    /// 组件名称.
    /// </summary>
    [SugarColumn(ColumnName = "NAME", ColumnDescription = "组件名称")]
    public string Name { get; set; }

    /// <summary>
    /// url.
    /// </summary>
    [SugarColumn(ColumnName = "URL", ColumnDescription = "url")]
    public string Url { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    [SugarColumn(ColumnName = "DATATYPE", ColumnDescription = "数据类型")]
    public int? DataType { get; set; }

    /// <summary>
    /// 数据方法.
    /// </summary>
    [SugarColumn(ColumnName = "DATAMETHOD", ColumnDescription = "组件名称")]
    public string DataMethod { get; set; }

    /// <summary>
    /// 数据头部.
    /// </summary>
    [SugarColumn(ColumnName = "DATAHEADER", ColumnDescription = "数据头部")]
    public string DataHeader { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    [SugarColumn(ColumnName = "DATA", ColumnDescription = "数据")]
    public string Data { get; set; }

    /// <summary>
    /// 数据查询.
    /// </summary>
    [SugarColumn(ColumnName = "DATAQUERY", ColumnDescription = "数据查询")]
    public string DataQuery { get; set; }

    /// <summary>
    /// 数据查询类型.
    /// </summary>
    [SugarColumn(ColumnName = "DATAQUERYTYPE", ColumnDescription = "数据查询类型")]
    public string DataQueryType { get; set; }

    /// <summary>
    /// 数据格式化程序.
    /// </summary>
    [SugarColumn(ColumnName = "DATAFORMATTER", ColumnDescription = "数据格式化程序")]
    public string DataFormatter { get; set; }

    /// <summary>
    /// 数据格式化程序.
    /// </summary>
    [SugarColumn(ColumnName = "PROXY", ColumnDescription = "是否跨域")]
    public int Proxy { get; set; } = 0;

    /// <summary>
    /// WS_URL.
    /// </summary>
    [SugarColumn(ColumnName = "WSURL", ColumnDescription = "WS_URL")]
    public string WsUrl { get; set; }

    /// <summary>
    /// 数据库sql.
    /// </summary>
    [SugarColumn(ColumnName = "DBSQL", ColumnDescription = "数据库sql")]
    public string Dbsql { get; set; }

    /// <summary>
    /// fsql.
    /// </summary>
    [SugarColumn(ColumnName = "FSQL", ColumnDescription = "Sql")]
    public string Fsql { get; set; }

    /// <summary>
    /// 结果.
    /// </summary>
    [SugarColumn(ColumnName = "RESULT", ColumnDescription = "结果")]
    public string Result { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    public string TenantId { get; set; }
}
