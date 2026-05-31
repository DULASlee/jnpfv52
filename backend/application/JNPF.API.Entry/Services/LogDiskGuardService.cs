using Microsoft.Extensions.Logging;

namespace JNPF.API.Entry.Services;

/// <summary>
/// 日志磁盘空间保护服务.
/// </summary>
public class LogDiskGuardService : BackgroundService
{
    private readonly ILogger<LogDiskGuardService> _logger;
    private readonly IConfiguration _cfg;

    // 阈值：剩余5GB时报警，剩余1GB时停止写入
    private const long WarningThresholdBytes = 5L * 1024 * 1024 * 1024;   // 5GB
    private const long CriticalThresholdBytes = 1L * 1024 * 1024 * 1024;  // 1GB

    // 静态标志，供Serilog Sink判断是否继续写入
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
                    IsDiskCritical = true;
                    _logger.LogCritical(
                        "LOG_DISK_CRITICAL | 剩余空间 {FreeMB}MB | 日志写入已暂停 | 请立即清理磁盘",
                        freeBytes / 1024 / 1024);
                }
                else if (freeBytes < WarningThresholdBytes)
                {
                    IsDiskCritical = false;
                    _logger.LogWarning(
                        "LOG_DISK_WARNING | 剩余空间 {FreeMB}MB | 请及时清理日志文件",
                        freeBytes / 1024 / 1024);
                }
                else
                {
                    IsDiskCritical = false;
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
