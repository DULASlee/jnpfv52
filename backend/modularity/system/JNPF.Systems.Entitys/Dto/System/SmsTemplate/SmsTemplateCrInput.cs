using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SmsTemplate;

/// <summary>
/// base_sms_template修改输入参数.
/// </summary>
[SuppressSniffer]
public class SmsTemplateCrInput
{
    /// <summary>
    /// 短信提供商（0:腾讯 1：阿里）.
    /// </summary>
    public int? company { get; set; }

    /// <summary>
    /// 模板名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 模板编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 签名内容.
    /// </summary>
    public string signContent { get; set; }

    /// <summary>
    /// 模板id（第三方）.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 请求地址.
    /// </summary>
    public string endpoint { get; set; }

    /// <summary>
    /// 地域参数.
    /// </summary>
    public string region { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }
}