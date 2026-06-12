using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using JNPF.InteAssistant;
using JNPF.Modules;

namespace JNPF.API.Entry;

public class Startup : AppStartup
{
    private IReadOnlyList<JnpfModule> _modules;

    public void ConfigureServices(IServiceCollection services)
    {
        _modules = services.AddJnpfModules();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 租户上下文中间件（ADR-003 铁律：try/finally 清除 AsyncLocal）
        app.UseMiddleware<TenantMiddleware>();

        // 创始人认证守卫（Sprint 0-B 地桩 #5，Phase 0 = /api/founder → 404）
        app.UseMiddleware<FounderGuardMiddleware>();

        // 按拓扑顺序调用各模块的 Configure
        app.UseJnpfModules(_modules);
    }
}
