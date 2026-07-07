using System;
using System.Linq;
using System.Threading.Tasks;
using JNPF.InteAssistant.Studio;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace JNPF.InteAssistant.Job;

/// <summary>
/// P7-E02 Judge 月度校准 Job — 每月 1 日 02:00 执行。
///
/// 2026 实践：Judge 必须定期校准（模型漂移）。
/// 遍历所有租户，对每个租户的 Judge 计算 Cohen's kappa：
///   - kappa < 0.6 → 告警（Judge 不可信，L4 应降级为 advisory）
///   - 写入最近一条 EvalRun.F_JudgeKappa 作为基线
///
/// cron: "0 0 2 1 * ?"（每月 1 日 02:00）
/// </summary>
[DisallowConcurrentExecution]
public class EvalCalibrationJob : IJob
{
    private const string LockKey = "eval-calibration";

    private readonly ISqlSugarClient _db;
    private readonly IJudgeCalibrationService _calibration;
    private readonly ILogger<EvalCalibrationJob> _logger;

    public EvalCalibrationJob(
        ISqlSugarClient db,
        IJudgeCalibrationService calibration,
        ILogger<EvalCalibrationJob> logger)
    {
        _db = db;
        _calibration = calibration;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (!DistributedLock.TryAcquire(LockKey, TimeSpan.FromMinutes(10)))
        {
            _logger.LogInformation("EvalCalibrationJob 跳过（已有实例在运行）");
            return;
        }

        try
        {
            // 遍历所有有 eval run 的租户（去重）
            var tenants = await _db.Queryable<EvalRunEntity>()
                .Where(x => x.F_TenantId != null && x.F_TenantId != "")
                .Select(x => x.F_TenantId)
                .Distinct()
                .ToListAsync(context.CancellationToken);

            if (tenants.Count == 0)
            {
                _logger.LogInformation("EvalCalibrationJob：无租户有 eval 记录，跳过");
                return;
            }

            _logger.LogInformation("EvalCalibrationJob 开始：{Count} 个租户", tenants.Count);

            foreach (var tenantId in tenants)
            {
                try
                {
                    var report = await _calibration.CalibrateAsync(tenantId, ct: context.CancellationToken);

                    // 写入该租户最近一条 EvalRun.F_JudgeKappa 作为基线
                    if (report.Kappa.HasValue)
                    {
                        var latestRun = await _db.Queryable<EvalRunEntity>()
                            .Where(x => x.F_TenantId == tenantId)
                            .OrderByDescending(x => x.F_RunAt)
                            .FirstAsync(context.CancellationToken);

                        if (latestRun != null)
                        {
                            await _db.Updateable<EvalRunEntity>()
                                .SetColumns(x => new EvalRunEntity { F_JudgeKappa = (decimal)report.Kappa.Value })
                                .Where(x => x.F_Id == latestRun.F_Id)
                                .ExecuteCommandAsync(context.CancellationToken);
                        }
                    }

                    if (report.Status == "untrusted")
                    {
                        _logger.LogWarning(
                            "Judge 校准告警 tenant={TenantId} kappa={Kappa} < 0.6，L4 应降级为 advisory",
                            tenantId, report.Kappa);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "租户 {TenantId} Judge 校准失败（非阻断）", tenantId);
                }
            }

            _logger.LogInformation("EvalCalibrationJob 完成");
        }
        finally
        {
            DistributedLock.Release(LockKey);
        }
    }
}
