using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SmsTemplate;

/// <summary>
/// 短信模板测试接口输出.
/// </summary>
[SuppressSniffer]
public class SmsTemplateSendTestInput : SmsTemplateCrInput
{
    /// <summary>
    /// 接收号码.
    /// </summary>
    public string phoneNumbers { get; set; }

    /// <summary>
    /// 参数信息.
    /// </summary>
    public Dictionary<string, string> parameters { get; set; } = new Dictionary<string, string>();
}