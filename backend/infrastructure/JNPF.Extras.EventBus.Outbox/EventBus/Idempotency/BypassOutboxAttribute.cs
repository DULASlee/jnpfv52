namespace JNPF.Extras.EventBus.Idempotency;

/// <summary>
/// 标记事件绕过 Outbox 管道，直接投递。
/// 仅允许用于系统心跳、健康检查等非关键事件。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class BypassOutboxAttribute : Attribute
{
}
