namespace JNPF.InteAssistant.Interfaces;

using JNPF.InteAssistant.Entitys.Enum;

/// <summary>
/// 五阶段 AI 流水线引擎
/// 阶段: requirement → architecture → design → development → delivery
/// 对齐前端 src/core/ai/pipeline/orchestrator.ts OrchestratorAgent
/// </summary>
public interface IPipelineEngine
{
    /// <summary>
    /// 创建流水线
    /// </summary>
    Task<PipelineResult> CreateAsync(
        PipelineCreateRequest request, long tenantId, long userId, CancellationToken ct = default);

    /// <summary>
    /// 启动流水线（进入第一阶段）
    /// </summary>
    Task<PipelineResult> StartAsync(long pipelineId, CancellationToken ct = default);

    /// <summary>
    /// 执行指定阶段
    /// </summary>
    Task<StageResult> ExecuteStageAsync(
        long pipelineId, string stageName, CancellationToken ct = default);

    /// <summary>
    /// 人工确认阶段产出
    /// </summary>
    Task<StageResult> ConfirmStageAsync(
        long stageId, StageConfirmation confirmation, CancellationToken ct = default);

    /// <summary>
    /// 回退到指定阶段
    /// </summary>
    Task<StageResult> RollbackAsync(
        long pipelineId, string targetStage, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// 冻结流水线(全量 checkpoint:状态机 + 最近消息 ID + IR 版本号)
    /// </summary>
    Task<PipelineResult> FreezeAsync(
        long pipelineId, string? reason = null, string? frozenBy = null, CancellationToken ct = default);

    /// <summary>
    /// 恢复流水线(从 checkpoint 重建状态,生成新会话)
    /// </summary>
    Task<PipelineResult> ResumeAsync(long pipelineId, CancellationToken ct = default);

    /// <summary>
    /// 获取流水线详情
    /// </summary>
    Task<PipelineDetail> GetDetailAsync(long pipelineId, CancellationToken ct = default);

    /// <summary>
    /// 分页查询流水线列表。
    /// <paramref name="creatorUserId"/> 非空时按创建人过滤（R12 同租户隔离；超管传 null）。
    /// </summary>
    Task<List<PipelineSummary>> ListAsync(
        long tenantId, int pageIndex, int pageSize, string? creatorUserId = null, CancellationToken ct = default);
}

// ─── DTO ───

public record PipelineCreateRequest
{
    public string Name { get; init; } = "";
    public string PipelineType { get; init; } = "full_app";
    public string UserRequirement { get; init; } = "";
}

public record PipelineResult
{
    public long PipelineId { get; init; }
    public string Name { get; init; } = "";
    public string CurrentStage { get; init; } = "";
    public string Status { get; init; } = "";
}

public record PipelineDetail
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string CurrentStage { get; init; } = "";
    public string Status { get; init; } = "";
    public string WorkMode { get; init; } = PipelineWorkMode.Greenfield;
    public long? SourcePipelineId { get; init; }
    public string? TargetPageRoute { get; init; }
    public string? TargetPageLabel { get; init; }
    public string? ProjectId { get; init; }
    public List<StageInfo> Stages { get; init; } = new();
    public List<PipelineMessageInfo> Messages { get; init; } = new();
}

public record PipelineSummary
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string PipelineType { get; init; } = "";
    public string CurrentStage { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime UpdatedAt { get; init; }
}

public record StageInfo
{
    public long Id { get; init; }
    public string StageName { get; init; } = "";
    public string Status { get; init; } = "";
    public int StageOrder { get; init; }
    public int? TokensUsed { get; init; }
}

public record StageResult
{
    public long StageId { get; init; }
    public string StageName { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Output { get; init; }

    /// <summary>SUP-01a：用户确认通过时的阶段（推进前）</summary>
    public string? ConfirmedStage { get; init; }

    /// <summary>SUP-01a：确认后已调度触发的 SkillId 列表</summary>
    public IReadOnlyList<string>? TriggeredSkillIds { get; init; }

    /// <summary>SUP-01a：后台任务名（便于日志/诊断）</summary>
    public IReadOnlyList<string>? BackgroundTaskNames { get; init; }
}

public record PipelineMessageInfo
{
    public string Id { get; init; } = "";
    public string Role { get; init; } = "";
    public string Content { get; init; } = "";
    public string Stage { get; init; } = "";
    public int Sequence { get; init; }
    public DateTime? CreateTime { get; init; }
}

public record StageConfirmation
{
    public bool Approved { get; init; }
    public string Comment { get; init; } = "";
}
