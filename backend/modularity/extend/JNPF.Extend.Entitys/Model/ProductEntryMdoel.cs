using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Model;

/// <summary>
/// 产品明细.
/// </summary>
[SuppressSniffer]
public class ProductEntryMdoel
{
    /// <summary>
    /// 产品规格.
    /// </summary>
    public string productSpecification { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public string qty { get; set; }

    /// <summary>
    /// 单价.
    /// </summary>
    public decimal money { get; set; }

    /// <summary>
    /// 折后单价.
    /// </summary>
    public decimal price { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string util { get; set; }

    /// <summary>
    /// 控制方式.
    /// </summary>
    public string commandType { get; set; }
}