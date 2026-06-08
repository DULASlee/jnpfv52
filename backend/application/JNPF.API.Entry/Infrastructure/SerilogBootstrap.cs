using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace JNPF.API.Entry.Infrastructure;

/// <summary>
/// Serilog configuration bootstrap.
/// </summary>
public static class SerilogBootstrap
{
    /// <summary>
    /// 全局日志级别开关，可由 LogDiskGuardService 动态调整.
    /// </summary>
    public static LoggingLevelSwitch LevelSwitch { get; } = new(LogEventLevel.Information);

    public static void Configure(IConfiguration cfg)
    {
        // SelfLog: sink 写入失败时输出到 stderr（Docker/K8s 环境可通过 docker logs 捕获）
        SelfLog.Enable(Console.Error);

        var logDir = cfg["Logging:File:LogDir"] ?? "logs";
        var fileFormatter = new JsonFormatter(renderMessage: true);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LevelSwitch)
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
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        // Seq Sink — 条件启用（默认关闭，不影响现有日志输出）
        var seqEnabled = cfg.GetValue<bool>("Logging:Seq:Enabled");
        var seqUrl = cfg["Logging:Seq:ServerUrl"];
        if (seqEnabled && !string.IsNullOrEmpty(seqUrl))
        {
            loggerConfig = loggerConfig.WriteTo.Seq(seqUrl);
        }

        Log.Logger = loggerConfig.CreateLogger();
    }
}
