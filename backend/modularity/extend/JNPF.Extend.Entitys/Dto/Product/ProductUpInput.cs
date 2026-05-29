using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.Product;

/// <summary>
/// 订单示例更新输入.
/// </summary>
[SuppressSniffer]
public class ProductUpInput : ProductCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}