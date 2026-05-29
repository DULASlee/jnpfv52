using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.Schedule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.TaskScheduler.Listener;

/// <summary>
/// 本地任务Demo.
/// </summary>
[JobDetail("job_builtIn_test", Description = "本地任务Demo", GroupName = "BuiltIn", Concurrent = true)]
public class SpareTimeDemo : IJob, IDisposable
{
    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceScope _serviceScope;

    private readonly ILogger<SpareTimeDemo> _logger;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public SpareTimeDemo(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SpareTimeDemo> logger,
        ITenantManager tenantManager)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _logger = logger;
        _tenantManager = tenantManager;
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var sqlSugarClient = _serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        // 多租户场景 需要获取到 租户ID
        var tenantId = context.TriggerId.Match("(.+(?=_trigger_schedule_))");
        var taskId = context.TriggerId.Match("((?<=_trigger_schedule_).+)");

        _logger.LogInformation($"租户ID:{tenantId}");

        if (KeyVariable.MultiTenancy)
        {
            await _tenantManager.ChangTenant(sqlSugarClient, tenantId);
        }

        // 对应的数据库操作=========================

        // job 内必须 CopyNew 下 SugarClien
        sqlSugarClient = sqlSugarClient.CopyNew();

        await Task.Delay(2000, stoppingToken); // 这里模拟耗时操作，比如耗时2秒

        // 必须有执行结构返回
        context.Result = string.Format("执行成功");
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}