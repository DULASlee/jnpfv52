using JNPF.Authorization;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Manager;
using JNPF.DataEncryption;
using Microsoft.AspNetCore.Authorization;

namespace JNPF.API.Entry.Handlers;

/// <summary>
/// jwt处理程序.
/// </summary>
public class JwtHandler : AppAuthorizeHandler
{
    /// <summary>
    /// 重写 Handler 添加自动刷新.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task HandleAsync(AuthorizationHandlerContext context)
    {
        // 自动刷新Token
        if (JWTEncryption.AutoRefreshToken(context, context.GetCurrentHttpContext(),
            App.GetOptions<JWTSettingsOptions>().ExpiredTime))
        {
            await AuthorizeHandleAsync(context);
        }
        else
        {
            context.Fail(); // 授权失败
            DefaultHttpContext currentHttpContext = context.GetCurrentHttpContext();
            if (currentHttpContext == null)
                return;
            currentHttpContext.SignoutToSwagger();
        }
    }

    /// <summary>
    /// 授权判断逻辑，授权通过返回 true，否则返回 false.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public override async Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        // 此处已经自动验证 Jwt Token的有效性了，无需手动验证
        return await CheckAuthorzieAsync(httpContext);
    }

    /// <summary>
    /// 检查权限（渐进式恢复：白名单 + 权限组缓存校验）.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    private static async Task<bool> CheckAuthorzieAsync(DefaultHttpContext httpContext)
    {
        // 管理员跳过判断
        if (App.User.FindFirst(ClaimConst.CLAINMADMINISTRATOR)?.Value == ((int)AccountType.Administrator).ToString())
            return true;

        var path = httpContext.Request.Path.Value ?? "";

        // 白名单路径免检（可配置）
        if (IsWhitelistedPath(path))
            return true;

        // 默认路由(获取登录用户信息)
        if (httpContext.Request.Path.StartsWithSegments("/api/oauth/CurrentUser"))
            return true;

        // 获取用户权限组（带 Redis 缓存）
        var userManager = App.GetService<IUserManager>();
        if (userManager == null || string.IsNullOrEmpty(userManager.UserId))
            return false;

        var permissionGroups = await GetCachedPermissionGroupsAsync(userManager.TenantId, userManager.UserId);

        // 阶段 1：仅验证用户是否拥有有效权限组（非空 = 已授权）
        // 阶段 2+：扩展为路由级权限匹配（需要 menu-route 映射基础设施）
        return permissionGroups.Count > 0;
    }

    /// <summary>
    /// 检查路径是否在白名单中.
    /// </summary>
    private static bool IsWhitelistedPath(string path)
    {
        var whitelist = App.GetConfig<List<string>>("Auth:AllowAnonymousPaths");
        if (whitelist == null || whitelist.Count == 0)
        {
            // 默认白名单
            return path.StartsWith("/api/oauth/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/api/file/", StringComparison.OrdinalIgnoreCase);
        }

        return whitelist.Any(pattern =>
        {
            if (pattern.EndsWith("*"))
            {
                return path.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
            }
            return string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// 获取用户权限组（Redis 缓存，TTL 5 分钟）.
    /// </summary>
    private static async Task<List<string>> GetCachedPermissionGroupsAsync(string tenantId, string userId)
    {
        var cacheKey = $"jnpf:permission:groups:{tenantId}:{userId}";
        var cache = App.GetService<ICacheManager>();

        if (cache != null)
        {
            var cached = await cache.GetAsync<List<string>>(cacheKey);
            if (cached != null)
                return cached;
        }

        var userManager = App.GetService<IUserManager>();
        var permissionGroups = userManager?.GetPermissionByUserId(userId) ?? new List<string>();

        if (cache != null)
        {
            var cacheMinutes = App.GetConfig<int?>("Auth:PermissionCacheMinutes") ?? 5;
            await cache.SetAsync(cacheKey, permissionGroups, TimeSpan.FromMinutes(cacheMinutes));
        }

        return permissionGroups;
    }
}
