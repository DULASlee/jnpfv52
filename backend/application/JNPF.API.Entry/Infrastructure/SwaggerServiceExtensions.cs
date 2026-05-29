using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JNPF.API.Entry.Infrastructure;

/// <summary>
/// Swagger 缓存服务注册扩展.
/// </summary>
public static class SwaggerServiceExtensions
{
    /// <summary>
    /// 用缓存包装默认 Swagger 生成器（须在 AddSwaggerGen 之后调用）.
    /// </summary>
    public static IServiceCollection AddCachingSwaggerProvider(this IServiceCollection services)
    {
        services.AddSingleton<SwaggerGenerator>();
        services.Replace(ServiceDescriptor.Singleton<ISwaggerProvider>(sp =>
        {
            var inner = sp.GetRequiredService<SwaggerGenerator>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            return new CachingSwaggerProvider(inner, cache);
        }));

        return services;
    }

    /// <summary>
    /// 应用启动时预热 Default 分组 Swagger 文档.
    /// </summary>
    public static void WarmupSwagger(this IServiceProvider serviceProvider, string documentName = "Default")
    {
        try
        {
            var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();
            swaggerProvider.GetSwagger(documentName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Swagger warmup failed: {ex.Message}");
        }
    }
}
