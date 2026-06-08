using JNPF.Modules;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.RegisterServices;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// 微信 SDK 模块.
/// </summary>
[JNPF.Modules.DependsOn(typeof(JsonSettingsModule))]
public class WeixinModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSenparcGlobalServices(configuration)
                .AddSenparcWeixinServices(configuration);
        services.AddSession();
    }

    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        var senparcSetting = app.ApplicationServices.GetRequiredService<IOptions<SenparcSetting>>();
        var senparcWeixinSetting = app.ApplicationServices.GetRequiredService<IOptions<SenparcWeixinSetting>>();

        IRegisterService register = RegisterService.Start(senparcSetting.Value).UseSenparcGlobal();
        register.UseSenparcWeixin(senparcWeixinSetting.Value, senparcSetting.Value);
    }
}
