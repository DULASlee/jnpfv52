using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using Microsoft.AspNetCore.Http;

namespace JNPF.Tests.ADR012;

/// <summary>
/// 可配置的 ITenantContext 测试替身.
/// 直接设置属性，无需 DI 容器.
/// </summary>
public class MockTenantContext : ITenantContext
{
    public string TenantId { get; set; } = "";
    public string SystemId { get; set; } = "";
    public string UserId { get; set; } = "";
    public TenantConnectionInfo? ConnectionInfo { get; set; }
    public int IsolationType { get; set; }
    public string IsolationFieldValue { get; set; } = "";
    public bool IsMultiTenant { get; set; }
    public bool IsMultiSystem { get; set; }
    public bool ShouldSkipSystemFilter { get; set; }

    // 以下方法在单元测试中不使用，抛出 NotImplementedException
    public void SetFromHttpContext(HttpContext httpContext) => throw new NotImplementedException();
    public void SetExplicit(string tenantId, string systemId = null) => throw new NotImplementedException();
    public void SetFromEvent(object eventSource) => throw new NotImplementedException();
    public IDisposable BeginScope(TenantInfo info) => throw new NotImplementedException();
    public void ClearScope() => throw new NotImplementedException();

    public bool IsDefaultTenant() => string.IsNullOrEmpty(TenantId) || TenantId == "default";
}
