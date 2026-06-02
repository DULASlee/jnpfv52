using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.API.Entry.Services;

/// <summary>
/// 日志系统健康检查端点.
/// </summary>
// AllowAnonymous: 健康检查端点供运维监控系统（K8s probe / Prometheus / 负载均衡器）访问，无需认证
[AllowAnonymous]
[ApiDescriptionSettings(Tag = "System", Name = "LogHealthCheck", Order = 221)]
[Route("api/system/[controller]")]
public class LogHealthCheckService : IDynamicApiController, ITransient
{
    private readonly IConfiguration _cfg;

    public LogHealthCheckService(IConfiguration cfg)
    {
        _cfg = cfg;
    }

    /// <summary>
    /// 日志系统健康检查.
    /// </summary>
    [HttpGet("")]
    public IActionResult Get()
    {
        var logDir = _cfg["Logging:File:LogDir"] ?? "logs";
        var today = DateTime.Now.ToString("yyyyMMdd");

        var errorFile = Path.Combine(logDir, $"error-{today}.json");
        var warningFile = Path.Combine(logDir, $"warning-{today}.json");

        var isDiskCritical = LogDiskGuardService.IsDiskCritical;

        var result = new
        {
            status = isDiskCritical ? "degraded" : "healthy",
            timestamp = DateTime.UtcNow,
            logDirectory = logDir,
            diskGuard = new
            {
                isDiskCritical,
            },
            errorLog = new
            {
                exists = File.Exists(errorFile),
                lastModified = File.Exists(errorFile)
                    ? File.GetLastWriteTime(errorFile)
                    : (DateTime?)null,
                sizeBytes = File.Exists(errorFile)
                    ? new FileInfo(errorFile).Length
                    : 0,
            },
            warningLog = new
            {
                exists = File.Exists(warningFile),
                lastModified = File.Exists(warningFile)
                    ? File.GetLastWriteTime(warningFile)
                    : (DateTime?)null,
                sizeBytes = File.Exists(warningFile)
                    ? new FileInfo(warningFile).Length
                    : 0,
            },
        };

        if (isDiskCritical)
            return new ObjectResult(result) { StatusCode = 503 };

        return new OkObjectResult(result);
    }
}
