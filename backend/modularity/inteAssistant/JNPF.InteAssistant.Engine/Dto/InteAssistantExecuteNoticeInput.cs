using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model.Integrate;
using JNPF.InteAssistant.Entitys.Entity;

namespace JNPF.InteAssistant.Engine.Dto;

/// <summary>
/// 集成助手执行通知输入.
/// </summary>
[SuppressSniffer]
public class InteAssistantExecuteNoticeInput
{
    /// <summary>
    /// 信息配置.
    /// </summary>
    public MessageConfig mesConfig { get; set; }

    /// <summary>
    /// 消息编码.
    /// </summary>
    public string msgEnCode { get; set; }

    /// <summary>
    /// 默认标题.
    /// </summary>
    public string defaultTitle { get; set; }

    /// <summary>
    /// 通知人类型.
    /// </summary>
    public List<int> msgUserType { get; set; }

    /// <summary>
    /// 通知人.
    /// </summary>
    public List<string> msgUserIds { get; set; }

    /// <summary>
    /// 创建人ID.
    /// </summary>
    public string creatorUserId { get; set; }

    /// <summary>
    /// 消息实体
    /// </summary>
    public IntegrateTaskEntity? taskEntity { get; set; }
}