using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JNPF.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace JNPF.InteAssistant.Security;

/// <summary>
/// DisableQueryFilter 白名单守卫（千问 SEC-03 · 2026-06-20）。
///
/// 防护机制：
///   1. 调用栈自检：检查调用方方法是否标注 [RequiresFounder] 特性
///   2. 运行时角色校验：检查当前 HttpContext 用户角色是否为 founder
///   3. 双重校验通过后才允许调用 ISqlSugarClient.QueryFilter.Disable()
///
/// 使用方式：
///   [RequiresFounder]
///   public async Task SomeMethod() {
///       DisableQueryFilterGuard.Verify();  // 调用前校验
///       db.QueryFilter.Disable();          // 校验通过 → 允许
///   }
///
/// 违反时抛出 UnauthorizedAccessException。
/// </summary>
public static class DisableQueryFilterGuard
{
    /// <summary>
    /// 校验当前调用方是否有权禁用 QueryFilter。
    /// 检查调用栈中是否有标注 [RequiresFounder] 的方法 + 当前用户角色。
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">校验失败</exception>
    public static void Verify(IServiceProvider serviceProvider)
    {
        // 层 1：调用栈自检 — 调用方方法必须标注 [RequiresFounder]
        var callerMethod = GetCallerMethod();
        var requiresFounder = callerMethod?.GetCustomAttribute<RequiresFounderAttribute>();
        if (requiresFounder == null)
        {
            throw new UnauthorizedAccessException(
                $"DisableQueryFilter called from unauthorized method '{callerMethod?.Name}'. " +
                "The calling method must be decorated with [RequiresFounder] attribute.");
        }

        // 层 2：运行时角色校验 — 当前用户必须是 founder
        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor?.HttpContext;
        if (httpContext == null)
        {
            throw new UnauthorizedAccessException(
                "DisableQueryFilter called outside HTTP request context. DisableQueryFilter is restricted.");
        }

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "DisableQueryFilter called by unauthenticated user. Only founder can disable query filters.");
        }

        var isFounder = user.Claims.Any(c =>
            (c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role") &&
            c.Value.Equals("founder", StringComparison.OrdinalIgnoreCase));

        if (!isFounder)
        {
            throw new UnauthorizedAccessException(
                "DisableQueryFilter restricted: only founder role can disable query filters. " +
                $"Caller method: {callerMethod?.DeclaringType?.FullName}.{callerMethod?.Name}");
        }
    }

    /// <summary>
    /// 获取直接调用本方法的外部方法（跳过本类自身帧）。
    /// </summary>
    private static MethodBase? GetCallerMethod()
    {
        var stackTrace = new StackTrace(3, false);
        foreach (var frame in stackTrace.GetFrames())
        {
            var method = frame.GetMethod();
            if (method?.DeclaringType != typeof(DisableQueryFilterGuard))
            {
                return method;
            }
        }
        return null;
    }
}
