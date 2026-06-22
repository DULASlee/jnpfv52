// 文件：Infrastructure/Background/BackgroundTaskShutdownService.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Background
// 职责：进程退出时优雅关闭所有在途后台任务

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Infrastructure.Background;

public sealed class BackgroundTaskShutdownService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IBackgroundTaskRunner _runner;
    private readonly ILogger<BackgroundTaskShutdownService> _logger;

    public BackgroundTaskShutdownService(
        IHostApplicationLifetime lifetime,
        IBackgroundTaskRunner runner,
        ILogger<BackgroundTaskShutdownService> logger)
    {
        _lifetime = lifetime;
        _runner = runner;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _lifetime.ApplicationStopping.Register(() =>
        {
            var active = _runner.GetAllActive();
            if (active.Count == 0) return;

            _logger.LogInformation("进程退出，取消 {Count} 个在途任务", active.Count);

            foreach (var kvp in active)
            {
                try { _runner.CancelTask(kvp.Key); } catch { /* 已释放 */ }
            }

            // 等待最多 30 秒让任务自行清理
            Task.Delay(TimeSpan.FromSeconds(30)).Wait();
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
