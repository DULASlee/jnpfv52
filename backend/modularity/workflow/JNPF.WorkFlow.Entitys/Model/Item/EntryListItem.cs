using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.Item;

[SuppressSniffer]
public class EntryListItem
{
    /// <summary>
    /// 金额.
    /// </summary>
    public decimal? amount { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 商品名.
    /// </summary>
    public string goodsName { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 关联id.
    /// </summary>
    public string invoiceId { get; set; }

    /// <summary>
    /// 单价.
    /// </summary>
    public decimal? price { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public string qty { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string specifications { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string unit { get; set; }
}
