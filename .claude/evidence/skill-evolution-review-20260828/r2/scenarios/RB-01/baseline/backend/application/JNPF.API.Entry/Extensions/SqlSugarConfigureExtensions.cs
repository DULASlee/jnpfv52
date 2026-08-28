using JNPF;
using JNPF.Common.Core.Manager;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
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
        // 提前解析 scoped 访问器，供 AOP lambda 闭包使用 (Sprint 1: App.GetService → DI)
        var httpContextAccessor = services.BuildServiceProvider().GetRequiredService<IHttpContextAccessor>();
        // IUserManager 改为延迟解析——在 AOP 回调中通过 RequestServices 获取，
        // 避免 BuildServiceProvider() 快照不包含后续注册的 ISqlSugarRepository<>
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
                SetDbAop(dbProvider, httpContextAccessor);
            });
        });

        services.AddSingleton<ISqlSugarClient>(sqlSugar);                                    // 单例注册
        services.AddScoped<ISqlSugarDbContextProvider, SqlSugarDbContextProvider>();           // 【新增】上下文提供器注册
        services.AddScoped(typeof(ISqlSugarRepository<>), typeof(SqlSugarRepository<>));      // 仓储注册
        services.AddUnitOfWork<SqlSugarUnitOfWork>();                                          // 事务与工作单元注册

        // DiffLog 收集器基础设施（阶段 1 新增）
        services.AddScoped<IDiffLogCollector, DiffLogCollector>();
        services.AddScoped<IDiffLogPublisher, NoOpDiffLogPublisher>();
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
    public static void SetDbAop(SqlSugarScopeProvider db, IHttpContextAccessor httpContextAccessor)
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

        // 慢查询阈值从配置读取，默认 1000ms
        var slowQueryThreshold = App.GetConfig<int?>("Database:SlowQueryThreshold") ?? 1000;

        db.Aop.OnLogExecuted = (sql, pars) =>
        {
            sqlStopwatch.Stop();
            var elapsed = sqlStopwatch.ElapsedMilliseconds;

            if (elapsed > slowQueryThreshold)
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

        // Oracle 特殊 SQL 转换（从 Repository 迁移）
        if (config.DbType == SqlSugar.DbType.Oracle)
        {
            db.Aop.OnExecutingChangeSql = (sql, pars) =>
            {
                // Oracle 的布尔值处理
                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };
        }

        // DiffLog — 数据变更审计（收集器模式，ADR-011）
        var enableDiffLog = App.GetConfig<bool?>("Database:EnableDiffLog") ?? false;
        if (enableDiffLog)
        {
            db.Aop.OnDiffLogEvent = (diff) =>
            {
                try
                {
                    // 通过 IHttpContextAccessor 获取 Scoped 的 IDiffLogCollector
                    var collector = httpContextAccessor?.HttpContext?.RequestServices?
                        .GetService<IDiffLogCollector>();

                    if (collector != null)
                    {
                        collector.Collect(new DiffLogData
                        {
                            TableName = diff.GetType().GetProperty("TableName")?.GetValue(diff)?.ToString()
                                ?? diff.AfterData?.FirstOrDefault()?.GetType().GetProperty("TableName")?.GetValue(diff.AfterData?.FirstOrDefault())?.ToString()
                                ?? "Unknown",
                            OperationType = diff.DiffType.ToString(),
                            BeforeData = diff.BeforeData?.ToDictionary(
                                d => d.GetType().GetProperty("TableName")?.GetValue(d)?.ToString() ?? "Unknown",
                                d => (object)d),
                            AfterData = diff.AfterData?.ToDictionary(
                                d => d.GetType().GetProperty("TableName")?.GetValue(d)?.ToString() ?? "Unknown",
                                d => (object)d),
                            TenantId = httpContextAccessor?.HttpContext?.RequestServices?.GetService<IUserManager>()?.TenantId,
                            TraceId = Activity.Current?.Id,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                catch
                {
                    // DiffLog 收集失败不应影响业务操作
                }
            };
        }

        // ConfigureGlobalDataExecuting — ADR-002 情况 B：统一委托模式
        ConfigureGlobalDataExecuting(db, httpContextAccessor);
    }

    /// <summary>
    /// 启动时一次性组装统一的 DataExecuting 回调。
    /// ADR-002 情况 B：= 覆盖模式，CopyNew 继承 AOP。
    /// 运行时通过静态访问点读取当前请求的租户/系统信息。
    /// </summary>
    private static void ConfigureGlobalDataExecuting(SqlSugarScopeProvider db, IHttpContextAccessor httpContextAccessor)
    {
        db.Aop.DataExecuting = (oldValue, entityColumnInfo) =>
        {
            var propertyName = entityColumnInfo.PropertyName;
            var entityType = entityColumnInfo.EntityValue.GetType();

            // 仅在 Insert / Update 操作时处理
            var isWriteOperation =
                entityColumnInfo.OperationType == DataFilterType.InsertByObject ||
                entityColumnInfo.OperationType == DataFilterType.UpdateByObject;

            if (!isWriteOperation) return;

            // 租户字段自动填充
            // 优先从 HTTP Claims 读取，降级到 TenantContextImpl.AsyncLocal（非 HTTP 场景：EventBus/Schedule）
            if (propertyName == "TenantId"
                && typeof(ITenantFilter).IsAssignableFrom(entityType))
            {
                var tenantId = httpContextAccessor?.HttpContext?.User?.FindFirst("TenantId")?.Value
                    ?? TenantContextImpl.Current?.TenantId;
                if (!string.IsNullOrEmpty(tenantId))
                {
                    entityColumnInfo.SetValue(tenantId);
                }
            }

            // 系统字段自动填充
            if (propertyName == "ZxSystemId"
                && typeof(IZxSystemFilter).IsAssignableFrom(entityType))
            {
                var systemId = httpContextAccessor?.HttpContext?.User?.FindFirst("ZxSystemId")?.Value
                    ?? TenantContextImpl.Current?.SystemId;
                if (!string.IsNullOrEmpty(systemId))
                {
                    entityColumnInfo.SetValue(systemId);
                }
            }
        };
    }
}
