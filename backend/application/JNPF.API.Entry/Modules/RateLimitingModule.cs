using JNPF.Modules;
using Microsoft.AspNetCore.RateLimiting;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// API 限流模块 — 全局固定窗口 + 登录/导出专用策略.
/// 从 JsonSettingsModule 中提取，独立管理限流配置。
/// </summary>
public class RateLimitingModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // 全局默认限流：200 次/秒，队列 20
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = 200;
                opt.Window = TimeSpan.FromSeconds(1);
                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 20;
            });

            // 登录限流：20 次/分钟，队列 5
            options.AddFixedWindowLimiter("login", opt =>
            {
                opt.PermitLimit = 20;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            // 导出限流：10 次/分钟，队列 2
            options.AddFixedWindowLimiter("export", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });

            options.OnRejected = (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                var result = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    code = 429,
                    msg = "请求过于频繁，请稍后再试"
                });
                context.HttpContext.Response.WriteAsync(result, cancellationToken);
                return ValueTask.CompletedTask;
            };
        });
    }
}
