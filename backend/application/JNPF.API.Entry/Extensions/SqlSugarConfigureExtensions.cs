using JNPF;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.Logging;
using Mapster;
using SqlSugar;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// SqlSugar配置拓展.
/// </summary>
public static class SqlSugarConfigureExtensions
{
    public static void SqlSugarConfigure(this IServiceCollection services)
    {
        // 获取选项
        var dbOptions = App.GetOptions<ConnectionStringsOptions>();
        //add by harry  域名模式，只保留一个，现模式会自动取第一个
        var defaulConnection = dbOptions.DefaultConnectionConfig;
        if(defaulConnection!.ConnectionString == null) 
        {
            var existConn = dbOptions.ConnectionConfigs.FirstOrDefault(aa => aa.ConfigId == defaulConnection.ConfigId);
            dbOptions.ConnectionConfigs.Remove(existConn);
            dbOptions.ConnectionConfigs.Insert(0, existConn);
        }
        //end
        dbOptions.ConnectionConfigs.ForEach(SetDbConfig);

        List<ConnectionConfig> connectConfigList = new List<ConnectionConfig>();

        SqlSugarScope sqlSugar = new(dbOptions.ConnectionConfigs.Adapt<List<ConnectionConfig>>(), db =>
        {
            dbOptions.ConnectionConfigs.ForEach(config =>
            {
                var dbProvider = db.GetConnectionScope(config.ConfigId);
                SetDbAop(dbProvider);
            });
        });

        services.AddSingleton<ISqlSugarClient>(sqlSugar);                                    // 单例注册
        services.AddScoped<ISqlSugarDbContextProvider, SqlSugarDbContextProvider>();           // 【新增】上下文提供器注册
        services.AddScoped(typeof(ISqlSugarRepository<>), typeof(SqlSugarRepository<>));      // 仓储注册
        services.AddUnitOfWork<SqlSugarUnitOfWork>();                                          // 事务与工作单元注册

    }

    /// <summary>
    /// 配置连接属性.
    /// </summary>
    /// <param name="config"></param>
    private static void SetDbConfig(DbConnectionConfig config)
    {
        Log.Information(config.ToJsonString());
        config.ConnectionString = JNPFTenantExtensions.ToConnectionString(config);
        config.ConfigureExternalServices = new ConfigureExternalServices
        {
            EntityService = (type, column) => // 处理列
            {
                if (new NullabilityInfoContext().Create(type).WriteState is NullabilityState.Nullable)
                    column.IsNullable = true;

                if (config.DbType == SqlSugar.DbType.Oracle)
                {
                    if (type.PropertyType == typeof(long) || type.PropertyType == typeof(long?))
                        column.DataType = "number(18)";
                    if (type.PropertyType == typeof(bool) || type.PropertyType == typeof(bool?))
                        column.DataType = "number(1)";
                }
            },
        };
        config.IsAutoCloseConnection = true;
        config.MoreSettings = new ConnMoreSettings
        {
            IsAutoRemoveDataCache = true,
            SqlServerCodeFirstNvarchar = true, // 采用Nvarchar
            IsAutoToUpper = false
        };
    }

    /// <summary>
    /// 配置Aop.
    /// </summary>
    /// <param name="db"></param>
    public static void SetDbAop(SqlSugarScopeProvider db)
    {
        var config = db.CurrentConnectionConfig;

        // 设置超时时间
        db.Ado.CommandTimeOut = 30;

        var sqlStopwatch = new Stopwatch();

        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            sqlStopwatch.Restart();
            App.PrintToMiniProfiler("SqlSugar", "Info", sql + "\r\n" + db.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
        };

        db.Aop.OnLogExecuted = (sql, pars) =>
        {
            sqlStopwatch.Stop();
            var elapsed = sqlStopwatch.ElapsedMilliseconds;

            if (elapsed > 1000) // Slow query threshold: 1 second
            {
                Serilog.Log.ForContext("Sql", sql)
                   .ForContext("Elapsed", elapsed)
                   .Warning("Slow SQL ({Elapsed}ms): {Sql}", elapsed, sql);
            }
        };

        db.Aop.OnError = ex =>
        {
            if (ex.Parametres == null) return;
            var pars = db.Utilities.SerializeObject(((SugarParameter[])ex.Parametres).ToDictionary(it => it.ParameterName, it => it.Value));

            Serilog.Log.ForContext("Sql", ex.Sql)
               .Error(ex, "SQL Error: {Sql}", ex.Sql);

            App.PrintToMiniProfiler("SqlSugar", "Error", $"{ex.Message}{Environment.NewLine}{ex.Sql}{pars}{Environment.NewLine}");
        };
    }
}