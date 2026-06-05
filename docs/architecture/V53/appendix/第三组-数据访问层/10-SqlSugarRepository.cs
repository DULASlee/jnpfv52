using JNPF;
using JNPF.Common.Manager;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.FriendlyException;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace SqlSugar;

/// <summary>
/// SqlSugar 仓储实现类
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public partial class SqlSugarRepository<TEntity> : SimpleClient<TEntity>, ISqlSugarRepository<TEntity>
where TEntity : class, new()
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SqlSugarRepository(IServiceProvider serviceProvider, ISqlSugarClient context = null) : base(context)
    {
        using var serviceScope = serviceProvider.CreateScope();
        var _cacheManager = serviceScope.ServiceProvider.GetService<ICacheManager>();

        // 获取数据库连接选项
        ConnectionStringsOptions connectionStrings = App.GetConfig<ConnectionStringsOptions>("ConnectionStrings", true);

        // 获取多租户选项
        TenantOptions tenant = App.GetConfig<TenantOptions>("Tenant", true);
        var httpContext = App.HttpContext;

        base.Context = (SqlSugarScope)context;

        string tenantId = connectionStrings.DefaultConnectionConfig.ConfigId.ToString();
        string tenantDbName = string.Empty;
        if (httpContext?.GetEndpoint()?.Metadata?.GetMetadata<AllowAnonymousAttribute>() == null)
        {
            if (tenant.MultiTenancy && httpContext != null)
            {
                tenantId = httpContext?.User.FindFirst("TenantId")?.Value;
                var tenantCache = _cacheManager.Get<List<GlobalTenantCacheModel>>("jnpf:global:tenant").Find(it => it.TenantId.Equals(tenantId));
                if (tenantCache != null)
                {
                    if (!base.Context.AsTenant().IsAnyConnection(tenantCache.connectionConfig.ConfigId))
                    {
                        base.Context.AsTenant().AddConnection(JNPFTenantExtensions.GetConfig(tenantCache.connectionConfig));
                    }
                    base.Context = base.Context.AsTenant().GetConnectionScope(tenantCache.connectionConfig.ConfigId);
                    // 字段隔离追加过滤器
                    if (tenantCache.type == 1)
                    {
                        tenantDbName = tenantCache.connectionConfig.IsolationField;
                        if (!"default".Equals(tenantId))
                        {
                            base.Context.QueryFilter.Clear();
                            base.Context.QueryFilter.AddTableFilter<ITenantFilter>(it => it.TenantId == tenantDbName);
                            base.Context.Aop.DataExecuting = (oldValue, entityInfo) =>
                            {
                                if (!typeof(ITenantFilter).IsAssignableFrom(entityInfo.EntityValue.GetType()))
                                {
                                    return;
                                }

                                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.InsertByObject)
                                {
                                    entityInfo.SetValue(tenantDbName);
                                }
                                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.UpdateByObject)
                                {
                                    entityInfo.SetValue(tenantDbName);
                                }
                                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.DeleteByObject)
                                {
                                    entityInfo.SetValue(tenantDbName);
                                }
                            };
                        }
                    }
                    if (!base.Context.Ado.IsValidConnection())
                    {
                        throw Oops.Oh("数据库连接错误");
                    }
                }
            }
            else
            {
                base.Context = base.Context.AsTenant().GetConnectionScope(tenantId);
            }
        }


        if (tenant.MultiSystem && httpContext != null) //tenant.MultiSystem
        {
            string systemConst = "ZxSystemId";

            string userId = httpContext?.User.FindFirst("UserId")?.Value;

            string systemId = null;
            if (!string.IsNullOrEmpty(userId))
            {
                systemId = _cacheManager.Get(userId + "_devSystemId");
            }

            if(string.IsNullOrEmpty(systemId)) systemId = httpContext.User.FindFirst(systemConst)?.Value;

            //组织架构子系统是管理所有子系统，不需要进行系统过虑
            if (systemId == "orgSystem")
            {
                return;
            }

            base.Context.QueryFilter.AddTableFilter<IZxSystemFilter>(it => it.ZxSystemId == systemId);
            base.Context.QueryFilter.Clear<IZxSystemFilter>(); //首次移除时出现异常，故前面先添加一个

            base.Context.QueryFilter.AddTableFilter<IZxSystemFilter>(it => it.ZxSystemId == systemId);                      

            base.Context.Aop.DataExecuting = (oldValue, entityInfo) =>
            {
                if (!typeof(IZxSystemFilter).IsAssignableFrom(entityInfo.EntityValue.GetType())) return;

                if (entityInfo.PropertyName == systemConst && entityInfo.OperationType == DataFilterType.InsertByObject)
                {
                    entityInfo.SetValue(systemId);
                }
                if (entityInfo.PropertyName == systemConst && entityInfo.OperationType == DataFilterType.UpdateByObject)
                {
                    entityInfo.SetValue(systemId);
                }
                if (entityInfo.PropertyName == systemConst && entityInfo.OperationType == DataFilterType.DeleteByObject)
                {
                    entityInfo.SetValue(systemId);
                }
            };
        }

        // 设置超时时间
        base.Context.Ado.CommandTimeOut = 30;

        base.Context.Aop.OnLogExecuting = (sql, pars) =>
        {
            if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Green;
            if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) || sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.White;
            if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Blue;
            // 在控制台输出sql语句
            Console.WriteLine("【" + DateTime.Now + "——执行SQL】\r\n" + UtilMethods.GetSqlString(base.Context.CurrentConnectionConfig.DbType, sql, pars) + "\r\n");
            //App.PrintToMiniProfiler("SqlSugar", "Info", sql + "\r\n" + base.Context.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
        };

        base.Context.Aop.OnError = (ex) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            var pars = base.Context.Utilities.SerializeObject(((SugarParameter[])ex.Parametres).ToDictionary(it => it.ParameterName, it => it.Value));
            Console.WriteLine("【" + DateTime.Now + "——错误SQL】\r\n" + UtilMethods.GetSqlString(base.Context.CurrentConnectionConfig.DbType, ex.Sql, (SugarParameter[])ex.Parametres) + "\r\n");
            //App.PrintToMiniProfiler("SqlSugar", "Error", $"{ex.Message}{Environment.NewLine}{ex.Sql}{pars}{Environment.NewLine}");
        };

        if (base.Context.CurrentConnectionConfig.DbType == DbType.Oracle)
        {
            base.Context.Aop.OnExecutingChangeSql = (sql, pars) =>
            {
                if (pars != null)
                {
                    foreach (var item in pars)
                    {
                        //如果是DbTppe=string设置成OracleDbType.Nvarchar2 
                        item.IsNvarchar2 = true;
                    }
                };
                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };
        }
    }
}