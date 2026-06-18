using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace JNPF.InteAssistant;

/// <summary>
/// 五阶段流水线引擎实现
/// 阶段: requirement → architecture → design → development → delivery
/// </summary>
public class PipelineEngineService : IPipelineEngine, ISingleton
{
    private readonly ILogger<PipelineEngineService> _logger;
    private readonly SqlSugar.ISqlSugarClient _db;
    private readonly ConcurrentDictionary<long, PipelineState> _pipelines = new();
    private long _nextId = 1;
    private int _idSeedLoaded;

    public PipelineEngineService(ILogger<PipelineEngineService> logger, SqlSugar.ISqlSugarClient db = null!)
    {
        _logger = logger;
        _db = db;
    }

    public Task<PipelineResult> CreateAsync(
        PipelineCreateRequest request, long tenantId, long userId, CancellationToken ct = default)
    {
        EnsureNextIdSeedLoaded();
        var id = Interlocked.Increment(ref _nextId);
        var state = new PipelineState
        {
            Id = id,
            Name = request.Name,
            PipelineType = request.PipelineType,
            UserRequirement = request.UserRequirement,
            CurrentStage = PipelineStage.Requirement,
            Status = "draft",
            TenantId = tenantId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _pipelines[id] = state;

        _logger.LogInformation("流水线创建: ID={Id}, Name={Name}, Tenant={TenantId}",
            id, request.Name, tenantId);

        return Task.FromResult(new PipelineResult
        {
            PipelineId = id,
            Name = request.Name,
            CurrentStage = PipelineStage.Requirement,
            Status = "draft"
        });
    }

    public async Task<PipelineResult> StartAsync(long pipelineId, CancellationToken ct = default)
    {
        var state = await GetPipelineStateAsync(pipelineId, ct);
        if (state == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        state.Status = "running";
        state.StartedAt = DateTime.UtcNow;
        await PersistPipelineSnapshotAsync(state, ct);

        _logger.LogInformation("流水线启动: ID={Id}", pipelineId);

        return new PipelineResult
        {
            PipelineId = pipelineId,
            Name = state.Name,
            CurrentStage = state.CurrentStage,
            Status = state.Status
        };
    }

    public async Task<StageResult> ExecuteStageAsync(
        long pipelineId, string stageName, CancellationToken ct = default)
    {
        var state = await GetPipelineStateAsync(pipelineId, ct);
        if (state == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        if (!PipelineStage.Order.Contains(stageName))
            throw new ArgumentException($"未知阶段: {stageName}");

        state.CurrentStage = stageName;
        state.Status = "running";
        var stageOrder = Array.IndexOf(PipelineStage.Order, stageName);
        var stage = state.Stages.FirstOrDefault(s => s.StageName == stageName);
        if (stage == null)
        {
            stage = new StageRecord
            {
                Id = state.Stages.Count + 1,
                StageName = stageName,
                StageOrder = stageOrder,
                StartedAt = DateTime.UtcNow
            };
            state.Stages.Add(stage);
        }

        stage.Status = "running";
        if (stage.StartedAt == default) stage.StartedAt = DateTime.UtcNow;
        await PersistPipelineSnapshotAsync(state, ct);

        _logger.LogInformation("流水线阶段执行: ID={Id}, Stage={Stage}", pipelineId, stageName);

        return new StageResult
        {
            StageId = stage.Id,
            StageName = stageName,
            Status = "running"
        };
    }

    public async Task<StageResult> ConfirmStageAsync(
        long stageId, StageConfirmation confirmation, CancellationToken ct = default)
    {
        // 前端当前传的是 pipelineId；这里兼容「按 pipelineId」和「按 stageId」两种调用。
        var pipeline = await GetPipelineStateAsync(stageId, ct);
        StageRecord? stage = null;

        if (pipeline != null)
        {
            stage = pipeline.Stages.FirstOrDefault(s => s.StageName == pipeline.CurrentStage)
                ?? pipeline.Stages.OrderByDescending(s => s.StageOrder).FirstOrDefault();
        }
        else
        {
            pipeline = _pipelines.Values.FirstOrDefault(p => p.Stages.Any(s => s.Id == stageId));
            if (pipeline != null) stage = pipeline.Stages.FirstOrDefault(s => s.Id == stageId);
        }

        if (pipeline == null || stage == null)
            throw new InvalidOperationException($"阶段 {stageId} 不存在");

        stage.Status = confirmation.Approved ? "approved" : "review";
        stage.CompletedAt = DateTime.UtcNow;
        var systemText = confirmation.Approved
            ? $"✅ 已确认「{stage.StageName}」阶段"
            : $"🛠️ 已退回「{stage.StageName}」阶段，请根据意见继续完善";

        if (confirmation.Approved)
        {
            var nextStage = PipelineStage.GetNext(stage.StageName);
            if (nextStage != null)
            {
                pipeline.CurrentStage = nextStage;
                var nextOrder = Array.IndexOf(PipelineStage.Order, nextStage);
                if (!pipeline.Stages.Any(s => s.StageName == nextStage))
                {
                    pipeline.Stages.Add(new StageRecord
                    {
                        Id = pipeline.Stages.Count + 1,
                        StageName = nextStage,
                        StageOrder = nextOrder,
                        Status = "pending",
                        StartedAt = DateTime.UtcNow
                    });
                }
                systemText = $"✅ 已进入阶段 {nextOrder + 1}：{nextStage}";
            }
            else
            {
                pipeline.Status = "completed";
                systemText = "🎉 已完成全部阶段";
            }
        }
        else
        {
            pipeline.Status = "review";
        }

        await PersistPipelineSnapshotAsync(pipeline, ct);
        await AppendSystemMessageAsync(pipeline.Id, pipeline.CurrentStage, systemText, ct);

        _logger.LogInformation("阶段确认: StageId={Id}, Approved={Approved}, Next={Next}",
            stageId, confirmation.Approved, pipeline.CurrentStage);

        return new StageResult
        {
            StageId = stage.Id,
            StageName = stage.StageName,
            Status = stage.Status
        };
    }

    public async Task<StageResult> RollbackAsync(
        long pipelineId, string targetStage, string? reason = null, CancellationToken ct = default)
    {
        var pipeline = await GetPipelineStateAsync(pipelineId, ct);
        if (pipeline == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");
        if (!PipelineStage.Order.Contains(targetStage))
            throw new ArgumentException($"未知阶段: {targetStage}");

        var targetOrder = Array.IndexOf(PipelineStage.Order, targetStage);
        pipeline.CurrentStage = targetStage;
        pipeline.Status = "running";

        pipeline.Stages.RemoveAll(s => s.StageOrder > targetOrder);
        foreach (var stage in pipeline.Stages)
        {
            stage.Status = stage.StageOrder < targetOrder ? "completed" : "running";
            if (stage.StageOrder == targetOrder) stage.CompletedAt = null;
        }

        if (!pipeline.Stages.Any(s => s.StageName == targetStage))
        {
            pipeline.Stages.Add(new StageRecord
            {
                Id = pipeline.Stages.Count + 1,
                StageName = targetStage,
                Status = "running",
                StageOrder = targetOrder,
                StartedAt = DateTime.UtcNow
            });
        }

        await PersistPipelineSnapshotAsync(pipeline, ct);
        var rollbackMessage = string.IsNullOrWhiteSpace(reason)
            ? $"↩️ 已回退到阶段 {targetOrder + 1}：{targetStage}"
            : $"↩️ 已回退到阶段 {targetOrder + 1}：{targetStage}，原因：{reason}";
        await AppendSystemMessageAsync(pipeline.Id, targetStage, rollbackMessage, ct);

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

    public async Task<PipelineDetail> GetDetailAsync(long pipelineId, CancellationToken ct = default)
    {
        var state = await GetPipelineStateAsync(pipelineId, ct);
        if (state == null)
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        var messages = await GetPipelineMessagesAsync(pipelineId, ct);

        return new PipelineDetail
        {
            Id = state.Id,
            Name = state.Name,
            CurrentStage = state.CurrentStage,
            Status = state.Status,
            Stages = state.Stages.Select(s => new StageInfo
            {
                Id = s.Id,
                StageName = s.StageName,
                Status = s.Status,
                StageOrder = s.StageOrder
            }).ToList(),
            Messages = messages
        };
    }

    public async Task<List<PipelineSummary>> ListAsync(
        long tenantId, int pageIndex, int pageSize, CancellationToken ct = default)
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

        var entities = await query
            .OrderBy(x => x.LastModifyTime, SqlSugar.OrderByType.Desc)
            .OrderBy(x => x.CreatorTime, SqlSugar.OrderByType.Desc)
            .ToPageListAsync(pageIndex + 1, pageSize);

        var list = entities.Select(x => new PipelineSummary
            {
                Id = long.TryParse(x.Id, out var parsedId) ? parsedId : 0,
                Name = x.Name ?? "",
                PipelineType = "full_app",
                CurrentStage = x.CurrentStage ?? PipelineStage.Requirement,
                Status = x.Status ?? "draft",
                UpdatedAt = x.LastModifyTime ?? x.CreatorTime ?? DateTime.Now
            })
            .ToList();

        return list;
    }

    private class PipelineState
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string PipelineType { get; set; } = "";
        public string UserRequirement { get; set; } = "";
        public string CurrentStage { get; set; } = "";
        public string Status { get; set; } = "draft";
        public long TenantId { get; set; }
        public long UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public List<StageRecord> Stages { get; set; } = new();
    }

    private void EnsureNextIdSeedLoaded()
    {
        if (Interlocked.CompareExchange(ref _idSeedLoaded, 1, 0) != 0) return;
        if (_db == null) return;

        try
        {
            var maxId = _db.Ado.GetLong("SELECT ISNULL(MAX(CAST(F_ID AS BIGINT)), 0) FROM BASE_AI_PIPELINE");
            _nextId = Math.Max(_nextId, maxId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载流水线 ID 种子失败，继续使用内存自增");
        }
    }

    private async Task<PipelineState?> GetPipelineStateAsync(long pipelineId, CancellationToken ct = default)
    {
        if (_pipelines.TryGetValue(pipelineId, out var memoryState)) return memoryState;
        if (_db == null) return null;

        var entity = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString() && (x.DeleteMark == null || x.DeleteMark == 0))
            .FirstAsync();
        if (entity == null) return null;

        var stage = string.IsNullOrWhiteSpace(entity.CurrentStage) ? PipelineStage.Requirement : entity.CurrentStage;
        if (!PipelineStage.Order.Contains(stage)) stage = PipelineStage.Requirement;

        var hydrated = new PipelineState
        {
            Id = pipelineId,
            Name = entity.Name ?? $"Pipeline-{pipelineId}",
            CurrentStage = stage,
            Status = string.IsNullOrWhiteSpace(entity.Status) ? "draft" : entity.Status,
            CreatedAt = entity.CreatorTime ?? DateTime.UtcNow,
            StartedAt = entity.StartedTime,
            TenantId = long.TryParse(entity.TenantId, out var tenantId) ? tenantId : 0
        };

        var currentStageOrder = Array.IndexOf(PipelineStage.Order, stage);
        if (currentStageOrder < 0) currentStageOrder = 0;

        for (var i = 0; i <= currentStageOrder; i++)
        {
            hydrated.Stages.Add(new StageRecord
            {
                Id = i + 1,
                StageName = PipelineStage.Order[i],
                Status = i < currentStageOrder ? "completed" : "running",
                StageOrder = i,
                StartedAt = entity.StartedTime ?? hydrated.CreatedAt
            });
        }

        _pipelines[pipelineId] = hydrated;
        _logger.LogInformation("流水线状态回填: ID={Id}, Stage={Stage}, Status={Status}", pipelineId, stage, hydrated.Status);
        return hydrated;
    }

    private async Task<List<PipelineMessageInfo>> GetPipelineMessagesAsync(long pipelineId, CancellationToken ct = default)
    {
        if (_db == null) return new List<PipelineMessageInfo>();
        var rows = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString() && (x.DeleteMark == null || x.DeleteMark == 0))
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

    private async Task PersistPipelineSnapshotAsync(PipelineState state, CancellationToken ct = default)
    {
        if (_db == null) return;
        var entity = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == state.Id.ToString() && (x.DeleteMark == null || x.DeleteMark == 0))
            .FirstAsync();
        if (entity == null) return;

        entity.CurrentStage = state.CurrentStage;
        entity.Status = state.Status;
        if (state.StartedAt.HasValue && !entity.StartedTime.HasValue) entity.StartedTime = state.StartedAt;
        if (state.Status == "completed") entity.FinishedTime = DateTime.Now;
        entity.LastModify();

        await _db.Updateable(entity)
            .UpdateColumns(x => new
            {
                x.CurrentStage,
                x.Status,
                x.StartedTime,
                x.FinishedTime,
                x.LastModifyTime,
                x.LastModifyUserId
            })
            .ExecuteCommandAsync();
    }

    private async Task AppendSystemMessageAsync(long pipelineId, string stage, string content, CancellationToken ct = default)
    {
        if (_db == null || string.IsNullOrWhiteSpace(content)) return;
        var maxSeq = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString() && x.Stage == stage)
            .MaxAsync(x => (int?)x.Sequence) ?? 0;

        var msg = new AiPipelineMessageEntity
        {
            PipelineId = pipelineId.ToString(),
            Stage = stage,
            Role = "system",
            Content = content,
            Sequence = maxSeq + 1,
            DeleteMark = 0
        };
        msg.Creator();
        await _db.Insertable(msg).ExecuteCommandAsync();
    }

    #region I-12: 失败计数原子操作

    /// <summary>
    /// 原子递增失败计数（JSON_MODIFY，无并发覆盖风险）
    /// </summary>
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

    /// <summary>
    /// 归零指定类型计数（仅归零该类型，其他类型保留）
    /// </summary>
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

    /// <summary>
    /// 读取失败计数（服务重启时恢复内存缓存）
    /// </summary>
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

    /// <summary>
    /// 检查是否触发熔断（任一类型 >= 3）
    /// </summary>
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
