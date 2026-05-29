using JNPF.Common.Models.WorkFlow;
using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Dto.WorkFlowForm.SalesOrder;

[SuppressSniffer]
public class SalesOrderInput : FlowTaskOtherModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 流程标题.
    /// </summary>
    public string flowTitle { get; set; }

    /// <summary>
    /// 流程主键.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? flowUrgent { get; set; }

    /// <summary>
    /// 流程单据.
    /// </summary>
    public string billNo { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    public string customerName { get; set; }

    /// <summary>
    /// 发票日期.
    /// </summary>
    public DateTime? ticketDate { get; set; }

    /// <summary>
    /// 联系电话.
    /// </summary>
    public string contactPhone { get; set; }

    /// <summary>
    /// 联系人员.
    /// </summary>
    public string contacts { get; set; }

    /// <summary>
    /// 客户地址.
    /// </summary>
    public string customerAddres { get; set; }

    /// <summary>
    /// 开单备注.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 相关附件.
    /// </summary>
    public string fileJson { get; set; }

    /// <summary>
    /// 发票类型.
    /// </summary>
    public string invoiceType { get; set; }

    /// <summary>
    /// 付款方式.
    /// </summary>
    public string paymentMethod { get; set; }

    /// <summary>
    /// 付款金额.
    /// </summary>
    public decimal? paymentMoney { get; set; }

    /// <summary>
    /// 业务日期.
    /// </summary>
    public DateTime? salesDate { get; set; }

    /// <summary>
    /// 业务人员.
    /// </summary>
    public string salesman { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 发票编号.
    /// </summary>
    public string ticketNum { get; set; }

    /// <summary>
    /// 销售明细-商品名称.
    /// </summary>
    public List<EntryListItem> entryList { get; set; }
}
