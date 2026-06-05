using JNPF.API.Entry.Infrastructure;
using Serilog;

Serve.Run(RunOptions.Default
    .AddWebComponent<WebComponent>().WithArgs(args));

public class WebComponent : IWebComponent
{
    public void Load(WebApplicationBuilder builder, ComponentContext componentContext)
    {
        // Configure Serilog
        SerilogBootstrap.Configure(builder.Configuration);
        builder.Host.UseSerilog();

        // DbJobPersistence 等 async void 方法的 catch 块使用 Trace.WriteLine 输出诊断信息
        // 必须注册 TraceListener 才能在生产环境捕获这些输出
        var traceLogPath = Path.Combine(
            builder.Configuration["Logging:File:LogDir"] ?? "logs",
            "trace-diagnostics.log");
        System.Diagnostics.Trace.Listeners.Add(
            new System.Diagnostics.TextWriterTraceListener(traceLogPath));
        System.Diagnostics.Trace.AutoFlush = true;

        // 日志过滤
        builder.Logging.AddFilter((provider, category, logLevel) =>
        {
            return !new[] { "Microsoft.Hosting", "Microsoft.AspNetCore" }.Any(u => category.StartsWith(u))
                && logLevel >= LogLevel.Information;
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            // 长度最好不要设置 null
            options.Limits.MaxRequestBodySize = 52428800;
        });
    }
}
