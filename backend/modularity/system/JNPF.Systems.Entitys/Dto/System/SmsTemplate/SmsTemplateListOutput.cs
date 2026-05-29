using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SmsTemplate;

/// <summary>
/// base_sms_template输入参数.
/// </summary>
[SuppressSniffer]
public class SmsTemplateListOutput
{
    /// <summary>
    /// 自然主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 短信提供商.
    /// </summary>
    public string company { get; set; }

    /// <summary>
    /// 签名内容.
    /// </summary>
    public string signContent { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 模板编号.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 模板名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 模板编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }
}