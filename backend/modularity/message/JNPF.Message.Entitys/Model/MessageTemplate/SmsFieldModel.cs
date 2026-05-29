namespace JNPF.Message.Entitys.Model.MessageTemplate;

public class SmsFieldModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 字段id.
    /// </summary>
    public string fieldId { get; set; }

    /// <summary>
    /// 短信字段.
    /// </summary>
    public string smsField { get; set; }

    /// <summary>
    /// 字段说明.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    public string tenantId { get; set; }
}
