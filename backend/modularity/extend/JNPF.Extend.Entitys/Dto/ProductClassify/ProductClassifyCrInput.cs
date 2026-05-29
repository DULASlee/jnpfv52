using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.ProductClassify;

/// <summary>
/// 产品分类.
/// </summary>
[SuppressSniffer]
public class ProductClassifyCrInput
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 上级.
    /// </summary>
    public string? parentId { get; set; }
}