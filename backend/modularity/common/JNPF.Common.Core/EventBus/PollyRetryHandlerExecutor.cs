using JNPF.EventBus;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace JNPF.EventHandler;

/// <summary>
/// Polly 风格事件执行器 — 指数退避 + 随机抖动 + 熔断。
/// 替代 RetryEventHandlerExecutor 的简单 3 次重试。
/// </summary>
public class PollyRetryHandlerExecutor : IEventHandlerExecutor
{
    private readonly ILogger<PollyRetryHandlerExecutor> _logger;

    // 熔断器状态：key=EventName, value=连续失败次数
    private static readonly ConcurrentDictionary<string, int> _failureCounts = new();
    private static readonly ConcurrentDictionary<string, DateTime> _circuitBreakerUntil = new();

    private const int MaxRetries = 5;
    private const int CircuitBreakerThreshold = 10;
    private static readonly TimeSpan CircuitBreakerDuration = TimeSpan.FromSeconds(30);

    public PollyRetryHandlerExecutor(ILogger<PollyRetryHandlerExecutor> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler)
    {
        var eventName = context.Source?.EventId ?? "unknown";

        // 检查熔断器
        if (_circuitBreakerUntil.TryGetValue(eventName, out var until) && DateTime.UtcNow < until)
        {
            _logger.LogWarning("Circuit breaker open for {EventName}, skipping until {Until}", eventName, until);
            return;
        }

        Exception? lastException = null;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await handler(context);

                // 成功 → 重置失败计数
                _failureCounts.TryRemove(eventName, out _);
                _circuitBreakerUntil.TryRemove(eventName, out _);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                var failureCount = _failureCounts.AddOrUpdate(eventName, 1, (_, v) => v + 1);

                // 触发熔断
                if (failureCount >= CircuitBreakerThreshold)
                {
                    _circuitBreakerUntil[eventName] = DateTime.UtcNow.Add(CircuitBreakerDuration);
                    _logger.LogError(ex, "Circuit breaker triggered for {EventName} after {Count} failures", eventName, failureCount);
                    break;
                }

                if (attempt < MaxRetries)
                {
                    // 指数退避 + ±20% 随机抖动
                    var baseDelay = Math.Pow(2, attempt) * 1000; // 1s, 2s, 4s, 8s, 16s
                    var jitter = baseDelay * 0.2 * (Random.Shared.NextDouble() * 2 - 1);
                    var delay = TimeSpan.FromMilliseconds(Math.Min(baseDelay + jitter, 60000));

                    _logger.LogWarning(ex, "Event {EventName} attempt {Attempt} failed, retrying in {Delay}ms",
                        eventName, attempt + 1, delay.TotalMilliseconds);

                    await Task.Delay(delay);
                }
            }
        }

        _logger.LogError(lastException, "Event {EventName} failed after {MaxRetries} retries", eventName, MaxRetries);
        throw lastException!;
    }
}
