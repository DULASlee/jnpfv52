using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.ProductEntry;

/// <summary>
/// 销售订单信息.
/// </summary>
[SuppressSniffer]
public class ProductEntryInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 产品编号.
    /// </summary>
    public string productCode { get; set; }

    /// <summary>
    /// 产品名称.
    /// </summary>
    public string productName { get; set; }

    /// <summary>
    /// 产品规格.
    /// </summary>
    public string productSpecification { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public int qty { get; set; }

    /// <summary>
    /// 订货类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 单价.
    /// </summary>
    public decimal money { get; set; }

    /// <summary>
    /// 折后单价.
    /// </summary>
    public decimal price { get; set; }

    /// <summary>
    /// 金额.
    /// </summary>
    public decimal amount { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string description { get; set; }
}