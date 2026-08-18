using JNPF.Authorization;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Manager;
using JNPF.DataEncryption;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace JNPF.API.Entry.Handlers;

/// <summary>
/// jwt处理程序.
/// </summary>
public class JwtHandler : AppAuthorizeHandler
{
    private readonly IUserManager _userManager;
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<JwtHandler> _logger;
    private readonly JWTSettingsOptions _jwtSettings;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 路由授权策略（从 Auth:RoutePolicy 配置读取）.
    /// </summary>
    private enum RouteAuthPolicy
    {
        /// <summary>策略 C：无声明放行 + Warning 日志</summary>
        GradualEnforcement,
        /// <summary>策略 A：无声明返回 403</summary>
        StrictEnforcement
    }

    /// <summary>
    /// 初始化一个<see cref="JwtHandler"/>类型的新实例
    /// </summary>
    public JwtHandler(IUserManager userManager, ICacheManager cacheManager, ILogger<JwtHandler> logger,
        IOptions<JWTSettingsOptions> jwtOptions, IConfiguration configuration)
    {
        _userManager = userManager;
        _cacheManager = cacheManager;
        _logger = logger;
        _jwtSettings = jwtOptions.Value;
        _configuration = configuration;
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
            _jwtSettings.ExpiredTime))
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
    /// 检查权限（策略 C：已声明路由严格匹配 + 未声明路由放行记日志）.
    /// </summary>
    private async Task<bool> CheckAuthorzieAsync(DefaultHttpContext httpContext)
    {
        // ── 1. 管理员直接放行 ──
        if (await IsAdministratorCachedAsync())
            return true;

        var path = httpContext.Request.Path.Value ?? "";

        // ── 2. 白名单路径免检 ──
        if (IsWhitelistedPath(path))
            return true;

        // ── 3. 提取端点元数据 ──
        var endpoint = httpContext.GetEndpoint();
        var hasAllowAnonymous = endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null;
        var securityDefine = endpoint?.Metadata.GetMetadata<SecurityDefineAttribute>();
        var resourceId = securityDefine?.ResourceId;

        // ── 4. [AllowAnonymous] → 放行 ──
        if (hasAllowAnonymous)
            return true;

        // ── 5. 获取用户权限组（无权限组 → 拒绝）──
        if (_userManager == null || string.IsNullOrEmpty(_userManager.UserId))
            return false;

        var permissionGroups = await GetCachedPermissionGroupsAsync(_userManager.TenantId, _userManager.UserId);
        if (permissionGroups.Count == 0)
            return false;

        // ── 6. [SecurityDefine] → 严格匹配授权资源 ──
        if (!string.IsNullOrEmpty(resourceId))
        {
            var authorized = await GetCachedAuthorizedResourcesAsync(_userManager.TenantId, _userManager.UserId);
            return authorized.Contains(resourceId);
        }

        // ── 7. 无属性声明 → 策略分支 ──
        var policy = GetRouteAuthPolicy();

        if (policy == RouteAuthPolicy.StrictEnforcement)
        {
            // 策略 A：拒绝
            _logger.LogWarning(
                "[RouteAuth:BLOCKED] 未声明权限属性的路由被拒绝: Path={Path}, UserId={UserId}, TenantId={TenantId}",
                path, _userManager.UserId, _userManager.TenantId);
            return false;
        }

        // 策略 C (GradualEnforcement)：放行 + 记录裸奔 API
        _logger.LogWarning(
            "[RouteAuth:PASSTHROUGH] 未声明权限属性的路由被放行: Path={Path}, UserId={UserId}, TenantId={TenantId}, Time={Time}",
            path, _userManager.UserId, _userManager.TenantId, DateTime.UtcNow.ToString("O"));
        return true;
    }

    /// <summary>
    /// 获取当前路由授权策略.
    /// </summary>
    private RouteAuthPolicy GetRouteAuthPolicy()
    {
        var configValue = _configuration["Auth:RoutePolicy"];
        return string.Equals(configValue, "StrictEnforcement", StringComparison.OrdinalIgnoreCase)
            ? RouteAuthPolicy.StrictEnforcement
            : RouteAuthPolicy.GradualEnforcement;
    }

    /// <summary>
    /// 获取用户已授权的资源ID集合（Redis 缓存，TTL 5 分钟）.
    /// </summary>
    private async Task<HashSet<string>> GetCachedAuthorizedResourcesAsync(string tenantId, string userId)
    {
        var cacheKey = $"jnpf:authorized:resources:{tenantId}:{userId}";

        if (_cacheManager != null)
        {
            var cached = await _cacheManager.GetAsync<HashSet<string>>(cacheKey);
            if (cached != null)
                return cached;
        }

        var resources = await _userManager.GetAuthorizedResourceIdsAsync(userId);

        if (_cacheManager != null)
        {
            var cacheMinutes = _configuration.GetValue<int?>("Auth:PermissionCacheMinutes") ?? 5;
            await _cacheManager.SetAsync(cacheKey, resources, TimeSpan.FromMinutes(cacheMinutes));
        }

        return resources;
    }

    /// <summary>
    /// 检查管理员状态（Redis 缓存，TTL 5 分钟）.
    /// </summary>
    private async Task<bool> IsAdministratorCachedAsync()
    {
        if (_userManager == null || string.IsNullOrEmpty(_userManager.UserId))
            return false;

        var cacheKey = $"jnpf:user:isadmin:{_userManager.TenantId}:{_userManager.UserId}";

        if (_cacheManager != null)
        {
            var cached = await _cacheManager.GetAsync<bool?>(cacheKey);
            if (cached.HasValue)
                return cached.Value;
        }

        var isAdmin = App.User.FindFirst(ClaimConst.CLAINMADMINISTRATOR)?.Value
            == ((int)AccountType.Administrator).ToString();

        if (_cacheManager != null)
        {
            var cacheMinutes = _configuration.GetValue<int?>("Auth:PermissionCacheMinutes") ?? 5;
            await _cacheManager.SetAsync(cacheKey, isAdmin, TimeSpan.FromMinutes(cacheMinutes));
        }

        return isAdmin;
    }

    /// <summary>
    /// 检查路径是否在白名单中.
    /// </summary>
    private bool IsWhitelistedPath(string path)
    {
        var whitelist = _configuration.GetSection("Auth:AllowAnonymousPaths").Get<List<string>>();
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
            var cacheMinutes = _configuration.GetValue<int?>("Auth:PermissionCacheMinutes") ?? 5;
            await _cacheManager.SetAsync(cacheKey, permissionGroups, TimeSpan.FromMinutes(cacheMinutes));
        }

        return permissionGroups;
    }
}
