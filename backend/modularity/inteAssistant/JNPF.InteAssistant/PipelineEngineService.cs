using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Common;
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

    public PipelineEngineService(ILogger<PipelineEngineService> logger, SqlSugar.ISqlSugarClient db = null!)
    {
        _logger = logger;
        _db = db;
    }

    public Task<PipelineResult> CreateAsync(
        PipelineCreateRequest request, long tenantId, long userId, CancellationToken ct = default)
    {
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

    public Task<PipelineResult> StartAsync(long pipelineId, CancellationToken ct = default)
    {
        if (!_pipelines.TryGetValue(pipelineId, out var state))
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        state.Status = "running";
        state.StartedAt = DateTime.UtcNow;

        _logger.LogInformation("流水线启动: ID={Id}", pipelineId);

        return Task.FromResult(new PipelineResult
        {
            PipelineId = pipelineId,
            Name = state.Name,
            CurrentStage = state.CurrentStage,
            Status = state.Status
        });
    }

    public Task<StageResult> ExecuteStageAsync(
        long pipelineId, string stageName, CancellationToken ct = default)
    {
        if (!_pipelines.TryGetValue(pipelineId, out var state))
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        if (!PipelineStage.Order.Contains(stageName))
            throw new ArgumentException($"未知阶段: {stageName}");

        state.CurrentStage = stageName;
        state.Status = "running";

        var stage = new StageRecord
        {
            Id = state.Stages.Count + 1,
            StageName = stageName,
            Status = "running",
            StageOrder = Array.IndexOf(PipelineStage.Order, stageName),
            StartedAt = DateTime.UtcNow
        };
        state.Stages.Add(stage);

        _logger.LogInformation("流水线阶段执行: ID={Id}, Stage={Stage}", pipelineId, stageName);

        return Task.FromResult(new StageResult
        {
            StageId = stage.Id,
            StageName = stageName,
            Status = "running"
        });
    }

    public Task<StageResult> ConfirmStageAsync(
        long stageId, StageConfirmation confirmation, CancellationToken ct = default)
    {
        var pipeline = _pipelines.Values
            .FirstOrDefault(p => p.Stages.Any(s => s.Id == stageId))
            ?? throw new InvalidOperationException($"阶段 {stageId} 不存在");

        var stage = pipeline.Stages.First(s => s.Id == stageId);
        stage.Status = confirmation.Approved ? "approved" : "review";
        stage.CompletedAt = DateTime.UtcNow;

        if (confirmation.Approved)
        {
            var nextStage = PipelineStage.GetNext(stage.StageName);
            if (nextStage != null)
            {
                pipeline.CurrentStage = nextStage;
            }
            else
            {
                pipeline.Status = "completed";
            }
        }

        _logger.LogInformation("阶段确认: StageId={Id}, Approved={Approved}, Next={Next}",
            stageId, confirmation.Approved, pipeline.CurrentStage);

        return Task.FromResult(new StageResult
        {
            StageId = stage.Id,
            StageName = stage.StageName,
            Status = stage.Status
        });
    }

    public Task<PipelineDetail> GetDetailAsync(long pipelineId, CancellationToken ct = default)
    {
        if (!_pipelines.TryGetValue(pipelineId, out var state))
            throw new InvalidOperationException($"流水线 {pipelineId} 不存在");

        return Task.FromResult(new PipelineDetail
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
            }).ToList()
        });
    }

    public Task<List<PipelineSummary>> ListAsync(
        long tenantId, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var list = _pipelines.Values
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(p => new PipelineSummary
            {
                Id = p.Id,
                Name = p.Name,
                PipelineType = p.PipelineType,
                CurrentStage = p.CurrentStage,
                Status = p.Status,
                UpdatedAt = p.CreatedAt
            })
            .ToList();

        return Task.FromResult(list);
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
