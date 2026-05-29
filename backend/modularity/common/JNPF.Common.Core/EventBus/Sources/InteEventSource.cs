using JNPF.Common.Models.InteAssistant;
using JNPF.EventBus;
using Newtonsoft.Json;

namespace JNPF.EventHandler;

/// <summary>
/// 集成事件源.
/// </summary>
public class InteEventSource : IEventSource
{
    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="eventId">事件ID.</param>
    /// <param name="userId">用户ID.</param>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="model">事件数据模型.</param>
    public InteEventSource(string eventId, string userId, string tenantId, InteAssiEventModel model)
    {
        UserId = userId;
        EventId = eventId;
        TenantId = tenantId;
        Model = model;
    }

    /// <summary>
    /// 用户ID.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 租户ID.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// 集成助手模型.
    /// </summary>
    public InteAssiEventModel Model { get; set; }

    /// <summary>
    /// 事件 Id.
    /// </summary>
    public string EventId { get; }

    /// <summary>
    /// 事件承载（携带）数据.
    /// </summary>
    public object Payload { get; }

    /// <summary>
    /// 取消任务 Token.
    /// </summary>
    /// <remarks>用于取消本次消息处理.</remarks>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// 事件创建时间.
    /// </summary>
    public DateTime CreatedTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// 消息是否只消费一次.
    /// </summary>
    public bool IsConsumOnce { get; set; }
}