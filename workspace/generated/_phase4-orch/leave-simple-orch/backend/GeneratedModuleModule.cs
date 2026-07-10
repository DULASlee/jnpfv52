using JNPF.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JNPF.Generated;

/// <summary>
/// AI 生成业务模块 — GeneratedModuleModule
/// 得益于 JnpfModule 自动发现，无需修改 Program.cs。
/// 所有 Service 实现 IDynamicApiController，路由自动映射。
/// </summary>
[JNPF.Modules.DependsOn]
public class GeneratedModuleModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 模块 DI 注册（如需自定义服务，在此注册）
        // Entity/Service 类通过 ITransient/IScoped/ISingleton 自动注册
    }

    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        // 模块中间件注册（如需自定义中间件，在此注册）
    }
}
