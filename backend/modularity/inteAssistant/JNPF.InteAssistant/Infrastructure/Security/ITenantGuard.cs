// 文件：Infrastructure/Security/ITenantGuard.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Security

using JNPF.InteAssistant.Infrastructure.Background;

namespace JNPF.InteAssistant.Infrastructure.Security;

/// <summary>
/// 多租户守卫接口
///
/// 安全原则：fail-closed（无租户字段 → 拒绝，不放行）
///
/// 用法：
///   // 插入前
///   _tenantGuard.WithTenant(entity, ctx.TenantId);
///   await db.Insertable(entity).ExecuteCommandAsync();
///
///   // 查询后
///   if (!_tenantGuard.VerifyOwnership(entity, ctx.TenantId))
///       return Forbid();
/// </summary>
public interface ITenantGuard
{
    /// <summary>
    /// 包装实体的 TenantId——插入数据库前必须调用
    /// </summary>
    T WithTenant<T>(T entity, string tenantId) where T : class;

    /// <summary>
    /// 校验实体归属——查询后必须调用
    /// fail-closed：无租户字段或 TenantId 不匹配 → 返回 false
    /// </summary>
    bool VerifyOwnership<T>(T entity, string currentTenantId) where T : class;

    /// <summary>
    /// 获取文件上传 headers（带 X-Tenant-Id）
    /// </summary>
    Dictionary<string, string> GetUploadHeaders(RequestContext ctx);
}
