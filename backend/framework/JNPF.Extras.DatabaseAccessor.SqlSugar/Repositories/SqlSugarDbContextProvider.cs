using JNPF;
using JNPF.Common.Manager;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.FriendlyException;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlSugar;

/// <summary>
/// SqlSugar 数据库上下文提供器实现
/// 从 SqlSugarRepository 构造函数中提取的全部租户解析、系统过滤和 AOP 配置逻辑
/// </summary>
public class SqlSugarDbContextProvider : ISqlSugarDbContextProvider
{
    /// <summary>
    /// 全局单例 SqlSugarScope（通过 DI 注入，用于获取租户连接作用域）
    /// </summary>
    private readonly SqlSugarScope _rootScope;

    /// <summary>
    /// 缓存管理器（用于读取租户缓存和系统缓存）
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// HTTP 上下文访问器（用于获取当前用户声明和端点元数据）
    /// </summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 租户字段隔离时的 TenantId 值（仅字段隔离模式下有值）
    /// </summary>
    private string _fieldIsolationTenantDbName;

    /// <summary>
    /// 当前解析到的系统 ID（仅多系统模式下有值）
    /// </summary>
    private string _resolvedSystemId;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="rootContext">全局注册的 ISqlSugarClient（实际为 SqlSugarScope 单例）</param>
    /// <param name="cacheManager">缓存管理器（原仓储中通过 CreateScope 获取，现改为构造函数注入）</param>
    /// <param name="httpContextAccessor">HTTP 上下文访问器（原仓储中通过 App.HttpContext 获取，现改为构造函数注入）</param>
    public SqlSugarDbContextProvider(
        ISqlSugarClient rootContext,
        ICacheManager cacheManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _rootScope = (SqlSugarScope)rootContext;
        _cacheManager = cacheManager;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 获取当前请求对应的 SqlSugar 客户端
    /// </summary>
    public ISqlSugarClient GetDbContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // 读取配置（与原仓储构造函数保持一致）
        var connectionStrings = App.GetConfig<ConnectionStringsOptions>("ConnectionStrings", true);
        var tenant = App.GetConfig<TenantOptions>("Tenant", true);

        // 第一步：根据租户配置解析正确的数据库连接作用域
        ISqlSugarClient context = ResolveTenantConnection(connectionStrings, tenant, httpContext);

        // 第二步：应用系统级数据过滤（ZxSystemId）
        if (tenant.MultiSystem && httpContext != null)
        {
            ApplySystemFilter(context, httpContext);
        }

        // 第三步：统一配置 DataExecuting 回调
        // 修复原代码中的缺陷：当租户字段隔离和系统过滤同时生效时，原代码的 DataExecuting 被后者覆盖
        // 现在合并为一个回调，同时处理 TenantId 和 ZxSystemId 的自动填充
        ApplyDataExecutingFilter(context);

        // 第四步：配置通用 AOP（命令超时、SQL 日志、错误日志、Oracle 适配）
        ConfigureCommonAop(context);

        return context;
    }

    #region 租户连接解析

    /// <summary>
    /// 根据租户配置解析正确的数据库连接作用域
    /// </summary>
    /// <param name="connectionStrings">数据库连接配置</param>
    /// <param name="tenant">多租户配置</param>
    /// <param name="httpContext">当前 HTTP 上下文</param>
    /// <returns>已切换到正确连接作用域的 ISqlSugarClient</returns>
    private ISqlSugarClient ResolveTenantConnection(
        ConnectionStringsOptions connectionStrings,
        TenantOptions tenant,
        HttpContext httpContext)
    {
        // 匿名接口不进行租户解析，直接返回默认连接
        if (httpContext?.GetEndpoint()?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            return _rootScope;
        }

        string tenantId = connectionStrings.DefaultConnectionConfig.ConfigId.ToString();

        if (tenant.MultiTenancy && httpContext != null)
        {
            // 多租户模式：从当前用户的声明中获取租户 ID
            tenantId = httpContext.User.FindFirst("TenantId")?.Value;

            // 从缓存中获取租户配置信息
            var tenantCacheList = _cacheManager.Get<List<GlobalTenantCacheModel>>("jnpf:global:tenant");
            var tenantCache = tenantCacheList?.Find(it => it.TenantId.Equals(tenantId));

            if (tenantCache == null)
            {
                // 缓存中未找到租户配置，回退到默认连接
                return _rootScope;
            }

            // 确保当前 SqlSugarScope 已注册该租户的数据库连接
            if (!_rootScope.AsTenant().IsAnyConnection(tenantCache.connectionConfig.ConfigId))
            {
                _rootScope.AsTenant().AddConnection(JNPFTenantExtensions.GetConfig(tenantCache.connectionConfig));
            }

            // 切换到租户对应的连接作用域
            var scopedContext = _rootScope.AsTenant().GetConnectionScope(tenantCache.connectionConfig.ConfigId);

            // 处理字段隔离模式（type == 1 表示字段隔离）
            if (tenantCache.type == 1)
            {
                _fieldIsolationTenantDbName = tenantCache.connectionConfig.IsolationField;

                if (!"default".Equals(tenantId))
                {
                    // 清除默认查询过滤器，添加租户字段过滤
                    scopedContext.QueryFilter.Clear();
                    scopedContext.QueryFilter.AddTableFilter<ITenantFilter>(
                        it => it.TenantId == _fieldIsolationTenantDbName);
                }
            }

            // 验证数据库连接可用性
            if (!scopedContext.Ado.IsValidConnection())
            {
                throw Oops.Oh("数据库连接错误");
            }

            return scopedContext;
        }
        else
        {
            // 非多租户模式：使用默认连接 ID 对应的连接作用域
            return _rootScope.AsTenant().GetConnectionScope(tenantId);
        }
    }

    #endregion

    #region 系统过滤

    /// <summary>
    /// 应用系统级数据过滤（ZxSystemId）
    /// </summary>
    /// <param name="context">已解析的 SqlSugar 客户端</param>
    /// <param name="httpContext">当前 HTTP 上下文</param>
    private void ApplySystemFilter(ISqlSugarClient context, HttpContext httpContext)
    {
        const string systemConst = "ZxSystemId";

        string userId = httpContext.User.FindFirst("UserId")?.Value;

        // 优先级 1：从缓存获取开发者当前选中的系统 ID
        string systemId = null;
        if (!string.IsNullOrEmpty(userId))
        {
            systemId = _cacheManager.Get(userId + "_devSystemId");
        }

        // 优先级 2：从用户声明中获取系统 ID
        if (string.IsNullOrEmpty(systemId))
        {
            systemId = httpContext.User.FindFirst(systemConst)?.Value;
        }

        // 组织架构子系统管理所有子系统，不进行系统过滤
        if (systemId == "orgSystem")
        {
            return;
        }

        _resolvedSystemId = systemId;

        // 添加系统过滤器
        // 注意：此处保留原代码的写法——先添加、再清除、再添加
        // 目的是防止首次添加时出现已注册异常（原代码注释说明）
        context.QueryFilter.AddTableFilter<IZxSystemFilter>(it => it.ZxSystemId == _resolvedSystemId);
        context.QueryFilter.Clear<IZxSystemFilter>();
        context.QueryFilter.AddTableFilter<IZxSystemFilter>(it => it.ZxSystemId == _resolvedSystemId);
    }

    #endregion

    #region DataExecuting 统一回调

    /// <summary>
    /// 统一配置 DataExecuting 回调
    /// 同时处理租户字段隔离的 TenantId 自动填充和系统过滤的 ZxSystemId 自动填充
    /// </summary>
    /// <param name="context">已解析的 SqlSugar 客户端</param>
    /// <remarks>
    /// 修复原代码缺陷：
    /// 原 SqlSugarRepository 构造函数中，租户过滤和系统过滤分别设置 Aop.DataExecuting，
    /// 后者会覆盖前者，导致同时启用两种过滤时 TenantId 自动填充失效。
    /// 现在合并为一个回调方法，按顺序检查两种过滤条件。
    /// </remarks>
    private void ApplyDataExecutingFilter(ISqlSugarClient context)
    {
        // 两种过滤都不需要时，不设置回调
        if (_fieldIsolationTenantDbName == null && _resolvedSystemId == null)
        {
            return;
        }

        context.Aop.DataExecuting = (oldValue, entityInfo) =>
        {
            var entityType = entityInfo.EntityValue.GetType();

            // 仅在 Insert / Update / Delete 操作时处理
            var isWriteOperation =
                entityInfo.OperationType == DataFilterType.InsertByObject ||
                entityInfo.OperationType == DataFilterType.UpdateByObject ||
                entityInfo.OperationType == DataFilterType.DeleteByObject;

            if (!isWriteOperation) return;

            // 租户字段自动填充（仅字段隔离模式生效）
            if (_fieldIsolationTenantDbName != null
                && typeof(ITenantFilter).IsAssignableFrom(entityType)
                && entityInfo.PropertyName == "TenantId")
            {
                entityInfo.SetValue(_fieldIsolationTenantDbName);
            }

            // 系统字段自动填充
            if (_resolvedSystemId != null
                && typeof(IZxSystemFilter).IsAssignableFrom(entityType)
                && entityInfo.PropertyName == "ZxSystemId")
            {
                entityInfo.SetValue(_resolvedSystemId);
            }
        };
    }

    #endregion

    #region 通用 AOP 配置

    /// <summary>
    /// 配置通用 AOP：命令超时、SQL 日志、错误日志、Oracle 适配
    /// </summary>
    /// <param name="context">已解析的 SqlSugar 客户端</param>
    /// <remarks>
    /// TODO: 日志输出当前使用 Console.WriteLine，应替换为 Serilog 结构化日志
    /// （与已迭代的日志管理系统保持一致）
    /// </remarks>
    private void ConfigureCommonAop(ISqlSugarClient context)
    {
        // 设置命令超时时间
        context.Ado.CommandTimeOut = 30;

        // SQL 执行前日志
        context.Aop.OnLogExecuting = (sql, pars) =>
        {
            if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Green;
            if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
                || sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.White;
            if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Blue;

            Console.WriteLine(
                "【" + DateTime.Now + "——执行SQL】\r\n"
                + UtilMethods.GetSqlString(context.CurrentConnectionConfig.DbType, sql, pars)
                + "\r\n");
        };

        // SQL 执行错误日志
        context.Aop.OnError = (ex) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(
                "【" + DateTime.Now + "——错误SQL】\r\n"
                + UtilMethods.GetSqlString(
                    context.CurrentConnectionConfig.DbType,
                    ex.Sql,
                    (SugarParameter[])ex.Parametres)
                + "\r\n");
        };

        // Oracle 数据库特殊处理：所有字符串参数使用 Nvarchar2
        if (context.CurrentConnectionConfig.DbType == DbType.Oracle)
        {
            context.Aop.OnExecutingChangeSql = (sql, pars) =>
            {
                if (pars != null)
                {
                    foreach (var item in pars)
                    {
                        item.IsNvarchar2 = true;
                    }
                }
                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };
        }
    }

    #endregion
}
