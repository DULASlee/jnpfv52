using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JNPF.Modules;

/// <summary>
/// 旧 AppStartup 桥接模块.
/// 拓扑排序中始终排在最前，保证旧系统先初始化.
/// 所有新 JnpfModule 必须通过 [DependsOn(typeof(LegacyModule))] 声明依赖.
/// </summary>
[DependsOn] // 无依赖 — 排在拓扑序最前
public sealed class LegacyModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 桥接：旧的 AddStartups() 已在 AddApp() 中调用
        // 此模块仅作为拓扑排序的锚点，确保所有新模块依赖它
        // 从而保证新模块在旧系统之后初始化
    }
}
