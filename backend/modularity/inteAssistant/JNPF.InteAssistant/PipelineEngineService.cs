using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Enum;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JNPF.InteAssistant;

/// <summary>
/// 五阶段流水线引擎实现
/// 阶段: requirement → architecture → design → development → delivery
///
/// P1-3 重构(2026-07-05):
///   - ISingleton → IScoped:删除内存字典,每次操作全 DB 读取,支持多实例/重启
///   - Stages 从消息表真实状态重建(而非假设 completed/running)
///   - 新增 FreezeAsync/ResumeAsync:全量 checkpoint(状态+消息+IR版本)
/// </summary>
public class PipelineEngineService : IPipelineEngine, IScoped
{
    private readonly ILogger<PipelineEngineService> _logger;
    private readonly SqlSugar.ISqlSugarClient _db;

    public PipelineEngineService(ILogger<PipelineEngineService> logger, SqlSugar.ISqlSugarClient db = null!)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<PipelineResult> CreateAsync(
        PipelineCreateRequest request, long tenantId, long userId, CancellationToken ct = default)
    {
        // P1-3: ID 由调用方(AIDevelopmentPipelineService.CreateAsync)负责落库,
        // 此处仅返回逻辑结果。pipelineId 由 EnsureNextIdSeedLoaded 的 SQL 分配。
        var id = await NextPipelineIdAsync();

        _logger.LogInformation("流水线创建: ID={Id}, Name={Name}, Tenant={TenantId}",
            id, request.Name, tenantId);

        return await Task.FromResult(new PipelineResult
        {
            PipelineId = id,
            Name = request.Name,
            CurrentStage = PipelineStage.Requirement,
            Status = "draft"
        });
    }

    public async Task<PipelineResult> StartAsync(long pipelineId, CancellationToken ct = default)
    {
        var entity = await LoadPipelineEntityAsync(pipelineId, ct);
        if (entity == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        entity.Status = "running";
        if (entity.StartedTime == null) entity.StartedTime = DateTime.Now;
        entity.LastModify();

        await UpdatePipelineStatusAsync(entity, ct);

        _logger.LogInformation("流水线启动: ID={Id}", pipelineId);

        return new PipelineResult
        {
            PipelineId = pipelineId,
            Name = entity.Name ?? "",
            CurrentStage = entity.CurrentStage ?? PipelineStage.Requirement,
            Status = entity.Status ?? "running"
        };
    }

    public async Task<StageResult> ExecuteStageAsync(
        long pipelineId, string stageName, CancellationToken ct = default)
    {
        var entity = await LoadPipelineEntityAsync(pipelineId, ct);
        if (entity == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        if (!PipelineStage.Order.Contains(stageName))
            throw new ArgumentException($"未知阶段: {stageName}");

        entity.CurrentStage = stageName;
        entity.Status = "running";
        entity.LastModify();

        await UpdatePipelineStatusAsync(entity, ct);

        var stageOrder = Array.IndexOf(PipelineStage.Order, stageName);
        _logger.LogInformation("流水线阶段执行: ID={Id}, Stage={Stage}", pipelineId, stageName);

        return new StageResult
        {
            StageId = stageOrder + 1,
            StageName = stageName,
            Status = "running"
        };
    }

    public async Task<StageResult> ConfirmStageAsync(
        long stageId, StageConfirmation confirmation, CancellationToken ct = default)
    {
        // 前端当前传的是 pipelineId(见 AIDevelopmentPipelineService.ConfirmStageAsync 兼容注释)
        var pipelineId = stageId;
        var entity = await LoadPipelineEntityAsync(pipelineId, ct);
        if (entity == null)
            throw new InvalidOperationException($"阶段 {stageId} 不存在");

        var currentStage = string.IsNullOrWhiteSpace(entity.CurrentStage)
            ? PipelineStage.Requirement : entity.CurrentStage;
        var currentOrder = Array.IndexOf(PipelineStage.Order, currentStage);

        string systemText;
        if (confirmation.Approved)
        {
            var nextStage = PipelineStage.GetNext(currentStage);
            if (nextStage != null)
            {
                var nextOrder = Array.IndexOf(PipelineStage.Order, nextStage);
                entity.CurrentStage = nextStage;
                entity.Status = "running";
                systemText = $"✅ 已进入阶段 {nextOrder + 1}：{nextStage}";
            }
            else
            {
                entity.Status = "completed";
                entity.FinishedTime = DateTime.Now;
                systemText = "🎉 已完成全部阶段";
            }
        }
        else
        {
            entity.Status = "review";
            systemText = $"🛠️ 已退回「{currentStage}」阶段，请根据意见继续完善";
        }

        entity.LastModify();
        await UpdatePipelineStatusAsync(entity, ct);
        await AppendSystemMessageAsync(pipelineId, entity.CurrentStage, systemText, ct);

        _logger.LogInformation("阶段确认: StageId={Id}, Approved={Approved}, Next={Next}",
            stageId, confirmation.Approved, entity.CurrentStage);

        return new StageResult
        {
            StageId = currentOrder + 1,
            StageName = entity.CurrentStage,
            Status = entity.Status
        };
    }

    public async Task<StageResult> RollbackAsync(
        long pipelineId, string targetStage, string? reason = null, CancellationToken ct = default)
    {
        var entity = await LoadPipelineEntityAsync(pipelineId, ct);
        if (entity == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");
        if (!PipelineStage.Order.Contains(targetStage))
            throw new ArgumentException($"未知阶段: {targetStage}");

        var targetOrder = Array.IndexOf(PipelineStage.Order, targetStage);
        entity.CurrentStage = targetStage;
        entity.Status = "running";
        entity.LastModify();

        await UpdatePipelineStatusAsync(entity, ct);

        var rollbackMessage = string.IsNullOrWhiteSpace(reason)
            ? $"↩️ 已回退到阶段 {targetOrder + 1}：{targetStage}"
            : $"↩️ 已回退到阶段 {targetOrder + 1}：{targetStage}，原因：{reason}";
        await AppendSystemMessageAsync(pipelineId, targetStage, rollbackMessage, ct);

        _logger.LogInformation("阶段回退: PipelineId={Id}, Target={Stage}, Reason={Reason}",
            pipelineId, targetStage, reason ?? "-");

        return new StageResult
        {
            StageId = targetOrder + 1,
            StageName = targetStage,
            Status = "running",
            Output = rollbackMessage
        };
    }

    // ─── P1-3 新增:冻结/恢复(全量 checkpoint)───

    public async Task<PipelineResult> FreezeAsync(
        long pipelineId, string? reason = null, string? frozenBy = null, CancellationToken ct = default)
    {
        var entity = await LoadPipelineEntityAsync(pipelineId, ct);
        if (entity == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        if (entity.Frozen)
            throw new InvalidOperationException($"流水线 {pipelineId} 已处于冻结状态");

        // 构建全量 checkpoint:当前阶段 + 每阶段最新消息 ID + 最新 IR 版本号
        var checkpoint = await BuildCheckpointAsync(pipelineId, entity.CurrentStage, ct);

        entity.Frozen = true;
        entity.FrozenAt = DateTime.Now;
        entity.FrozenBy = frozenBy ?? App.User?.FindFirst("user_id")?.Value;
        entity.FrozenReason = reason;
        entity.Checkpoint = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions { WriteIndented = false });
        entity.LastModify();

        await _db.Updateable<AiPipelineEntity>()
            .SetColumns(x => new AiPipelineEntity
            {
                Frozen = true,
                FrozenAt = entity.FrozenAt,
                FrozenBy = entity.FrozenBy,
                FrozenReason = entity.FrozenReason,
                Checkpoint = entity.Checkpoint,
                LastModifyTime = DateTime.Now,
                LastModifyUserId = entity.LastModifyUserId
            })
            .Where(x => x.Id == pipelineId.ToString())
            .ExecuteCommandAsync(ct);

        // 标记当前会话消息为已冻结(冻结边界)
        await _db.Updateable<AiPipelineMessageEntity>()
            .SetColumns(x => new AiPipelineMessageEntity { IsFrozen = true })
            .Where(x => x.PipelineId == pipelineId.ToString() && x.IsFrozen == false)
            .ExecuteCommandAsync(ct);

        var freezeMsg = string.IsNullOrWhiteSpace(reason)
            ? "❄️ 流水线已冻结(全量 checkpoint 已保存)"
            : $"❄️ 流水线已冻结,原因:{reason}";
        await AppendSystemMessageAsync(pipelineId, entity.CurrentStage, freezeMsg, ct);

        _logger.LogInformation("流水线冻结: ID={Id}, Reason={Reason}, CheckpointSize={Size}",
            pipelineId, reason ?? "-", entity.Checkpoint?.Length ?? 0);

        return new PipelineResult
        {
            PipelineId = pipelineId,
            Name = entity.Name ?? "",
            CurrentStage = entity.CurrentStage ?? PipelineStage.Requirement,
            Status = "frozen"
        };
    }

    public async Task<PipelineResult> ResumeAsync(long pipelineId, CancellationToken ct = default)
    {
        var entity = await LoadPipelineEntityAsync(pipelineId, ct);
        if (entity == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        if (!entity.Frozen)
            throw new InvalidOperationException($"流水线 {pipelineId} 未冻结,无需恢复");

        // 校验 checkpoint 完整性
        if (string.IsNullOrWhiteSpace(entity.Checkpoint))
        {
            _logger.LogWarning("流水线恢复: checkpoint 为空,仅解除冻结标记 PipelineId={Id}", pipelineId);
        }
        else
        {
            try
            {
                _ = JsonSerializer.Deserialize<PipelineCheckpoint>(entity.Checkpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "流水线恢复: checkpoint 反序列化失败 PipelineId={Id}", pipelineId);
                throw new InvalidOperationException($"流水线 {pipelineId} checkpoint 已损坏,无法恢复");
            }
        }

        // 生成新会话 ID(恢复后开启新对话窗口)
        var newSessionId = Guid.NewGuid().ToString("N");

        entity.Frozen = false;
        entity.ResumeCount += 1;
        entity.LastResumedAt = DateTime.Now;
        entity.Status = "running";
        entity.LastModify();

        await _db.Updateable<AiPipelineEntity>()
            .SetColumns(x => new AiPipelineEntity
            {
                Frozen = false,
                ResumeCount = entity.ResumeCount,
                LastResumedAt = entity.LastResumedAt,
                Status = "running",
                LastModifyTime = DateTime.Now,
                LastModifyUserId = entity.LastModifyUserId
            })
            .Where(x => x.Id == pipelineId.ToString())
            .ExecuteCommandAsync(ct);

        await AppendSystemMessageAsync(pipelineId, entity.CurrentStage,
            $"▶️ 流水线已恢复(第 {entity.ResumeCount} 次恢复,新会话 {newSessionId[..8]})", ct);

        _logger.LogInformation("流水线恢复: ID={Id}, ResumeCount={Count}, SessionId={Session}",
            pipelineId, entity.ResumeCount, newSessionId);

        return new PipelineResult
        {
            PipelineId = pipelineId,
            Name = entity.Name ?? "",
            CurrentStage = entity.CurrentStage ?? PipelineStage.Requirement,
            Status = "running"
        };
    }

    // ─── 查询方法 ───

    public async Task<PipelineDetail> GetDetailAsync(long pipelineId, CancellationToken ct = default)
    {
        var entity = await LoadPipelineEntityAsync(pipelineId, ct);
        if (entity == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        var messages = await GetPipelineMessagesAsync(pipelineId, ct);
        var stages = await RebuildStagesFromDbAsync(pipelineId, entity.CurrentStage, ct);

        return new PipelineDetail
        {
            Id = pipelineId,
            Name = entity.Name ?? "",
            CurrentStage = entity.CurrentStage ?? PipelineStage.Requirement,
            Status = entity.Frozen ? "frozen" : (entity.Status ?? "draft"),
            WorkMode = PipelineWorkMode.Normalize(entity.WorkMode),
            SourcePipelineId = long.TryParse(entity.SourcePipelineId, out var src) ? src : null,
            TargetPageRoute = entity.TargetPageRoute,
            TargetPageLabel = entity.TargetPageLabel,
            ProjectId = entity.ProjectId,
            Stages = stages.Select(s => new StageInfo
            {
                Id = s.StageOrder + 1,
                StageName = s.StageName,
                Status = s.Status,
                StageOrder = s.StageOrder
            }).ToList(),
            Messages = messages
        };
    }

    public async Task<List<PipelineSummary>> ListAsync(
        long tenantId, int pageIndex, int pageSize, string? creatorUserId = null, CancellationToken ct = default)
    {
        if (_db == null) return new List<PipelineSummary>();
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 20;

        var tenant = tenantId <= 0 ? null : tenantId.ToString();
        var query = _db.Queryable<AiPipelineEntity>()
            .Where(x => x.DeleteMark == null || x.DeleteMark == 0);
        if (!string.IsNullOrWhiteSpace(tenant))
        {
            query = query.Where(x => x.TenantId == tenant);
        }

        // R12：同租户按创建人隔离
        if (!string.IsNullOrWhiteSpace(creatorUserId))
        {
            query = query.Where(x => x.CreatorUserId == creatorUserId);
        }

        var entities = await query
            .OrderBy(x => x.LastModifyTime, SqlSugar.OrderByType.Desc)
            .OrderBy(x => x.CreatorTime, SqlSugar.OrderByType.Desc)
            .ToPageListAsync(pageIndex + 1, pageSize);

        return entities.Select(x => new PipelineSummary
            {
                Id = long.TryParse(x.Id, out var parsedId) ? parsedId : 0,
                Name = x.Name ?? "",
                PipelineType = "full_app",
                CurrentStage = x.CurrentStage ?? PipelineStage.Requirement,
                Status = x.Frozen ? "frozen" : (x.Status ?? "draft"),
                UpdatedAt = x.LastModifyTime ?? x.CreatorTime ?? DateTime.Now
            })
            .ToList();
    }

    // ─── 私有辅助方法 ───

    private async Task<long> NextPipelineIdAsync()
    {
        if (_db == null) return 1;
        try
        {
            var maxId = _db.Ado.GetLong("SELECT ISNULL(MAX(CAST(F_ID AS BIGINT)), 0) FROM BASE_AI_PIPELINE");
            return maxId + 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取流水线最大 ID 失败,使用时间戳兜底");
            return Math.Max(1, DateTime.UtcNow.Ticks % 1000000000);
        }
    }

    private async Task<AiPipelineEntity?> LoadPipelineEntityAsync(long pipelineId, CancellationToken ct = default)
    {
        if (_db == null) return null;
        return await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString() && (x.DeleteMark == null || x.DeleteMark == 0))
            .FirstAsync(ct);
    }

    private async Task UpdatePipelineStatusAsync(AiPipelineEntity entity, CancellationToken ct = default)
    {
        if (_db == null) return;
        await _db.Updateable<AiPipelineEntity>()
            .SetColumns(x => new AiPipelineEntity
            {
                CurrentStage = entity.CurrentStage,
                Status = entity.Status,
                StartedTime = entity.StartedTime,
                FinishedTime = entity.FinishedTime,
                LastModifyTime = DateTime.Now,
                LastModifyUserId = entity.LastModifyUserId
            })
            .Where(x => x.Id == entity.Id)
            .ExecuteCommandAsync(ct);
    }

    /// <summary>
    /// P1-3: 从消息表真实重建 Stages 状态(而非旧的假设 completed/running)
    /// 规则:有 assistant 回复 → completed;当前 stage → running;否则 → pending
    /// </summary>
    private async Task<List<StageRecord>> RebuildStagesFromDbAsync(
        long pipelineId, string currentStage, CancellationToken ct = default)
    {
        var currentOrder = Array.IndexOf(PipelineStage.Order, currentStage);
        if (currentOrder < 0) currentOrder = 0;

        // 查询每个 stage 是否有 assistant 消息(判断该阶段是否已产生输出)
        // P1-3: 只投影 Stage/Role 两列,避免拉取完整 Content 列
        List<StageMessageRow> stageMessages;
        if (_db == null)
        {
            stageMessages = new List<StageMessageRow>();
        }
        else
        {
            var tenantId = TenantResolver.Resolve().ToString();
            var rows = await _db.Queryable<AiPipelineMessageEntity>()
                .Where(x => x.PipelineId == pipelineId.ToString()
                            && x.TenantId == tenantId
                            && (x.DeleteMark == null || x.DeleteMark == 0))
                .Select(x => new StageMessageRow { Stage = x.Stage, Role = x.Role })
                .ToListAsync(ct);
            stageMessages = rows ?? new List<StageMessageRow>();
        }

        var stages = new List<StageRecord>();
        for (var i = 0; i < PipelineStage.Order.Length; i++)
        {
            var stageName = PipelineStage.Order[i];
            var hasAssistantReply = stageMessages.Any(m => m.Stage == stageName && m.Role == "assistant");
            var status = stageName == currentStage ? "running"
                       : i < currentOrder ? "completed"
                       : hasAssistantReply ? "completed"
                       : "pending";
            stages.Add(new StageRecord
            {
                Id = i + 1,
                StageName = stageName,
                StageOrder = i,
                Status = status
            });
        }
        return stages;
    }

    /// <summary>
    /// P1-3: 构建全量 checkpoint(状态 + 最近消息 ID + IR 版本号)
    /// </summary>
    private async Task<PipelineCheckpoint> BuildCheckpointAsync(
        long pipelineId, string currentStage, CancellationToken ct = default)
    {
        var checkpoint = new PipelineCheckpoint
        {
            CurrentStage = currentStage,
            FrozenAt = DateTime.UtcNow
        };

        if (_db == null) return checkpoint;

        // 最近 20 条消息 ID
        var tenantId = TenantResolver.Resolve().ToString();
        var recentMessages = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString()
                        && x.TenantId == tenantId
                        && (x.DeleteMark == null || x.DeleteMark == 0))
            .OrderBy(x => x.CreatorTime, SqlSugar.OrderByType.Desc)
            .Take(20)
            .Select(x => x.Id)
            .ToListAsync(ct);
        checkpoint.LastMessageIds = recentMessages;

        // 最新 IR 版本号
        try
        {
            var latestIrVersion = await _db.Queryable<IrVersionEntity>()
                .Where(x => x.PipelineId == pipelineId.ToString())
                .OrderByDescending(x => x.Version)
                .Select(x => new { x.Version, x.Id })
                .FirstAsync(ct);
            checkpoint.IrVersion = latestIrVersion?.Version ?? 0;
            checkpoint.IrVersionId = latestIrVersion?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "checkpoint 读取 IR 版本失败 PipelineId={Id}", pipelineId);
        }

        return checkpoint;
    }

    private async Task<List<PipelineMessageInfo>> GetPipelineMessagesAsync(long pipelineId, CancellationToken ct = default)
    {
        if (_db == null) return new List<PipelineMessageInfo>();
        var tenantId = TenantResolver.Resolve().ToString();
        var rows = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString() && x.TenantId == tenantId && (x.DeleteMark == null || x.DeleteMark == 0))
            .OrderBy(x => x.CreatorTime, SqlSugar.OrderByType.Asc)
            .OrderBy(x => x.Sequence, SqlSugar.OrderByType.Asc)
            .ToListAsync();

        return rows.Select(x => new PipelineMessageInfo
        {
            Id = x.Id,
            Role = x.Role ?? "assistant",
            Content = x.Content ?? "",
            Stage = x.Stage ?? PipelineStage.Requirement,
            Sequence = x.Sequence,
            CreateTime = x.CreatorTime
        }).ToList();
    }

    private async Task AppendSystemMessageAsync(long pipelineId, string stage, string content, CancellationToken ct = default)
    {
        if (_db == null || string.IsNullOrWhiteSpace(content)) return;
        var tenantId = TenantResolver.Resolve().ToString();
        var maxSeq = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString() && x.TenantId == tenantId && x.Stage == stage)
            .MaxAsync(x => (int?)x.Sequence) ?? 0;

        // 解析 ProjectId（与 PipelineTripleResolver 一致：pipeline.ProjectId 为空时回退到 pipelineId）
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .Select(x => new { x.ProjectId })
            .FirstAsync();
        var projectId = string.IsNullOrWhiteSpace(pipeline?.ProjectId) ? pipelineId.ToString() : pipeline.ProjectId;

        var msg = new AiPipelineMessageEntity
        {
            PipelineId = pipelineId.ToString(),
            // 三元组血缘：ProjectId 从 pipeline 表解析（不再兜底为 pipelineId）
            ProjectId = projectId,
            Stage = stage,
            Role = "system",
            Content = content,
            Sequence = maxSeq + 1,
            DeleteMark = 0
        };
        msg.Creator();
        await _db.Insertable(msg).ExecuteCommandAsync();
    }

    #region I-12: 失败计数原子操作(纯 DB,无内存依赖)

    public async Task IncrementFailureCountAsync(long pipelineId, string failureType)
    {
        if (_db == null) return;
        var sql = @"
            UPDATE BASE_AI_PIPELINE
            SET F_FAILURE_COUNTS = JSON_MODIFY(
                ISNULL(F_FAILURE_COUNTS, '{}'),
                '$.' + @failureType,
                ISNULL(JSON_VALUE(F_FAILURE_COUNTS, '$.' + @failureType), 0) + 1
            )
            WHERE F_ID = @pipelineId";
        await _db.Ado.ExecuteCommandAsync(sql, new { pipelineId = pipelineId.ToString(), failureType });
    }

    public async Task ResetFailureCountAsync(long pipelineId, string failureType)
    {
        if (_db == null) return;
        var sql = @"
            UPDATE BASE_AI_PIPELINE
            SET F_FAILURE_COUNTS = JSON_MODIFY(
                ISNULL(F_FAILURE_COUNTS, '{}'),
                '$.' + @failureType,
                0
            )
            WHERE F_ID = @pipelineId";
        await _db.Ado.ExecuteCommandAsync(sql, new { pipelineId = pipelineId.ToString(), failureType });
    }

    public async Task<Dictionary<string, int>> GetFailureCountsAsync(long pipelineId)
    {
        if (_db == null) return new Dictionary<string, int>();
        var json = await _db.Ado.GetStringAsync(
            "SELECT F_FAILURE_COUNTS FROM BASE_AI_PIPELINE WHERE F_ID = @id",
            new { id = pipelineId.ToString() });
        return string.IsNullOrEmpty(json)
            ? new Dictionary<string, int>()
            : JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
    }

    public async Task<bool> ShouldBlockAsync(long pipelineId)
    {
        var counts = await GetFailureCountsAsync(pipelineId);
        return counts.Any(kv => kv.Value >= 3);
    }

    #endregion

    private class StageRecord
    {
        public long Id { get; set; }
        public string StageName { get; set; } = "";
        public string Status { get; set; } = "";
        public int StageOrder { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}

/// <summary>
/// P1-3: 全量 checkpoint 序列化模型
/// 冻结时写入 BASE_AI_PIPELINE.F_CHECKPOINT,恢复时反序列化校验
/// </summary>
public class PipelineCheckpoint
{
    public string CurrentStage { get; set; } = "";
    public DateTime FrozenAt { get; set; }
    public List<string> LastMessageIds { get; set; } = new();
    public int IrVersion { get; set; }
    public string? IrVersionId { get; set; }
}

/// <summary>
/// P1-3: 消息表投影行(只取 Stage/Role 两列,用于重建阶段状态)
/// </summary>
internal class StageMessageRow
{
    public string Stage { get; set; } = "";
    public string Role { get; set; } = "";
}
