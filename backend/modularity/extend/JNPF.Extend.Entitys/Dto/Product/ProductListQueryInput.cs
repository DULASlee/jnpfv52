using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.Product;

/// <summary>
/// 销售订单列表.
/// </summary>
[SuppressSniffer]
public class ProductListQueryInput : PageInputBase
{
    /// <summary>
    /// 订单编号.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    public string customerName { get; set; }

    /// <summary>
    /// 联系方式.
    /// </summary>
    public string contactTel { get; set; }

    /// <summary>
    /// 审核状态.
    /// </summary>
    public string auditState { get; set; }

    /// <summary>
    /// 关闭状态.
    /// </summary>
    public string closeState { get; set; }

    /// <summary>
    /// 制单人.
    /// </summary>
    public string creatorUser { get; set; }
}