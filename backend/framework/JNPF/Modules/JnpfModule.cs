using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JNPF.Modules;

/// <summary>
/// 模块基类 — 所有功能模块继承此类.
/// </summary>
public abstract class JnpfModule
{
    /// <summary>
    /// 获取当前模块的依赖模块类型列表（从 [DependsOn] 特性读取）.
    /// </summary>
    public IReadOnlyList<Type> Dependencies =>
        GetType().GetCustomAttributes(typeof(DependsOnAttribute), true)
            .OfType<DependsOnAttribute>()
            .SelectMany(a => a.Dependencies)
            .Distinct()
            .ToList();

    /// <summary>
    /// 注册模块服务（DI 容器配置阶段）.
    /// </summary>
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    /// <summary>
    /// 配置模块中间件（应用启动阶段）.
    /// </summary>
    public virtual void OnApplicationInitialization(IApplicationBuilder app)
    {
    }

    /// <summary>
    /// 应用关闭时清理（可选）.
    /// </summary>
    public virtual void OnApplicationShutdown(IApplicationBuilder app)
    {
    }

    /// <summary>
    /// 辅助方法：检查服务是否已被注册.
    /// </summary>
    protected static bool IsServiceRegistered<TService>(IServiceCollection services)
    {
        return services.Any(d => d.ServiceType == typeof(TService));
    }

    /// <summary>
    /// 辅助方法：仅在服务未注册时添加.
    /// </summary>
    protected static void TryAddScoped<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!IsServiceRegistered<TService>(services))
        {
            services.AddScoped<TService, TImplementation>();
        }
    }

    /// <summary>
    /// 辅助方法：仅在服务未注册时添加单例.
    /// </summary>
    protected static void TryAddSingleton<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!IsServiceRegistered<TService>(services))
        {
            services.AddSingleton<TService, TImplementation>();
        }
    }
}
