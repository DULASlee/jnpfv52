using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SmsTemplate;

/// <summary>
/// base_sms_template更新输入参数.
/// </summary>
[SuppressSniffer]
public class SmsTemplateUpInput : SmsTemplateCrInput
{
    /// <summary>
    /// 自然主键.
    /// </summary>
    public string id { get; set; }
}