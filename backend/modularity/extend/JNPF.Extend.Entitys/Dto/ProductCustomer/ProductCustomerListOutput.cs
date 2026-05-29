using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.Customer;

/// <summary>
/// 客户信息.
/// </summary>
[SuppressSniffer]
public class ProductCustomerListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 客户编号.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    public string customerName { get; set; }

    /// <summary>
    /// 地址.
    /// </summary>
    public string address { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 联系方式.
    /// </summary>
    public string contactTel { get; set; }
}
