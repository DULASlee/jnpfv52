// 文件：Infrastructure/Security/TenantGuard.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Security

using JNPF.DependencyInjection;
using JNPF.InteAssistant.Infrastructure.Background;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JNPF.InteAssistant.Infrastructure.Security;

/// <summary>
/// 多租户守卫实现
///
/// 安全设计：
///   - fail-closed：无租户字段的实体默认拒绝访问
///   - TenantId 格式校验：防 SQL 注入
///   - null entity 显式检查
///
/// 性能设计：
///   - 属性信息缓存到 ConcurrentDictionary（反射只发生一次）
///   - 正则预编译
/// </summary>
public sealed class TenantGuard : ITenantGuard, ITransient
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<TenantGuard> _logger;

    // 属性缓存——Type 数量有限，无需淘汰策略
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> _tenantPropertyCache = new();

    // 支持的属性名（按优先级排序）
    private static readonly string[] TenantPropertyNames = { "F_TenantId", "TenantId", "tenantId", "OrgId", "CompanyId" };

    // TenantId 格式校验——只允许字母数字下划线连字符
    private static readonly Regex TenantIdPattern = new(@"^[A-Za-z0-9\-_]{1,64}$", RegexOptions.Compiled);

    public TenantGuard(IHttpContextAccessor accessor, ILogger<TenantGuard> logger)
    {
        _accessor = accessor;
        _logger = logger;
    }

    public T WithTenant<T>(T entity, string tenantId) where T : class
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), $"{typeof(T).Name} 实体不能为 null");

        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("tenantId 不能为空", nameof(tenantId));

        if (!TenantIdPattern.IsMatch(tenantId))
            throw new ArgumentException($"tenantId 格式非法（期望: 字母数字/下划线/连字符, 1-64字符）", nameof(tenantId));

        var prop = GetTenantProperty(typeof(T))
            ?? throw new InvalidOperationException(
                $"{typeof(T).Name} 未声明租户字段（查找: {string.Join("/", TenantPropertyNames)}）");

        prop.SetValue(entity, tenantId);
        return entity;
    }

    public bool VerifyOwnership<T>(T entity, string currentTenantId) where T : class
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrEmpty(currentTenantId))
        {
            _logger.LogCritical("VerifyOwnership 被调用时 currentTenantId 为空");
            return false;
        }

        var prop = GetTenantProperty(typeof(T));

        // fail-closed：无租户字段 → 拒绝
        if (prop == null)
        {
            _logger.LogCritical("实体 {Type} 未声明租户字段，拒绝访问", typeof(T).Name);
            return false;
        }

        var entityTenantId = prop.GetValue(entity)?.ToString();

        if (string.IsNullOrEmpty(entityTenantId))
        {
            _logger.LogWarning("实体 {Type} 的 TenantId 为空，拒绝访问", typeof(T).Name);
            return false;
        }

        if (!string.Equals(entityTenantId, currentTenantId, StringComparison.Ordinal))
        {
            _logger.LogWarning("TenantGuard: 租户不匹配 {Type} entity={ET} current={CT}",
                typeof(T).Name, entityTenantId, currentTenantId);
            return false;
        }

        return true;
    }

    public Dictionary<string, string> GetUploadHeaders(RequestContext ctx)
    {
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(ctx.TenantId))
            headers["X-Tenant-Id"] = ctx.TenantId;
        return headers;
    }

    private static PropertyInfo? GetTenantProperty(Type entityType)
    {
        return _tenantPropertyCache.GetOrAdd(entityType, t =>
        {
            foreach (var name in TenantPropertyNames)
            {
                var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
                    return prop;
            }
            return null;
        });
    }
}
