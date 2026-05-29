using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.ProductClassify;

/// <summary>
/// 产品分类.
/// </summary>
[SuppressSniffer]
public class ProductClassifyTreeOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    /// <returns></returns>
    public string fullName { get; set; }
}
