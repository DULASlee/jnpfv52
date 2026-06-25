using System.Linq.Expressions;
using JNPF.FriendlyException;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.Common.Core.MultiTenancy;

/// <summary>
/// 多租户解析器 — 全系统唯一入口
/// ═══════════════════════════════════════════════════════
///
/// 【铁律】（参见 IRON_RULES.md R1）
/// 1. 禁止自行从 JWT 解析 TenantId → 必须调用此类
/// 2. 禁止硬编码租户 ID → 从 appsettings MultiTenancy 配置读取
/// 3. 新增数据库查询必须加 ApplyTenantFilter → 漏加 = 安全漏洞
/// 4. admin 的 tenantId 由代码逻辑决定，数据库 f_tenant_id 保持 NULL
///
/// 【租户ID体系】
///   平台租户（默认0）= 超级租户（上帝视角）
///     ├─ 不参与业务数据过滤
///     ├─ 只有 isAdministrator=1 且通过资格校验的用户可持有
///     └─ 用于过 LLM Gateway 校验 + 系统级操作
///   1+ = 普通业务租户
///     ├─ 严格按 tenantId 过滤数据
///     └─ 租户间数据完全隔离
///
/// 【防越权核心逻辑】
///   JWT 中 TenantId=平台租户ID 的请求，必须同时满足 isAdministrator=1
///   否则返回 -1（未授权），业务查询返回空集
/// ═══════════════════════════════════════════════════════
/// </summary>
public static class TenantResolver
{
    private static long _platformTenantId = 0;
    private static long _platformOrgId = 0;
    private static volatile bool _initialized;
    private static ILogger? _logger;
    private static readonly object _lock = new();

    /// <summary>
    /// 首次调用时自动从 App.Configuration 初始化（无需 Program.cs 显式调用）
    /// 若需要指定 ILoggerFactory，可在启动时显式调用 Initialize() 一次
    /// </summary>
    public static void Initialize(IConfiguration? configuration = null, ILoggerFactory? loggerFactory = null)
    {
        configuration ??= App.Configuration;
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            Volatile.Write(ref _platformTenantId, configuration.GetValue("MultiTenancy:PlatformTenantId", 0L));
            Volatile.Write(ref _platformOrgId, configuration.GetValue("MultiTenancy:PlatformOrgId", 0L));
            _logger = loggerFactory?.CreateLogger("TenantResolver");
            _initialized = true;
            _logger?.LogInformation(
                "TenantResolver 初始化完成: PlatformTenantId={TenantId}, PlatformOrgId={OrgId}",
                Volatile.Read(ref _platformTenantId), Volatile.Read(ref _platformOrgId));
        }
    }

    /// <summary>
    /// 确保已初始化（懒初始化——兼容 Furion Serve.Run 启动模式）
    /// </summary>
    private static void EnsureInit()
    {
        if (!_initialized) Initialize();
    }

    /// <summary>平台租户 ID（首次访问时自动初始化）</summary>
    public static long PlatformTenantId { get { EnsureInit(); return Volatile.Read(ref _platformTenantId); } }

    // ═══════════════════════════════════════════════════
    // 核心方法
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 获取当前用户的 TenantId
    /// </summary>
    public static long Resolve()
    {
        EnsureInit();
        var httpContext = App.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return -1;

        var claim = httpContext.User.FindFirst("TenantId")?.Value;

        if (long.TryParse(claim, out var id) && id >= 0)
        {
            // 防越权：平台租户只有管理员可持有
            if (id == Volatile.Read(ref _platformTenantId) && !IsAdministrator())
            {
                _logger?.LogWarning(
                    "安全告警：非管理员尝试持有平台租户。Claim={Claim}, UserId={UserId}",
                    claim, GetUserId());
                return -1;
            }
            return id;
        }

        // "default" / null / 空 / 无效值
        if (IsAdministrator())
        {
            _logger?.LogWarning(
                "管理员 TenantId 无效({Claim})，回退为平台租户 {PlatformId}",
                claim ?? "null", _platformTenantId);
            return Volatile.Read(ref _platformTenantId);
        }

        _logger?.LogWarning(
            "用户 TenantId 无效({Claim})且非管理员。UserId={UserId}",
            claim ?? "null", GetUserId());
        return -1;
    }

    /// <summary>
    /// 获取当前用户的 OrgId
    /// </summary>
    public static long ResolveOrgId()
    {
        EnsureInit();
        var httpContext = App.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return -1;

        var claim = httpContext.User.FindFirst("OrgId")?.Value;

        if (long.TryParse(claim, out var id) && id >= 0)
        {
            if (id == Volatile.Read(ref _platformOrgId) && !IsAdministrator())
            {
                _logger?.LogWarning("安全告警：非管理员尝试持有平台组织。Claim={Claim}", claim);
                return -1;
            }
            return id;
        }

        if (IsAdministrator())
            return Volatile.Read(ref _platformOrgId);

        return -1;
    }

    // ═══════════════════════════════════════════════════
    // 判断方法
    // ═══════════════════════════════════════════════════

    /// <summary>当前用户是否为超级租户（上帝视角）</summary>
    public static bool IsSuperTenant() => IsAdministrator() && Resolve() == Volatile.Read(ref _platformTenantId);

    /// <summary>当前用户是否为管理员</summary>
    public static bool IsAdministrator()
    {
        var claim = App.HttpContext?.User?.FindFirst("Administrator")?.Value;
        return claim == "1" || string.Equals(claim, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>当前用户是否已登录</summary>
    public static bool IsAuthenticated() => App.HttpContext?.User?.Identity?.IsAuthenticated == true;

    // ═══════════════════════════════════════════════════
    // 查询过滤方法
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 给查询附加租户过滤（long 类型字段）
    /// </summary>
    public static IQueryable<T> ApplyTenantFilter<T>(
        IQueryable<T> query,
        Expression<Func<T, long>> tenantIdSelector) where T : class
    {
        var tenantId = Resolve();
        if (tenantId < 0) return query.Where(x => false);
        if (tenantId == Volatile.Read(ref _platformTenantId)) return query;

        var param = tenantIdSelector.Parameters[0];
        var body = Expression.Equal(tenantIdSelector.Body, Expression.Constant(tenantId));
        return query.Where(Expression.Lambda<Func<T, bool>>(body, param));
    }

    /// <summary>
    /// 给查询附加租户过滤（string 类型字段）
    /// </summary>
    public static IQueryable<T> ApplyTenantFilterString<T>(
        IQueryable<T> query,
        Expression<Func<T, string>> tenantIdSelector) where T : class
    {
        var tenantId = Resolve();
        if (tenantId < 0) return query.Where(x => false);
        if (tenantId == Volatile.Read(ref _platformTenantId)) return query;

        var tenantIdStr = tenantId.ToString();
        var param = tenantIdSelector.Parameters[0];
        var body = Expression.Equal(tenantIdSelector.Body, Expression.Constant(tenantIdStr));
        return query.Where(Expression.Lambda<Func<T, bool>>(body, param));
    }

    // ═══════════════════════════════════════════════════
    // 外部服务调用专用
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 获取传给 SA Service / LLM Gateway 的 TenantId
    /// 无效租户直接拒绝，不兜底为平台租户
    /// </summary>
    public static string ResolveForExternalService()
    {
        var tenantId = Resolve();
        if (tenantId < 0)
        {
            _logger?.LogError("拒绝外部服务调用：无法解析租户信息。UserId={UserId}", GetUserId());
            throw Oops.Bah("无法解析租户信息，拒绝外部服务调用");
        }
        return tenantId.ToString();
    }

    // ═══════════════════════════════════════════════════
    // 辅助
    // ═══════════════════════════════════════════════════

    private static string GetUserId()
    {
        return App.HttpContext?.User?.FindFirst("UserId")?.Value ?? "anonymous";
    }
}
