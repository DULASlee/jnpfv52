using JNPF.API.Entry.Handlers;
using JNPF.API.Entry.Infrastructure;
using JNPF.Modules;
using JNPF.VirtualFileServer;
using JNPF.SpecificationDocument;
using JNPF.UnifyResult;
using IGeekFan.AspNetCore.Knife4jUI;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// JWT 认证 + CORS + 远程请求 + Health 端点模块.
/// </summary>
[JNPF.Modules.DependsOn(
    typeof(JsonSettingsModule),
    typeof(RateLimitingModule),
    typeof(WeixinModule))]
public class AuthenticationModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // JWT
        services.AddJwt<JwtHandler>(enableGlobalAuthorize: true, jwtBearerConfigure: options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var httpContext = context.HttpContext;

                    if (httpContext.Request.Query.ContainsKey("token"))
                    {
                        var token = httpContext.Request.Query["token"].ToString();

                        switch (token.StartsWith("Bearer") || token.StartsWith("bearer"))
                        {
                            case true:
                                token = token.Replace("Bearer", string.Empty).Replace("bearer", string.Empty);
                                break;
                        }

                        token = token.TrimStart();
                        switch (token.StartsWith("%20"))
                        {
                            case true:
                                token = token.Replace("%20", string.Empty);
                                break;
                        }

                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = context => Task.CompletedTask,
            };
        });

        // CORS
        services.AddCorsAccessor();

        // 远程请求
        services.AddRemoteRequest();

        // Swagger 文档缓存
        services.AddCachingSwaggerProvider();
    }

    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        // 状态码拦截
        app.UseUnifyResultStatusCodes();

        // 静态文件
        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = FS.GetFileExtensionContentTypeProvider()
        });

        // TraceId
        app.UseMiddleware<Infrastructure.TraceIdMiddleware>();

        app.UseRouting();
        app.UseCorsAccessor();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        // 任务调度看板
        app.UseScheduleUI();

        // Knife4UI
        app.UseKnife4UI(options =>
        {
            options.RoutePrefix = "newapi";
            foreach (var groupInfo in SpecificationDocumentBuilder.GetOpenApiGroups())
            {
                options.SwaggerEndpoint("/" + groupInfo.RouteTemplate, groupInfo.Title);
            }
        });

        app.UseInject(string.Empty);

        // 端点
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        });

        // Swagger 预热
        app.ApplicationServices.WarmupSwagger();
    }
}
