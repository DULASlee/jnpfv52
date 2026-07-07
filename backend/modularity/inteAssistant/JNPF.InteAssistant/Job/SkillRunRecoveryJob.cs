using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace JNPF.InteAssistant.Job;

/// <summary>
/// P6-W01 Worker 恢复 — 定时扫描超时的 ai_skill_runs，标记 failed（防进程崩溃后 run 卡死）。
///
/// 复用 StaleMonitorService 骨架：Quartz IJob + DisallowConcurrentExecution + DistributedLock。
/// 超时阈值从配置读：AI:SkillRun:TimeoutMinutes（默认 15 分钟）。
/// </summary>
[DisallowConcurrentExecution]
public class SkillRunRecoveryJob : IJob, ITransient
{
    private const string LockKey = "skill-run-recovery";

    private readonly ISqlSugarClient _db;
    private readonly IConfiguration _configuration;
    private readonly ISkillRunGuard _runGuard;
    private readonly ILogger<SkillRunRecoveryJob> _logger;

    public SkillRunRecoveryJob(
        ISqlSugarClient db,
        IConfiguration configuration,
        ISkillRunGuard runGuard,
        ILogger<SkillRunRecoveryJob> logger)
    {
        _db = db;
        _configuration = configuration;
        _runGuard = runGuard;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var timeoutMinutes = _configuration.GetValue("AI:SkillRun:TimeoutMinutes", 15);
        var cutoff = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

        if (!DistributedLock.TryAcquire(LockKey, TimeSpan.FromMinutes(5)))
            return;

        try
        {
            // 查询超时的 running skill runs
            var staleRuns = await _db.Queryable<AiSkillRunEntity>()
                .Where(x => x.Status == "running" && x.StartedAt < cutoff)
                .Take(50)
                .ToListAsync(context.CancellationToken);

            if (staleRuns.Count == 0)
                return;

            _logger.LogWarning("检测到 {Count} 个超时 skill runs（>{Min}min），标记 failed", staleRuns.Count, timeoutMinutes);

            foreach (var run in staleRuns)
            {
                // 标记 failed
                await _db.Updateable<AiSkillRunEntity>()
                    .SetColumns(x => x.Status == "failed")
                    .SetColumns(x => x.CompletedAt == DateTime.UtcNow)
                    .SetColumns(x => x.ErrorMessage == $"Worker 超时恢复：started at {run.StartedAt:O}，超过 {timeoutMinutes}min 未完成")
                    .Where(x => x.Id == run.Id)
                    .ExecuteCommandAsync(context.CancellationToken);

                // 释放进程内锁（防内存锁泄漏）
                if (long.TryParse(run.PipelineId, out var pid))
                {
                    try { _runGuard.Release(run.TenantId, pid, run.SkillId); }
                    catch { /* 进程内锁可能已释放，忽略 */ }
                }

                _logger.LogInformation(
                    "Skill run 恢复：skillId={SkillId} runId={RunId} pipelineId={PipelineId} 标记 failed",
                    run.SkillId, run.Id, run.PipelineId);
            }
        }
        finally
        {
            DistributedLock.Release(LockKey);
        }
    }
}
