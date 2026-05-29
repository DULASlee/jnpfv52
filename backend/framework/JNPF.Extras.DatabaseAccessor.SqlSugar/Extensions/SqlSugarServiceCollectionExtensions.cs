using SqlSugar;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// SqlSugar 拓展类
/// </summary>
public static class SqlSugarServiceCollectionExtensions
{
    /// <summary>
    /// 添加 SqlSugar 拓展
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="buildAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddSqlSugar(this IServiceCollection services, ConnectionConfig config, Action<ISqlSugarClient> buildAction = default)
    {
        var list = new List<ConnectionConfig>();
        list.Add(config);
        return services.AddSqlSugar(list, buildAction);
    }

    /// <summary>
    /// 添加 SqlSugar 拓展
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configs"></param>
    /// <param name="buildAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddSqlSugar(this IServiceCollection services, List<ConnectionConfig> configs, Action<ISqlSugarClient> buildAction = default)
    {
        // SqlSugarScope是线程安全，可使用单例注入
        // 注册 SqlSugar 客户端
        services.AddSingleton<ISqlSugarClient>(u =>
        {
            var sqlSugarClient = new SqlSugarScope(configs);
            buildAction?.Invoke(sqlSugarClient);

            return sqlSugarClient;
        });

        // 注册 SqlSugar 仓储
        services.AddScoped(typeof(ISqlSugarRepository<>), typeof(SqlSugarRepository<>));

        return services;
    }
}