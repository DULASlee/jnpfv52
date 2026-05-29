using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 订单明细
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("EXT_ORDER_ENTRY")]
public class OrderEntryEntity : CLDEntityBase
{
    /// <summary>
    /// 订单主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORDER_ID")]
    public string? OrderId { get; set; }

    /// <summary>
    /// 商品Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_GOODS_ID")]
    public string? GoodsId { get; set; }

    /// <summary>
    /// 商品编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_GOODS_CODE")]
    public string? GoodsCode { get; set; }

    /// <summary>
    /// 商品名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_GOODS_NAME")]
    public string? GoodsName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    [SugarColumn(ColumnName = "F_SPECIFICATIONS")]
    public string? Specifications { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    [SugarColumn(ColumnName = "F_UNIT")]
    public string? Unit { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    [SugarColumn(ColumnName = "F_QTY")]
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单价.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRICE")]
    public decimal? Price { get; set; }

    /// <summary>
    /// 金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_AMOUNT")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// 折扣%.
    /// </summary>
    [SugarColumn(ColumnName = "F_DISCOUNT")]
    public decimal? Discount { get; set; }

    /// <summary>
    /// 税率%.
    /// </summary>
    [SugarColumn(ColumnName = "F_CESS")]
    public decimal? Cess { get; set; }

    /// <summary>
    /// 实际单价.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACTUAL_PRICE")]
    public decimal? ActualPrice { get; set; }

    /// <summary>
    /// 实际金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACTUAL_AMOUNT")]
    public decimal? ActualAmount { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
