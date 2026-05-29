using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.Product;

/// <summary>
/// 销售订单列表.
/// </summary>
[SuppressSniffer]
public class ProductListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 订单编号.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    public string customerName { get; set; }

    /// <summary>
    /// 业务员.
    /// </summary>
    public string business { get; set; }

    /// <summary>
    /// 送货地址
    /// </summary>
    public string address { get; set; }

    /// <summary>
    /// 联系方式.
    /// </summary>
    public string contactTel { get; set; }

    /// <summary>
    /// 制单人.
    /// </summary>
    public string salesmanName { get; set; }

    /// <summary>
    /// 审核状态.
    /// </summary>
    public int auditState { get; set; }

    /// <summary>
    /// 发货状态.
    /// </summary>
    public int goodsState { get; set; }

    /// <summary>
    /// 关闭状态.
    /// </summary>
    public int closeState { get; set; }

    /// <summary>
    /// 关闭日期.
    /// </summary>
    public DateTime? closeDate { get; set; }

    /// <summary>
    /// 联系人.
    /// </summary>
    public string contactName { get; set; }

    /// <summary>
    /// 子表数据.
    /// </summary>
    public List<ProductEntryEntity> productEntryList { get; set; }
}