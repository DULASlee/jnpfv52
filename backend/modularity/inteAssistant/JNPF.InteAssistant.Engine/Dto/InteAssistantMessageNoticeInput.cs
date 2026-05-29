using JNPF.Common.Dtos.Message;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Engine.Dto;

/// <summary>
/// 集成助手执行通知输入.
/// </summary>
[SuppressSniffer]
public class InteAssistantMessageNoticeInput
{
    /// <summary>
    /// 模板配置json.
    /// </summary>
    public List<MessageSendModel>? templateJson { get; set; }

    /// <summary>
    /// 通知人.
    /// </summary>
    public List<string> msgUserIds { get; set; }

    /// <summary>
    /// 数据字符串.
    /// </summary>
    public string data { get; set; }
}
