using JNPF.Modules;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// OpenTelemetry 可观测性模块 — 全链路追踪 + Metrics 导出.
/// 与现有 MiniProfiler（开发 APM）并行运行，互不冲突。
/// </summary>
[JNPF.Modules.DependsOn(typeof(DatabaseModule))]
public class ObservabilityModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var otelCfg = configuration.GetSection("Observability");
        var otlpEndpoint = otelCfg["OtlpEndpoint"] ?? "http://localhost:4317";
        var serviceName = otelCfg["ServiceName"] ?? "jnpf-api";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: "5.2.0")
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health")
                        && !ctx.Request.Path.StartsWithSegments("/health/live")
                        && !ctx.Request.Path.StartsWithSegments("/health/ready");
                    options.RecordException = true;
                })
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddSource("JNPF.EventBus")
                .AddSource("JNPF.Studio")  // P6-O01 InteAssistant Studio 埋点（skill.run/llm.call/ir.append）
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }))
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }));

        // OpenTelemetry Logging 集成 — Activity.Current.TraceId 自动注入 Serilog
        services.Configure<OpenTelemetryLoggerOptions>(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });
    }
}
