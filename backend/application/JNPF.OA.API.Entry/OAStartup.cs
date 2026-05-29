using IGeekFan.AspNetCore.Knife4jUI;
using JNPF.API.Entry.Handlers;
using JNPF.Common.Cache;
using JNPF.Common.Core;
using JNPF.Common.Core.Filter;
using JNPF.Common.Core.Handlers;
using JNPF.EventHandler;
using JNPF.SpecificationDocument;
using JNPF.UnifyResult;
using JNPF.VirtualFileServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.RegisterServices;
using SqlSugar;

namespace JNPF.API.Entry;

public class OAStartup : Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 不调用 base.ConfigureServices()，因为 Furion 框架已自动发现并独立执行
        // Startup.ConfigureServices()。重复调用会导致 "Scheme already exists: Bearer" 异常。
        // 在此处仅添加 OA 项目特有的服务注册。
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting)
    {
        // 不调用 base.Configure()，原因同上。
        // 在此处仅添加 OA 项目特有的中间件。
    }
}