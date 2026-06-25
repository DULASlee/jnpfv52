using JNPF.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using JNPF.Modules;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// Docker 沙箱调度模块 (Phase 6 Day 3-5).
/// 注册 SandboxManager + 清理后台服务.
/// </summary>
public class SandboxModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 沙箱管理器（单例，共享并发信号量）
        services.AddSingleton<ISandboxManager, SandboxManager>();

        // 超时清理后台服务
        services.AddHostedService<SandboxCleanupService>();
    }
}
