using System.Collections.Concurrent;
using JNPF.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Llm;

/// <summary>
/// LLM Provider 熔断器 — 时间窗口 + 半开 + 冷却。
/// 三态：Closed → Open → HalfOpen → Closed（或 Open）。
/// 线程安全：ConcurrentDictionary + per-entry lock。
/// </summary>
public interface ILlmCircuitBreaker
{
    /// <summary>
    /// 检查指定 provider 是否处于熔断开启状态。
    /// 注意：此方法有副作用——可能在检查时自动将 Open 转换为 HalfOpen（冷却期满）。
    /// </summary>
    bool CheckAndTransition(string providerCode);

    /// <summary>记录一次成功调用。</summary>
    void RecordSuccess(string providerCode);

    /// <summary>记录一次失败调用。</summary>
    void RecordFailure(string providerCode);

    /// <summary>获取 provider 当前状态（用于日志/监控）。</summary>
    CircuitState GetState(string providerCode);
}

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public class LlmCircuitBreaker : ILlmCircuitBreaker, ITransient
{
    private readonly ILogger<LlmCircuitBreaker> _logger;
    private readonly int _failureThreshold;
    private readonly int _windowSeconds;
    private readonly int _cooldownMs;

    private readonly ConcurrentDictionary<string, CircuitEntry> _entries = new();

    // Fix-12: 定期清理长期不活动的熔断条目，防止内存泄漏
    private static readonly TimeSpan StaleEntryMaxAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public LlmCircuitBreaker(
        IConfiguration configuration,
        ILogger<LlmCircuitBreaker> logger)
    {
        _logger = logger;
        _failureThreshold = configuration.GetValue("AI:CircuitBreaker:FailureThreshold", 5);
        _windowSeconds = configuration.GetValue("AI:CircuitBreaker:WindowSeconds", 60);
        _cooldownMs = configuration.GetValue("AI:CircuitBreaker:CooldownMs", 300_000); // 5 min
    }

    // ─── ILlmCircuitBreaker ───

    public bool CheckAndTransition(string providerCode)
    {
        // Fix-12: 定期清理 stale entries（超过 24h 未活动的 Open 条目）
        ThrottledCleanupStaleEntries();

        var entry = _entries.GetOrAdd(providerCode, _ => new CircuitEntry());
        lock (entry)
        {
            return EvaluateAndTransition(entry, providerCode) == CircuitState.Open;
        }
    }

    public void RecordSuccess(string providerCode)
    {
        var entry = _entries.GetOrAdd(providerCode, _ => new CircuitEntry());
        lock (entry)
        {
            var oldState = entry.State;

            if (entry.State == CircuitState.HalfOpen)
            {
                // HalfOpen 探测成功 → 关闭熔断
                entry.State = CircuitState.Closed;
                entry.FailureTimestamps.Clear();
                entry.OpenSince = null;
                _logger.LogInformation(
                    "熔断器恢复: Provider={Provider}, HalfOpen→Closed（探测成功）", providerCode);
            }
            else if (entry.State == CircuitState.Closed)
            {
                // Closed 状态下的成功，清理过期失败记录
                PruneOldFailures(entry);
            }
            // Open 状态下不应有调用（调用方应已检查 CheckAndTransition），但忽略
        }
    }

    public void RecordFailure(string providerCode)
    {
        var entry = _entries.GetOrAdd(providerCode, _ => new CircuitEntry());
        lock (entry)
        {
            var oldState = entry.State;

            if (entry.State == CircuitState.HalfOpen)
            {
                // HalfOpen 探测失败 → 重新打开
                entry.State = CircuitState.Open;
                entry.OpenSince = DateTime.UtcNow;
                entry.FailureTimestamps.Clear();
                _logger.LogWarning(
                    "熔断器重新开启: Provider={Provider}, HalfOpen→Open（探测失败）", providerCode);
            }
            else if (entry.State == CircuitState.Closed)
            {
                // 记录失败时间戳
                entry.FailureTimestamps.Add(DateTime.UtcNow);
                PruneOldFailures(entry);

                if (entry.FailureTimestamps.Count >= _failureThreshold)
                {
                    entry.State = CircuitState.Open;
                    entry.OpenSince = DateTime.UtcNow;
                    _logger.LogWarning(
                        "熔断器开启: Provider={Provider}, {Count}/{Threshold} 次失败（{Window}s窗口）",
                        providerCode, entry.FailureTimestamps.Count, _failureThreshold, _windowSeconds);
                }
            }
            // Open 状态下额外失败：不做额外处理（状态保持）
        }
    }

    public CircuitState GetState(string providerCode)
    {
        if (!_entries.TryGetValue(providerCode, out var entry))
            return CircuitState.Closed;

        lock (entry)
        {
            return EvaluateAndTransition(entry, providerCode);
        }
    }

    // ─── 内部 ───

    /// <summary>
    /// 评估当前状态并执行可能的转换（Open → HalfOpen）。
    /// 必须在 entry lock 内调用。
    /// </summary>
    private CircuitState EvaluateAndTransition(CircuitEntry entry, string providerCode)
    {
        if (entry.State == CircuitState.Open && entry.OpenSince.HasValue)
        {
            var elapsed = DateTime.UtcNow - entry.OpenSince.Value;
            if (elapsed.TotalMilliseconds >= _cooldownMs)
            {
                entry.State = CircuitState.HalfOpen;
                entry.OpenSince = null;
                _logger.LogInformation(
                    "熔断器半开: Provider={Provider}, Open→HalfOpen（冷却{Cooldown}ms已过，允许探测）",
                    providerCode, _cooldownMs);
            }
        }

        return entry.State;
    }

    /// <summary>清理窗口外的旧失败记录。</summary>
    private void PruneOldFailures(CircuitEntry entry)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-_windowSeconds);
        entry.FailureTimestamps.RemoveAll(t => t < cutoff);
    }

    /// <summary>
    /// 节流清理：距上次清理超过 1 小时后，扫描并移除 Open 超过 24h 且不在 HalfOpen 的条目。
    /// 仅在 CheckAndTransition 调用路径上触发（低频且已持有锁的上下文外执行）。
    /// </summary>
    private void ThrottledCleanupStaleEntries()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup < CleanupInterval)
            return;

        _lastCleanup = now;
        var removedCount = 0;

        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            // 仅清理长时间 Open 的条目（HalfOpen/Closed 保留）
            if (entry.State != CircuitState.Open || entry.OpenSince == null)
                continue;

            if (now - entry.OpenSince.Value > StaleEntryMaxAge)
            {
                if (_entries.TryRemove(kvp.Key, out _))
                    removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation(
                "熔断器定期清理: 移除了 {Count} 个 stale 条目（Open>{Hours}h 未恢复）",
                removedCount, (int)StaleEntryMaxAge.TotalHours);
        }
    }

    /// <summary>每 provider 的熔断状态。</summary>
    private sealed class CircuitEntry
    {
        public CircuitState State = CircuitState.Closed;
        public List<DateTime> FailureTimestamps = new();
        public DateTime? OpenSince;
    }
}
