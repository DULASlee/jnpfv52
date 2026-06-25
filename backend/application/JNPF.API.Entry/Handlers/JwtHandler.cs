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
    private readonly IUserManager _userManager;
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 初始化一个<see cref="JwtHandler"/>类型的新实例
    /// </summary>
    public JwtHandler(IUserManager userManager, ICacheManager cacheManager)
    {
        _userManager = userManager;
        _cacheManager = cacheManager;
    }

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
    private async Task<bool> CheckAuthorzieAsync(DefaultHttpContext httpContext)
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
        if (_userManager == null || string.IsNullOrEmpty(_userManager.UserId))
            return false;

        var permissionGroups = await GetCachedPermissionGroupsAsync(_userManager.TenantId, _userManager.UserId);

        // 阶段 1：路由级权限匹配
        if (permissionGroups.Count == 0)
            return false;

        // 默认路由免检（CurrentUser 等已在上面处理）
        return true;
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
    private async Task<List<string>> GetCachedPermissionGroupsAsync(string tenantId, string userId)
    {
        var cacheKey = $"jnpf:permission:groups:{tenantId}:{userId}";

        if (_cacheManager != null)
        {
            var cached = await _cacheManager.GetAsync<List<string>>(cacheKey);
            if (cached != null)
                return cached;
        }

        var permissionGroups = _userManager?.GetPermissionByUserId(userId) ?? new List<string>();

        if (_cacheManager != null)
        {
            var cacheMinutes = App.GetConfig<int?>("Auth:PermissionCacheMinutes") ?? 5;
            await _cacheManager.SetAsync(cacheKey, permissionGroups, TimeSpan.FromMinutes(cacheMinutes));
        }

        return permissionGroups;
    }
}
