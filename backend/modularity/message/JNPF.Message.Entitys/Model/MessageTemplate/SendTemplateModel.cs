namespace JNPF.Message.Entitys.Model.MessageTemplate;

public class SendTemplateModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 发送id.
    /// </summary>
    public string sendConfigId { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    public string messageType { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 账号id.
    /// </summary>
    public string accountConfigId { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 模板编码.
    /// </summary>
    public string templateCode { get; set; }

    /// <summary>
    /// 模板名称.
    /// </summary>
    public string templateName { get; set; }

    /// <summary>
    /// 账号.
    /// </summary>
    public string accountCode { get; set; }

    /// <summary>
    /// 账号名.
    /// </summary>
    public string accountName { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    public string tenantId { get; set; }
}
