using System.Collections.Concurrent;

namespace JNPF.InteAssistant;

/// <summary>
/// 简易内存分布式锁（单机多实例防重复）
/// 生产环境建议替换为 Redis RedLock
/// 版 本：v5.2.0
/// </summary>
public static class DistributedLock
{
    private static readonly ConcurrentDictionary<string, LockEntry> Locks = new();

    /// <summary>
    /// 尝试获取锁
    /// </summary>
    /// <param name="key">锁键</param>
    /// <param name="timeout">超时后自动释放</param>
    /// <returns>是否成功获取</returns>
    public static bool TryAcquire(string key, TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        var entry = Locks.GetOrAdd(key, _ => new LockEntry { AcquiredAt = now, Timeout = timeout });

        // 锁已过期 → 重新获取
        if (now - entry.AcquiredAt > entry.Timeout)
        {
            entry.AcquiredAt = now;
            return true;
        }

        // 锁由当前调用持有（重入）
        return false;
    }

    /// <summary>
    /// 释放锁
    /// </summary>
    public static void Release(string key)
    {
        Locks.TryRemove(key, out _);
    }

    private class LockEntry
    {
        public DateTime AcquiredAt { get; set; }
        public TimeSpan Timeout { get; set; }
    }
}
