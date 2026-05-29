namespace JNPF.Extras.CollectiveOAuth.Cache;

/// <summary>
/// 授权状态缓存抽象类.
/// </summary>
public interface IAuthStateCache
{
    /// <summary>
    /// 存入缓存.
    /// </summary>
    /// <param name="key">缓存key.</param>
    /// <param name="value">缓存内容.</param>
    void cache(string key, string value);

    /// <summary>
    /// 存入缓存.
    /// </summary>
    /// <param name="key">缓存key.</param>
    /// <param name="value">缓存内容.</param>
    /// <param name="timeout">指定缓存过期时间（毫秒）.</param>
    void cache(string key, string value, long timeout);

    /// <summary>
    /// 获取缓存内容.
    /// </summary>
    /// <param name="key">缓存key.</param>
    /// <returns>缓存内容.</returns>
    string get(string key);

    /// <summary>
    /// 是否存在key，如果对应key的value值已过期，也返回false.
    /// </summary>
    /// <param name="key">缓存key.</param>
    /// <returns>true：存在key，并且value没过期；false：key不存在或者已过期.</returns>
    bool containsKey(string key);
}