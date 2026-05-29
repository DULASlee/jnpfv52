using JNPF.DependencyInjection;
using JNPF.Extend.Entitys.Dto.ProductEntry;

namespace JNPF.Extend.Entitys.Dto.Product;

/// <summary>
/// 新建销售订单.
/// </summary>
[SuppressSniffer]
public class ProductCrInput
{
    /// <summary>
    /// 订单编号.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 客户id.
    /// </summary>
    public string customerId { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    public string customerName { get; set; }

    /// <summary>
    /// 审核人.
    /// </summary>
    public string auditName { get; set; }

    /// <summary>
    /// 审核日期.
    /// </summary>
    public DateTime? auditDate { get; set; }

    /// <summary>
    /// 发货仓库.
    /// </summary>
    public string goodsWarehouse { get; set; }

    /// <summary>
    /// 发货通知时间.
    /// </summary>
    public DateTime? goodsDate { get; set; }

    /// <summary>
    /// 发货通知人.
    /// </summary>
    public string goodsName { get; set; }

    /// <summary>
    /// 收款方式.
    /// </summary>
    public string gatheringType { get; set; }

    /// <summary>
    /// 业务员.
    /// </summary>
    public string business { get; set; }

    /// <summary>
    /// 送货地址.
    /// </summary>
    public string address { get; set; }

    /// <summary>
    /// 联系方式.
    /// </summary>
    public string contactTel { get; set; }

    /// <summary>
    /// 收货消息.
    /// </summary>
    public int harvestMsg { get; set; }

    /// <summary>
    /// 收货仓库.
    /// </summary>
    public string harvestWarehouse { get; set; }

    /// <summary>
    /// 代发客户.
    /// </summary>
    public string issuingName { get; set; }

    /// <summary>
    /// 让利金额.
    /// </summary>
    public decimal? partPrice { get; set; }

    /// <summary>
    /// 优惠金额.
    /// </summary>
    public decimal? reducedPrice { get; set; }

    /// <summary>
    /// 折后金额.
    /// </summary>
    public decimal? discountPrice { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 子表数据.
    /// </summary>
    public List<ProductEntryCrInput> productEntryList { get; set; }
}