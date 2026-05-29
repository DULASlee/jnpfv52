using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 订单信息
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("EXT_ORDER")]
public class OrderEntity : CLDSEntityBase
{
    /// <summary>
    /// 客户Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_CUSTOMER_ID")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_CUSTOMER_NAME")]
    public string? CustomerName { get; set; }

    /// <summary>
    /// 业务员Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SALESMAN_ID")]
    public string? SalesmanId { get; set; }

    /// <summary>
    /// 业务员.
    /// </summary>
    [SugarColumn(ColumnName = "F_SALESMAN_NAME")]
    public string? SalesmanName { get; set; }

    /// <summary>
    /// 订单日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORDER_DATE")]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 订单编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORDER_CODE")]
    public string? OrderCode { get; set; }

    /// <summary>
    /// 运输方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_TRANSPORT_MODE")]
    public string? TransportMode { get; set; }

    /// <summary>
    /// 发货日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_DELIVERY_DATE")]
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 发货地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_DELIVERY_ADDRESS")]
    public string? DeliveryAddress { get; set; }

    /// <summary>
    /// 付款方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_PAYMENT_MODE")]
    public string? PaymentMode { get; set; }

    /// <summary>
    /// 应收金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_RECEIVABLE_MONEY")]
    public decimal? ReceivableMoney { get; set; }

    /// <summary>
    /// 定金比率.
    /// </summary>
    [SugarColumn(ColumnName = "F_EARNEST_RATE")]
    public decimal? EarnestRate { get; set; }

    /// <summary>
    /// 预付定金.
    /// </summary>
    [SugarColumn(ColumnName = "F_PREPAY_EARNEST")]
    public decimal? PrepayEarnest { get; set; }

    /// <summary>
    /// 当前状态.
    /// </summary>
    [SugarColumn(ColumnName = "F_CURRENT_STATE")]
    public int? CurrentState { get; set; }

    /// <summary>
    /// 附件信息.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILE_JSON")]
    public string? FileJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }
}
