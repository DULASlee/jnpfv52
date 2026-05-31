using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace JNPF.API.Entry.Infrastructure;

/// <summary>
/// Serilog configuration bootstrap.
/// </summary>
public static class SerilogBootstrap
{
    public static void Configure(IConfiguration cfg)
    {
        var logDir = cfg["Logging:File:LogDir"] ?? "logs";
        var fileFormatter = new JsonFormatter(renderMessage: true);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("SqlSugar", LogEventLevel.Warning)
            .Enrich.FromLogContext()

            // Error logs
            .WriteTo.File(
                formatter: fileFormatter,
                path: Path.Combine(logDir, "error-.json"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 50 * 1024 * 1024)

            // Warning logs (includes slow SQL)
            .WriteTo.File(
                formatter: fileFormatter,
                path: Path.Combine(logDir, "warning-.json"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 50 * 1024 * 1024)

            // Console
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")

            .CreateLogger();
    }
}
