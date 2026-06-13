using JNPF.Common.Const;
using JNPF.Common.Manager;
using JNPF.Schedule;

namespace JNPF.InteAssistant.Engine;

/// <summary>
/// 程序启动时移除集成助手部分逻辑缓存.
/// </summary>
[JobDetail("job_builtIn_ProgramStartup", Description = "集成助手-程序启动时", GroupName = "Integrate", Concurrent = false)]
[PeriodSeconds(1, TriggerId = "trigger_builtIn_InteAssistant_ProgramStartup", Description = "集成助手-程序启动时", MaxNumberOfRuns = 1, RunOnStart = true)]
public class InteAssistantProgramStartupJob : IJob
{
    private readonly ICacheManager _cacheManager;

    public InteAssistantProgramStartupJob(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var keys = _cacheManager.GetAllCacheKeys().FindAll(q => q.Contains(CommonConst.INTEASSISTANTRETRY));

        if (keys.Any())
        {
            foreach (var key in keys)
            {
                await _cacheManager.DelAsync(key);
            }
        }
    }
}
