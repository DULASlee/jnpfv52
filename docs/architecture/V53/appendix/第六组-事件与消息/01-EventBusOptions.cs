using JNPF.ConfigurableOptions;

namespace JNPF.Common.Core;

/// <summary>
/// 事件总线配置.
/// </summary>
public class EventBusOptions : IConfigurableOptions
{
    /// <summary>
    /// 事件总线类型.
    /// </summary>
    public EventBusType EventBusType { get; set; }

    /// <summary>
    /// 服务器地址.
    /// </summary>
    public string HostName { get; set; }

    /// <summary>
    /// 账号.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 密码.
    /// </summary>
    public string Password { get; set; }
}

/// <summary>
/// 事件总线自定义事件源存储器类型.
/// </summary>
public enum EventBusType
{
    /// <summary>
    /// 内存.
    /// </summary>
    Memory,

    /// <summary>
    /// RabbitMQ.
    /// </summary>
    RabbitMQ,

    /// <summary>
    /// Redis.
    /// </summary>
    Redis,

    /// <summary>
    /// Kafka.
    /// </summary>
    Kafka,
}