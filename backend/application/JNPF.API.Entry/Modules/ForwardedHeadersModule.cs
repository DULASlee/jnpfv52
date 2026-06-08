using JNPF.Modules;
using Microsoft.AspNetCore.HttpOverrides;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// Nginx 反向代理转发头模块.
/// </summary>
public class ForwardedHeadersModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }
}
