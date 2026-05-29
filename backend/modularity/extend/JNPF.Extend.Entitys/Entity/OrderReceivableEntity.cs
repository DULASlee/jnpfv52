using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 订单收款
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("EXT_ORDER_RECEIVABLE")]
public class OrderReceivableEntity : CLDEntityBase
{
    /// <summary>
    /// 订单主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORDER_ID")]
    public string? OrderId { get; set; }

    /// <summary>
    /// 收款摘要.
    /// </summary>
    [SugarColumn(ColumnName = "F_ABSTRACT")]
    public string? Abstract { get; set; }

    /// <summary>
    /// 收款日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_RECEIVABLE_DATE")]
    public DateTime? ReceivableDate { get; set; }

    /// <summary>
    /// 收款比率.
    /// </summary>
    [SugarColumn(ColumnName = "F_RECEIVABLE_RATE")]
    public decimal? ReceivableRate { get; set; }

    /// <summary>
    /// 收款金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_RECEIVABLE_MONEY")]
    public decimal? ReceivableMoney { get; set; }

    /// <summary>
    /// 收款方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_RECEIVABLE_MODE")]
    public string? ReceivableMode { get; set; }

    /// <summary>
    /// 收款状态.
    /// </summary>
    [SugarColumn(ColumnName = "F_RECEIVABLE_STATE")]
    public int? ReceivableState { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
