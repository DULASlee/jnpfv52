using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 产品明细.
/// </summary>
[SugarTable("EXT_PRODUCT_ENTRY")]
[Tenant(ClaimConst.TENANTID)]
public class ProductEntryEntity : CLDEntityBase
{
    /// <summary>
    /// 订单主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRODUCT_ID")]
    public string ProductId { get; set; }

    /// <summary>
    /// 产品编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRODUCT_CODE")]
    public string ProductCode { get; set; }

    /// <summary>
    /// 产品名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRODUCT_NAME")]
    public string ProductName { get; set; }

    /// <summary>
    /// 产品规格.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRODUCT_SPECIFICATION")]
    public string ProductSpecification { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    [SugarColumn(ColumnName = "F_QTY")]
    public int Qty { get; set; }

    /// <summary>
    /// 控制方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_COMMAND_TYPE")]
    public string CommandType { get; set; }

    /// <summary>
    /// 订货类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public string Type { get; set; }

    /// <summary>
    /// 单价.
    /// </summary>
    [SugarColumn(ColumnName = "F_MONEY")]
    public decimal Money { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    [SugarColumn(ColumnName = "F_UTIL")]
    public string Util { get; set; }

    /// <summary>
    /// 折后单价.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRICE")]
    public decimal Price { get; set; }

    /// <summary>
    /// 金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_AMOUNT")]
    public decimal Amount { get; set; }

    /// <summary>
    /// 活动.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACTIVITY")]
    public string Activity { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}