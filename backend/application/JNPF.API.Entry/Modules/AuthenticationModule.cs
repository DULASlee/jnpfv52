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
                    var token = (string?)null;

                    // 1. Query string: ?token=xxx
                    if (httpContext.Request.Query.ContainsKey("token"))
                    {
                        token = httpContext.Request.Query["token"].ToString();
                    }
                    // 2. URL path segment: /api/message/websocket/{token}
                    else
                    {
                        var path = httpContext.Request.Path.Value ?? "";
                        if (path.StartsWith("/api/message/websocket/", StringComparison.OrdinalIgnoreCase))
                        {
                            token = path["/api/message/websocket/".Length..];
                        }
                    }

                    if (!string.IsNullOrEmpty(token))
                    {
                        // Strip Bearer prefix if present
                        if (token.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
                            token = token["Bearer".Length..];
                        token = token.TrimStart();
                        // Strip URL-encoded leading space (%20)
                        if (token.StartsWith("%20", StringComparison.OrdinalIgnoreCase))
                            token = token["%20".Length..];
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

        // 租户上下文（ADR-003）：MUST 在 Authorization 之后执行，
        // 此时 HttpContext.User 已由 JWT 中间件填充，可从 claims 提取 TenantId
        app.UseMiddleware<JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext.TenantMiddleware>();

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
