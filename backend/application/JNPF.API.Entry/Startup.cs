using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
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

        // 按拓扑顺序调用各模块的 Configure
        app.UseJnpfModules(_modules);
    }
}
