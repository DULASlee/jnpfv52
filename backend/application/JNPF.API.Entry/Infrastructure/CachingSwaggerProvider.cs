using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace JNPF.API.Entry.Infrastructure;

/// <summary>
/// Swagger 文档缓存包装器，避免每次请求重复生成 OpenAPI 文档.
/// </summary>
public class CachingSwaggerProvider : ISwaggerProvider
{
    private readonly ISwaggerProvider _innerProvider;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public CachingSwaggerProvider(ISwaggerProvider innerProvider, IMemoryCache cache)
    {
        _innerProvider = innerProvider;
        _cache = cache;
    }

    public OpenApiDocument GetSwagger(string documentName, string host = null, string basePath = null)
    {
        var cacheKey = $"swagger:{documentName}:{host}:{basePath}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return _innerProvider.GetSwagger(documentName, host, basePath);
        });
    }
}
