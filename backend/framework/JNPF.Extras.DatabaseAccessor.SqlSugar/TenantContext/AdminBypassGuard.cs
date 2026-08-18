using System;
using JNPF;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 超管租户过滤豁免守卫（R4 方案 A）
/// <para>
/// 镜像 <c>JNPF.Common.Core.MultiTenancy.TenantResolver.IsAdministrator</c> 的 claim 解析逻辑，
/// 避免 framework 层反向依赖 common 层。超管（Administrator claim=1）跨租户管理，不附加 ITenantFilter。
/// </para>
/// <para>背景：admin 用户的 BASE_USER.F_TENANT_ID 可能与请求租户不一致（如 NULL），ITenantFilter 会误杀
/// 导致 UserEntity 查询返回 null → CurrentUser NRE。详见 workspace/debug_report.md (2026-07-07)。</para>
/// </summary>
internal static class AdminBypassGuard
{
    public static bool IsAdministrator()
    {
        var claim = App.HttpContext?.User?.FindFirst("Administrator")?.Value;
        return claim == "1" || string.Equals(claim, "true", StringComparison.OrdinalIgnoreCase);
    }
}
