using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.ProductGoods;

/// <summary>
/// 产品列表.
/// </summary>
[SuppressSniffer]
public class ProductGoodsListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 分类主键.
    /// </summary>
    public string classifyId { get; set; }

    /// <summary>
    /// 订单编号.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 订单名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 订单名称.
    /// </summary>
    public int qty { get; set; }

    /// <summary>
    /// 订货类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 金额.
    /// </summary>
    public string amount { get; set; }

    /// <summary>
    /// 单价.
    /// </summary>
    public string money { get; set; }
}