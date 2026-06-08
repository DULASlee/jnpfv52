using JNPF.EventBus;
using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

namespace JNPF.EventHandler;

/// <summary>
/// 事件执行器 — 租户传播 + 失败重试.
/// 包装 RetryEventHandlerExecutor，在事件处理前设置租户上下文，处理后清除.
/// ADR-013: 非 HTTP 上下文传播拦截器.
/// </summary>
public class TenantPropagationExecutor : IEventHandlerExecutor
{
    private readonly IEventHandlerExecutor _inner;

    public TenantPropagationExecutor()
    {
        _inner = new RetryEventHandlerExecutor();
    }

    public async Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler)
    {
        // 从事件源提取 TenantId 并设置租户上下文
        var tenantId = ExtractTenantId(context.Source);

        if (!string.IsNullOrEmpty(tenantId))
        {
            TenantContextImpl.SetTenant(tenantId);
        }

        try
        {
            await _inner.ExecuteAsync(context, handler);
        }
        finally
        {
            // 铁律：必须清除，防止线程池复用导致的幽灵租户
            TenantContextImpl.ClearCurrent();
        }
    }

    /// <summary>
    /// 从事件源提取 TenantId（兼容各 IEventSource 子类）.
    /// </summary>
    private static string? ExtractTenantId(IEventSource source)
    {
        // 各 IEventSource 子类（LogEventSource, UserEventSource 等）自行添加了 TenantId 属性
        var prop = source.GetType().GetProperty("TenantId");
        if (prop != null && prop.PropertyType == typeof(string))
        {
            return prop.GetValue(source)?.ToString();
        }

        // 尝试从 Payload 提取
        var payload = source.Payload;
        if (payload != null)
        {
            var payloadProp = payload.GetType().GetProperty("TenantId");
            if (payloadProp != null && payloadProp.PropertyType == typeof(string))
            {
                return payloadProp.GetValue(payload)?.ToString();
            }
        }

        return null;
    }
}
