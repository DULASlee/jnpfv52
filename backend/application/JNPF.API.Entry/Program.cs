using JNPF.API.Entry.Infrastructure;
using Serilog;

// Bootstrap logger: 捕获 Serilog 正式配置之前的启动阶段异常
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/startup.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Serve.Run(RunOptions.Default
        .AddWebComponent<WebComponent>().WithArgs(args));
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public class WebComponent : IWebComponent
{
    public void Load(WebApplicationBuilder builder, ComponentContext componentContext)
    {
        // Configure Serilog
        SerilogBootstrap.Configure(builder.Configuration);
        builder.Host.UseSerilog();

        // 捕获未观察的 Task 异常，防止静默丢失
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Log.Warning(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        };

        builder.WebHost.ConfigureKestrel(options =>
        {
            // 长度最好不要设置 null
            options.Limits.MaxRequestBodySize = 52428800;
        });
    }
}
