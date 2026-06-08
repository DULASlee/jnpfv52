using JNPF.Modules;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using SqlSugar;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// 健康检查模块 — /health, /health/live, /health/ready.
/// 从 DatabaseModule 和 AuthenticationModule 中提取，统一管理。
/// </summary>
[JNPF.Modules.DependsOn(typeof(DatabaseModule))]
public class HealthCheckModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Health Check 注册（从 DatabaseModule 移入）
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: JNPFTenantExtensions.ToConnectionString(
                    App.GetOptions<ConnectionStringsOptions>().DefaultConnectionConfig),
                name: "sqlserver",
                tags: new[] { "db" });
    }

    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            // 全量健康报告（所有检查项）
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = WriteJsonResponse
            });

            // 存活探针（无依赖检查，K8s livenessProbe 用）
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            // 就绪探针（仅检查 "db" 标签的依赖，K8s readinessProbe 用）
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("db"),
                ResponseWriter = WriteJsonResponse
            });
        });
    }

    private static async Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration
            }),
            totalDuration = report.TotalDuration
        };
        await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
    }
}
