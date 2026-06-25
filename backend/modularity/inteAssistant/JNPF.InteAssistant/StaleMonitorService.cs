using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Enum;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace JNPF.InteAssistant;

/// <summary>
/// 定时扫描超时流水线（Quartz.NET IJob 实现）
/// 每小时扫描一次，将超时未操作的流水线标记为 stale
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-20
/// </summary>
[DisallowConcurrentExecution]
public class StaleMonitorService : IJob, ITransient
{
    private readonly SqlSugar.ISqlSugarClient _db;
    private readonly IHubContext<PipelineHub> _hub;
    private readonly ILogger<StaleMonitorService> _logger;
    private readonly IConfiguration _configuration;

    public StaleMonitorService(
        SqlSugar.ISqlSugarClient db,
        IHubContext<PipelineHub> hub,
        IConfiguration configuration,
        ILogger<StaleMonitorService> logger)
    {
        _db = db;
        _hub = hub;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Quartz.NET 调度入口
    /// 每小时整点执行
    /// </summary>
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await ScanAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StaleMonitorService 扫描异常");
            throw new JobExecutionException(ex) { RefireImmediately = false };
        }
    }

    /// <summary>
    /// 扫描所有阶段，标记超时流水线
    /// </summary>
    public async Task ScanAsync(CancellationToken ct = default)
    {
        // I-13-C1: 分布式锁（防止多实例重复扫描）
        var lockKey = $"stale-monitor:{DateTime.Now:yyyyMMddHH}";
        if (!DistributedLock.TryAcquire(lockKey, TimeSpan.FromMinutes(5)))
        {
            _logger.LogInformation("StaleMonitor 被另一实例锁定(lockKey={LockKey})，跳过本次扫描", lockKey);
            return;
        }

        try
        {
            var now = DateTime.Now;
            var totalMarked = 0;
            var allStaleIds = new List<string>();

            var timeouts = GetTimeoutThresholds();

            foreach (var (stage, hours) in timeouts)
            {
                var threshold = now.AddHours(-hours);

                var stalePipelines = await _db.Queryable<AiPipelineEntity>()
                    .Where(x => x.StageStatus == PipelineStatus.Review
                             || x.StageStatus == PipelineStatus.Running)
                    .Where(x => x.CurrentStage == stage)
                    .Where(x => x.LastModifyTime < threshold)
                    .ToListAsync(ct);

                if (stalePipelines.Count == 0) continue;

                var ids = stalePipelines.Select(p => p.Id).ToList();
                allStaleIds.AddRange(ids);

                // I-13-C2: 批量更新（替代逐条更新，降低DB往返）
                await _db.Updateable<AiPipelineEntity>()
                    .SetColumns(x => new AiPipelineEntity
                    {
                        StageStatus = PipelineStatus.Stale,
                        StaleFromStage = stage,
                        StaleAt = now
                    })
                    .Where(x => ids.Contains(x.Id))
                    .ExecuteCommandAsync(ct);

                // 批量 SignalR 推送 + 消息记录
                foreach (var p in stalePipelines)
                {
                    var tenantId = p.TenantId;
                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        await _hub.Clients.Group($"tenant_{tenantId}")
                            .SendAsync("PipelineEvent", new PipelineEventPayload
                            {
                                EventType = "pipeline_stale",
                                PipelineId = p.Id,
                                Stage = p.CurrentStage,
                                UserId = p.CreatorUserId,
                                Reason = $"流水线在{stage}阶段已超过{hours}小时未操作"
                            }, ct);
                    }
                }

                totalMarked += stalePipelines.Count;
            }

            if (totalMarked > 0)
            {
                _logger.LogInformation(
                    "StaleMonitor 扫描完成: 标记 {Count} 条超时流水线", totalMarked);
            }

            // I-13-C3: 30天无响应自动abandoned + 资源回收
            await AutoAbandonLongStalePipelinesAsync(now, ct);

            // 知识图谱备份清理（玛维思 #7 · 2026-06-20）
            await CleanupKnowledgeBackupsAsync(now, ct);
        }
        finally
        {
            DistributedLock.Release(lockKey);
        }
    }

    /// <summary>
    /// 30天无响应自动abandoned + 资源回收
    /// </summary>
    private async Task AutoAbandonLongStalePipelinesAsync(DateTime now, CancellationToken ct)
    {
        var cutoffDate = now.AddDays(-30);

        // 使用 ISNULL COALESCE 选择最早的非空时间
        var autoAbandonList = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.StageStatus == PipelineStatus.Stale)
            .Where(x => (x.StaleAt ?? x.StaleSince ?? x.LastModifyTime) < cutoffDate)
            .ToListAsync(ct);

        if (autoAbandonList.Count == 0) return;

        foreach (var p in autoAbandonList)
        {
            p.StageStatus = PipelineStatus.Abandoned;
            p.AbandonedBy = "system_stale_monitor";
            p.AbandonReason = "超过30天未响应，自动放弃";
            p.AbandonedAt = now;
        }

        await _db.Updateable(autoAbandonList).ExecuteCommandAsync(ct);

        _logger.LogInformation("StaleMonitor 自动放弃: {Count} 条超过30天的stale流水线", autoAbandonList.Count);
    }

    /// <summary>
    /// 清理知识图谱备份表中超过 30 天的记录（玛维思 #7 · DB-20）。
    /// 分批执行，每批最多 1000 条，防止长事务锁表。
    /// </summary>
    private async Task CleanupKnowledgeBackupsAsync(DateTime now, CancellationToken ct)
    {
        const int batchSize = 1000;
        const int retentionDays = 30;
        var cutoff = now.AddDays(-retentionDays);
        var totalDeleted = 0;

        try
        {
            // 清理 BASE_KNOWLEDGE_NODE_BACKUP
            totalDeleted += await BatchDeleteAsync(
                "BASE_KNOWLEDGE_NODE_BACKUP", "F_BACKUP_AT", cutoff, batchSize, ct);

            // 清理 BASE_KNOWLEDGE_EDGE_BACKUP
            totalDeleted += await BatchDeleteAsync(
                "BASE_KNOWLEDGE_EDGE_BACKUP", "F_BACKUP_AT", cutoff, batchSize, ct);

            if (totalDeleted > 0)
            {
                _logger.LogInformation(
                    "KnowledgeBackup 清理完成: 删除 {Count} 条超过 {Days} 天的备份记录",
                    totalDeleted, retentionDays);
            }
        }
        catch (Exception ex)
        {
            // 备份清理失败不阻塞主流程
            _logger.LogWarning(ex, "KnowledgeBackup 清理异常（不阻塞 StaleMonitor 主流程）");
        }
    }

    /// <summary>
    /// 分批删除过期备份记录，返回删除总数。
    /// </summary>
    private async Task<int> BatchDeleteAsync(
        string tableName, string dateColumn, DateTime cutoff, int batchSize, CancellationToken ct)
    {
        var totalDeleted = 0;

        while (!ct.IsCancellationRequested)
        {
            var sql = $"DELETE TOP({batchSize}) FROM {tableName} WHERE {dateColumn} < @cutoff";
            var affected = await _db.Ado.ExecuteCommandAsync(sql, new { cutoff });

            if (affected <= 0) break;

            totalDeleted += affected;
            _logger.LogDebug("KnowledgeBackup 分批清理: {Table} 本批删除 {Count} 条", tableName, affected);
        }

        return totalDeleted;
    }

    /// <summary>
    /// 从 AI.json 读取超时阈值配置
    /// </summary>
    private Dictionary<string, int> GetTimeoutThresholds()
    {
        var section = _configuration.GetSection("AI:Pipeline:StaleTimeouts");
        return new Dictionary<string, int>
        {
            ["requirement"] = section.GetValue("requirement", 168),   // 7 天
            ["architecture"] = section.GetValue("architecture", 168),  // 7 天
            ["design"] = section.GetValue("design", 72),               // 3 天
            ["development"] = section.GetValue("development", 168),    // 7 天
            ["delivery"] = section.GetValue("delivery", 336)           // 14 天
        };
    }
}
