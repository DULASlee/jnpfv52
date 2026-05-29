using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.MessageTemplate;

/// <summary>
/// base_message_template输出参数.
/// </summary>
[SuppressSniffer]
public class MessageTemplateInfoOutput : MessageTemplateCrInput
{
    /// <summary>
    /// 自然主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 短信名称.
    /// </summary>
    public string smsTemplateName { get; set; }
}