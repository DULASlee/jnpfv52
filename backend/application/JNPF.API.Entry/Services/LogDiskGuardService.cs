using JNPF.API.Entry.Infrastructure;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace JNPF.API.Entry.Services;

/// <summary>
/// 日志磁盘空间保护服务.
/// </summary>
public class LogDiskGuardService : BackgroundService
{
    private readonly ILogger<LogDiskGuardService> _logger;
    private readonly IConfiguration _cfg;

    // 阈值：剩余5GB时报警，剩余1GB时提升日志级别至 Error only
    private const long WarningThresholdBytes = 5L * 1024 * 1024 * 1024;   // 5GB
    private const long CriticalThresholdBytes = 1L * 1024 * 1024 * 1024;  // 1GB

    // 静态标志，供健康检查端点使用
    public static bool IsDiskCritical { get; private set; }

    public LogDiskGuardService(ILogger<LogDiskGuardService> logger, IConfiguration cfg)
    {
        _logger = logger;
        _cfg = cfg;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var logDir = _cfg["Logging:File:LogDir"] ?? "logs";
                var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(logDir)));
                var freeBytes = driveInfo.AvailableFreeSpace;

                if (freeBytes < CriticalThresholdBytes)
                {
                    if (!IsDiskCritical)
                    {
                        IsDiskCritical = true;
                        // 动态提升 Serilog 日志级别：只记录 Error 及以上，减少日志写入量
                        SerilogBootstrap.LevelSwitch.MinimumLevel = LogEventLevel.Error;
                        _logger.LogCritical(
                            "LOG_DISK_CRITICAL | 剩余空间 {FreeMB}MB | 日志级别已提升至 Error only | 请立即清理磁盘",
                            freeBytes / 1024 / 1024);
                    }
                }
                else if (freeBytes < WarningThresholdBytes)
                {
                    if (IsDiskCritical)
                    {
                        IsDiskCritical = false;
                        // 恢复正常日志级别
                        SerilogBootstrap.LevelSwitch.MinimumLevel = LogEventLevel.Information;
                        _logger.LogInformation("LOG_DISK_RECOVERED | 磁盘空间恢复正常 | 日志级别已恢复");
                    }
                    _logger.LogWarning(
                        "LOG_DISK_WARNING | 剩余空间 {FreeMB}MB | 请及时清理日志文件",
                        freeBytes / 1024 / 1024);
                }
                else
                {
                    if (IsDiskCritical)
                    {
                        IsDiskCritical = false;
                        SerilogBootstrap.LevelSwitch.MinimumLevel = LogEventLevel.Information;
                        _logger.LogInformation("LOG_DISK_RECOVERED | 磁盘空间恢复正常 | 日志级别已恢复");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "磁盘空间检测异常");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
