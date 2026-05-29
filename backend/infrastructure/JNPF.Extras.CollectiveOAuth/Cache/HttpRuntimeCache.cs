using Microsoft.Extensions.Caching.Memory;

namespace JNPF.Extras.CollectiveOAuth.Cache;

/// <summary>
/// http运行时缓存.
/// </summary>
public class HttpRuntimeCache
{
    /// <summary>
    /// 缓存.
    /// </summary>
    public static IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions() { });

    /// <summary>
    /// 获取数据缓存.
    /// </summary>
    /// <param name="CacheKey">键.</param>
    public static object Get(string CacheKey)
    {
        memoryCache.TryGetValue(CacheKey, out object result);
        return result;
    }

    /// <summary>
    /// 设置数据缓存
    /// 变化时间过期（平滑过期）。表示缓存连续2个小时没有访问就过期（TimeSpan.FromSeconds(7200)）.
    /// </summary>
    /// <param name="CacheKey">键.</param>
    /// <param name="objObject">值.</param>
    /// <param name="Second">过期时间，默认7200秒.</param>
    /// <param name="Sliding">是否相对过期，默认是；否，则固定时间过期.</param>
    public static void Set(string CacheKey, object objObject, long Second = 7200, bool Sliding = true) =>
        memoryCache.Set(CacheKey, objObject, Sliding ?
            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(Second)) :
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(Second)));

    /// <summary>
    /// 移除指定数据缓存.
    /// </summary>
    /// <param name="CacheKey">键.</param>
    public static void Remove(string CacheKey) => memoryCache.Remove(CacheKey);

    /// <summary>
    /// 移除全部缓存.
    /// </summary>
    public static void RemoveAll() => memoryCache.Dispose();
}