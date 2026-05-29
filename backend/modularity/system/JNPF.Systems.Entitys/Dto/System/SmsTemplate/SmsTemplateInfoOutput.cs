using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SmsTemplate;

/// <summary>
/// base_sms_template输出参数.
/// </summary>
[SuppressSniffer]
public class SmsTemplateInfoOutput
{
    /// <summary>
    /// 自然主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 短信提供商.
    /// </summary>
    public int? company { get; set; }

    /// <summary>
    /// 应用编号.
    /// </summary>
    public string appId { get; set; }

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
    /// 域名.
    /// </summary>
    public string endpoint { get; set; }

    /// <summary>
    /// 地域.
    /// </summary>
    public string region { get; set; }
}