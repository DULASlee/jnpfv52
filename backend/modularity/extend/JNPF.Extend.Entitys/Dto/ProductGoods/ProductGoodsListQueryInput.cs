using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.ProductGoods;

/// <summary>
/// 产品列表.
/// </summary>
[SuppressSniffer]
public class ProductGoodsListQueryInput : PageInputBase
{
    /// <summary>
    /// 订单编号.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 产品名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 分类ID.
    /// </summary>
    public string classifyId { get; set; }
}
