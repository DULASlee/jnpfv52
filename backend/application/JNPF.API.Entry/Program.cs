Serve.Run(RunOptions.Default
    .AddWebComponent<WebComponent>().WithArgs(args));

public class WebComponent : IWebComponent
{
    public void Load(WebApplicationBuilder builder, ComponentContext componentContext)
    {
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

        builder.Logging.AddConsoleFormatter(options =>
        {
            options.DateFormat = "yyyy-MM-dd HH:mm:ss(zzz) dddd";
            options.WithTraceId = true; // 显示线程Id
            options.WithStackFrame = true; // 显示程序集
        });
    }
}