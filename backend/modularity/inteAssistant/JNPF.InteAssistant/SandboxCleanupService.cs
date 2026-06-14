using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// 沙箱超时自动清理服务 (Phase 6 Day 4).
/// 每 30 秒检查超时沙箱并自动销毁.
/// </summary>
public sealed class SandboxCleanupService : BackgroundService
{
    private readonly ISandboxManager _sandboxManager;
    private readonly ILogger<SandboxCleanupService> _logger;

    public SandboxCleanupService(
        ISandboxManager sandboxManager,
        ILogger<SandboxCleanupService> logger)
    {
        _sandboxManager = sandboxManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("沙箱清理服务已启动，检查间隔 30 秒");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSandboxes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "沙箱清理时发生未处理异常");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task CleanupExpiredSandboxes()
    {
        var sandboxes = await _sandboxManager.GetAllAsync();
        var now = DateTime.UtcNow;

        foreach (var sandbox in sandboxes)
        {
            if (sandbox.Status is "destroying" or "destroyed")
                continue;

            var elapsed = now - sandbox.CreatedAt;
            if (elapsed.TotalSeconds > sandbox.Config.TimeoutSeconds)
            {
                _logger.LogWarning(
                    "沙箱 {SandboxId} 超时 (已运行 {Elapsed:F0}s，限制 {Limit}s)，自动销毁",
                    sandbox.Id, elapsed.TotalSeconds, sandbox.Config.TimeoutSeconds);

                await _sandboxManager.DestroyAsync(sandbox.Id);
            }
        }
    }
}
