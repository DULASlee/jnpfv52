using JNPF.DependencyInjection;
using JNPF.Extend.Entitys.Model;

namespace JNPF.Extend.Entitys.Dto.ProductEntry;

/// <summary>
/// 产品明细.
/// </summary>
[SuppressSniffer]
public class ProductEntryListOutput
{
    /// <summary>
    /// 产品编号.
    /// </summary>
    public string productCode { get; set; }

    /// <summary>
    /// 产品名称.
    /// </summary>
    public string productName { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public int qty { get; set; }

    /// <summary>
    /// 订货类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 活动.
    /// </summary>
    public string activity { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public List<ProductEntryMdoel> dataList { get; set; }
}