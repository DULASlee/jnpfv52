using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 销售订单.
/// </summary>
[SugarTable("EXT_PRODUCT")]
[Tenant(ClaimConst.TENANTID)]
public class ProductEntity : CLDEntityBase
{
    /// <summary>
    /// 订单编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 客户类别.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public string Type { get; set; }

    /// <summary>
    /// 客户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_CUSTOMER_ID")]
    public string CustomerId { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_CUSTOMER_NAME")]
    public string CustomerName { get; set; }

    /// <summary>
    /// 制单人id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SALESMAN_ID")]
    public string SalesmanId { get; set; }

    /// <summary>
    /// 制单人名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_SALESMAN_NAME")]
    public string SalesmanName { get; set; }

    /// <summary>
    /// 制单日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_SALESMAN_DATE")]
    public DateTime? SalesmanDate { get; set; }

    /// <summary>
    /// 审核人.
    /// </summary>
    [SugarColumn(ColumnName = "F_AUDIT_NAME")]
    public string AuditName { get; set; }

    /// <summary>
    /// 审核日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_AUDIT_DATE")]
    public DateTime? AuditDate { get; set; }

    /// <summary>
    /// 审核状态.
    /// </summary>
    [SugarColumn(ColumnName = "F_AUDIT_STATE")]
    public int AuditState { get; set; }

    /// <summary>
    /// 发货仓库.
    /// </summary>
    [SugarColumn(ColumnName = "F_GOODS_WAREHOUSE")]
    public string GoodsWarehouse { get; set; }

    /// <summary>
    /// 发货通知时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_GOODS_DATE")]
    public DateTime? GoodsDate { get; set; }

    /// <summary>
    /// 发货通知人.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONSIGNOR")]
    public string Consignor { get; set; }

    /// <summary>
    /// 发货状态.
    /// </summary>
    [SugarColumn(ColumnName = "F_GOODS_STATE")]
    public int GoodsState { get; set; }

    /// <summary>
    /// 关闭状态.
    /// </summary>
    [SugarColumn(ColumnName = "F_CLOSE_STATE")]
    public int CloseState { get; set; }

    /// <summary>
    /// 关闭日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_CLOSE_DATE")]
    public DateTime? CloseDate { get; set; }

    /// <summary>
    /// 收款方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_GATHERING_TYPE")]
    public string GatheringType { get; set; }

    /// <summary>
    /// 业务员.
    /// </summary>
    [SugarColumn(ColumnName = "F_BUSINESS")]
    public string Business { get; set; }

    /// <summary>
    /// 送货地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_ADDRESS")]
    public string Address { get; set; }

    /// <summary>
    /// 联系方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTACT_TEL")]
    public string ContactTel { get; set; }

    /// <summary>
    /// 联系人.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTACT_NAME")]
    public string ContactName { get; set; }

    /// <summary>
    /// 收货消息.
    /// </summary>
    [SugarColumn(ColumnName = "F_HARVEST_MSG")]
    public int HarvestMsg { get; set; }

    /// <summary>
    /// 收货仓库.
    /// </summary>
    [SugarColumn(ColumnName = "F_HARVEST_WAREHOUSE")]
    public string HarvestWarehouse { get; set; }

    /// <summary>
    /// 代发客户.
    /// </summary>
    [SugarColumn(ColumnName = "F_ISSUING_NAME")]
    public string IssuingName { get; set; }

    /// <summary>
    /// 让利金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_PART_PRICE")]
    public decimal? PartPrice { get; set; }

    /// <summary>
    /// 优惠金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_REDUCED_PRICE")]
    public decimal? ReducedPrice { get; set; }

    /// <summary>
    /// 折后金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_DISCOUNT_PRICE")]
    public decimal? DiscountPrice { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    [SugarColumn(ColumnName = "F_Description")]
    public string Description { get; set; }

    /// <summary>
    /// 订单明细.
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(ProductEntryEntity.ProductId), nameof(Id))]
    public List<ProductEntryEntity> productEntryList { get; set; }
}